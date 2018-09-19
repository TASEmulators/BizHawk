using BizHawk.Emulation.Cores.Components.Z80A;
using System;
using System.Collections.Generic;

namespace BizHawk.Emulation.Cores.Computers.AmstradCPC
{
    /// <summary>
    /// CPC6128 construction
    /// </summary>
    public partial class CPC6128 : CPCBase
    {
        #region Construction

        /// <summary>
        /// Main constructor
        /// </summary>
        /// <param name="spectrum"></param>
        /// <param name="cpu"></param>
        public CPC6128(AmstradCPC cpc, Z80A cpu, List<byte[]> files, bool autoTape, AmstradCPC.BorderType borderType)
        {
            CPC = cpc;
            CPU = cpu;

            FrameLength = 79872;

            CRCT = new CRCT_6845(CRCT_6845.CRCTType.MC6845, this);
            //CRT = new CRTDevice(this);
            GateArray = new AmstradGateArray(this, AmstradGateArray.GateArrayType.Amstrad40007);
            PPI = new PPI_8255(this);

            TapeBuzzer = new Beeper(this);
            TapeBuzzer.Init(44100, FrameLength);

            //AYDevice = new PSG(this, PSG.ay38910_type_t.AY38910_TYPE_8912, GateArray.PSGClockSpeed, 882 * 50);
            AYDevice = new AY38912(this);
            AYDevice.Init(44100, FrameLength);

            KeyboardDevice = new StandardKeyboard(this);

            TapeDevice = new DatacorderDevice(autoTape);
            TapeDevice.Init(this);

            UPDDiskDevice = new NECUPD765();
            UPDDiskDevice.Init(this);

            InitializeMedia(files);
        }

        #endregion
    }
}
