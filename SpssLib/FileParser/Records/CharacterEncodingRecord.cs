using System.IO;
using System.Text;

namespace SpssLib.FileParser.Records
{
    public class CharacterEncodingRecord : BaseInfoRecord
    {
        public override int SubType { get { return InfoRecordType.CharacterEncoding; } }

		protected string Name { get; set; }

        internal CharacterEncodingRecord()
        {}

		internal CharacterEncodingRecord(Encoding encoding)
		{
		    ItemSize = 1;
            // Supposedly has to be the IANA name
			Name = encoding.WebName;
		    ItemCount = Name.Length;
		}

        protected override void WriteInfo(BinaryWriter writer)
		{
            var bytes = Encoding.ASCII.GetBytes(Name);
			writer.Write(bytes);
		}

        protected override void FillInfo(BinaryReader reader)
        {
            CheckInfoHeader(1); // items must be of size 1 (byte)

            // TODO test if ReadString will work
            var nameBytes = reader.ReadBytes(ItemCount);
            Name = Encoding.ASCII.GetString(nameBytes);
        }
	}
}