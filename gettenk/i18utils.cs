using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace gettenk
{
    public static class i18utils
    {

        public static string EscapeForPot(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) 
                return s;
            int i = s.IndexOf("\"");
            if (i >= 0)
                return s.Replace("\"", "\\\"", StringComparison.InvariantCultureIgnoreCase);
            else
                return s;
        }

        public static List<string> BreakUpLines(string text, int maxLen = LineLen)
        {
            List<string> result = new List<string>();
            if (string.IsNullOrWhiteSpace(text))
            {
                result.Add(text);
                return result;
            }
            int i = 0;
            while (i < text.Length)
            {
                string sub = text.Substring(i, Math.Min(text.Length - i, maxLen));
                if (sub.Length == maxLen)
                {
                    int j = sub.Length - 1;
                    while ((j >= 0) && (sub[j] != ' ') && (sub[j] != '\t') && (sub[j] != '\r') && (sub[j] != '\n'))
                        j--;
                    if (j >= 0) 
                        sub = sub.Substring(0, j+1);
                }
                result.Add(sub);
                i += sub.Length;
            }
            return result;
        }

        public const int LineLen = 77;
        public static string ToPotString(this LocalizedLine l)
        {
            if (l == null) return "";

            /*  Pot sample:
             *  
                #: ..\Storage\Dev\GitHub\Other\NGettext\examples\Examples.HelloForms\Form1.cs:
                #, csharp-format
                msgid "{0} (non contextual)"
                msgstr ""                 
             */

            StringBuilder b = new StringBuilder();

            if (!string.IsNullOrEmpty(l.filename))
            {
                b.Append("#: ");
                b.Append(l.filename);
                b.Append(": ");
                b.Append(l.line.ToString());
                if (l.isAnswerOption)
                    b.Append(" (answer option)");
                b.AppendLine();
            }

            if (!string.IsNullOrEmpty(l.extraInfo))
            {
                string[] el = l.extraInfo.Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
                foreach (string eline in el)
                {
                    b.Append("#. ");
                    b.Append(eline);
                    b.AppendLine();
                }
            }

            if (l.text.Length <= LineLen)
            {
                b.Append("msgid \"");
                //75
                b.Append(EscapeForPot(l.text));
                b.Append("\"");
                b.AppendLine();
            }
            else
            {
                b.AppendLine("msgid \"\"");
                List<string> list = BreakUpLines(l.text);
                foreach(string sub in list)
                {
                    b.Append("\"");
                    b.Append(EscapeForPot(sub));
                    b.AppendLine("\"");
                }
            }
            b.AppendLine("msgstr \"\"");
            return b.ToString();
        }
    }
}
