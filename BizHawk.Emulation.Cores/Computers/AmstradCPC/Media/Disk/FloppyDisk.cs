using BizHawk.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Cores.Computers.AmstradCPC
{
    /// <summary>
    /// This abstract class defines a logical floppy disk
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
        /// Examines the floppydisk data to work out what protection (if any) is present
        /// If possible it will also fix the disk data for this protection
        /// This should be run at the end of the ParseDisk() method
        /// </summary>
        public virtual void ParseProtection()
        {
            int[] weakArr = new int[2];

            // speedlock
            if (DetectSpeedlock(ref weakArr))
            {
                Protection = ProtectionType.Speedlock;

                Sector sec = DiskTracks[0].Sectors[1];
                if (!sec.ContainsMultipleWeakSectors)
                {
                    byte[] origData = sec.SectorData.ToArray();
                    List<byte> data = new List<byte>();
                    for (int m = 0; m < 3; m++)
                    {
                        for (int i = 0; i < 512; i++)
                        {
                            // deterministic 'random' implementation
                            int n = origData[i] + m + 1;
                            if (n > 0xff)
                                n = n - 0xff;
                            else if (n < 0)
                                n = 0xff + n;

                            byte nByte = (byte)n;

                            if (m == 0)
                            {
                                data.Add(origData[i]);
                                continue;
                            }

                            if (i < weakArr[0])
                            {
                                data.Add(origData[i]);
                            }
                            
                            else if (weakArr[1] > 0)
                            {
                                data.Add(nByte);
                                weakArr[1]--;
                            }
                            
                            else
                            {
                                data.Add(origData[i]);
                            }
                        }
                    }

                    sec.SectorData = data.ToArray();
                    sec.ActualDataByteLength = data.Count();
                    sec.ContainsMultipleWeakSectors = true;
                }
            }
            else if (DetectAlkatraz(ref weakArr))
            {
                Protection = ProtectionType.Alkatraz;
            }
            else if (DetectPaulOwens(ref weakArr))
            {
                Protection = ProtectionType.PaulOwens;
            }
            else if (DetectHexagon(ref weakArr))
            {
                Protection = ProtectionType.Hexagon;
            }
            else if (DetectShadowOfTheBeast())
            {
                Protection = ProtectionType.ShadowOfTheBeast;
            }
        }

        /// <summary>
        /// Detection routine for shadow of the beast game
        /// Still cannot get this to work, but at least the game is detected
        /// </summary>
        /// <returns></returns>
        public bool DetectShadowOfTheBeast()
        {
            if (DiskTracks[0].Sectors.Length != 9)
                return false;

            var zeroSecs = DiskTracks[0].Sectors;
            if (zeroSecs[0].SectorID != 65 ||
                zeroSecs[1].SectorID != 66 ||
                zeroSecs[2].SectorID != 67 ||
                zeroSecs[3].SectorID != 68 ||
                zeroSecs[4].SectorID != 69 ||
                zeroSecs[5].SectorID != 70 ||
                zeroSecs[6].SectorID != 71 ||
                zeroSecs[7].SectorID != 72 ||
                zeroSecs[8].SectorID != 73)
                return false;

            var oneSecs = DiskTracks[1].Sectors;

            if (oneSecs.Length != 8)
                return false;

            if (oneSecs[0].SectorID != 17 ||
                oneSecs[1].SectorID != 18 ||
                oneSecs[2].SectorID != 19 ||
                oneSecs[3].SectorID != 20 ||
                oneSecs[4].SectorID != 21 ||
                oneSecs[5].SectorID != 22 ||
                oneSecs[6].SectorID != 23 ||
                oneSecs[7].SectorID != 24)
                return false;

            return true;
        }

        /// <summary>
        /// Detect speedlock weak sector
        /// </summary>
        /// <param name="weak"></param>
        /// <returns></returns>
        public bool DetectSpeedlock(ref int[] weak)
        {
            // SPEEDLOCK NOTES (-asni 2018-05-01)
            // ---------------------------------
            // Speedlock is one of the more common +3 disk protections and there are a few different versions
            // Usually, track 0 sector 1 (ID 2) has data CRC errors that result in certain bytes returning a different value every time they are read
            // Speedlock will generally read this track a number of times during the load process
            // and if the correct bytes are not different between reads, the load fails

            // always must have track 0 containing 9 sectors
            if (DiskTracks[0].Sectors.Length != 9)
                return false;

            // check for SPEEDLOCK ident in sector 0
            string ident = Encoding.ASCII.GetString(DiskTracks[0].Sectors[0].SectorData, 0, DiskTracks[0].Sectors[0].SectorData.Length);
            if (!ident.ToUpper().Contains("SPEEDLOCK"))
                return false;

            // check for correct sector 0 lengths
            if (DiskTracks[0].Sectors[0].SectorSize != 2 ||
                DiskTracks[0].Sectors[0].SectorData.Length < 0x200)
                return false;

            // sector[1] (SectorID 2) contains the weak sectors
            Sector sec = DiskTracks[0].Sectors[1];

            // check for correct sector 1 lengths
            if (sec.SectorSize != 2 ||
                sec.SectorData.Length < 0x200)
                return false;

            // secID 2 needs a CRC error
            //if (!(sec.Status1.Bit(5) || sec.Status2.Bit(5)))
                //return false;

            // check for filler
            bool startFillerFound = true;
            for (int i = 0; i < 250; i++)
            {
                if (sec.SectorData[i] != sec.SectorData[i + 1])
                {
                    startFillerFound = false;
                    break;
                }
            }

            if (!startFillerFound)
            {
                weak[0] = 0;
                weak[1] = 0x200;
            }
            else
            {
                weak[0] = 0x150;
                weak[1] = 0x20;
            }

            return true;
        }

        /// <summary>
        /// Detect Alkatraz
        /// </summary>
        /// <param name="weak"></param>
        /// <returns></returns>
        public bool DetectAlkatraz(ref int[] weak)
        {
            try
            {
                var data1 = DiskTracks[0].Sectors[0].SectorData;
                var data2 = DiskTracks[0].Sectors[0].SectorData.Length;
            }
            catch (Exception)
            {
                return false;
            }

            // check for ALKATRAZ ident in sector 0
            string ident = Encoding.ASCII.GetString(DiskTracks[0].Sectors[0].SectorData, 0, DiskTracks[0].Sectors[0].SectorData.Length);
            if (!ident.ToUpper().Contains("ALKATRAZ PROTECTION SYSTEM"))
                return false;

            // ALKATRAZ NOTES (-asni 2018-05-01)
            // ---------------------------------
            // Alkatraz protection appears to revolve around a track on the disk with 18 sectors,
            // (track position is not consistent) with the sector ID info being incorrect:
            //      TrackID is consistent between the sectors although is usually high (233, 237 etc)
            //      SideID is fairly random looking but with all IDs being even
            //      SectorID is also fairly random looking but contains both odd and even numbers
            //            
            // There doesnt appear to be any CRC errors in this track, but the sector size is always 1 (256 bytes)
            // Each sector contains different filler byte
            // Once track 0 is loaded the CPU completely reads all the sectors in this track one-by-one.
            // Data transferred during execution must be correct, also result ST0, ST1 and ST2 must be 64, 128 and 0 respectively

            // Immediately following this track are a number of tracks and sectors with a DAM set.
            // These are all read in sector by sector
            // Again, Alkatraz appears to require that ST0, ST1, and ST2 result bytes are set to 64, 128 and 0 respectively
            // (so the CM in ST2 needs to be reset)

            return true;
        }

        /// <summary>
        /// Detect Paul Owens
        /// </summary>
        /// <param name="weak"></param>
        /// <returns></returns>
        public bool DetectPaulOwens(ref int[] weak)
        {
            try
            {
                var data1 = DiskTracks[0].Sectors[2].SectorData;
                var data2 = DiskTracks[0].Sectors[2].SectorData.Length;
            }
            catch (Exception)
            {
                return false;
            }

            // check for PAUL OWENS ident in sector 2
            string ident = Encoding.ASCII.GetString(DiskTracks[0].Sectors[2].SectorData, 0, DiskTracks[0].Sectors[2].SectorData.Length);
            if (!ident.ToUpper().Contains("PAUL OWENS"))
                return false;

            // Paul Owens Disk Protection Notes (-asni 2018-05-01)
            // ---------------------------------------------------
            //
            // This scheme looks a little similar to Alkatraz with incorrect sector ID info in many places
            // and deleted address marks (although these do not seem to show the strict relience on removing the CM mark from ST2 result that Alkatraz does)
            // There are also data CRC errors but these dont look to be read more than once/checked for changes during load
            // Main identifiers:
            //
            // * There are more than 10 cylinders
            // * Cylinder 1 has no sector data
            // * The sector ID infomation in most cases contains incorrect track IDs
            // * Tracks 0 (boot) and 5 appear to be pretty much the only tracks that do not have incorrect sector ID marks

            return true;
        }

        /// <summary>
        /// Detect Hexagon copy protection
        /// </summary>
        /// <param name="weak"></param>
        /// <returns></returns>
        public bool DetectHexagon(ref int[] weak)
        {
            try
            {
                var data1 = DiskTracks[0].Sectors.Length;
                var data2 = DiskTracks[0].Sectors[8].ActualDataByteLength;
                var data3 = DiskTracks[0].Sectors[8].SectorData;
                var data4 = DiskTracks[0].Sectors[8].SectorData.Length;
                var data5 = DiskTracks[1].Sectors[0];
            }
            catch (Exception)
            {
                return false;
            }

            if (DiskTracks[0].Sectors.Length != 10 || DiskTracks[0].Sectors[8].ActualDataByteLength != 512)
                return false;

            // check for Hexagon ident in sector 8
            string ident = Encoding.ASCII.GetString(DiskTracks[0].Sectors[8].SectorData, 0, DiskTracks[0].Sectors[8].SectorData.Length);
            if (ident.ToUpper().Contains("GON DISK PROT"))
                return true;

            // hexagon protection may not be labelled as such
            var track = DiskTracks[1];
            var sector = track.Sectors[0];

            if (sector.SectorSize == 6 && sector.Status1 == 0x20 && sector.Status2 == 0x60)
            {
                if (track.Sectors.Length == 1)
                    return true;
            }


            // Hexagon Copy Protection Notes (-asni 2018-05-01)
            // ---------------------------------------------------
            //
            // 

            return false;
        }

        /*
        /// <summary>
        /// Should be run at the end of the ParseDisk process
        /// If speedlock is detected the flag is set in the disk image
        /// </summary>
        /// <returns></returns>
        protected virtual void SpeedlockDetection()
        {

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
        */

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

            public int WeakReadIndex = 0;

            public void SectorReadCompleted()
            {
                if (ContainsMultipleWeakSectors)
                    WeakReadIndex++;
            }

            public int DataLen
            {
                get
                {
                    if (!ContainsMultipleWeakSectors)
                    {
                        return ActualDataByteLength;
                    }
                    else
                    {
                        return ActualDataByteLength / (ActualDataByteLength / (0x80 << SectorSize));
                    }
                }
            }


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
                                //l.Add(SectorData[i]);
                                l.Add(SectorData.Last());
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
                        // weak read neccessary
                        int copies = ActualDataByteLength / (0x80 << SectorSize);

                        // handle index wrap-around
                        if (WeakReadIndex > copies - 1)
                            WeakReadIndex = copies - 1;

                        // get the sector data based on the current weakreadindex
                        int step = WeakReadIndex * (0x80 << SectorSize);
                        byte[] res = new byte[(0x80 << SectorSize)];
                        Array.Copy(SectorData, step, res, 0, 0x80 << SectorSize);
                        return res;

                        /*
                        int copies = ActualDataByteLength / (0x80 << SectorSize);
                        Random rnd = new Random();
                        int r = rnd.Next(0, copies - 1);
                        int step = r * (0x80 << SectorSize);
                        byte[] res = new byte[(0x80 << SectorSize)];
                        Array.Copy(SectorData, step, res, 0, 0x80 << SectorSize);
                        return res;
                        */
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
        Speedlock,
        Alkatraz,
        Hexagon,
        Frontier,
        PaulOwens,
        ShadowOfTheBeast
    }

}
