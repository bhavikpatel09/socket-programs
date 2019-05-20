using System;
using System.Configuration;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace JsonSender
{
    class Program
    {
        public static int Main(String[] args)
        {

            string ipaddress = ConfigurationManager.AppSettings["ipaddress"];
            string port = ConfigurationManager.AppSettings["port"];
            StartClient(ipaddress, port);

            return 0;
        }


        public static void StartClient(string ipaddress, string port)
        {
            byte[] bytes = new byte[1024000];

            //String jsonFileName = "HD.json";
            //String jsonFileName = "HDF.json";
            //String jsonFileName = "HDF_Pre.json";
            //String jsonFileName = "HF.json";
            //String jsonFileName = "NIBP.json";
            //String jsonFileName = "HF_Pre.json";
            //String jsonFileName = "HD_Alarm.json";
            //String jsonFileName = "alarm_HD1.json";
            //   String jsonFileName = "alarm_HD2.json";
            //String jsonFileName = "Alarm_BP.json";


            try
            {
                IPAddress iPAddress = IPAddress.Parse(ipaddress);
                IPEndPoint remoteEP = new IPEndPoint(iPAddress, Int32.Parse(port));

                byte[] msg = null;
                try
                {

                    DirectoryInfo dir = new DirectoryInfo(@"D:\Bhavik\files\");//Folder path of 103 files
                    int cnt = dir.GetFiles().Length;

                    for (int i = 1; i <= cnt; i++)
                    {
                        using (Socket sender = new Socket(iPAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp))
                        {
                            if (!sender.Connected)
                                sender.Connect(remoteEP);
                            string fullpath = i + ".json";


                            string path = dir + fullpath;
                            string s = "";
                            string fullfile = "";
                            using (StreamReader sr = File.OpenText(path))
                            {

                                while ((s = sr.ReadLine()) != null)
                                {
                                    fullfile += s;
                                }
                            }
                            fullfile += "<EOF>";

                            msg = Encoding.ASCII.GetBytes(fullfile);

                            Console.WriteLine("Sent files {0}", i);

                            int bytesSent = sender.Send(msg);
                            System.Threading.Thread.Sleep(300);
                        }
                    }
                }
                catch (ArgumentNullException ane)
                {
                    Console.WriteLine("ArgumentNullException : {0}", ane.ToString());
                }
                catch (SocketException se)
                {
                    Console.WriteLine("SocketException : {0}", se.ToString());
                }
                catch (Exception e)
                {
                    Console.WriteLine("Unexpected exception : {0}", e.ToString());
                }

            }
            catch (Exception e)
            {
                Console.WriteLine("exception in try block " + e.ToString());
            }
        }


        public byte[] surroundFisFrame(string data_, UInt32 plgId_)
        {
            byte[] data = Encoding.ASCII.GetBytes(data_);
            byte[] opId = BitConverter.GetBytes((UInt32)0);
            byte[] plgId = BitConverter.GetBytes((UInt32)plgId_);
            byte[] len = BitConverter.GetBytes((UInt32)(data_.Length + 1));

            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(opId);
                Array.Reverse(plgId);
                Array.Reverse(len);
            }

            byte[] fisPkg = new byte[opId.Length + plgId.Length + len.Length + data_.Length + 1];
            opId.CopyTo(fisPkg, 0);
            plgId.CopyTo(fisPkg, opId.Length);
            len.CopyTo(fisPkg, opId.Length + plgId.Length);
            data.CopyTo(fisPkg, opId.Length + plgId.Length + len.Length);
            fisPkg[fisPkg.Length - 1] = 0;
            return fisPkg;
        }
    }


}


