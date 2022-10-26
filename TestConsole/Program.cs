using CSBase.Diagnose;

using CSharpCommonTest;

using Microsoft.Win32;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            // DiagnoseGlobalInfo.GetInstance().Init("Test", Registry.LocalMachine, "Software\\NextLabs\\Compliant Enterprise\\Exchange Enforcer", "InstallDir", "Logs", "Config\\logcfg.xml", "Logs", "Bin");

            DiagnoseGlobalInfo.GetInstance().Init("Test", "Logs\\", "Config\\logcfg.xml", "Logs", "");

            Console.WriteLine("\nPlease input any key to start test\n");
            Console.ReadKey();

            ConsoleKeyInfo chKey = new ConsoleKeyInfo('e', ConsoleKey.E, false, false, false);
            do
            {
                DomainHelperTester.Test();

                Console.WriteLine("\nInput 'E' or 'e' to do test again, otherwise exit\n");
                chKey = Console.ReadKey();
            } while ('E' == chKey.KeyChar || 'e' == chKey.KeyChar);
        }
    }
}
