using System;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BizHawk.DiscSystem;
using System.IO;
using System.Diagnostics;

namespace BizHawk
{
    class AudioExtractor
    {
        public static string FFmpegPath;

        public static void Extract(Disc disc, string path, string filebase)
        {
			bool confirmed = false;
            var tracks = disc.TOC.Sessions[0].Tracks;
            foreach (var track in tracks)
            {
                if (track.TrackType != ETrackType.Audio)
                    continue;

                var waveData = new byte[track.length_aba * 2352];
                int startLba = track.Indexes[1].LBA;
                for (int sector = 0; sector < track.length_aba; sector++)
                    disc.ReadLBA_2352(startLba + sector, waveData, sector * 2352);

				string mp3Path = string.Format("{0} - Track {1:D2}.mp3", Path.Combine(path, filebase), track.num);
				if(File.Exists(mp3Path))
				{
					if (!confirmed)
					{
						var dr = MessageBox.Show("This file already exists. Do you want extraction to proceed overwriting files, or cancel the entire operation immediately?", "File already exists", MessageBoxButtons.OKCancel);
						if (dr == DialogResult.Cancel) return;
						confirmed = true;
					}
					File.Delete(mp3Path);
				}

				string tempfile = Path.GetTempFileName();

				try
				{
					File.WriteAllBytes(tempfile, waveData);
					var ffmpeg = new FFMpeg();
					ffmpeg.Run("-f", "s16le", "-ar", "44100", "-ac", "2", "-i", tempfile, "-f", "mp3", "-ab", "192k", mp3Path);
				}
				finally
				{
					File.Delete(tempfile);
				}
            }
        }
    }
}
