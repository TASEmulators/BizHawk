using System;
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
            var tracks = disc.TOC.Sessions[0].Tracks;
            foreach (var track in tracks)
            {
                if (track.TrackType != ETrackType.Audio)
                    continue;

                var waveData = new byte[track.length_aba * 2352];
                int startLba = track.Indexes[1].LBA;
                for (int sector = 0; sector < track.length_aba; sector++)
                    disc.ReadLBA_2352(startLba + sector, waveData, sector * 2352);

				string tempfile = Path.GetTempFileName();

				try
				{
					File.WriteAllBytes(tempfile, waveData);
					Encode(tempfile, string.Format("{0} - Track {1:D2}.mp3", Path.Combine(path, filebase), track.num));
				}
				finally
				{
					File.Delete(tempfile);
				}
            }
        }

        static void Encode(string wavePath, string mp3Path)
        {
			var ffmpeg = new FFMpeg();
			ffmpeg.Run("-f", "s16le", "-ar", "44100", "-ac", "2", "-i", wavePath, "-f", "mp3", "-ab", "192k", mp3Path);
        }

        static string[] Escape(params string[] args)
        {
            return args.Select(s => s.Contains(" ") ? string.Format("\"{0}\"", s) : s).ToArray();
        }
    }
}
