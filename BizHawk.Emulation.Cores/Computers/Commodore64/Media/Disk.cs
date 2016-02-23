using System;
using System.Collections.Generic;
using System.Linq;
using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Computers.Commodore64.Media
{
	public sealed class Disk
	{
        public const int FLUX_BITS_PER_ENTRY = 32;
        public const int FLUX_BITS_PER_TRACK = 16000000 / 5;
        public const int FLUX_ENTRIES_PER_TRACK = FLUX_BITS_PER_TRACK/FLUX_BITS_PER_ENTRY;

        private class Track
        {
            public int Index;
            public bool Present;
            public int[] FluxData;
        }

        [SaveState.DoNotSave] private readonly Track[] _tracks;
		[SaveState.DoNotSave] public bool Valid;

        /// <summary>
        /// Create a blank, unformatted disk.
        /// </summary>
	    public Disk(int trackCapacity)
	    {
            _tracks = new Track[trackCapacity];
            FillMissingTracks();
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
            _tracks = new Track[trackCapacity];
            for (var i = 0; i < trackData.Count; i++)
            {
                var track = new Track
                {
                    Index = trackNumbers[i],
                    Present = true,
                    FluxData = ConvertToFluxTransitions(trackDensities[i], trackData[i], 0)
                };
                _tracks[trackNumbers[i]] = track;
            }
            FillMissingTracks();
            Valid = true;
	    }

	    private int[] ConvertToFluxTransitions(int density, byte[] bytes, int fluxBitOffset)
	    {
	        var result = new int[FLUX_ENTRIES_PER_TRACK];
	        var length = bytes.Length;
	        var lengthBits = length*8;
            var offsets = new List<long>();

	        const long bitsNum = FLUX_ENTRIES_PER_TRACK * FLUX_BITS_PER_ENTRY;
	        long bitsDen = lengthBits;

            for (var i = 0; i < length; i++)
	        {
	            var byteData = bytes[i];
                for (var j = 0; j < 8; j++)
                {
                    var offset = fluxBitOffset + ((i * 8 + j) * bitsNum / bitsDen);
                    var byteOffset = (int)(offset / FLUX_BITS_PER_ENTRY);
                    var bitOffset = (int)(offset % FLUX_BITS_PER_ENTRY);
                    offsets.Add(offset);
                    result[byteOffset] |= ((byteData & 0x80) != 0 ? 1 : 0) << bitOffset;
	                byteData <<= 1;
	            }
	        }

	        return result;
	    }

	    private void FillMissingTracks()
	    {
	        for (var i = 0; i < _tracks.Length; i++)
	        {
	            if (_tracks[i] == null)
	            {
	                _tracks[i] = new Track
	                {
	                    Index = i,
                        FluxData = new int[FLUX_ENTRIES_PER_TRACK]
	                };
	            }
            }
	    }

	    public int[] GetDataForTrack(int halftrack)
	    {
	        return _tracks[halftrack].FluxData;
	    }

	    public IEnumerable<int> GetPresentTracks()
	    {
	        return _tracks.Where(t => t.Present).Select(t => t.Index);
	    } 

        public void SyncState(Serializer ser)
        {
            SaveState.SyncObject(ser, this);
        }
    }
}
