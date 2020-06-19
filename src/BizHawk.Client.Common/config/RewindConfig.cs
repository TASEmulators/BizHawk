namespace BizHawk.Client.Common
{
	public interface IRewindSettings
	{
		/// <summary>
		/// Gets a value indicating whether or not to enable rewinding
		/// </summary>
		public bool Enabled { get; }

		/// <summary>
		/// Gets a value indicating whether or not to compress savestates before storing them
		/// </summary>
		public bool UseCompression { get; }
		

		/// <summary>
		/// Max amount of buffer space to use in MB
		/// </summary>
		public int BufferSize { get; }

		/// <summary>
		/// Desired frame length (number of emulated frames you can go back before running out of buffer)
		/// </summary>
		public int TargetFrameLength { get; }
	}

	public class RewindConfig : IRewindSettings
	{
		public bool UseCompression { get; set; }
		public bool Enabled { get; set; } = true;
		public int BufferSize { get; set; } = 512; // in mb
		public int TargetFrameLength { get; set; } = 600;
	}
}
