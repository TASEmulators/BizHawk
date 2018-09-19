using BizHawk.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
    public class UDI1_0FloppyDisk : FloppyDisk
    {
        /// <summary>
        /// The format type
        /// </summary>
        public override DiskType DiskFormatType => DiskType.UDI;

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
            string ident = Encoding.ASCII.GetString(data, 0, 4);

            if (!ident.StartsWith("UDI!") && !ident.StartsWith("udi!"))
            {
                // incorrect format
                return false;
            }

            if (data[0x08] != 0)
            {
                // wrong version
                return false;
            }

            if (ident == "udi!")
            {
                // cant handle compression yet
                return false;
            }

            DiskHeader.DiskIdent = ident;
            DiskHeader.NumberOfTracks = (byte)(data[0x09] + 1);
            DiskHeader.NumberOfSides = (byte)(data[0x0A] + 1);

            DiskTracks = new Track[DiskHeader.NumberOfTracks * DiskHeader.NumberOfSides];

            int fileSize = MediaConverter.GetInt32(data, 4); // not including the final 4-byte checksum

            // ignore extended header
            var extHdrSize = MediaConverter.GetInt32(data, 0x0C);
            int pos = 0x10 + extHdrSize;

            // process track information
            for (int t = 0; t < DiskHeader.NumberOfTracks; t++)
            {
                DiskTracks[t] = new UDIv1Track();
                DiskTracks[t].TrackNumber = (byte)t;
                DiskTracks[t].SideNumber = 0;
                DiskTracks[t].TrackType = data[pos++];
                DiskTracks[t].TLEN = MediaConverter.GetWordValue(data, pos); pos += 2;
                DiskTracks[t].TrackData = new byte[DiskTracks[t].TLEN + DiskTracks[t].CLEN];
                Array.Copy(data, pos, DiskTracks[t].TrackData, 0, DiskTracks[t].TLEN + DiskTracks[t].CLEN);
                pos += DiskTracks[t].TLEN + DiskTracks[t].CLEN;
            }

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
            string ident = Encoding.ASCII.GetString(data, 0, 4);

            if (!ident.StartsWith("UDI!") && !ident.StartsWith("udi!"))
            {
                // incorrect format
                return false;
            }

            if (data[0x08] != 0)
            {
                // wrong version
                return false;
            }

            if (ident == "udi!")
            {
                // cant handle compression yet
                return false;
            }

            byte[] S0 = new byte[data.Length];
            byte[] S1 = new byte[data.Length];

            // header
            var extHdr = MediaConverter.GetInt32(data, 0x0C);
            Array.Copy(data, 0, S0, 0, 0x10 + extHdr);
            Array.Copy(data, 0, S1, 0, 0x10 + extHdr);
            // change side number
            S0[0x0A] = 0;
            S1[0x0A] = 0;

            int pos = 0x10 + extHdr;
            int fileSize = MediaConverter.GetInt32(data, 4); // not including the final 4-byte checksum

            int s0Pos = pos;
            int s1Pos = pos;

            // process track information
            for (int t = 0; t < (data[0x09] + 1) * 2; t++)
            {
                var TLEN = MediaConverter.GetWordValue(data, pos + 1);
                var CLEN = TLEN / 8 + (TLEN % 8 / 7) / 8;
                var blockSize = TLEN + CLEN + 3;

                // 2 sided image: side 0 tracks will all have t as an even number
                try
                {
                    if (t == 0 || t % 2 == 0)
                    {
                        Array.Copy(data, pos, S0, s0Pos, blockSize);
                        s0Pos += blockSize;
                    }
                    else
                    {
                        Array.Copy(data, pos, S1, s1Pos, blockSize);
                        s1Pos += blockSize;
                    }
                }
                catch (Exception ex)
                {

                }
                

                pos += blockSize;
            }

            // skip checkum bytes for now

            byte[] s0final = new byte[s0Pos];
            byte[] s1final = new byte[s1Pos];
            Array.Copy(S0, 0, s0final, 0, s0Pos);
            Array.Copy(S1, 0, s1final, 0, s1Pos);

            results.Add(s0final);
            results.Add(s1final);

            return true;
        }

        public class UDIv1Track : Track
        {
            /// <summary>
            /// Parse the UDI TrackData byte[] array into sector objects
            /// </summary>
            public override Sector[] Sectors
            {
                get
                {
                    List<UDIv1Sector> secs = new List<UDIv1Sector>();
                    var datas = TrackData.Skip(3).Take(TLEN).ToArray();
                    var clocks = new BitArray(TrackData.Skip(3 + TLEN).Take(CLEN).ToArray());

                    return secs.ToArray();
                }
            }
        }

        public class UDIv1Sector : Sector
        {

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
