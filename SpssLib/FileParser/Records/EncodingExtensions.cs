using System;
using System.Text;

namespace SpssLib.FileParser.Records
{
    /// <summary>
    /// Extentions for encoding methods to help with string writing/reading in spss formats
    /// </summary>
    internal static class EncodingExtensions
    {
        /// <summary>
        /// Gets a decoded string, with trailing spaces trimmed out
        /// </summary>
        internal static string GetTrimmed(this Encoding enc, byte[] arr)
        {
            if (enc == null)
            {
                throw new ArgumentNullException(nameof(enc), "No encoding set");
            }

            if (arr == null)
            {
                throw new ArgumentNullException(nameof(arr));
            }

            return enc.GetString(arr).TrimEnd();
        }


        /// <summary>
        /// Gets the encoded byte array representation of a string, for a fixed array length and padded
        /// with a specific char if needed
        /// </summary>
        /// <param name="enc">To encode the string</param>
        /// <param name="value">String to be encoded into the resulting byte[]</param>
        /// <param name="length">The fixed length requested for the returning byte[]</param>
        /// <param name="padding">The padding character to use (if the encoded bytes are less than length)</param>
        /// <remarks>
        /// If the last char to be encoded does not fit entirely on the array, it will bi removed and the space remaninig
        /// will be filled with the padding byte.
        /// </remarks>
        /// <returns>The fixed lenght byte[]</returns> 
        internal static byte[] GetPadded(this Encoding enc, string value, int length, byte padding = 0x20)
        {
            if (enc == null)
            {
                throw new ArgumentNullException(nameof(enc), "No encoding set");
            }

            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            int lastCharIndex;
            var charArr = AsCharArr(enc, value, length, out lastCharIndex);

            var byteArr = GetPaddedByteArray(enc, length, padding, charArr, lastCharIndex);
            return byteArr;
        }

        /// <summary>
        /// Gets the encoded byte array representation of a string, for a fixed array length and padded
        /// with a specific char if needed
        /// </summary>
        /// <param name="enc">To encode the string</param>
        /// <param name="value">String to be encoded into the resulting byte[]</param>
        /// <param name="roundUpBytes">A number of byte to round up to. The resulting array will be a multiple of this</param>
        /// <param name="maxLength">The max lenght of the resulting byte[], should be a multiple of <see cref="roundUpBytes"/></param>
        /// <param name="padding">The padding character to use (if there are remining bytes on the array)</param>
        /// <param name="lenght">Used to report back the actual length of the encoded string in bytes, with out the added roundUp bytes</param>
        /// <param name="roundUpDelta">
        ///     For when the round up must be performed for the string and some more characters.
        ///     When specified, the original eocoded byte length + this  will be the multiple of <see cref="roundUpBytes"/>
        /// </param>
        /// <remarks>
        /// If the last char to be encoded does not fit entirely on the array, it will bi removed and the space remaninig
        /// will be filled with the padding byte.
        /// </remarks>
        /// <returns>A byte array who's length is a multiple of <see cref="roundUpBytes"/></returns>
        internal static byte[] GetPaddedRounded(this Encoding enc, string value, int roundUpBytes, out int lenght, 
                                                int maxLength = int.MaxValue, byte padding = 0x20, int roundUpDelta = 0)
        {
            if (enc == null)
            {
                throw new ArgumentNullException(nameof(enc), "No encoding set");
            }

            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (maxLength != int.MaxValue && maxLength % roundUpBytes != 0)
            {
                throw new ArgumentException("The max length should be a multiple of the round up bytes", nameof(maxLength));
            }

            int lastCharIndex, byteCount;
            var charArr = AsCharArr(enc, value, maxLength, out lastCharIndex, out byteCount);
            lenght = byteCount;

            // Get the new rounde up length
            var roundUpLength = Common.RoundUp(byteCount + roundUpDelta, roundUpBytes) - roundUpDelta;

            var byteArr = GetPaddedByteArray(enc, roundUpLength, padding, charArr, lastCharIndex);
            return byteArr;
        }

        /// <summary>
        /// Gets the encoded bytes of a string, up to maxLength bytes
        /// </summary>
        /// <param name="enc">Encoding to use</param>
        /// <param name="value">String to encode</param>
        /// <param name="maxLength">Max size of the sequence of bytes to produce</param>
        /// <param name="byteArr">The byte array to write into</param>
        /// <param name="byteIndex">The index at which to start writting the resulting sequence of bytes</param>
        internal static int GetUpToMaxLenght(this Encoding enc, string value, int maxLength, byte[] byteArr, int byteIndex)
        {
            int lastCharIndex;
            var charArr = AsCharArr(enc, value, maxLength, out lastCharIndex);

            // Get the bytes
            return enc.GetBytes(charArr, 0, lastCharIndex, byteArr, byteIndex);
        }

