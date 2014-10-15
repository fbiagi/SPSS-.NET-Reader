using System.IO;
using System.Text;

namespace SpssLib.FileParser.Records
{
    public class Common
    {
        public static int RoundUp(int numToRound, int multiple)
        {
            if (multiple == 0)
            {
                return numToRound;
            }

            int remainder = numToRound % multiple;
            if (remainder == 0) return numToRound;
            return numToRound + multiple - remainder;
        }

        public static string ByteArrayToString(byte[] arr)
        {	// TODO detect actual code page instead of relying on utf-8 normalization
            //var enc1252 = System.Text.Encoding.GetEncoding(1252);
            //var enc437 = System.Text.Encoding.GetEncoding(437);
            var encUtf8 = System.Text.Encoding.UTF8;
			//var encUnicode = System.Text.Encoding.Unicode;
            //var encBigEndianUnicode = System.Text.Encoding.BigEndianUnicode;
            //var encDefault = Encoding.Default;

            //Console.WriteLine(enc1252.GetString(arr));
            //Console.WriteLine(enc437.GetString(arr));
            //Console.WriteLine(encUtf8.GetString(arr));
            //Console.WriteLine(encUnicode.GetString(arr));
            //Console.WriteLine(encBigEndianUnicode.GetString(arr));
            //Console.WriteLine(encDefault.GetString(arr));

            return encUtf8.GetString(arr);
        }

		
		public static byte[] StringToByteArray(string arr)
		{
			var enc = Encoding.UTF8;
			return enc.GetBytes(arr);
		}
    }

	public static class Extensions
	{
		public static void WriteChars(this BinaryWriter writer, string s)
		{
			writer.Write(s.ToCharArray());
		}
	}
}