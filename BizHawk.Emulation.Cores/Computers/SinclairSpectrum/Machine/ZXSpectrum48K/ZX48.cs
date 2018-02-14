using BizHawk.Emulation.Cores.Components.Z80A;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
    public partial class ZX48 : SpectrumBase
    {
        #region Construction

        /// <summary>
        /// Main constructor
        /// </summary>
        /// <param name="spectrum"></param>
        /// <param name="cpu"></param>
        public ZX48(ZXSpectrum spectrum, Z80A cpu, ZXSpectrum.BorderType borderType, byte[] file)
        {
            Spectrum = spectrum;
            CPU = cpu;

            ReInitMemory();

            ULADevice = new ULA48(this);

            BuzzerDevice = new Buzzer(this);
            BuzzerDevice.Init(44100, ULADevice.FrameLength);

            KeyboardDevice = new Keyboard48(this);
            KempstonDevice = new KempstonJoystick(this);

            //TapeProvider = new DefaultTapeProvider(file);

            TapeDevice = new DatacorderDevice();
            TapeDevice.Init(this);
            TapeDevice.LoadTape(file);
        }

        #endregion
    }
}
