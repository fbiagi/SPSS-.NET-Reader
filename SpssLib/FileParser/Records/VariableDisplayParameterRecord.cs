using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using SpssLib.SpssDataset;

namespace SpssLib.FileParser.Records
{
    public class VariableDisplayParameterRecord
    {
        private InfoRecord record;
        public VariableDisplayInfoCollection VariableDisplayEntries { get; private set; }

        internal VariableDisplayParameterRecord(InfoRecord record, int variableCount)
        {
            if (record.SubType != 11 || record.ItemSize != 4)
                throw new UnexpectedFileFormatException();
            this.record = record;

            var infoList = new List<VariableDisplayInfo>();

            // Record can either have 2 or 3 fields per variable:
            int fieldCount = this.record.ItemCount / variableCount;
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
            
            this.VariableDisplayEntries = new VariableDisplayInfoCollection(infoList);
        }

    }
}
