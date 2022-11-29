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
        public bool isAnswerOption;
        public string extraInfo;
    }

    public class InkToLocalizeLines
    {
        public List<LocalizedLine> lines = new List<LocalizedLine>();

        private LocalizedLine lastLine;
        private bool hasGlue;
        private bool glueAll; // is used for answers

        private string startFile = "";
        public bool OnlyStartFile = false;
        public bool GoExpressions = true;
        public bool SkipTags = true;

        private bool InTag = false;

        private void GatherSimple(List<Ink.Parsed.Object> content)
        {
            if (content == null) return;
            for (int i = 0; i < content.Count; i++)
                GatherSimple(content[i]);
        }

        private void GatherSimple(Ink.Parsed.Object obj)
        {
            if (obj == null) return;

            if ((obj is Expression) && (!GoExpressions))
                return;

            Tag tg = (obj as Tag);
            if (tg != null)
            {
                if (tg.isStart)
                    InTag = true;
                else
                    InTag = false;
                return;
            }

            if (InTag && SkipTags) 
                return;

            if ((OnlyStartFile)
                && (obj.debugMetadata != null)
                && (startFile != null)
                && (startFile != obj.debugMetadata.fileName))
            {
                return;
            }

            bool goToContent = true;

            if ((obj is Text t))
            {
                if (string.IsNullOrWhiteSpace(t.text)) return;

                LocalizedLine ll;
                if ((hasGlue) && (lastLine != null))
                {
                    ll = lastLine;
                    if (!glueAll) hasGlue = false;
                }
                else
                {
                    ll = new LocalizedLine();
                    lines.Add(ll);
                    if (t.hasOwnDebugMetadata)
                    {
                        ll.filename = t.debugMetadata.fileName;
                        ll.line = t.debugMetadata.startLineNumber;
                    }
                }
                lastLine = ll;

                if (ll.text == "") ll.text = t.text;
                else ll.text += t.text;
                ll.objects.Add(t);
            } else if (obj is Choice ch)
            {
                goToContent = false;

                lastLine = null;
                if (ch.choiceOnlyContent != null)
                {
                    int i = lines.Count;
                    GatherSimple(ch.choiceOnlyContent);
                    for (int j = i; j < lines.Count; j++)
                        lines[j].isAnswerOption = true;
                    lastLine = null;
                }
                if (ch.startContent != null)
                {
                    bool tGlue = hasGlue;

                    // the entire text line
                    hasGlue = true;
                    glueAll = true;
                    try
                    {
                        if (ch.content != null) GatherSimple(ch.content);
                    }
                    finally
                    {
                        glueAll = false;
                        hasGlue = tGlue;
                    }
                }
            }
            else if (obj is Glue)
                hasGlue = true;

            if (goToContent)
                GatherSimple(obj.content);
        }

        public void GatherLines(Story parsedStory, string explicitStartFile)
        {
            if (parsedStory == null) return;
            startFile = explicitStartFile;
            if ((parsedStory.debugMetadata != null)&&(string.IsNullOrEmpty(startFile)))
            {
                startFile = parsedStory.debugMetadata.fileName;
            }
            GatherSimple(parsedStory);
        }
    }
}
