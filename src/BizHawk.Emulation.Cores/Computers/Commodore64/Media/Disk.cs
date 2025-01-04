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
		public bool Valid;
		public bool WriteProtected;

		/// <summary>
		/// Create a blank, unformatted disk.
		/// </summary>
		public Disk(int trackCount)
		{
			WriteProtected = false;
			_tracks = new DiskTrack[trackCount];
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
					_tracks[i] = _tracks[i - 1].Clone();
			}

			// Fill vacant tracks
			for (var i = 0; i < _tracks.Length; i++)
			{
				if (_tracks[i] == null)
					_tracks[i] = new DiskTrack();
			}
		}

		public IReadOnlyList<DiskTrack> Tracks => _tracks;
		
		public void SyncState(Serializer ser)
		{
			ser.Sync(nameof(WriteProtected), ref WriteProtected);
		}
	}
}
