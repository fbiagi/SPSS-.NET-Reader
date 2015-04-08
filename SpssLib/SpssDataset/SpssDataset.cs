using System;
using System.Collections.ObjectModel;
using System.Linq;
using SpssLib.FileParser;
using System.IO;

namespace SpssLib.SpssDataset
{
	[Obsolete("Use SpssReader")]
    public class SpssDataset
    {
        public Collection<Variable> Variables { get; private set; }
        public RecordCollection Records { get; private set; }
		// TODO delete
        public RecordCollectionMissingCategoryValuesAsNull RecordsMissingCategoryValuesAsNull { get; private set; }

        public SpssDataset()
        {
            Variables = new Collection<Variable>();
            Records = new RecordCollection();
            RecordsMissingCategoryValuesAsNull = new RecordCollectionMissingCategoryValuesAsNull();
        }

        public SpssDataset(SavFileParser fileReader)
            : this()
        {
            foreach (var variable in fileReader.Variables)
            {
                Variables.Add(variable);
            }

            foreach (var dataRecord in fileReader.ParsedDataRecords)
            {
                var dataArray = dataRecord.ToArray();
                Records.Add(new Record(dataArray));
                RecordsMissingCategoryValuesAsNull.Add(new RecordMissingCategoryValuesAsNull(dataArray));
            }
        }

        public SpssDataset(Stream fileStream)
            : this(new SavFileParser(fileStream))
        {
        }
    }
}
