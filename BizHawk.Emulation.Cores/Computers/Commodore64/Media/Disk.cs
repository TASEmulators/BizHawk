using System;
using System.Collections.Generic;
using System.Linq;
using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Computers.Commodore64.Media
{
	public sealed class Disk
	{
        [SaveState.DoNotSave] public const int FluxBitsPerEntry = 32;
        [SaveState.DoNotSave] public const int FluxBitsPerTrack = 16000000 / 5;
        [SaveState.DoNotSave] public const int FluxEntriesPerTrack = FluxBitsPerTrack/FluxBitsPerEntry;
        [SaveState.DoNotSave] private int[][] _tracks;
	    [SaveState.DoNotSave] private readonly int[] _originalMedia;
		[SaveState.DoNotSave] public bool Valid;
	    [SaveState.SaveWithName("DiskIsWriteProtected")] public bool WriteProtected;

        /// <summary>
        /// Create a blank, unformatted disk.
        /// </summary>
	    public Disk(int trackCapacity)
        {
            WriteProtected = false;
            _tracks = new int[trackCapacity][];
            FillMissingTracks();
            _originalMedia = SerializeTracks(_tracks);
            Valid = true;
        }

        /// <summary>
        /// Create an expanded representation of a magnetic disk.
        /// </summary>
        /// <param name="trackData">Raw bit data.</param>
        /// <param name="trackNumbers">Track numbers for the raw bit data.</param>
        /// <param name="trackDensities">Density zones for the raw bit data.</param>
        /// <param name="trackLengths">Length, in bits, of each raw bit data.</param>
        /// <param name="trackCapacity">Total number of tracks on the media.</param>
	    public Disk(IList<byte[]> trackData, IList<int> trackNumbers, IList<int> trackDensities, int trackCapacity)
	    {
            WriteProtected = true;
            _tracks = new int[trackCapacity][];
            for (var i = 0; i < trackData.Count; i++)
            {
                _tracks[trackNumbers[i]] = ConvertToFluxTransitions(trackDensities[i], trackData[i], 0);
            }
            FillMissingTracks();
            Valid = true;
            _originalMedia = SerializeTracks(_tracks);
        }

        private int[] ConvertToFluxTransitions(int density, byte[] bytes, int fluxBitOffset)
        {
            var paddedLength = bytes.Length;
            switch (density)
            {
                case 3:
                    paddedLength = Math.Max(bytes.Length, 7692);
                    break;
                case 2:
                    paddedLength = Math.Max(bytes.Length, 7142);
                    break;
                case 1:
                    paddedLength = Math.Max(bytes.Length, 6666);
                    break;
                case 0:
                    paddedLength = Math.Max(bytes.Length, 6250);
                    break;
            }

            paddedLength++;
            var paddedBytes = new byte[paddedLength];
            Array.Copy(bytes, paddedBytes, bytes.Length);
            for (var i = bytes.Length; i < paddedLength; i++)
            {
                paddedBytes[i] = 0xAA;
            }
	        var result = new int[FluxEntriesPerTrack];
	        var lengthBits = (paddedLength * 8) - 7;
            var offsets = new List<long>();
            var remainingBits = lengthBits;

	        const long bitsNum = FluxEntriesPerTrack * FluxBitsPerEntry;
	        long bitsDen = lengthBits;

            for (var i = 0; i < paddedLength; i++)
	        {
	            var byteData = paddedBytes[i];
                for (var j = 0; j < 8; j++)
                {
                    var offset = fluxBitOffset + ((i * 8 + j) * bitsNum / bitsDen);
                    var byteOffset = (int)(offset / FluxBitsPerEntry);
                    var bitOffset = (int)(offset % FluxBitsPerEntry);
                    offsets.Add(offset);
                    result[byteOffset] |= ((byteData & 0x80) != 0 ? 1 : 0) << bitOffset;
	                byteData <<= 1;
                    remainingBits--;
                    if (remainingBits <= 0)
                        break;
                }
                if (remainingBits <= 0)
                    break;
            }

            return result;
	    }

	    private void FillMissingTracks()
	    {
            // Fill half tracks (should assist with EA "fat-track" protections)
	        for (var i = 1; i < _tracks.Length; i += 2)
	        {
	            if (_tracks[i] == null && _tracks[i - 1] != null)
	            {
	                _tracks[i] = new int[FluxEntriesPerTrack];
                    Array.Copy(_tracks[i - 1], _tracks[i], FluxEntriesPerTrack);
	            }
	        }

            // Fill vacant tracks
	        for (var i = 0; i < _tracks.Length; i++)
	        {
	            if (_tracks[i] == null)
	            {
	                _tracks[i] = new int[FluxEntriesPerTrack];
	            }
            }
	    }

	    public int[] GetDataForTrack(int halftrack)
	    {
	        return _tracks[halftrack];
	    }

	    /// <summary>
	    /// Combine the tracks into a single bitstream.
	    /// </summary>
	    private int[] SerializeTracks(int[][] tracks)
	    {
	        var trackCount = tracks.Length;
	        var result = new int[trackCount * FluxEntriesPerTrack];
	        for (var i = 0; i < trackCount; i++)
	        {
	            Array.Copy(tracks[i], 0, result, i * FluxEntriesPerTrack, FluxEntriesPerTrack);
	        }
	        return result;
	    }

        /// <summary>
        /// Split a bitstream into tracks.
        /// </summary>
	    private int[][] DeserializeTracks(int[] data)
        {
            var trackCount = data.Length/FluxEntriesPerTrack;
            var result = new int[trackCount][];
            for (var i = 0; i < trackCount; i++)
            {
                result[i] = new int[FluxEntriesPerTrack];
                Array.Copy(data, i * FluxEntriesPerTrack, result[i], 0, FluxEntriesPerTrack);
            }
            return result;
        }

        public void SyncState(Serializer ser)
        {
            if (ser.IsReader)
            {
                var mediaState = new int[_originalMedia.Length];
                SaveState.SyncDelta("MediaState", ser, _originalMedia, ref mediaState);
                _tracks = DeserializeTracks(mediaState);
            }
            else if (ser.IsWriter)
            {
                var mediaState = SerializeTracks(_tracks);
                SaveState.SyncDelta("MediaState", ser, _originalMedia, ref mediaState);
            }
            SaveState.SyncObject(ser, this);
        }
    }
}
