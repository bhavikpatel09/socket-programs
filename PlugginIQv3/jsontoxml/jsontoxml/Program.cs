using log4net;
using Newtonsoft.Json;
using PluginIQ;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using Topshelf;

namespace pluginiq
{
    class jsontoxml
    {
        //private static ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private static readonly ILog Log =
              LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        //config file ipaddress and port number
        static string listenerHostName = ConfigurationManager.AppSettings["plugin.iq.listener.host"];
        static string listenerPort = ConfigurationManager.AppSettings["plugin.iq.listener.port"];
        static string pluginIQTargetHostName = ConfigurationManager.AppSettings["plugin.iq.target.host"];
        static string pluginIQTargetPort = ConfigurationManager.AppSettings["plugin.iq.target.port"];
        static string hostName = Dns.GetHostName(); // Retrive the Name of HOST  

        IPHostEntry host = Dns.GetHostEntry(listenerHostName);
        //IPAddress ipAddress = host.AddressList[0];
        static IPAddress ipAddress = IPAddress.Parse(listenerHostName);
        // IPEndPoint localEndPoint = new IPEndPoint(ipAddress, Int32.Parse(listenerPort));
        static IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Parse(listenerHostName), Int32.Parse(listenerPort));

        //IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Parse(ipfirst), Int32.Parse(listenerPort));

