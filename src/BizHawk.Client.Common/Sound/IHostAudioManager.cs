namespace BizHawk.Client.Common
{
	public interface IHostAudioManager
	{
		int BlockAlign { get; }

		int BytesPerSample { get; }

		int ChannelCount { get; }

		int ConfigBufferSizeMs { get; }

		string ConfigDevice { get; }

		int SampleRate { get; }

		void HandleInitializationOrUnderrun(bool isUnderrun, ref int samplesNeeded);
	}
}
