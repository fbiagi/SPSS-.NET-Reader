using System.IO;

namespace SpssLib.FileParser.Records
{
	/// <summary>
	/// Record type to signal the start of the data records
	/// it has two bytes the record type and a filler
	/// </summary>
	class DictionaryTerminationRecord : IRecord
	{
		public RecordType RecordType { get { return RecordType.End; } }

		public void WriteRecord(BinaryWriter writer)
		{
			writer.Write((int)RecordType);
            // write filler
			writer.Write(0); 
		}

	    public void FillRecord(BinaryReader reader)
	    {
            // skip filler
	        reader.ReadInt32();
	    }
	}
}
