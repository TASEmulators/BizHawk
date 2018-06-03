using BizHawk.Emulation.Cores.Components.Z80A;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
    public class CPUMonitor
    {
        private SpectrumBase _machine;
        private Z80A _cpu;
        public MachineType machineType = MachineType.ZXSpectrum48;

        public CPUMonitor(SpectrumBase machine)
        {
            _machine = machine;
            _cpu = _machine.CPU;
        }

        public ushort[] cur_instr => _cpu.cur_instr;
        public int instr_pntr => _cpu.instr_pntr;
        public ushort RegPC => _cpu.RegPC;
        public long TotalExecutedCycles => _cpu.TotalExecutedCycles;

        /// <summary>
        /// Called when the first byte of an instruction is fetched
        /// </summary>
        /// <param name="firstByte"></param>
        public void OnExecFetch(ushort firstByte)
        {
            // fetch instruction without incrementing pc
            //_cpu.FetchInstruction(_cpu.FetchMemory(firstByte));
        }

        /// <summary>
        /// A CPU monitor cycle
        /// </summary>
        public void Cycle()
        {
            
            if (portContending)
            {
                RunPortContention();
            }
            /*
            else
            {
                // check for wait state on cycle that has just happened
                // next cycle should be a read/write operation
                if (cur_instr[instr_pntr] == Z80A.WAIT)
                {
                    ushort addr = 0;
                    bool abort = false;

                    // identify the type of operation and get the targetted address
                    switch (cur_instr[instr_pntr + 1])
                    {
                        // op fetch
                        case Z80A.OP_F:
                            addr = RegPC;
                            break;
                        // read/writes
                        case Z80A.RD:
                        case Z80A.RD_INC:
                        case Z80A.WR:
                        case Z80A.WR_INC:
                            addr = (ushort)(_cpu.Regs[cur_instr[instr_pntr + 3]] | _cpu.Regs[cur_instr[instr_pntr + 4]] << 8);
                            break;
                        default:
                            abort = true;
                            break;
                    }

                    if (!abort)
                    {
                        // is the address in a potentially contended bank?
                        if (_machine.IsContended(addr))
                        {
                            // will the ULA be contending this address on the next cycle?
                            var delay = _machine.ULADevice.GetContentionValue((int)_machine.CurrentFrameCycle + 1);
                            _cpu.TotalExecutedCycles += delay;
                        }
                    }
                }
                
            }
            */
            return;
            // check for wait state on next cycle
            // the cycle after that should be a read/write operation or op fetch
            if (instr_pntr >= cur_instr.Length - 1)
            {
                // will overflow
                return;
            }

            if (cur_instr[instr_pntr + 1] == Z80A.WAIT)
            {
               // return;
                ushort addr = 0;

                // identify the type of operation and get the targetted address
                var op = cur_instr[instr_pntr + 2];
                switch (op)
                {
                    // op fetch
                    case Z80A.OP_F:
                        addr = (ushort)(RegPC);
                        if (_machine.IsContended(addr))
                        {
                            var delay = _machine.ULADevice.GetContentionValue((int)_machine.CurrentFrameCycle);
                            if (delay > 0)
                            {
                                _cpu.TotalExecutedCycles += delay;
                                _machine.ULADevice.RenderScreen((int)_machine.CurrentFrameCycle);
                            }
                        }
                        break;
                    // read/writes
                    case Z80A.RD:
                    case Z80A.RD_INC:
                    case Z80A.WR:
                    case Z80A.WR_INC:
                        addr = (ushort)(_cpu.Regs[cur_instr[instr_pntr + 4]] | _cpu.Regs[cur_instr[instr_pntr + 5]] << 8);
                        if (_machine.IsContended(addr))
                        {
                            var delay = _machine.ULADevice.GetContentionValue((int)_machine.CurrentFrameCycle);
                            if (delay > 0)
                            {
                                _cpu.TotalExecutedCycles += delay;
                                _machine.ULADevice.RenderScreen((int)_machine.CurrentFrameCycle);
                            }
                        }
                        break;
                    case Z80A.FTCH_DB:
                        break;
                    default:
                        break;
                }
            }
        }


        #region Port Contention

        public int portContCounter = 0;
        public bool portContending = false;
        public ushort lastPortAddr;

        /// <summary>
        /// Perfors the actual port contention (if necessary)
        /// </summary>
        private void RunPortContention()
        {
            //return;
            bool lowBitSet = false;
            bool highByte407f = false;

            int offset = 0; // _machine.ULADevice.contentionOffset; // -5;// 57;// - 10;
            var c = _machine.CurrentFrameCycle;
            var t = _machine.ULADevice.FrameLength;
            int f = (int)c + offset;
            if (f >= t)
                f = f - t;
            else if (f < 0)
                f = t + f;

            if ((lastPortAddr & 0x0001) != 0)
                lowBitSet = true;

            portContCounter--;

            switch (machineType)
            {
                case MachineType.ZXSpectrum16:
                case MachineType.ZXSpectrum48:
                case MachineType.ZXSpectrum128:
                case MachineType.ZXSpectrum128Plus2:

                    if ((lastPortAddr & 0xc000) == 0x4000)
                        highByte407f = true;

                    if (highByte407f)
                    {
                        // high byte 40-7f
                        if (lowBitSet)
                        {
                            // high byte 40-7f
                            // low bit set
                            // C:1, C:1, C:1, C:1
                            switch (portContCounter)
                            {
                                case 3: _cpu.TotalExecutedCycles += _machine.ULADevice.GetContentionValue(f); break;
                                case 2: _cpu.TotalExecutedCycles += _machine.ULADevice.GetContentionValue(f); break;
                                case 1: _cpu.TotalExecutedCycles += _machine.ULADevice.GetContentionValue(f); break;
                                case 0: _cpu.TotalExecutedCycles += _machine.ULADevice.GetContentionValue(f); break;
                                default:
                                    portContCounter = 0;
                                    portContending = false;
                                    break;
                            }
                        }
                        else
                        {
                            // high byte 40-7f
                            // low bit reset
                            // C:1, C:3
                            switch (portContCounter)
                            {
                                case 3: _cpu.TotalExecutedCycles += _machine.ULADevice.GetContentionValue(f); break;
                                case 2: _cpu.TotalExecutedCycles += _machine.ULADevice.GetContentionValue(f); break;
                                case 1: break;
                                case 0: break;
                                default:
                                    portContCounter = 0;
                                    portContending = false;
                                    break;
                            }
                        }
                    }
                    else
                    {
                        // high byte not 40-7f
                        if (lowBitSet)
                        {
                            // high byte not 40-7f
                            // low bit set
                            // N:4
                            switch (portContCounter)
                            {
                                case 3: break;
                                case 2: break;
                                case 1: break;
                                case 0: break;
                                default:
                                    portContCounter = 0;
                                    portContending = false;
                                    break;
                            }
                        }
                        else
                        {
                            // high byte not 40-7f
                            // low bit reset
                            // N:1, C:3
                            switch (portContCounter)
                            {
                                case 3: break;
                                case 2: _cpu.TotalExecutedCycles += _machine.ULADevice.GetContentionValue(f); break;
                                case 1: break;
                                case 0: break;
                                default:
                                    portContCounter = 0;
                                    portContending = false;
                                    break;
                            }
                        }
                    }
                    break;

                case MachineType.ZXSpectrum128Plus2a:
                case MachineType.ZXSpectrum128Plus3:
                    break;
            }
        }

        /// <summary>
        /// Starts the port contention process
        /// </summary>
        /// <param name="type"></param>
        public void ContendPort(ushort port)
        {
            portContending = true;
            portContCounter = 4;
            lastPortAddr = port;
        }

        #endregion

    }
}
