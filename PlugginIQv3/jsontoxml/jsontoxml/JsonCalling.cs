using System;
using System.Collections.Generic;
using System.IO;
using System.ServiceProcess;
using System.Text;
//using System.Threading;
using System.Timers;

namespace pluginiq
{
    class JsonCalling
    {
        private static Timer _timer;

        public JsonCalling()
        {
            _timer = new Timer(10000);
            _timer.Elapsed += TimerElapsed;
            //Console.WriteLine("test1");
        }

        private static void TimerElapsed(Object sender, ElapsedEventArgs e)
        {
            //Console.WriteLine("test");
            
            jsontoxml js = new jsontoxml();
            js.jsonmain();
            //string[] lines = new string[] { DateTime.Now.ToString() };
             
        }

        public void Start()
        {
            jsontoxml js = new jsontoxml();
            js.jsonmain();
            Console.WriteLine("start");
            _timer.Start();
            
        }

        public void Stop()
        {
            _timer.Stop();
        }


       /* public void InvokeMethod()
        {
            while (true)
            {
                jsontoxml js = new jsontoxml();
                js.jsonmain();
                Thread.Sleep(1000); // 1 seconds
            }
        }

        public void OnStop()
        {
            
        }*/

    }
}