        IPAddress targetipAddress = IPAddress.Parse(pluginIQTargetHostName);
        IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Parse(pluginIQTargetHostName), Int32.Parse(pluginIQTargetPort));
        static Socket listener = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        static Socket handler = null;
        static Socket sender;
        static IDictionary<string, string> map;

        static void Main(string[] args)
        {
            FreeTcpPort();
            listener.Bind(localEndPoint);
            //if (checkValidity())
            if (true)
            {
                Log.Info("Host Name of machine =" + hostName);

                Log.Info("IP Address of Machine : " + listenerHostName);

                var exitcode = HostFactory.Run(x =>
                {
                    x.Service<JsonCalling>(s =>
                    {
                        s.ConstructUsing(jsoncalling => new JsonCalling());
                        s.WhenStarted(jsoncalling => jsoncalling.Start());
                        s.WhenStopped(jsoncalling => jsoncalling.Stop());
                    });

                    x.RunAsLocalSystem();

                    x.SetServiceName("PluginIQ");
                    x.SetDisplayName("PluginIQ");
                    x.SetDescription("XML Generation with ByteCode and CRC Code");

                });

            }
            else
            {
                Log.Info("Invalid Licence.");
            }
        }

        public void jsonmain()
        {
            jsontoxml jsontoxml = new jsontoxml();


            map = jsontoxml.populateMapWithCSV();

            try
            {


                listener.Listen(10);

                while (true)
                {
                    byte[] msg = null;

                    Console.WriteLine(localEndPoint);

                    using (Socket handler = listener.Accept())
                    {
                        try
                        {



                            while (true)
                            {
                                msg = null;
                                string data = null;
                                byte[] bytes = null;
                                bytes = new byte[1024000];
                                int bytesRec = handler.Receive(bytes, bytes.Length, SocketFlags.None);
                                Log.Info(bytesRec);
                                Console.WriteLine(bytesRec);
                                data += Encoding.ASCII.GetString(bytes, 0, bytesRec).Replace("\0", string.Empty);
                                Log.Info(data);
                                msg = jsontoxml.processJson(data);
                                break;
                            }
                            handler.Send(msg);




                            // UNCOMMENT THIS SECTION
                            if (handler.Connected)
                            {
                                handler.Close();
                                SendDataToFinalServer(msg);
                            }

                            // UNCOMMENT UNTIL HERE



                            string lines = DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss") + " Successfully sent the ByteFrame to server";
                            Log.Info("Successfully sent the ByteFrame to server");

                            // handler.Shutdown(SocketShutdown.Both);
                            //handler.Close();
                        }
                        catch (Exception exc)
                        {

                            var e = exc;
                            Console.WriteLine("Exception: " + e.Message);
                            if (handler.Connected)
                            {
                                handler.Shutdown(SocketShutdown.Both);
                                handler.Close();
                            }
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                var e = ex;                
                //if (handler.Connected)
                //{
                //    handler.Shutdown(SocketShutdown.Both);
                //    handler.Close();
                //}
                //throw;
                //string lines = DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss") + " Error in Sending ByteFrame to the Server";
                //Log.Error("Error occured while creating the JSON Listener Connection:" + e.ToString());
                //Log.Error("Printing the stack trace" + e.StackTrace);
            }

        }

        private void SendDataToFinalServer(byte[] msg)
        {
            try
            {
                using (Socket finalHandler = new Socket(targetipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp))
                {
                    if (!finalHandler.Connected)
                    {
                        finalHandler.Connect(remoteEndPoint);
                    }
                    finalHandler.Send(msg);
                    Console.WriteLine("Successfully sent the ByteFrame to server");
                }

                //// newEP = new IPEndPoint(ipAddress, 50000);
                //Socket handler1 = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                //handler1.Connect(remoteEndPoint);

                ////handler1.Connect(remoteEndPoint);
                //// sending bytes 
                //handler1.Send(msg);
                //Console.WriteLine("Successfully sent the ByteFrame to server");
            }
            catch (Exception ex)
            {
                var e = ex;
                Console.WriteLine("Exception: " + e.Message);
            }
        }

        static int FreeTcpPort()
        {
            TcpListener l = new TcpListener(IPAddress.Loopback, 0);
            l.Start();
            int port = ((IPEndPoint)l.LocalEndpoint).Port;
            l.Stop();
            return port;
        }
        private byte[] processJson(string jsonData)
        {
            byte[] xmlByteArray = null;

            try
            {
                string xmlString = transformJsonToXMLNoRootNode(jsonData, map);
                if (xmlString != null)
                {
                    //Calculating the Cyclic Redundancy Code
                    GenerateCRC crcGenerator = new GenerateCRC();
                    ushort crcCode = crcGenerator.CalcUTF8CRC(xmlString);

                    Console.WriteLine("crcHexValue " + crcCode);

                    string crcHexValue = crcGenerator.toHex(crcCode);
                    Log.Info("Genereated code.." + crcHexValue);
                    //Populating the root node to the xml fragment
                    string xmlStringWithRootNode = populateXMLWithRootNode(xmlString, crcHexValue);
                    // Log.Info("Complete XML is.." + xmlStringWithRootNode);
                    //byte conversion
                    PackageFrame pkgFrame = new PackageFrame();
                    xmlByteArray = pkgFrame.surroundFisFrame(xmlStringWithRootNode, 102);



                }

            }
            catch (Exception ex)
            {
                //  string lines = DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss") + " Error sent sent the ByteFrame to server";
                Log.Error("ByteFrame processing is cancelled due to Invalid Json");
                Log.Error("Error occured while processing the json message." + ex.Message);
                Log.Error("faulty  json..." + jsonData);
            }
            return xmlByteArray.ToArray();

        }

        private IDictionary<string, string> populateMapWithCSV()
        {
            IDictionary<string, string> Pairsmap = new Dictionary<string, string>();
            try
            {
                IDictionary<string, string> Pairs = new Dictionary<string, string>
                    {
                            {"timeStamp","DtTm"},
                            {"main_phase_act","TSMAIN"},
                            {"arterial_pres_act","DCARPR"},
                            {"venous_pres_act","DCVEPR"},
                            {"blood_flow_act","DCBLFL"},
                            {"dial_end_cond_act","DCENDC"},
                            {"dial_temp_act_c","DCDITP"},
                            {"dial_flow_act","DCDIFL"},
                            {"uf_volume_act_net","DCUFVO"},
                            {"therapy_time_act","DCTHTM"},
                            {"heparin_rate_act_ml","DCHEPR"},
                            {"heparin_volume_act_ml","DCHEPV"},
                            {"tmp_act","DCTMPV"},
                            {"blood_volume_act","DCPRBV"},
                            {"reinf_vol_act","DCRIVO"},
                            {"art_bol_vol_sum","DCHDFC"},
                            {"hep_bolus_vol_sum_ml","DCTHBO"},
                            {"uf_rate_des_disp","DCUFRA"},
                            {"total_inf_vol_act","DCHDFV"},
                            {"online_hdf_volume_act","DSHDFV"},
                            {"ktv_esti_effici_act","DCEKTV"},
                            {"ktv_uv_sp_ktv","DCPSPK"},
                            {"crit_hct","DCHMCT"},
                            {"uf_volume_des","DSUFVO"},
                            {"therapy_time_des","DSUFTS"},
                            {"max_uf_rate_des","DSRMAX"},
                            {"sn_kk_max_pv_des","DSKKXP"},
                            {"sn_kk_min_pv_des","DSKKIP"},
                            {"hep_stop_time_des","DSHEST"},
                            {"av_blood_flow_sn_act","DCTVBF"},
                            {"hep_bolus_vol_des","DSHBVO"},
                            {"nibp_max_sys_limit","DSXSYP"},
                            {"nibp_min_sys_limit","DSISYP"},
                            {"nibp_max_dia_limit","DSXDIP"},
                            {"nibp_min_dia_limit","DSIDIP"},
                            {"nibp_max_pr_limit","DSXPUL"},
                            {"nibp_min_pr_limit","DSIPUL"},
                            {"nibp_cycle_time","DSCYTM"},
                            {"rbv_slope_limit","DSCRBV"},
                            {"nibp_sap","VPRRSY"},
                            {"nibp_dap","VPRRDI"},
                            {"nibp_map","VPBMAP"},
                            {"nibp_pr","VPPULS"},
                            {"min_uf_des","DSUFRA"},
                            {"ktv_uv_ktv_target","DSTKTV"}

                    };
                Pairsmap = Pairs;

            }
            catch (Exception ex)
            {
                Log.Error("Error occured while the IQMapIdentifier CSV Lookup.." + ex.StackTrace);
            }
            return Pairsmap;
        }

        /**
         * This is method to send the ByteFrame packets over TCP/IP 
        */


        public void sendXMLByteFrameToTargetServer(byte[] xmlByteArray)
        {
            Log.Info("Start of sendXMLByteFrameToTargetServer");
            Log.Info("About to transfer the xml byte array.." + xmlByteArray);
            byte[] bytes = new byte[1024000];

            try
            {

                IPHostEntry host = Dns.GetHostEntry(pluginIQTargetHostName);

                IPAddress[] ipv4Addresses = Array.FindAll(host.AddressList,
                                        a => a.AddressFamily == AddressFamily.InterNetwork);
                IPAddress ipAddress = ipv4Addresses[0];

                TcpClient tcpClient = new TcpClient(pluginIQTargetHostName, int.Parse(pluginIQTargetPort));

                try
                {
                    Log.Info("Posting the XmlByteArray to target");

                    NetworkStream stream;
                    stream = tcpClient.GetStream();
                    stream.Write(xmlByteArray, 0, xmlByteArray.Length);
                    stream.Close();
                    tcpClient.Close();

                    Log.Info("End of sendXMLByteFrameToTargetServer");
                }
                catch (ArgumentNullException ane)
                {
                    Log.Error("ArgumentNullException : {0}" + ane.ToString());
                }
                catch (SocketException se)
                {
                    Log.Error("SocketException : {0}" + se.ToString());
                }
                catch (Exception e)
                {
                    Log.Error("Unexpected exception : {0}" + e.ToString());
                }

            }
            catch (Exception e)
            {
                Log.Error("Exception occurred while sending package byte frame to target server" + e.ToString());
            }
        }

        /**
        * Method to transform json to target XML structure without root node
        * 
        * @param - jsonData 
        * @param - IDictionary
        */
        public string transformJsonToXMLNoRootNode(String jsonData, IDictionary<string, string> map)
        {
            Log.Info("Start of transformJsonToXMLNoRootNode");
            Log.Info("About to transform the jsonData to xml..");

            XmlDocument xmlDoc = new XmlDocument();

            try
            {
                XmlNode rootNode = xmlDoc.CreateElement("Rec");
                xmlDoc.AppendChild(rootNode);

                XmlNode userNode = xmlDoc.CreateElement("ver");
                userNode.InnerText = "1";
                rootNode.AppendChild(userNode);


                XmlDocument xmlDocument = JsonConvert.DeserializeXmlNode(jsonData.Replace("<EOF>", " "), "root");
                XmlNodeList xnList = xmlDocument.SelectNodes("/root");
                foreach (XmlNode xn in xnList)
                {
                    XmlNode root = xn.SelectSingleNode("record");
                    if (root != null)
                    {
                        string id = root["timeStamp"].InnerText;
                        userNode = xmlDoc.CreateElement("DtTm");
                        userNode.InnerText = id;
                        rootNode.AppendChild(userNode);

                        userNode = xmlDoc.CreateElement("Type");
                        userNode.InnerText = "1";
                        rootNode.AppendChild(userNode);


                        XmlNodeList CNodes = xn.SelectNodes("/root/record/deviceMetricList");
                        XmlNode firstDeviceMetricNode = CNodes[0]["source"];
                        if (firstDeviceMetricNode["patient"] != null && (firstDeviceMetricNode["patient"].InnerText != null || firstDeviceMetricNode["patient"].InnerText != ""))
                        {

                            XmlDocumentFragment patientFrag = xmlDoc.CreateDocumentFragment();
                            patientFrag.InnerXml = @"<Prm><Id>ADPKID</Id><Val>" + "3" + firstDeviceMetricNode["patient"].InnerText + "</Val></Prm>";
                            rootNode.AppendChild(patientFrag);

                            XmlDocumentFragment nameFrag = xmlDoc.CreateDocumentFragment();
                            nameFrag.InnerXml = @"<Prm><Id>TSSRNB</Id><Val>" + firstDeviceMetricNode["name"].InnerText + "</Val></Prm>";
                            rootNode.AppendChild(nameFrag);

                            XmlDocumentFragment urlFrag = xmlDoc.CreateDocumentFragment();
                            urlFrag.InnerXml = @"<Prm><Id>TSDVNM</Id><Val>" + firstDeviceMetricNode["url"].InnerText + "</Val></Prm>";
                            rootNode.AppendChild(urlFrag);


                            Boolean hdfFlag = false;
                            Boolean hfFlag = false;
                            Boolean predilutionFlag = false;
                            string prmNode = "";
                            string treatmentVar = "";
                            XmlDocumentFragment hdfXFrag = xmlDoc.CreateDocumentFragment();

                            string hdfSubstValue = "";
                            string hfSubstValue = "";
                            string concSourceVal = "";
                            string artBol = "";
                            string hdfArtBol = "";
                            string plannedTime = "";
                            string actTime = "";
                            int remainingTime;
                            bool dbalamrid = false;
                            ArrayList list = new ArrayList();
                            foreach (XmlNode node in CNodes)
                            {
                                //Console.WriteLine("nodeidentifier.InnerText " + node["identifier"].InnerText);
                                // Console.WriteLine("count " + count++ + "CNodes " + CNodes.Count);

                                XmlNode rootNode1 = xmlDoc.CreateElement("Prm");

                                XmlDocumentFragment xfrag = xmlDoc.CreateDocumentFragment();
                                string ide = node["identifier"].InnerText;
                                // string va = node["value"].InnerText;


                                if (ide == "dbi_alarm_id")
                                {
                                    dbalamrid = true;

                                }

                                list.Add(node["identifier"]);
                                list.Add(node["value"]);

                                foreach (KeyValuePair<string, string> kvp in map)
                                {

                                    if (kvp.Key == ide)
                                    {

                                        // mapping the JSON to xml based on csv file 
                                        xfrag.InnerXml = @"<Prm><Id>" + kvp.Value + "</Id><Val>" + node["value"].InnerText + "</Val></Prm>";

                                    }
                                }
                                rootNode.AppendChild(xfrag);
                                // Remaining time logic
                                if (node["identifier"].InnerText.Equals("therapy_time_des"))
                                {
                                    plannedTime = (node["value"].InnerText);
                                }
                                if (node["identifier"].InnerText.Equals("therapy_time_act"))
                                {
                                    actTime = (node["value"].InnerText);
                                }

                                // HF , HDF , Pre-Post Logic
                                if (node["identifier"].InnerText.Equals("hdf_enable_button") && node["value"].InnerText.Equals("0"))
                                {
                                    hdfFlag = true;
                                }
                                if (node["identifier"].InnerText.Equals("hf_enable_button") && node["value"].InnerText.Equals("0"))
                                {
                                    hfFlag = true;
                                }
                                if (node["identifier"].InnerText.Equals("predilution_but_attrs") && node["value"].InnerText.Equals("0"))
                                {
                                    predilutionFlag = true;
                                }
                                if (node["identifier"].InnerText.Equals("hdf_subst_flow"))
                                {
                                    hdfSubstValue = node["value"].InnerText;
                                }
                                if (node["identifier"].InnerText.Equals("hf_subst_flow"))
                                {
                                    hfSubstValue = node["value"].InnerText;
                                }
                                if (node["identifier"].InnerText.Equals("conc_source_des"))
                                {
                                    concSourceVal = node["value"].InnerText;
                                }
                                if (node["identifier"].InnerText.Equals("art_bol_vol_act"))
                                {
                                    artBol = node["value"].InnerText;
                                }
                                if (node["identifier"].InnerText.Equals("hdf_hf_inf_bol_vol_act"))
                                {
                                    hdfArtBol = node["value"].InnerText;
                                }


                            }

                            // remaining time calculation
                            if (plannedTime != "" && actTime != "")
                            {
                                remainingTime = (Int32.Parse(plannedTime) - Int32.Parse(actTime)) / 60;
                                if (remainingTime >= 0)
                                {
                                    XmlDocumentFragment remainingTimeFrag = xmlDoc.CreateDocumentFragment();
                                    remainingTimeFrag.InnerXml = @"<Prm><Id>DCUFTM</Id><val>" + remainingTime.ToString() + "</val></Prm>";
                                    rootNode.AppendChild(remainingTimeFrag);
                                }
                                else
                                {
                                    XmlDocumentFragment remainingTimeFrag = xmlDoc.CreateDocumentFragment();
                                    remainingTimeFrag.InnerXml = @"<Prm><Id>DCUFTM</Id><val>0</val></Prm>";
                                    rootNode.AppendChild(remainingTimeFrag);
                                }
                            }
                            // treatment type logic
                            if (hdfFlag == true && hfFlag == true && predilutionFlag == true)
                            {
                                treatmentVar = "HD";
                                prmNode = @"<Prm><Id>DSHDFT</Id><Val>0</Val></Prm>";
                            }
                            else if (hdfFlag == false && hfFlag == true && predilutionFlag == false)
                            {

                                treatmentVar = "HDF";
                                prmNode = @"<Prm><Id>DSHDFT</Id><Val>4</Val></Prm>";
                            }
                            else if (hdfFlag == false && hfFlag == true && predilutionFlag == true)
                            {
                                treatmentVar = "HDF";
                                prmNode = @"<Prm><Id>DSHDFT</Id><Val>5</Val></Prm>";

                            }
                            else if (hdfFlag == true && hfFlag == false && predilutionFlag == false)
                            {
                                treatmentVar = "HF";
                                prmNode = @"<Prm><Id>DSHDFT</Id><Val>6</Val></Prm>";

                            }
                            else if (hdfFlag == true && hfFlag == false && predilutionFlag == true)
                            {
                                treatmentVar = "HF";
                                prmNode = @"<Prm><Id>DSHDFT</Id><Val>7</Val></Prm>";

                            }

                            if (!prmNode.Equals(""))
                            {
                                XmlDocumentFragment treatmentXFrag = xmlDoc.CreateDocumentFragment();
                                treatmentXFrag.InnerXml = prmNode;
                                rootNode.AppendChild(treatmentXFrag);
                            }

                            // substitute flow logic
                            if (treatmentVar.Equals("HDF"))
                            {
                                XmlDocumentFragment subFlowFrag = xmlDoc.CreateDocumentFragment();
                                subFlowFrag.InnerXml = @"<Prm><Id>DSHRFM</Id><val>" + hdfSubstValue + "</val></Prm>";
                                rootNode.AppendChild(subFlowFrag);
                            }
                            else if (treatmentVar.Equals("HF"))
                            {
                                XmlDocumentFragment subFlowFrag = xmlDoc.CreateDocumentFragment();
                                subFlowFrag.InnerXml = @"<Prm><Id>DSHRFM</Id><val>" + hfSubstValue + "</val></Prm>";
                                rootNode.AppendChild(subFlowFrag);
                            }
                            // Arterial bolus volume logic
                            if (treatmentVar.Equals("HDF") || treatmentVar.Equals("HF"))
                            {
                                XmlDocumentFragment hdfArtBolVol = xmlDoc.CreateDocumentFragment();
                                hdfArtBolVol.InnerXml = @"<Prm><Id>DSOBOV</Id><val>" + hdfArtBol + "</val></Prm>";
                                rootNode.AppendChild(hdfArtBolVol);
                            }
                            if (treatmentVar.Equals("HD"))
                            {
                                XmlDocumentFragment hdArtBolVol = xmlDoc.CreateDocumentFragment();
                                hdArtBolVol.InnerXml = @"<Prm><Id>DSOBOV</Id><val>" + artBol + "</val></Prm>";
                                rootNode.AppendChild(hdArtBolVol);
                            }
                            // alarm logic
                            if (!dbalamrid)
                            {
                                XmlDocumentFragment dbAlramXFrag = xmlDoc.CreateDocumentFragment();
                                dbAlramXFrag.InnerXml = @"<Prm><Id>TSBCAB</Id><val>FFFF</val></Prm>";
                                rootNode.AppendChild(dbAlramXFrag);
                            }
                            else
                            {
                                XmlDocumentFragment dbAlramXFrag = xmlDoc.CreateDocumentFragment();
                                dbAlramXFrag.InnerXml = @"<Prm><Id>TSBCAB</Id><val>FFFB</val></Prm>";
                                rootNode.AppendChild(dbAlramXFrag);
                            }


                            // concentrate source logic
                            if (concSourceVal.Equals("0"))
                            {
                                XmlDocumentFragment concSourceFrag = xmlDoc.CreateDocumentFragment();
                                concSourceFrag.InnerXml = @"<Prm><Id>DSCOSY</Id><val>0</val></Prm>";
                                rootNode.AppendChild(concSourceFrag);
                            }
                            else if (concSourceVal.Equals("1"))
                            {
                                XmlDocumentFragment concSourceFrag = xmlDoc.CreateDocumentFragment();
                                concSourceFrag.InnerXml = @"<Prm><Id>DSCOSY</Id><val>2</val></Prm>";
                                rootNode.AppendChild(concSourceFrag);
                            }
                            else if (concSourceVal.Equals("2"))
                            {
                                XmlDocumentFragment concSourceFrag = xmlDoc.CreateDocumentFragment();
                                concSourceFrag.InnerXml = @"<Prm><Id>DSCOSY</Id><val>3</val></Prm>";
                                rootNode.AppendChild(concSourceFrag);
                            }
                            //rootNode = createXmlFragBasedOnRule(xmlDocument, rootNode);
                        }
                        else
                        {
                            Log.Error("Invalid Json. No PatientID found");
                            throw new JsonSerializationException();
                        }

                    }
                }

                //  xmlDoc.Save("XMLWithoutRoot.xml");
                //Log.Info("The transformed inner xml without root node is.." + xmlDoc.DocumentElement.InnerXml);
                Log.Info("End of transformJsonToXMLNoRootNode");
                return xmlDoc.DocumentElement.InnerXml;
            }
            catch (JsonReaderException jse)
            {
                Log.Error("Error occured while transforming the json to XML.." + jse.Message);
                Log.Error(jse.StackTrace);
                Log.Error("Invalid Json.. Unable to parse the recieved json");
                throw new JsonReaderException("Invalid Json..", jse);
            }
        }

        /**
         * Method to transform json to target XML structure
         * 
         * @param  - jsonData 
         */
        public string populateXMLWithRootNode(string xmlFragment, string crcHexValue)
        {
            Log.Info("Start of populateXMLWithRootNode");
            //Log.Info("The xml fragment is.."+ xmlFragment);
            Log.Info("The calucated CRC hex value  is.." + crcHexValue);
            XmlDocument xmlDocumentWithRootNode = new XmlDocument();
            var declaration = xmlDocumentWithRootNode.CreateXmlDeclaration("1.0", "UTF-8", null);
            xmlDocumentWithRootNode.AppendChild(declaration);
            XmlNode rootNode = xmlDocumentWithRootNode.CreateElement("Rec");
            XmlAttribute attribute;
            attribute = xmlDocumentWithRootNode.CreateAttribute("crc");
            attribute.Value = crcHexValue;
            rootNode.Attributes.Append(attribute);
            xmlDocumentWithRootNode.AppendChild(rootNode);
            //xmlDocumentWithRootNode.LoadXml(xmlFragment);
            XmlDocumentFragment xmlDocFrag = xmlDocumentWithRootNode.CreateDocumentFragment();
            xmlDocFrag.InnerXml = xmlFragment;
            XmlElement rootElement = xmlDocumentWithRootNode.DocumentElement;
            rootElement.AppendChild(xmlDocFrag);

            // xmlDocumentWithRootNode.Save("CompleteXML.xml");

            //string conversion
            string completeXMLString;

            using (TextWriter writer = new StringWriter())
            {
                xmlDocumentWithRootNode.Save(writer);
                completeXMLString = writer.ToString().Replace("\r\n\t", string.Empty).Replace("utf-16", "utf-8");
            }

            //Log.Info("The complete xml with root node is.." + removeWhitespace(completeXMLString));
            Log.Info("End of populateXMLWithRootNode");

            return removeWhitespace(completeXMLString);
        }

        /**
         * Utility to remove unwanted whitespaces in the xml string
         * 
         * @param  - jsonData 
         */
        public static string removeWhitespace(string xml)
        {
            Regex regex = new Regex(@">\s*<");
            xml = regex.Replace(xml, "><");

            return xml.Trim();
        }
        public static string stripJson(string json)
        {
            json = Regex.Replace(json, "}?$", "");
            Regex regex = new Regex(@"}?*");
            json = regex.Replace(json, "}");

            return json.Trim();
        }

        private static void stopJsonListener()
        {
            handler.Shutdown(SocketShutdown.Both);
            handler.Close();
        }

        private static void stopXmlSender()
        {
            sender.Shutdown(SocketShutdown.Both);
            sender.Close();
        }

        private XmlNode createXmlFragBasedOnRule(XmlDocument xmlDoc, XmlNode rootNode)
        {
            XmlNodeList nodeList = xmlDoc.SelectNodes("/root/record/deviceMetricList");
            Boolean hdfFlag = false;
            Boolean hfFlag = false;
            Boolean predilutionFlag = false;
            string prmNode = "";
            string treatmentVar = "";
            XmlDocumentFragment hdfXFrag = xmlDoc.CreateDocumentFragment();
            string hdfSubstValue = "";
            string hfSubstValue = "";
            string concSourceVal = "";
            string artBol = "";
            string hdfArtBol = "";


            foreach (XmlNode xn in nodeList)
            {
                if (xn["identifier"].InnerText.Equals("hdf_enable_button") && xn["value"].InnerText.Equals("0"))
                {
                    hdfFlag = true;
                }
                if (xn["identifier"].InnerText.Equals("hf_enable_button") && xn["value"].InnerText.Equals("0"))
                {
                    hfFlag = true;
                }
                if (xn["identifier"].InnerText.Equals("predilution_but_attrs") && xn["value"].InnerText.Equals("0"))
                {
                    predilutionFlag = true;
                }
                if (xn["identifier"].InnerText.Equals("hdf_subst_flow"))
                {
                    hdfSubstValue = xn["value"].InnerText;
                }
                if (xn["identifier"].InnerText.Equals("hf_subst_flow"))
                {
                    hfSubstValue = xn["value"].InnerText;
                }
                if (xn["identifier"].InnerText.Equals("conc_source_des"))
                {
                    concSourceVal = xn["value"].InnerText;
                }
                if (xn["identifier"].InnerText.Equals("art_bol_vol_act"))
                {
                    artBol = xn["value"].InnerText;
                }
                if (xn["identifier"].InnerText.Equals("hdf_hf_inf_bol_vol_act"))
                {
                    hdfArtBol = xn["value"].InnerText;
                }

            }

            // treatment type logic
            if (hdfFlag == true && hfFlag == true && predilutionFlag == true)
            {
                treatmentVar = "HD";
                prmNode = @"<Prm><Id>DSHDFT</Id><Val>0</Val></Prm>";
            }
            else if (hdfFlag == false && hfFlag == true && predilutionFlag == false)
            {

                treatmentVar = "HDF";
                prmNode = @"<Prm><Id>DSHDFT</Id><Val>4</Val></Prm>";
            }
            else if (hdfFlag == false && hfFlag == true && predilutionFlag == true)
            {
                treatmentVar = "HDF";
                prmNode = @"<Prm><Id>DSHDFT</Id><Val>5</Val></Prm>";

            }
            else if (hdfFlag == true && hfFlag == false && predilutionFlag == false)
            {
                treatmentVar = "HF";
                prmNode = @"<Prm><Id>DSHDFT</Id><Val>6</Val></Prm>";

            }
            else if (hdfFlag == true && hfFlag == false && predilutionFlag == true)
            {
                treatmentVar = "HF";
                prmNode = @"<Prm><Id>DSHDFT</Id><Val>7</Val></Prm>";

            }

            if (!prmNode.Equals(""))
            {
                XmlDocumentFragment treatmentXFrag = xmlDoc.CreateDocumentFragment();
                treatmentXFrag.InnerXml = prmNode;
                rootNode.AppendChild(treatmentXFrag);
            }

            // substitute flow logic
            if (treatmentVar.Equals("HDF"))
            {
                XmlDocumentFragment subFlowFrag = xmlDoc.CreateDocumentFragment();
                subFlowFrag.InnerXml = @"<Prm><Id>DSHRFM</Id><val>" + hdfSubstValue + "</val></Prm>";
                rootNode.AppendChild(subFlowFrag);
            }
            else if (treatmentVar.Equals("HF"))
            {
                XmlDocumentFragment subFlowFrag = xmlDoc.CreateDocumentFragment();
                subFlowFrag.InnerXml = @"<Prm><Id>DSHRFM</Id><val>" + hfSubstValue + "</val></Prm>";
                rootNode.AppendChild(subFlowFrag);
            }
            // Arterial bolus volume logic
            if (treatmentVar.Equals("HDF") || treatmentVar.Equals("HF"))
            {
                XmlDocumentFragment hdfArtBolVol = xmlDoc.CreateDocumentFragment();
                hdfArtBolVol.InnerXml = @"<Prm><Id>DSOBOV</Id><val>" + hdfArtBol + "</val></Prm>";
                rootNode.AppendChild(hdfArtBolVol);
            }
            if (treatmentVar.Equals("HD"))
            {
                XmlDocumentFragment hdArtBolVol = xmlDoc.CreateDocumentFragment();
                hdArtBolVol.InnerXml = @"<Prm><Id>DSOBOV</Id><val>" + artBol + "</val></Prm>";
                rootNode.AppendChild(hdArtBolVol);
            }


            // concentrate source logic
            if (concSourceVal.Equals("0"))
            {
                XmlDocumentFragment concSourceFrag = xmlDoc.CreateDocumentFragment();
                concSourceFrag.InnerXml = @"<Prm><Id>DSCOSY</Id><val>0</val></Prm>";
                rootNode.AppendChild(concSourceFrag);
            }
            else if (concSourceVal.Equals("1"))
            {
                XmlDocumentFragment concSourceFrag = xmlDoc.CreateDocumentFragment();
                concSourceFrag.InnerXml = @"<Prm><Id>DSCOSY</Id><val>2</val></Prm>";
                rootNode.AppendChild(concSourceFrag);
            }
            else if (concSourceVal.Equals("2"))
            {
                XmlDocumentFragment concSourceFrag = xmlDoc.CreateDocumentFragment();
                concSourceFrag.InnerXml = @"<Prm><Id>DSCOSY</Id><val>3</val></Prm>";
                rootNode.AppendChild(concSourceFrag);
            }



            return rootNode; ;
        }

        private static string GetMacAddress()
        {
            string macAddresses = string.Empty;

            foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (nic.OperationalStatus == OperationalStatus.Up)
                {
                    macAddresses += nic.GetPhysicalAddress().ToString();
                    break;
                }
            }

            return macAddresses;
        }
        public static bool checkValidity()
        {
            CipherText obj = new CipherText();
            bool valid = false;
            DateTime todayDate = DateTime.Now;
            XmlDocument xmlDom = new XmlDocument();
            xmlDom.Load(@"Settings.xml");
            XmlNode newXMLNode1 = xmlDom.SelectSingleNode("/Validations/Param1");
            string p2 = obj.Encrypt("02-20-2019");
            string p1 = obj.Decrypt(newXMLNode1.InnerText);
            DateTime validdate = Convert.ToDateTime(p1);
            XmlNode newXMLNode2 = xmlDom.SelectSingleNode("/Validations/Param2");
            string macadd = GetMacAddress();
            if (!string.IsNullOrEmpty(newXMLNode2.InnerText))
            {
                string existingAdd = obj.Decrypt(newXMLNode2.InnerText);
                if (macadd == existingAdd)
                    valid = true;
            }
            else
            {
                if (todayDate <= validdate)
                {
                    newXMLNode2.InnerText = obj.Encrypt(macadd);
                    valid = true;
                }
                else
                {
                    Log.Info("Invalid Licence.");
                    valid = false;
                }
            }
            xmlDom.Save("Settings.xml");
            return valid;
        }


    }


}