using BizHawk.Emulation.Cores.Components.Z80A;
using System;
using System.Collections.Generic;

namespace BizHawk.Emulation.Cores.Computers.AmstradCPC
{
    /// <summary>
    /// CPC464 construction
    /// </summary>
    public partial class CPC464 : CPCBase
    {
        #region Construction

        /// <summary>
        /// Main constructor
        /// </summary>
        /// <param name="spectrum"></param>
        /// <param name="cpu"></param>
        public CPC464(AmstradCPC cpc, Z80A cpu, List<byte[]> files)
        {
            CPC = cpc;
            CPU = cpu;

            FrameLength = 79872;

            CRCT = new CRCT_6845(CRCT_6845.CRCTType.Motorola_MC6845, this);
            GateArray = new AmstradGateArray(this, AmstradGateArray.GateArrayType.Amstrad40007);
            PPI = new PPI_8255(this);

            KeyboardDevice = new StandardKeyboard(this);

            TapeBuzzer = new Beeper(this);
            TapeBuzzer.Init(44100, FrameLength);

            AYDevice = new AY38912(this);
            AYDevice.Init(44100, FrameLength);

            TapeDevice = new DatacorderDevice();
            TapeDevice.Init(this);

            InitializeMedia(files);
        }

        #endregion
    }
}
