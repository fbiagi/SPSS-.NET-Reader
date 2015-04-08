using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SpssLib.FileParser;
using SpssLib.SpssDataset;

namespace SpssLib.DataReader
{
	public class SpssReader : IDisposable
	{
		private readonly SavFileParser _fileReader;

        // TODO add needed metadata info (use SpssOptions?)

		public ICollection<Variable> Variables { get; private set; }
		public IEnumerable<Record> Records { get; private set; }

		internal SpssReader(SavFileParser fileReader)
		{
			_fileReader = fileReader;
			Variables = fileReader.Variables.ToList();
			Records = fileReader.ParsedDataRecords.Select(d => new Record(d.ToArray()));
        }

		public SpssReader(Stream fileStream)
			: this(new SavFileParser(fileStream))
        {}

		public void Dispose()
		{
			_fileReader.Dispose();
		}
	}
}
