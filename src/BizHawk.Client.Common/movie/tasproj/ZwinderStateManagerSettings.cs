﻿using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

using BizHawk.Common;

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
			CurrentStoreType = settings.CurrentStoreType;

			RecentUseCompression = settings.RecentUseCompression;
			RecentBufferSize = settings.RecentBufferSize;
			RecentTargetFrameLength = settings.RecentTargetFrameLength;
			RecentStoreType = settings.RecentStoreType;

			GapsUseCompression = settings.GapsUseCompression;
			GapsBufferSize = settings.GapsBufferSize;
			GapsTargetFrameLength = settings.GapsTargetFrameLength;
			GapsStoreType = settings.GapsStoreType;

			AncientStateInterval = settings.AncientStateInterval;
			AncientStoreType = settings.AncientStoreType;
		}

		/// <summary>
		/// Buffer settings when navigating near now
		/// </summary>
		[DisplayName("Current - Use Compression.")]
		[Description("The Current buffer is the primary buffer used near the last edited frame. This should be the largest buffer to ensure minimal gaps during editing.")]
		public bool CurrentUseCompression { get; set; }

		[DisplayName("Current - Buffer Size")]
		[Description("Max amount of buffer space to use in MB.\n\nThe Current buffer is the primary buffer used near the last edited frame. This should be the largest buffer to ensure minimal gaps during editing.")]
		[Range(64, 32768)]
		[TypeConverter(typeof(ConstrainedIntConverter))]
		public int CurrentBufferSize { get; set; } = 256;

		[DisplayName("Current - Target Frame Length")]
		[Description("Desired minimum rewind range (number of emulated frames you can go back before running out of buffer)\n\nThe Current buffer is the primary buffer used near the last edited frame. This should be the largest buffer to ensure minimal gaps during editing.")]
		[Range(1, int.MaxValue)]
		[TypeConverter(typeof(ConstrainedIntConverter))]
		public int CurrentTargetFrameLength { get; set; } = 500;

		[DisplayName("Current - Storage Type")]
		[Description("Where to keep the buffer.")]
		public IRewindSettings.BackingStoreType CurrentStoreType { get; set; } = IRewindSettings.BackingStoreType.Memory;

		/// <summary>
		/// Buffer settings when navigating directly before the Current buffer
		/// </summary>
		[DisplayName("Recent - Use Compression")]
		[Description("The Recent buffer is where the current frames decay as the buffer fills up. The goal of this buffer is to maximize the amount of movie that can be fairly quickly navigated to. Therefore, a high target frame length is ideal here.")]
		public bool RecentUseCompression { get; set; }

		[DisplayName("Recent - Buffer Size")]
		[Description("Max amount of buffer space to use in MB.\n\nThe Recent buffer is where the current frames decay as the buffer fills up. The goal of this buffer is to maximize the amount of movie that can be fairly quickly navigated to. Therefore, a high target frame length is ideal here.")]
		[Range(64, 32768)]
		[TypeConverter(typeof(ConstrainedIntConverter))]
		public int RecentBufferSize { get; set; } = 128;

		[DisplayName("Recent - Target Frame Length")]
		[Description("Desired minimum rewind range (number of emulated frames you can go back before running out of buffer).\n\nThe Recent buffer is where the current frames decay as the buffer fills up. The goal of this buffer is to maximize the amount of movie that can be fairly quickly navigated to. Therefore, a high target frame length is ideal here.")]
		[Range(1, int.MaxValue)]
		[TypeConverter(typeof(ConstrainedIntConverter))]
		public int RecentTargetFrameLength { get; set; } = 2000;

		[DisplayName("Recent - Storage Type")]
		[Description("Where to keep the buffer.")]
		public IRewindSettings.BackingStoreType RecentStoreType { get; set; } = IRewindSettings.BackingStoreType.Memory;

		/// <summary>
		/// Priority States for special use cases
		/// </summary>
		[DisplayName("Gaps - Use Compression")]
		[Description("The Gap buffer is used for temporary storage when replaying older segment of the run without editing. It is used to 're-greenzone' large gaps while navigating around in an older area of the movie. This buffer can be small, and a similar size to target frame length ratio as current is ideal.")]
		public bool GapsUseCompression { get; set; }

		[DisplayName("Gaps - Buffer Size")]
		[Description("Max amount of buffer space to use in MB\n\nThe Gap buffer is used for temporary storage when replaying older segment of the run without editing. It is used to 're-greenzone' large gaps while navigating around in an older area of the movie. This buffer can be small, and a similar size to target frame length ratio as current is ideal.")]
		[Range(64, 32768)]
		[TypeConverter(typeof(ConstrainedIntConverter))]
		public int GapsBufferSize { get; set; } = 64;

		[DisplayName("Gaps - Target Frame Length")]
		[Description("Desired minimum rewind range (number of emulated frames you can go back before running out of buffer)\n\nThe Gap buffer is used for temporary storage when replaying older segment of the run without editing.  It is used to 're-greenzone' large gaps while navigating around in an older area of the movie. This buffer can be small, and a similar size to target frame length ratio as current is ideal.")]
		[Range(1, int.MaxValue)]
		[TypeConverter(typeof(ConstrainedIntConverter))]
		public int GapsTargetFrameLength { get; set; } = 125;

		[DisplayName("Gaps - Storage Type")]
		[Description("Where to keep the buffer.")]
		public IRewindSettings.BackingStoreType GapsStoreType { get; set; } = IRewindSettings.BackingStoreType.Memory;

		[DisplayName("Ancient State Interval")]
		[Description("Once both the Current and Recent buffers have filled, some states are put into reserved to ensure there is always a state somewhat near a desired frame to navigate to. These states never decay but are invalidated. This number should be as high as possible without being overly cumbersome to replay this many frames.")]
		[Range(1, int.MaxValue)]
		[TypeConverter(typeof(ConstrainedIntConverter))]
		public int AncientStateInterval { get; set; } = 5000;

		[DisplayName("Ancient - Storage Type")]
		[Description("Where to keep the reserved states.")]
		public IRewindSettings.BackingStoreType AncientStoreType { get; set; } = IRewindSettings.BackingStoreType.Memory;

		// Just to simplify some other code.
		public RewindConfig Current()
		{
			return new RewindConfig
			{
				UseCompression = CurrentUseCompression,
				BufferSize = CurrentBufferSize,
				UseFixedRewindInterval = false,
				TargetFrameLength = CurrentTargetFrameLength,
				AllowOutOfOrderStates = false,
				BackingStore = CurrentStoreType,
			};
		}
		public RewindConfig Recent()
		{
			return new RewindConfig
			{
				UseCompression = RecentUseCompression,
				BufferSize = RecentBufferSize,
				UseFixedRewindInterval = false,
				TargetFrameLength = RecentTargetFrameLength,
				AllowOutOfOrderStates = false,
				BackingStore = RecentStoreType,
			};
		}
		public RewindConfig GapFiller()
		{
			return new RewindConfig
			{
				UseCompression = GapsUseCompression,
				BufferSize = GapsBufferSize,
				UseFixedRewindInterval = false,
				TargetFrameLength = GapsTargetFrameLength,
				AllowOutOfOrderStates = false,
				BackingStore = GapsStoreType,
			};
		}
	}
}
