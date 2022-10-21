using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gettenk
{
    public static class i18utils
    {
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
                string[] el = l.extraInfo.Split(new string[] { "\r\n","\r","\n"}, StringSplitOptions.None );
                foreach (string eline in el)
                {
                    b.Append("#. ");
                    b.Append(eline);
                    b.AppendLine();
                }
            }

            b.Append("msgid \"");
            b.Append(l.text);
            b.Append("\"");
            b.AppendLine();
            b.AppendLine("msgstr \"\"");
            return b.ToString();
        }
    }
}
