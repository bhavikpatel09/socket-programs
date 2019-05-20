using Newtonsoft.Json;
using System;
using System.Net;
using System.Xml;

namespace jsontoxml
{
    class jsontoxml
    {
        static void Main(string[] args)
        {
            String json = null;
            using (WebClient wc = new WebClient())
            {

                 json = System.IO.File.ReadAllText(@"C:/Users/Administrator/Downloads/test2.json");

                //json = wc.DownloadString("http://api.plos.org/search?q=title:DNA");

                // Display the file contents to the console. Variable text is a string.
                //System.Console.WriteLine("Contents of WriteText.txt = {0}", json);

               // json = wc.DownloadString("http://api.plos.org/search?q=title:DNA");
            }
            //string json = "";

             //XmlDocument doc = (XmlDocument)JsonConvert.DeserializeXmlNode(json,"root");
            //Console.WriteLine("doc "+doc);
            // XmlDocument xmlDocument = JsonConvert.DeserializeXmlNode(System.IO.File.ReadAllText(@"C:\Users\Administrator\Downloads\test.txt"));
            
            XmlDocument xmlDocument = JsonConvert.DeserializeXmlNode(json, "root");
            XmlTextWriter xmlTextWriter = new XmlTextWriter("json1.xml", null);
            xmlTextWriter.Formatting = System.Xml.Formatting.Indented;
            xmlDocument.Save(xmlTextWriter);






        }
    }
}