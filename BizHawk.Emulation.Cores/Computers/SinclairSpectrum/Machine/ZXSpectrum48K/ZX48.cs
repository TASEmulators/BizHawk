using BizHawk.Emulation.Cores.Components.Z80A;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
    public class ZX48 : SpectrumBase
    {
        /// <summary>
        /// Main constructor
        /// </summary>
        /// <param name="spectrum"></param>
        /// <param name="cpu"></param>
        public ZX48(ZXSpectrum spectrum, Z80A cpu)
        {
            Spectrum = spectrum;
            CPU = cpu;

            RAM = new byte[0x4000 + 0xC000];

            InitScreenConfig();
            InitScreen();

            ResetULACycle();

            BuzzerDevice = new Buzzer(this);
            BuzzerDevice.Init();

            KeyboardDevice = new Keyboard48(this);

            TapeDevice = new Tape();
            TapeDevice.Init(this);
        }
    }
}
