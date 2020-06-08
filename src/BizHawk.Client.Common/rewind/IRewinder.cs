using System;

namespace BizHawk.Client.Common
{
	public interface IRewinder : IDisposable
	{
		int Count { get; }
		float FullnessRatio { get; }
		long Size { get; }
		int RewindFrequency { get; }

		bool Active { get; }

		void Capture(int frame);
		bool Rewind(int frames);

		void Suspend();
		void Resume();
	}
}
