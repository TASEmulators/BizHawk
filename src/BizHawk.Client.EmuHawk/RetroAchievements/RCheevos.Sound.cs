using System;
using System.IO;

using BizHawk.Common.PathExtensions;

namespace BizHawk.Client.EmuHawk
{
	public partial class RCheevos
	{
		private readonly Action<string> _playWavFileCallback;

		private static readonly string _loginSound = Path.Combine(PathUtils.ExeDirectoryPath, "overlay/login.wav");
		private static readonly string _unlockSound = Path.Combine(PathUtils.ExeDirectoryPath, "overlay/unlock.wav");
		private static readonly string _lboardStartSound = Path.Combine(PathUtils.ExeDirectoryPath, "overlay/lb.wav");
		private static readonly string _lboardFailedSound = Path.Combine(PathUtils.ExeDirectoryPath, "overlay/lbcancel.wav");
		private static readonly string _infoSound = Path.Combine(PathUtils.ExeDirectoryPath, "overlay/info.wav");

		private bool EnableSoundEffects { get; set; }

		private void PlaySound(string path)
		{
			if (EnableSoundEffects)
			{
				_playWavFileCallback(path);
			}
		}
	}
}