using System;

namespace BizHawk.Client.Common
{
	public interface IRewindSettings
	{
		public bool UseCompression { get; }
		public bool EnabledSmall { get; }
		public bool EnabledMedium { get; }
		public bool EnabledLarge { get; }
		public int FrequencySmall { get; }
		public int FrequencyMedium { get; }
		public int FrequencyLarge { get; }
		public int MediumStateSize { get; }
		public int LargeStateSize { get; }
		public int BufferSize { get; }
		public bool OnDisk { get; }
		public bool IsThreaded { get; }
	}

	public class RewindConfig : IRewindSettings
	{
		public bool UseCompression { get; set; }
		public bool EnabledSmall { get; set; } = true;
		public bool EnabledMedium { get; set; }
		public bool EnabledLarge { get; set; }
		public int FrequencySmall { get; set; } = 1;
		public int FrequencyMedium { get; set; } = 4;
		public int FrequencyLarge { get; set; } = 60;

		public int MediumStateSize { get; set; } = 262144; // 256kb
		public int LargeStateSize { get; set; } = 1048576; // 1mb
		public int BufferSize { get; set; } = 128; // in mb
		public bool OnDisk { get; set; }

		public bool IsThreaded { get; set; } = Environment.ProcessorCount > 1;
		public int SpeedMultiplier { get; set; } = 1;
	}
}
