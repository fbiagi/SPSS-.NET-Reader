using System;
using System.Text;
using SpssLib.FileParser.Records;

namespace SpssLib.FileParser
{
    internal class StringWriter
    {
        private readonly IRecordWriter _recordWriter;

        /// <summary>
        /// Encoder to get the bytes of string data
        /// </summary>
        private readonly Encoder _encoder;

        /// <summary>
        /// Buffer to hold encoded string bytes
        /// </summary>
        private readonly byte[] _stringBytesBuffer;

        /// <summary>
        /// Size in chars for the encoded bytes
        /// </summary>
        private const int CharsBufferSize = 1024;

        /// <summary>
        /// Buffer to hold up to 255 encoded bytes + 1 space char byte (the bytes for the string segment)
        /// This wil be used to direclty write into the uncompressed buffer
        /// </summary>
        private readonly byte[] _stringSegmentBuffer;

        /// <summary>
        /// The max useful byte capacity for a segment
        /// </summary>
        private const int StringSegmentByteSize = 255;

        public StringWriter(Encoding encoding, IRecordWriter recordWriter)
        {
            _recordWriter = recordWriter;
            _encoder = encoding.GetEncoder();

            _stringBytesBuffer = new byte[encoding.GetMaxByteCount(CharsBufferSize)];

            _stringSegmentBuffer = new byte[256];
            _stringSegmentBuffer[255] = 0x20;
        }

        public void WriteString(string s, int width)
        {
            _recordWriter.StartString();

            // Get the char array (excluding trailing spaces)
            var chars = (s ?? string.Empty).TrimEnd(' ').ToCharArray();

            // Actual byte length of this variable
            var length = VariableRecord.GetLongStringBytesCount(width);

            var charIndex = 0;
            int writtenBytes = 0;
            var writtenUpToIndex = 0;

            while (chars.Length > charIndex && writtenBytes < length)
            {
                var charsToRead = Math.Min(chars.Length - charIndex, CharsBufferSize);

                int charsRead, bytesRead;
                bool completed;
                _encoder.Convert(chars, charIndex, charsToRead, _stringBytesBuffer, 0, _stringBytesBuffer.Length,
                                 false, out charsRead, out bytesRead, out completed);

                // Move index forward for next read
                charIndex += charsRead;

                var bytesLeftToWrite = bytesRead;
                writtenBytes = WriteBytes(bytesLeftToWrite, writtenBytes, length, ref writtenUpToIndex);
            }

            // Clean encoder internal buffer
            _encoder.Reset();
            
            _recordWriter.EndStringVariable(writtenBytes, length);
        }

        private int WriteBytes(int bytesLeftToWrite, int writtenBytes, int length, ref int writtenUpToIndex)
        {
            var copiedToBufferIndex = 0;

            while (bytesLeftToWrite > 0 && writtenBytes < length)
            {
                var lengthToCopy = Math.Min(bytesLeftToWrite, StringSegmentByteSize - writtenUpToIndex);

                Buffer.BlockCopy(_stringBytesBuffer, copiedToBufferIndex, _stringSegmentBuffer, writtenUpToIndex,
                           lengthToCopy);

                copiedToBufferIndex += lengthToCopy;

                var writeUpToIndex = writtenUpToIndex + lengthToCopy;
                // Include the space at position 256 of the VLS segment, if we've reached the end of the segment buffer
                if (writeUpToIndex == StringSegmentByteSize)
                {
                    writeUpToIndex = StringSegmentByteSize + 1;
                }

                while (writtenUpToIndex < writeUpToIndex && writtenBytes < length)
                {
                    // Write wither a full block or less, not more
                    var writeBytes = Math.Min(writeUpToIndex - writtenUpToIndex, Constants.BLOCK_BYTE_SIZE);
                    _recordWriter.WriteCharBytes(_stringSegmentBuffer, writtenUpToIndex, writeBytes);
                    writtenUpToIndex += writeBytes;
                    writtenBytes += writeBytes;
                }
                // Decrement the ammount of bytes left to write
                bytesLeftToWrite -= lengthToCopy;

                // If we've reached the end of the segment buffer, reset the position
                if (writtenUpToIndex == StringSegmentByteSize + 1)
                {
                    writtenUpToIndex = 0;
                }
            }
            return writtenBytes;
        }
    }
}