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

                case MachineType.ZXSpectrum128:
                case MachineType.ZXSpectrum128Plus2:
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
