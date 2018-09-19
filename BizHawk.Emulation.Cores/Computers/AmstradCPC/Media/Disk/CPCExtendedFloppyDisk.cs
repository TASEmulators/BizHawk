using System.Text;
using BizHawk.Common;
using System;
using System.Collections.Generic;

namespace BizHawk.Emulation.Cores.Computers.AmstradCPC
{
    /// <summary>
    /// Logical object representing a standard +3 disk image
    /// </summary>
    public class CPCExtendedFloppyDisk : FloppyDisk
    {
        /// <summary>
        /// The format type
        /// </summary>
        public override DiskType DiskFormatType => DiskType.CPCExtended;

        /// <summary>
        /// Attempts to parse incoming disk data
        /// </summary>
        /// <param name="diskData"></param>
        /// <returns>
        /// TRUE:   disk parsed
        /// FALSE:  unable to parse disk
        /// </returns>
        public override bool ParseDisk(byte[] data)
        {
            // look for standard magic string
            string ident = Encoding.ASCII.GetString(data, 0, 16);

            if (!ident.ToUpper().Contains("EXTENDED CPC DSK"))
            {
                // incorrect format
                return false;
            }

            // read the disk information block
            DiskHeader.DiskIdent = ident;
            DiskHeader.DiskCreatorString = Encoding.ASCII.GetString(data, 0x22, 14);
            DiskHeader.NumberOfTracks = data[0x30];
            DiskHeader.NumberOfSides = data[0x31];
            DiskHeader.TrackSizes = new int[DiskHeader.NumberOfTracks * DiskHeader.NumberOfSides];
            DiskTracks = new Track[DiskHeader.NumberOfTracks * DiskHeader.NumberOfSides];
            DiskData = data;
            int pos = 0x34;

            if (DiskHeader.NumberOfSides > 1)
            {
                StringBuilder sbm = new StringBuilder();
                sbm.AppendLine();
                sbm.AppendLine();
                sbm.AppendLine("The detected disk image contains multiple sides.");
                sbm.AppendLine("This is NOT currently supported in CPCHawk.");
                sbm.AppendLine("Please find an alternate image/dump where each side has been saved as a separate *.dsk image (and use the mutli-disk bundler tool to load into Bizhawk).");
                throw new System.NotImplementedException(sbm.ToString());
            }

            for (int i = 0; i < DiskHeader.NumberOfTracks * DiskHeader.NumberOfSides; i++)
            {
                DiskHeader.TrackSizes[i] = data[pos++] * 256;                
            }

            // move to first track information block
            pos = 0x100;

            // parse each track
            for (int i = 0; i < DiskHeader.NumberOfTracks * DiskHeader.NumberOfSides; i++)
            {
                // check for unformatted track
                if (DiskHeader.TrackSizes[i] == 0)
                {
                    DiskTracks[i] = new Track();
                    DiskTracks[i].Sectors = new Sector[0];
                    continue;
                }

                int p = pos;
                DiskTracks[i] = new Track();

                // track info block
                DiskTracks[i].TrackIdent = Encoding.ASCII.GetString(data, p, 12);
                p += 16;
                DiskTracks[i].TrackNumber = data[p++];
                DiskTracks[i].SideNumber = data[p++];
                DiskTracks[i].DataRate = data[p++];
                DiskTracks[i].RecordingMode = data[p++];
                DiskTracks[i].SectorSize = data[p++];
                DiskTracks[i].NumberOfSectors = data[p++];
                DiskTracks[i].GAP3Length = data[p++];
                DiskTracks[i].FillerByte = data[p++];

                int dpos = pos + 0x100;

                // sector info list
                DiskTracks[i].Sectors = new Sector[DiskTracks[i].NumberOfSectors];                
                for (int s = 0; s < DiskTracks[i].NumberOfSectors; s++)
                {
                    DiskTracks[i].Sectors[s] = new Sector();

                    DiskTracks[i].Sectors[s].TrackNumber = data[p++];
                    DiskTracks[i].Sectors[s].SideNumber = data[p++];
                    DiskTracks[i].Sectors[s].SectorID = data[p++];
                    DiskTracks[i].Sectors[s].SectorSize = data[p++];
                    DiskTracks[i].Sectors[s].Status1 = data[p++];
                    DiskTracks[i].Sectors[s].Status2 = data[p++];
                    DiskTracks[i].Sectors[s].ActualDataByteLength = MediaConverter.GetWordValue(data, p);
                    p += 2;

                    // sector data - begins at 0x100 offset from the start of the track info block (in this case dpos)
                    DiskTracks[i].Sectors[s].SectorData = new byte[DiskTracks[i].Sectors[s].ActualDataByteLength];

                    // copy the data
                    for (int b = 0; b < DiskTracks[i].Sectors[s].ActualDataByteLength; b++)
                    {
                        DiskTracks[i].Sectors[s].SectorData[b] = data[dpos + b];
                    }

                    // check for multiple weak/random sectors stored
                    if (DiskTracks[i].Sectors[s].SectorSize <= 7)
                    {
                        // sectorsize n=8 is equivilent to n=0 - FDC will use DTL for length
                        int specifiedSize = 0x80 << DiskTracks[i].Sectors[s].SectorSize;

                        if (specifiedSize < DiskTracks[i].Sectors[s].ActualDataByteLength)
                        {
                            // more data stored than sectorsize defines
                            // check for multiple weak/random copies
                            if (DiskTracks[i].Sectors[s].ActualDataByteLength % specifiedSize != 0)
                            {
                                DiskTracks[i].Sectors[s].ContainsMultipleWeakSectors = true;
                            }
                        }                        
                    }

                    // move dpos to the next sector data postion
                    dpos += DiskTracks[i].Sectors[s].ActualDataByteLength;
                }

                // move to the next track info block
                pos += DiskHeader.TrackSizes[i];
            }

            // run protection scheme detector
            ParseProtection();

            return true;
        }

