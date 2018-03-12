using BizHawk.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
    public interface IBeeperDevice
    {
        void Init(int sampleRate, int tStatesPerFrame);

        void ProcessPulseValue(bool fromTape, bool earPulse);

        void StartFrame();

        void EndFrame();

        void SetTapeMode(bool tapeMode);

        void SyncState(Serializer ser);
    }
}
