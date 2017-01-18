using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace IniParser
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length > 0 && File.Exists(args[0]))
            {
                string input = File.ReadAllText(args[0]);
                Console.WriteLine(string.Format("read {0} chars from {1}", input.Length, args[0]));
                BMyIni ini = new BMyIni(input);
                string output = ini.GetSerialized();
                string savefile = string.Format(@"{0}.{1}.txt", args[0], Guid.NewGuid());
                File.WriteAllText(savefile, output);
                Console.WriteLine("saved {0} chars to {1}", output.Length, savefile);
            } else
            {
                Console.WriteLine("args < 2");
            }
            Console.ReadLine();
        }        
    }
}
