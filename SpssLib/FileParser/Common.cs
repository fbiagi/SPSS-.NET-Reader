namespace SpssLib.FileParser
{
    public class Common
    {
        /// <summary>
        /// Returns the next multiple of the specified multiple, since the specified number 
        /// </summary>
        /// <param name="numToRound"></param>
        /// <param name="multiple"></param>
        /// <returns>The first number divisible by <see cref="multiple"/> that's greater or equal to <see cref="numToRound"/></returns>
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
    }
}