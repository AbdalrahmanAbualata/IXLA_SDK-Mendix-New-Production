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
    // To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
    // [System.Web.Script.Services.ScriptService]
    public class IXLAWebService : System.Web.Services.WebService
    {

        [WebMethod]
        public string Insert(string Ip, int Port)
        {


            // them machine client is written in such a way that prevents to execute commands in parallel
            // if you want to handle encoding and marking in parallel the simplest implementation would 
            // be to use a second instance (that connects to port 5556) where you send the commands for 
            // the encoder (connect2rfid and transmit2rfid)
            var result = Task.Run(async () =>
            {
            var client = new MachineClient();

                
                
              

                try
                {
                    // we don't need the stopping token 
                    await client.ConnectAsync(Ip, Port, CancellationToken.None).ConfigureAwait(false);
                    var machineApi = new MachineApi(client);



                    await machineApi.ResetAsync().ConfigureAwait(false);
            
                // load a new passport
                await machineApi.LoadPassportAsync().ConfigureAwait(false);

                
                    return "Ok";
                }
                catch (Exception e)
                {
                    return e.ToString();
                }
                finally
                {
                    try
                    {
                        // graceful disconnect sends \r\n before disposing the stream
                        // to avoid hanging connections server side
                        Console.WriteLine("Graceful disconnect...");
                        
                        await client.GracefulDisconnectAsync().ConfigureAwait(false);
                    }
                    catch
                    {
                    }
                }
            }).Result;


            return result;
        }

        //  MarkLayout method



        [WebMethod]
        public string MarkLayout(string Ip, int Port,string SerialNumber, string Type, string Country, string Passport, string Name1EN
        , string DateOfBirth, string Name2AR, string Surname1EN, string Surname1AR, string MatherNameEN, string MatherNameAR,
            string Sex, string PlaceOfBirth, string PlaceOfBirthArabic, string Nationality,
            string dateOfIssue, string DateOfExpiry, string Signature, string AuthorityPlaceEN, string MRZ1,
            string MRZ2, string Photo)
        {





            if (Ip == string.Empty || SerialNumber == string.Empty ) 
            {
                return "No IP or SerialNumber Provided";
            }




            var result = Task.Run(async () =>
            {
                var client = new MachineClient();



                try
                {
                    // we don't need the stopping token 
                    await client.ConnectAsync(Ip, Port, CancellationToken.None).ConfigureAwait(false);
                    var machineApi = new MachineApi(client);




                    // Comment the IP check if statements when you use for Prodaction .


                    //if (Ip == "10.9.2.213") // done
                    //{

                    //    //6220907

                    //    SerialNumber = "62209074";



                    //}
                    //else if (Ip == "10.9.2.189")
                    //{
                    //    //62209056

                    //    SerialNumber = "62209056";



                    //}
                    //else if (Ip == "10.9.2.196") // done 
                    //{
                    //    //62001004

                    //    SerialNumber = "62001004";

                    //}
                    //else if (Ip == "10.9.2.215") // done 
                    //{
                    //    //62209079

                    //    SerialNumber = "62209079";
                    //}
                    //else
                    //{

                    //    return "NO PRINTER WITH THIS Serial Number";
                    //}




                    // load a new document


                    //          ****** server ****

                    // when use for production uncommint the below 2 line  .


                    await machineApi.LoadDocumentAsync("layout", System.IO.File.ReadAllBytes("\\Users\\Administrator\\Desktop\\IraqLayoutNew\\all Printer\\" + SerialNumber + "\\" + SerialNumber + "Layout.sjf")).ConfigureAwait(false);
                    await machineApi.LoadDocumentAsync("mli", System.IO.File.ReadAllBytes("\\Users\\Administrator\\Desktop\\IraqLayoutNew\\all Printer\\" + SerialNumber + "\\" + SerialNumber + "Mli.sjf")).ConfigureAwait(false);


                    //           **** local ******

                    // when use for test in local device  uncommnt the below 2 line

                    //await machineApi.LoadDocumentAsync("layout", System.IO.File.ReadAllBytes("\\Users\\admin\\Desktop\\IraqLayoutNew\\all Printer\\" + SerialNumber + "\\" + SerialNumber + "Layout.sjf")).ConfigureAwait(false);
                    //await machineApi.LoadDocumentAsync("mli", System.IO.File.ReadAllBytes("\\Users\\admin\\Desktop\\IraqLayoutNew\\all Printer\\" + SerialNumber + "\\" + SerialNumber + "Mli.sjf")).ConfigureAwait(false);





                    //// example for connecting / reading UID of a smart card        
                    //// await machineApi.Connect2RfId();
                    //// var transmitResponse = await machineApi.Transmit2RfId(0xff, 0xca, 0x00, 0x00, 0x00);
                    //// Console.WriteLine($"chip reply: {BitConverter.ToString(transmitResponse.ChipReply)}");

                    //string staticPoto = "";

                    //// update some entities
                    ///
                    //// if you comment the load document you should comment update document too during tests. 

                    await machineApi.UpdateDocumentAsync(new Entity[]
                    {


                     //******
                          new UpdateTextEntity("Type", Type),
                          new UpdateTextEntity("Country", Country),
                          new UpdateTextEntity("Passport", Passport),
                          new UpdateTextEntity("Name1_EN", Name1EN),
                          new UpdateTextEntity("Name2_AR", Name2AR),
                          new UpdateTextEntity("Surname", Surname1EN),
                          new UpdateTextEntity("Surname1_AR", Surname1AR),
                          new UpdateTextEntity("Mather Name_EN", MatherNameEN),
                          new UpdateTextEntity("Mather Name_AR",MatherNameAR),
                          new UpdateTextEntity("Sex", Sex),

                          new UpdateTextEntity("Date of Birth", DateOfBirth),
                          new UpdateTextEntity("Place of Birth", PlaceOfBirth),
                          new UpdateTextEntity("Place of Birth-Arabic",PlaceOfBirthArabic),
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



                    // If you configured XY AutoPositioning patterns (using the web interface) you can use the "autoposition" 
                    // command to select one of the configured patterns and obtain the offset by which entities need to be translated
                    // to match the position of a preprinted marker in the passport 

                    var autoPosResponse = await machineApi.PerformAutoPosition("IraqAutopos");

                    //// mark layout
                    //// the offsetX and offsetY parameters can be retrived using the "autoposition" command. 
                    //// I left them commented here because i didn't configure autoposition in the machine that I'm using for testing 


                    //this one should be commented too if you're not loading the any document

                    //marking the layout for the passport without the MLI.

                    await machineApi.MarkLayoutAsync("layout"
                    , offsetX: autoPosResponse.XOffset,
                    offsetY: autoPosResponse.YOffset
                    ).ConfigureAwait(false);


                    //marking the MLI for the passport with only xOffset

                    await machineApi.MarkLayoutAsync("mli"
                   , offsetX: autoPosResponse.XOffset, 0
                   ).ConfigureAwait(false);








                    return "Ok";
                }
                catch (Exception e)
                {
                    return e.ToString();
                }
                finally
                {
                    try
                    {
                        // graceful disconnect sends \r\n before disposing the stream
                        // to avoid hanging connections server side
                        Console.WriteLine("Graceful disconnect...");

                        await client.GracefulDisconnectAsync().ConfigureAwait(false);
                    }
                    catch
                    {
                    }
                }
            }).Result;


            return result;
        }



        //  Eject method


        [WebMethod]
        public string Eject(string Ip, int Port)
        {

            var result = Task.Run(async () =>
            {
                var client = new MachineClient();



                try
                {
                    // we don't need the stopping token 
                    await client.ConnectAsync(Ip, Port, CancellationToken.None).ConfigureAwait(false);
                    var machineApi = new MachineApi(client);


                    // eject the passport
                    await machineApi.EjectAsync().ConfigureAwait(false);

                    return "Ok";
                }
                catch (Exception e)
                {
                    return e.ToString();
                }
                finally
                {
                    try
                    {
                        // graceful disconnect sends \r\n before disposing the stream
                        // to avoid hanging connections server side
                        Console.WriteLine("Graceful disconnect...");

                        await client.GracefulDisconnectAsync().ConfigureAwait(false);
                    }
                    catch
                    {
                    }
                }
            }).Result;


            return result;
        }


    }
}
