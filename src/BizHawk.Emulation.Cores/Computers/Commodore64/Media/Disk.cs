using System.Collections.Generic;

using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Computers.Commodore64.Media
{
	public sealed class Disk
	{
		public const int FluxBitsPerEntry = 32;
		public const int FluxBitsPerTrack = 16000000 / 5;
		public const int FluxEntriesPerTrack = FluxBitsPerTrack / FluxBitsPerEntry;
		private readonly DiskTrack[] _tracks;
		private bool[] _usedTracks;
		public bool Valid;
		public bool WriteProtected;

		/// <summary>
		/// Create a blank, unformatted disk.
		/// </summary>
		public Disk(int trackCount)
		{
			WriteProtected = false;
			_tracks = new DiskTrack[trackCount];
			_usedTracks = new bool[trackCount];
			FillMissingTracks();
			Valid = true;
		}

		/// <summary>
		/// Create an expanded representation of a magnetic disk.
		/// </summary>
		/// <param name="trackData">Raw bit data.</param>
		/// <param name="trackNumbers">Track numbers for the raw bit data.</param>
		/// <param name="trackDensities">Density zones for the raw bit data.</param>
		/// <param name="trackCapacity">Total number of tracks on the media.</param>
		public Disk(IList<byte[]> trackData, IList<int> trackNumbers, IList<int> trackDensities, int trackCapacity)
		{
			WriteProtected = true;
			_tracks = new DiskTrack[trackCapacity];
			_usedTracks = new bool[trackCapacity];
			for (var i = 0; i < trackData.Count; i++)
			{
				var track = new DiskTrack();
				track.ReadFromGCR(trackDensities[i], trackData[i], 0);
				_tracks[trackNumbers[i]] = track;
			}

			FillMissingTracks();
			Valid = true;
		}

		private void FillMissingTracks()
		{
			// Fill half tracks (should assist with EA "fat-track" protections)
			for (var i = 1; i < _tracks.Length; i += 2)
			{
				if (_tracks[i] == null && _tracks[i - 1] != null)
				{
					_tracks[i] = _tracks[i - 1].Clone();
				}
			}

			// Fill vacant tracks
			for (var i = 0; i < _tracks.Length; i++)
			{
				_tracks[i] ??= new();
			}
		}

		/// <summary>
		/// Generic update of the deltas stored in Drive1541's ISaveRam implementation.
		/// deltaUpdateCallback will be called for each track which has been possibly dirtied
		/// </summary>
		/// <param name="deltaUpdateCallback">callback</param>
		public void DeltaUpdate(Action<int, DiskTrack> deltaUpdateCallback)
		{
			for (var i = 0; i < _tracks.Length; i++)
			{
				if (_usedTracks[i])
				{
					deltaUpdateCallback(i, _tracks[i]);
				}
			}
		}

		public DiskTrack GetTrack(int trackNumber)
			=> _tracks[trackNumber];

		public void SyncState(Serializer ser)
		{
			ser.Sync(nameof(WriteProtected), ref WriteProtected);
		}
	}
}
