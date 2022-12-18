using System;
using System.Text;
using System.Collections.Generic;
using System.IO;
using Ink;
using Ink.Runtime;
using Ink.Parsed;

namespace useofink
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

        public class FuncTrack
        {
            public string funcName;
            public List<int> prm = new List<int>();
            public bool AllParams = true;

            public Dictionary<string, bool> usedParams = new Dictionary<string, bool>();

            public bool IsTrack(int idx)
            {
                if (AllParams) return true;
                return (prm.IndexOf(idx) >= 0);
            }

            public bool ParseNameArgs(string nameArgs)
            {
                string[] na = nameArgs.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (na.Length == 0) return false;

                funcName = na[0];
                if (na.Length == 1)
                {
                    AllParams = true;
                    return true;
                }
                AllParams = !IsParamsNum(na[1], prm);
                return true;
            }
        }

        static void GatherParams(FuncTrack ft, FunctionCall fc)
        {
            //if (fc.proxyDivert == null) return;
            if ((fc.arguments == null) || (fc.arguments.Count == 0))
                return;
            for (int i = 0; i < fc.arguments.Count; i++)
            {
                int idx = i + 1;
                if (!ft.IsTrack(idx)) continue;

                if (fc.arguments[i] is StringExpression x)
                {
                    // the functionality is based on the internal implementation of
                    // isSingleString and ToString() are implemented 
                    if (x.isSingleString)
                        ft.usedParams[x.ToString()] = true;
                }
                /*
                Console.Write(idx);
                Console.Write("(");
                Console.Write(fc.arguments[i].GetType().Name);
                Console.Write(")");
                Console.Write(": ");
                Console.WriteLine(fc.arguments[i].ToString());
                */
            }
        }

        static void SearchForFunc(Ink.Parsed.Object obj)
        {
            if (obj == null) return;


            if (obj is FunctionCall fc)
            {
                bool gather = true;
                if ((onlyInputFile)&&(obj.debugMetadata != null))
                {
                    gather = string.Compare(obj.debugMetadata.fileName, inputFn, true)==0;
                    Console.WriteLine("gather={0} {1} {2}", gather, obj.debugMetadata.fileName, inputFn);
                }

                if (gather)
                {
                    //Console.Write(fc.name);
                    usedFuncs[fc.name] = true;
                    FuncTrack ft;
                    if (funcs.TryGetValue(fc.name, out ft))
                    {
                        GatherParams(ft, fc);
                        //Console.WriteLine("... gathered.");
                    }
                }
            }

            if (obj.content != null)
            {
                foreach (Ink.Parsed.Object sub in obj.content)
                    SearchForFunc(sub);
            }

        }

        static List<string> CollectFuncParams()
        {
            Dictionary<string, bool> res = new Dictionary<string, bool>();
            foreach(FuncTrack ft in funcs.Values)
            {
                foreach (string p in ft.usedParams.Keys)
                    res[p] = true;
            }

            List<string> results = new List<string>();
            results.AddRange(res.Keys);
            return results;
        }


        static bool onlyWalk = false;
        static bool showWhiteSpace = false;
        static string inputFn = "";
        static bool onlyInputFile = false;
        static List<string> PrefixRemove = new List<string>();
        static Dictionary<string, FuncTrack> funcs = new Dictionary<string, FuncTrack>();
        static string outputFn = "";
        static bool showHelp;


        static Dictionary<string, bool> usedFuncs = new Dictionary<string, bool>();

        static bool IsParamsNum(string inp, List<int> dst)
        {
            if (string.IsNullOrEmpty(inp)) return false;
            string[] num = inp.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
            int[] idx = new int[num.Length];
            for (int i = 0; i < num.Length; i++)
            {
                string n = num[i].Trim();
                int pidx;
                if (!int.TryParse(n, out pidx))
                    return false;
                idx[i] = pidx;
            }
            dst.AddRange(idx);
            return true;
        }

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
                    case "-o":
                    case "--output":
                        i++;
                        if (i < args.Length) outputFn = args[i];
                        break;
                    case "-h":
                    case "--help":
                        showHelp = true;
                        break;
                    case "-f":
                    case "-fn":
                    case "--fn":
                    case "--func":
                        i++;
                        string fn = "";
                        if (i >= args.Length) break;

                        fn = args[i].Trim();
                        if (string.IsNullOrEmpty(fn))
                        {
                            i--;
                            break;
                        }
                        if (fn.IndexOf("@") < 0)
                        {
                            FuncTrack ft = new FuncTrack();
                            ft.ParseNameArgs(fn);
                            funcs[ft.funcName] = ft;
                        } 
                        else
                        {
                            string fln = fn.Substring(1);
                            string[] fnNames = File.ReadAllLines(fln, Encoding.UTF8);
                            foreach (string funName in fnNames)
                            {
                                if (string.IsNullOrWhiteSpace(funName))
                                    continue;
                                FuncTrack ft = new FuncTrack();
                                if (ft.ParseNameArgs(funName))
                                    funcs[ft.funcName] = ft;
                            }

                        }
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
@" useofink [options] %inputFile.ink%

%inputFile.ink% - the input .ink file

options:

  --fn         - name of the function to track the parameters of

  --walk       - don't try to produce result file, only produce the parsed structure
                 to stdout. (output file is not used)

  -o, --output - the output file name. File is written in UTF8 encoding
                 if not specified, the file is written to stdout
  
  --onlyinput  - only gather strings from the %inputFile.ink%
                 this is used for multi-file scenarios (where INPUT is used)
  
  
  -h, --help   - show this text
");
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
                SearchForFunc(flow);
                List<string> vals = CollectFuncParams();
                if (string.IsNullOrEmpty(outputFn))
                {
                    foreach (string s in vals)
                        Console.WriteLine(s);
                }
                else
                {
                    File.WriteAllLines(outputFn, vals.ToArray());
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