        /// <summary>
        /// Takes a double-sided disk byte array and converts into 2 single-sided arrays
        /// </summary>
        /// <param name="data"></param>
        /// <param name="results"></param>
        /// <returns></returns>
        public static bool SplitDoubleSided(byte[] data, List<byte[]> results)
        {
            // look for standard magic string
            string ident = Encoding.ASCII.GetString(data, 0, 16);
            if (!ident.ToUpper().Contains("EXTENDED CPC DSK"))
            {
                // incorrect format
                return false;
            }

            byte[] S0 = new byte[data.Length];
            byte[] S1 = new byte[data.Length];

            // disk info block
            Array.Copy(data, 0, S0, 0, 0x100);
            Array.Copy(data, 0, S1, 0, 0x100);
            // change side number
            S0[0x31] = 1;
            S1[0x31] = 1;

            // extended format has different track sizes
            int[] trkSizes = new int[data[0x30] * data[0x31]];

            int pos = 0x34;
            for (int i = 0; i < data[0x30] * data[0x31]; i++)
            {
                trkSizes[i] = data[pos] * 256;
                // clear destination trk sizes (will be added later)
                S0[pos] = 0;
                S1[pos] = 0;
                pos++;
            }

            // start at track info blocks
            int mPos = 0x100;
            int s0Pos = 0x100;
            int s0tCount = 0;
            int s1tCount = 0;
            int s1Pos = 0x100;
            int tCount = 0;

            while (tCount < data[0x30] * data[0x31])
            {
                // which side is this?
                var side = data[mPos + 0x11];
                if (side == 0)
                {
                    // side 1
                    Array.Copy(data, mPos, S0, s0Pos, trkSizes[tCount]);
                    s0Pos += trkSizes[tCount];
                    // trk size table
                    S0[0x34 + s0tCount++] = (byte)(trkSizes[tCount] / 256);
                }
                else if (side == 1)
                {
                    // side 2
                    Array.Copy(data, mPos, S1, s1Pos, trkSizes[tCount]);
                    s1Pos += trkSizes[tCount];
                    // trk size table
                    S1[0x34 + s1tCount++] = (byte)(trkSizes[tCount] / 256);
                }
                
                mPos += trkSizes[tCount++];
            }

            byte[] s0final = new byte[s0Pos];
            byte[] s1final = new byte[s1Pos];
            Array.Copy(S0, 0, s0final, 0, s0Pos);
            Array.Copy(S1, 0, s1final, 0, s1Pos);

            results.Add(s0final);
            results.Add(s1final);

            return true;
        }

        /// <summary>
        /// State serlialization
        /// </summary>
        /// <param name="ser"></param>
        public override void SyncState(Serializer ser)
        {
            ser.BeginSection("Plus3FloppyDisk");

            ser.Sync("CylinderCount", ref CylinderCount);
            ser.Sync("SideCount", ref SideCount);
            ser.Sync("BytesPerTrack", ref BytesPerTrack);
            ser.Sync("WriteProtected", ref WriteProtected);
            ser.SyncEnum("Protection", ref Protection);

            ser.Sync("DirtyData", ref DirtyData);
            if (DirtyData)
            {

            }

            // sync deterministic track and sector counters
            ser.Sync(" _randomCounter", ref _randomCounter);
            RandomCounter = _randomCounter;

            ser.EndSection();
        }
    }
}
