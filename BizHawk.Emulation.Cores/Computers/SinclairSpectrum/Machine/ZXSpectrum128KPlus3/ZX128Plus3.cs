using BizHawk.Emulation.Cores.Components.Z80A;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
    public partial class ZX128Plus3 : SpectrumBase
    {
        #region Construction

        /// <summary>
        /// Main constructor
        /// </summary>
        /// <param name="spectrum"></param>
        /// <param name="cpu"></param>
        public ZX128Plus3(ZXSpectrum spectrum, Z80A cpu, ZXSpectrum.BorderType borderType, byte[] file)
        {
            Spectrum = spectrum;
            CPU = cpu;

            ROMPaged = 0;
            SHADOWPaged = false;
            RAMPaged = 0;
            PagingDisabled = false;

            // init addressable memory from ROM and RAM banks
            ReInitMemory();

            //DisplayLineTime = 132;
            //VsyncNumerator = 3546900;

            InitScreenConfig(borderType);
            InitScreen();

            ResetULACycle();

            BuzzerDevice = new Buzzer(this);
            BuzzerDevice.Init(44100, UlaFrameCycleCount);

            AYDevice = new AY38912();
            AYDevice.Init(44100, UlaFrameCycleCount);

            KeyboardDevice = new Keyboard48(this);
            KempstonDevice = new KempstonJoystick(this);

            TapeProvider = new DefaultTapeProvider(file);

            TapeDevice = new Tape(TapeProvider);
            TapeDevice.Init(this);
        }

        #endregion
    }
}