        /// <summary>
        /// Gets the string as an array of chars. The reulting array has already been sliced 
        /// to the amount of chars that will fit into a byte[] of size <see cref="length"/> when 
        /// encoded with <see cref="enc"/>
        /// </summary>
        /// <param name="enc">To test the lenght of the encoded characters</param>
        /// <param name="value">The string to be encoded</param>
        /// <param name="length">
        ///     The max lenght of bytes, by using <see cref="enc"/> <see cref="Encoding.GetByteCount(char[])"/>
        ///     whe can check how many of the resulting char[] would fit on an ecoded byte array of this lenght
        /// </param>
        /// <param name="lastCharIndex">
        ///     The index of up to which character should be encoded to produce a byte[] of <see cref="length"/>
        /// </param>
        /// <returns>
        ///     All the chars of value. You should, use the <see cref="lastCharIndex"/> to indicate up to wich 
        ///     character of the returning array to encode.
        /// </returns> 
        private static char[] AsCharArr(Encoding enc, string value, int length, out int lastCharIndex)
        {
            int discardByteCount;
            return AsCharArr(enc, value, length, out lastCharIndex, out discardByteCount);
        }

        /// <summary>
        /// Gets the string as an array of chars. The reulting array has already been sliced 
        /// to the amount of chars that will fit into a byte[] of size <see cref="length"/> when 
        /// encoded with <see cref="enc"/>
        /// </summary>
        /// <param name="enc">To test the lenght of the encoded characters</param>
        /// <param name="value">The string to be encoded</param>
        /// <param name="length">
        ///     The max lenght of bytes, by using <see cref="enc"/> <see cref="Encoding.GetByteCount(char[])"/>
        ///     whe can check how many of the resulting char[] would fit on an ecoded byte array of this lenght
        /// </param>
        /// <param name="lastCharIndex">
        ///     The index of up to which character should be encoded to produce a byte[] of <see cref="length"/>
        /// </param>
        /// <returns>
        ///     All the chars of value. You should, use the <see cref="lastCharIndex"/> to indicate up to wich 
        ///     character of the returning array to encode.
        /// </returns> 
        private static char[] AsCharArr(Encoding enc, string value, int length, out int lastCharIndex, out int byteCount)
        {
            var charArr = value.ToCharArray(0, Math.Min(value.Length, length));

            // Up to which char array index should be converted (to fith the length)
            lastCharIndex = charArr.Length;

            // Find the number of chars that will fit on 'length' bytes
            while ((byteCount = enc.GetByteCount(charArr, 0, lastCharIndex)) > length) lastCharIndex--;
            return charArr;
        }


        /// <summary>
        /// Gets the encoded representation of thte char array.
        /// </summary>
        /// <param name="enc">To encode the char[]</param>
        /// <param name="length">The fixed length of the returning array</param>
        /// <param name="padding">
        ///     The character to fill the remaining bytes of the array 
        ///     not filled by the character encodings
        /// </param>
        /// <param name="charArr">The char array to be encoded</param>
        /// <param name="lastCharIndex">
        ///     The last character of the array that should
        ///     be encoded to fit in the resulting byte[]
        /// </param>
        /// <exception cref="ArgumentException">
        ///     <see cref="length"/> is not enough to accommodate the resulting bytes of the encoding of
        ///     <see cref="charArr"/> up to <see cref="lastCharIndex"/>
        /// </exception>
        /// <remarks>
        ///     The <see cref="lastCharIndex"/> must have already been calculating according to 
        ///     how many chars from <see cref="charArr"/> could fit in <see cref="length"/> bytes
        ///     when encoded using <see cref="enc"/>
        /// </remarks> 
        /// <returns>The fixed lenght byte[]</returns> 
        private static byte[] GetPaddedByteArray(Encoding enc, int length, byte padding, char[] charArr, int lastCharIndex)
        {
            // Create array of size 'length'
            var byteArr = new byte[length];

            // Get the bytes
            var writtenLenght = enc.GetBytes(charArr, 0, lastCharIndex, byteArr, 0);

            for (int i = writtenLenght; i < byteArr.Length; i++)
            {
                byteArr[i] = padding;
            }
            return byteArr;
        }
    }
}