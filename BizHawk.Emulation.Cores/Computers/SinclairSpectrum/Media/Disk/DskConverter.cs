using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
    /// <summary>
    /// Reponsible for DSK format serializaton
    /// File format info taken from:  http://www.cpcwiki.eu/index.php/Format:DSK_disk_image_file_format
    /// </summary>
    public class DskConverter : MediaConverter
    {
        /// <summary>
        /// The type of serializer
        /// </summary>
        private MediaConverterType _formatType = MediaConverterType.DSK;
        public override MediaConverterType FormatType
        {
            get
            {
                return _formatType;
            }
        }

        /// <summary>
        /// Signs whether this class can be used to read data
        /// </summary>
        public override bool IsReader { get { return true; } }

        /// <summary>
        /// Signs whether this class can be used to write data
        /// </summary>
        public override bool IsWriter { get { return false; } }

        /// <summary>
        /// The disk image that we will be populating
        /// </summary>
        private DiskImage _diskImage = new DiskImage();


        /// <summary>
        /// The current position whilst parsing the incoming data
        /// </summary>
        private int _position = 0;

        #region Construction

        private IFDDHost _diskDrive;

        public DskConverter(IFDDHost diskDrive)
        {
            _diskDrive = diskDrive;
        }

        #endregion

        /// <summary>
        /// Returns TRUE if dsk header is detected
        /// </summary>
        /// <param name="data"></param>
        public override bool CheckType(byte[] data)
        {
            // check whether this is a valid dsk format file by looking at the identifier in the header
            // (first 16 bytes of the file)
            string ident = Encoding.ASCII.GetString(data, 0, 16);

            if (!ident.ToUpper().Contains("EXTENDED CPC DSK") && !ident.ToUpper().Contains("MV - CPCEMU"))
            {
                // this is not a valid DSK format file
                return false;
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// Read method
        /// </summary>
        /// <param name="data"></param>
        public override void Read(byte[] data)
        {
            // populate dsk header info
            string ident = Encoding.ASCII.GetString(data, 0, 34);
            _diskImage.Header_SystemIdent = ident;

            if (ident.ToUpper().Contains("EXTENDED CPC DSK"))
                ReadExtendedDsk(data);
            else if (ident.ToUpper().Contains("MV - CPCEMU"))
                ReadDsk(data);

            // load the disk 'into' the controller
            //_diskDrive.Disk = _diskImage;
        }

        /// <summary>
        /// Parses the extended disk format
        /// The extended DSK image is a file designed to describe copy-protected floppy disk software
        /// </summary>
        /// <param name="data"></param>
        private void ReadExtendedDsk(byte[] data)
        {
            /* DISK INFORMATION BLOCK
             
                offset	    description	                                bytes
                00 - 21	    "EXTENDED CPC DSK File\r\nDisk-Info\r\n"	34
                22 - 2f	    name of creator (utility/emulator)	        14
                30	        number of tracks	                        1
                31	        number of sides	                            1
                32 -        33	unused	                                2
                34 -        xx	track size table	                    number of tracks*number of sides
            */

            // name of creator
            _position = 0x22;
            _diskImage.Header_CreatorSoftware = Encoding.ASCII.GetString(data, _position, 14);

            // number of tracks
            _position = 0x30;
            _diskImage.Header_TrackCount = data[_position++];

            // number of sides
            _diskImage.Header_SideCount = data[_position++];

            _position += 2;

            /* TRACK OFFSET TABLE
             
                offset	description	                                                    bytes
                01	    high byte of track 0 length (equivalent to track length/256)	1
                ...	    ...	                                                            ...

                track lengths are stored in the same order as the tracks in the image e.g. In the case of a double sided disk: Track 0 side 0, Track 0 side 1, Track 1 side 0 etc...
                A size of "0" indicates an unformatted track. In this case there is no data, and no track information block for this track in the image file!
                Actual length of track data = (high byte of track length) * 256  
                Track length includes the size of the TRACK INFORMATION BLOCK (256 bytes)
                The location of a Track Information Block for a chosen track is found by summing the sizes of all tracks up to the chosen track plus the size of the Disc Information Block (&100 bytes). The first track is at offset &100 in the disc image.
             */

            // iterate through the track size table and do an initial setup of all the tracks
            // (this is just new tracks with the offset value saved)
            int trkPos = 0x100;
            for (int i = 0; i < _diskImage.Header_TrackCount * _diskImage.Header_SideCount; i++)
            {
                Track track = new Track();

                // calc actual track length (including track info block)
                int ts = data[_position++];
                int tLen = ts * 256;

                track.TrackStartOffset = trkPos;
                track.TrackByteLength = tLen;
                _diskImage.Tracks.Add(track);
                trkPos += tLen;
            }

            // iterate through each newly created track and parse it based on the offset
            List<Track> tracks = new List<Track>();
            for (int tr = 0; tr < _diskImage.Tracks.Count(); tr++)
            {
                // get the track we are interested in
                var t = _diskImage.Tracks[tr];

                // validity check
                if (t.TrackByteLength == 0)
                {
                    // unformatted track that is not present in the disk image
                    Track trU = new Track();
                    trU.TrackNumber = (byte)tr;
                    trU.TrackByteLength = 0;
                    tracks.Add(trU);
                    continue;
                }

                Track track = new Track();
                track.TrackStartOffset = t.TrackStartOffset;
                track.TrackByteLength = t.TrackByteLength;

                // get data for this track block
                byte[] tData = data.Skip(t.TrackStartOffset).Take(t.TrackByteLength).ToArray();

                // start of data block
                int pos = 0;

                // seek past track info ident
                pos += 0x10;

                /* TRACK INFORMATION BLOCK

                        offset	    description	            bytes
                        00 - 0b	    "Track-Info\r\n"	    12
                        0c - 0f     unused	                4
                        10	        track number	        1
                        11	        side number	            1
                        12  	    data rate               1
                        13          recording mode          1
                        14	        sector size	            1
                        15	        number of sectors	    1
                        16	        GAP#3 length	        1
                        17	        filler byte	            1
                        18 - xx	    Sector Information List xx
                    */

                /*  Format extensions
                    -----------------

                    Date rate	description
                    0	        Unknown.
                    1	        Single or double density
                    2	        High Density
                    3	        Extended density

                    Data rate defines the rate at which data was written to the track. This value applies to the entire track.

                    Recording mode	description
                    0	            Unknown.
                    1	            FM
                    2	            MFM

                    Recording mode is used to define how the data was written. It defines the encoding used to write the data to the disc and the structure of the data on the disc including the layout of the sectors. 
                    This value applies to the entire track

                    The NEC765 floppy disc controller is supplied with a single clock. When reading from and writing to a disc using the NEC765 you can choose 
                    FM or MFM recording modes. Use of these modes and the clock into the NEC765 define the final rate at which the data is written to the disc.
                    When FM recording mode is used, data is read from or written to at a rate which is double that of when MFM is used. 
                    The time for each bit will be twice the time for MFM

                    NEC765 Clock	FM/MFM	Actual rate
                    4Mhz	        FM	    4us per bit
                    4Mhz	        MFM	    2us per bit
                 */

                // track number
                track.TrackNumber = tData[pos++];
                // side number
                track.SideNumber = tData[pos++];
                // data rate
                track.DataRate = tData[pos++];
                // recording mode
                track.RecordingMode = tData[pos++];
                // sector size
                track.SectorSize = tData[pos++];
                // number of sectors
                track.SectorCount = tData[pos++];
                // GAP#3 Length
                track.GAP3Length = tData[pos++];
                // filler byte
                track.FillerByte = tData[pos++];

                /* SECTOR INFORMATION LIST

                        offset	description	                                                        bytes
                        00	    track (equivalent to C parameter in NEC765 commands)	            1
                        01	    side (equivalent to H parameter in NEC765 commands)	                1
                        02	    sector ID (equivalent to R parameter in NEC765 commands)	        1
                        03	    sector size (equivalent to N parameter in NEC765 commands)	        1
                        04	    FDC status register 1 (equivalent to NEC765 ST1 status register)	1
                        05	    FDC status register 2 (equivalent to NEC765 ST2 status register)	1
                        06 - 07	actual data length in bytes                                         2
                */

                // parse sector information list
                for (int s = 0; s < track.SectorCount; s++)
                {
                    Sector sector = new Sector();
                    sector.Track = tData[pos++];
                    sector.Side = tData[pos++];
                    sector.SectorID = tData[pos++];
                    sector.SectorSize = tData[pos++];
                    sector.ST1 = tData[pos++];
                    sector.ST2 = tData[pos++];
                    sector.TotalDataLength = GetWordValue(tData, pos);
                    pos += 2;

                    // get the sector data - lives directly after the 256byte track info block
                    int secDataPos = 0x100;

                    /*  Format extension
                            ---------------_
                            It has been found that many protections using 8K Sectors (N="6") do store more than &1800 bytes of useable data. 
                            It was thought that &1800 was the maximum useable limit, but this has proved wrong. So you should support 8K of data to ensure 
                            this data is read correctly. The size of the sector will be reported in the SECTOR INFORMATION LIST as described above.
                            For sector size N="7" the full 16K will be stored. It is assumed that sector sizes are defined as 3 bits only, 
                            so that a sector size of N="8" is equivalent to N="0".
                        */
                    int bps = 0x80 << sector.SectorSize;

                    if (sector.SectorSize == 8 || sector.SectorSize == 0)
                    {
                        // no sector data
                        bps = 0x80 << 0;
                        //continue;
                    }
                        

                    /*  Format extension
                        ----------------
                        Storing Multiple Versions of Weak/Random Sectors.
                        Some copy protections have what is described as 'weak/random' data. Each time the sector is read one or more bytes will change, 
                        the value may be random between consecutive reads of the same sector.
                        To support these formats the following extension has been proposed.
                        Where a sector has weak/random data, there are multiple copies stored. The actual sector size field in the SECTOR INFORMATION LIST 
                        describes the size of all the copies. To determine if a sector has multiple copies then compare the actual sector size field to the 
                        size defined by the N parameter. For multiple copies the actual sector size field will have a value which is a multiple of the size 
                        defined by the N parameter. The emulator should then choose which copy of the sector it should return on each read.
                    */

                    int sizeOfAllCopies = sector.TotalDataLength;

                    if (sizeOfAllCopies % bps == 0)
                    {
                        // sector size is the same as total data length (indicating 1 sector data)
                        // or a factor of the total data length (indicating multiple copies)
                        for (int sd = 0; sd < sizeOfAllCopies / bps; sd++)
                        {
                            byte[] sData = new byte[bps];
                            for (int c = 0; c < bps; c++)
                            {
                                sData[c] = data[secDataPos++];
                            }

                            sector.AddSectorData(sd, sData);
                        }
                    }
                    else
                    {
                        // assume that there is one sector copy, but the total data length does not match
                        if (sector.TotalDataLength <= sizeOfAllCopies)
                        {
                            byte[] sData = new byte[bps];
                            for (int c = 0; c < bps; c++)
                            {
                                sData[c] = data[secDataPos++];
                            }

                            sector.AddSectorData(0, sData);
                        }
                        else
                        {
                            byte[] sData = new byte[sizeOfAllCopies];
                            for (int c = 0; c < sizeOfAllCopies; c++)
                            {
                                sData[c] = data[secDataPos++];
                            }

                            sector.AddSectorData(0, sData);
                        }
                    }

                    track.Sectors.Add(sector);

                }

                // add track to working list
                tracks.Add(track);
            }

            // replace the tracks collection
            _diskImage.Tracks = tracks;         
        }
   

        /// <summary>
        /// Parses the standard disk image format
        /// </summary>
        /// <param name="data"></param>
        private void ReadDsk(byte[] data)
        {
            /* DISK INFORMATION BLOCK
             
                offset	description	                                                    bytes
                00-21	"MV - CPCEMU Disk-File\r\nDisk-Info\r\n"	                    34
                22-2f	name of creator	                                                14
                30	    number of tracks	                                            1
                31	    number of sides	                                                1
                32-33	size of a track (little endian; low byte followed by high byte)	2
                34-ff	not used (0)	                                                204
            */

            // name of creator
            _position = 0x22;
            _diskImage.Header_CreatorSoftware = Encoding.ASCII.GetString(data, _position, 14);

            // number of tracks
            _position = 0x30;
            _diskImage.Header_TrackCount = data[_position++];

            // number of sides
            _diskImage.Header_SideCount = data[_position++];

            // size of a track (little endian)
            _diskImage.Header_TrackSize = GetWordValue(data, _position);

            // 34-ff not used

            // move to start of first track info block
            _position = 0x100;

            int tmpPos = _position;

            // iterate through each track
            for (int t = 0; t < _diskImage.Header_TrackCount; t++)
            {
                /* TRACK INFORMATION BLOCK
                              
                    offset	    description	            bytes
                    00 - 0b	    "Track-Info\r\n"	    12
                    0c - 0f     unused	                4
                    10	        track number	        1
                    11	        side number	            1
                    12 - 13	    unused	                2
                    14	        sector size	            1
                    15	        number of sectors	    1
                    16	        GAP#3 length	        1
                    17	        filler byte	            1
                    18 - xx	    Sector Information List xx
                */

                Track track = new Track();
                _position += 0x10;

                track.TrackNumber = data[_position++];
                track.SideNumber = data[_position++];
                _position += 2;

                /*
                    BPS (bytes per sector)
                    0 = 128     0x80
                    1 = 256     0x100
                    2 = 512     0x200
                    3 = 1024    0x400
                    4 = 2048    0x800
                    5 = 4096    0x1000
                    6 = 8192    0x2000
                */

                track.SectorSize = data[_position++];

                track.SectorCount = data[_position++];
                track.GAP3Length = data[_position++];
                track.FillerByte = data[_position++];

                /* SECTOR INFORMATION LIST
                              
                    offset	description	                                                        bytes
                    00	    track (equivalent to C parameter in NEC765 commands)	            1
                    01	    side (equivalent to H parameter in NEC765 commands)	                1
                    02	    sector ID (equivalent to R parameter in NEC765 commands)	        1
                    03	    sector size (equivalent to N parameter in NEC765 commands)	        1
                    04	    FDC status register 1 (equivalent to NEC765 ST1 status register)	1
                    05	    FDC status register 2 (equivalent to NEC765 ST2 status register)	1
                    06 - 07	notused (0)	                                                        2
                */

                // get the sector info first
                for (int s = 0; s < track.SectorCount; s++)
                {
                    Sector sector = new Sector();
                    sector.Track = data[_position++];
                    sector.Side = data[_position++];
                    sector.SectorID = data[_position++];
                    sector.SectorSize = data[_position++];
                    sector.ST1 = data[_position++];
                    sector.ST2 = data[_position++];
                    track.Sectors.Add(sector);
                    _position += 2;
                }

                // now process the sector data - always offset 0x100 from the start of the track information block
                tmpPos += 0x100;
                _position = tmpPos;

                // sectorsize in the track information list implies the boundaries of the sector data
                // sectorsize in the sector info list gives us the actual size of each sector to read
                for (int s = 0; s < track.SectorCount; s++)
                {
                    var sec = track.Sectors[s];

                    List<byte> tmpData = new List<byte>();

                    // read the sector data bytes
                    int bps = 0x80 << sec.SectorSize;
                    if (bps > 0x1800)
                        bps = 0x1800;

                    for (int d = 0; d < bps; d++)
                    {
                        tmpData.Add(data[_position + d]);
                    }

                    sec.AddSectorData(0, tmpData.ToArray());
                    track.Sectors[s] = sec;

                    // sector data parsed - move to next sector data block
                    int trackBps = 0x80 << track.SectorSize;
                    if (trackBps > 0x1800)
                        trackBps = 0x1800;

                    _position += track.SectorSize;
                }

                // add the track to the disk image
                _diskImage.Tracks.Add(track);
            }
        }
    }
}
