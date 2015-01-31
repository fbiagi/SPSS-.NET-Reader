using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using SpssLib.FileParser.Records;

namespace SpssLib.FileParser
{
    public class MetaData
    {
        private IList<IRecord> _records;
        internal MetaData(IList<IRecord> records)
        {
            _records = records;

            SetHeader(records);
            SetVariables(records);

            ValueLabelRecords = records.OfType<ValueLabelRecord>().ToList();
            InfoRecords = records.OfType<BaseInfoRecord>().ToList();
            MachineIntegerInfo = records.OfType<MachineIntegerInfoRecord>().SingleOrDefault();
            FloatingPointInfo = records.OfType<MachineFloatingPointInfoRecord>().SingleOrDefault();
            SystemMissingValue = FloatingPointInfo != null
                                            ? FloatingPointInfo.SystemMissingValue
                                            : double.MinValue;
            LongVariableNames = records.OfType<LongVariableNamesRecord>().SingleOrDefault();
            VariableDisplayParameters = records.OfType<VariableDisplayParameterRecord>().SingleOrDefault();
            if (VariableDisplayParameters != null)
            {
                VariableDisplayParameters.VariableCount = VariableCount;
            }
        }

        public HeaderRecord HeaderRecord { get; private set; }
        public IList<VariableRecord> VariableRecords { get; private set; }
        public IList<ValueLabelRecord> ValueLabelRecords { get; private set; }
        public DocumentRecord DocumentRecord
        {
            get { return _records.OfType<DocumentRecord>().SingleOrDefault(); }
        }

        public MachineIntegerInfoRecord MachineIntegerInfo { get; private set; }
        public MachineFloatingPointInfoRecord FloatingPointInfo { get; private set; }
        public LongVariableNamesRecord LongVariableNames { get; private set; }
        public VariableDisplayParameterRecord VariableDisplayParameters { get; private set; }

        public IList<BaseInfoRecord> InfoRecords { get; private set; }

        public double SystemMissingValue { get; private set; }
        // Count number of variables (the number of variable-records with a name,
        //     the rest is part of a long string variable):
        public int VariableCount { get { return VariableRecords.Count(v => v.Type != -1); } }

        private void SetHeader(IList<IRecord> records)
        {
            HeaderRecord = records.OfType<HeaderRecord>().SingleOrDefault();
            if (HeaderRecord == null)
            {
                if (records.OfType<HeaderRecord>().Count() > 1)
                {
                    throw new SpssFileFormatException("More than one header record found");
                }
                throw new SpssFileFormatException("No header record found");
            }
        }

        private void SetVariables(IList<IRecord> records)
        {
            VariableRecords = records.OfType<VariableRecord>().ToList();
            if (!VariableRecords.Any())
            {
                throw new SpssFileFormatException("No variable records found");
            }
        }
    }
}
