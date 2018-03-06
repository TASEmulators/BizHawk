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
        public ZX128Plus3(ZXSpectrum spectrum, Z80A cpu, ZXSpectrum.BorderType borderType, List<byte[]> files, List<JoystickType> joysticks)
        {
            Spectrum = spectrum;
            CPU = cpu;

            ROMPaged = 0;
            SHADOWPaged = false;
            RAMPaged = 0;
            PagingDisabled = false;

            // init addressable memory from ROM and RAM banks
            ReInitMemory();

            ULADevice = new ULA128(this);

            BuzzerDevice = new Buzzer(this);
            BuzzerDevice.Init(44100, ULADevice.FrameLength);

            AYDevice = new AY38912();
            AYDevice.Init(44100, ULADevice.FrameLength);

            KeyboardDevice = new Keyboard48(this);

            InitJoysticks(joysticks);
            //KempstonDevice = new KempstonJoystick(this);

            TapeDevice = new DatacorderDevice();
            TapeDevice.Init(this);

            InitializeMedia(files);
        }

        #endregion
    }
}
