namespace BizHawk.Client.Common
{
	public interface IRewindSettings
	{
		/// <summary>
		/// Gets a value indicating whether or not to compress savestates before storing them
		/// </summary>
		bool UseCompression { get; }

		/// <summary>
		/// Buffer space to use in MB
		/// </summary>
		long BufferSize { get; }

		/// <summary>
		/// Desired frame length (number of emulated frames you can go back before running out of buffer)
		/// </summary>
		int TargetFrameLength { get; }

		public enum BackingStoreType
		{
			Memory,
			TempFile,
		}

		public BackingStoreType BackingStore { get; }
	}

	public class RewindConfig : IRewindSettings
	{
		public bool UseCompression { get; set; }
		public bool Enabled { get; set; } = true;
		public long BufferSize { get; set; } = 512; // in mb
		public int TargetFrameLength { get; set; } = 600;
		public IRewindSettings.BackingStoreType BackingStore { get; set; } = IRewindSettings.BackingStoreType.Memory;
	}
}
