using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;
using System.Xml;

namespace jsontoxml
{
    internal class XNode
    {

        static void Main(string[] args)
        {
            String json = null;
            using (WebClient wc = new WebClient())
            {
                json = System.IO.File.ReadAllText(@"C:/Users/Administrator/Downloads/test.xml");
            }
            Byte[] data = System.Text.UTF8Encoding.ASCII.GetBytes(json);

            NetworkStream stream = client.GetStream();
            StreamWriter writer = new StreamWriter(stream, Encoding.UTF8);
            writer.AutoFlush = false;
            writer.Write(Encoding.UTF8.GetBytes(json).Length);
            writer.Write(json);
            writer.Flush();
            stream.Write(data, 0, data.Length);
            MessageBox.Show(stream.DataAvailable.ToString()); // Here I'm getting the status for DataAvailable as False


            Byte[] bytes = new Byte[client.ReceiveBufferSize];
            string responseData;
            stream.Read(bytes, 0, Convert.ToInt32(client.ReceiveBufferSize));
            responseData = Encoding.ASCII.GetString(bytes);
            stream.Close();
            client.Close();
        }

    }
}