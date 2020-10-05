namespace BizHawk.Client.Common
{
	public static class HostAudioManagerExtensions
	{
		public static int MillisecondsToSamples(this IHostAudioManager audioMan, int milliseconds)
		{
			return milliseconds * audioMan.SampleRate / 1000;
		}

		public static double SamplesToMilliseconds(this IHostAudioManager audioMan, int samples)
		{
			return samples * 1000.0 / audioMan.SampleRate;
		}
	}
}
