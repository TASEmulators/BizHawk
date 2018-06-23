using System.Collections.Generic;
using BizHawk.Emulation.Cores.Components.Z80A;

namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
    /// <summary>
    /// 128K Constructor
    /// </summary>
    public partial class ZX128 : SpectrumBase
    {
        #region Construction

        /// <summary>
        /// Main constructor
        /// </summary>
        /// <param name="spectrum"></param>
        /// <param name="cpu"></param>
        public ZX128(ZXSpectrum spectrum, Z80A cpu, ZXSpectrum.BorderType borderType, List<byte[]> files, List<JoystickType> joysticks)
        {
            Spectrum = spectrum;
            CPU = cpu;

            CPUMon = new CPUMonitor(this);
            CPUMon.machineType = MachineType.ZXSpectrum128;

            ROMPaged = 0;
            SHADOWPaged = false;
            RAMPaged = 0;
            PagingDisabled = false;
            
            ULADevice = new Screen128(this);

            BuzzerDevice = new Beeper(this);
            BuzzerDevice.Init(44100, ULADevice.FrameLength);

            TapeBuzzer = new Beeper(this);
            TapeBuzzer.Init(44100, ULADevice.FrameLength);

            AYDevice = new AY38912(this);
            AYDevice.Init(44100, ULADevice.FrameLength);

            KeyboardDevice = new StandardKeyboard(this);

            InitJoysticks(joysticks);

            TapeDevice = new DatacorderDevice(spectrum.SyncSettings.AutoLoadTape);
            TapeDevice.Init(this);

            InitializeMedia(files);
        }

        #endregion        
    }
}
