using System.IO;

namespace SpssLib.FileParser.Records
{
	/// <summary>
	/// Record type to signal the start of the data records
	/// it has two bytes the record type and a filler
	/// </summary>
	class DictionaryTerminationRecord : IBaseRecord
	{
		public int RecordType { get { return 999; } }

		public void WriteRecord(BinaryWriter writer)
		{
			writer.Write(RecordType);
			writer.Write(0); // filler
		}
	}
}
