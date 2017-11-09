using System;
using System.Text;
using System.IO;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace BizHawk.Emulation.DiscSystem
{
    /// <summary>
    /// Parsing Alcohol 120% files
    /// Info taken from:
    /// * http://forum.redump.org/post/41803/#p41803
    /// * Libmirage image-mds parser - https://sourceforge.net/projects/cdemu/files/libmirage/
    /// * DiscImageChef -   https://github.com/claunia/DiscImageChef/blob/master/DiscImageChef.DiscImages/Alcohol120.cs
    /// </summary>
	public class MDS_Format
    {
        /// <summary>
        /// A loose representation of an Alcohol 120 .mds file (with a few extras)
        /// </summary>
        public class AFile
        {
            /// <summary>
            /// Full path to the MDS file
            /// </summary>
            public string MDSPath;

            /// <summary>
            /// MDS Header
            /// </summary>
            public AHeader Header = new AHeader();

            /// <summary>
            /// List of MDS session blocks
            /// </summary>
            public List<ASession> Sessions = new List<ASession>();

            /// <summary>
            /// List of track blocks
            /// </summary>
            public List<ATrack> Tracks = new List<ATrack>();

            /// <summary>
            /// Current parsed session objects
            /// </summary>
            public List<Session> ParsedSession = new List<Session>();

            /// <summary>
            /// Calculated MDS TOC entries (still to be parsed into BizHawk)
            /// </summary>
            public List<ATOCEntry> TOCEntries = new List<ATOCEntry>();
            
        }

        public class AHeader
        {
            /// <summary>
            /// Standard alcohol 120% signature - usually "MEDIA DESCRIPTOR"
            /// </summary>
            public string Signature;                // 16 bytes

            /// <summary>
            /// Alcohol version?
            /// </summary>
            public byte[] Version;                  // 2 bytes

            /// <summary>
            /// The medium type
            /// * 0x00  -   CD
            /// * 0x01  -   CD-R
            /// * 0x02  -   CD-RW
            /// * 0x10  -   DVD
            /// * 0x12  -   DVD-R
            /// </summary>
            public int Medium;

            /// <summary>
            /// Number of sessions
            /// </summary>
            public int SessionCount;

            /// <summary>
            /// Burst Cutting Area length
            /// </summary>
            public int BCALength;

            /// <summary>
            /// Burst Cutting Area data offset
            /// </summary>
            public Int64 BCAOffset;

            /// <summary>
            /// Offset to disc (DVD?) structures
            /// </summary>
            public Int64 StructureOffset;

            /// <summary>
            /// Offset to the first session block
            /// </summary>
            public Int64 SessionOffset;

            /// <summary>
            /// Data Position Measurement offset
            /// </summary>
            public Int64 DPMOffset;

            /// <summary>
            /// Parse mds stream for the header
            /// </summary>
            /// <param name="stream"></param>
            /// <returns></returns>
            public AHeader Parse(Stream stream)
            {
                EndianBitConverter bc = EndianBitConverter.CreateForLittleEndian();
                EndianBitConverter bcBig = EndianBitConverter.CreateForBigEndian();
                
                byte[] header = new byte[88];
                stream.Read(header, 0, 88);

                this.Signature = Encoding.ASCII.GetString(header.Take(16).ToArray());
                this.Version = header.Skip(16).Take(2).ToArray();
                this.Medium = bc.ToInt16(header.Skip(18).Take(2).ToArray());
                this.SessionCount = bc.ToInt16(header.Skip(20).Take(2).ToArray());
                this.BCALength = bc.ToInt16(header.Skip(26).Take(2).ToArray());
                this.BCAOffset = bc.ToInt32(header.Skip(36).Take(4).ToArray());
                this.StructureOffset = bc.ToInt32(header.Skip(64).Take(4).ToArray());
                this.SessionOffset = bc.ToInt32(header.Skip(80).Take(4).ToArray());
                this.DPMOffset = bc.ToInt32(header.Skip(84).Take(4).ToArray());

                return this;
            }
        }

        /// <summary>
        /// MDS session block representation
        /// </summary>
        public class ASession
        {
            public int SessionStart;        /* Session's start address */
            public int SessionEnd;          /* Session's end address */
            public int SessionNumber;       /* Session number */
            public byte AllBlocks;          /* Number of all data blocks. */
            public byte NonTrackBlocks;     /* Number of lead-in data blocks */
            public int FirstTrack;          /* First track in session */
            public int LastTrack;           /* Last track in session */
            public Int64 TrackOffset;       /* Offset of lead-in+regular track data blocks. */
        }

        /// <summary>
        /// Representation of an MDS track block
        /// For convenience (and extra confusion) this also holds the track extrablock, filename(footer) block infos
        /// as well as the calculated image filepath as specified in the MDS file
        /// </summary>
        public class ATrack
        {
            /// <summary>
            /// The specified data mode
            /// 0x00    -   None (no data)
            /// 0x02    -   DVD
            /// 0xA9    -   Audio
            /// 0xAA    -   Mode1
            /// 0xAB    -   Mode2
            /// 0xAC    -   Mode2 Form1
            /// 0xAD    -   Mode2 Form2
            /// </summary>
            public byte Mode;               /* Track mode */

            /// <summary>
            /// Subchannel mode for the track (0x00 = None, 0x08 = Interleaved)
            /// </summary>
            public byte SubMode;            /* Subchannel mode */

            /* These are the fields from Sub-channel Q information, which are
                also returned in full TOC by READ TOC/PMA/ATIP command */
            public int ADR_Control;         /* Adr/Ctl */
            public int TrackNo;             /* Track number field */
            public int Point;               /* Point field (= track number for track entries) */
            public int AMin;                /* Min */
            public int ASec;                /* Sec */
            public int AFrame;              /* Frame */
            public int Zero;                /* Zero */
            public int PMin;                /* PMin */
            public int PSec;                /* PSec */
            public int PFrame;              /* PFrame */

            public Int64 ExtraOffset;       /* Start offset of this track's extra block. */
            public int SectorSize;          /* Sector size. */
            public Int64 PLBA;               /* Track start sector (PLBA). */
            public ulong StartOffset;       /* Track start offset (from beginning of MDS file) */
            public Int64 Files;             /* Number of filenames for this track */
            public Int64 FooterOffset;      /* Start offset of footer (from beginning of MDS file) */

            /// <summary>
            /// Track extra block
            /// </summary>
            public ATrackExtra ExtraBlock = new ATrackExtra();

            /// <summary>
            /// List of footer(filename) blocks for this track
            /// </summary>
            public List<AFooter> FooterBlocks = new List<AFooter>();

            /// <summary>
            /// List of the calculated full paths to this track's image file
            /// The MDS file itself may contain a filename, or just an *.extension
            /// </summary>
            public List<string> ImageFileNamePaths = new List<string>();

            public int BlobIndex;
        }

        /// <summary>
        /// Extra track block
        /// </summary>
        public class ATrackExtra
        {
            public Int64 Pregap;            /* Number of sectors in pregap. */
            public Int64 Sectors;           /* Number of sectors in track. */
        }

        /// <summary>
        /// Footer (filename) block - potentially one for every track
        /// </summary>
        public class AFooter
        {
            public Int64 FilenameOffset;    /* Start offset of image filename string (from beginning of mds file) */
            public Int64 WideChar;          /* Seems to be set to 1 if widechar filename is used */
        }

        /// <summary>
        /// Represents a parsed MDS TOC entry
        /// </summary>
        public class ATOCEntry
        {
            public ATOCEntry(int entryNum)
            {
                EntryNum = entryNum;
            }

            /// <summary>
            /// these should be 0-indexed
            /// </summary>
            public int EntryNum;


            /// <summary>
            /// 1-indexed - the session that this entry belongs to
            /// </summary>
            public int Session;

            /// <summary>
            /// this seems just to be the LBA corresponding to AMIN:ASEC:AFRAME (give or take 150). It's not stored on the disc, and it's redundant.
            /// </summary>
            //public int ALBA;

            /// <summary>
            /// this seems just to be the LBA corresponding to PMIN:PSEC:PFRAME (give or take 150).
            /// </summary>
            public int PLBA;

            //these correspond pretty directly to values in the Q subchannel fields
            //NOTE: they're specified as absolute MSF. That means, they're 2 seconds off from what they should be when viewed as final TOC values
            public int ADR_Control;
            public int TrackNo;
            public int Point;
            public int AMin;
            public int ASec;
            public int AFrame;
            public int Zero;
            public int PMin;
            public int PSec;
            public int PFrame;


            public int SectorSize;
            public long TrackOffset;

            /// <summary>
            /// List of the calculated full paths to this track's image file
            /// The MDS file itself may contain a filename, or just an *.extension
            /// </summary>
            public List<string> ImageFileNamePaths = new List<string>();

            /// <summary>
            /// Track extra block
            /// </summary>
            public ATrackExtra ExtraBlock = new ATrackExtra();

            public int BlobIndex;
        }

        public AFile Parse(Stream stream)
        {
            EndianBitConverter bc = EndianBitConverter.CreateForLittleEndian();
            EndianBitConverter bcBig = EndianBitConverter.CreateForBigEndian();
            bool isDvd = false;

            AFile aFile = new AFile();

            aFile.MDSPath = (stream as FileStream).Name;

            stream.Seek(0, SeekOrigin.Begin);

            // check whether the header in the mds file is long enough
            if (stream.Length < 88) throw new MDSParseException("Malformed MDS format: The descriptor file does not appear to be long enough.");

            // parse header
            aFile.Header = aFile.Header.Parse(stream);

            // check version to make sure this is only v1.x
            // currently NO support for version 2.x

            if (aFile.Header.Version[0] > 1)
            {
                throw new MDSParseException("MDS Parse Error: Only MDS version 1.x is supported!\nDetected version: " + aFile.Header.Version[0] + "." + aFile.Header.Version[1]);
            }

            // parse sessions
            Dictionary<int, ASession> aSessions = new Dictionary<int, ASession>();

            stream.Seek(aFile.Header.SessionOffset, SeekOrigin.Begin);
            for (int se = 0; se < aFile.Header.SessionCount; se++)
            {
                byte[] sessionHeader = new byte[24];
                stream.Read(sessionHeader, 0, 24);
                //sessionHeader.Reverse().ToArray();

                ASession session = new ASession();

                session.SessionStart = bc.ToInt32(sessionHeader.Take(4).ToArray());
                session.SessionEnd = bc.ToInt32(sessionHeader.Skip(4).Take(4).ToArray());
                session.SessionNumber = bc.ToInt16(sessionHeader.Skip(8).Take(2).ToArray());
                session.AllBlocks = sessionHeader[10];
                session.NonTrackBlocks = sessionHeader[11];
                session.FirstTrack = bc.ToInt16(sessionHeader.Skip(12).Take(2).ToArray());
                session.LastTrack = bc.ToInt16(sessionHeader.Skip(14).Take(2).ToArray());
                session.TrackOffset = bc.ToInt32(sessionHeader.Skip(20).Take(4).ToArray());


                //mdsf.Sessions.Add(session);
                aSessions.Add(session.SessionNumber, session);
            }

            long footerOffset = 0;

            // parse track blocks
            Dictionary<int, ATrack> aTracks = new Dictionary<int, ATrack>();

            // iterate through each session block
            foreach (ASession session in aSessions.Values)
            {
                stream.Seek(session.TrackOffset, SeekOrigin.Begin);
                //Dictionary<int, ATrack> sessionToc = new Dictionary<int, ATrack>();

                // iterate through every block specified in each session
                for (int bl = 0; bl < session.AllBlocks; bl++)
                {
                    byte[] trackHeader;
                    ATrack track = new ATrack();

                    trackHeader = new byte[80];

                    stream.Read(trackHeader, 0, 80);

                    track.Mode = trackHeader[0];
                    track.SubMode = trackHeader[1];
                    track.ADR_Control = trackHeader[2];
                    track.TrackNo = trackHeader[3];
                    track.Point = trackHeader[4];
                    track.AMin = trackHeader[5];
                    track.ASec = trackHeader[6];
                    track.AFrame = trackHeader[7];
                    track.Zero = trackHeader[8];
                    track.PMin = trackHeader[9];
                    track.PSec = trackHeader[10];
                    track.PFrame = trackHeader[11];
                    track.ExtraOffset = bc.ToInt32(trackHeader.Skip(12).Take(4).ToArray());
                    track.SectorSize = bc.ToInt16(trackHeader.Skip(16).Take(2).ToArray());
                    track.PLBA = bc.ToInt32(trackHeader.Skip(36).Take(4).ToArray());
                    track.StartOffset = BitConverter.ToUInt64(trackHeader.Skip(40).Take(8).ToArray(), 0);
                    track.Files = bc.ToInt32(trackHeader.Skip(48).Take(4).ToArray());
                    track.FooterOffset = bc.ToInt32(trackHeader.Skip(52).Take(4).ToArray());

                    if (track.Mode == 0x02)
                    {
                        isDvd = true;
                        throw new MDSParseException("DVD Detected. Not currently supported!");
                    }
                        

                    // check for track extra block - this can probably be handled in a separate loop,
                    // but I'll just store the current stream position then seek forward to the extra block for this track
                    Int64 currPos = stream.Position;

                    // Only CDs have extra blocks - for DVDs ExtraOffset = track length
                    if (track.ExtraOffset > 0 && !isDvd)
                    {
                        byte[] extHeader = new byte[8];
                        stream.Seek(track.ExtraOffset, SeekOrigin.Begin);
                        stream.Read(extHeader, 0, 8);
                        track.ExtraBlock.Pregap = bc.ToInt32(extHeader.Take(4).ToArray());
                        track.ExtraBlock.Sectors = bc.ToInt32(extHeader.Skip(4).Take(4).ToArray());
                        stream.Seek(currPos, SeekOrigin.Begin);
                    }
                    else if (isDvd == true)
                    {
                        track.ExtraBlock.Sectors = track.ExtraOffset;
                    }

                    // read the footer/filename block for this track
                    currPos = stream.Position;
                    long numOfFilenames = track.Files;
                    for (long fi = 1; fi <= numOfFilenames; fi++)
                    {
                        // skip leadin/out info tracks
                        if (track.FooterOffset == 0)
                            continue;

                        byte[] foot = new byte[16];
                        stream.Seek(track.FooterOffset, SeekOrigin.Begin);
                        stream.Read(foot, 0, 16);

                        AFooter f = new AFooter();
                        f.FilenameOffset = bc.ToInt32(foot.Take(4).ToArray());
                        f.WideChar = bc.ToInt32(foot.Skip(4).Take(4).ToArray());
                        track.FooterBlocks.Add(f);
                        track.FooterBlocks = track.FooterBlocks.Distinct().ToList();

                        // parse the filename string
                        string fileName = "*.mdf";
                        if (f.FilenameOffset > 0)
                        {
                            // filename offset is present
                            stream.Seek(f.FilenameOffset, SeekOrigin.Begin);
                            byte[] fname;

                            if (numOfFilenames == 1)
                            {
                                if (aFile.Header.DPMOffset == 0)
                                {
                                    // filename is in the remaining space to EOF
                                    fname = new byte[stream.Length - stream.Position];
                                }
                                else
                                {
                                    // filename is in the remaining space to EOF + dpm offset
                                    fname = new byte[aFile.Header.DPMOffset - stream.Position];
                                }
                            }

                            else
                            {
                                // looks like each filename string is 6 bytes with a trailing \0
                                fname = new byte[6];
                            }
                            

                            // read the filename
                            stream.Read(fname, 0, fname.Length);

                            // if widechar is 1 filename is stored using 16-bit, otherwise 8-bit is used
                            if (f.WideChar == 1)
                                fileName = Encoding.Unicode.GetString(fname).TrimEnd('\0');
                            else
                                fileName = Encoding.Default.GetString(fname).TrimEnd('\0');
                        }

                        else
                        {
                            // assume an MDF file with the same name as the MDS
                        }

                        string dir = Path.GetDirectoryName(aFile.MDSPath);

                        if (f.FilenameOffset == 0 ||
                            string.Compare(fileName, "*.mdf", StringComparison.InvariantCultureIgnoreCase) == 0)
                        {
                            fileName = dir + @"\" + Path.GetFileNameWithoutExtension(aFile.MDSPath) + ".mdf";
                        }
                        else
                        {
                            fileName = dir + @"\" + fileName;
                        }

                        track.ImageFileNamePaths.Add(fileName);
                        track.ImageFileNamePaths = track.ImageFileNamePaths.Distinct().ToList();
                    }

                    stream.Position = currPos;


                    aTracks.Add(track.Point, track);
                    aFile.Tracks.Add(track);

                    if (footerOffset == 0)
                        footerOffset = track.FooterOffset;
                }
            }

            
            // build custom session object
            aFile.ParsedSession = new List<Session>();
            foreach (var s in aSessions.Values)
            {
                Session session = new Session();
                ATrack startTrack;
                ATrack endTrack;

                if (!aTracks.TryGetValue(s.FirstTrack, out startTrack))
                {
                    break;
                }

                if (!aTracks.TryGetValue(s.LastTrack, out endTrack))
                {
                    break;
                }

                session.StartSector = startTrack.PLBA;
                session.StartTrack = s.FirstTrack;
                session.SessionSequence = s.SessionNumber;
                session.EndSector = endTrack.PLBA + endTrack.ExtraBlock.Sectors - 1;
                session.EndTrack = s.LastTrack;

                aFile.ParsedSession.Add(session);
            }

            // now build the TOC object
            foreach (var se in aFile.ParsedSession)
            {
                // get the first and last tracks
                int sTrack = se.StartTrack;
                int eTrack = se.EndTrack;

                // get list of all tracks from aTracks for this session
                var tracks = (from a in aTracks.Values
                              where a.TrackNo >= sTrack || a.TrackNo <= eTrack
                              orderby a.TrackNo
                              select a).ToList();

                // create the TOC entries
                foreach (var t in tracks)
                {
                    ATOCEntry toc = new ATOCEntry(t.Point);
                    toc.ADR_Control = t.ADR_Control;
                    toc.AFrame = t.AFrame;
                    toc.AMin = t.AMin;
                    toc.ASec = t.ASec;
                    toc.EntryNum = t.TrackNo;
                    toc.PFrame = t.PFrame;
                    toc.PLBA = Convert.ToInt32(t.PLBA);
                    toc.PMin = t.PMin;
                    toc.Point = t.Point;
                    toc.PSec = t.PSec;
                    toc.SectorSize = t.SectorSize;
                    toc.Zero = t.Zero;
                    toc.TrackOffset = Convert.ToInt64(t.StartOffset);
                    toc.Session = se.SessionSequence;
                    toc.ImageFileNamePaths = t.ImageFileNamePaths;
                    toc.ExtraBlock = t.ExtraBlock;
                    toc.BlobIndex = t.BlobIndex;
                    aFile.TOCEntries.Add(toc);
                }
                
            }

            return aFile;
        }

        /// <summary>
        /// Custom session object
        /// </summary>
        public class Session
        {
            public long StartSector;
            public int StartTrack;
            public int SessionSequence;
            public long EndSector;
            public int EndTrack;
        }


        public class MDSParseException : Exception
        {
            public MDSParseException(string message) : base(message) { }
        }
        

        public class LoadResults
        {
            public List<RawTOCEntry> RawTOCEntries;
            public AFile ParsedMDSFile;
            public bool Valid;
            public Exception FailureException;
            public string MdsPath;
        }

        public static LoadResults LoadMDSPath(string path)
        {
            LoadResults ret = new LoadResults();
            ret.MdsPath = path;
            //ret.MdfPath = Path.ChangeExtension(path, ".mdf");
            try
            {
                if (!File.Exists(path)) throw new MDSParseException("Malformed MDS format: nonexistent MDS file!");

                AFile mdsf;
                using (var infMDS = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                    mdsf = new MDS_Format().Parse(infMDS);

                ret.ParsedMDSFile = mdsf;

                ret.Valid = true;
            }
            catch (MDSParseException ex)
            {
                ret.FailureException = ex;
            }

            return ret;
        }

        Dictionary<int, IBlob> MountBlobs(AFile mdsf, Disc disc)
        {
            Dictionary<int, IBlob> BlobIndex = new Dictionary<int, IBlob>();

            int count = 0;
            foreach (var track in mdsf.Tracks)
            {
                foreach (var file in track.ImageFileNamePaths.Distinct())
                {
                    if (!File.Exists(file))
                        throw new MDSParseException("Malformed MDS format: nonexistent image file: " + file);

                    IBlob mdfBlob = null;
                    long mdfLen = -1;

                    //mount the file			
                    if (mdfBlob == null)
                    {
                        var mdfFile = new Disc.Blob_RawFile() { PhysicalPath = file };
                        mdfLen = mdfFile.Length;
                        mdfBlob = mdfFile;
                    }

                    bool dupe = false;
                    foreach (var re in disc.DisposableResources)
                    {
                        if (re.ToString() == mdfBlob.ToString())
                            dupe = true;
                    }

                    if (!dupe)
                    {
                        // wrap in zeropadadapter
                        disc.DisposableResources.Add(mdfBlob);
                        BlobIndex[count] = mdfBlob;
                    }
                }
            }

            return BlobIndex;
        }

        RawTOCEntry EmitRawTOCEntry(ATOCEntry entry)
        {
            BCD2 tno, ino;

            //this should actually be zero. im not sure if this is stored as BCD2 or not
            tno = BCD2.FromDecimal(entry.TrackNo);

            //these are special values.. I think, taken from this:
            //http://www.staff.uni-mainz.de/tacke/scsi/SCSI2-14.html
            //the CCD will contain Points as decimal values except for these specially converted decimal values which should stay as BCD. 
            //Why couldn't they all be BCD? I don't know. I guess because BCD is inconvenient, but only A0 and friends have special meaning. It's confusing.
            ino = BCD2.FromDecimal(entry.Point);
            if (entry.Point == 0xA0) ino.BCDValue = 0xA0;
            else if (entry.Point == 0xA1) ino.BCDValue = 0xA1;
            else if (entry.Point == 0xA2) ino.BCDValue = 0xA2;

            // get ADR & Control from ADR_Control byte
            byte adrc = Convert.ToByte(entry.ADR_Control);
            var Control = adrc & 0x0F;
            var ADR = adrc >> 4;

            var q = new SubchannelQ
            {
                q_status = SubchannelQ.ComputeStatus(ADR, (EControlQ)(Control & 0xF)),
                q_tno = tno,
                q_index = ino,
                min = BCD2.FromDecimal(entry.AMin),
                sec = BCD2.FromDecimal(entry.ASec),
                frame = BCD2.FromDecimal(entry.AFrame),
                zero = (byte)entry.Zero,
                ap_min = BCD2.FromDecimal(entry.PMin),
                ap_sec = BCD2.FromDecimal(entry.PSec),
                ap_frame = BCD2.FromDecimal(entry.PFrame),
                q_crc = 0, //meaningless
            };

            return new RawTOCEntry { QData = q };
        }


        /// <summary>
        /// Loads a MDS at the specified path to a Disc object
        /// </summary>
        public Disc LoadMDSToDisc(string mdsPath, DiscMountPolicy IN_DiscMountPolicy)
        {
            var loadResults = LoadMDSPath(mdsPath);
            if (!loadResults.Valid)
                throw loadResults.FailureException;

            Disc disc = new Disc();

            // load all blobs
            Dictionary<int, IBlob> BlobIndex = MountBlobs(loadResults.ParsedMDSFile, disc);

            var mdsf = loadResults.ParsedMDSFile;
            
            //generate DiscTOCRaw items from the ones specified in the MDS file
            disc.RawTOCEntries = new List<RawTOCEntry>();
            foreach (var entry in mdsf.TOCEntries)
            {
                disc.RawTOCEntries.Add(EmitRawTOCEntry(entry));
            }

            //analyze the RAWTocEntries to figure out what type of track track 1 is
            var tocSynth = new Synthesize_DiscTOC_From_RawTOCEntries_Job() { Entries = disc.RawTOCEntries };
            tocSynth.Run();

            // now build the sectors
            int currBlobIndex = 0;
            foreach (var session in mdsf.ParsedSession)
            {
                for (int i = session.StartTrack; i <= session.EndTrack; i++)
                {
                    int relMSF = -1;

                    var track = mdsf.TOCEntries.Where(t => t.Point == i).FirstOrDefault();
                    if (track == null)
                        break;

                    // ignore the info entries
                    if (track.Point == 0xA0 ||
                    track.Point == 0xA1 ||
                    track.Point == 0xA2)
                    {
                        continue;
                    }

                    // get the blob(s) for this track
                    // its probably a safe assumption that there will be only one blob per track, 
                    // but i'm still not 100% sure on this 
                    var tr = (from a in mdsf.TOCEntries
                                  where a.Point == i
                                  select a).FirstOrDefault();

                    if (tr == null)
                        throw new MDSParseException("BLOB Error!");

                    List<string> blobstrings = new List<string>();
                    foreach (var t in tr.ImageFileNamePaths)
                    {
                        if (!blobstrings.Contains(t))
                            blobstrings.Add(t);
                    }

                    var tBlobs = (from a in tr.ImageFileNamePaths
                                     select a).ToList();

                    if (tBlobs.Count < 1)
                        throw new MDSParseException("BLOB Error!");

                    // is the currBlob valid for this track, or do we need to increment?   
                    string bString = tBlobs.First();

                    IBlob mdfBlob = null;
                    
                    // check for track pregap and create if neccessary
                    // this is specified in the track extras block
                    if (track.ExtraBlock.Pregap > 0)
                    {
                        CUE.CueTrackType pregapTrackType = CUE.CueTrackType.Audio;
                        if (tocSynth.Result.TOCItems[1].IsData)
                        {
                            if (tocSynth.Result.Session1Format == SessionFormat.Type20_CDXA)
                                pregapTrackType = CUE.CueTrackType.Mode2_2352;
                            else if (tocSynth.Result.Session1Format == SessionFormat.Type10_CDI)
                                pregapTrackType = CUE.CueTrackType.CDI_2352;
                            else if (tocSynth.Result.Session1Format == SessionFormat.Type00_CDROM_CDDA)
                                pregapTrackType = CUE.CueTrackType.Mode1_2352;
                        }
                        for (int pre = 0; pre < track.ExtraBlock.Pregap; pre++)
                        {
                            relMSF++;

                            var ss_gap = new CUE.SS_Gap()
                            {
                                Policy = IN_DiscMountPolicy,
                                TrackType = pregapTrackType
                            };
                            disc._Sectors.Add(ss_gap);

                            int qRelMSF = pre - Convert.ToInt32(track.ExtraBlock.Pregap);

                            //tweak relMSF due to ambiguity/contradiction in yellowbook docs
                            if (!IN_DiscMountPolicy.CUE_PregapContradictionModeA)
                                qRelMSF++;

                            //setup subQ
                            byte ADR = 1; //absent some kind of policy for how to set it, this is a safe assumption:
                            ss_gap.sq.SetStatus(ADR, tocSynth.Result.TOCItems[1].Control);
                            ss_gap.sq.q_tno = BCD2.FromDecimal(1);
                            ss_gap.sq.q_index = BCD2.FromDecimal(0);
                            ss_gap.sq.AP_Timestamp = pre;
                            ss_gap.sq.Timestamp = qRelMSF;

                            //setup subP
                            ss_gap.Pause = true;
                        }
                        // pregap processing completed
                    }



                    // create track sectors
                    long currBlobOffset = track.TrackOffset;
                    for (long sector = session.StartSector; sector <= session.EndSector; sector++)
                    {
                        CUE.SS_Base sBase = null;

                        // get the current blob from the BlobIndex
                        Disc.Blob_RawFile currBlob = BlobIndex[currBlobIndex] as Disc.Blob_RawFile;
                        long currBlobLength = currBlob.Length;
                        long currBlobPosition = sector;
                        if (currBlobPosition == currBlobLength)
                            currBlobIndex++;
                        mdfBlob = disc.DisposableResources[currBlobIndex] as Disc.Blob_RawFile;

                        int userSector = 2048;
                        switch (track.SectorSize)
                        {
                            case 2448:
                                sBase = new CUE.SS_2352()
                                {
                                    Policy = IN_DiscMountPolicy                                 
                                };
                                userSector = 2352;                          
                                break;
                            case 2048:
                            default:
                                sBase = new CUE.SS_Mode1_2048()
                                {
                                    Policy = IN_DiscMountPolicy
                                };
                                userSector = 2048;
                                break;
                            
                                //throw new Exception("Not supported: Sector Size " + track.SectorSize);
                        }

                        // configure blob
                        sBase.Blob = mdfBlob;
                        sBase.BlobOffset = currBlobOffset;

                        currBlobOffset += track.SectorSize; // userSector;
                        
                        // add subchannel data
                        relMSF++;
                        BCD2 tno, ino;

                        //this should actually be zero. im not sure if this is stored as BCD2 or not
                        tno = BCD2.FromDecimal(track.TrackNo);

                        //these are special values.. I think, taken from this:
                        //http://www.staff.uni-mainz.de/tacke/scsi/SCSI2-14.html
                        //the CCD will contain Points as decimal values except for these specially converted decimal values which should stay as BCD. 
                        //Why couldn't they all be BCD? I don't know. I guess because BCD is inconvenient, but only A0 and friends have special meaning. It's confusing.
                        ino = BCD2.FromDecimal(track.Point);
                        if (track.Point == 0xA0) ino.BCDValue = 0xA0;
                        else if (track.Point == 0xA1) ino.BCDValue = 0xA1;
                        else if (track.Point == 0xA2) ino.BCDValue = 0xA2;

                        // get ADR & Control from ADR_Control byte
                        byte adrc = Convert.ToByte(track.ADR_Control);
                        var Control = adrc & 0x0F;
                        var ADR = adrc >> 4;

                        var q = new SubchannelQ
                        {
                            q_status = SubchannelQ.ComputeStatus(ADR, (EControlQ)(Control & 0xF)),
                            q_tno = BCD2.FromDecimal(track.Point),
                            q_index = ino,
                            AP_Timestamp = disc._Sectors.Count,
                            Timestamp = relMSF - Convert.ToInt32(track.ExtraBlock.Pregap)
                        };

                        sBase.sq = q;

                        disc._Sectors.Add(sBase);
                        
                    }
                }
            }

            return disc;
        }

    } //class MDS_Format
}


