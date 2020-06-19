namespace BizHawk.Client.Common
{
	public interface IRewindSettings
	{
		public bool UseCompression { get; }
		public bool EnabledSmall { get; }
		public bool EnabledMedium { get; }
		public bool EnabledLarge { get; }
		public int FrequencySmall { get; }
		public int FrequencyMedium { get; }
		public int FrequencyLarge { get; }
		public int BufferSize { get; }
		public bool OnDisk { get; }
	}

	public class RewindConfig : IRewindSettings
	{
		public bool UseCompression { get; set; }
		public bool EnabledSmall { get; set; } = true;
		public bool EnabledMedium { get; set; }
		public bool EnabledLarge { get; set; }
		public int FrequencySmall { get; set; } = 1;
		public int FrequencyMedium { get; set; } = 4;
		public int FrequencyLarge { get; set; } = 60;

		public int BufferSize { get; set; } = 128; // in mb
		public bool OnDisk { get; set; }

		public int SpeedMultiplier { get; set; } = 1;
	}
}
