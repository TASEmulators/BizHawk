using System.IO;
using System.Linq;

using BizHawk.Common;
using BizHawk.Common.PathExtensions;
using BizHawk.Common.StringExtensions;

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
			var filePaths = new DirectoryInfo(dir!).GetFiles().Select(static fi => fi.FullName).ToArray();
			return filePaths.Where(audioPath.EqualsIgnoreCase) // first, look for the file type we actually asked for...
				.Concat(filePaths.Where(filePath => Path.GetFileNameWithoutExtension(filePath).EqualsIgnoreCase(basePath))) // ...then look for any other type
				.FirstOrDefault(CheckForAudio);
		}

		/// <exception cref="AudioDecoder_Exception">could not find source audio for <paramref name="audioPath"/></exception>
		public byte[] AcquireWaveData(string audioPath) => FFmpegService
			.DecodeAudio(FindAudio(audioPath) ?? throw new AudioDecoder_Exception($"Could not find source audio for: {Path.GetFileName(audioPath)}"));
	}
}
