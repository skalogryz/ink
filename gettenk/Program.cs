using System;
using System.Text;
using System.IO;
using Ink;
using Ink.Runtime;
using Ink.Parsed;

namespace gettenk
{
    class Program
    {

        static void WalkObj(Ink.Parsed.Object obj, string pfx = "")
        {
            if (obj == null) return;
            Console.Write(pfx);
            Console.Write(obj.GetType().Name);
            if ((obj is Text t) && (!string.IsNullOrWhiteSpace(t.text)))
            {
                Console.Write(" \"{0}\"", t.text);
                if (t.hasOwnDebugMetadata)
                {
                    Console.Write(" (");
                    Console.Write(t.debugMetadata.fileName);
                    Console.Write(":");
                    Console.Write(t.debugMetadata.startLineNumber);
                    Console.Write(") *");
                }
            }
            Console.WriteLine();
            if (obj.content != null)
            {
                foreach (Ink.Parsed.Object sub in obj.content)
                    WalkObj(sub, pfx + "  ");
            }
        }

        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("please provide the input file name");
                return;
            }

            string js = File.ReadAllText(args[0]);
            InkParser p = new InkParser(js, args[0]);

            try
            {
                Console.WriteLine("parsing...");
                Ink.Parsed.Story flow = p.Parse();
                Console.WriteLine("done!");
                if (flow == null)
                {
                    Console.WriteLine("empty");
                    return;
                }
                WalkObj(flow);

            }
            catch (Exception x)
            {
                Console.WriteLine(x.Message);
                Console.WriteLine(x.StackTrace);
            }
        }
    }
}
