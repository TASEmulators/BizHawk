using System;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
    /// <summary>
    /// ZXHawk: Core Class
    /// * IInputPollable *
    /// </summary>
    public partial class ZXSpectrum : IInputPollable
    {
        public int LagCount
        {
            get { return _lagCount; }
            set { _lagCount = value; }
        }

        public bool IsLagFrame
        {
            get { return _isLag; }
            set { _isLag = value; }
        }

        public IInputCallbackSystem InputCallbacks { get; }

        private int _lagCount = 0;
        private bool _isLag = false;
    }
}
