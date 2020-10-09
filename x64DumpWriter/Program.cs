using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MinidumpWriter;

namespace x64DumpWriter
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                return;
            }

            Utility.MakeDump(args[0], Int32.Parse(args[1]));
        }
    }
}
