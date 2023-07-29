namespace BizHawk.Client.Common
{
	public interface IRewindSettings
	{
		/// <summary>
		/// Gets a value indicating whether or not to compress savestates before storing them
		/// </summary>
		bool UseCompression { get; }

		/// <summary>
		/// Gets a value indicating whether or not to delta compress savestates before storing them
		/// </summary>
		/// <value></value>
		// TODO: This is in here for frontend reasons, but the buffer itself doesn't interact with this.
		bool UseDelta { get; }

		/// <summary>
		/// Buffer space to use in MB
		/// </summary>
		long BufferSize { get; }

		/// <summary>
		/// Specifies whether TargetFrameLength or TargetRewindInterval is used.
		/// </summary>
		bool UseFixedRewindInterval { get; }

		/// <summary>
		/// Desired frame length (number of emulated frames you can go back before running out of buffer)
		/// </summary>
		int TargetFrameLength { get; }

		/// <summary>
		/// Desired rewind interval (number of emulated frames you can go back per rewind)
		/// </summary>
		int TargetRewindInterval { get; }

		/// <summary>
		/// Specifies if the rewinder should accept states that are given out of order.
		/// </summary>
		bool AllowOutOfOrderStates { get; }

		public enum BackingStoreType
		{
			Memory,
			TempFile,
		}

		BackingStoreType BackingStore { get; }
	}

	public class RewindConfig : IRewindSettings
	{
		public bool UseCompression { get; set; } = false;
		public bool UseDelta { get; set; } = false;
		public bool Enabled { get; set; } = true;
		public bool AllowSlowStates { get; set; } = false; // TODO: Hook up in UI
		public long BufferSize { get; set; } = 512; // in mb
		public bool UseFixedRewindInterval { get; set; } = false;
		public int TargetFrameLength { get; set; } = 600;
		public int TargetRewindInterval { get; set; } = 5;
		public bool AllowOutOfOrderStates { get; set; } = true;

		public IRewindSettings.BackingStoreType BackingStore { get; set; } = IRewindSettings.BackingStoreType.Memory;
	}
}
