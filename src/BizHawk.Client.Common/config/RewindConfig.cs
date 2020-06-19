namespace BizHawk.Client.Common
{
	public interface IRewindSettings
	{
		public bool UseCompression { get; }
		public bool Enabled { get; }
		public int BufferSize { get; }
	}

	public class RewindConfig : IRewindSettings
	{
		public bool UseCompression { get; set; }
		public bool Enabled { get; set; } = true;
		public int BufferSize { get; set; } = 512; // in mb
	}
}
