﻿using IXLA.Sdk.Xp24.Protocol.Commands.Transport;
using Polly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using NLog;
using System.Web.Services.Description;

namespace IXLA.Sdk.Xp24
{

    public class MachineClientOptions
    {
        public bool ThrowExceptionOnServerError { get; set; }
    }

    /// <summary>
    /// This type abstracts the network layer and handles the application protocol, based on the type of command
    /// waits for acks/execution
    /// </summary>
    public class MachineClient : IDisposable
    {
        private readonly SemaphoreSlim _semaphore = new(1, 1);

        /// <summary>
        /// Handler for interlock events raised by the machine when the laser interlock status changes
        /// </summary>
        public delegate void InterlockEventHandler(MachineClient sender, InterlockNotificationEventArgs args);
        /// <summary>
        /// Raised when the client receives <command name="interlock" />
        /// </summary>
        public event InterlockEventHandler OnInterlockNotification;

        private bool _disposing = false;
        private bool _disposed = false;

        private readonly TcpClient _tcpClient = new()
        {
            LingerState = new LingerOption(true, 0)
        };

        private StreamReader _reader;
        private StreamWriter _writer;

        public MachineClient()
        {
        }

        // NLog logger initialization
        private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

        public async Task ConnectAsync(string hostname, int Port)
        {
            await _tcpClient.ConnectAsync(hostname, Port).ConfigureAwait(false);
            var networkStream = _tcpClient.GetStream();
            _writer = new StreamWriter(networkStream) { AutoFlush = true };
            _reader = new StreamReader(networkStream);
            await ConsumeWelcomeMessage().ConfigureAwait(false);
            Logger.Info($" {hostname}:{Port} --> Connected successfully To Ixla Printer.");
        }

        public async Task ConnectWithRetryAsync(string hostname, int port)
        {
            var retryPolicy = Policy
                .Handle<SocketException>()
                  .Or<TimeoutException>()
                .WaitAndRetryAsync(2, retryAttempt => TimeSpan.FromSeconds(Math.Pow(1, retryAttempt)),
                    onRetry: (exception, timeSpan, context) =>
                    {
                        Logger.Warn($" {hostname}:{port} --> starting connecting in retry Mode....");
                        Logger.Warn($" {hostname}:{port} --> Retrying due to {exception.GetType().Name}. Attempting again in {timeSpan.Seconds} seconds.");
                    });

            await retryPolicy.ExecuteAsync(async () =>
            {
                await ConnectAsync(hostname, port);
            });
        }



        /// <summary>
        /// Used only by ConnectAsync to consume the welcome message from the machine
        /// </summary>
        private async Task ConsumeWelcomeMessage()
        {
            string content; 
            content = await _reader.ReadLineAsync().ConfigureAwait(false);
            content = await _reader.ReadLineAsync().ConfigureAwait(false);
        }



        /// <summary>
        /// Sends \r\n to the server before disposing the Tcp client/stream. This was 
        /// </summary>
        public async Task GracefulDisconnectAsync()
        {
            if (!_disposed)
            {
                try
                {
                    await _tcpClient.GetStream().WriteAsync(new[] { (byte)'\r', (byte)'\n' },0,2).ConfigureAwait(false);
                }
                catch
                {
                }
            }

            Dispose();
        }

