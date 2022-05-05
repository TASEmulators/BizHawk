#nullable enable

using System;
using System.Collections.Generic;

using BizHawk.Bizware.BizwareGL;
using BizHawk.Client.Common;

namespace BizHawk.Bizware.DirectX
{
	/// <summary>An indirection, so that types from the SlimDX assembly don't need to be resolved if DirectX/XAudio2 features are never used.</summary>
	public static class IndirectX
	{
		public static IGL CreateD3DGLImpl()
			=> new IGL_SlimDX9();

		public static ISoundOutput CreateDSSoundOutput(IHostAudioManager sound, IntPtr mainWindowHandle, string chosenDeviceName)
			=> new DirectSoundSoundOutput(sound, mainWindowHandle, chosenDeviceName);

		public static ISoundOutput CreateXAudio2SoundOutput(IHostAudioManager sound, string chosenDeviceName)
			=> new XAudio2SoundOutput(sound, chosenDeviceName);

		public static IEnumerable<string> GetDSSinkNames()
			=> DirectSoundSoundOutput.GetDeviceNames();

		public static IEnumerable<string> GetXAudio2SinkNames()
			=> XAudio2SoundOutput.GetDeviceNames();
	}
}
