using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
    /// <summary>
    /// Object that represents logical disk media once imported
    /// </summary>
    public class DiskImage
    {
        #region File Header Info

        /// <summary>
        /// The identified format
        /// (in the case of .dsk files, this is probably "EXTENDED CPC DSK")
        /// </summary>
        public string Header_SystemIdent { get; set; }

        /// <summary>
        /// The software originally used to create thsi image
        /// </summary>
        public string Header_CreatorSoftware { get; set; }

        /// <summary>
        /// The number of tracks in the disk image (header specified)
        /// </summary>
        public int Header_TrackCount { get; set; }

        /// <summary>
        /// The number of sides on the disk (header specified)
        /// </summary>
        public int Header_SideCount { get; set; }

        /// <summary>
        /// Size of a track including 0x100 byte track information block
        /// (in a .dsk all tracks will be the same size)
        /// </summary>
        public int Header_TrackSize { get; set; }

        #endregion

        public List<Track> Tracks = new List<Track>();

        /// <summary>
        /// Reads an entire sector
        /// </summary>
        /// <param name="side"></param>
        /// <param name="track"></param>
        /// <param name="sector"></param>
        /// <returns></returns>
        public CHRN ReadSector(int side, int track, int sector)
        {
            var t = Tracks.Where(a => a.TrackNumber == track).FirstOrDefault();

            if (t == null)
                return null;

            var s = t.Sectors.Where(a => a.SectorID == sector).FirstOrDefault();

            if (s == null)
                return null;

            var chrn = s.GetCHRN();
            chrn.DataBytes = s.SectorData;

            return chrn;
        }

        /// <summary>
        /// Reads data from a sector based on offset and length
        /// </summary>
        /// <param name="side"></param>
        /// <param name="track"></param>
        /// <param name="sector"></param>
        /// <returns></returns>
        public CHRN ReadSector(int side, int track, int sector, int offset, int length)
        {
            var result = ReadSector(side, track, sector);

            if (result == null)
                return null;

            if (offset > result.DataBytes.Length)
                return null;

            if (length + offset > result.DataBytes.Length)
                return null;

            // data is safe to read
            var data = result.DataBytes.Skip(offset).Take(length).ToArray();

            result.DataBytes = data;

            return result;
        }

        /// <summary>
        /// Reads an entire track
        /// </summary>
        /// <param name="side"></param>
        /// <param name="track"></param>
        /// <returns></returns>
        public Track ReadTrack(int side, int track)
        {
            return Tracks.Where(a => a.TrackNumber == track).FirstOrDefault();
        }

        /// <summary>
        /// Reads a track based on offset and length
        /// </summary>
        /// <param name="side"></param>
        /// <param name="track"></param>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public Track ReadTrack(int side, int track, int offset, int length)
        {
            var t = ReadTrack(side, track);

            if (t == null)
                return null;

            if (offset >= t.AllSectorBytes.Count() || offset + length >= t.AllSectorBytes.Count())
                return null;

            var data = t.AllSectorBytes.Skip(offset).Take(length).ToArray();

            t.ResultBytes = data;

            return t;
        }

        /// <summary>
        /// Returns the number of tracks preset on the disk
        /// </summary>
        /// <returns></returns>
        public int GetTrackCount()
        {
            return Tracks.Count();
        }        
    }
}
