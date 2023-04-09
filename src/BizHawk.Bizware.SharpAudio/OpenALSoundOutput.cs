using System.Collections.Generic;
using BizHawk.Client.Common;

using SharpAudio;

namespace BizHawk.Bizware.SharpAudio
{
	public sealed class OpenALSoundOutput : SharpAudioSoundOutput
	{
		private const AudioBackend AUDIO_BACKEND = AudioBackend.OpenAL;

		public OpenALSoundOutput(IHostAudioManager sound, string chosenDeviceName)
			: base(AUDIO_BACKEND, sound, chosenDeviceName)
		{
		}

		public static IEnumerable<string> GetDeviceNames() => GetDeviceNames(AUDIO_BACKEND);
	}
}
