using System;
using System.IO;
using SpssLib.SpssDataset;

namespace SpssLib.FileParser.Records
{
    public class VariableDisplayParameterRecord : BaseInfoRecord
    {
        private int[] _data;
        public override int SubType { get { return InfoRecordType.VariableDisplayParameter; } }
        
        internal int VariableCount { get; set; }

        public VariableDisplayInfo this[int variableIndex]
        {
            get
            {
                if (VariableCount == 0)
                {
                    throw new Exception("Varaible count not set");
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

                throw new SpssFileFormatException(string.Format("There must be 2 or 3 fields per variable on the variable display info. Count of items is {0}and variable count has be set to {1}, thus fielc count is {2}", ItemCount, VariableCount, fieldCount));
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
            throw new SpssFileFormatException(string.Format("Value {0} is invalid for Alignment"));
        }


        public override void RegisterMetadata(MetaData metaData)
        {
            metaData.VariableDisplayParameters = this;
        }

        protected override void WriteInfo(BinaryWriter writer)
        {
            throw new NotImplementedException();
        }

        protected override void FillInfo(BinaryReader reader)
        {
            CheckInfoHeader(4);

            _data = new int[ItemCount];
            for (int i = 0; i < _data.Length; i++)
            {
                _data[i] = reader.ReadInt32();
            }

            /*
            var infoList = new List<VariableDisplayInfo>(); 
             
            // Record can either have 2 or 3 fields per variable:
            int fieldCount = ItemCount / variableCount;
            int currentItemIndex = 0;
            
            for (int variableIndex = 0; variableIndex < variableCount; variableIndex++)
            {
                var info = new VariableDisplayInfo();

                for (int fieldIndex = 0; fieldIndex < fieldCount; fieldIndex++)
                {
                    var value = BitConverter.ToInt32(this.record.Items[currentItemIndex++], 0);
                    
                    // Measurement type:
                    if (fieldIndex == 0)
                    {
                        if (value == 1)
                        {
                            info.MeasurementType = MeasurementType.Nominal;
                        }
                        else if (value == 2)
                        {
                            info.MeasurementType = MeasurementType.Ordinal;
                        }
                        else if (value == 3)
                        {
                            info.MeasurementType = MeasurementType.Scale;
                        }
                        else
                        {
                            // Default option:
                            info.MeasurementType = MeasurementType.Nominal;
                        }
                    }
                    // TODO This is wrong, if there are only 2 fields, with is not supposed to be present
                    // Width:
                    if (fieldIndex == 1)
                    {
                        info.Width = value;
                    }

                    // Alignment:
                    if (fieldIndex == 2)
                    {
                        info.Alignment = (Alignment)value;
                    }
                }
                infoList.Add(info);
            }
            
            this.VariableDisplayEntries = new VariableDisplayInfoCollection(infoList);*/
        }

    }
}
