using System.IO;

using BizHawk.Common.PathExtensions;

namespace BizHawk.Client.EmuHawk
{
	public partial class RCheevos
	{
		private static MemoryStream ReadWavFile(string path)
		{
			try
			{
				return new(File.ReadAllBytes(Path.Combine(PathUtils.ExeDirectoryPath, path)), false);
			}
			catch
			{
				return null;
			}
		}

		private static readonly MemoryStream _loginSound = ReadWavFile("overlay/login.wav");
		private static readonly MemoryStream _unlockSound = ReadWavFile( "overlay/unlock.wav");
		private static readonly MemoryStream _lboardStartSound = ReadWavFile("overlay/lb.wav");
		private static readonly MemoryStream _lboardFailedSound = ReadWavFile("overlay/lbcancel.wav");
		private static readonly MemoryStream _infoSound = ReadWavFile("overlay/info.wav");

		private readonly Action<Stream> _playWavFileCallback;

		private bool EnableSoundEffects { get; set; }

		private void PlaySound(Stream wavFile)
		{
			if (EnableSoundEffects && wavFile != null)
			{
				wavFile.Position = 0;
				_playWavFileCallback(wavFile);
			}
		}
	}
}