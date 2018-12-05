using System.IO;
using System.Runtime.Serialization;

namespace SpssLib.FileParser.Records
{
    internal interface IRecordParser
    {
        RecordType Accepts { get; }
        IRecord ParseRecord(DualBinaryReader reader);
    }

    internal class GeneralRecordParser<TRecord> : IRecordParser where TRecord : IRecord
    {
        public RecordType Accepts { get; }

        public GeneralRecordParser(RecordType accepts)
        {
            Accepts = accepts;
        }

        public IRecord ParseRecord(DualBinaryReader reader)
        {
            TRecord record = CreateRecord();
            record.FillRecord(reader);
            return record;
        }

        private TRecord CreateRecord()
        {
            return (TRecord)FormatterServices.GetUninitializedObject(typeof(TRecord));
        }
    }
}