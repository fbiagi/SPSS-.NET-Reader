using SpssLib.SpssDataset;

namespace SpssLib.DataReader
{
	public class SpssOptions
	{
		public string Label { get; set; }
		public bool Compressed { get; set; }
		public long Bias { get; set; }
		public Variable WeightVariable { get; set; }
		public int Cases { get; set; }
		
		public SpssOptions()
		{
			// Default values
			Compressed = true;
			Bias = 100;
			Cases = -1;
		}
	}
}