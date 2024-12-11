using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;
using System.Threading.Tasks;
using IXLA.Sdk.Xp24;
using IXLA.Sdk.Xp24.Protocol.Commands.Interface.Model;
using System.Web.Services;
using System.ComponentModel;
using System.ComponentModel.Composition.Primitives;
using System.Reflection;


namespace IXLA_SDK
{
    /// <summary>
    /// Summary description for WebService1
    /// </summary>
    [WebService(Namespace = "Polaris-IXLA")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]

    //************************************************************************//
    // To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
    //[System.Web.Script.Services.ScriptService]
    //***********************************************************************//
    public class IXLAWebService : System.Web.Services.WebService
    {

        //    // them machine client is written in such a way that prevents to execute commands in parallel
        //    // if you want to handle encoding and marking in parallel the simplest implementation would 
        //    // be to use a second instance (that connects to port 5556) where you send the commands for 
        //    // the encoder (connect2rfid and transmit2rfid)

        [WebMethod]
        public string Insert(string ip, int port)
        {
            try
            {
                return Task.Run(() => InsertAsync(ip, port)).Result;
            }
            catch (Exception e)
            {
                // Log the error
                // Example: LogError(e);
                return $"Error: {e.Message}";
            }
        }


        private async Task<string> InsertAsync(string ip, int port)
        {
            using var client = new MachineClient();

            try
            {
                await client.ConnectAsync(ip, port, CancellationToken.None);
                var machineApi = new MachineApi(client);

                await machineApi.ResetAsync();
                await machineApi.LoadPassportAsync();

                return "Ok";
            }
            catch (Exception e)
            {
                // Log the error
                // Example: LogError(e);
                return $"Error: {e.Message}";
            }
            finally
            {
                await GracefulDisconnectClientAsync(client);
            }
        }

        private async Task GracefulDisconnectClientAsync(MachineClient client)
        {
            try
            {
                Console.WriteLine("Graceful disconnect...");
                await client.GracefulDisconnectAsync();
            }
            catch
            {
                // Log the error
                // Example: LogError(e);
            }
        }




        //  MarkLayout method

        [WebMethod]
        public string MarkLayout(string Ip, int Port, string SerialNumber, string Type, string Country, string Passport, string Name1EN, string DateOfBirth, string Name2AR, string Surname1EN, string Surname1AR, string MatherNameEN, string MatherNameAR, string Sex, string PlaceOfBirth, string PlaceOfBirthArabic, string Nationality, string dateOfIssue, string DateOfExpiry, string Signature, string AuthorityPlaceEN, string MRZ1, string MRZ2, string Photo)
        {
            if (Ip == string.Empty || SerialNumber == string.Empty)
            {
                return "No IP or SerialNumber Provided";
            }

            try
            {
                return Task.Run(() => MarkLayoutAsync(Ip, Port, SerialNumber, Type, Country, Passport, Name1EN, DateOfBirth, Name2AR, Surname1EN, Surname1AR, MatherNameEN, MatherNameAR, Sex, PlaceOfBirth, PlaceOfBirthArabic, Nationality, dateOfIssue, DateOfExpiry, Signature, AuthorityPlaceEN, MRZ1, MRZ2, Photo)).Result;
            }
            catch (Exception e)
            {
                // Log the error
                // Example: LogError(e);
                return $"Error: {e.Message}";
            }
        }

