using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;

namespace EconnectTMSservice
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {

            ClsAccountOpening acopen = new ClsAccountOpening();
            string inmsg = "569980#AGENCY#0000011300031089#1198480014850159#250788731151#1001#";
            acopen.LookUp(inmsg, "1234", "12345");
            Thread.Sleep(10000);
            acopen.OpenAccount(inmsg, "1234", "1234");

            Console.ReadLine();
            //if (args.Length > 0 && args[0].ToLower() == "/console")
            //{


            //    ClsMain EcThirdparty = new ClsMain();
            //    EcThirdparty.Start();

            //    string input = string.Empty;
            //    Console.Write("EconnectTMS Service Engine Console started. Type 'exit' to stop the application: ");

            //    // Wait for the user to exit the application
            //    while (input.ToLower() != "exit") input = Console.ReadLine();

            //    // Stop the application.
            //    EcThirdparty.Stop();
            //}
            //else
            //{
            //    ServiceBase[] ServicesToRun;
            //    ServicesToRun = new ServiceBase[] 
            //{ 
            //    new Service1() 
            //};
            //    ServiceBase.Run(ServicesToRun);
            //}
        }
    }
}
