using System;
using System.IO;
using BizHawk.DiscSystem;

// The state of the cd player is quantized to the frame level.
// This isn't ideal. But life's too short. 
// I decided not to let the perfect be the enemy of the good.
// It can always be refactored. It's at least deterministic.

namespace BizHawk.Emulation.Sound
{
    public sealed class CDAudio : ISoundProvider
    {
        public enum CDAudioMode
        {
            Stopped,
            Playing,
            Paused
        }

        public enum PlaybackMode
        {
            StopOnCompletion,
            NextTrackOnCompletion,
            LoopOnCompletion,            
            CallbackOnCompletion
        }

        public Action CallbackAction = delegate { };

        public Disc Disc;
        public CDAudioMode Mode = CDAudioMode.Stopped;
        public PlaybackMode PlayMode = PlaybackMode.LoopOnCompletion;

        public int MaxVolume { get; set; }
        public int LogicalVolume = 100;

        public int StartLBA, EndLBA;
        public int PlayingTrack;

        public int CurrentSector, SectorOffset; // Offset is in SAMPLES, not bytes. Sector is 588 samples long.
        int CachedSector;
        byte[] SectorCache = new byte[2352];

        public int FadeOutOverFrames = 0;
        public int FadeOutFramesRemaining = 0;

        public CDAudio(Disc disc, int maxVolume = short.MaxValue)
        {
            Disc = disc;
            MaxVolume = maxVolume;
        }

        public void PlayTrack(int track)
        {
            if (track < 1 || track > Disc.TOC.Sessions[0].Tracks.Count)
                return;

            StartLBA = Disc.TOC.Sessions[0].Tracks[track - 1].Indexes[1].aba - 150;
            EndLBA = StartLBA + Disc.TOC.Sessions[0].Tracks[track - 1].length_aba;
            PlayingTrack = track;
            CurrentSector = StartLBA;
            SectorOffset = 0;
            Mode = CDAudioMode.Playing;
            FadeOutOverFrames = 0;
            FadeOutFramesRemaining = 0;
            LogicalVolume = 100;
        }

        public void PlayStartingAtLba(int lba)
        {
            var point = Disc.TOC.SeekPoint(lba);
            if (point == null || point.Track == null) return;

            PlayingTrack = point.TrackNum;
            StartLBA = lba;
            EndLBA = point.Track.Indexes[1].aba + point.Track.length_aba - 150;

            CurrentSector = StartLBA;
            SectorOffset = 0;
            Mode = CDAudioMode.Playing;
            FadeOutOverFrames = 0;
            FadeOutFramesRemaining = 0;
            LogicalVolume = 100;
        }

        public void Stop()
        {
            Mode = CDAudioMode.Stopped;
            FadeOutOverFrames = 0;
            FadeOutFramesRemaining = 0;
            LogicalVolume = 100;
        }

        public void Pause()
        {
            if (Mode != CDAudioMode.Playing)
                return;
            Mode = CDAudioMode.Paused;
            FadeOutOverFrames = 0;
            FadeOutFramesRemaining = 0;
            LogicalVolume = 100;
        }

        public void Resume()
        {
            if (Mode != CDAudioMode.Paused)
                return;
            Mode = CDAudioMode.Playing;
        }

        public void PauseResume()
        {
            if (Mode == CDAudioMode.Playing) Mode = CDAudioMode.Paused;
            else if (Mode == CDAudioMode.Paused) Mode = CDAudioMode.Playing;
            else if (Mode == CDAudioMode.Stopped) return;
        }

        public void FadeOut(int frames)
        {
            FadeOutOverFrames = frames;
            FadeOutFramesRemaining = frames;
        }

        void EnsureSector()
        {
            if (CachedSector != CurrentSector)
            {
                if (CurrentSector >= Disc.LBACount)
                    Array.Clear(SectorCache, 0, 2352); // request reading past end of available disc
                else
                    Disc.ReadLBA_2352(CurrentSector, SectorCache, 0);
                CachedSector = CurrentSector;
            }
        }

        public void GetSamples(short[] samples)
        {
            if (Mode != CDAudioMode.Playing)
                return;

            if (FadeOutFramesRemaining > 0)
            {
                FadeOutFramesRemaining--;
                LogicalVolume = FadeOutFramesRemaining * 100 / FadeOutOverFrames;
            }

            EnsureSector();

            int sampleLen = samples.Length / 2;
            int offset = 0;
            for (int s = 0; s < sampleLen; s++)
            {
                int sectorOffset = SectorOffset * 4;
                short left = (short)((SectorCache[sectorOffset + 1] << 8) | (SectorCache[sectorOffset + 0]));
                short right = (short)((SectorCache[sectorOffset + 3] << 8) | (SectorCache[sectorOffset + 2]));

                samples[offset++] += (short) (left * LogicalVolume / 100 * MaxVolume / short.MaxValue);
                samples[offset++] += (short) (right * LogicalVolume / 100 * MaxVolume / short.MaxValue);
                SectorOffset++;

                if (SectorOffset == 588)
                {
                    CurrentSector++;
                    SectorOffset = 0;

                    if (CurrentSector == EndLBA)
                    {
                        switch (PlayMode)
                        {
                            case PlaybackMode.NextTrackOnCompletion:
                                PlayTrack(PlayingTrack + 1);
                                break;

                            case PlaybackMode.StopOnCompletion:
                                Stop();
                                return;

                            case PlaybackMode.LoopOnCompletion:
                                CurrentSector = StartLBA;
                                break;

                            case PlaybackMode.CallbackOnCompletion:
                                CallbackAction();
                                if (Mode != CDAudioMode.Playing)
                                    return;
                                break;
                        }
                    }

                    EnsureSector();
                }
            }
        }

