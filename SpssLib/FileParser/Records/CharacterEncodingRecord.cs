using System;
using System.IO;
using System.Text;

namespace SpssLib.FileParser.Records
{
	public class CharacterEncodingRecord : IBaseRecord
	{
		public int RecordType {
			get { return 7; }
		}

		protected string Name { get; set; }

		public CharacterEncodingRecord(Encoding encoding)
		{
			// Supposedly has to be the IANA name
			Name = encoding.WebName;
		}

		public void WriteRecord(BinaryWriter writer)
		{
			writer.Write(RecordType);
			writer.Write(20);	// subtype
			writer.Write(1);	// lenght

			var bytes = Encoding.ASCII.GetBytes(Name.ToUpper());
			writer.Write(bytes.Length);
			writer.Write(bytes);
		}
	}
}