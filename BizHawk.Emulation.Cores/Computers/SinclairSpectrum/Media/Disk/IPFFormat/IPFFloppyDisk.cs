using BizHawk.Common;
using BizHawk.Common.NumberExtensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
    public class IPFFloppyDisk : FloppyDisk
    {
        /// <summary>
        /// The format type
        /// </summary>
        public override DiskType DiskFormatType => DiskType.IPF;

        /// <summary>
        /// Attempts to parse incoming disk data 
        /// </summary>
        /// <returns>
        /// TRUE:   disk parsed
        /// FALSE:  unable to parse disk
        /// </returns>
        public override bool ParseDisk(byte[] data)
        {
            // look for standard magic string
            string ident = Encoding.ASCII.GetString(data, 0, 16);

            if (!ident.ToUpper().Contains("CAPS"))
            {
                // incorrect format
                return false;
            }

            int pos = 0;

            List<IPFBlock> blocks = new List<IPFBlock>();

            while (pos < data.Length)
            {
                try
                {
                    var block = IPFBlock.ParseNextBlock(ref pos, this, data, blocks);

                    if (block == null)
                    {
                        // EOF
                        break;
                    }

                    if (block.RecordType == RecordHeaderType.None)
                    {
                        // unknown block
                    }

                    blocks.Add(block);
                }
                catch (Exception ex)
                {
					var e = ex.ToString();
                }
            }

            // now process the blocks
            var infoBlock = blocks.Where(a => a.RecordType == RecordHeaderType.INFO).FirstOrDefault();
            var IMGEblocks = blocks.Where(a => a.RecordType == RecordHeaderType.IMGE).ToList();
            var DATAblocks = blocks.Where(a => a.RecordType == RecordHeaderType.DATA);

            DiskHeader.NumberOfTracks = (byte)(IMGEblocks.Count());
            DiskHeader.NumberOfSides = (byte)(infoBlock.INFOmaxSide + 1);
            DiskTracks = new Track[DiskHeader.NumberOfTracks];

            for (int t = 0; t < DiskHeader.NumberOfTracks * DiskHeader.NumberOfSides; t++)
            {
                // each imge block represents one track
                var img = IMGEblocks[t];
                DiskTracks[t] = new Track();
                var trk = DiskTracks[t];

                var blockCount = img.IMGEblockCount;
                var dataBlock = DATAblocks.Where(a => a.DATAdataKey == img.IMGEdataKey).FirstOrDefault();

                trk.SideNumber = (byte)img.IMGEside;
                trk.TrackNumber = (byte)img.IMGEtrack;

                trk.Sectors = new Sector[blockCount];

                // process data block descriptors
                int p = 0;
                for (int d = 0; d < blockCount; d++)
                {
                    var extraDataAreaStart = 32 * blockCount;
                    trk.Sectors[d] = new Sector();
                    var sector = trk.Sectors[d];

                    int dataBits = MediaConverter.GetBEInt32(dataBlock.DATAextraDataRaw, p); p += 4;
                    int gapBits = MediaConverter.GetBEInt32(dataBlock.DATAextraDataRaw, p); p += 4;
                    int dataBytes;
                    int gapBytes;
                    int gapOffset;
                    int cellType;
                    if (infoBlock.INFOencoderType == 1)
                    {
                        dataBytes = MediaConverter.GetBEInt32(dataBlock.DATAextraDataRaw, p); p += 4;
                        gapBytes = MediaConverter.GetBEInt32(dataBlock.DATAextraDataRaw, p); p += 4;
                    }
                    else if (infoBlock.INFOencoderType == 2)
                    {
                        gapOffset = MediaConverter.GetBEInt32(dataBlock.DATAextraDataRaw, p); p += 4;
                        cellType = MediaConverter.GetBEInt32(dataBlock.DATAextraDataRaw, p); p += 4;
                    }
                    int encoderType = MediaConverter.GetBEInt32(dataBlock.DATAextraDataRaw, p); p += 4;
                    int? blockFlags = null;
                    if (infoBlock.INFOencoderType == 2)
                    {
                        blockFlags = MediaConverter.GetBEInt32(dataBlock.DATAextraDataRaw, p);
                    }
                    p += 4;

                    int gapDefault = MediaConverter.GetBEInt32(dataBlock.DATAextraDataRaw, p); p += 4;
                    int dataOffset = MediaConverter.GetBEInt32(dataBlock.DATAextraDataRaw, p); p += 4;

                    // gap stream elements
                    if (infoBlock.INFOencoderType == 2 && gapBits != 0 && blockFlags != null)
                    {
                        if (!blockFlags.Value.Bit(1) && !blockFlags.Value.Bit(0))
                        {
                            // no gap stream
                        }
                        if (!blockFlags.Value.Bit(1) && blockFlags.Value.Bit(0))
                        {
                            // Forward gap stream list only
                        }
                        if (blockFlags.Value.Bit(1) && !blockFlags.Value.Bit(0))
                        {
                            //  Backward gap stream list only
                        }
                        if (blockFlags.Value.Bit(1) && blockFlags.Value.Bit(0))
                        {
                            // Forward and Backward stream lists
                        }
                    }

                    // data stream elements
                    if (dataBits != 0)
                    {
                        var dsLocation = dataOffset;

                        for (;;)
                        {
                            byte dataHead = dataBlock.DATAextraDataRaw[dsLocation++];
                            if (dataHead == 0)
                            {
                                // end of data stream list
                                break;
                            }

                            var sampleSize = ((dataHead & 0xE0) >> 5);
                            var dataType = dataHead & 0x1F;
                            byte[] dSize = new byte[sampleSize];
                            Array.Copy(dataBlock.DATAextraDataRaw, dsLocation, dSize, 0, sampleSize);
                            var dataSize = MediaConverter.GetBEInt32FromByteArray(dSize);
                            dsLocation += dSize.Length;
                            int dataLen;
                            byte[] dataStream = new byte[0];

                            if (blockFlags != null && blockFlags.Value.Bit(2))
                            {
                                // bits
                                if (dataType != 5)
                                {
                                    dataLen = dataSize / 8;
                                    if (dataSize % 8 != 0)
                                    {
                                        // bits left over
                                    }
                                    dataStream = new byte[dataLen];
                                    Array.Copy(dataBlock.DATAextraDataRaw, dsLocation, dataStream, 0, dataLen);
                                }
                            }
                            else
                            {
                                // bytes
                                if (dataType != 5)
                                {
                                    dataStream = new byte[dataSize];
                                    Array.Copy(dataBlock.DATAextraDataRaw, dsLocation, dataStream, 0, dataSize);
                                }
                            }

                            // dataStream[] now contains the data
                            switch (dataType)
                            {
                                // SYNC
                                case 1:
                                    break;
                                // DATA
                                case 2:
                                    if (dataStream.Length == 7)
                                    {
                                        // ID
                                        // first byte IAM
                                        sector.TrackNumber = dataStream[1];
                                        sector.SideNumber = dataStream[2];
                                        sector.SectorID = dataStream[3];
                                        sector.SectorSize = dataStream[4];
                                    }
                                    else if (dataStream.Length > 255)
                                    {
                                        // DATA
                                        // first byte DAM
                                        if (dataStream[0] == 0xF8)
                                        {
                                            // deleted address mark
                                            //sector.Status1
                                        }
                                        sector.SectorData = new byte[dataStream.Length - 1 - 2];
                                        Array.Copy(dataStream, 1, sector.SectorData, 0, dataStream.Length - 1 - 2);
                                    }
                                    break;
                                // GAP
                                case 3:
                                    break;
                                // RAW
                                case 4:
                                    break;
                                // FUZZY
                                case 5:
                                    break;
                                default:
                                    break;
                            }


                            dsLocation += dataStream.Length;
                        }
                    }
                }              
            }

            return true;
        }

        public class IPFBlock
        {
            public RecordHeaderType RecordType;
            public int BlockLength;
            public int CRC;
            public byte[] RawBlockData;
            public int StartPos;

            #region INFO

            public int INFOmediaType;
            public int INFOencoderType;
            public int INFOencoderRev;
            public int INFOfileKey;
            public int INFOfileRev;
            public int INFOorigin;
            public int INFOminTrack;
            public int INFOmaxTrack;
            public int INFOminSide;
            public int INFOmaxSide;
            public int INFOcreationDate;
            public int INFOcreationTime;
            public int INFOplatform1;
            public int INFOplatform2;
            public int INFOplatform3;
            public int INFOplatform4;
            public int INFOdiskNumber;
            public int INFOcreatorId;

            #endregion

            #region IMGE

            public int IMGEtrack;
            public int IMGEside;
            public int IMGEdensity;
            public int IMGEsignalType;
            public int IMGEtrackBytes;
            public int IMGEstartBytePos;
            public int IMGEstartBitPos;
            public int IMGEdataBits;
            public int IMGEgapBits;
            public int IMGEtrackBits;
            public int IMGEblockCount;
            public int IMGEencoderProcess;
            public int IMGEtrackFlags;
            public int IMGEdataKey;

            #endregion

            #region DATA

            public int DATAlength;
            public int DATAbitSize;
            public int DATAcrc;
            public int DATAdataKey;
            public byte[] DATAextraDataRaw;

            #endregion

            public static IPFBlock ParseNextBlock(ref int startPos, FloppyDisk disk, byte[] data, List<IPFBlock> blockCollection)
            {
                IPFBlock ipf = new IPFBlock();
                ipf.StartPos = startPos;

                if (startPos >= data.Length)
                {
                    // EOF
                    return null;
                }

                // assume the startPos passed in is actually the start of a new block
                // look for record header ident
                string ident = Encoding.ASCII.GetString(data, startPos, 4);
                startPos += 4;
                try
                {
                    ipf.RecordType = (RecordHeaderType)Enum.Parse(typeof(RecordHeaderType), ident);
                }
                catch
                {
                    ipf.RecordType = RecordHeaderType.None;
                }

                // setup for actual block size
                ipf.BlockLength = MediaConverter.GetBEInt32(data, startPos); startPos += 4;
                ipf.CRC = MediaConverter.GetBEInt32(data, startPos); startPos += 4;
                ipf.RawBlockData = new byte[ipf.BlockLength];
                Array.Copy(data, ipf.StartPos, ipf.RawBlockData, 0, ipf.BlockLength);

                switch (ipf.RecordType)
                {
                    // Nothing to process / unknown
                    // just move ahead
                    case RecordHeaderType.CAPS:
                    case RecordHeaderType.TRCK:
                    case RecordHeaderType.DUMP:
                    case RecordHeaderType.CTEI:
                    case RecordHeaderType.CTEX:
                    default:
                        startPos = ipf.StartPos + ipf.BlockLength;
                        break;

                    // INFO block
                    case RecordHeaderType.INFO:
                        // INFO header is followed immediately by an INFO block
                        ipf.INFOmediaType = MediaConverter.GetBEInt32(data, startPos); startPos += 4;
                        ipf.INFOencoderType = MediaConverter.GetBEInt32(data, startPos); startPos += 4;
                        ipf.INFOencoderRev = MediaConverter.GetBEInt32(data, startPos); startPos += 4;
                        ipf.INFOfileKey = MediaConverter.GetBEInt32(data, startPos); startPos += 4;
                        ipf.INFOfileRev = MediaConverter.GetBEInt32(data, startPos); startPos += 4;
                        ipf.INFOorigin = MediaConverter.GetBEInt32(data, startPos); startPos += 4;
                        ipf.INFOminTrack = MediaConverter.GetBEInt32(data, startPos); startPos += 4;
                        ipf.INFOmaxTrack = MediaConverter.GetBEInt32(data, startPos); startPos += 4;
                        ipf.INFOminSide = MediaConverter.GetBEInt32(data, startPos); startPos += 4;
                        ipf.INFOmaxSide = MediaConverter.GetBEInt32(data, startPos); startPos += 4;
                        ipf.INFOcreationDate = MediaConverter.GetBEInt32(data, startPos); startPos += 4;
                        ipf.INFOcreationTime = MediaConverter.GetBEInt32(data, startPos); startPos += 4;
                        ipf.INFOplatform1 = MediaConverter.GetBEInt32(data, startPos); startPos += 4;
                        ipf.INFOplatform2 = MediaConverter.GetBEInt32(data, startPos); startPos += 4;
                        ipf.INFOplatform3 = MediaConverter.GetBEInt32(data, startPos); startPos += 4;
                        ipf.INFOplatform4 = MediaConverter.GetBEInt32(data, startPos); startPos += 4;
                        ipf.INFOdiskNumber = MediaConverter.GetBEInt32(data, startPos); startPos += 4;
                        ipf.INFOcreatorId = MediaConverter.GetBEInt32(data, startPos); startPos += 4;
                        startPos += 12; // reserved
                        break;

                    case RecordHeaderType.IMGE:
                        ipf.IMGEtrack = MediaConverter.GetBEInt32(data, startPos); startPos += 4;
                        ipf.IMGEside = MediaConverter.GetBEInt32(data, startPos); startPos += 4;
                        ipf.IMGEdensity = MediaConverter.GetBEInt32(data, startPos); startPos += 4;
                        ipf.IMGEsignalType = MediaConverter.GetBEInt32(data, startPos); startPos += 4;
                        ipf.IMGEtrackBytes = MediaConverter.GetBEInt32(data, startPos); startPos += 4;
                        ipf.IMGEstartBytePos = MediaConverter.GetBEInt32(data, startPos); startPos += 4;
                        ipf.IMGEstartBitPos = MediaConverter.GetBEInt32(data, startPos); startPos += 4;
                        ipf.IMGEdataBits = MediaConverter.GetBEInt32(data, startPos); startPos += 4;
                        ipf.IMGEgapBits = MediaConverter.GetBEInt32(data, startPos); startPos += 4;
                        ipf.IMGEtrackBits = MediaConverter.GetBEInt32(data, startPos); startPos += 4;
                        ipf.IMGEblockCount = MediaConverter.GetBEInt32(data, startPos); startPos += 4;
                        ipf.IMGEencoderProcess = MediaConverter.GetBEInt32(data, startPos); startPos += 4;
                        ipf.IMGEtrackFlags = MediaConverter.GetBEInt32(data, startPos); startPos += 4;
                        ipf.IMGEdataKey = MediaConverter.GetBEInt32(data, startPos); startPos += 4;
                        startPos += 12; // reserved
                        break;

                    case RecordHeaderType.DATA:
                        ipf.DATAlength = MediaConverter.GetBEInt32(data, startPos);
                        if (ipf.DATAlength == 0)
                        {
                            ipf.DATAextraDataRaw = new byte[0];
                            ipf.DATAlength = 0;
                        }
                        else
                        {
                            ipf.DATAextraDataRaw = new byte[ipf.DATAlength];
                        }
                        startPos += 4;
                        ipf.DATAbitSize = MediaConverter.GetBEInt32(data, startPos); startPos += 4;
                        ipf.DATAcrc = MediaConverter.GetBEInt32(data, startPos); startPos += 4;
                        ipf.DATAdataKey = MediaConverter.GetBEInt32(data, startPos); startPos += 4;

                        if (ipf.DATAlength != 0)
                        {
                            Array.Copy(data, startPos, ipf.DATAextraDataRaw, 0, ipf.DATAlength);
                        }

                        startPos += ipf.DATAlength;
                        break;
                }

                return ipf;
            }
        }

        public enum RecordHeaderType
        {
            None,
            CAPS,
            DUMP,
            DATA,
            TRCK,
            INFO,
            IMGE,
            CTEI,
            CTEX,            
        }


        /// <summary>
        /// State serlialization
        /// </summary>
        public override void SyncState(Serializer ser)
        {
            ser.BeginSection("Plus3FloppyDisk");

            ser.Sync(nameof(CylinderCount), ref CylinderCount);
            ser.Sync(nameof(SideCount), ref SideCount);
            ser.Sync(nameof(BytesPerTrack), ref BytesPerTrack);
            ser.Sync(nameof(WriteProtected), ref WriteProtected);
            ser.SyncEnum(nameof(Protection), ref Protection);

            ser.Sync(nameof(DirtyData), ref DirtyData);
            if (DirtyData)
            {

            }

            // sync deterministic track and sector counters
            ser.Sync(nameof( _randomCounter), ref _randomCounter);
            RandomCounter = _randomCounter;

            ser.EndSection();
        }
    }
}
