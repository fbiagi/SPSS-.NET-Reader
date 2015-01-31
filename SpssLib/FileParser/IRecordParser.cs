using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using SpssLib.FileParser.Records;

namespace SpssLib.FileParser
{

    public static class InfoRecordType
    {
        public const int MachineInteger = 3;
        public const int MachineFloatingPoint = 4;
        public const int GroupedVariables = 5;              // not sure, not implemented
        public const int DateInfo = 6;                      // not sure, not implemented
        public const int MultipleResponseSets = 7;          // TODO implement may be?
        public const int VariableDisplayParameter = 11;     // TODO implement writing
        public const int LongVariableNames = 13;
        public const int VeryLongStringRecord = 14;         // TODO implement, VLS vars might look like multiple vars, find what should happend with variable indexes
        public const int ExtendedNumberOfCases = 16;        // TODO implement may be?
        public const int DataFileAttributes = 17;           // TODO implement may be?
        public const int VariableAttributes = 18;           // TODO implement may be?
        public const int MultipleResponseSetsV14 = 19;      // TODO implement may be?
        public const int CharacterEncoding = 20;            // TODO implement reading
        public const int LongStringValueLabels = 21;        // TODO implement
    }

    internal class ParserProvider
    {
        private readonly Dictionary<RecordType, IRecordParser> _parsers;

        internal ParserProvider()
        {
            var parsers = new IRecordParser[]
                {
                    new GeneralRecordParser<HeaderRecord>(RecordType.HeaderRecord),
                    new GeneralRecordParser<VariableRecord>(RecordType.VariableRecord),
                    new GeneralRecordParser<ValueLabelRecord>(RecordType.ValueLabelRecord),
                    new GeneralRecordParser<DocumentRecord>(RecordType.DocumentRecord),
                    new InfoRecordParser((new List<KeyValuePair<int, Type>>
                        {
                            RegisterInfoRecord<MachineIntegerInfoRecord>(InfoRecordType.MachineInteger),
                            RegisterInfoRecord<MachineFloatingPointInfoRecord>(InfoRecordType.MachineFloatingPoint),
                            RegisterInfoRecord<VariableDisplayParameterRecord>(InfoRecordType.VariableDisplayParameter),
                            RegisterInfoRecord<LongVariableNamesRecord>(InfoRecordType.LongVariableNames),
                            RegisterInfoRecord<CharacterEncodingRecord>(InfoRecordType.CharacterEncoding),
                        }).ToDictionary(p => p.Key, p => p.Value)),
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
            throw new UnexpectedFileFormatException();
        }
    }

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

            CheckType();
        }

        /// <summary>
        /// Checks if TType has a parameterless constructor, to able used with
        /// <see cref="Activator.CreateInstance{T}()"/>
        /// </summary>
        private void CheckType()
        {
            return;
            var type = typeof(TRecord);
            var parameterlessContructor = type.GetConstructor(new Type[0]);
            if (parameterlessContructor == null)
            {
                throw new Exception(
                    string.Format("Type {0} does not have a parameter-less constructor.",
                                  type.Name));
            }
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

    internal class InfoRecordParser : IRecordParser
    {
        public RecordType Accepts { get { return RecordType.InfoRecord; } }
        private readonly IDictionary<int, Type> _infoRecordsTypes;

        public InfoRecordParser(IDictionary<int, Type> infoRecordsTypes)
        {
            _infoRecordsTypes = infoRecordsTypes;
        }

        public IRecord ParseRecord(BinaryReader reader)
        {
            IRecord record = CreateRecord(reader);
            record.FillRecord(reader);
            return record;
        }

        private IRecord CreateRecord(BinaryReader reader)
        {
            int subType = reader.ReadInt32();
            Type type;
            var record = _infoRecordsTypes.TryGetValue(subType, out type)
                        ? (BaseInfoRecord)FormatterServices.GetUninitializedObject(type)
                        : new UnknownInfoRecord(subType);
            
            // Check that we created the correct one
            if (record.SubType != subType)
            {
                 // if it gets to here, we fucked up registering the infoRecordsTypes when calling the constructor
                throw new Exception(string.Format("Wrong info record created for {0}, obtained record instance for {1}. Please, fix the InfoRecordParser", subType, record.SubType));
            }

            return record;
        }
    }
}