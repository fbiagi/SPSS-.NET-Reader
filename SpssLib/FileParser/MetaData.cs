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
        public HeaderRecord HeaderRecord { get; set; }
        public Collection<VariableRecord> VariableRecords { get; internal set; }
        public Collection<ValueLabelRecord> ValueLabelRecords { get; internal set; }
        public DocumentRecord DocumentRecord { get; set; }
        public InfoRecords InfoRecords { get; set; }
        
        public int VariableCount
        {
            get
            {
                // Count number of variables (the number of variable-records with a name,
                //     the rest is part of a long string variable):
                return (from record in this.VariableRecords
                     where record.Type != -1
                     select record).Count();
            }
        }
    }

}
