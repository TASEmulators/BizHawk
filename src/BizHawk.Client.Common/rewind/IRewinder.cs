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
		/// <summary>
		/// Rewind 1 saved frame, if possible
		/// </summary>
		bool Rewind();

		void Suspend();
		void Resume();
	}
}
