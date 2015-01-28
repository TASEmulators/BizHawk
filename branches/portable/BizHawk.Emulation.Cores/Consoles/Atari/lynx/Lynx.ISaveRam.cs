using System;
using System.Runtime.InteropServices;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Atari.Lynx
{
    public partial class Lynx : ISaveRam
    {
        public byte[] CloneSaveRam()
        {
            int size;
            IntPtr data;
            if (!LibLynx.GetSaveRamPtr(Core, out size, out data))
                return null;
            byte[] ret = new byte[size];
            Marshal.Copy(data, ret, 0, size);
            return ret;
        }

        public void StoreSaveRam(byte[] srcdata)
        {
            int size;
            IntPtr data;
            if (!LibLynx.GetSaveRamPtr(Core, out size, out data))
                throw new InvalidOperationException();
            if (size != srcdata.Length)
                throw new ArgumentOutOfRangeException();
            Marshal.Copy(srcdata, 0, data, size);
        }

        public bool SaveRamModified
        {
            get
            {
                int unused;
                IntPtr unused2;
                return LibLynx.GetSaveRamPtr(Core, out unused, out unused2);
            }
        }
    }
}
