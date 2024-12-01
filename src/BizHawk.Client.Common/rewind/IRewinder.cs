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
		/// Rewind 1 or 2 saved frames, avoiding frameToAvoid if possible.
		/// </summary>
		bool Rewind(int frameToAvoid);

		void Suspend();
		void Resume();

		void Clear();
	}
}
