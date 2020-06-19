namespace BizHawk.Client.Common
{
	public interface IRewindSettings
	{
		public bool UseCompression { get; }
		public bool EnabledSmall { get; }
		public bool EnabledMedium { get; }
		public bool EnabledLarge { get; }
		public int BufferSize { get; }
	}

	public class RewindConfig : IRewindSettings
	{
		public bool UseCompression { get; set; }
		public bool EnabledSmall { get; set; } = true;
		public bool EnabledMedium { get; set; }
		public bool EnabledLarge { get; set; }
		public int BufferSize { get; set; } = 128; // in mb
		public int SpeedMultiplier { get; set; } = 1;
	}
}
