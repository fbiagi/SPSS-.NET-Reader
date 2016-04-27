using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SpssLib.FileParser;
using SpssLib.SpssDataset;

namespace SpssLib.DataReader
{
    /// <summary>
    /// Reads a spss files variables &amp; data from any stream
    /// </summary>
	public class SpssReader : IDisposable
	{
		private readonly SavFileParser _fileReader;

        // TODO add needed metadata info (use SpssOptions?)

        /// <summary>
        /// A collection of variables read from teh file 
        /// </summary>
		public ICollection<Variable> Variables { get; private set; }
        /// <summary>
        /// An enumerable of the cases contained in the file
        /// </summary>
		public IEnumerable<Record> Records { get; private set; }

		internal SpssReader(SavFileParser fileReader)
		{
			_fileReader = fileReader;
			Variables = fileReader.Variables.ToList();
			Records = fileReader.ParsedDataRecords.Select(d => new Record(d.ToArray()));
        }
        
        /// <summary>
        /// Creates a reader and read the file header (with variables) from a stream
        /// </summary>
        /// <param name="fileStream"></param>
		public SpssReader(Stream fileStream)
			: this(new SavFileParser(fileStream))
        {}

        /// <summary>
        /// Disposes the inner stream
        /// </summary>
		public void Dispose()
		{
			_fileReader.Dispose();
		}
	}
}
