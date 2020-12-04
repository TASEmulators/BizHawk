using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

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
		[DisplayName("Current - Use Compression.")]
		[Description("The Current buffer is the primary buffer used near the last edited frame. This should be the largest buffer to ensure minimal gaps during editing.")]
		public bool CurrentUseCompression { get; set; }

		[DisplayName("Current - Buffer Size")]
		[Description("Max amount of buffer space to use in MB.\n\nThe Current buffer is the primary buffer used near the last edited frame. This should be the largest buffer to ensure minimal gaps during editing.")]
		[TypeConverter(typeof(IntConverter)), Range(64, 32768)]
		public int CurrentBufferSize { get; set; } = 256;

		[DisplayName("Current - Target Frame Length")]
		[Description("Desired frame length (number of emulated frames you can go back before running out of buffer)\n\nThe Current buffer is the primary buffer used near the last edited frame. This should be the largest buffer to ensure minimal gaps during editing.")]
		[TypeConverter(typeof(IntConverter)), Range(1, int.MaxValue)]
		public int CurrentTargetFrameLength { get; set; } = 500;

		/// <summary>
		/// Buffer settings when navigating directly before the Current buffer
		/// </summary>
		[DisplayName("Recent - Use Compression")]
		[Description("The Recent buffer is where the current frames decay as the buffer fills up. The goal of this buffer is to maximize the amount of movie that can be fairly quickly navigated to. Therefore, a high target frame length is ideal here.")]
		public bool RecentUseCompression { get; set; }

		[DisplayName("Recent - Buffer Size")]
		[Description("Max amount of buffer space to use in MB.\n\nThe Recent buffer is where the current frames decay as the buffer fills up. The goal of this buffer is to maximize the amount of movie that can be fairly quickly navigated to. Therefore, a high target frame length is ideal here.")]
		[TypeConverter(typeof(IntConverter)), Range(64, 32768)]
		public int RecentBufferSize { get; set; } = 128;

		[DisplayName("Recent - Target Frame Length")]
		[Description("Desired frame length (number of emulated frames you can go back before running out of buffer).\n\nThe Recent buffer is where the current frames decay as the buffer fills up. The goal of this buffer is to maximize the amount of movie that can be fairly quickly navigated to. Therefore, a high target frame length is ideal here.")]
		[TypeConverter(typeof(IntConverter)), Range(1, int.MaxValue)]
		public int RecentTargetFrameLength { get; set; } = 2000;

		/// <summary>
		/// Priority States for special use cases
		/// </summary>
		[DisplayName("Gaps - Use Compression")]
		[Description("The Gap buffer is used for temporary storage when replaying older segment of the run without editing. It is used to 're-greenzone' large gaps while navigating around in an older area of the movie. This buffer can be small, and a similar size to target frame length ratio as current is ideal.")]
		public bool GapsUseCompression { get; set; }

		[DisplayName("Gaps - Buffer Size")]
		[Description("Max amount of buffer space to use in MB\n\nThe Gap buffer is used for temporary storage when replaying older segment of the run without editing. It is used to 're-greenzone' large gaps while navigating around in an older area of the movie. This buffer can be small, and a similar size to target frame length ratio as current is ideal.")]
		[TypeConverter(typeof(IntConverter)), Range(64, 32768)]
		public int GapsBufferSize { get; set; } = 64;

		[DisplayName("Gaps - Target Frame Length")]
		[Description("Desired frame length (number of emulated frames you can go back before running out of buffer)\n\nThe Gap buffer is used for temporary storage when replaying older segment of the run without editing.  It is used to 're-greenzone' large gaps while navigating around in an older area of the movie. This buffer can be small, and a similar size to target frame length ratio as current is ideal.")]
		[TypeConverter(typeof(IntConverter)), Range(1, int.MaxValue)]
		public int GapsTargetFrameLength { get; set; } = 125;

		[DisplayName("Ancient State Interval")]
		[Description("Once both the Current and Recent buffers have filled, some states are put into reserved to ensure there is always a state somewhat near a desired frame to navigate to. These states never decay but are invalidated. This number should be as high as possible without being overly cumbersome to replay this many frames.")]
		[TypeConverter(typeof(IntConverter)), Range(1, int.MaxValue)]
		public int AncientStateInterval { get; set; } = 5000;

		// Just to simplify some other code.
		public RewindConfig Current()
		{
			return new RewindConfig
			{
				UseCompression = CurrentUseCompression,
				BufferSize = CurrentBufferSize,
				TargetFrameLength = CurrentTargetFrameLength
			};
		}
		public RewindConfig Recent()
		{
			return new RewindConfig
			{
				UseCompression = RecentUseCompression,
				BufferSize = RecentBufferSize,
				TargetFrameLength = RecentTargetFrameLength
			};
		}
		public RewindConfig GapFiller()
		{
			return new RewindConfig
			{
				UseCompression = GapsUseCompression,
				BufferSize = GapsBufferSize,
				TargetFrameLength = GapsTargetFrameLength
			};
		}
	}
}
