using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{

    public class RomData
    {
        /// <summary>
        /// ROM Contents
        /// </summary>
        public byte[] RomBytes { get; set; }
        
        /// <summary>
        /// Useful ROM addresses that are needed during tape operations
        /// </summary>
        public ushort SaveBytesRoutineAddress { get; set; }
        public ushort SaveBytesResumeAddress { get; set; }
        public ushort LoadBytesRoutineAddress { get; set; }
        public ushort LoadBytesResumeAddress { get; set; }
        public ushort LoadBytesInvalidHeaderAddress { get; set; }

        public static RomData InitROM(MachineType machineType, byte[] rom)
        {
            RomData RD = new RomData();
            RD.RomBytes = new byte[rom.Length];
            RD.RomBytes = rom;

            switch (machineType)
            {
                case MachineType.ZXSpectrum48:
                    RD.SaveBytesRoutineAddress = 0x04C2;
                    RD.SaveBytesResumeAddress = 0x0000;
                    RD.LoadBytesRoutineAddress = 0x056C;
                    RD.LoadBytesResumeAddress = 0x05E2;
                    RD.LoadBytesInvalidHeaderAddress = 0x05B6;
                    break;
            }

            return RD;
        }

    }
}
