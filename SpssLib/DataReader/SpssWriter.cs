using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SpssLib.FileParser;
using SpssLib.SpssDataset;

namespace SpssLib.DataReader
{
    /// <summary>
    /// Writes a spss data file to a stream
    /// </summary>
	public class SpssWriter : IDisposable
	{
		private readonly SavFileWriter _output;

		private readonly SpssOptions _options = new SpssOptions();
        
        // TODO use read only collection and make it public
		private readonly ICollection<Variable> _variables;

        /// <summary>
        /// Creates a spss writer
        /// </summary>
        /// <param name="output">The binary stream to write to</param>
        /// <param name="variables">The variable collection to use</param>
        /// <param name="options"></param>
		public SpssWriter(Stream output, ICollection<Variable> variables, SpssOptions options = null)
			: this(new SavFileWriter(output), variables, options) { }

		private SpssWriter(SavFileWriter output, ICollection<Variable> variables, SpssOptions options = null)
		{
			_output = output;
			_variables = variables.ToList();
            _options = options ?? _options;
			WriteFileHeader();
		}

        /// <summary>
        /// The file options used.
        /// </summary>
		public SpssOptions Options
		{
			get { return _options; }
		}

        /// <summary>
        /// The variable collection. Once the writting has started, it should not be modified.
        /// </summary>
		public ICollection<Variable> Variables 
		{
			get { return _variables; }
		}

        /// <summary>
        /// Creates a record array with the variable count as lenght
        /// </summary>
        /// <returns></returns>
		public object[] CreateRecord()
		{
			return new object[_variables.Count()];
		}

        /// <summary>
        /// Writes the record into the stream.
        /// </summary>
        /// <param name="record"></param>
		public void WriteRecord(object[] record)
		{
			_output.WriteRecord(record);
		}

		private void WriteFileHeader()
		{
			_output.WriteFileHeader(_options, _variables);
		}

        /// <summary>
        /// Finishes writting the file. If not used, last compressed values could be not written to the stream
        /// </summary>
		public void EndFile()
        {
            _output.EndFile();
        }

        /// <summary>
        /// Disposes the write stream
        /// </summary>
		public void Dispose()
		{   // TODO call EndFile
			_output.Dispose();
		}
	}
}