        public short VolumeLeft
        {
            get
            {
                if (Mode != CDAudioMode.Playing)
                    return 0;

                int offset = SectorOffset * 4;
                short sample = (short)((SectorCache[offset + 1] << 8) | (SectorCache[offset + 0]));
                return (short) (sample * LogicalVolume / 100);
            }
        }

        public short VolumeRight
        {
            get
            {
                if (Mode != CDAudioMode.Playing)
                    return 0;

                int offset = SectorOffset * 4;
                short sample = (short) ((SectorCache[offset + 3] << 8) | (SectorCache[offset + 2]));
                return (short)(sample * LogicalVolume / 100);
            }
        }

        public void DiscardSamples() { }

        public void SaveStateText(TextWriter writer)
        {
            writer.WriteLine("[CDAudio]");
            writer.WriteLine("Mode "+ Enum.GetName(typeof(CDAudioMode), Mode));
            writer.WriteLine("PlayMode "+ Enum.GetName(typeof(PlaybackMode), PlayMode));
            writer.WriteLine("LogicalVolume {0}", LogicalVolume);
            writer.WriteLine("StartLBA {0}", StartLBA);
            writer.WriteLine("EndLBA {0}", EndLBA);
            writer.WriteLine("PlayingTrack {0}", PlayingTrack);
            writer.WriteLine("CurrentSector {0}", CurrentSector);
            writer.WriteLine("SectorOffset {0}", SectorOffset);
            writer.WriteLine("FadeOutOverFrames {0}", FadeOutOverFrames);
            writer.WriteLine("FadeOutFramesRemaining {0}", FadeOutFramesRemaining);
            writer.WriteLine("[/CDAudio]");
            writer.WriteLine();
        }

        public void LoadStateText(TextReader reader)
        {
            while (true)
            {
                string[] args = reader.ReadLine().Split(' ');
                if (args[0].Trim() == "") continue;
                if (args[0] == "[/CDAudio]") break;
                if (args[0] == "Mode")
                    Mode = (CDAudioMode) Enum.Parse(typeof (CDAudioMode), args[1]);
                else if (args[0] == "PlayMode")
                    PlayMode = (PlaybackMode) Enum.Parse(typeof (PlaybackMode), args[1]);
                else if (args[0] == "LogicalVolume")
                    LogicalVolume = int.Parse(args[1]);
                else if (args[0] == "StartLBA")
                    StartLBA = int.Parse(args[1]);
                else if (args[0] == "EndLBA")
                    EndLBA = int.Parse(args[1]);
                else if (args[0] == "PlayingTrack")
                    PlayingTrack = int.Parse(args[1]);
                else if (args[0] == "CurrentSector")
                    CurrentSector = int.Parse(args[1]);
                else if (args[0] == "SectorOffset")
                    SectorOffset = int.Parse(args[1]);
                else if (args[0] == "FadeOutOverFrames")
                    FadeOutOverFrames = int.Parse(args[1]);
                else if (args[0] == "FadeOutFramesRemaining")
                    FadeOutFramesRemaining = int.Parse(args[1]);

                else
                    Console.WriteLine("Skipping unrecognized identifier " + args[0]);
            }
            EnsureSector();
        }

        public void SaveStateBinary(BinaryWriter writer)
        {
            writer.Write((byte)Mode);
            writer.Write((byte)PlayMode);
            writer.Write(LogicalVolume);
            writer.Write(CurrentSector);
            writer.Write(SectorOffset);
            writer.Write(StartLBA);
            writer.Write(EndLBA);
            writer.Write(PlayingTrack);
            writer.Write((short)FadeOutOverFrames);
            writer.Write((short)FadeOutFramesRemaining);
        }
        
        public void LoadStateBinary(BinaryReader reader)
        {
            Mode = (CDAudioMode) reader.ReadByte();
            PlayMode = (PlaybackMode) reader.ReadByte();
            LogicalVolume = reader.ReadInt32();
            CurrentSector = reader.ReadInt32();
            SectorOffset = reader.ReadInt32();
            StartLBA = reader.ReadInt32();
            EndLBA = reader.ReadInt32();
            PlayingTrack = reader.ReadInt32();
            FadeOutOverFrames = reader.ReadInt16();
            FadeOutFramesRemaining = reader.ReadInt16();
        }
    }
}