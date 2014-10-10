using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SpssLib.FileParser;
using SpssLib.SpssDataset;

namespace SpssLib.DataReader
{
	public class SpssWriter : IDisposable
	{
		private readonly SavFileWriter _output;

		private readonly SpssOptions _options = new SpssOptions();
		private ICollection<Variable> _variables;

		public SpssWriter(Stream output, ICollection<Variable> variables, SpssOptions options = null)
			: this(new SavFileWriter(output), variables, options) { }

		private SpssWriter(SavFileWriter output, ICollection<Variable> variables, SpssOptions options = null)
		{
			_output = output;
			_variables = variables.ToList();
			_options = Options;
			WriteFileHeader();
		}

		public SpssOptions Options
		{
			get { return _options; }
		}

		public ICollection<Variable> Variables 
		{
			get { return _variables; }
		}

		public object[] CreateRecord()
		{
			return new object[_variables.Count()];
		}

		public void WriteRecord(object[] record)
		{
			_output.WriteRecord(record);
		}

		private void WriteFileHeader()
		{
			_output.WriteFileHeader(_options, _variables);
		}

		public void Dispose()
		{
			_output.Dispose();
		}

		public void EndFile()
		{
			_output.EndFile();
		}
	}
}