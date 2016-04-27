using System;
using System.IO;
using System.Text;

namespace SpssLib.FileParser.Records
{
    public class MachineIntegerInfoRecord : BaseInfoRecord
    {
        public override int SubType => InfoRecordType.MachineInteger;

        public int VersionMajor { get; private set; }
		public int VersionMinor { get; private set; }
		public int VersionRevision { get; private set; }
		public int MachineCode { get; private set; }
		public int FloatingPointRepresentation { get; private set; }
		public int CompressionCode { get; private set; }
		public int Endianness { get; private set; }
		public int CharacterCode { get; private set; }

		public MachineIntegerInfoRecord()
		{}

		public MachineIntegerInfoRecord(Encoding encoding)
		{
            ItemSize = 4;
            ItemCount = 8;

			var assemblyName = GetType().Assembly.GetName();

			VersionMajor = assemblyName.Version.Major;
			VersionMinor = assemblyName.Version.Minor;
			VersionRevision = assemblyName.Version.Revision;
			MachineCode = -1;
			FloatingPointRepresentation = 1;	// IEEE 754
			CompressionCode = 1;
			Endianness = 2;						// Little endian
			CharacterCode = GetCharacterCode(encoding);
		}

        private int GetCharacterCode(Encoding encoding)
        {
            if (encoding.EncodingName.Equals("US-ASCII", StringComparison.InvariantCultureIgnoreCase))
            {
                return 2;
            }
            return encoding.CodePage;
        }

        public new Encoding Encoding
        {
            get
            {
                switch (CharacterCode)
                {
                    case 1: // EBCDIC
                        throw new NotSupportedException("EBCDIC???? Who uses that? Honestly!!");
                    case 2: // 7-bit ASCII
                        return Encoding.ASCII;
                    case 3: // 8-bit ASCII
                        return Encoding.UTF8;
                    case 4: // DEC Kanji
                        throw new NotSupportedException("SPSS machine info record character code not suported: 4 - DEC Kanji. What year is this?? Where am I?");
                    default: // Other, look by codepage
                        try
                        {
                            return Encoding.GetEncoding(CharacterCode);
                        }
                        catch (ArgumentException ex)
                        {
                            throw new NotSupportedException("SPSS machine info record character code not suported: " + CharacterCode, ex);
                        }
                }
            }
        }

        public override void RegisterMetadata(MetaData metaData)
        {
            metaData.MachineIntegerInfo = this;
            metaData.HeaderEncoding = Encoding;
        }

        protected override void WriteInfo(BinaryWriter writer)
		{
			writer.Write(VersionMajor);
			writer.Write(VersionMinor);
			writer.Write(VersionRevision);
			writer.Write(MachineCode);
			writer.Write(FloatingPointRepresentation);
			writer.Write(CompressionCode);
			writer.Write(Endianness);
			writer.Write(CharacterCode);
		}

        protected override void FillInfo(BinaryReader reader)
        {
            // Must have 8 int of 32 bits
            CheckInfoHeader(itemSize:4, itemCount:8);

            VersionMajor = reader.ReadInt32();
            VersionMinor = reader.ReadInt32();
            VersionRevision = reader.ReadInt32();
            MachineCode = reader.ReadInt32();
            FloatingPointRepresentation = reader.ReadInt32();
            CompressionCode = reader.ReadInt32();
            Endianness = reader.ReadInt32();
            CharacterCode = reader.ReadInt32(); 
        }
    }
}
