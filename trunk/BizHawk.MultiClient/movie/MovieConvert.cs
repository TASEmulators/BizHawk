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

            byte[] signatureBytes = new byte[4];
            for (int x = 0; x < 4; x++)
                signatureBytes[x] = r.ReadByte();
            string signature = System.Text.Encoding.UTF8.GetString(signatureBytes);
            if (signature.Substring(0, 3) != "FCM")
                return null; //TODO: invalid movie type error

            
            UInt32 version = r.ReadUInt32();
            m.SetHeaderLine(MovieHeader.MovieVersion, "FCEU movie version " + version.ToString() + " (.fcm)");
            
            byte[] flags = new byte[4];
            for (int x = 0; x < 4; x++)
                flags[x] = r.ReadByte();

            UInt32 frameCount = r.ReadUInt32();
            
            UInt32 rerecords = r.ReadUInt32();
            m.SetHeaderLine(MovieHeader.RERECORDS, rerecords.ToString());

            UInt32 movieDataSize = r.ReadUInt32();
            UInt32 savestateOffset = r.ReadUInt32();
            UInt32 firstFrameOffset = r.ReadUInt32();

            byte[] romCheckSum = r.ReadBytes(16);
            //TODO: ROM checksum movie header line (MD5)

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
            //byte[] throwaway = new byte[firstFrameOffset];
            //r.Read(throwaway, 0, (int)firstFrameOffset);
            r.BaseStream.Position = firstFrameOffset;
            //moviedatasize stuff

            //read frame data
            //TODO: special commands like fds disk switch, etc, and power/reset

            //TODO: use stringbuilder class for speed
            string ButtonLookup = "RLDUSsBARLDUSsBARLDUSsBARLDUSsBA"; //TODO: This assumes input data is the same in fcm as bizhawk, which it isn't
            string frame = "|0|"; //TODO: read reset command rather than hard code it off
            for (int x = 0; x < frameCount; x++)
            {
                byte joy = r.ReadByte();
                
                
                //Read each byte of controller one data


                frame += "|";

                r.ReadBytes(3); //Lose remaining controllers for now
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

            byte[] signatureBytes = new byte[4];
            for (int x = 0; x < 4; x++)
                signatureBytes[x] = r.ReadByte();
            string signature = System.Text.Encoding.UTF8.GetString(signatureBytes);
            if (signature != "MMV\0")
                return null; //TODO: invalid movie type error

            UInt32 version = r.ReadUInt32();
            m.SetHeaderLine(MovieHeader.MOVIEVERSION, "Dega version " + version.ToString());

            UInt32 framecount = r.ReadUInt32();
            
            UInt32 rerecords = r.ReadUInt32();
            m.SetHeaderLine(MovieHeader.RERECORDS, rerecords.ToString());

            UInt32 IsFromReset = r.ReadUInt32();
            if (IsFromReset == 0)
                return null; //TODO: Movies from savestate not supported error

            UInt32 stateOffset = r.ReadUInt32();
            UInt32 inputDataOffset = r.ReadUInt32();
            UInt32 inputPacketSize = r.ReadUInt32();
            
            byte[] authorBytes = new byte[64];
            for (int x = 0; x < 64; x++)
                authorBytes[x] = r.ReadByte();

            string author = System.Text.Encoding.UTF8.GetString(authorBytes);
            //TODO: remove null characters
            m.SetHeaderLine(MovieHeader.AUTHOR, author);

            //4-byte little endian flags
            byte flags = r.ReadByte(); 

            bool pal;
            if ((int)(flags & 2) > 0)
                pal = true;
            else 
                pal = false;
            m.SetHeaderLine("PAL", pal.ToString());

            bool japan;
            if ((int)(flags & 4) > 0)
                japan = true;
            else
                japan = false;
            m.SetHeaderLine("Japan", japan.ToString());

            bool gamegear;
            if ((int)(flags & 8) > 0)
            {
                gamegear = true;
                m.SetHeaderLine(MovieHeader.PLATFORM, "GG");
            }
            else
            {
                gamegear = false;
                m.SetHeaderLine(MovieHeader.PLATFORM, "SMS");
            }

            r.ReadBytes(3); //Unused flags

            byte[] romnameBytes = new byte[128];
            for (int x = 0; x < 128; x++)
                romnameBytes[x] = r.ReadByte();
            string romname = System.Text.Encoding.UTF8.GetString(romnameBytes.ToArray());
            //TODO: remove null characters
            m.SetHeaderLine(MovieHeader.GAMENAME, romname);

            byte[] MD5Bytes = new byte[16];
            for (int x = 0; x < 16; x++)
                MD5Bytes[x] = r.ReadByte();
            string MD5 = System.Text.Encoding.UTF8.GetString(MD5Bytes.ToArray());
            //TODO: format correctly
            m.SetHeaderLine("MD5", MD5);

            
            for (int x = 0; x < (framecount); x++)
            {
                //TODO: use StringBuilder
                
                string frame = "|";
                char start;
                byte tmp;

                tmp = r.ReadByte();
                if ((int)(tmp & 1) > 0) frame += "U"; else frame += ".";
                if ((int)(tmp & 2) > 0) frame += "D"; else frame += ".";
                if ((int)(tmp & 4) > 0) frame += "L"; else frame += ".";
                if ((int)(tmp & 8) > 0) frame += "R"; else frame += ".";
                if ((int)(tmp & 16) > 0) frame += "1"; else frame += ".";
                if ((int)(tmp & 32) > 0) frame += "2|"; else frame += ".|";
                
                if ((int)(tmp & 64) > 0 && (!gamegear)) start = 'P'; else start = '.';
                if ((int)(tmp & 128)> 0 && gamegear) start = 'P'; else start = '.';

                //Controller 2
                tmp = r.ReadByte();
                if ((int)(tmp & 1) > 0) frame += "U"; else frame += ".";
                if ((int)(tmp & 2) > 0) frame += "D"; else frame += ".";
                if ((int)(tmp & 4) > 0) frame += "L"; else frame += ".";
                if ((int)(tmp & 8) > 0) frame += "R"; else frame += ".";
                if ((int)(tmp & 16) > 0) frame += "1"; else frame += ".";
                if ((int)(tmp & 32) > 0) frame += "2|"; else frame += ".|";

                frame += start;
                frame += ".|";
                m.AddFrame(frame);
            }
            m.WriteMovie();
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
