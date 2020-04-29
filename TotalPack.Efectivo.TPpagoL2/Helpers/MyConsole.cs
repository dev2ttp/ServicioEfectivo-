using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TPpagoL2.Helpers
{
    static class MyConsole
    {
        public static void WriteLine()
        {
            if (Environment.UserInteractive) Console.WriteLine();
        }

        public static void WriteLine(string value)
        {
            if (Environment.UserInteractive) Console.WriteLine(DateTime.Now.ToString("HH:mm:ss") + new String(' ', 3) + value);
        }

        public static void WriteLine(string format, params object[] args)
        {
            if (Environment.UserInteractive) Console.WriteLine(DateTime.Now.ToString("HH:mm:ss") + new String(' ', 3) + format, args);
        }

        public static void Clear()
        {
            if (Environment.UserInteractive) Console.Clear();
        }
    }
}
