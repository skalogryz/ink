using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace gettenk
{
    public static class i18utils
    {

        public static bool IsEscapeChar(char c)
        {
            switch (c)
            {
                case '\\':
                case '\"':
                case '\r':
                case '\t':
                case '\n':
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsWhiteSpace(char c)
        {
            switch (c)
            {
                case ' ':
                case '\r':
                case '\t':
                case '\n':
                    return true;
                default:
                    return false;
            }
        }
        public static bool NeedsEscape(string s)
        {
            int l = s.Length;
            for (int i = 0; i < l; i++)
                if (IsEscapeChar(s[i]))
                    return true;
            return false;
        }
        public static string EscapeForPot(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) 
                return s;
            if (NeedsEscape(s))
            {
                return
                    s.Replace("\\", "\\\\", StringComparison.InvariantCulture)
                    .Replace("\"", "\\\"", StringComparison.InvariantCulture)
                    .Replace("\r", "\\r", StringComparison.InvariantCulture)
                    .Replace("\t", "\\t", StringComparison.InvariantCulture)
                    .Replace("\n", "\\n", StringComparison.InvariantCulture);
            }
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
                    while ((j >= 0) && (!IsWhiteSpace(sub[j])))
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

        public static string EstimateMsgId(this LocalizedLine l, bool useLangTag = true)
        {
            if (l == null) return "";
            if ((useLangTag) && (!string.IsNullOrEmpty(l.langTag)))
                return l.langTag;
            else
                return l.text.Trim();
        }

        public static string ToPotString(this LocalizedLine l, bool useLangTag = true)
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
                b.AppendLine();
            }

            if (!string.IsNullOrEmpty(l.extraInfo))
            {
                string[] el = l.extraInfo.Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
                foreach (string eline in el)
                {
                    if (l.isAnswerOption)
                        b.AppendLine("#. (answer option)");
                    b.Append("#. ");
                    b.Append(eline);
                    b.AppendLine();
                }
            }

            string txt = l.text.Trim();
            string tag = l.langTag;
            string msgid;
            if ((useLangTag) && (!string.IsNullOrEmpty(tag)))
            {
                msgid = tag;
                List <string> cmt = BreakUpLines(txt);
                b.AppendLine("#. original text:");
                foreach (string s in cmt)
                {
                    b.Append("#. ");
                    b.Append(s);
                    b.AppendLine();
                }
            }
            else
                msgid = txt;

            if (msgid.Length <= LineLen)
            {
                b.Append("msgid \"");
                b.Append(EscapeForPot(msgid));
                b.Append("\"");
                b.AppendLine();
            }
            else
            {
                b.AppendLine("msgid \"\"");
                List<string> list = BreakUpLines(msgid);
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
