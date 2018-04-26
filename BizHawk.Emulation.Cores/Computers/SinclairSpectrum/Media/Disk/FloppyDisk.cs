using BizHawk.Common;
using BizHawk.Common.NumberExtensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
    /// <summary>
    /// This interface defines a logical floppy disk
    /// </summary>
    public abstract class FloppyDisk
    {
        /// <summary>
        /// The disk format type
        /// </summary>
        public abstract DiskType DiskFormatType { get; }

        /// <summary>
        /// Disk information header
        /// </summary>
        public Header DiskHeader = new Header();

        /// <summary>
        /// Track array
        /// </summary>
        public Track[] DiskTracks = null;

        /// <summary>
        /// No. of tracks per side
        /// </summary>
        public int CylinderCount;
        
        /// <summary>
        /// The number of physical sides
        /// </summary>
        public int SideCount;

        /// <summary>
        /// The number of bytes per track
        /// </summary>
        public int BytesPerTrack;

        /// <summary>
        /// The write-protect tab on the disk
        /// </summary>
        public bool WriteProtected;

        /// <summary>
        /// The detected protection scheme (if any)
        /// </summary>
        public ProtectionType Protection;

        /// <summary>
        /// The actual disk image data
        /// </summary>
        public byte[] DiskData;

        /// <summary>
        /// If TRUE then data on the disk has changed (been written to)
        /// This will be used to determine whether the disk data needs to be included
        /// in any SyncState operations
        /// </summary>
        protected bool DirtyData = false;

        /// <summary>
        /// Used to deterministically choose a 'random' sector when dealing with weak reads
        /// </summary>
        public int RandomCounter
        {
            get { return _randomCounter; }
            set
            {
                _randomCounter = value;

                foreach (var trk in DiskTracks)
                {
                    foreach (var sec in trk.Sectors)
                    {
                        sec.RandSecCounter = _randomCounter;
                    }
                }
            }
        }
        protected int _randomCounter;


        /// <summary>
        /// Attempts to parse incoming disk data
        /// </summary>
        /// <param name="diskData"></param>
        /// <returns>
        /// TRUE:   disk parsed
        /// FALSE:  unable to parse disk
        /// </returns>
        public virtual bool ParseDisk(byte[] diskData)
        {
            // default result
            // override in inheriting class
            return false;
        }

        /// <summary>
        /// Should be run at the end of the ParseDisk process
        /// If speedlock is detected the flag is set in the disk image
        /// </summary>
        /// <returns></returns>
        protected virtual void SpeedlockDetection()
        {
            /*
                Based on the information here:  http://simonowen.com/samdisk/plus3/      
            */

            if (DiskTracks.Length == 0)
                return;

            // check for speedlock copyright notice
            string ident = Encoding.ASCII.GetString(DiskData, 0x100, 0x1400);
            if (!ident.ToUpper().Contains("SPEEDLOCK"))
            {
                // speedlock not found
                return;
            }

            // get cylinder 0
            var cyl = DiskTracks[0];

            // get sector with ID=2
            var sec = cyl.Sectors.Where(a => a.SectorID == 2).FirstOrDefault();

            if (sec == null)
                return;

            // check for already multiple weak copies
            if (sec.ContainsMultipleWeakSectors || sec.SectorData.Length != 0x80 << sec.SectorSize)
                return;

            // check for invalid crcs in sector 2
            if (sec.Status1.Bit(5) || sec.Status2.Bit(5))
            {
                Protection = ProtectionType.Speedlock;
            }
            else
            {
                return;
            }

            // we are going to create a total of 5 weak sector copies
            // keeping the original copy
            byte[] origData = sec.SectorData.ToArray();
            List<byte> data = new List<byte>();
            //Random rnd = new Random();

            for (int i = 0; i < 6; i++)
            {
                for (int s = 0; s < origData.Length; s++)
                {
                    if (i == 0)
                    {
                        data.Add(origData[s]);
                        continue;
                    }

                    // deterministic 'random' implementation
                    int n = origData[s] + i + 1;
                    if (n > 0xff)
                        n = n - 0xff;
                    else if (n < 0)
                        n = 0xff + n;

                    byte nByte = (byte)n;

                    if (s < 336)
                    {
                        // non weak data
                        data.Add(origData[s]);
                    }
                    else if (s < 511)
                    {
                        // weak data
                        data.Add(nByte);
                    }
                    else if (s == 511)
                    {
                        // final sector byte
                        data.Add(nByte);
                    }
                    else
                    {
                        // speedlock sector should not be more than 512 bytes
                        // but in case it is just do non weak
                        data.Add(origData[i]);
                    }
                }
            }

            // commit the sector data
            sec.SectorData = data.ToArray();
            sec.ContainsMultipleWeakSectors = true;
            sec.ActualDataByteLength = data.Count();

        }

        /// <summary>
        /// Returns the track count for the disk
        /// </summary>
        /// <returns></returns>
        public virtual int GetTrackCount()
        {
            return DiskHeader.NumberOfTracks * DiskHeader.NumberOfSides;
        }

        /// <summary>
        /// Reads the current sector ID info
        /// </summary>
        /// <param name="track"></param>
        /// <returns></returns>
        public virtual CHRN ReadID(byte trackIndex, byte side, int sectorIndex)
        {
            if (side != 0)
                return null;

            if (DiskTracks.Length <= trackIndex || trackIndex < 0)
            {
                // invalid track - wrap around
                trackIndex = 0;
            }

            var track = DiskTracks[trackIndex];

            if (track.NumberOfSectors <= sectorIndex)
            {
                // invalid sector - wrap around
                sectorIndex = 0;
            }

            var sector = track.Sectors[sectorIndex];

            CHRN chrn = new CHRN();

            chrn.C = sector.TrackNumber;
            chrn.H = sector.SideNumber;
            chrn.R = sector.SectorID;

            // wrap around for N > 7
            if (sector.SectorSize > 7)
            {
                chrn.N = (byte)(sector.SectorSize - 7);
            }
            else if (sector.SectorSize < 0)
            {
                chrn.N = 0;
            }
            else
            {
                chrn.N = sector.SectorSize;
            }

            chrn.Flag1 = (byte)(sector.Status1 & 0x25);
            chrn.Flag2 = (byte)(sector.Status2 & 0x61);

            chrn.DataBytes = sector.ActualData;

            return chrn;
        }

        /// <summary>
        /// State serialization routines
        /// </summary>
        /// <param name="ser"></param>
        public abstract void SyncState(Serializer ser);


        public class Header
        {
            public string DiskIdent { get; set; }
            public string DiskCreatorString { get; set; }
            public byte NumberOfTracks { get; set; }
            public byte NumberOfSides { get; set; }
            public int[] TrackSizes { get; set; }
        }

        public class Track
        {
            public string TrackIdent { get; set; }
            public byte TrackNumber { get; set; }
            public byte SideNumber { get; set; }
            public byte DataRate { get; set; }
            public byte RecordingMode { get; set; }
            public byte SectorSize { get; set; }
            public byte NumberOfSectors { get; set; }
            public byte GAP3Length { get; set; }
            public byte FillerByte { get; set; }
            public Sector[] Sectors { get; set; }

            /// <summary>
            /// Presents a contiguous byte array of all sector data for this track
            /// (including any multiple weak/random data)
            /// </summary>
            public byte[] TrackSectorData
            {
                get
                {
                    List<byte> list = new List<byte>();

                    foreach (var sec in Sectors)
                    {
                        list.AddRange(sec.ActualData);
                    }

                    return list.ToArray();
                }
            }
        }

        public class Sector
        {
            public byte TrackNumber { get; set; }
            public byte SideNumber { get; set; }
            public byte SectorID { get; set; }
            public byte SectorSize { get; set; }
            public byte Status1 { get; set; }
            public byte Status2 { get; set; }
            public int ActualDataByteLength { get; set; }
            public byte[] SectorData { get; set; }
            public bool ContainsMultipleWeakSectors { get; set; }

            public int RandSecCounter = 0;

            public byte[] ActualData
            {
                get
                {
                    if (!ContainsMultipleWeakSectors)
                    {
                        // check whether filler bytes are needed
                        int size = 0x80 << SectorSize;
                        if (size > ActualDataByteLength)
                        {
                            List<byte> l = new List<byte>();
                            l.AddRange(SectorData);
                            for (int i = 0; i < size - ActualDataByteLength; i++)
                            {
                                l.Add(0xe5);
                            }

                            return l.ToArray();
                        }
                        else
                        {
                            return SectorData;
                        }
                    }
                    else
                    {
                        int copies = ActualDataByteLength / (0x80 << SectorSize);
                        Random rnd = new Random();
                        int r = rnd.Next(0, copies - 1);
                        int step = r * (0x80 << SectorSize);
                        byte[] res = new byte[(0x80 << SectorSize)];
                        Array.Copy(SectorData, step, res, 0, 0x80 << SectorSize);
                        return res;
                    }
                }
            }

            public CHRN SectorIDInfo
            {
                get
                {
                    return new CHRN
                    {
                        C = TrackNumber,
                        H = SideNumber,
                        R = SectorID,
                        N = SectorSize,
                        Flag1 = Status1,
                        Flag2 = Status2,                         
                    };
                }
            }
        }
    }

    /// <summary>
    /// Defines the type of speedlock detection found
    /// </summary>
    public enum ProtectionType
    {
        None,
        Speedlock
    }

}
