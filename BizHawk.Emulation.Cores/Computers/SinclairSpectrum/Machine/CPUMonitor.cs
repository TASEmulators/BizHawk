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
        #region Devices

        private SpectrumBase _machine;
        private Z80A _cpu;
        public MachineType machineType = MachineType.ZXSpectrum48;

        #endregion

        #region Lookups

        public ushort[] cur_instr => _cpu.cur_instr;
        public int instr_pntr => _cpu.instr_pntr;
        public ushort RegPC => _cpu.RegPC;
        public long TotalExecutedCycles => _cpu.TotalExecutedCycles;
        public ushort BUSRQ
        {
            get
            {
                //if (_cpu.bus_pntr < _cpu.BUSRQ.Length - 1)
                return _cpu.BUSRQ[_cpu.bus_pntr];

                //return 0;
            }
        }

        #endregion

        #region Construction

        public CPUMonitor(SpectrumBase machine)
        {
            _machine = machine;
            _cpu = _machine.CPU;
        }

        #endregion

        #region State

        public bool IsContending = false;
        public int ContCounter = -1;
        public int portContCounter = 0;
        public int portContTotalLen = 0;
        public bool portContending = false;
        public ushort lastPortAddr;
        public int[] portContArr = new int[4];

        #endregion

        #region Methods
        
        /// <summary>
        /// Handles the ULA and CPU cycle clocks, along with any memory and port contention
        /// </summary>
        public void ExecuteCycle()
        {
            _machine.ULADevice.RenderScreen((int)_machine.CurrentFrameCycle);

            if (portContending)
            {
                RunPortContention();
            }
            else
            {
                // is the next CPU cycle causing a BUSRQ?
                if (BUSRQ > 0)
                {
                    // is the memory address of the BUSRQ potentially contended?
                    if (_machine.IsContended(AscertainBUSRQAddress()))
                    {
                        var cont = _machine.ULADevice.GetContentionValue((int)_machine.CurrentFrameCycle);
                        if (cont > 0)
                        {
                            _cpu.TotalExecutedCycles += cont;
                        }
                    }
                }
            }

            _cpu.ExecuteOne();

            /*
            else if (ContCounter > 0)
            {
                // still contention cycles to process
                IsContending = true;
            }
            else
            {
                // is the next CPU cycle causing a BUSRQ?
                if (BUSRQ > 0)
                {
                    // is the memory address of the BUSRQ potentially contended?
                    if (_machine.IsContended(AscertainBUSRQAddress()))
                    {
                        var cont = _machine.ULADevice.GetContentionValue((int)_machine.CurrentFrameCycle);
                        if (cont > 0)
                        {
                            ContCounter = cont + 1;
                            IsContending = true;
                        }
                    }
                }
            }
            /*
            else
            {
                // no contention cycles to process (so far) on this cycle
                IsContending = false;
                ContCounter = 0;

                if (portContending)
                {
                    // a port operation is still in progress
                    portContCounter++;
                    if (portContCounter > 3)
                    {
                        // we are now out of the IN/OUT operation
                        portContCounter = 0;
                        portContending = false;
                    }
                    else
                    {
                        // still IN/OUT cycles to process
                        if (IsPortContended(portContCounter))
                        {
                            var cont = _machine.ULADevice.GetContentionValue((int)_machine.CurrentFrameCycle);
                            if (cont > 0)
                            {
                                ContCounter = cont + 1;
                                IsContending = true;
                                // dont let this fall through
                                // just manually do the first contention cycle
                                ContCounter--;
                                _cpu.TotalExecutedCycles++;
                                return;
                            }
                        }
                    }
                }

                // is the next CPU cycle causing a BUSRQ?
                if (BUSRQ > 0)
                {
                    // is the memory address of the BUSRQ potentially contended?
                    if (_machine.IsContended(AscertainBUSRQAddress()))
                    {
                        var cont = _machine.ULADevice.GetContentionValue((int)_machine.CurrentFrameCycle);
                        if (cont > 0)
                        {
                            ContCounter = cont + 1;
                            IsContending = true;
                        }
                    }
                }
                /*
                // is the next CPU cycle an OUT operation?
                else if (cur_instr[instr_pntr] == Z80A.OUT)
                {
                    portContending = true;
                    lastPortAddr = (ushort)(_cpu.Regs[cur_instr[instr_pntr + 1]] | _cpu.Regs[cur_instr[instr_pntr + 2]] << 8);
                    portContCounter = 0;
                    if (IsPortContended(portContCounter))
                    {
                        var cont = _machine.ULADevice.GetContentionValue((int)_machine.CurrentFrameCycle);
                        if (cont > 0)
                        {
                            ContCounter = cont;
                            IsContending = true;
                        }
                    }
                }
                // is the next cpu cycle an IN operation?
                else if (cur_instr[instr_pntr] == Z80A.IN)
                {
                    portContending = true;
                    lastPortAddr = (ushort)(_cpu.Regs[cur_instr[instr_pntr + 2]] | _cpu.Regs[cur_instr[instr_pntr + 3]] << 8);
                    portContCounter = 0;
                    if (IsPortContended(portContCounter))
                    {
                        var cont = _machine.ULADevice.GetContentionValue((int)_machine.CurrentFrameCycle);
                        if (cont > 0)
                        {
                            ContCounter = cont;
                            IsContending = true;
                        }
                    }
                }
                */
          /*  }*/
        
            /*
            // run a CPU cycle if no contention is applicable
            if (!IsContending)
            {
                _cpu.ExecuteOne();
            }
            else
            {
                _cpu.TotalExecutedCycles++;
                ContCounter--;
            }
            */
        }

        /// <summary>
        /// Looks up the BUSRQ address that is about to be signalled
        /// </summary>
        /// <returns></returns>
        private ushort AscertainBUSRQAddress()
        {
            ushort addr = 0;
            switch (BUSRQ)
            {
                // PCh
                case 1:
                    addr = (ushort)(_cpu.Regs[_cpu.PCl] | _cpu.Regs[_cpu.PCh] << 8);
                    break;
                // SPh
                case 3:
                    addr = (ushort)(_cpu.Regs[_cpu.SPl] | _cpu.Regs[_cpu.SPh] << 8);
                    break;
                // A
                case 4:
                    addr = (ushort)(_cpu.Regs[_cpu.F] | _cpu.Regs[_cpu.A] << 8);
                    break;
                // B
                case 6:
                    addr = (ushort)(_cpu.Regs[_cpu.C] | _cpu.Regs[_cpu.B] << 8);
                    break;
                // D
                case 8:
                    addr = (ushort)(_cpu.Regs[_cpu.E] | _cpu.Regs[_cpu.D] << 8);
                    break;
                // H
                case 10:
                    addr = (ushort)(_cpu.Regs[_cpu.L] | _cpu.Regs[_cpu.H] << 8);
                    break;
                // W
                case 12:
                    addr = (ushort)(_cpu.Regs[_cpu.Z] | _cpu.Regs[_cpu.W] << 8);
                    break;
                // Ixh
                case 16:
                    addr = (ushort)(_cpu.Regs[_cpu.Ixl] | _cpu.Regs[_cpu.Ixh] << 8);
                    break;
                // Iyh
                case 18:
                    addr = (ushort)(_cpu.Regs[_cpu.Iyl] | _cpu.Regs[_cpu.Iyh] << 8);
                    break;
                // I
                case 21:
                    addr = (ushort)(_cpu.Regs[_cpu.R] | _cpu.Regs[_cpu.I] << 8);
                    break;
            }

            return addr;
        }
        
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
                                case 3: _cpu.TotalExecutedCycles += _machine.ULADevice.GetPortContentionValue(f); break;
                                case 2: _cpu.TotalExecutedCycles += _machine.ULADevice.GetPortContentionValue(f); break;
                                case 1: _cpu.TotalExecutedCycles += _machine.ULADevice.GetPortContentionValue(f); break;
                                case 0:
                                    _cpu.TotalExecutedCycles += _machine.ULADevice.GetPortContentionValue(f);
                                    portContCounter = 0;
                                    portContending = false;
                                    break;
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
                                case 3: _cpu.TotalExecutedCycles += _machine.ULADevice.GetPortContentionValue(f); break;
                                case 2: _cpu.TotalExecutedCycles += _machine.ULADevice.GetPortContentionValue(f); break;
                                case 1: break;
                                case 0:
                                    portContCounter = 0;
                                    portContending = false;
                                    break;
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
                                case 0:
                                    portContCounter = 0;
                                    portContending = false;
                                    break;
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
                                case 2: _cpu.TotalExecutedCycles += _machine.ULADevice.GetPortContentionValue(f); break;
                                case 1: break;
                                case 0:
                                    portContCounter = 0;
                                    portContending = false;
                                    break;
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

        /// <summary>
        /// Called when the first byte of an instruction is fetched
        /// </summary>
        /// <param name="firstByte"></param>
        public void OnExecFetch(ushort firstByte)
        {
            // fetch instruction without incrementing pc
            //_cpu.FetchInstruction(_cpu.FetchMemory(firstByte));
        }

        #endregion

    }
}
