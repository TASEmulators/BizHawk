using System.Collections.Generic;
using BizHawk.Client.Common;

using SharpAudio;

namespace BizHawk.Bizware.SharpAudio
{
	public sealed class XAudio2SoundOutput : SharpAudioSoundOutput
	{
		private const AudioBackend AUDIO_BACKEND = AudioBackend.XAudio2;

		public XAudio2SoundOutput(IHostAudioManager sound, string chosenDeviceName)
			: base(AUDIO_BACKEND, sound, chosenDeviceName)
		{
		}

		public static IEnumerable<string> GetDeviceNames() => GetDeviceNames(AUDIO_BACKEND);
	}
}
