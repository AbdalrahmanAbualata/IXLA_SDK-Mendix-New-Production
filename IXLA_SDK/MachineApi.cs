﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using IXLA.Sdk.Xp24.Protocol.Commands;
using IXLA.Sdk.Xp24.Protocol.Commands.Encoder;
using IXLA.Sdk.Xp24.Protocol.Commands.Interface;
using IXLA.Sdk.Xp24.Protocol.Commands.Interface.Model;
using IXLA.Sdk.Xp24.Protocol.Commands.Transport;
using IXLA.Sdk.Xp24.Protocol.Commands.Vision;
using NLog;

namespace IXLA.Sdk.Xp24
{
    /// <summary>
    /// This is a Facade for the machines API surface. It uses the Machine client to handle the
    /// communication. I didn't implement the entire machine API, feel free to extend/modify
    /// </summary>
    public class MachineApi : IDisposable
    {
        private readonly MachineClient _client;
        private static readonly ILogger Logger = LogManager.GetCurrentClassLogger(); // NLog logger initialization

        /// <summary>
        /// </summary>
        /// <param name="client">A machine client. Make sure you call ConnectAsync on the machine client instance before trying to use the methods of this class</param>
        public MachineApi(MachineClient client)
        {
            _client = client;
        }

        /// <summary>
        /// Resets the machine and waits for the response
        /// </summary>
        /// <returns>A ResetResponse object</returns>
        public async Task<ResetResponse> ResetAsync() => await _client.SendCommandAsync<ResetResponse>(new ResetCommand()).ConfigureAwait(false);


        /// <summary>
        /// ResetInterface in machine and waits for the response
        /// </summary>
        /// <returns>A ResetInterfaceResponse object</returns>
        public async Task<ResetInterfaceResponse> ResetInterfaceASync() => await _client.SendCommandAsync<ResetInterfaceResponse>(new ResetInterfaceCommand()).ConfigureAwait(false);


        /// <summary>
        /// Moves a passport from the feeder to the laser position
        /// </summary>
        /// <returns></returns>
        public async Task<LoadPassportResponse> LoadPassportAsync() => await _client.SendCommandAsync<LoadPassportResponse>(new LoadPassportCommand()).ConfigureAwait(false);

        /// <summary>
        /// Ejects a passport from the laser position
        /// </summary>
        /// <returns></returns>
        public async Task<EjectResponse> EjectAsync() => await _client.SendCommandAsync<EjectResponse>(new EjectCommand()).ConfigureAwait(false);

        /// <summary>
        /// Retrieves the list of currently loaded layouts in SAMLight
        /// </summary>
        /// <returns></returns>
        public async Task<GetLayoutsResponse> GetLayoutsAsync() => await _client.SendCommandAsync<GetLayoutsResponse>(new GetLayoutsCommand()).ConfigureAwait(false);

        /// <summary>
        /// Loads a new document in samlight
        /// </summary>
        /// <param name="layoutName">The name that will be used to reference the loaded sjf file in other calls (eg. MarkLayout)</param>
        /// <param name="data"></param>
        /// <param name="rotation"></param>
        /// <returns></returns>
        public async Task<LoadDocumentResponse> LoadDocumentAsync(string layoutName, byte[] data, int rotation = default) =>
            await _client.SendCommandAsync<LoadDocumentResponse>(new LoadDocumentCommand(layoutName, data, rotation)).ConfigureAwait(false);

        /// <summary>
        /// Updates data in the current layout
        /// </summary>
        /// <param name="entities"></param>
        /// <returns></returns>
        public async Task<UpdateDocumentResponse> UpdateDocumentAsync(IEnumerable<Entity> entities) => await _client.SendCommandAsync<UpdateDocumentResponse>(new UpdateDocumentCommand(entities)).ConfigureAwait(false);

        /// <summary>
        /// Tries to connect to a smart card using the contactless reader using the PCSC interface
        /// </summary>
        /// <returns>A Connect2RfIdRespnose object. The response object contains </returns>
        public async Task<Connect2RfIdResponse> Connect2RfId() => await _client.SendCommandAsync<Connect2RfIdResponse>(new Connect2RfIdCommand()).ConfigureAwait(false);

        /// <summary>
        /// Tries to send an APDU to the card
        /// </summary>
        /// <param name="apdu"></param>
        /// <returns></returns>
        public async Task<Transmit2RfIdResponse> Transmit2RfId(string apdu) => await _client.SendCommandAsync<Transmit2RfIdResponse>(new Transmit2RfIdCommand(apdu)).ConfigureAwait(false);

        /// <summary>
        /// Marks the specified layout
        /// </summary>
        /// <param name="layout">The name of the layout we want to mark. Layouts name are given when </param>
        /// <param name="offsetX"></param>
        /// <param name="offsetY"></param>
        /// <param name="angle"></param>
        /// <returns></returns>
        public async Task<MarkLayoutResponse> MarkLayoutAsync(string layout, double offsetX = 0.0d, double offsetY = 0.0d, double angle = 0.0d) => await _client.SendCommandAsync<MarkLayoutResponse>(new MarkLayoutCommand
        {
            Layout = layout,
            XOffset = offsetX,
            YOffset = offsetY,
            Angle = angle
        }).ConfigureAwait(false);

        /// <summary>
        /// Get the list of XY AutoPosition patterns present in the system. AutoPosition patterns
        /// can be created using the web interface
        /// </summary>
        /// <returns></returns>
        public async Task<GetPatternsResponse> GetPatterns() => await _client.SendCommandAsync<GetPatternsResponse>(new GetPatternsCommand()).ConfigureAwait(false);

        /// <summary>
        /// Executes XY AutoPosition
        /// </summary>
        /// <param name="patternName">The pattern we want to use as a reference</param>
        /// <returns>
        /// If the pattern matches, returns the offsets by which we should translate entities
        /// before marking
        /// </returns>
        public async Task<PerformXyAutoPositionResponse> PerformAutoPosition(string patternName) => await _client.SendCommandAsync<PerformXyAutoPositionResponse>(new PerformXyAutoPositionCommand
        {
            PatternName = patternName
        }).ConfigureAwait(false);

        /// <summary>
        /// Disposes the underlying MachineClient
        /// </summary>
        public void Dispose()
        {
            _client?.Dispose();
        }
    }
}