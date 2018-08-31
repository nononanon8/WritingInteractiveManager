using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace WIMCore
{
    public class Chapter
    {
        public string Title { get; private set; }
        public string Author { get; private set; }
        public byte ChoiceNum { get; private set; }
        public ushort ParentChapter { get; private set; }
        public string Text { get; private set; }
        public List<ushort> ChildChapters { get; private set; }
        public Dictionary<byte, string> ChoiceDescriptions { get; private set; }

        private Chapter()
        {
            Title = "";
            Author = "";
            ChoiceNum = 0;
            ParentChapter = 0xFFFF;
            Text = "";
            ChildChapters = new List<ushort>();
            ChoiceDescriptions = new Dictionary<byte, string>();
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
            {
                byte key = binReader.ReadByte();
                string val = binReader.ReadString();
                if (chapter.ChoiceDescriptions.ContainsKey(key))
                    chapter.ChoiceDescriptions[key] = val;
                else
                    chapter.ChoiceDescriptions.Add(key, val);
            }
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
            binWriter.Write((byte)ChoiceDescriptions.Keys.Count);
            foreach (byte k in ChoiceDescriptions.Keys)
            {
                binWriter.Write(k);
                binWriter.Write(ChoiceDescriptions[k]);
            }
        }
    }
}
