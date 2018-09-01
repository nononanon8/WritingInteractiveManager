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
        private const string BaseUrl = "https://www.writing.com/main/interact/item_id";

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
            HtmlDocument htmlDoc = await WebUtilities.GetHtmlDocumentAsync(BaseUrl + '/' + itemId);
            //HtmlNode pageTitleNode = WebUtilities.GetHtmlNodeByTag(htmlDoc.DocumentNode, "title");
            HtmlNode pageTitleNode = WebUtilities.GetHtmlPageTitleNode(htmlDoc);
            if (pageTitleNode.InnerText.Contains("Item Not Found"))
                throw new Exception("Story not found");
            HtmlNode titleNode = WebUtilities.GetHtmlNodeByClass(htmlDoc.DocumentNode, "proll");
            story.Title = WebUtilities.CleanHtmlSymbols(titleNode.InnerText);
            HtmlNode ownerNode = WebUtilities.GetHtmlNodeByAttributePartial(htmlDoc.DocumentNode, "title", "Username:");
            story.Owner = ownerNode.InnerText;
            HtmlNode descriptionNode = WebUtilities.GetHtmlNodeByAttribute(htmlDoc.DocumentNode, "NAME", "description");
            story.Description = WebUtilities.CleanHtmlSymbols(descriptionNode.Attributes["content"].Value);
            HtmlNode infoTextNode = WebUtilities.GetHtmlNodeByTag(htmlDoc.DocumentNode, "td");
            story.InfoText = infoTextNode.InnerText;
            // RootChapters
            // Chapters

            return story;
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
