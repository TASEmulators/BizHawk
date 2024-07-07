using System.Threading.Tasks;
using System.IO;

using BizHawk.Common;

namespace BizHawk.Emulation.DiscSystem
{
	public static class AudioExtractor
	{
		public static void Extract(Disc disc, string path, string fileBase, Func<string, bool?> getOverwritePolicy)
		{
			var dsr = new DiscSectorReader(disc);

			var shouldHalt = false;
			bool? overwriteExisting = null; // true = overwrite, false = skip existing, null = unset

			var tracks = disc.Session1.Tracks;
			Parallel.ForEach(tracks, track =>
			{
				if (shouldHalt || track.NextTrack == null) return;
				if (!track.IsAudio) return;

				var startLba = track.LBA;
				var trackLength = track.NextTrack.LBA - startLba;
				var waveData = new byte[trackLength * 2352];
				lock (disc)
				{
					for (var sector = 0; sector < trackLength; sector++)
					{
						dsr.ReadLBA_2352(startLba + sector, waveData, sector * 2352);
					}
				}

				var mp3Path = $"{Path.Combine(path, fileBase)} - Track {track.Number:D2}.mp3";
				if (File.Exists(mp3Path))
				{
					overwriteExisting ??= getOverwritePolicy(mp3Path);
					switch (overwriteExisting)
					{
						case true: // "Yes" -- overwrite
							File.Delete(mp3Path);
							break;
						case false: // "No" -- skip
							return;
						case null: // "Cancel" -- halt
							shouldHalt = true;
							return;
					}
				}

				var tempfile = Path.GetTempFileName();
				try
				{
					File.WriteAllBytes(tempfile, waveData);
					FFmpegService.Run("-f", "s16le", "-ar", "44100", "-ac", "2", "-i", tempfile, "-f", "mp3", "-ab", "192k", mp3Path);
				}
				finally
				{
					File.Delete(tempfile);
				}
			});
		}
	}
}
