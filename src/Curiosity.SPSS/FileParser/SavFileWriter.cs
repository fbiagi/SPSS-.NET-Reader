using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Curiosity.SPSS.DataReader;
using Curiosity.SPSS.FileParser.Records;
using Curiosity.SPSS.SpssDataset;

namespace Curiosity.SPSS.FileParser
{
	public class SavFileWriter : IDisposable
	{
		private readonly Stream _output;
		private readonly BinaryWriter _writer;
		private Variable[] _variables;
		private IRecordWriter _recordWriter;
		private long _bias;
		private bool _compress;
	    private SpssOptions _options;
        private readonly bool _disposeStream;

        private StringWriter _stringWriter;
         
		public SavFileWriter(Stream output, bool disposeStream = true)
		{
			_output = output;
            _disposeStream = disposeStream;
			_writer = new BinaryWriter(_output, Constants.BaseEncoding);
		}
		 
	    public void WriteFileHeader(SpssOptions options, IEnumerable<Variable> variables)
		{
		    _options = options;
			_compress = options.Compressed;
			_bias = options.Bias;
			_variables = variables.ToArray();

            // SPSS file header
            var headerRecords = new List<IRecord>
	                            {
	                                new HeaderRecord(options)
	                            };

	        // Process all variable info
			var variableLongNames = new Dictionary<string, string>();
            var veryLongStrings = new Dictionary<string, int>();
	        var displayInfoList = new List<VariableDisplayInfo>(_variables.Length);
            SetVariables(headerRecords, variableLongNames, veryLongStrings, displayInfoList);
            
			// Integer & encoding info
			var intInfoRecord = new MachineIntegerInfoRecord(_options.HeaderEncoding);
			headerRecords.Add(intInfoRecord);

            // Integer & encoding info
            var fltInfoRecord = new MachineFloatingPointInfoRecord();
            headerRecords.Add(fltInfoRecord);

            // Variable Display info, beware that the number of variables here must match the count of named variables 
            // (exclude the string continuation, include VLS segments)
	        var varDisplRecord = new VariableDisplayParameterRecord(displayInfoList.Count);
	        for (int index = 0; index < displayInfoList.Count; index++)
	        {
	            varDisplRecord[index] = displayInfoList[index];
	        }
            headerRecords.Add(varDisplRecord);

	        // Variable Long names (as info record)
			if (variableLongNames.Any())
			{
				var longNameRecord = new LongVariableNamesRecord(variableLongNames, _options.HeaderEncoding);
				headerRecords.Add(longNameRecord);
			}

            if (veryLongStrings.Any())
            {
                var veryLongStringsRecord = new VeryLongStringRecord(veryLongStrings, _options.HeaderEncoding);
                headerRecords.Add(veryLongStringsRecord);
            }

			// Char encoding info record (for data)
			var charEncodingRecord = new CharacterEncodingRecord(_options.DataEncoding);
			headerRecords.Add(charEncodingRecord);
			
			// End of the info records
			headerRecords.Add(new DictionaryTerminationRecord());

			// Write all of header, variable and info records
			foreach (var headerRecord in headerRecords)
			{
				headerRecord.WriteRecord(_writer);
			}
            
            if (_compress)
            {
                _recordWriter = new CompressedRecordWriter(_writer, _bias, double.MinValue);
            }
            else
            {
                throw new NotImplementedException("Uncompressed SPSS data writing is not yet implemented. Please set compressed to true");
            }
            
            _stringWriter = new StringWriter(_options.DataEncoding, _recordWriter);
		}

        private void SetVariables(List<IRecord> headerRecords, IDictionary<string, string> variableLongNames, IDictionary<string, int> veryLongStrings, List<VariableDisplayInfo> displayInfoList)
		{
			var variableRecords = new List<VariableRecord>(_variables.Length);
            var valueLabels = new List<ValueLabel>(_variables.Length);

            // Read the variables and create the needed records
            ProcessVariables(variableLongNames, veryLongStrings, variableRecords, valueLabels);
			headerRecords.AddRange(variableRecords);
			
			// Set the count of variables as "nominal case size" on the HeaderRecord
			var header = headerRecords.OfType<HeaderRecord>().First();
			header.NominalCaseSize = variableRecords.Count;

            var namedVariables = variableRecords.Where(v => v.DisplayInfo != null).ToList();

            displayInfoList.AddRange(namedVariables.Select(variableRecord => variableRecord.DisplayInfo));


            SetValueLabels(headerRecords, valueLabels);
		}

		private void SetValueLabels(List<IRecord> headerRecords, List<ValueLabel> valueLabels)
		{
			headerRecords.AddRange(valueLabels
									.Select(vl => new ValueLabelRecord(vl, _options.HeaderEncoding)));
		}

        private void ProcessVariables(IDictionary<string, string> variableLongNames, IDictionary<string, int> veryLongStrings, List<VariableRecord> variableRecords, List<ValueLabel> valueLabels)
		{
            int longNameCounter = 0;
            var namesList = new SortedSet<byte[]>(new ByteArrayComparer());
            var segmentsNamesList = new SortedList<byte[], int>(new ByteArrayComparer());

            foreach (var variable in _variables)
			{
				int dictionaryIndex = variableRecords.Count + 1;

                var records = VariableRecord.GetNeededVariables(variable, _options.HeaderEncoding, namesList, ref longNameCounter, veryLongStrings, segmentsNamesList);
				variableRecords.AddRange(records);

                // Even if the variable name is the same, it still needs a long record indicator otherwise SPSS doesn't know how to handle it.
				variableLongNames.Add(records[0].Name, variable.Name);

				// TODO Avoid repeating the same valueLabels on the file
				// Add ValueLabels if necesary
				if (variable.ValueLabels != null && variable.ValueLabels.Any())
				{
					var valueLabel = new ValueLabel(variable.ValueLabels.ToDictionary(p => BitConverter.GetBytes(p.Key), p => p.Value));
					valueLabel.VariableIndex.Add(dictionaryIndex);
					valueLabels.Add(valueLabel);
				}
			}
		}

	    private class ByteArrayComparer : IComparer<byte[]>
	    {
	        public int Compare(byte[] x, byte[] y)
	        {
	            var val = x[0] - y[0];
                for (int i = 0; val == 0 && ++i < x.Length; val = x[i] - y[i]);
	            return val;
	        }
	    }

	    public void Dispose()
		{
			_writer.Flush();
			_writer.Close();

			_output.Flush();

            if (_disposeStream)
            {
                _output.Dispose();
            }
		}

		public void WriteRecord(object[] record)
		{
			if (_recordWriter == null)
			{
				throw new SpssFileFormatException("Record writer not set");
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
                    if (_stringWriter == null)
                    {
                        throw new SpssFileFormatException("String writer not set");
                    }
					_stringWriter.WriteString((string)record[i], variable.TextWidth);
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
        public IDictionary<byte[], string> Labels { get; private set; }

		public IList<int> VariableIndex { get; } = new List<int>();

        public ValueLabel(IDictionary<byte[], string> labels)
		{
			Labels = labels;
		}
	}
}