using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BizHawk.DiscSystem;
using WaveLibrary;
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

                var wave = new WaveFile(2, 16, 44100);
                var waveData = new byte[track.length_aba * 2352];
                int startLba = track.Indexes[1].LBA;
                for (int sector = 0; sector < track.length_aba; sector++)
                    disc.ReadLBA_2352(startLba + sector, waveData, sector * 2352);

                wave.SetData(waveData, waveData.Length / 4);
                string waveFilePath = Path.Combine(path, "__temp.wav");
                wave.WriteFile(waveFilePath);

                Encode(waveFilePath, string.Format("{0} - Track {1:D2}.mp3", Path.Combine(path, filebase), track.num));

                File.Delete(waveFilePath);
            }
        }

        static void Encode(string wavePath, string mp3Path)
        {
            var args = Escape("-i", wavePath, "-ab", "192k", mp3Path);

            StringBuilder sbCmdline = new StringBuilder();
            for (int i = 0; i < args.Length; i++)
            {
                sbCmdline.Append(args[i]);
                if (i != args.Length - 1) sbCmdline.Append(' ');
            }

            ProcessStartInfo oInfo = new ProcessStartInfo(FFmpegPath, sbCmdline.ToString());
            oInfo.UseShellExecute = false;
            oInfo.CreateNoWindow = true;
            oInfo.RedirectStandardOutput = true;
            oInfo.RedirectStandardError = true;

            Process proc = System.Diagnostics.Process.Start(oInfo);
            proc.WaitForExit();
            string result = proc.StandardError.ReadToEnd();
        }

        static string[] Escape(params string[] args)
        {
            return args.Select(s => s.Contains(" ") ? string.Format("\"{0}\"", s) : s).ToArray();
        }
    }
}
