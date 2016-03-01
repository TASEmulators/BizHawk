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
        [SaveState.DoNotSave] private readonly List<int[]> _tracks;
	    [SaveState.DoNotSave] private readonly int[] _originalMedia;
		[SaveState.DoNotSave] public bool Valid;

        /// <summary>
        /// Create a blank, unformatted disk.
        /// </summary>
	    public Disk(int trackCapacity)
	    {
            _tracks = new List<int[]>(trackCapacity);
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
	    public Disk(IList<byte[]> trackData, IList<int> trackNumbers, IList<int> trackDensities, IList<int> trackLengths, int trackCapacity)
	    {
            _tracks = Enumerable.Repeat<int[]>(null, trackCapacity).ToList();
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
            var paddedBytes = new byte[bytes.Length + 1];
            Array.Copy(bytes, paddedBytes, bytes.Length);
            paddedBytes[paddedBytes.Length - 1] = 0x00;
	        var result = new int[FluxEntriesPerTrack];
	        var length = paddedBytes.Length;
	        var lengthBits = length*8+7;
            var offsets = new List<long>();
            var remainingBits = lengthBits;

	        const long bitsNum = FluxEntriesPerTrack * FluxBitsPerEntry;
	        long bitsDen = lengthBits;

            for (var i = 0; i < length; i++)
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
	            }

	            remainingBits--;
	            if (remainingBits <= 0)
                    break;
            }

            return result;
	    }

	    private void FillMissingTracks()
	    {
	        for (var i = 0; i < _tracks.Count; i++)
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
	    private int[] SerializeTracks(IEnumerable<int[]> tracks)
	    {
	        return tracks.SelectMany(t => t).ToArray();
	    }

        /// <summary>
        /// Split a bitstream into tracks.
        /// </summary>
	    private IEnumerable<int[]> DeserializeTracks(int[] data)
        {
            var trackCount = data.Length/FluxEntriesPerTrack;
            for (var i = 0; i < trackCount; i++)
            {
                yield return data.Skip(i*FluxEntriesPerTrack).Take(FluxEntriesPerTrack).ToArray();
            }
	    }

        public void SyncState(Serializer ser)
        {
            if (ser.IsReader)
            {
                var mediaState = new int[_originalMedia.Length];
                SaveState.SyncDeltaInts("MediaState", ser, _originalMedia, ref mediaState);
                _tracks.Clear();
                _tracks.AddRange(DeserializeTracks(mediaState));
            }
            else if (ser.IsWriter)
            {
                var mediaState = SerializeTracks(_tracks);
                SaveState.SyncDeltaInts("MediaState", ser, _originalMedia, ref mediaState);
            }
            SaveState.SyncObject(ser, this);
        }
    }
}
