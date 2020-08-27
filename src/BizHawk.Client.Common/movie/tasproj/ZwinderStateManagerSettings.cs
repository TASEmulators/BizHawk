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

			GapsUseCompression = settings.GapsUseCompression;
			GapsBufferSize = settings.GapsBufferSize;
			GapsTargetFrameLength = settings.GapsTargetFrameLength;

			AncientStateInterval = settings.AncientStateInterval;
		}

		/// <summary>
		/// Buffer settings when navigating near now
		/// </summary>
		[DisplayName("Current - Use Compression")]
		public bool CurrentUseCompression { get; set; }

		[DisplayName("Current - Buffer Size")]
		[Description("Max amount of buffer space to use in MB")]
		public int CurrentBufferSize { get; set; } = 256;

		[DisplayName("Current - Target Frame Length")]
		[Description("Desired frame length (number of emulated frames you can go back before running out of buffer)")]
		public int CurrentTargetFrameLength { get; set; } = 500;

		/// <summary>
		/// Buffer settings when navigating directly before the Current buffer
		/// </summary>
		[DisplayName("Recent - Use Compression")]
		public bool RecentUseCompression { get; set; }

		[DisplayName("Recent - Buffer Size")]
		[Description("Max amount of buffer space to use in MB")]
		public int RecentBufferSize { get; set; } = 128;

		[DisplayName("Recent - Target Frame Length")]
		[Description("Desired frame length (number of emulated frames you can go back before running out of buffer)")]
		public int RecentTargetFrameLength { get; set; } = 2000;

		/// <summary>
		/// Priority States for special use cases
		/// </summary>
		[DisplayName("Gaps - Use Compression")]
		public bool GapsUseCompression { get; set; }

		[DisplayName("Gaps - Buffer Size")]
		[Description("Max amount of buffer space to use in MB")]
		public int GapsBufferSize { get; set; } = 64;

		[DisplayName("Gaps - Target Frame Length")]
		[Description("Desired frame length (number of emulated frames you can go back before running out of buffer)")]
		public int GapsTargetFrameLength { get; set; } = 125;

		[DisplayName("Ancient State Interval")]
		[Description("How often to maintain states when outside of Current and Recent intervals")]
		public int AncientStateInterval { get; set; } = 5000;

		// Just to simplify some other code.
		public RewindConfig Current()
		{
			return new RewindConfig()
			{
				UseCompression = CurrentUseCompression,
				BufferSize = CurrentBufferSize,
				TargetFrameLength = CurrentTargetFrameLength
			};
		}
		public RewindConfig Recent()
		{
			return new RewindConfig()
			{
				UseCompression = RecentUseCompression,
				BufferSize = RecentBufferSize,
				TargetFrameLength = RecentTargetFrameLength
			};
		}
		public RewindConfig GapFiller()
		{
			return new RewindConfig()
			{
				UseCompression = GapsUseCompression,
				BufferSize = GapsBufferSize,
				TargetFrameLength = GapsTargetFrameLength
			};
		}
	}
}
