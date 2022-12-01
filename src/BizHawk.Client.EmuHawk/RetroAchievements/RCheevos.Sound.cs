using System.IO;
using System.Media;

using BizHawk.Common.PathExtensions;

namespace BizHawk.Client.EmuHawk
{
	public partial class RCheevos
	{
		// NOTE: these are net framework only...
		// this logic should probably be the main sound class
		// this shouldn't be a blocker to moving to net core anyways
		private static readonly SoundPlayer _loginSound = new(Path.Combine(PathUtils.ExeDirectoryPath, "overlay/login.wav"));
		private static readonly SoundPlayer _unlockSound = new(Path.Combine(PathUtils.ExeDirectoryPath, "overlay/unlock.wav"));
		private static readonly SoundPlayer _lboardStartSound = new(Path.Combine(PathUtils.ExeDirectoryPath, "overlay/lb.wav"));
		private static readonly SoundPlayer _lboardFailedSound = new(Path.Combine(PathUtils.ExeDirectoryPath, "overlay/lbcancel.wav"));
		private static readonly SoundPlayer _infoSound = new(Path.Combine(PathUtils.ExeDirectoryPath, "overlay/info.wav"));

		private bool EnableSoundEffects { get; set; }
	}
}