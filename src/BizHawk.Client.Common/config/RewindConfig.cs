using System;

namespace BizHawk.Client.Common
{
	public class RewindConfig
	{
		public bool UseDelta { get; set; } = true;
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
