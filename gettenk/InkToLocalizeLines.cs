using System;
using System.Collections.Generic;
using System.Text;
using Ink;
using Ink.Parsed;

namespace gettenk
{
    public class LocalizedLine
    {
        public string text;
        public string filename;
        public int line;
        public List<Ink.Parsed.Object> objects = new List<Ink.Parsed.Object>();
    }

    public class InkToLocalizeLines
    {
        public List<LocalizedLine> lines = new List<LocalizedLine>();

        private LocalizedLine lastLine;

        private void GatherSimple(Ink.Parsed.Object obj)
        {
            if (obj == null) return;
            if ((obj is Text t) && (!string.IsNullOrWhiteSpace(t.text)))
            {
                LocalizedLine ll = new LocalizedLine();
                lines.Add(ll);
                lastLine = ll;

                if (t.hasOwnDebugMetadata)
                {
                    ll.filename = t.debugMetadata.fileName;
                    ll.line = t.debugMetadata.startLineNumber;
                }
                ll.text = t.text;
                ll.objects.Add(t);
            }
            if (obj.content != null)
            {
                foreach (Ink.Parsed.Object sub in obj.content)
                    GatherSimple(sub);
            }
        }

        public void GatherLines(Story parsedStory)
        {
            GatherSimple(parsedStory);
        }
    }
}
