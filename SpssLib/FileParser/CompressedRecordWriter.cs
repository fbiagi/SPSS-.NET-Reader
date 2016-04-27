using System;
using System.IO;

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

	    /// <summary>
		/// Index for the next item on the current compressed block (from 0 to 7, 8 must reset & flush)
		/// </summary>
		private int _blockIndex;
		
        /// <summary>
		/// Next index to be written on the uncompressed data buffer.
        /// When this reachs a multiple of 8, a new <see cref="UncompressedValue"/> has to be written
        /// to the compressed block, and check if the uncompressed buffer has to be flushed.
		/// </summary>
		private int _uncompressedIndex;
		/// <summary>
		/// The buffer to accumulate the uncompressed data. The uncompressed data item is written in a 8 byte block, 
		/// and the it can have a max of 8 items (the size of the compressed block)
		/// </summary>
		private readonly byte[] _uncompressedBuffer = new byte[(Constants.BLOCK_BYTE_SIZE+1) * Constants.BLOCK_BYTE_SIZE];

        // Compessed codes
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
			CheckBlock();
		}

        /// <summary>
        /// Writes a byte into the compression block and advances the compression 
        /// block next available index
        /// </summary>
        /// <param name="code">the compression code to write</param>
        private void WriteCompressedCode(byte code)
        {
            // Incremet the compressed codes counter for the block and write the code to the output
            _blockIndex++;
            _writer.Write(code);
        }

		/// <summary>
		/// Writes a numeric value to the file
		/// </summary>
		/// <param name="d">The numeric value to be written</param>
		public void WriteNumber(double d)
		{
            // Check that the last block is full
            CheckUncompressedBlock();

			// Write it's compressed value
			if (!WriteCompressedValue(d))
			{
				// If it couldn't be compressed, write into the uncomppressed buffer its bytes
                WriteNumberToBuffer(BitConverter.GetBytes(d));
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

            // Is compressible if the value + bias is between 1 and 251 and it's and integer value
			double val = d + _bias;
			if (val > Padding && val < EndOfFile && Math.Abs(val % 1) < 0.00001)
			{
				WriteCompressedCode((byte)val);
				return true;
			}

			// If not compressible, set the flag accordingly
			WriteCompressedCode(UncompressedValue);
			return false;
		}

        /// <summary>
        /// Writes an 8 byte value to the buffer.
        /// </summary>
        /// <param name="bytes">The 8 bytes that represent a double</param>
        private void WriteNumberToBuffer(byte[] bytes)
        {
            if (bytes.Length != 8)
            {
                throw new ArgumentException("Uncompressed numbers must have an 8 bytes representation", nameof(bytes));
            }

            // Add bytes to uncompressed buffer
            Buffer.BlockCopy(bytes, 0, _uncompressedBuffer, _uncompressedIndex, Constants.BLOCK_BYTE_SIZE);
            _uncompressedIndex += Constants.BLOCK_BYTE_SIZE;
        }

	    /// <summary>
	    /// Checks if the last block is complete.
	    /// </summary>
	    public void StartString()
        {
            CheckUncompressedBlock();
        }

        /// <summary>
	    /// Writes bytes that correspond to chars in a string from a buffer.
	    /// The caller method should keep track of the segments itself and the max length 
	    /// in bytes that should be written according to the variable info
	    /// </summary>
	    /// <param name="bytes">The byte array to copy from</param>
	    /// <param name="start">The starting position from where to copy</param>
	    /// <param name="length">Ammount of bytes to write from the buffer</param>
	    public void WriteCharBytes(byte[] bytes, int start = 0, int length = Constants.BLOCK_BYTE_SIZE)
		{
            if(length > Constants.BLOCK_BYTE_SIZE)
                throw new ArgumentException("Can only write up to 8 bytes max");

            var currentUncompressedBlock = FullBlocksCount(_uncompressedIndex);
            
            // Add the bytes to the uncompressed buffer
            Buffer.BlockCopy(bytes, start, _uncompressedBuffer, _uncompressedIndex, length);
            _uncompressedIndex += length;

            // Check if a new uncompressed block indicator is needed. This should be 
            // when a new block gets filled. When that happens, we should check the 
            // compressed block.
            if (currentUncompressedBlock != FullBlocksCount(_uncompressedIndex))
            {
                WriteCompressedCode(UncompressedValue);
                CheckBlock();
            }
        }

	    /// <summary>
	    /// Fills the last block with padding spaces and fill the rest of the length of the
	    /// variables with padding spaces blocks (if needed).
	    /// </summary>
	    /// <param name="writtenBytes">Bytes that have already been written</param>
	    /// <param name="length">Total length of bytes that must be written for the variable</param>
	    public void EndStringVariable(int writtenBytes, int length)
        {
            // Close the uncompressed block (if there where any remainding bytes on it)
            // and add the written bytes
            writtenBytes += CloseUncompressBufferBlock();

            while (writtenBytes < length)
            {
                // Write 8 padding spaces compressed char block
                WriteCompressedCode(SpaceCharsBlock);
                // Add the 8 padding spaces bytes to the written count
                writtenBytes += 8;
                CheckBlock();
            }

            // Check that all the bytes of the string have been written to the output
            if (writtenBytes != length)
            {
                throw new Exception("Wrong count of bytes written for string variable. Expecting to write " + length +
                                    " but end up writting " + writtenBytes + " bytes");
            }
        }

        /// <summary>
        /// Gets wether there is an uncompleted block on the uncompressed buffer
        /// </summary>
        /// <returns>True if there is an uncompleted block on the uncompressed buffer, false otherwise</returns>
        private bool AreUncompletedUncompressedBlocks()
        {
            return _uncompressedIndex % Constants.BLOCK_BYTE_SIZE != 0;
        }

        /// <summary>
        /// Throws an exception if there is an uncompleted block on the uncompressed buffer.
        /// This method should be called when starting to write a new variable, to detect possible errors
        /// (like a string that hasn't been well terminated)
        /// </summary>
        private void CheckUncompressedBlock()
        {
            if (AreUncompletedUncompressedBlocks())
            {
                throw new Exception("Blocks on the uncompressed buffer must be all filled before starting to write another value");
            }
        }

        /// <summary>
        /// Gives the count of full uncompressed blocks (8 bytes) depending on the ammount of 
        /// uncompressedBytes supposedly written.
        /// i.e.: if there have been 17 bytes written to the uncompressed buffer, it means that
        /// there are 2 full blocks (and one with only one byte written)
        /// </summary>
        /// <param name="uncompressedBytes">Count of uncompressed bytes supposedly written</param>
        /// <returns>The count of full uncompressed blocks</returns>
        private int FullBlocksCount(int uncompressedBytes)
        {
            return uncompressedBytes/Constants.BLOCK_BYTE_SIZE;
        }

        /// <summary>
        /// Fills up the last uncompressed block (if there is an uncomplete block)
        /// </summary>
        /// <returns>
        /// The count of chars that have to be written to complete the block (0 if no uncomplete block 
        /// is found, otherwise a max of 7)
        /// </returns>
        private int CloseUncompressBufferBlock()
        {
            // If there are no uncomplete uncompressed blocks, do nothing
            if(!AreUncompletedUncompressedBlocks()) return 0;
            
            // Get the end of the uncomplete uncompressed buffer block
            var blockBoundary = Common.RoundUp(_uncompressedIndex, Constants.BLOCK_BYTE_SIZE);
            // Fill the remainding bytes with padding spaces
            for (int i = _uncompressedIndex; i < blockBoundary; i++)
            {
                _uncompressedBuffer[i] = 0x20;
            }
            int bytesWritten = blockBoundary - _uncompressedIndex;
            _uncompressedIndex = blockBoundary;
            WriteCompressedCode(UncompressedValue);
            CheckBlock();
            return bytesWritten;
        }
        
		/// <summary>
		/// Check if the compressed block is full and flush the uncompressed buffer to the writer if so.
		/// This method also move any uncomplete block data to the begining and set <see cref="_uncompressedIndex"/>
		/// correspondingly
		/// </summary>
		private void CheckBlock()
		{
			// Check if the end of a compressed block has been reached
            if (_blockIndex < Constants.BLOCK_BYTE_SIZE) return;

            // We should have written up to 8 compressed code blocks befor flusing, if not, there's something wrong
            if (_blockIndex > Constants.BLOCK_BYTE_SIZE)
                throw new Exception("A compressed block size must be no longer than 8. Current: "+_blockIndex);

            // Reset compresed block index
            _blockIndex = 0;
			
			// If there's something on the uncompressed buffer, write it 
			if (_uncompressedIndex >= 0)
			{
                // Get the ammount of uncompressed bytes  ready to be written 
			    var currentFullBlockIndex = FullBlocksCount(_uncompressedIndex) * Constants.BLOCK_BYTE_SIZE;
                // Write buffer to stream
				_writer.Write(_uncompressedBuffer, 0, currentFullBlockIndex);

                // Reset uncompresed buffer index, or set to the remaining bytes
                _uncompressedIndex = _uncompressedIndex - currentFullBlockIndex;

                //Move remaining bytes to the start of the uncompressed buffer
                Buffer.BlockCopy(_uncompressedBuffer, currentFullBlockIndex, _uncompressedBuffer, 0, _uncompressedIndex);
			}
		}

        /// <summary>
        /// Completes the compressed block, so the last uncompressed blocks of the file can be written
        /// </summary>
	    private void CompleteBlock()
		{
			if (_blockIndex != 0)
			{   // if there's at least one compressed code on the last block, fill it with pading and check for flush
				for (int i = _blockIndex; i < Constants.BLOCK_BYTE_SIZE; i++)
				{
					WriteCompressedCode(Padding);
				}
				CheckBlock();
			}
		}

		/// <summary>
		/// Finish writing the file.
		/// </summary>
		public void EndFile()
		{
			CompleteBlock();
		}
	}
}