        /// <summary>
        /// Sends a Machine command to the server and waits for responses accordingly. 
        /// </summary>
        /// <param name="command"></param>
        /// <param name="timeout"></param>
        /// <param name="cancellationToken"></param>
        /// <typeparam name="TResponse"></typeparam>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="Exception"></exception>
        public async Task<TResponse> SendCommandAsync<TResponse>(MachineCommand command,
            int timeout = Timeout.Infinite,
            CancellationToken cancellationToken = default) where TResponse : MachineResponseBase
        {
            try
            {
                // i use a semaphore to avoid mixing responses. If you need to parallelize some operations
                // (typically encoding and marking) you can open a parallel connection (on port 5556 for example) 
                // and send the encoding commands there. 
                await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

                // careful, this does not guarantee that the tcp connection is still open. The connected flag only indicates
                // that the last packet transmission was successful
                if (!_tcpClient.Connected)
                    throw new InvalidOperationException("Tcp client disconnected");

                var payload = command.Serialize();
                await _writer.WriteLineAsync(payload).ConfigureAwait(false);

                // eject if any error occur
                var payloadEject = new EjectCommand().Serialize();

                // wait command valid=true/false from the server
                using var ackXmlReader = await WaitAck();
                // instantiate the response object
                var response = (TResponse)Activator.CreateInstance(typeof(TResponse), command);

                // if this throws is a bug (response object with wrong constructor) 
                if (response is null) {
                    //await _writer.WriteLineAsync(payloadEject).ConfigureAwait(false);//new added for eject passport if error occur
                    throw new Exception($"Failed to create response object via reflection for type {typeof(TResponse).Name}"); }

                // populate response properties
                response.Hydrate(ackXmlReader, true);
                // if the server returns valid=false then we will not receive further messages 
                if (!response.Valid) {
                    //await _writer.WriteLineAsync(payloadEject).ConfigureAwait(false);//new added for eject passport if error occur
                    throw new Exception($"Invalid command {command.Name}. Error message: {response.Error}"); }
                if (!command.IsAsync) return response;

                using var executionXmlReader = await WaitCompletion(timeout).ConfigureAwait(false);

                // for async commands usually the second response (executed true/false) carries also date
                response.Hydrate(executionXmlReader, false);
                if (!response.Executed) {
                    //await _writer.WriteLineAsync(payloadEject).ConfigureAwait(false);//new added for eject passport if error occur
                    throw new Exception($"Failed to execute command {command.Name}. Error message: {response.Error}"); }

                return response;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Wait for <command valid="true/false" /> from the server
        /// </summary>
        private async Task<XmlReader> WaitAck(int timeout = Timeout.Infinite)
        {
            var response = await WaitMessage(timeout);
            if (response.GetAttribute("valid") is null) throw new Exception($"Unexpected server response: {await response.ReadOuterXmlAsync().ConfigureAwait(false)}");
            return response;
        }


        /// <summary>
        /// Waits for valid async commands to complete execution (eg. <command executed="true/false" />)
        /// </summary>
        public async Task<XmlReader> WaitCompletion(int timeout = Timeout.Infinite)
        {
            var response = await WaitMessage(timeout);
            if (response.GetAttribute("executed") is null) throw new Exception($"Unexpected server response: {await response.ReadOuterXmlAsync().ConfigureAwait(false)}");
            return response;
        }

        /// <summary>
        /// Handle interlock notifications sent by the machine. These notifications are sent to all connected clients
        /// when the laser interlock changes status. You can use the GetInterlocksCommand to read the current interlock status
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="line"></param>
        public void HandleInterlockEvent(XmlReader reader, string line)
        {
            if (reader.GetAttribute("name")?.Trim().ToLower() == "interlock")
                OnInterlockNotification?.Invoke(this, new InterlockNotificationEventArgs { MessageText = line });
        }

        /// <summary>
        /// Asynchronously consumes lines from the network stream until it finds a <command ... /> tag
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<XmlReader> WaitMessage(CancellationToken cancellationToken)
        {
            string line = null;
            while (!cancellationToken.IsCancellationRequested)
            {
                // discard whatever is not a response (eg. welcome message)
                line = await _reader.ReadLineAsync().ConfigureAwait(false);
                if (line?.StartsWith("<command") ?? false)
                    break;
            }

            if (string.IsNullOrEmpty(line)) return null;
            line = line.Trim();

            using var stringReader = new StringReader(line);
            var xmlReader = XmlReader.Create(stringReader, new XmlReaderSettings
            {
                Async = true
            });
            await xmlReader.ReadAsync().ConfigureAwait(false);
            HandleInterlockEvent(xmlReader, line);

            return xmlReader;
        }

        /// <summary>
        /// Asynchronously consumes lines from the network stream until it finds a <command ... /> tag
        /// </summary>
        /// <param name="timeout">The number of milliseconds to wait before cancelling the operation</param>
        /// <returns></returns>
        public async Task<XmlReader> WaitMessage(int timeout = Timeout.Infinite)
        {
            if (timeout == Timeout.Infinite)
                return await WaitMessage(CancellationToken.None);
            using var tcs = new CancellationTokenSource();
            tcs.CancelAfter(timeout);
            return await WaitMessage(tcs.Token).ConfigureAwait(false);
        }

        /// <summary>
        /// Disposes the tcp client and the stream reader/writer
        /// </summary>
        public void Dispose()
        {
            if (_disposing || _disposed) return;
            try
            {
                _disposing = true;
                _tcpClient?.Dispose();
                _reader?.Dispose();
                _writer?.Dispose();
            }
            catch
            {
            }
            finally 
            {
                _disposing = false;
                _disposed = true;
            }

        }
    }
}