using System;
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
        static bool skipExpression = false;
        static List<string> PrefixRemove = new List<string>();
        static List<string> TagLangPrefix = new List<string>();
        static List<string> PathsRemove = new List<string>();
        static bool addPrefixAsExtraInfo = true;
        static string outputFn = "";
        static string procdll = "";
        static string fn_optprintval = "";
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
                    case "--skipexpr":
                        skipExpression = true;
                        break;
                    case "--prefix":
                        i++;
                        if (i < args.Length)
                            PrefixRemove.Add(args[i]);
                        break;
                    case "--langprefix":
                        i++;
                        if (i < args.Length)
                            TagLangPrefix.Add(args[i]);
                        break;
                    case "--removelinepath":
                        i++;
                        if (i < args.Length) 
                            PathsRemove.Add(args[i]);
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
                    case "--procdll":
                        i++;
                        if (i < args.Length) procdll = args[i];
                        break;
                    case "--fn_optvalue":
                        i++;
                        if (i < args.Length) fn_optprintval = args[i];
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

  -o, --output %fn% - the output file name. File is written in UTF8 encoding
                 if not specified, the file is written to stdout
  
  --prefix %txt% - the prefix symbol or substring used in strings.
                 any text before prefix would be removed from the resulting translating string
                 However the text before prefix should not contain whitespace

  --skipexpr   - don't look for the lines in expressions (lines starting with ~)

  --langprefix %txt% - the TAG prefix that is used to indicate the language transaltion Id.
  
  --alphabet   - the text must have some characters in order to translated
                 if the line consists only of numbers and non characters, it's skipped

  --onlyinput  - only gather strings from the %inputFile.ink%
                 this is used for multi-file scenarios (where INPUT is used)
  
  --walk       - don't try to produce POT file, only prodcedure the parsed structure
                 to stdout. (output file is not used)

  --procdll    - the dll file to be loaded
                        
  --fn_optvalue - the classname and the method to be called for processing
                 the option value to be processed

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

        static void RemoveFilePaths(List<string> rm, InkToLocalizeLines lines)
        {
            foreach(LocalizedLine ll in lines.lines)
            {
                foreach(string p in rm)
                {
                    int i = ll.filename.IndexOf(p, StringComparison.OrdinalIgnoreCase);
                    if (i == 0)
                        ll.filename = ll.filename.Substring(p.Length);
                }
            }
        }

        static void RemovePrefix(List<string> pfx, InkToLocalizeLines lines, bool updateExtraInfo)
        {
            if ((pfx == null) || (pfx.Count == 0)) return;
            //List<LocalizedLine> empty = new List<LocalizedLine>();
            for (int i = lines.lines.Count-1; i >=0; i --)
            {
                LocalizedLine l = lines.lines[i];
                string cutpfx;
                l.text = RemovePrefix(pfx, l.text, out cutpfx);
                if (updateExtraInfo)
                    l.extraInfo += string.Format("prefix ({0})", cutpfx);
                if (string.IsNullOrWhiteSpace(l.text))
                    lines.lines.RemoveAt(i);
            }
        }

        static int RemoveDuplicates(InkToLocalizeLines lines, List<LocalizedLine> removed = null)
        {
            Dictionary<string, LocalizedLine> used = new Dictionary<string, LocalizedLine>();
            List<int> rm = new List<int>();
            for (int i = 0; i < lines.lines.Count; i++)
            {
                LocalizedLine l = lines.lines[i];
                string m = l.EstimateMsgId();
                LocalizedLine p;
                if (!used.TryGetValue(m, out p))
                {
                    used[m] = l;
                    continue;
                }
                rm.Add(i);
            }

            for (int i = rm.Count-1; i>=0; i--)
            {
                int j = rm[i];
                if (removed != null) removed.Add(lines.lines[j]);
                lines.lines.RemoveAt(j);
            }
            return rm.Count;
        }


        static void AddPotHeader(StringBuilder b)
        {
            b.AppendLine("#, fuzzy");
            b.AppendLine("msgid \"\"");
            b.AppendLine("msgstr \"\"");
            b.AppendLine("\"Content-Type: text/plain; charset=UTF-8\\n\"");
            b.AppendLine("\"Content-Transfer-Encoding: 8bit\\n\"");
            b.AppendLine();
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
                ll.GoExpressions = !skipExpression;
                ll.langTagPrefix.AddRange(TagLangPrefix);
                ll.GatherLines(flow, inputFn);

                if (PrefixRemove.Count > 0)
                    RemovePrefix(PrefixRemove, ll, addPrefixAsExtraInfo);
                if (PathsRemove.Count > 0)
                    RemoveFilePaths(PathsRemove, ll);

                LinesProcess lp;
                if (!string.IsNullOrEmpty(procdll))
                {
                    DllProcess dll = new DllProcess();
                    procdll = System.IO.Path.GetFullPath(procdll);
                    dll.dll = procdll;
                    dll.optvalfn = fn_optprintval;
                    dll.LoadAsm();
                    lp = dll;
                 } else 
                    lp = new LinesProcess();

                foreach(LocalizedLine l in ll.lines)
                {
                    if (l.isAnswerOption)
                        l.text = lp.OptValForPot(l.text);
                    else
                        l.text = lp.LineForPot(l.text);
                }


                int r = RemoveDuplicates(ll);
                if (r > 0)
                    Console.WriteLine("removed duplicated: {0}", r);

                StringBuilder b = new StringBuilder();

                AddPotHeader(b);

                foreach (LocalizedLine l in ll.lines)
                    b.AppendLine(l.ToPotString());
                if (!string.IsNullOrWhiteSpace(outputFn))
                    File.WriteAllText(outputFn, b.ToString(), new UTF8Encoding(false));
                else
                    Console.WriteLine(b.ToString());
            }
            catch (Exception x)
            {
                Console.WriteLine(x.Message);
                Console.WriteLine(x.StackTrace);
            }
        }
    }
}
