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

            UInt32 length = r.ReadUInt32();
            
            UInt32 rerecords = r.ReadUInt32();
            m.SetHeaderLine(MovieHeader.RERECORDS, rerecords.ToString());

            UInt32 movieDataSize = r.ReadUInt32();
            UInt32 savestateOffset = r.ReadUInt32();
            UInt32 firstFrameOffset = r.ReadUInt32();

            //TODO: ROM checksum movie header line
            byte[] romCheckSum = r.ReadBytes(16);
            
            UInt32 EmuVersion = r.ReadUInt32();
            m.SetHeaderLine(MovieHeader.EMULATIONVERSION, "FCEU " + EmuVersion.ToString());

            //rom Filename
            

            
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

            int xx = 0;
            
            //synchack
            //flags[0] & 16

            //pal
            //flags[0] & 4
                
            //Power on vs reset
            //flags[0] & 8 = power on


            //flags[0] & 2 = reset
            

            //else starts from savestate so freak out, this isn't supported
            

            //moviedatasize stuff

            //read frame data
            byte joopcmd = 0;
            for (int x = 0; x < length; x++)
            {
                joopcmd = 0;
                //Check for reset or power-on on first frame

            }


            //set 4 score flag if necessary

            return m;
        }

        public static string ConvertMMV(string path)
        {
            string converted = Path.ChangeExtension(path, ".tas");
            
            return converted;
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
