﻿using System;
using System.Text;
using System.Collections.Generic;
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
            if (obj is Text t)
            {
                if ((!showWhiteSpace) && (string.IsNullOrWhiteSpace(t.text)))
                    return;
                Console.Write(pfx);
                Console.Write(obj.GetType().Name);
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
            else
            {
                Console.Write(pfx);
                Console.Write(obj.GetType().Name);

                if (obj is Ink.Parsed.Choice ch)
                {
                    Console.WriteLine();
                    Console.Write(pfx);
                    Console.WriteLine(" has start:  {0}", (ch.startContent != null));
                    Console.Write(pfx);
                    Console.WriteLine(" has inner:  {0}", (ch.innerContent != null));
                    Console.Write(pfx);
                    Console.WriteLine(" has choice: {0}", (ch.choiceOnlyContent != null));
                }
            }

            Console.WriteLine();
            if (obj.content != null)
            {
                foreach (Ink.Parsed.Object sub in obj.content)
                    WalkObj(sub, pfx + "  ");
            }
        }

        static bool onlyWalk = false;
        static bool showWhiteSpace = false;
        static string inputFn = "";
        static bool onlyInputFile = false;
        static List<string> PrefixRemove = new List<string>();
        static bool addPrefixAsExtraInfo = true;
        static string outputFn = "";
        static bool showHelp;

        static void ParseArgs(string[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                string l = arg.ToLower();
                switch (l)
                {
                    case "--walk":
                        onlyWalk = true;
                        break;
                    case "--onlyinput":
                        // only get the translation from the input file
                        // and ignore INCLUDE files
                        onlyInputFile = true;
                        break;
                    case "--prefix":
                        i++;
                        if (i < args.Length)
                            PrefixRemove.Add(args[i]);
                        break;
                    case "-o":
                    case "--output":
                        i++;
                        if (i < args.Length) outputFn = args[i];
                        break;
                    case "-h":
                    case "--help":
                        showHelp = true;
                        break;
                    default:
                        inputFn = arg;
                        break;
                }
            }
        }

        static void PrintHelp()
        {
            Console.WriteLine(
@" gettenk.exe [options] %inputFile.ink%

%inputFile.ink% - the input .ink file

options:

  -o, --output - the output file name. File is written in UTF8 encoding
                 if not specified, the file is written to stdout
  
  --prefix     - the prefix symbol or substring used in strings.
                 any text before prefix would be removed from the resulting translating string
                 However the text before prefix should not contain whitespace
  
  --onlyinput  - only gather strings from the %inputFile.ink%
                 this is used for multi-file scenarios (where INPUT is used)
  
  --walk       - don't try to produce POT file, only prodcedure the parsed structure
                 to stdout. (output file is not used)
  
  -h, --help   - show this text
");
        }

        static bool IsPrefix(string s, int ofs)
        {
            while ((ofs >= 0) && (s[ofs] != ' ') && (s[ofs] != '\t'))
                ofs--;
            return (ofs < 0);
        }

        static string RemovePrefix(List<string> pfx, string ln, out string cut)
        {
            cut = "";
            foreach (string prefix in pfx)
            {
                int i = ln.IndexOf(prefix);
                if (i < 0) continue;
                if (IsPrefix(ln, i))
                {
                    cut = ln.Substring(0, i);
                    return ln.Substring(i + prefix.Length).TrimStart();
                }
            }
            return ln;
        }

        static void RemovePrefix(List<string> pfx, InkToLocalizeLines lines, bool updateExtraInfo)
        {
            if ((pfx == null) || (pfx.Count == 0)) return;
            foreach (LocalizedLine l in lines.lines)
            {
                string cutpfx;
                l.text = RemovePrefix(pfx, l.text, out cutpfx);
                if (updateExtraInfo)
                    l.extraInfo += string.Format("prefix ({0})", cutpfx);
            }
        }

        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("please provide the input file name");
                Console.WriteLine();
                PrintHelp();
                return;
            }

            ParseArgs(args);
            if (showHelp)
            {
                PrintHelp();
                return;
            }

            if (string.IsNullOrEmpty(inputFn))
            {
                Console.WriteLine("need the input .ink file");
                return;
            }

            string js = File.ReadAllText(inputFn);
            InkParser p = new InkParser(js, inputFn);

            try
            {
                Ink.Parsed.Story flow;
                if (onlyWalk)
                {
                    flow = p.Parse();
                    WalkObj(flow);
                    Console.WriteLine("done.");
                    return;
                }

                flow = p.Parse();
                InkToLocalizeLines ll = new InkToLocalizeLines();
                ll.OnlyStartFile = onlyInputFile;
                ll.GatherLines(flow);

                if (PrefixRemove.Count > 0)
                    RemovePrefix(PrefixRemove, ll, addPrefixAsExtraInfo);

                if (!string.IsNullOrWhiteSpace(outputFn))
                {
                    StringBuilder b = new StringBuilder();
                    foreach (LocalizedLine l in ll.lines)
                        b.AppendLine(l.ToPotString());
                    File.WriteAllText(outputFn, b.ToString(), Encoding.UTF8);
                }
                else
                {
                    foreach (LocalizedLine l in ll.lines)
                    {
                        Console.WriteLine(l.ToPotString());
                    }
                }
            }
            catch (Exception x)
            {
                Console.WriteLine(x.Message);
                Console.WriteLine(x.StackTrace);
            }
        }
    }
}
