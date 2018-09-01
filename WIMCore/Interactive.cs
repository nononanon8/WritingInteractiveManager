using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using HtmlAgilityPack;
using System.IO;

namespace WIMCore
{
    public class Interactive
    {
        private const string BaseUrl = "https://www.writing.com/main/interact/item_id/";
        private const string OutlineUrlSeg = "/action/pop_outline";

        public uint ItemId { get; private set; }
        public string Title { get; private set; }
        public string Owner { get; private set; }
        public string Description { get; private set; }
        public string InfoText { get; private set; }
        public List<ushort> RootChapters { get; private set; }
        public List<Chapter> Chapters { get; private set; }

        private Interactive()
        {
            ItemId = 0;
            Title = "";
            Owner = "";
            Description = "";
            InfoText = "";
            RootChapters = new List<ushort>();
            Chapters = new List<Chapter>();
        }

        private Interactive(uint itemId) : this()
        {
            ItemId = itemId;
        }

        public static async Task<Interactive> LoadWebSkeleton(uint itemId)
        {
            Interactive story = new Interactive(itemId);
            // Get HTML document for main story page.
            HtmlDocument htmlDoc = await WebUtilities.GetHtmlDocumentAsync(BaseUrl + itemId);
            // Start downloading outline page.
            Task<HtmlDocument> outlineDocTask = WebUtilities.GetHtmlDocumentAsync(BaseUrl + itemId + OutlineUrlSeg);
            // Check page title node to see if a valid ID was given.
            HtmlNode pageTitleNode = WebUtilities.GetHtmlPageTitleNode(htmlDoc);
            if (pageTitleNode.InnerText.Contains("Item Not Found"))
                throw new Exception("Story not found");

            // Find the HTML node with the text for each field we need, and assign the field value.
            HtmlNode titleNode = WebUtilities.GetHtmlNodeByClass(htmlDoc.DocumentNode, "proll");
            story.Title = WebUtilities.CleanHtmlSymbols(titleNode.InnerText);
            HtmlNode ownerNode = WebUtilities.GetHtmlNodeByAttributePartial(htmlDoc.DocumentNode, "title", "Username:");
            story.Owner = WebUtilities.CleanHtmlSymbols(ownerNode.InnerText);
            HtmlNode descriptionNode = WebUtilities.GetHtmlNodeByAttribute(htmlDoc.DocumentNode, "NAME", "description");
            story.Description = WebUtilities.CleanHtmlSymbols(descriptionNode.Attributes["content"].Value);
            HtmlNode infoTextNode = WebUtilities.GetHtmlNodeByTag(htmlDoc.DocumentNode, "td");
            story.InfoText = WebUtilities.CleanHtmlSymbols(infoTextNode.InnerText);

            // Finish getting the outline HTML document.
            HtmlDocument outlineDoc = await outlineDocTask;
            // Parse out chapter names and map structure.
            HtmlNode outlineParentNode = WebUtilities.GetHtmlNodeByTag(outlineDoc.DocumentNode, "pre");
            List<HtmlNode> outlineHtmlNodes = new List<HtmlNode>(outlineParentNode.ChildNodes);
            int nodeIndex = 0;
            HtmlNode spanNode = null;
            HtmlNode bNode = null;
            while (nodeIndex < outlineHtmlNodes.Count)
            {
                // span element contains choice path string (1-2-1-1-3-...).
                if (outlineHtmlNodes[nodeIndex].Name.Equals("span", StringComparison.OrdinalIgnoreCase))
                {
                    spanNode = outlineHtmlNodes[nodeIndex];
                    // span element must come first.
                    bNode = null;
                }
                // b element contains chapter name.
                else if (outlineHtmlNodes[nodeIndex].Name.Equals("b", StringComparison.OrdinalIgnoreCase))
                    bNode = outlineHtmlNodes[nodeIndex];

                if(spanNode != null && bNode != null)
                {
                    // Get choice path as array of ints.
                    string choicePathStr = spanNode.InnerText;
                    // Remove junk at end.
                    choicePathStr = choicePathStr.Substring(0, choicePathStr.Length - 7);
                    // Convert to int.
                    List<byte> choices = new List<byte>();
                    foreach (string s in choicePathStr.Split('-'))
                        choices.Add(byte.Parse(s));

                    string chapterName = WebUtilities.CleanHtmlSymbols(bNode.InnerText);
                    // Remove "#_: " from start of name.
                    chapterName = chapterName.Substring(chapterName.IndexOf(' ') + 1);
                    Chapter chapter = new Chapter(chapterName, choices[choices.Count - 1]);
                    story.AddChapter(chapter, choices);
                    spanNode = null;
                    bNode = null;
                }
                nodeIndex++;
            }

            return story;
        }

