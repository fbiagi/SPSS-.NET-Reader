using System;
using System.Collections.Generic;
using System.Linq;

namespace SpssLib.FileParser.Records
{
    internal class ParserProvider
    {
        private readonly Dictionary<RecordType, IRecordParser> _parsers;

        internal ParserProvider()
        {
            var parsers = new IRecordParser[]
                {
                    new GeneralRecordParser<HeaderRecord>(RecordType.HeaderRecord),
                    new GeneralRecordParser<EbcdicHeaderRecord>(RecordType.EbcdicHeaderRecord),
                    new GeneralRecordParser<VariableRecord>(RecordType.VariableRecord),
                    new GeneralRecordParser<ValueLabelRecord>(RecordType.ValueLabelRecord),
                    new GeneralRecordParser<DocumentRecord>(RecordType.DocumentRecord),
                    new InfoRecordParser(new List<KeyValuePair<int, Type>>
                                         {
                                             RegisterInfoRecord<MachineIntegerInfoRecord>(InfoRecordType.MachineInteger),
                                             RegisterInfoRecord<MachineFloatingPointInfoRecord>(InfoRecordType.MachineFloatingPoint),
                                             RegisterInfoRecord<VariableDisplayParameterRecord>(InfoRecordType.VariableDisplayParameter),
                                             RegisterInfoRecord<LongVariableNamesRecord>(InfoRecordType.LongVariableNames),
                                             RegisterInfoRecord<VeryLongStringRecord>(InfoRecordType.VeryLongString),
                                             RegisterInfoRecord<CharacterEncodingRecord>(InfoRecordType.CharacterEncoding),
                                         }.ToDictionary(p => p.Key, p => p.Value)),
                    new GeneralRecordParser<DictionaryTerminationRecord>(RecordType.End),
                };

            _parsers = parsers.ToDictionary(p => p.Accepts, p => p);
        }

        private KeyValuePair<int, Type> RegisterInfoRecord<TRecord>(int subtype) where TRecord : BaseInfoRecord
        {
            return new KeyValuePair<int, Type>(subtype, typeof(TRecord));
        }

        internal IRecordParser GetParser(RecordType type)
        {
            IRecordParser parser;
            if (_parsers.TryGetValue(type, out parser))
            {
                return parser;
            }
            throw new SpssFileFormatException();
        }
    }
}