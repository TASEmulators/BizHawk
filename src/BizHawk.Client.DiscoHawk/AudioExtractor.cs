using System.Windows.Forms;
using System.Threading.Tasks;
using System.IO;
using BizHawk.Emulation.DiscSystem;

using BizHawk.Common;

namespace BizHawk.Client.DiscoHawk
{
	public static class AudioExtractor
	{
		public static string FFmpegPath;

		public static void Extract(Disc disc, string path, string fileBase)
		{
			var dsr = new DiscSectorReader(disc);

			var shouldHalt = false;
			bool? overwriteExisting = null; // true = overwrite, false = skip existing (unimplemented), null = unset

			var tracks = disc.Session1.Tracks;
			Parallel.ForEach(tracks, track =>
			{
				if (shouldHalt) return;

				if (!track.IsAudio)
					return;

				if (track.NextTrack == null)
					return;

				int trackLength = track.NextTrack.LBA - track.LBA;
				var waveData = new byte[trackLength * 2352];
				int startLba = track.LBA;
				lock(disc)
					for (int sector = 0; sector < trackLength; sector++)
					{
						dsr.ReadLBA_2352(startLba + sector, waveData, sector * 2352);
					}

				string mp3Path = $"{Path.Combine(path, fileBase)} - Track {track.Number:D2}.mp3";
				if (File.Exists(mp3Path))
				{
					if (overwriteExisting is null)
					{
						var dr = MessageBox.Show("This file already exists. Do you want extraction to proceed overwriting files, or cancel the entire operation immediately?", "File already exists", MessageBoxButtons.OKCancel);
						if (dr == DialogResult.Cancel)
						{
							shouldHalt = true;
							return;
						}
						overwriteExisting = true;
					}

					File.Delete(mp3Path);
				}

				string tempfile = Path.GetTempFileName();

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
