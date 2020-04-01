using System;
using PeNet.Structures;

namespace PeNet
{
    internal static class PeValidator
    {
        public static bool HasMagicHeader(byte[] buff)
        {
            try
            {
                var imageDosHeader = new IMAGE_DOS_HEADER(buff, 0);
                return imageDosHeader.e_magic == 0x5a4d;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool IsPeFileParseable(byte[] buff)
        {
            try
            {
                PeFile peFile = new PeFile(buff);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool HasValidNumberOfDirectories(byte[] buff)
        {
            try
            {
                PeFile peFile = new PeFile(buff);
                return peFile.ImageNtHeaders.OptionalHeader.NumberOfRvaAndSizes <= 16;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool IsPeValidPeFile(byte[] buff)
        {
            if (HasMagicHeader(buff) == false)
                return false;

            if (IsPeFileParseable(buff) == false)
                return false;

            if (HasValidNumberOfDirectories(buff) == false)
                return false;

            return true;
        }
    }
}
