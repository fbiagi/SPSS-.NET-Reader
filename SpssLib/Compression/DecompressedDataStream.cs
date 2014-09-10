using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace SpssLib.Compression
{
    class DecompressedDataStream: Stream
    {
        private const int InstructionSetByteSize = 8;
        private const int DataElementByteSize = 8;
        const string SpaceString = "        ";
        
        public Stream CompressedDataStream { get; private set; }
        public double Bias { get; private set; }
        public double SystemMissing { get; private set; }
        
        private long position = 0;
        private byte[][] elementBuffer = new byte[8][];
        private int elementBufferPosition = 0;
        private int elementBufferSize;
        private int inElementPosition = 0; // for those rare cases where we end up in the middle of an element.

        private byte[] systemMissingBytes;
        private byte[] spacesBytes;

        private BinaryReader reader;

        public DecompressedDataStream(Stream compressedDataStream, double bias, double systemMissing)
        {
            this.CompressedDataStream = compressedDataStream;
            this.Bias = bias;
            this.SystemMissing = systemMissing;
            this.reader = new BinaryReader(compressedDataStream, Encoding.ASCII);

            spacesBytes = Encoding.ASCII.GetBytes(SpaceString);
            systemMissingBytes = BitConverter.GetBytes(this.SystemMissing);
        }

        public override bool CanRead
        {
            get { return CompressedDataStream.CanRead; }
        }

        public override bool CanSeek
        {
            get { return CompressedDataStream.CanSeek; }
        }

        public override bool CanWrite
        {
            get { return CompressedDataStream.CanWrite; }
        }

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
                return this.position;
            }
            set
            {
                throw new NotSupportedException();
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            // Usually we can just send out the next 8-byte element.
            if (count == 8 && offset == 0 && this.inElementPosition == 0)
            {
                if (PreserveBuffer())
                {
                    this.elementBuffer[this.elementBufferPosition++].CopyTo(buffer, offset);
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
                    if (this.inElementPosition == 8)
                    {
                        // Flow over to next 8-byte element
                        this.elementBufferPosition++;
                        this.inElementPosition = 0;
                    }
                    if (PreserveBuffer())
                    {
                        buffer[i + offset] = this.elementBuffer[this.elementBufferPosition][this.inElementPosition];
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
            if (!(this.elementBufferPosition < this.elementBufferSize))
            {
                if (this.parseNextInstructionSet())
                {
                    elementBufferPosition = 0;
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

        private bool parseNextInstructionSet()
        {
            byte[] instructionSet = reader.ReadBytes(InstructionSetByteSize);

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
                    double value = instruction - this.Bias;
                    this.elementBuffer[bufferPosition++] = BitConverter.GetBytes(value);
                }
                else if (instruction == 252) // end of file
                {
                    this.elementBufferSize = bufferPosition;
                    return false;
                }
                else if (instruction == 253) // uncompressed value
                {
                    uncompressedElementBufferPositions.Add(bufferPosition++);
                }
                else if (instruction == 254) // space string
                {
                    this.elementBuffer[bufferPosition++] = this.spacesBytes;
                }
                else  if (instruction == 255) // system missing value
                {
                    this.elementBuffer[bufferPosition++] = this.systemMissingBytes;
                }
            }
            this.elementBufferSize = bufferPosition++;

            // Read the uncompressed values (they follow after the instruction set):
            foreach (int pos in uncompressedElementBufferPositions)
            {
                this.elementBuffer[pos] = reader.ReadBytes(8);
            }
            return true;
        }
    }
}
