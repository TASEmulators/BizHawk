using System.ComponentModel;

namespace BizHawk.Client.Common
{
	public class ZwinderStateManagerSettings
	{
		public ZwinderStateManagerSettings() { }

		public ZwinderStateManagerSettings(ZwinderStateManagerSettings settings)
		{
			CurrentUseCompression = settings.CurrentUseCompression;
			CurrentBufferSize = settings.CurrentBufferSize;
			CurrentTargetFrameLength = settings.CurrentTargetFrameLength;

			RecentUseCompression = settings.RecentUseCompression;
			RecentBufferSize = settings.RecentBufferSize;
			RecentTargetFrameLength = settings.RecentTargetFrameLength;

			PriorityUseCompression = settings.PriorityUseCompression;
			PriorityBufferSize = settings.PriorityBufferSize;
			PriorityTargetFrameLength = settings.PriorityTargetFrameLength;

			AncientStateInterval = settings.AncientStateInterval;
			SaveStateHistory = settings.SaveStateHistory;
		}

		/// <summary>
		/// Buffer settings when navigating near now
		/// </summary>
		[DisplayName("Current - Use Compression")]
		public bool CurrentUseCompression { get; set; }

		[DisplayName("Current - Buffer Size")]
		[Description("Max amount of buffer space to use in MB")]
		public int CurrentBufferSize { get; set; } = 64;

		[DisplayName("Current - Target Frame Length")]
		[Description("Desired frame length (number of emulated frames you can go back before running out of buffer)")]
		public int CurrentTargetFrameLength { get; set; } = 1000;

		/// <summary>
		/// Buffer settings when navigating directly before the Current buffer
		/// </summary>
		[DisplayName("Recent - Use Compression")]
		public bool RecentUseCompression { get; set; }

		[DisplayName("Recent - Buffer Size")]
		[Description("Max amount of buffer space to use in MB")]
		public int RecentBufferSize { get; set; } = 64;

		[DisplayName("Recent - Target Frame Length")]
		[Description("Desired frame length (number of emulated frames you can go back before running out of buffer)")]
		public int RecentTargetFrameLength { get; set; } = 10000;

		/// <summary>
		/// Priority States for special use cases
		/// </summary>
		[DisplayName("Priority - Use Compression")]
		public bool PriorityUseCompression { get; set; }

		[DisplayName("Priority - Buffer Size")]
		[Description("Max amount of buffer space to use in MB")]
		public int PriorityBufferSize { get; set; } = 64;

		[DisplayName("Priority - Target Frame Length")]
		[Description("Desired frame length (number of emulated frames you can go back before running out of buffer)")]
		public int PriorityTargetFrameLength { get; set; } = 10000;

		[DisplayName("Ancient State Interval")]
		[Description("How often to maintain states when outside of Current and Recent intervals")]
		public int AncientStateInterval { get; set; } = 5000;

		[DisplayName("Save Savestate History")]
		[Description("Whether or not to save savestate history into .tasproj files")]
		public bool SaveStateHistory { get; set; } = true;
	}
}
