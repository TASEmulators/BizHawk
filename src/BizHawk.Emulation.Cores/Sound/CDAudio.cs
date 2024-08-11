using BizHawk.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.DiscSystem;

// The state of the cd player is quantized to the frame level.
// This isn't ideal. But life's too short. 
// I decided not to let the perfect be the enemy of the good.
// It can always be refactored. It's at least deterministic.

namespace BizHawk.Emulation.Cores.Components
{
	public sealed class CDAudio : IMixedSoundProvider
	{
		public const byte CDAudioMode_Stopped = 0;
		public const byte CDAudioMode_Playing = 1;
		public const byte CDAudioMode_Paused = 2;

		public const byte PlaybackMode_StopOnCompletion = 0;
		public const byte PlaybackMode_NextTrackOnCompletion = 1;
		public const byte PlaybackMode_LoopOnCompletion = 2;
		public const byte PlaybackMode_CallbackOnCompletion = 3;

		public Action CallbackAction = () => {};

		public Disc Disc;
		public DiscSectorReader DiscSectorReader;
		public byte Mode = CDAudioMode_Stopped;
		public byte PlayMode = PlaybackMode_LoopOnCompletion;

		public int MaxVolume { get; set; }
		public int LogicalVolume = 100;

		public int StartLBA, EndLBA;
		public int PlayingTrack;

		public int CurrentSector, SectorOffset; // Offset is in SAMPLES, not bytes. Sector is 588 samples long.
		private int CachedSector;
		private readonly byte[] SectorCache = new byte[2352];

		public int FadeOutOverFrames = 0;
		public int FadeOutFramesRemaining = 0;

		public CDAudio(Disc disc, int maxVolume = short.MaxValue)
		{
			Disc = disc;
			DiscSectorReader = new DiscSectorReader(disc);
			MaxVolume = maxVolume;
		}

		public void PlayTrack(int track)
		{
			if (track < 1 || track > Disc.Session1.InformationTrackCount)
				return;

			StartLBA = Disc.Session1.Tracks[track].LBA;
			
			//play until the beginning of the next track (?)
			EndLBA = Disc.Session1.Tracks[track + 1].LBA;

			PlayingTrack = track;
			CurrentSector = StartLBA;
			SectorOffset = 0;
			Mode = CDAudioMode_Playing;
			FadeOutOverFrames = 0;
			FadeOutFramesRemaining = 0;
			LogicalVolume = 100;
		}

		public void PlayStartingAtLba(int lba)
		{
			var track = Disc.Session1.SeekTrack(lba);
			if (track == null) return;
			// returning Leadout means no sound is playing
			// and since we don't have any information about where leadout ends
			// return in this case as well to avoid track.Number+1 below
			if (track == Disc.Session1.LeadoutTrack) return;

			PlayingTrack = track.Number;
			StartLBA = lba;

			//play until the beginning of the next track (?)
			EndLBA = Disc.Session1.Tracks[track.Number + 1].LBA;

			CurrentSector = StartLBA;
			SectorOffset = 0;
			Mode = CDAudioMode_Playing;
			FadeOutOverFrames = 0;
			FadeOutFramesRemaining = 0;
			LogicalVolume = 100;
		}

		public void Stop()
		{
			Mode = CDAudioMode_Stopped;
			FadeOutOverFrames = 0;
			FadeOutFramesRemaining = 0;
			LogicalVolume = 100;
		}

		public void Pause()
		{
			if (Mode != CDAudioMode_Playing)
				return;
			Mode = CDAudioMode_Paused;
			FadeOutOverFrames = 0;
			FadeOutFramesRemaining = 0;
			LogicalVolume = 100;
		}

		public void Resume()
		{
			if (Mode != CDAudioMode_Paused)
				return;
			Mode = CDAudioMode_Playing;
		}

		public void PauseResume()
		{
			if (Mode == CDAudioMode_Playing) Mode = CDAudioMode_Paused;
			else if (Mode == CDAudioMode_Paused) Mode = CDAudioMode_Playing;
			else if (Mode == CDAudioMode_Stopped) return;
		}

		public void FadeOut(int frames)
		{
			FadeOutOverFrames = frames;
			FadeOutFramesRemaining = frames;
		}

		private void EnsureSector()
		{
			if (CachedSector != CurrentSector)
			{
				if (CurrentSector >= Disc.Session1.LeadoutLBA)
					Array.Clear(SectorCache, 0, 2352); // request reading past end of available disc
				else
					DiscSectorReader.ReadLBA_2352(CurrentSector, SectorCache, 0);
				CachedSector = CurrentSector;
			}
		}

		public bool CanProvideAsync => true;
		public void SetSyncMode(SyncSoundMode mode)
		{
			if (mode != SyncSoundMode.Async)
			{
				throw new NotImplementedException("Only async currently supported.");
			}
		}

		public SyncSoundMode SyncMode => SyncSoundMode.Async;

		public void GetSamplesSync(out short[] samples, out int nsamp)
		{
			throw new NotImplementedException("Sync sound not yet supported");
		}

		public void GetSamplesAsync(short[] samples)
		{
			if (Mode != CDAudioMode_Playing)
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

				samples[offset++] += (short)(left * LogicalVolume / 100 * MaxVolume / short.MaxValue);
				samples[offset++] += (short)(right * LogicalVolume / 100 * MaxVolume / short.MaxValue);
				SectorOffset++;

				if (SectorOffset == 588)
				{
					CurrentSector++;
					SectorOffset = 0;

					if (CurrentSector == EndLBA)
					{
						switch (PlayMode)
						{
							case PlaybackMode_NextTrackOnCompletion:
								PlayTrack(PlayingTrack + 1);
								break;

							case PlaybackMode_StopOnCompletion:
								Stop();
								return;

							case PlaybackMode_LoopOnCompletion:
								CurrentSector = StartLBA;
								break;

							case PlaybackMode_CallbackOnCompletion:
								CallbackAction();
								if (Mode != CDAudioMode_Playing)
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
				if (Mode != CDAudioMode_Playing)
					return 0;

				int offset = SectorOffset * 4;
				short sample = (short)((SectorCache[offset + 1] << 8) | (SectorCache[offset + 0]));
				return (short)(sample * LogicalVolume / 100);
			}
		}

		public short VolumeRight
		{
			get
			{
				if (Mode != CDAudioMode_Playing)
					return 0;

				int offset = SectorOffset * 4;
				short sample = (short)((SectorCache[offset + 3] << 8) | (SectorCache[offset + 2]));
				return (short)(sample * LogicalVolume / 100);
			}
		}

		public void DiscardSamples() { }

		public void SyncState(Serializer ser)
		{
			ser.BeginSection(nameof(CDAudio));
			ser.Sync(nameof(Mode), ref Mode);
			ser.Sync(nameof(PlayMode), ref PlayMode);
			ser.Sync(nameof(LogicalVolume), ref LogicalVolume);
			ser.Sync(nameof(StartLBA), ref StartLBA);
			ser.Sync(nameof(EndLBA), ref EndLBA);
			ser.Sync(nameof(PlayingTrack), ref PlayingTrack);
			ser.Sync(nameof(CurrentSector), ref CurrentSector);
			ser.Sync(nameof(SectorOffset), ref SectorOffset);
			ser.Sync(nameof(FadeOutOverFrames), ref FadeOutOverFrames);
			ser.Sync(nameof(FadeOutFramesRemaining), ref FadeOutFramesRemaining);
			ser.EndSection();

			if (ser.IsReader)
				EnsureSector();
		}
	}
}
