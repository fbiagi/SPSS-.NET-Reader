using System.Collections.Generic;
using System.Linq;
using System.Text;
using SpssLib.FileParser.Records;

namespace SpssLib.FileParser
{
    public class MetaData
    {
        private MachineFloatingPointInfoRecord _floatingPointInfo;
        private HeaderRecord _headerRecord;
        private VariableDisplayParameterRecord _variableDisplayParameters;

        internal Encoding HeaderEncoding { get; set; }
        internal Encoding DataEncoding { get; set; }

        internal MetaData()
        {
            SystemMissingValue = double.MinValue;
            VariableRecords = new List<VariableRecord>();
            ValueLabelRecords = new List<ValueLabelRecord>();
            InfoRecords = new List<BaseInfoRecord>();
        }

        public HeaderRecord HeaderRecord
        {
            get { return _headerRecord; }
            internal set
            {
                if (_headerRecord != null)
                {
                    throw new SpssFileFormatException("The header record must be unique");
                }
                _headerRecord = value;
            }
        }

        public IList<VariableRecord> VariableRecords { get; private set; }
        public IList<ValueLabelRecord> ValueLabelRecords { get; private set; }
        public DocumentRecord DocumentRecord { get; internal set; }

        public MachineIntegerInfoRecord MachineIntegerInfo { get; internal set; }
        public MachineFloatingPointInfoRecord FloatingPointInfo
        {
            get { return _floatingPointInfo; }
            internal set
            {
                _floatingPointInfo = value;
                SystemMissingValue = _floatingPointInfo.SystemMissingValue;
            }
        }

        public LongVariableNamesRecord LongVariableNames { get; internal set; }
        public VariableDisplayParameterRecord VariableDisplayParameters
        {
            get { return _variableDisplayParameters; }
            internal set
            {
                _variableDisplayParameters = value;
                VariableDisplayParameters.VariableCount = VariableCount;
            }
        }

        public CharacterEncodingRecord CharEncodingRecord { get; internal set; }

        public IList<BaseInfoRecord> InfoRecords { get; private set; }

        public double SystemMissingValue { get; private set; }
        // Count number of variables (the number of variable-records with a name,
        //     the rest is part of a long string variable):
        public int VariableCount
        {
            get
            {
                if (!VariableRecords.Any())
                {
                    throw new SpssFileFormatException("No variable records found");
                }
                return VariableRecords.Count(v => v.Type != -1);
            }
        }

        internal void ChechDictionaryRecords()
        {
            if (HeaderRecord == null)
            {
                throw new SpssFileFormatException("No header record found");
            }
            
            if (!VariableRecords.Any())
            {
                throw new SpssFileFormatException("No variable records found");
            }
        }
    }
}
