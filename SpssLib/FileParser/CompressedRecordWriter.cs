using System;
using System.IO;
using System.Text;
using SpssLib.FileParser.Records;

namespace SpssLib.FileParser
{
	/// <summary>
	/// Provides the functionality to write the compressed values to the underlying writer
	/// </summary>
	public class CompressedRecordWriter : IRecordWriter
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

	    private readonly Encoding _encoding;

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
		public CompressedRecordWriter(BinaryWriter writer, double bias, double sysMiss, Encoding encoding)
		{
			_writer = writer;
			_bias = bias;
			_sysMiss = sysMiss;
		    _encoding = encoding;
		}

		/// <summary>
		/// Writes a sysmiss value on the stream.
		/// This is to be used for null values mostly
		/// </summary>
		public void WriteSysMiss()
		{
			WriteCompressedCode(SysmissCode);
			CheckBlock();
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
			if (val > Padding && val < EndOfFile && (val % 1) == 0)
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
				for (int i = _uncompressedIndex + length; i < _uncompressedIndex + Constants.BlockByteSize; i++)
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
		    var byteCount = Common.RoundUp(width, 8);
			var bytes = string.IsNullOrEmpty(s) ? new byte[0] :  _encoding.GetPaddedRounded(s, 8, out byteCount, byteCount);
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
				if (bytes[j] != spaceByte)
				{
					return false;
				}
			}
			return true;
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
}