using System.IO;

using BizHawk.Common;
using BizHawk.Common.PathExtensions;

namespace BizHawk.Emulation.DiscSystem
{
	internal class AudioDecoder
	{
		[Serializable]
		public class AudioDecoder_Exception : Exception
		{
			public AudioDecoder_Exception(string message)
				: base(message)
			{
			}
		}

		public AudioDecoder()
		{
		}

		private bool CheckForAudio(string path) => FFmpegService.QueryAudio(path).IsAudio;

		/// <summary>
		/// finds audio at a path similar to the provided path (i.e. finds Track01.mp3 for Track01.wav)
		/// TODO - isnt this redundant with CueFileResolver?
		/// </summary>
		private string FindAudio(string audioPath)
		{
			var (dir, basePath, _) = audioPath.SplitPathToDirFileAndExt();
			//look for potential candidates
			DirectoryInfo di = new(dir!);
			var fis = di.GetFiles();
			//first, look for the file type we actually asked for
			foreach (var fi in fis)
			{
				if (string.Equals(fi.FullName, audioPath, StringComparison.OrdinalIgnoreCase))
					if (CheckForAudio(fi.FullName))
						return fi.FullName;
			}
			//then look for any other type
			foreach (var fi in fis)
			{
				if (string.Equals(Path.GetFileNameWithoutExtension(fi.FullName), basePath, StringComparison.OrdinalIgnoreCase))
				{
					if (CheckForAudio(fi.FullName))
					{
						return fi.FullName;
					}
				}
			}
			return null;
		}

		/// <exception cref="AudioDecoder_Exception">could not find source audio for <paramref name="audioPath"/></exception>
		public byte[] AcquireWaveData(string audioPath) => FFmpegService
			.DecodeAudio(FindAudio(audioPath) ?? throw new AudioDecoder_Exception($"Could not find source audio for: {Path.GetFileName(audioPath)}"));
	}
}
