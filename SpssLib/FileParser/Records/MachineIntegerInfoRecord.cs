using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace SpssLib.FileParser.Records
{
	public class MachineIntegerInfoRecord : IBaseRecord
    {
		public int RecordType { get { return 7; } }
		
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
			var assemblyName = GetType().Assembly.GetName();

			VersionMajor = assemblyName.Version.Major;
			VersionMinor = assemblyName.Version.Minor;
			VersionRevision = assemblyName.Version.Revision;
			MachineCode = -1;
			FloatingPointRepresentation = 1;	// IEEE 754
			CompressionCode = 1;
			Endianness = 2;						// Little endian
			CharacterCode = encoding.CodePage;
		}


		public void WriteRecord(BinaryWriter writer)
		{
			writer.Write(RecordType);
			writer.Write(3);	// Subtype
			writer.Write(4);	// Item size (int 32 - 4 bytes)
			writer.Write(8);	// 8 items

			writer.Write(VersionMajor);
			writer.Write(VersionMinor);
			writer.Write(VersionRevision);
			writer.Write(MachineCode);
			writer.Write(FloatingPointRepresentation);
			writer.Write(CompressionCode);
			writer.Write(Endianness);
			writer.Write(CharacterCode);
		}
        
        internal MachineIntegerInfoRecord(InfoRecord record)
        {
            if (record.SubType != 3 || record.ItemSize != 4 || record.ItemCount != 8)
                throw new UnexpectedFileFormatException();

	        VersionMajor =					BitConverter.ToInt32(record.Items[0], 0);
			VersionMinor =					BitConverter.ToInt32(record.Items[1], 0);
			VersionRevision =				BitConverter.ToInt32(record.Items[2], 0);
			MachineCode =					BitConverter.ToInt32(record.Items[3], 0);
			FloatingPointRepresentation =	BitConverter.ToInt32(record.Items[4], 0);
			CompressionCode =				BitConverter.ToInt32(record.Items[5], 0);
			Endianness =					BitConverter.ToInt32(record.Items[6], 0);
			CharacterCode =					BitConverter.ToInt32(record.Items[7], 0);
        }
    }
}
