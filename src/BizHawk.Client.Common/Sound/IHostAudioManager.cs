namespace BizHawk.Client.Common
{
	public interface IHostAudioManager
	{
		int BlockAlign { get; }

		int BytesPerSample { get; }

		int ChannelCount { get; }

		int SampleRate { get; }

		void HandleInitializationOrUnderrun(bool isUnderrun, ref int samplesNeeded);
	}
}
