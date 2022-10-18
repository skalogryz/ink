using System;
using System.Collections.Generic;
using System.Text;
using Ink;
using Ink.Parsed;

namespace gettenk
{
    public class LocalizedLine
    {
        public string line;
        public List<Ink.Parsed.Object> objects = new List<Ink.Parsed.Object>();
    }

    public class InkToLocalizeLines
    {
        List<LocalizedLine> lines = new List<LocalizedLine>();

        private void GatherSimple(Object obj)
        {
            if (obj == null) return;
            if ((obj is Text t) && (!string.IsNullOrWhiteSpace(t.text)))
            {
                if (t.hasOwnDebugMetadata)
                {
                }
            }
            if (obj.content != null)
            {
                foreach (Ink.Parsed.Object sub in obj.content)
                    WalkObj(sub, pfx + "  ");
            }
        }

        public void GatherLines(Story parsedStory)
        {
            GatherSimple(parsedStory);
        }
    }
}
