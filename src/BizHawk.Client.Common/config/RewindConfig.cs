namespace BizHawk.Client.Common
{
	public interface IRewindSettings
	{
		public bool UseCompression { get; }
		public bool EnabledSmall { get; }
		public int BufferSize { get; }
	}

	public class RewindConfig : IRewindSettings
	{
		public bool UseCompression { get; set; }
		public bool EnabledSmall { get; set; } = true;
		public int BufferSize { get; set; } = 128; // in mb
	}
}
