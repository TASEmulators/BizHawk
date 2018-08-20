using BizHawk.Emulation.Cores.Components.Z80A;
using System.Collections.Generic;

namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
    /// <summary>
    /// The +2 is almost identical to the 128k from an emulation point of view
    /// There are just a few small changes in the ROMs
    /// </summary>
    public partial class ZX128Plus2 : ZX128
    {
        #region Construction

        /// <summary>
        /// Main constructor
        /// </summary>
        /// <param name="spectrum"></param>
        /// <param name="cpu"></param>
        public ZX128Plus2(ZXSpectrum spectrum, Z80A cpu, ZXSpectrum.BorderType borderType, List<byte[]> files, List<JoystickType> joysticks) 
            : base(spectrum, cpu, borderType, files, joysticks)
        {
            
        }

        #endregion
    }
}
