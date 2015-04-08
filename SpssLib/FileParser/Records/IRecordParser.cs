using System.IO;
using System.Runtime.Serialization;
using SpssLib.FileParser.Records;

namespace SpssLib.FileParser
{
    internal interface IRecordParser
    {
        RecordType Accepts { get; }
        IRecord ParseRecord(BinaryReader reader);
    }

    internal class GeneralRecordParser<TRecord> : IRecordParser where TRecord : IRecord
    {
        private readonly RecordType _accepts;

        public RecordType Accepts
        {
            get { return _accepts; }
        }

        public GeneralRecordParser(RecordType accepts)
        {
            _accepts = accepts;
        }

        public IRecord ParseRecord(BinaryReader reader)
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