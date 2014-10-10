using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using SpssLib.FileParser;
using SpssLib.SpssDataset;

namespace SpssLib.DataReader
{
	public class SpssReader
	{
		public ICollection<Variable> Variables { get; private set; }
		public IEnumerable<Record> Records { get; private set; }
		
		public void Read(SavFileParser fileReader)
		{
			Variables = fileReader.Variables.ToList();
			Records = fileReader.ParsedDataRecords.Select(d => new Record(d.ToArray()));
        }

        public void Read(Stream fileStream)
        {
			Read(new SavFileParser(fileStream));
        }
      
	}
}
