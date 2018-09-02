using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using HtmlAgilityPack;

namespace WIMCore
{
    public class Chapter
    {
        public string Title { get; private set; }
        public string Author { get; private set; }
        public byte ChoiceNum { get; private set; }
        public ushort ParentChapter { get; set; }
        public string Text { get; private set; }
        public List<ushort> ChildChapters { get; private set; }
        public List<string> ChoiceDescriptions { get; private set; }

        private Chapter()
        {
            Title = "";
            Author = "";
            ChoiceNum = 0xFF;
            ParentChapter = 0xFFFF;
            Text = "";
            ChildChapters = new List<ushort>();
            ChoiceDescriptions = new List<string>();
        }

        public Chapter(string title, byte choiceNum) : this()
        {
            Title = title;
            ChoiceNum = choiceNum;
        }

        public static Chapter LoadLocal(Stream stream)
        {
            Chapter chapter = new Chapter();
            BinaryReader binReader = new BinaryReader(stream);
            chapter.Title = binReader.ReadString();
            chapter.Author = binReader.ReadString();
            chapter.ChoiceNum = binReader.ReadByte();
            chapter.ParentChapter = binReader.ReadUInt16();
            chapter.Text = binReader.ReadString();
            byte count = binReader.ReadByte();
            for (byte b = 0; b < count; b++)
                chapter.ChildChapters.Add(binReader.ReadUInt16());
            count = binReader.ReadByte();
            for (byte b = 0; b < count; b++)
                chapter.ChoiceDescriptions.Add(binReader.ReadString());
            return chapter;
        }

        public void Write(Stream stream)
        {
            BinaryWriter binWriter = new BinaryWriter(stream);
            binWriter.Write(Title);
            binWriter.Write(Author);
            binWriter.Write(ChoiceNum);
            binWriter.Write(ParentChapter);
            binWriter.Write(Text);
            binWriter.Write((byte)ChildChapters.Count);
            foreach (ushort c in ChildChapters)
                binWriter.Write(c);
            binWriter.Write((byte)ChoiceDescriptions.Count);
            foreach (string s in ChoiceDescriptions)
                binWriter.Write(s);
        }

        public void Update(Chapter updatedChapter)
        {
            if (updatedChapter.Title != "")
                Title = updatedChapter.Title;
            if (updatedChapter.Author != "")
                Author = updatedChapter.Author;
            if (updatedChapter.ChoiceNum != 0xFF)
                ChoiceNum = updatedChapter.ChoiceNum;
            if (updatedChapter.ParentChapter != 0xFFFF)
                ParentChapter = updatedChapter.ParentChapter;
            if (updatedChapter.Text != "")
                Text = updatedChapter.Text;
            while (ChildChapters.Count < updatedChapter.ChildChapters.Count)
                ChildChapters.Add(0xFFFF);
            for(int i = 0; i < updatedChapter.ChildChapters.Count; i++)
            {
                if (updatedChapter.ChildChapters[i] != 0xFFFF)
                    ChildChapters[i] = updatedChapter.ChildChapters[i];
            }
            while (ChoiceDescriptions.Count < updatedChapter.ChoiceDescriptions.Count)
                ChoiceDescriptions.Add("");
            for(int i = 0; i < updatedChapter.ChoiceDescriptions.Count; i++)
            {
                if (updatedChapter.ChoiceDescriptions[i] != "")
                    ChoiceDescriptions[i] = updatedChapter.ChoiceDescriptions[i];
            }
        }

        public bool HasChild(byte choice)
        {
            if (choice >= ChildChapters.Count)
                return false;
            return ChildChapters[choice] != 0xFFFF;
        }

        public void AddChild(ushort chapterIndex, byte choice)
        {
            while (ChildChapters.Count <= choice)
                ChildChapters.Add(0xFFFF);
            ChildChapters[choice] = chapterIndex;
        }

        public async Task DownloadData(string chapterUrl)
        {
            HtmlDocument chapterDoc = await WebUtilities.GetHtmlDocumentAsync(chapterUrl);
            HtmlNode pageTitleNode = chapterDoc.DocumentNode.SelectSingleNode(ParseParams.PageTitleXPath);
            bool gotBusyPage = !pageTitleNode.InnerText.Contains(Title);
            while (gotBusyPage)
            {
                await Task.Delay(2000);
                chapterDoc = await WebUtilities.GetHtmlDocumentAsync(chapterUrl);
                pageTitleNode = chapterDoc.DocumentNode.SelectSingleNode(ParseParams.PageTitleXPath);
                gotBusyPage = !pageTitleNode.InnerText.Contains(Title);
            }
            Author = WebUtilities.GetHtmlNodeText(chapterDoc, ParseParams.ChAuthorXPath);
            Text = WebUtilities.GetHtmlNodeText(chapterDoc, ParseParams.ChTextXPath);
            HtmlNodeCollection cdns = chapterDoc.DocumentNode.SelectNodes(ParseParams.ChChoiceDescrptionsXPath);
            if(cdns != null)
            {
                List<HtmlNode> choiceDescrptionNodes = new List<HtmlNode>(cdns);
                for (int i = 0; i < choiceDescrptionNodes.Count; i++)
                {
                    HtmlNode choiceNumNode = choiceDescrptionNodes[i].SelectSingleNode("b");
                    string choiceNumText = choiceNumNode.InnerText;
                    int choice = int.Parse(choiceNumText.Substring(0, choiceNumText.IndexOf('.')));
                    HtmlNode descriptionNode = choiceDescrptionNodes[i].SelectSingleNode("a");
                    string description = WebUtilities.CleanHtmlSymbols(descriptionNode.InnerText);
                    while (ChoiceDescriptions.Count <= choice)
                        ChoiceDescriptions.Add("");
                    ChoiceDescriptions[choice] = description;
                }
            }
            
        }
    }
}
