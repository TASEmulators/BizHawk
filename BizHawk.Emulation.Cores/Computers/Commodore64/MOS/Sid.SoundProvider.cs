using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Computers.Commodore64.MOS
{
    public sealed partial class Sid : ISoundProvider, ISyncSoundProvider
    {
        public int MaxVolume
        {
            get { return short.MaxValue; }
            set { }
        }

        public void DiscardSamples()
        {
            _outputBufferIndex = 0;
        }

        public void GetSamples(short[] samples)
        {
            Flush();
            var length = Math.Min(samples.Length, _outputBufferIndex);
            for (var i = 0; i < length; i++)
            {
                samples[i] = _outputBuffer[i];
            }
            _outputBufferIndex = 0;
        }

        public void GetSamples(out short[] samples, out int nsamp)
        {
            Flush();
            samples = _outputBuffer;
            nsamp = _outputBufferIndex >> 1;
            _outputBufferIndex = 0;
        }
    }
}
