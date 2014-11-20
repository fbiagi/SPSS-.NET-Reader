using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using SpssLib.DataReader;
using SpssLib.FileParser.Records;
using SpssLib.SpssDataset;

namespace SpssLib.FileParser
{
	public class SavFileWriter : IDisposable
	{
		private readonly Stream _output;
		private readonly BinaryWriter _writer;
		private int _longNameCounter = 0;
		private Variable[] _variables;
		private IRecordWriter _recordWriter;
		private long _bias;
		private bool _compress;

		public SavFileWriter(Stream output)
		{
			_output = output;
			// TODO set system constant with file format base encoding
			_writer = new BinaryWriter(_output, Encoding.ASCII);
		}

		public void WriteFileHeader(SpssOptions options, ICollection<Variable> variables)
		{
			_compress = options.Compressed;
			_bias = options.Bias;
			_variables = variables.ToArray();
			var encoding = Encoding.UTF8; // TODO allow to change encoding, and reflec changes on headers & data writing
			var headerRecords = new List<IBaseRecord>();

			// SPSS file header
			headerRecords.Add(new HeaderRecord(options));

			// Process all variable info
			var variableLongNames = new Dictionary<string, string>();
			SetVaraibles(headerRecords, variableLongNames);

			// Integer & encoding info
			var intInfoRecord = new MachineIntegerInfoRecord(encoding);
			headerRecords.Add(intInfoRecord);
			
			// Variable Long names (as info record)
			if (variableLongNames.Any())
			{
				var longNameRecord = new LongVariableNamesRecord(variableLongNames);
				headerRecords.Add(longNameRecord);
			}

			// TODO: Machine Floating point info
			// TODO: Very Long String Record.
			// TODO: Variable display parameter record.

			// Char encoding info record (for data)
			var charEncodingRecord = new CharacterEncodingRecord(encoding);
			headerRecords.Add(charEncodingRecord);
			
			// End of the info records
			headerRecords.Add(new DictionaryTerminationRecord());


			// Write all of header, variable and info records
			foreach (var headerRecord in headerRecords)
			{
				headerRecord.WriteRecord(_writer);
			}
		}

		private void SetVaraibles(List<IBaseRecord> headerRecords, IDictionary<string, string> variableLongNames)
		{
			var variableRecords = new List<VariableRecord>();
			var valueLabels = new List<ValueLabel>();

			// Read the variables and create the needed records
			ProcessVariables(variableLongNames, variableRecords, valueLabels);
			headerRecords.AddRange(variableRecords.Cast<IBaseRecord>());
			
			// Set the count of varaibles as "nominal case size" on the HeaderRecord
			var header = headerRecords.OfType<HeaderRecord>().First();
			header.NominalCaseSize = variableRecords.Count;

			SetValueLabels(headerRecords, valueLabels);
		}

		private static void SetValueLabels(List<IBaseRecord> headerRecords, List<ValueLabel> valueLabels)
		{
			headerRecords.AddRange(valueLabels
									.Select(vl => new ValueLabelRecord(vl))
									.Cast<IBaseRecord>());
		}

		private void ProcessVariables(IDictionary<string, string> variableLongNames, List<VariableRecord> variableRecords, List<ValueLabel> valueLabels)
		{
			foreach (var variable in _variables)
			{
				SetShortVariableName(variable, variableRecords);

				int dictionaryIndex = variableRecords.Count + 1;

				var records = VariableRecord.GetNeededVaraibles(variable);
				variableRecords.AddRange(records);

				// Check if a longNameVariableRecord is needed
				if (records[0].Name != variable.Name)
				{
					variableLongNames.Add(records[0].Name, variable.Name);
				}

				// TODO Avoid repeating the same valueLabels on the file
				// Add ValueLabels if necesary
				if (variable.ValueLabels != null && variable.ValueLabels.Any())
				{
					var valueLabel = new ValueLabel(variable.ValueLabels);
					valueLabel.VariableIndex.Add(dictionaryIndex);
					valueLabels.Add(valueLabel);
				}
			}
		}

		/// <summary>
		/// Guaranties that the short name filed is an uper case string with a max of 8 chars that is 
		/// unique for all the variable records
		/// </summary>
		/// <param name="variable">The variable to set the shortName</param>
		/// <param name="variableRecords">The records that were already created for this file</param>
		private void SetShortVariableName(Variable variable, List<VariableRecord> variableRecords)
		{
			// Set the short name if missing
			if (string.IsNullOrEmpty(variable.ShortName))
			{
				variable.ShortName = variable.Name;
			}
			// Enforce 8 character limit
			if (variable.ShortName.Length > 8)
			{
				variable.ShortName = variable.ShortName.Substring(0, 8);
			}
			// Enforce upper case
			variable.ShortName = variable.ShortName.ToUpper();

			// Check if it's already on the variable records names
			if (variableRecords.All(r => r.Name != variable.ShortName))
			{
				return;
			}

			// Algorithm to create a variable with a short name.
			// As produced by "IBM SPSS STATISTICS 64-bit MS Windows 22.0.0.0"
			var currentLongNameIndex = ++_longNameCounter;

			// Avoid collitions in case there is already a var called VXX_A
			var appendableChars = Enumerable.Range('A', 'Z').Select(i => (char)i).ToArray();
			var appendCharIndex = 0;
			do
			{
				variable.ShortName = string.Format("V{0}_{1}", currentLongNameIndex, appendableChars[appendCharIndex++]);
			} while (variableRecords.Any(r => r.Name == variable.ShortName));
		}

		public void Dispose()
		{
			_writer.Flush();
			_writer.Close();
			_output.Dispose();
		}

		public void WriteRecord(object[] record)
		{
			if (_recordWriter == null)
			{
				if (_compress)
				{
					_recordWriter = new CompressedRecordWriter(_writer, _bias, double.MinValue); // TODO: repleace with correct SysMiss value
				}
				else
				{
					throw new NotImplementedException("Uncompressed data writing is not yet implemented. Please set compressed to true");
				}
			}

			for (int i = 0; i < _variables.Length; i++)
			{
				var variable = _variables[i];
				if (variable.Type == DataType.Numeric)
				{
					if (record[i] == null)
					{
						_recordWriter.WriteSysMiss();
					}
					else
					{
						_recordWriter.WriteNumber((double)record[i]);
					}
					
				}
				else
				{
					_recordWriter.WriteString((string) record[i], variable.TextWidth);
				}
			}
		}

		public void EndFile()
		{
			_recordWriter.EndFile();
		}
	}


	class ValueLabel
	{
		public IDictionary<double, string> Labels { get; private set; }

		public IList<int> VariableIndex
		{
			get { return _variableIndex; }
		}

		private IList<Int32> _variableIndex = new List<int>();

		public ValueLabel(IDictionary<double, string> labels)
		{
			Labels = labels;
		}
	}
}