using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
    /// <summary>
    /// Represents a disk track
    /// </summary>
    public class Track
    {
        /// <summary>
        /// The track number
        /// </summary>
        public byte TrackNumber { get; set; }
        /// <summary>
        /// The side number
        /// </summary>
        public byte SideNumber { get; set; }
        /// <summary>
        /// Data rate defines the rate at which data was written to the track. This value applies to the entire track
        /// </summary>
        public byte DataRate { get; set; }
        /// <summary>
        /// Recording mode is used to define how the data was written. It defines the encoding used to write the data to the disc and the structure of the data on the 
        /// disc including the layout of the sectors. This value applies to the entire track.
        /// </summary>
        public byte RecordingMode { get; set; }
        /// <summary>
        /// Used to calculate the location of each sector's data. Therefore, The data allocated for each sector must be the same
        /// </summary>
        public int SectorSize { get; set; }
        /// <summary>
        /// Identifies the number of valid entries in the sector information list
        /// </summary>
        public byte SectorCount { get; set; }
        /// <summary>
        /// The length of GAP3 data
        /// </summary>
        public byte GAP3Length { get; set; }
        /// <summary>
        /// Byte used as filler
        /// </summary>
        public byte FillerByte { get; set; }

        /// <summary>
        /// List containing all the sectors for this track
        /// </summary>
        public List<Sector> Sectors = new List<Sector>();

        /// <summary>
        /// Defines the offset in the disk image file at which the track information block starts
        /// </summary>
        public int TrackStartOffset { get; set; }

        /// <summary>
        /// The actual length of the track (including info block) in the disk image file
        /// </summary>
        public int TrackByteLength { get; set; }

        /// <summary>
        /// Returns all sector data for this track in one byte array
        /// </summary>
        public byte[] AllSectorBytes
        {
            get
            {
                int byteSize = 0;
                foreach (var s in Sectors)
                {
                    byteSize += s.SectorData.Count();
                }

                byte[] all = new byte[byteSize];

                int _pos = 0;

                foreach (var s in Sectors)
                {
                    foreach (var d in s.SectorData)
                    {
                        all[_pos] = d;
                        _pos++;
                    }
                }

                return all;
            }
        }

        public byte[] ResultBytes { get; set; }
    }
}
