
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Common
{
    /// <summary>
    /// A slightly convoluted way of determining the required System based on a *.dsk file
    /// This is here because (for probably good reason) there does not appear to be a route
    /// to BizHawk.Emulation.Cores from Bizhawk.Emultion.Common
    /// </summary>
    public class DSKIdentifier
    {
        /// <summary>
        /// Default fallthrough to AppleII
        /// </summary>
        public string IdentifiedSystem = "AppleII";
        private string PossibleIdent = "";

        private byte[] data;
        
        // dsk header
        public byte NumberOfTracks { get; set; }
        public byte NumberOfSides { get; set; }
        public int[] TrackSizes { get; set; }

        // state
        public int CylinderCount;
        public int SideCount;
        public int BytesPerTrack;

        public Track[] Tracks = null;

        public DSKIdentifier(byte[] imageData)
        {
            data = imageData;
            ParseDskImage();
        }

        private void ParseDskImage()
        {
            string ident = Encoding.ASCII.GetString(data, 0, 16).ToUpper();
            if (ident.Contains("MV - CPC"))
            {
                ParseDSK();
            }
            else if (ident.Contains("EXTENDED CPC DSK"))
            {
                ParseEDSK();
            }
            else
            {
                // fall through
                return;
            }

            CalculateFormat();
        }

        private void CalculateFormat()
        {
            // uses some of the work done here: https://github.com/damieng/DiskImageManager
            var trk = Tracks[0];

            // look for standard speccy bootstart
            if (trk.Sectors[0].SectorData != null && trk.Sectors[0].SectorData.Length > 0)
            {
                if (trk.Sectors[0].SectorData[0] == 0 && trk.Sectors[0].SectorData[1] == 0
                    && trk.Sectors[0].SectorData[2] == 40)
                {
                    PossibleIdent = "ZXSpectrum";
                }
            }

            // search for PLUS3DOS string
            foreach (var t in Tracks)
            {
                foreach (var s in t.Sectors)
                {
                    if (s.SectorData == null || s.SectorData.Length == 0)
                        continue;

                    string str = Encoding.ASCII.GetString(s.SectorData, 0, s.SectorData.Length).ToUpper();
                    if (str.Contains("PLUS3DOS"))
                    {
                        IdentifiedSystem = "ZXSpectrum";
                        return;
                    }
                }
            }

            // check for bootable status
            if (trk.Sectors[0].SectorData != null && trk.Sectors[0].SectorData.Length > 0)
            {
                int chksm = trk.Sectors[0].GetChecksum256();

                switch(chksm)
                {
                    case 3:
                        IdentifiedSystem = "ZXSpectrum";
                        return;
                    case 1:
                    case 255:
                        // different Amstrad PCW boot records
                        // just return CPC for now
                        IdentifiedSystem = "AmstradCPC";
                        return;
                }

                switch(trk.GetLowestSectorID())
                {
                    case 65:
                    case 193:
                        IdentifiedSystem = "AmstradCPC";
                        return;
                }
            }

            // at this point the disk is not standard bootable
            // try format analysis
            if (trk.Sectors.Length == 9 && trk.Sectors[0].SectorSize == 2)
            {
                switch(trk.GetLowestSectorID())
                {
                    case 1:
                        switch(trk.Sectors[0].GetChecksum256())
                        {
                            case 3:
                                IdentifiedSystem = "ZXSpectrum";
                                return;
                            case 1:
                            case 255:
                                // different Amstrad PCW checksums
                                // just return CPC for now
                                IdentifiedSystem = "AmstradCPC";
                                return;
                        }
                        break;
                    case 65:
                    case 193:
                        IdentifiedSystem = "AmstradCPC";
                        return;
                }
            }

            // could be an odd format disk
            switch (trk.GetLowestSectorID())
            {
                case 1:
                    if (trk.Sectors.Length == 8)
                    {
                        // CPC IBM
                        IdentifiedSystem = "AmstradCPC";
                        return;
                    }
                    break;
                case 65:
                case 193:
                    // possible CPC custom
                    PossibleIdent = "AmstradCPC";
                    break; 
            }

            // other custom ZX Spectrum formats
            if (NumberOfSides == 1 && trk.Sectors.Length == 10)
            {
                if (trk.Sectors[0].SectorData.Length > 10)
                {
                    if (trk.Sectors[0].SectorData[2] == 42 && trk.Sectors[0].SectorData[8] == 12)
                    {
                        switch (trk.Sectors[0].SectorData[5])
                        {
                            case 0:
                                if (trk.Sectors[1].SectorID == 8)
                                {
                                    switch (Tracks[1].Sectors[0].SectorID)
                                    {
                                        case 7:
                                            IdentifiedSystem = "ZXSpectrum";
                                            return;
                                        default:
                                            PossibleIdent = "ZXSpectrum";
                                            break;
                                    }
                                }
                                else
                                {
                                    PossibleIdent = "ZXSpectrum";
                                }
                                break;
                            case 1:
                                if (trk.Sectors[1].SectorID == 8)
                                {
                                    switch (Tracks[1].Sectors[0].SectorID)
                                    {
                                        case 7:
                                        case 1:
                                            IdentifiedSystem = "ZXSpectrum";
                                            return;
                                    }
                                }
                                else
                                {
                                    PossibleIdent = "ZXSpectrum";
                                }
                                break;
                        }
                    }

                    if (trk.Sectors[0].SectorData[7] == 3 &&
                        trk.Sectors[0].SectorData[9] == 23 &&
                        trk.Sectors[0].SectorData[2] == 40)
                    {
                        IdentifiedSystem = "ZXSpectrum";
                        return;
                    }
                }
            }

            // last chance. use the possible value
            if (IdentifiedSystem == "AppleII" && PossibleIdent != "")
            {
                IdentifiedSystem = "ZXSpectrum";
            }
        }

        private void ParseDSK()
        {
            NumberOfTracks = data[0x30];
            NumberOfSides = data[0x31];
            TrackSizes = new int[NumberOfTracks * NumberOfSides];
            Tracks = new Track[NumberOfTracks * NumberOfSides];
            int pos = 0x32;
            for (int i = 0; i < NumberOfTracks * NumberOfSides; i++)
            {
                TrackSizes[i] = (ushort)(data[pos] | data[pos + 1] << 8);
            }
            pos = 0x100;
            for (int i = 0; i < NumberOfTracks * NumberOfSides; i++)
            {
                if (TrackSizes[i] == 0)
                {
                    Tracks[i] = new Track();
                    Tracks[i].Sectors = new Sector[0];
                    continue;
                }
                int p = pos;
                Tracks[i] = new Track();
                Tracks[i].TrackIdent = Encoding.ASCII.GetString(data, p, 12);
                p += 16;
                Tracks[i].TrackNumber = data[p++];
                Tracks[i].SideNumber = data[p++];
                p += 2;
                Tracks[i].SectorSize = data[p++];
                Tracks[i].NumberOfSectors = data[p++];
                Tracks[i].GAP3Length = data[p++];
                Tracks[i].FillerByte = data[p++];
                int dpos = pos + 0x100;
                Tracks[i].Sectors = new Sector[Tracks[i].NumberOfSectors];
                for (int s = 0; s < Tracks[i].NumberOfSectors; s++)
                {
                    Tracks[i].Sectors[s] = new Sector();

                    Tracks[i].Sectors[s].TrackNumber = data[p++];
                    Tracks[i].Sectors[s].SideNumber = data[p++];
                    Tracks[i].Sectors[s].SectorID = data[p++];
                    Tracks[i].Sectors[s].SectorSize = data[p++];
                    Tracks[i].Sectors[s].Status1 = data[p++];
                    Tracks[i].Sectors[s].Status2 = data[p++];
                    Tracks[i].Sectors[s].ActualDataByteLength = (ushort)(data[p] | data[p + 1] << 8);
                    p += 2;
                    if (Tracks[i].Sectors[s].SectorSize == 0)
                    {
                        Tracks[i].Sectors[s].ActualDataByteLength = TrackSizes[i];
                    }
                    else if (Tracks[i].Sectors[s].SectorSize > 6)
                    {
                        Tracks[i].Sectors[s].ActualDataByteLength = TrackSizes[i];
                    }
                    else if (Tracks[i].Sectors[s].SectorSize == 6)
                    {
                        Tracks[i].Sectors[s].ActualDataByteLength = 0x1800;
                    }
                    else
                    {
                        Tracks[i].Sectors[s].ActualDataByteLength = 0x80 << Tracks[i].Sectors[s].SectorSize;
                    }
                    Tracks[i].Sectors[s].SectorData = new byte[Tracks[i].Sectors[s].ActualDataByteLength];
                    for (int b = 0; b < Tracks[i].Sectors[s].ActualDataByteLength; b++)
                    {
                        Tracks[i].Sectors[s].SectorData[b] = data[dpos + b];
                    }
                    dpos += Tracks[i].Sectors[s].ActualDataByteLength;
                }
                pos += TrackSizes[i];
            }
        }

        private void ParseEDSK()
        {
            NumberOfTracks = data[0x30];
            NumberOfSides = data[0x31];
            TrackSizes = new int[NumberOfTracks * NumberOfSides];
            Tracks = new Track[NumberOfTracks * NumberOfSides];
            int pos = 0x34;
            for (int i = 0; i < NumberOfTracks * NumberOfSides; i++)
            {
                TrackSizes[i] = data[pos++] * 256;
            }
            pos = 0x100;
            for (int i = 0; i < NumberOfTracks * NumberOfSides; i++)
            {
                if (TrackSizes[i] == 0)
                {
                    Tracks[i] = new Track();
                    Tracks[i].Sectors = new Sector[0];
                    continue;
                }
                int p = pos;
                Tracks[i] = new Track();
                Tracks[i].TrackIdent = Encoding.ASCII.GetString(data, p, 12);
                p += 16;
                Tracks[i].TrackNumber = data[p++];
                Tracks[i].SideNumber = data[p++];
                Tracks[i].DataRate = data[p++];
                Tracks[i].RecordingMode = data[p++];
                Tracks[i].SectorSize = data[p++];
                Tracks[i].NumberOfSectors = data[p++];
                Tracks[i].GAP3Length = data[p++];
                Tracks[i].FillerByte = data[p++];
                int dpos = pos + 0x100;
                Tracks[i].Sectors = new Sector[Tracks[i].NumberOfSectors];
                for (int s = 0; s < Tracks[i].NumberOfSectors; s++)
                {
                    Tracks[i].Sectors[s] = new Sector();

                    Tracks[i].Sectors[s].TrackNumber = data[p++];
                    Tracks[i].Sectors[s].SideNumber = data[p++];
                    Tracks[i].Sectors[s].SectorID = data[p++];
                    Tracks[i].Sectors[s].SectorSize = data[p++];
                    Tracks[i].Sectors[s].Status1 = data[p++];
                    Tracks[i].Sectors[s].Status2 = data[p++];
                    Tracks[i].Sectors[s].ActualDataByteLength = (ushort)(data[p] | data[p + 1] << 8);
                    p += 2;
                    Tracks[i].Sectors[s].SectorData = new byte[Tracks[i].Sectors[s].ActualDataByteLength];
                    for (int b = 0; b < Tracks[i].Sectors[s].ActualDataByteLength; b++)
                    {
                        Tracks[i].Sectors[s].SectorData[b] = data[dpos + b];
                    }
                    if (Tracks[i].Sectors[s].SectorSize <= 7)
                    {
                        int specifiedSize = 0x80 << Tracks[i].Sectors[s].SectorSize;
                        if (specifiedSize < Tracks[i].Sectors[s].ActualDataByteLength)
                        {
                            if (Tracks[i].Sectors[s].ActualDataByteLength % specifiedSize != 0)
                            {
                                Tracks[i].Sectors[s].ContainsMultipleWeakSectors = true;
                            }
                        }
                    }
                    dpos += Tracks[i].Sectors[s].ActualDataByteLength;
                }
                pos += TrackSizes[i];
            }
        }

        #region Internal Classes

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

            public byte GetLowestSectorID()
            {
                byte res = 0xFF;
                foreach (var s in Sectors)
                {
                    if (s.SectorID < res)
                        res = s.SectorID;
                }
                return res;
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

            public int GetChecksum256()
            {
                int res = 0;
                for (int i = 0; i < SectorData.Length; i++)
                {
                    res += SectorData[i] % 256;
                }
                return res;
            }
        }

        #endregion


    }
}