        public List<byte> GetChoicePath(ushort chapterIndex)
        {
            List<byte> choices = new List<byte>();
            
            while(chapterIndex != 0xFFFF)
            {
                choices.Add(Chapters[chapterIndex].ChoiceNum);
                chapterIndex = Chapters[chapterIndex].ParentChapter;
            }
            choices.Reverse();
            return choices;
        }

        public List<byte> GetChoicePath(Chapter chapter)
        {
            List<byte> choices = GetChoicePath(chapter.ParentChapter);
            choices.Add(chapter.ChoiceNum);
            return choices;
        }

        // Add and link chapter, or update if chapter already exists.
        private void AddChapter(Chapter chapter, List<byte> choicePath)
        {
            // Handle root chapter as a special case.
            if(choicePath.Count == 1)
            {
                // Make sure root choice is a valid root chapter index.
                while (RootChapters.Count <= choicePath[0])
                    RootChapters.Add(0xFFFF);
                // If the root choice chapter doesn't exist, add it.
                if (RootChapters[choicePath[0]] == 0xFFFF)
                {
                    RootChapters[choicePath[0]] = (ushort)Chapters.Count;
                    Chapters.Add(chapter);
                }
                // If the chapter already exists, update it.
                else
                    Chapters[RootChapters[choicePath[0]]].Update(chapter);
            }
            // If this is not a root chapter...
            else
            {
                // Get immediate parent.
                ushort parentIndex = RootChapters[choicePath[0]];
                for (int i = 1; i < choicePath.Count - 1; i++)
                    parentIndex = Chapters[parentIndex].ChildChapters[choicePath[i]];
                // Add or update chapter.
                byte finalChoice = choicePath[choicePath.Count - 1];
                if (Chapters[parentIndex].HasChild(finalChoice))
                    Chapters[Chapters[parentIndex].ChildChapters[finalChoice]].Update(chapter);
                else
                {
                    Chapters[parentIndex].AddChild((ushort)Chapters.Count, finalChoice);
                    Chapters.Add(chapter);
                    chapter.ParentChapter = parentIndex;
                }
            }
        }

        public int GetUsedRootChapterCount()
        {
            int count = 0;
            foreach(ushort rc in RootChapters)
            {
                if (rc != 0xFFFF)
                    count++;
            }
            return count;
        }

        public static Interactive LoadLocal(Stream stream)
        {
            Interactive story = new Interactive();
            BinaryReader binReader = new BinaryReader(stream);
            story.ItemId = binReader.ReadUInt32();
            story.Title = binReader.ReadString();
            story.Owner = binReader.ReadString();
            story.Description = binReader.ReadString();
            story.InfoText = binReader.ReadString();
            ushort count = binReader.ReadByte();
            for (ushort us = 0; us < count; us++)
                story.RootChapters.Add(binReader.ReadUInt16());
            count = binReader.ReadUInt16();
            for (ushort us = 0; us < count; us++)
                story.Chapters.Add(Chapter.LoadLocal(stream));
            return story;
        }

        public void Write(Stream stream)
        {
            BinaryWriter binWriter = new BinaryWriter(stream);
            binWriter.Write(ItemId);
            binWriter.Write(Title);
            binWriter.Write(Owner);
            binWriter.Write(Description);
            binWriter.Write(InfoText);
            binWriter.Write((byte)RootChapters.Count);   
            foreach (ushort rc in RootChapters)
                binWriter.Write(rc);
            binWriter.Write((ushort)Chapters.Count);
            foreach (Chapter c in Chapters)
                c.Write(stream);
        }

        public override string ToString()
        {
            string str = "";
            str += "Item ID: " + ItemId + Environment.NewLine;
            str += "Title: " + Title + Environment.NewLine;
            str += "Owner: " + Owner + Environment.NewLine;
            str += "Descrption: " + Description + Environment.NewLine;
            str += "Chapter Count: " + Chapters.Count;
            return str;
        }
    }
}
