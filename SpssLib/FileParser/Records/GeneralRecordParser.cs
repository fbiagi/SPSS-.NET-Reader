using System;
using System.IO;

namespace SpssLib.FileParser.Records
{
    internal class GeneralRecordParser<TRecord> : IRecordParser where TRecord : IRecord
    {
        public RecordType Accepts { get; }

        public GeneralRecordParser()
        {
            
        }

        public GeneralRecordParser(RecordType accepts)
        {
            Accepts = accepts;
        }

        public IRecord ParseRecord(BinaryReader reader)
        {
            TRecord record = CreateRecord();
            record.FillRecord(reader);
            return record;
        }

        private TRecord CreateRecord()
        {
            return Activator.CreateInstance<TRecord>();
        }
    }
}