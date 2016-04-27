using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace SpssLib.Compression
{
    class DecompressedDataStream: Stream
    {
        private const int InstructionSetByteSize = 8;
        private const int DataElementByteSize = 8;
        const string SpaceString = "        ";
        
        public Stream CompressedDataStream { get; }
        public double Bias { get; }
        public double SystemMissing { get; }
        
        private long _position = 0;
        private byte[][] _elementBuffer = new byte[8][];
        private int _elementBufferPosition = 0;
        private int _elementBufferSize;
        private int _inElementPosition = 0; // for those rare cases where we end up in the middle of an element.

        private byte[] _systemMissingBytes;
        private byte[] _spacesBytes;

        private BinaryReader _reader;

        public DecompressedDataStream(Stream compressedDataStream, double bias, double systemMissing)
        {
            CompressedDataStream = compressedDataStream;
            Bias = bias;
            SystemMissing = systemMissing;
            _reader = new BinaryReader(compressedDataStream, Encoding.ASCII);

            _spacesBytes = Encoding.ASCII.GetBytes(SpaceString);
            _systemMissingBytes = BitConverter.GetBytes(SystemMissing);
        }

        public override bool CanRead => CompressedDataStream.CanRead;

        public override bool CanSeek => CompressedDataStream.CanSeek;

        public override bool CanWrite => CompressedDataStream.CanWrite;

        public override void Flush()
        {
            CompressedDataStream.Flush();
        }

        public override long Length
        {
            get { throw new NotSupportedException(); }
        }

        public override long Position
        {
            get
            {
                return _position;
            }
            set
            {
                throw new NotSupportedException();
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            // Usually we can just send out the next 8-byte element.
            if (count == 8 && offset == 0 && _inElementPosition == 0)
            {
                if (PreserveBuffer())
                {
                    _elementBuffer[_elementBufferPosition++].CopyTo(buffer, offset);
                    return 8;
                }
                else
                {
                    // End of stream:
                    return 0;
                }
            }
            
            // Else we have to run thru the bytes one by one:
            else
            {
                for (int i = 0; i < count; i++)
                {                   
                    // Check for the unlikely case that the byte-request runs over multiple elements
                    if (_inElementPosition == 8)
                    {
                        // Flow over to next 8-byte element
                        _elementBufferPosition++;
                        _inElementPosition = 0;
                    }
                    if (PreserveBuffer())
                    {
                        buffer[i + offset] = _elementBuffer[_elementBufferPosition][_inElementPosition];
                    }
                    else
                    {
                        // End of stream:
                        return 0;
                    }
                }
                return count;
            }
        }

        private bool PreserveBuffer()
        {
            // Check whether the end of internal buffer is reached 
            if (!(_elementBufferPosition < _elementBufferSize))
            {
                if (ParseNextInstructionSet())
                {
                    _elementBufferPosition = 0;
                    return true;
                }
                else
                {
                    // End of stream
                    return false;
                }
            }
            return true;
        }
        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        private bool ParseNextInstructionSet()
        {
            byte[] instructionSet = _reader.ReadBytes(InstructionSetByteSize);

            if (instructionSet.Length < InstructionSetByteSize)
            {
                // End of stream.
                return false;
            }

            
            List<int> uncompressedElementBufferPositions = new List<int>();
            int bufferPosition = 0;

            for (int i = 0; i < InstructionSetByteSize; i++)
            {
                int instruction = instructionSet[i];

                if (instruction == 0) //padding
                {
                }

                else if (instruction > 0 && instruction < 252) // compressed value
                {
                    // compute actual value:
                    double value = instruction - Bias;
                    _elementBuffer[bufferPosition++] = BitConverter.GetBytes(value);
                }
                else if (instruction == 252) // end of file
                {
                    _elementBufferSize = bufferPosition;
                    return false;
                }
                else if (instruction == 253) // uncompressed value
                {
                    uncompressedElementBufferPositions.Add(bufferPosition++);
                }
                else if (instruction == 254) // space string
                {
                    _elementBuffer[bufferPosition++] = _spacesBytes;
                }
                else  if (instruction == 255) // system missing value
                {
                    _elementBuffer[bufferPosition++] = _systemMissingBytes;
                }
            }
            _elementBufferSize = bufferPosition;

            // Read the uncompressed values (they follow after the instruction set):
            foreach (int pos in uncompressedElementBufferPositions)
            {
                _elementBuffer[pos] = _reader.ReadBytes(8);
            }
            return true;
        }
    }
}