        private async Task<string> MarkLayoutAsync(string Ip, int Port, string SerialNumber, string Type, string Country, string Passport, string Name1EN, string DateOfBirth, string Name2AR, string Surname1EN, string Surname1AR, string MatherNameEN, string MatherNameAR, string Sex, string PlaceOfBirth, string PlaceOfBirthArabic, string Nationality, string dateOfIssue, string DateOfExpiry, string Signature, string AuthorityPlaceEN, string MRZ1, string MRZ2, string Photo)
        {
            using var client = new MachineClient();

            try
            {
                await client.ConnectAsync(Ip, Port, CancellationToken.None);
                var machineApi = new MachineApi(client);

                await machineApi.LoadDocumentAsync("layout", System.IO.File.ReadAllBytes($"\\Users\\Administrator\\Desktop\\IraqLayoutNew\\all Printer\\{SerialNumber}\\{SerialNumber}Layout.sjf")).ConfigureAwait(false);
                await machineApi.LoadDocumentAsync("mli", System.IO.File.ReadAllBytes($"\\Users\\Administrator\\Desktop\\IraqLayoutNew\\all Printer\\{SerialNumber}\\{SerialNumber}Mli.sjf")).ConfigureAwait(false);

                await machineApi.UpdateDocumentAsync(new Entity[]
                {
            new UpdateTextEntity("Type", Type),
            new UpdateTextEntity("Country", Country),
            new UpdateTextEntity("Passport", Passport),
            new UpdateTextEntity("Name1_EN", Name1EN),
            new UpdateTextEntity("Name2_AR", Name2AR),
            new UpdateTextEntity("Surname", Surname1EN),
            new UpdateTextEntity("Surname1_AR", Surname1AR),
            new UpdateTextEntity("Mather Name_EN", MatherNameEN),
            new UpdateTextEntity("Mather Name_AR", MatherNameAR),
            new UpdateTextEntity("Sex", Sex),
            new UpdateTextEntity("Date of Birth", DateOfBirth),
            new UpdateTextEntity("Place of Birth", PlaceOfBirth),
            new UpdateTextEntity("Place of Birth-Arabic", PlaceOfBirthArabic),
            new UpdateTextEntity("Nationality", Nationality),
            new UpdateTextEntity("date of issue", dateOfIssue),
            new UpdateTextEntity("date of expiry", DateOfExpiry),
            new ImageEntity("Signature", Convert.FromBase64String(Signature)),
            new UpdateTextEntity("AuthorityPlace_EN", AuthorityPlaceEN),
            new UpdateTextEntity("MRZ-1", MRZ1),
            new UpdateTextEntity("MRZ-2", MRZ2),
            new UpdateTextEntity("MLI-Text", DateOfBirth),
            new ImageEntity("MLI-Image", Convert.FromBase64String(Photo)),
            new ImageEntity("Photo", Convert.FromBase64String(Photo)),
            new ImageEntity("Clear Window", Convert.FromBase64String(Photo)),
                }).ConfigureAwait(false);

                var autoPosResponse = await machineApi.PerformAutoPosition("IraqAutopos");
                await machineApi.MarkLayoutAsync("layout", offsetX: autoPosResponse.XOffset, offsetY: autoPosResponse.YOffset).ConfigureAwait(false);
                await machineApi.MarkLayoutAsync("mli", offsetX: autoPosResponse.XOffset, offsetY: 0).ConfigureAwait(false);

                return "Ok";
            }
            catch (Exception e)
            {
                // Log the error
                // Example: LogError(e);
                return $"Error: {e.Message}";
            }
            finally
            {
                await GracefulDisconnectClientAsync(client);
            }
        }


        //  Eject method

        [WebMethod]
        public string Eject(string Ip, int Port)
        {
            try
            {
                return Task.Run(() => EjectAsync(Ip, Port)).Result;
            }
            catch (Exception e)
            {
                // Log the error
                // Example: LogError(e);
                return $"Error: {e.Message}";
            }
        }

        private async Task<string> EjectAsync(string Ip, int Port)
        {
            using var client = new MachineClient();

            try
            {
                await client.ConnectAsync(Ip, Port, CancellationToken.None);
                var machineApi = new MachineApi(client);

                // Eject the passport
                await machineApi.EjectAsync().ConfigureAwait(false);

                return "Ok";
            }
            catch (Exception e)
            {
                // Log the error
                // Example: LogError(e);
                return $"Error: {e.Message}";
            }
            finally
            {
                await GracefulDisconnectClientAsync(client);
            }
        }


        [WebMethod]
        public string CheckAutopos(string Ip, int Port)
        {
            try
            {
                return Task.Run(() => CheckAutoposAsync(Ip, Port)).Result;
            }
            catch (Exception e)
            {
                // Log the error
                // Example: LogError(e);
                return $"Error: {e.Message}";
            }
        }

        private async Task<string> CheckAutoposAsync(string Ip, int Port)
        {
            using var client = new MachineClient();

            try
            {
                await client.ConnectAsync(Ip, Port, CancellationToken.None);
                var machineApi = new MachineApi(client);

                // Perform the auto position check
                var autoPosResponse = await machineApi.PerformAutoPosition("IraqAutopos");

                return "Ok";
            }
            catch (Exception e)
            {
                // Log the error
                // Example: LogError(e);
                return $"Error: {e.Message}";
            }
            finally
            {
                await GracefulDisconnectClientAsync(client);
            }
        }

    }
}
