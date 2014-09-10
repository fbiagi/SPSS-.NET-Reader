using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;

namespace SpssLib.FileParser.Records
{
    public class InfoRecords
    {
        public Collection<InfoRecord> AllRecords { get; private set; }
        public MachineIntegerInfoRecord MachineIntegerInfo { get; private set; }
        public MachineFloatingPointInfoRecord MachineFloatingPointInfoRecord { get; private set; }
        public LongVariableNamesRecord LongVariableNamesRecord { get; private set; }
        public VeryLongStringRecord VeryLongStringRecord { get; private set; }
        public VariableDisplayParameterRecord VariableDisplayParameterRecord { get; private set; }

        public InfoRecords()
        {
            this.AllRecords = new Collection<InfoRecord>();
        }

        internal void ReadKnownRecords(int variableCount)
        {
            foreach (var record in this.AllRecords)
            {
                switch (record.SubType)
                {
                    case 3:
                        this.MachineIntegerInfo = new MachineIntegerInfoRecord(record);
                        break;
                    case 4:
                        this.MachineFloatingPointInfoRecord = new MachineFloatingPointInfoRecord(record);
                        break;
                    case 11:
                        this.VariableDisplayParameterRecord = new VariableDisplayParameterRecord(record, variableCount);
                        break;
                    case 13:
                        this.LongVariableNamesRecord = new LongVariableNamesRecord(record);
                        break;
                    case 14:
                        this.VeryLongStringRecord= new VeryLongStringRecord(record);
                        break;
                }
            }
        }

    }
}
