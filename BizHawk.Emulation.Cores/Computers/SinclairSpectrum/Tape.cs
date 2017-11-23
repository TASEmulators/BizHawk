using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
    /// <summary>
    /// Represents the tape device (or DATACORDER as AMSTRAD liked to call it)
    /// </summary>
    public class Tape
    {
        protected bool _micBitState;

        public SpectrumBase _machine { get; set; }
        public Buzzer _buzzer { get; set; }


        public virtual void Init(SpectrumBase machine)
        {
            _machine = machine;
            _buzzer = machine.BuzzerDevice;
            Reset();
        }

        public virtual void Reset()
        {
            _micBitState = true;
        }
        

        /// <summary>
        /// the EAR bit read from tape
        /// </summary>
        /// <param name="cpuCycles"></param>
        /// <returns></returns>
        public virtual bool GetEarBit(int cpuCycles)
        {
            return false;
        }

        /// <summary>
        /// Processes the mic bit change
        /// </summary>
        /// <param name="micBit"></param>
        public virtual void ProcessMicBit(bool micBit)
        {

        }
    }
}
