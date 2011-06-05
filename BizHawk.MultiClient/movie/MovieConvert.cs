using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace BizHawk.MultiClient
{
    public static class MovieConvert
    {
        public static Movie ConvertFCM(string path)
        {
            Movie m = new Movie(Path.ChangeExtension(path, ".tas"), MOVIEMODE.PLAY);
            
            FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read);
            BinaryReader r = new BinaryReader(fs);
            //TODO: if fail to open...do some kind of error

            UInt32 magic = r.ReadUInt32();
            
            UInt32 version = r.ReadUInt32();
            m.SetHeaderLine(MovieHeader.MovieVersion, "FCEU movie version " + version.ToString() + " (.fcm)");
            
            byte[] flags = new byte[4];
            for (int x = 0; x < 4; x++)
                flags[x] = r.ReadByte();

            UInt32 InputLength = r.ReadUInt32();
            
            UInt32 rerecords = r.ReadUInt32();
            m.SetHeaderLine(MovieHeader.RERECORDS, rerecords.ToString());

            UInt32 movieDataSize = r.ReadUInt32();
            UInt32 savestateOffset = r.ReadUInt32();
            UInt32 firstFrameOffset = r.ReadUInt32();

            byte[] romCheckSum = r.ReadBytes(16);
            //TODO: ROM checksum movie header line

            UInt32 EmuVersion = r.ReadUInt32();
            m.SetHeaderLine(MovieHeader.EMULATIONVERSION, "FCEU " + EmuVersion.ToString());

            List<byte> romBytes = new List<byte>();
            while (true)
            {
                if (r.PeekChar() == 0)
                    break;
                else
                    romBytes.Add(r.ReadByte());
            }
            string rom = System.Text.Encoding.UTF8.GetString(romBytes.ToArray());
            m.SetHeaderLine(MovieHeader.GAMENAME, rom);

            r.ReadByte(); //Advance past null byte

            List<byte> authorBytes = new List<byte>();
            while (true)
            {
                if (r.PeekChar() == 0)
                    break;
                else
                    authorBytes.Add(r.ReadByte());
            }
            string author = System.Text.Encoding.UTF8.GetString(authorBytes.ToArray());
            m.SetHeaderLine(MovieHeader.AUTHOR, author);

            r.ReadByte(); //Advance past null byte
            
            bool movieSyncHackOn = true;
            if ((int)(flags[0] & 16) > 0)
                movieSyncHackOn = false;

            bool pal = false;
            if ((int)(flags[0] & 4) > 0)
                pal = true;

            m.SetHeaderLine("PAL", pal.ToString());

            //Power on vs reset
            if ((int)(flags[0] & 8) > 0)
            { } //Power-on = default
            else if ((int)(flags[0] & 2) > 0)
            { } //we don't support start from reset, do some kind of notification here
            else
            { } //this movie starts from savestate, freak out here

            //Advance to first byte of input data
            byte[] throwaway = new byte[firstFrameOffset];
            r.Read(throwaway, 0, (int)firstFrameOffset);

            //moviedatasize stuff

            //read frame data
            //TODO: special commands like fds disk switch, etc, and power/reset

            //TODO: use stringbuilder class for speed
            string ButtonLookup = "RLDUSsBARLDUSsBARLDUSsBARLDUSsBA"; //TODO: This assumes input data is the same in fcm as bizhawk, which it isn't
            string frame = "|0|"; //TODO: read reset command rather than hard code it off
            for (int x = 0; x < InputLength; x++)
            {
                for (int y = 0; y < 8; y++) //TODO: read all controllers!
                {
                    //TODO: Check for reset or power-on on first frame
                    int z = r.ReadBit();
                    if (z > 0)
                        frame += ButtonLookup[y];
                    else
                        frame += ".";
                }
                frame += "|";
                for (int y = 0; y < 24; y++)
                    r.ReadBit();    //lose 3 remaining controllers for now
                m.AddFrame(frame);
            }


            //set 4 score flag if necessary
            r.Close();
            return m;
        }

        public static Movie ConvertMMV(string path)
        {
            Movie m = new Movie(Path.ChangeExtension(path, ".tas"), MOVIEMODE.PLAY);

            FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read);
            BinaryReader r = new BinaryReader(fs);

            UInt32 signature = r.ReadUInt32();
            //4 asci encoded chars, convert.  If not MMV\0 then error

            UInt32 version = r.ReadUInt32();
            //TODO: 4 ascii encoded chars, format properly
            m.SetHeaderLine(MovieHeader.MovieVersion, "Dega version " + version.ToString());

            UInt32 framecount = r.ReadUInt32();
            
            UInt32 rerecords = r.ReadUInt32();
            m.SetHeaderLine(MovieHeader.RERECORDS, rerecords.ToString());

            UInt32 IsFromReset = r.ReadUInt32();
            //TODO: we don't support movies that fail to start from reset

            UInt32 stateOffset = r.ReadUInt32();
            UInt32 inputDataOffset = r.ReadUInt32();
            UInt32 inputPacketSize = r.ReadUInt32();
            
            byte[] authorBytes = new byte[24];
            for (int x = 0; x < 24; x++)
                authorBytes[x] = r.ReadByte();

            string author = System.Text.Encoding.UTF8.GetString(authorBytes.ToArray());
            //TODO: remove null characters
            m.SetHeaderLine(MovieHeader.AUTHOR, author);

            //4-byte little endian flags
            r.ReadBit(); //First bit unused

            int pal = r.ReadBit();
            int japan = r.ReadBit();
            int gamegear = r.ReadBit();

            for (int x = 0; x < 28; x++)
                r.ReadBit();     //Unused

            

            return m;
        }

        public static string ConvertMCM(string path)
        {
            string converted = Path.ChangeExtension(path, ".tas");

            return converted;
        }

        public static string ConvertSMV(string path)
        {
            string converted = Path.ChangeExtension(path, ".tas");
            
            return converted;
        }

        public static string ConvertGMV(string path)
        {
            string converted = Path.ChangeExtension(path, ".tas");
            
            return converted;
        }

        public static string ConvertVBM(string path)
        {
            string converted = Path.ChangeExtension(path, ".tas");
            
            return converted;
        }
    }
}
