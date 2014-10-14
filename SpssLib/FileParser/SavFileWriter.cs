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
		//private ICollection<IBaseRecord> _headerRecords;
		private int _longNameCounter = 0;
		private Variable[] _variables;
		private CompressedRecordWriter _recordWriter;
		private long _bias;

		public SavFileWriter(Stream output)
		{
			_output = output;
			_writer = new BinaryWriter(_output, Encoding.ASCII);
		}

		// TODO split this method, is way too long now
		public void WriteFileHeader(SpssOptions options, ICollection<Variable> variables)
		{
			_bias = options.Bias;
			_variables = variables.ToArray();
			var headerRecords = new List<IBaseRecord>();

			var header = new HeaderRecord(options);
			header.NominalCaseSize = variables.Count;
			headerRecords.Add(header);

			var variableRecords = new List<VariableRecord>();
			var valueLabels = new List<ValueLabel>();
			// TODO: Machine Integer info
			// TODO: Machine Floating point info
			// TODO: Very Long String Record.
			// TODO: Variable display parameter record.
			var variableLongNames = new Dictionary<string, string>();

			foreach (var variable in _variables)
			{
				SetShortVariableName(variable, variableRecords);

				var record = new VariableRecord(variable);
				variableRecords.Add(record);

				// Check if a longNameVariableRecord is needed
				if (record.Name != variable.Name)
				{
					variableLongNames.Add(record.Name, variable.Name);
				}

				// TODO Avoid repeating the same valueLabels on the file
				// Add ValueLabels if necesary
				if (variable.ValueLabels != null && variable.ValueLabels.Any())
				{
					var valueLabel = new ValueLabel(variable.ValueLabels);
					valueLabel.VariableIndex.Add(variableRecords.Count); // TODO add & use a special counter for var Dictionary Index
					valueLabels.Add(valueLabel);
				}
			}

			headerRecords.AddRange(variableRecords.Cast<IBaseRecord>());

			foreach (var valueLabel in valueLabels)
			{
				var valueLabelRecord = new ValueLabelRecord(valueLabel);
				headerRecords.Add(valueLabelRecord);
			}
			
			if (variableLongNames.Any())
			{
				var longNameRecord = new LongVariableNamesRecord(variableLongNames);
				headerRecords.Add(longNameRecord);
			}

			headerRecords.Add(new DictionaryTerminationRecord());

			foreach (var headerRecord in headerRecords)
			{
				headerRecord.WriteRecord(_writer);
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
				_recordWriter = new CompressedRecordWriter(_writer, _bias, double.MinValue); // TODO: repleace with correct SysMiss value
			}
			else
			{
				// TODO: check if record is complete?, complete record if not?
				_recordWriter.NewRecord();
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
					_recordWriter.WriteString((string) record[i], variable.Width);
				}
			}
		}

		public void EndFile()
		{
			_recordWriter.EndFile();
		}
	}


	/// <summary>
	/// Provides the functionality to write the compressed values to the underlying writer
	/// </summary>
	public class CompressedRecordWriter
	{
		/// <summary>
		/// Underlying stream writer
		/// </summary>
		private readonly BinaryWriter _writer;
		/// <summary>
		/// The compression bias (compressed numbers will be stored as value + bias) 
		/// </summary>
		private readonly double _bias;
		/// <summary>
		/// The system missing value
		/// </summary>
		private readonly double _sysMiss;

		/// <summary>
		/// Next index in the block that is to be written (from 0 to 7)
		/// </summary>
		private int _blockIndex = 0;
		/// <summary>
		/// Next index to be written on the uncompressed data buffer. This should be incremented in steps of 8
		/// </summary>
		private int _uncompressedIndex = 0;
		/// <summary>
		/// The buffer to accumulate the uncompressed data. The uncompressed data item is written in a 8 byte block, 
		/// and the it can have a max of 8 items (the size of the compressed block)
		/// </summary>
		private byte[] _uncompressedBuffer = new byte[Constants.BlockByteSize * Constants.BlockByteSize];
		
		private const byte Padding = 0;
		// 1 to 251 are the compressed values
		private const byte EndOfFile = 252;
		private const byte UncompressedValue = 253;
		private const byte SpaceCharsBlock = 254;
		private const byte SysmissCode = 255;

		/// <summary>
		/// Creates a compressed recodr writer
		/// </summary>
		/// <param name="writer">The underlying binary writer with the stream</param>
		/// <param name="bias">The compression bias</param>
		/// <param name="sysMiss">The system missing value</param>
		public CompressedRecordWriter(BinaryWriter writer, double bias, double sysMiss)
		{
			_writer = writer;
			_bias = bias;
			_sysMiss = sysMiss;
		}

		/// <summary>
		/// Writes a sysmiss value on the stream.
		/// This is to be used for null values mostly
		/// </summary>
		public void WriteSysMiss()
		{
			WriteCompressedCode(SysmissCode);
		}

		/// <summary>
		/// Writes a numeric value to the file
		/// </summary>
		/// <param name="d">The numeric value to be written</param>
		public void WriteNumber(double d)
		{
			// Write it's compressed value
			if (!WriteCompressedValue(d))
			{
				// If it couldn't be compressed, wriote into the uncomppressed buffer its bytes
				WriteToBuffer(BitConverter.GetBytes(d));
			}
			// Check if the compressed block is full and flush the uncompressed buffer to the writer if so
			CheckBlock();
		}

		/// <summary>
		/// Writes the double value as the compressed byte into the compressed block
		/// </summary>
		/// <param name="d">The value to be written</param>
		/// <returns>True if the value could be written into the compressed block, False if the value could not be compressed.</returns>
		/// <remarks>
		///		In case this method has retured false, a <see cref="UncompressedValue"/> flag was written into the compression block,
		///		and the true value will have to be written into the <see cref="_uncompressedBuffer"/>.
		/// </remarks>
		private bool WriteCompressedValue(double d)
		{
			if (d == _sysMiss)
			{
				WriteCompressedCode(SysmissCode);
				return true;
			}

			double val = d + _bias;

			// Is compressible if the value + bias is between  1 and 251 and it's and integer value
			if (val > Padding && val <= EndOfFile && (val % 1) == 0)
			{
				WriteCompressedCode((byte)val);
				return true;
			}
			// If not compressible, set the flag accordingly
			WriteCompressedCode(UncompressedValue);
			return false;
		}

		/// <summary>
		/// Writes a byte into the compression block and advances the compression 
		/// block next available index
		/// </summary>
		/// <param name="code">the compression code to write</param>
		private void WriteCompressedCode(byte code)
		{
			_blockIndex++;
			_writer.Write(code);
		}

		/// <summary>
		/// Writes bytes to the uncompressed buffer and advances the next available uncompressed index.
		/// This method will write the block size (8 bytes) allways, padding with space chars if necesary.
		/// </summary>
		/// <param name="bytes">The byte array to copy from</param>
		/// <param name="start">The starting position from where to copy</param>
		private void WriteToBuffer(byte[] bytes, int start = 0)
		{
			// Check if an entire block is filled
			if (bytes.Length - start >= Constants.BlockByteSize)
			{
				Array.Copy(bytes, start, _uncompressedBuffer, _uncompressedIndex, Constants.BlockByteSize);
			}
			else
			{
				// The case for uncomplete blocks can be made when a string finishes
				// Write all remaining characters
				var length = bytes.Length - start;
				Array.Copy(bytes, start, _uncompressedBuffer, _uncompressedIndex, length);
				// Padd the block with spaces
				for (int i = _uncompressedIndex + length - Constants.BlockByteSize; i < _uncompressedIndex + Constants.BlockByteSize; i++)
				{
					_uncompressedBuffer[i] = 0x20;
				}
			}
			_uncompressedIndex += Constants.BlockByteSize;
		}
		
		/// <summary>
		/// Check if the compressed block is full and flush the uncompressed buffer to the writer if so
		/// </summary>
		private void CheckBlock()
		{
			// Check if the end of a compressed block has been reached
			if (_blockIndex < Constants.BlockByteSize) return;
			
			// If there's something on the uncompressed buffer, write it 
			if (_uncompressedIndex > 0)
			{
				// Write buffer to stream
				_writer.Write(_uncompressedBuffer, 0, _uncompressedIndex);
				// Reset uncompresed buffer index
				_uncompressedIndex = 0;
			}
			// Reset compresed block index
			_blockIndex = 0;
		}

		public void WriteString(string s, int width)
		{
			// TODO proper encoding, must be on header. What happen if encoding for spaces and other has more than 1 byte?
			var bytes = string.IsNullOrEmpty(s) ? new byte[0] :  Common.StringToByteArray(s);
			int i = 0;
			for (; i < bytes.Length && i < width; i+=8)
			{
				if (IsSpaceBlock(bytes, i))
				{
					WriteCompressedCode(SpaceCharsBlock);
				}
				else
				{
					WriteCompressedCode(UncompressedValue);
					WriteToBuffer(bytes, i);
				} 
				CheckBlock();
			}
			// Fill remaining with spaces
			if (i < width)
			{
				for (int j = i; j < width; j+=8)
				{
					WriteCompressedCode(SpaceCharsBlock);
					CheckBlock();
				}
			}
		}

		private bool IsSpaceBlock(byte[] bytes, int i)
		{
			const byte spaceByte = 0x20;
			for (int j = i; j < i + 8 && i < bytes.Length; j++)
			{
				if (bytes[i] != spaceByte)
				{
					return false;
				}
			}
			return true;
		}

		/// <summary>
		/// Closes the previous record and starts a new one
		/// </summary>
		public void NewRecord()
		{
			
		}

		private void CompleteBlock()
		{
			if (_blockIndex != 0)
			{
				for (int i = _blockIndex; i < Constants.BlockByteSize; i++)
				{
					WriteCompressedCode(Padding);
				}
				CheckBlock();
			}
		}

		/// <summary>
		/// Writes an end of file, if needed
		/// </summary>
		public void EndFile()
		{
			// Not sure if the eof should be after all uncompressed records. This library reader asumes so,
			// thats why we complete the current block (and flush uncompressed data) and then write a 
			// "termination" block.
			CompleteBlock();
			/*WriteCompressedCode(EndOfFile);
			CompleteBlock();*/
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