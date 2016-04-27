using System;
using System.IO;
using SpssLib.SpssDataset;

namespace SpssLib.FileParser.Records
{
    public class VariableDisplayParameterRecord : BaseInfoRecord
    {
        private int[] _data;
        public override int SubType => InfoRecordType.VariableDisplayParameter;

        /// <summary>
        /// Count number of variables (the number of variable-records with a name,
        /// the rest is part of a long string variable), this includes the variables 
        /// for VeryLongStrings segments
        /// </summary>
        internal int VariableCount { get; set; }

        /// <summary>
        /// Contructor to write the record
        /// </summary>
        /// <param name="variableCount">The count of named variables, this includes each VeryLongString segment
        /// (but not the variable records per each 8 bytes of additional text)</param>
        internal VariableDisplayParameterRecord(int variableCount)
        {
            ItemSize = 4;
            VariableCount = variableCount;
            ItemCount = variableCount*3;
            _data = new int[ItemCount];
        }

        public VariableDisplayInfo this[int variableIndex]
        {
            get
            {
                if (VariableCount == 0)
                {
                    throw new Exception("Variable count not set");
                }

                int fieldCount = ItemCount / VariableCount;

                if (fieldCount == 2)
                {
                    return new VariableDisplayInfo
                        {
                            MeasurementType = GetMeasurementType(_data[variableIndex*fieldCount + 0]),
                            Alignment = GetAlignmentType(_data[variableIndex*fieldCount + 1]),
                        };
                }
                
                if (fieldCount == 3)
                {
                    return new VariableDisplayInfo
                    {
                        MeasurementType = GetMeasurementType(_data[variableIndex * fieldCount + 0]),
                        Width = _data[variableIndex * fieldCount + 1],
                        Alignment = GetAlignmentType(_data[variableIndex * fieldCount + 2]),
                    };
                }

                throw new SpssFileFormatException(
                    $"There must be 2 or 3 fields per variable on the variable display info. Count of items is {ItemCount}and variable count has be set to {VariableCount}, thus fielc count is {fieldCount}");
            }
            set
            {
                var baseIndex = variableIndex*3;
                _data[baseIndex + 0] = (int)value.MeasurementType;
                _data[baseIndex + 1] = value.Width;
                _data[baseIndex + 2] = (int)value.Alignment;
            }
        }

        private MeasurementType GetMeasurementType(int measurement)
        {
            return Enum.IsDefined(typeof(MeasurementType), measurement) ? 
                        (MeasurementType)Enum.ToObject(typeof(MeasurementType), measurement)
                        : MeasurementType.Nominal;
        }

        private Alignment GetAlignmentType(int alignment)
        {
            if (Enum.IsDefined(typeof(Alignment), alignment))
            {
                return (Alignment)Enum.ToObject(typeof(Alignment), alignment);
            }
            throw new SpssFileFormatException($"Value {alignment} is invalid for Alignment");
        }


        public override void RegisterMetadata(MetaData metaData)
        {
            metaData.VariableDisplayParameters = this;
        }

        protected override void WriteInfo(BinaryWriter writer)
        {
            for (int i = 0; i < _data.Length; i++)
            {
                writer.Write(_data[i]);
            }
        }

        protected override void FillInfo(BinaryReader reader)
        {
            CheckInfoHeader(4);

            _data = new int[ItemCount];
            for (int i = 0; i < _data.Length; i++)
            {
                _data[i] = reader.ReadInt32();
            }
        }

    }
}
