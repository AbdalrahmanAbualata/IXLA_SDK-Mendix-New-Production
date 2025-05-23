﻿using System;
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
using NLog;
using System.Web.Services.Description;
using System.Net.Http;
using NLog.LayoutRenderers;




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


        // NLog logger initialization
        private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

        //    // them machine client is written in such a way that prevents to execute commands in parallel
        //    // if you want to handle encoding and marking in parallel the simplest implementation would 
        //    // be to use a second instance (that connects to port 5556) where you send the commands for 
        //    // the encoder (connect2rfid and transmit2rfid)

        [WebMethod]
        public string Insert(string Ip, int Port, bool PerformAutoPosition = false)
        {

            Logger.Info("***********************************************************************************************************************************************************************************");
            try
            {
                Logger.Info($" {Ip}:{Port} --> Insert method called.");
                return Task.Run(() => InsertAsync(Ip, Port, PerformAutoPosition)).Result;
            }
            catch (Exception e)
            {
                Logger.Error($"{Ip}:{Port} --> Error occurred in Insert method.");
                Logger.Error($"{Ip}:{Port} --> {e}");
                return $"{e}";
            }
        }


        private async Task<string> InsertAsync(string Ip, int Port, bool PerformAutoPosition)
        {
            using var client = new MachineClient();
            var machineApi = new MachineApi(client);
            try
            {
                Logger.Info($" {Ip}:{Port} --> Connecting to IXLA Printer at Insert method.");
                await client.ConnectWithRetryAsync(Ip, Port).ConfigureAwait(false);


                Logger.Info($" {Ip}:{Port} --> Performing ResetMachine operations in Ixla printer....");
                await machineApi.ResetAsync().ConfigureAwait(false);

                Logger.Info($" {Ip}:{Port} --> Reset machine operation completed successfully....");

                Logger.Info($" {Ip}:{Port} --> Performing LoadPassport operations in Ixla printer....");
                await machineApi.LoadPassportAsync().ConfigureAwait(false);

                Logger.Info($" {Ip}:{Port} --> Insert operation completed successfully in Ixla printer.");

                // AutoPosition

                if (PerformAutoPosition)
                {
                    Logger.Info($" {Ip}:{Port} --> Performing CheckAutopos operations...");
                    var autoPosResponseInitial = await machineApi.PerformAutoPosition("IraqAutopos").ConfigureAwait(false);

                    Logger.Info($" {Ip}:{Port} --> CheckAutopos operation completed successfully in Ixla printer, passport is in Ingraving position.");
                }

                //***************

                Logger.Info($" {Ip}:{Port} --> Sending OK response.");
                return "Ok";
            }
            catch (Exception e)
            {
                try
                {
                    Logger.Error($" {Ip}:{Port} --> Issue in Inserting or CheckAutopos Performing Eject operations...");
                    await machineApi.EjectAsync().ConfigureAwait(false);
                    Logger.Info($" {Ip}:{Port} --> Eject operation completed successfully in Ixla printer.");
                } catch (Exception) { Logger.Error($"{Ip}:{Port} --> Error occurred in EjectAsync method."); }


                Logger.Error($"{Ip}:{Port} --> {e}");
                return $"{Ip}:{Port} --> {e}";
            }
            finally
            {
                await GracefulDisconnectClientAsync(client, Ip, Port);
            }
        }



        //  MarkLayout method

        [WebMethod]
        public string MarkLayout(string Ip, int Port, string SerialNumber, string Type, string Country, string Passport, string Name1EN, string DateOfBirth, string Name2AR, string Surname1EN, string Surname1AR, string MatherNameEN, string MatherNameAR, string Sex, string PlaceOfBirth, string PlaceOfBirthArabic, string Nationality, string dateOfIssue, string DateOfExpiry, string Signature, string AuthorityPlaceEN, string MRZ1, string MRZ2, string Photo, bool PerformEject = false)
        {
            Logger.Info("***********************************************************************************************************************************************************************************");
            if (Ip == string.Empty || SerialNumber == string.Empty)
            {
                Logger.Error($"{Ip}:{Port} --> Error occurred in MarkLayout method");
                return "No IP or SerialNumber Provided";
            }
            try
            {
                Logger.Info($" {Ip}:{Port} --> MarkLayout method called with SerialNumber: {SerialNumber}");
                return Task.Run(() => MarkLayoutAsync(Ip, Port, SerialNumber, Type, Country, Passport, Name1EN, DateOfBirth, Name2AR, Surname1EN, Surname1AR, MatherNameEN, MatherNameAR, Sex, PlaceOfBirth, PlaceOfBirthArabic, Nationality, dateOfIssue, DateOfExpiry, Signature, AuthorityPlaceEN, MRZ1, MRZ2, Photo, PerformEject)).Result;
            }
            catch (Exception e)
            {
                Logger.Error($"{Ip}:{Port} --> Error occurred in MarkLayout method.");
                Logger.Error($"{Ip}:{Port} --> {e}");
                return $"{Ip}:{Port} --> {e}";
            }
        }

        private async Task<string> MarkLayoutAsync(string Ip, int Port, string SerialNumber, string Type, string Country, string Passport, string Name1EN, string DateOfBirth, string Name2AR, string Surname1EN, string Surname1AR, string MatherNameEN, string MatherNameAR, string Sex, string PlaceOfBirth, string PlaceOfBirthArabic, string Nationality, string dateOfIssue, string DateOfExpiry, string Signature, string AuthorityPlaceEN, string MRZ1, string MRZ2, string Photo, bool PerformEject)
        {
            using var client = new MachineClient();
            var machineApi = new MachineApi(client);
            try
            {
                Logger.Info($" {Ip}:{Port} --> Connecting to IXLA Printer {SerialNumber} at MarkLayout method.");
                await client.ConnectWithRetryAsync(Ip, Port).ConfigureAwait(false);

                Logger.Info($" {Ip}:{Port} --> Loading document {SerialNumber}Layout.sjf to SamLight for Ixla Printer with SerialNumber: {SerialNumber}");
                await machineApi.LoadDocumentAsync("layout", System.IO.File.ReadAllBytes($"\\Users\\Administrator\\Desktop\\IraqLayoutNew\\all Printer\\{SerialNumber}\\{SerialNumber}Layout.sjf")).ConfigureAwait(false);

                Logger.Info($" {Ip}:{Port} --> Load document {SerialNumber}Layout.sjf to SamLight for Ixla Printer with SerialNumber: {SerialNumber} completed successfully.");

                Logger.Info($" {Ip}:{Port} --> Loading document {SerialNumber}Mli.sjf to SamLight for Ixla Printer with SerialNumber: {SerialNumber}");
                await machineApi.LoadDocumentAsync("mli", System.IO.File.ReadAllBytes($"\\Users\\Administrator\\Desktop\\IraqLayoutNew\\all Printer\\{SerialNumber}\\{SerialNumber}Mli.sjf")).ConfigureAwait(false);

                Logger.Info($" {Ip}:{Port} --> Load document {SerialNumber}Mli.sjf to SamLight for Ixla Printer with SerialNumber: {SerialNumber} completed successfully.");

                Logger.Info($" {Ip}:{Port} --> Updating layout and Mli to SamLight for Ixla Printer with SerialNumber: {SerialNumber} with provided Data...");
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

                Logger.Info($" {Ip}:{Port} --> Update layout and Mli to SamLight in Ixla Printer with SerialNumber: {SerialNumber} with provided Data completed successfully.");

                Logger.Info($" {Ip}:{Port} --> Performing auto-position operation in Ixla Printer with SerialNumber: {SerialNumber}...");
                var autoPosResponse = await machineApi.PerformAutoPosition("IraqAutopos").ConfigureAwait(false);

                Logger.Info($" {Ip}:{Port} --> Auto-position operation in Ixla Printer with SerialNumber: {SerialNumber} completed successfully.");
                Logger.Info($" {Ip}:{Port} --> Auto-position values : Xoffset=({autoPosResponse.XOffset}) , Yoffset=({autoPosResponse.YOffset}) , Correlation=({autoPosResponse.Correlation}%");


                Logger.Info($" {Ip}:{Port} --> Start marking layout.sjf with Xoffset=({autoPosResponse.XOffset}) & Yoffset=({autoPosResponse.YOffset}) in Ixla Printer SerialNumber: {SerialNumber}...");
                await machineApi.MarkLayoutAsync("layout", offsetX: autoPosResponse.XOffset, offsetY: autoPosResponse.YOffset).ConfigureAwait(false);

                Logger.Info($" {Ip}:{Port} --> MarkLayout operation for layout completed successfully in Ixla Printer with SerialNumber: {SerialNumber}.");

                Logger.Info($" {Ip}:{Port} --> Start marking Mli.sjf with Yoffset=({autoPosResponse.XOffset}) in Ixla Printer SerialNumber: {SerialNumber}...");
                await machineApi.MarkLayoutAsync("mli", offsetX: autoPosResponse.XOffset, offsetY: 0).ConfigureAwait(false);

                Logger.Info($" {Ip}:{Port} --> MarkLayout operation for Mli completed successfully in Ixla Printer with SerialNumber: {SerialNumber}.");


                // Perform Eject if needed
                if (PerformEject)
                {
                    Logger.Info($" {Ip}:{Port} --> Performing Eject operations...");
                    await machineApi.EjectAsync().ConfigureAwait(false);

                    Logger.Info($" {Ip}:{Port} --> Eject operation completed successfully in Ixla printer.");
                }
                //*********************

                Logger.Info($" {Ip}:{Port} --> Sending OK response.");
                return "Ok";
            }
            catch (Exception e)
            {
                try
                {
                    Logger.Error($" {Ip}:{Port} --> Issue in MarkLayoutAsync Performing Eject operations...");
                    await machineApi.EjectAsync().ConfigureAwait(false);
                    Logger.Info($" {Ip}:{Port} --> Eject operation completed successfully in Ixla printer.");
                } catch (Exception) { Logger.Error($"{Ip}:{Port} --> Error occurred in EjectAsync method."); }


                Logger.Error($"{Ip}:{Port} --> Error occurred in MarkLayoutAsync method while marking Booklet in Ixla Printer with SerialNumber: {SerialNumber} .");
                Logger.Error($"{Ip}:{Port} --> {e}");
                return $"{Ip}:{Port} --> {e}";
            }
            finally
            {
                await GracefulDisconnectClientAsync(client, Ip, Port);
            }
        }


        //  Eject method

        [WebMethod]
        public string Eject(string Ip, int Port)
        {
            Logger.Info("***********************************************************************************************************************************************************************************");
            try
            {
                Logger.Info($" {Ip}:{Port} --> Eject method called.");
                return Task.Run(() => EjectAsync(Ip, Port)).Result;
            }
            catch (Exception e)
            {
                Logger.Error($"{Ip}:{Port} --> Error occurred in Eject method.");
                Logger.Error($"{Ip}:{Port} --> {e}");
                return $"{Ip}:{Port} --> {e}";
            }
        }

        private async Task<string> EjectAsync(string Ip, int Port)
        {
            using var client = new MachineClient();
            var machineApi = new MachineApi(client);
            try
            {
                Logger.Info($" {Ip}:{Port} --> Connecting to machine at EjectAsync method.");
                await client.ConnectWithRetryAsync(Ip, Port).ConfigureAwait(false);

                Logger.Info($" {Ip}:{Port} --> Performing Eject operations...");
                await machineApi.EjectAsync().ConfigureAwait(false);

                Logger.Info($" {Ip}:{Port} --> Eject operation completed successfully in Ixla printer.");

                Logger.Info($" {Ip}:{Port} --> Sending OK response.");
                return "Ok";
            }
            catch (Exception e)
            {
                Logger.Error($"{Ip}:{Port} --> Error occurred in EjectAsync method.");
                Logger.Error($"{Ip}:{Port} --> {e}");
                return $"{Ip}:{Port} --> {e}";
            }
            finally
            {
                await GracefulDisconnectClientAsync(client, Ip, Port);
            }
        }


        [WebMethod]
        public string CheckAutopos(string Ip, int Port)
        {
            Logger.Info("***********************************************************************************************************************************************************************************");
            try
            {
                Logger.Info($" {Ip}:{Port} --> CheckAutopos method called.");
                return Task.Run(() => CheckAutoposAsync(Ip, Port)).Result;
            }
            catch (Exception e)
            {
                Logger.Error($"{Ip}:{Port} --> Error occurred in CheckAutopos method.");
                Logger.Error($"{Ip}:{Port} --> {e}");
                return $"{Ip}:{Port} --> {e}";
            }
        }

        private async Task<string> CheckAutoposAsync(string Ip, int Port)
        {
            using var client = new MachineClient();
            var machineApi = new MachineApi(client);

            try
            {
                Logger.Info($" {Ip}:{Port} --> Connecting to machine at CheckAutoposAsync method....");
                await client.ConnectWithRetryAsync(Ip, Port).ConfigureAwait(false);


                Logger.Info($" {Ip}:{Port} --> Performing CheckAutopos operations....");
                var autoPosResponse = await machineApi.PerformAutoPosition("IraqAutopos").ConfigureAwait(false);

                Logger.Info($" {Ip}:{Port} --> CheckAutopos operation completed successfully in Ixla printer.");
                Logger.Info($" {Ip}:{Port} --> Auto-position values : Xoffset=({autoPosResponse.XOffset}) , Yoffset=({autoPosResponse.YOffset}) , Correlation=({autoPosResponse.Correlation}%");

                Logger.Info($" {Ip}:{Port} --> Sending OK response.");
                return "Ok";
            }
            catch (Exception e)
            {
                try
                {
                    Logger.Error($" {Ip}:{Port} --> Issue in CheckAutoposAsync Performing Eject operations...");
                    await machineApi.EjectAsync().ConfigureAwait(false);
                    Logger.Info($" {Ip}:{Port} --> Eject operation completed successfully in Ixla printer.");
                } catch (Exception) { Logger.Error($"{Ip}:{Port} --> Error occurred in EjectAsync method."); }

                Logger.Error($"{Ip}:{Port} --> {e}");
                return $"{Ip}:{Port} --> {e}";
            }
            finally
            {
                await GracefulDisconnectClientAsync(client, Ip, Port);
            }
        }



        // need to retry disconnect and return spacific error 
        private async Task GracefulDisconnectClientAsync(MachineClient client, string Ip, int Port)
        {
            try
            {
                Logger.Info($" {Ip}:{Port} --> Gracefully disconnecting client...");
                await client.GracefulDisconnectAsync();

                Logger.Info($" {Ip}:{Port} --> Gracefully disconnecting client completed...");
            }
            catch
            {
                Logger.Error($"{Ip}:{Port} --> Error during graceful disconnect");
            }
        }


        //*******************************************************************************************************************************************************************************************************

        [WebMethod]
        public string SupplementaryPrintting(string Ip, int Port, string SerialNumber, string Type, string Country, string Passport, string Name1EN, string DateOfBirth, string Name2AR, string Surname1EN, string Surname1AR, string MatherNameEN, string MatherNameAR, string Sex, string PlaceOfBirth, string PlaceOfBirthArabic, string Nationality, string dateOfIssue, string DateOfExpiry, string Signature, string AuthorityPlaceEN, string MRZ1, string MRZ2, string Photo, string CaseID = "1234")
        {

            Logger.Info("***********************************************************************************************************************************************************************************");
            if (Ip == string.Empty || SerialNumber == string.Empty)
            {
                Logger.Error($"{Ip}:{Port} --> Error occurred in SupplementaryPrintting method");
                return "No IP or SerialNumber Provided";
            }
            try
            {
                Logger.Info($" {Ip}:{Port} --> SupplementaryPrintting method called.");
                return Task.Run(() => SupplementaryPrinttingAsync(Ip, Port, SerialNumber, Type, Country, Passport, Name1EN, DateOfBirth, Name2AR, Surname1EN, Surname1AR, MatherNameEN, MatherNameAR, Sex, PlaceOfBirth, PlaceOfBirthArabic, Nationality, dateOfIssue, DateOfExpiry, Signature, AuthorityPlaceEN, MRZ1, MRZ2, Photo, CaseID)).Result;
            }
            catch (Exception e)
            {
                Logger.Error($"{Ip}:{Port} --> Error occurred in SupplementaryPrintting method.");
                Logger.Error($"{Ip}:{Port} --> {e}");
                return $"{e}";
            }
        }


        private async Task<string> SupplementaryPrinttingAsync(string Ip, int Port, string SerialNumber, string Type, string Country, string Passport, string Name1EN, string DateOfBirth, string Name2AR, string Surname1EN, string Surname1AR, string MatherNameEN, string MatherNameAR, string Sex, string PlaceOfBirth, string PlaceOfBirthArabic, string Nationality, string dateOfIssue, string DateOfExpiry, string Signature, string AuthorityPlaceEN, string MRZ1, string MRZ2, string Photo, string CaseID)
        {
            using var client = new MachineClient();
            var machineApi = new MachineApi(client);
            try
            {
                Logger.Info($" {Ip}:{Port} --> Connecting to IXLA Printer {SerialNumber} at SupplementaryPrintting method.");
                await client.ConnectWithRetryAsync(Ip, Port).ConfigureAwait(false); ;

                // CheckAutopos ******

                var IsPassportInInGravingPosition = true;

                try
                {

                    Logger.Info($" {Ip}:{Port} --> Performing CheckAutopos operations...");
                    var autoPosResponseInitial = await machineApi.PerformAutoPosition("IraqAutopos").ConfigureAwait(false);

                    Logger.Info($" {Ip}:{Port} --> CheckAutopos operation completed successfully in Ixla printer, passport is in Ingraving position.");

                }
                catch (Exception)
                {
                    Logger.Warn($" {Ip}:{Port} --> CheckAutopos operation not completed  in Ixla printer, no passport in Ingraving position.");
                    IsPassportInInGravingPosition = false;
                }

                // insert ************

                if (IsPassportInInGravingPosition)
                {
                    Logger.Info($" {Ip}:{Port} --> Performing ResetIterfaceAsync operations to clean Samlight in printer {SerialNumber}...");
                    await machineApi.ResetInterfaceASync().ConfigureAwait(false); ;
                }

                if (!IsPassportInInGravingPosition)
                {
                    Logger.Info($" {Ip}:{Port} --> Performing ResetMachine operations in Ixla printer....");
                    await machineApi.ResetAsync().ConfigureAwait(false); ;

                    Logger.Info($" {Ip}:{Port} --> Reset machine operation completed successfully....");


                    Logger.Info($" {Ip}:{Port} --> Performing LoadPassport operations in Ixla printer....");
                    await machineApi.LoadPassportAsync().ConfigureAwait(false); ;

                    Logger.Info($" {Ip}:{Port} --> ResetMachine operations completed successfully....");
                    Logger.Info($" {Ip}:{Port} --> Insert operation completed successfully in Ixla printer.");
                }


                // check UID *****

                Logger.Info($" {Ip}:{Port} --> Performing Connect2RfId operations to check Chip Validity...");
                await machineApi.Connect2RfId().ConfigureAwait(false); // add to try and catch for eject if there is no chip or coonection with the encoder 

                Logger.Info($" {Ip}:{Port} --> Connect2RfId completed successfully.");

                Logger.Info($" {Ip}:{Port} --> Sending Apdu to Chip to get UID....");
                var transmitResponse = await machineApi.Transmit2RfId("FFCA000000").ConfigureAwait(false); ;

                Logger.Info($" {Ip}:{Port} --> Response recived from Chip, ChipReply = '{transmitResponse.ChipReply}'");
                var chipReply = transmitResponse.ChipReply;

                Logger.Info($" {Ip}:{Port} --> Validating the chipReply....");
                if (string.IsNullOrEmpty(chipReply) || chipReply.Length < 1)
                {
                    Logger.Error($"{Ip}:{Port} --> chipReply is empty or null.");
                    Logger.Warn($" {Ip}:{Port} --> Sending Error message....");
                    return "ERROR: chipReply is empty or null.";
                }

                if (!chipReply.EndsWith("9000"))
                {
                    Logger.Error($" {Ip}:{Port} --> ChipReply Not Success(9000), can not extract UID.");
                    Logger.Warn($" {Ip}:{Port} --> Sending Error message....");
                    return "ERROR: ChipReply Not Success(9000), can not extract UID.";
                }
                Logger.Info($" {Ip}:{Port} --> ChipReply Success(9000), can extract UID");

                Logger.Info($" {Ip}:{Port} --> extracting UID from ChipReply....");
                var uid = chipReply.Substring(0, chipReply.Length - 4);

                Logger.Info($" {Ip}:{Port} --> UID extracted successfully UID = '{uid}'.");

                Logger.Info($" {Ip}:{Port} --> Validating the UID....");

                if (uid.Length == 14 || uid.StartsWith("04"))
                {
                    Logger.Error($"{Ip}:{Port} --> UID is valid, can not proceed in Supplementary Printting.");

                    // eject passport
                    Logger.Info($" {Ip}:{Port} --> Performing Eject operations...");
                    await machineApi.EjectAsync().ConfigureAwait(false);

                    Logger.Info($" {Ip}:{Port} --> Eject operation completed successfully in Ixla printer.");

                    Logger.Warn($" {Ip}:{Port} --> Sending Error message....");
                    return $"ERROR: Valid UID {uid}, can not proceed in Supplementary Printting.";
                }

                Logger.Info($" {Ip}:{Port} --> Invalid UID {uid}, Proceeding in Supplementary Printting.");


                //markLayout *********

                Logger.Info($" {Ip}:{Port} --> Loading document {SerialNumber}Layout.sjf to SamLight for Ixla Printer with SerialNumber: {SerialNumber}");
                await machineApi.LoadDocumentAsync("layout", System.IO.File.ReadAllBytes($"\\Users\\Administrator\\Desktop\\IraqLayoutNew\\all Printer\\{SerialNumber}\\{SerialNumber}Layout.sjf")).ConfigureAwait(false);

                Logger.Info($" {Ip}:{Port} --> Load document {SerialNumber}Layout.sjf to SamLight for Ixla Printer with SerialNumber: {SerialNumber} completed successfully.");

                Logger.Info($" {Ip}:{Port} --> Loading document {SerialNumber}Mli.sjf to SamLight for Ixla Printer with SerialNumber: {SerialNumber}");
                await machineApi.LoadDocumentAsync("mli", System.IO.File.ReadAllBytes($"\\Users\\Administrator\\Desktop\\IraqLayoutNew\\all Printer\\{SerialNumber}\\{SerialNumber}Mli.sjf")).ConfigureAwait(false);

                Logger.Info($" {Ip}:{Port} --> Load document {SerialNumber}Mli.sjf to SamLight for Ixla Printer with SerialNumber: {SerialNumber} completed successfully.");

                Logger.Info($" {Ip}:{Port} --> Updating layout and Mli to SamLight for Ixla Printer with SerialNumber: {SerialNumber} with provided Data...");
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

                Logger.Info($" {Ip}:{Port} --> Update layout and Mli to SamLight in Ixla Printer with SerialNumber: {SerialNumber} with provided Data completed successfully.");

                Logger.Info($" {Ip}:{Port} --> Performing auto-position operation in Ixla Printer with SerialNumber: {SerialNumber}...");
                var autoPosResponse = await machineApi.PerformAutoPosition("IraqAutopos").ConfigureAwait(false); ;

                Logger.Info($" {Ip}:{Port} --> Auto-position operation in Ixla Printer with SerialNumber: {SerialNumber} completed successfully.");
                Logger.Info($" {Ip}:{Port} --> Auto-position values : Xoffset=({autoPosResponse.XOffset}) , Yoffset=({autoPosResponse.YOffset}) , Correlation=({autoPosResponse.Correlation}%");


                Logger.Info($" {Ip}:{Port} --> Start marking layout.sjf with Xoffset=({autoPosResponse.XOffset}) & Yoffset=({autoPosResponse.YOffset}) in Ixla Printer SerialNumber: {SerialNumber}...");
                await machineApi.MarkLayoutAsync("layout", offsetX: autoPosResponse.XOffset, offsetY: autoPosResponse.YOffset).ConfigureAwait(false);

                Logger.Info($" {Ip}:{Port} --> MarkLayout operation for layout completed successfully in Ixla Printer with SerialNumber: {SerialNumber}.");

                Logger.Info($" {Ip}:{Port} --> Start marking Mli.sjf with Yoffset=({autoPosResponse.XOffset}) in Ixla Printer SerialNumber: {SerialNumber}...");
                await machineApi.MarkLayoutAsync("mli", offsetX: autoPosResponse.XOffset, offsetY: 0).ConfigureAwait(false);

                Logger.Info($" {Ip}:{Port} --> MarkLayout operation for Mli completed successfully in Ixla Printer with SerialNumber: {SerialNumber}.");

                // Eject ************

                Logger.Info($" {Ip}:{Port} --> Performing Eject operations...");
                await machineApi.EjectAsync().ConfigureAwait(false);

                Logger.Info($" {Ip}:{Port} --> Eject operation completed successfully in Ixla printer.");


                Logger.Info($" {Ip}:{Port} --> Sending OK response.");
                return "Ok";
            }
            catch (Exception e)
            {
                try
                {
                    Logger.Error($" {Ip}:{Port} --> Issue in SupplementaryPrinttingAsync Performing Eject operations...");
                    await machineApi.EjectAsync().ConfigureAwait(false);
                    Logger.Info($" {Ip}:{Port} --> Eject operation completed successfully in Ixla printer.");
                } catch (Exception) { Logger.Error($"{Ip}:{Port} --> Error occurred in EjectAsync method."); }

                Logger.Error($"{Ip}:{Port} --> Error occurred in SupplementaryPrinttingAsync method while marking Booklet in Ixla Printer with SerialNumber: {SerialNumber} .");
                Logger.Error($"{Ip}:{Port} --> {e}");
                return $"{Ip}:{Port} --> {e}";
            }
            finally
            {
                await GracefulDisconnectClientAsync(client, Ip, Port);
            }
        }
    }
}


