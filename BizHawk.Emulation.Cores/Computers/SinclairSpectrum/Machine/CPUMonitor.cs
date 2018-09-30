using BizHawk.Common;
using BizHawk.Emulation.Cores.Components.Z80A;

namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
    /// <summary>
    /// An intermediary class that manages cycling the ULA and CPU
    /// along with inherent Port and Memory contention
    /// </summary>
    public class CPUMonitor
    {
        #region Devices

        private SpectrumBase _machine;
        private Z80A _cpu;
        public MachineType machineType = MachineType.ZXSpectrum48;

        #endregion

        #region Lookups

        /// <summary>
        /// CPU total executes t-states
        /// </summary>
        public long TotalExecutedCycles => _cpu.TotalExecutedCycles;

        /// <summary>
        /// Current BUSRQ line array
        /// </summary>
        public ushort BUSRQ
        {
            get
            {
                switch (machineType)
                {
                    case MachineType.ZXSpectrum128Plus2a:
                    case MachineType.ZXSpectrum128Plus3:
                        return _cpu.MEMRQ[_cpu.bus_pntr];
                    default:
						return _cpu.BUSRQ[_cpu.mem_pntr];
                }
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

        /// <summary>
        /// The last 16-bit port address that was detected
        /// </summary>
        public ushort lastPortAddr;

        /// <summary>
        /// If true, the next read memory operation has been contended
        /// </summary>
        public bool NextMemReadContended;

        #endregion

        #region Methods

        /// <summary>
        /// Handles the ULA and CPU cycle clocks, along with any memory and port contention
        /// </summary>
        public void ExecuteCycle()
        {
            // simulate the ULA clock cycle before the CPU cycle
            _machine.ULADevice.CycleClock(TotalExecutedCycles);

            // is the next CPU cycle causing a BUSRQ or IORQ?
            if (BUSRQ > 0)
            {
                // check for IORQ
                if (!CheckIO())
                {
                    // is the memory address of the BUSRQ potentially contended?
                    if (_machine.IsContended(AscertainBUSRQAddress()))
                    {
                        var cont = _machine.ULADevice.GetContentionValue((int)_machine.CurrentFrameCycle);
                        if (cont > 0)
                        {
                            _cpu.TotalExecutedCycles += cont;
                            NextMemReadContended = true;
                        }
                    }
                }
            }

            _cpu.ExecuteOne();
        }

        /// <summary>
        /// Looks up the current BUSRQ address that is about to be signalled on the upcoming cycle
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
                // BC
                case Z80A.BIO1:
                case Z80A.BIO2:
                case Z80A.BIO3:
                case Z80A.BIO4:
                    addr = (ushort)(_cpu.Regs[_cpu.C] | _cpu.Regs[_cpu.B] << 8);
                    break;
                // WZ
                case Z80A.WIO1:
                case Z80A.WIO2:
                case Z80A.WIO3:
                case Z80A.WIO4:
                    addr = (ushort)(_cpu.Regs[_cpu.Z] | _cpu.Regs[_cpu.W] << 8);
                    break;
            }

            return addr;
        }

        /// <summary>
        /// Running every cycle, this determines whether the upcoming BUSRQ is for an IO operation
        /// Also processes any contention
        /// </summary>
        /// <returns></returns>
        private bool CheckIO()
        {
            bool isIO = false;

            switch (BUSRQ)
            {
                // BC: T1
                case Z80A.BIO1:
                    lastPortAddr = AscertainBUSRQAddress();
                    isIO = true;
                    if (IsIOCycleContended(1))
                        _cpu.TotalExecutedCycles += _machine.ULADevice.GetPortContentionValue((int)_machine.CurrentFrameCycle);
                    break;
                // BC: T2
                case Z80A.BIO2:
                    lastPortAddr = AscertainBUSRQAddress();
                    isIO = true;
                    if (IsIOCycleContended(2))
                        _cpu.TotalExecutedCycles += _machine.ULADevice.GetPortContentionValue((int)_machine.CurrentFrameCycle);
                    break;
                // BC: T3
                case Z80A.BIO3:
                    lastPortAddr = AscertainBUSRQAddress();
                    isIO = true;
                    if (IsIOCycleContended(3))
                        _cpu.TotalExecutedCycles += _machine.ULADevice.GetPortContentionValue((int)_machine.CurrentFrameCycle);
                    break;
                // BC: T4
                case Z80A.BIO4:
                    lastPortAddr = AscertainBUSRQAddress();
                    isIO = true;
                    if (IsIOCycleContended(4))
                        _cpu.TotalExecutedCycles += _machine.ULADevice.GetPortContentionValue((int)_machine.CurrentFrameCycle);
                    break;

                // WZ: T1
                case Z80A.WIO1:
                    lastPortAddr = AscertainBUSRQAddress();
                    isIO = true;
                    if (IsIOCycleContended(1))
                        _cpu.TotalExecutedCycles += _machine.ULADevice.GetPortContentionValue((int)_machine.CurrentFrameCycle);
                    break;
                // WZ: T2
                case Z80A.WIO2:
                    lastPortAddr = AscertainBUSRQAddress();
                    isIO = true;
                    if (IsIOCycleContended(2))
                        _cpu.TotalExecutedCycles += _machine.ULADevice.GetPortContentionValue((int)_machine.CurrentFrameCycle);
                    break;
                // WZ: T3
                case Z80A.WIO3:
                    lastPortAddr = AscertainBUSRQAddress();                    
                    isIO = true;
                    if (IsIOCycleContended(3))
                        _cpu.TotalExecutedCycles += _machine.ULADevice.GetPortContentionValue((int)_machine.CurrentFrameCycle);
                    break;
                // WZ: T4
                case Z80A.WIO4:
                    lastPortAddr = AscertainBUSRQAddress();
                    isIO = true;
                    if (IsIOCycleContended(4))
                        _cpu.TotalExecutedCycles += _machine.ULADevice.GetPortContentionValue((int)_machine.CurrentFrameCycle);
                    break;
            }

            return isIO;
        }

        /// <summary>
        /// Returns TRUE if the supplied T-cycle within an IO operation has the possibility of being contended
        /// This can be different based on the emulated ZX Spectrum model
        /// </summary>
        /// <param name="T"></param>
        /// <returns></returns>
        private bool IsIOCycleContended(int T)
        {
            bool lowBitSet = (lastPortAddr & 0x0001) != 0;
            bool highByte407f = false;

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
                            switch (T)
                            {
                                case 1:
                                case 2:
                                case 3:
                                case 4:
                                    return true;
                            }
                        }
                        else
                        {
                            // high byte 40-7f
                            // low bit reset
                            // C:1, C:3
                            switch (T)
                            {
                                case 1:
                                case 2:
                                    return true;
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
                        }
                        else
                        {
                            // high byte not 40-7f
                            // low bit reset
                            // N:1, C:3
                            switch (T)
                            {
                                case 2:
                                    return true;
                            }
                        }
                    }
                    break;

                case MachineType.ZXSpectrum128:
                case MachineType.ZXSpectrum128Plus2:
                    if ((lastPortAddr & 0xc000) == 0x4000 || (lastPortAddr & 0xc000) == 0xc000 && _machine.ContendedBankPaged())
                        highByte407f = true;

                    if (highByte407f)
                    {
                        // high byte 40-7f
                        if (lowBitSet)
                        {
                            // high byte 40-7f
                            // low bit set
                            // C:1, C:1, C:1, C:1
                            switch (T)
                            {
                                case 1:
                                case 2:
                                case 3:
                                case 4:
                                    return true;
                            }
                        }
                        else
                        {
                            // high byte 40-7f
                            // low bit reset
                            // C:1, C:3
                            switch (T)
                            {
                                case 1:
                                case 2:
                                    return true;
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
                        }
                        else
                        {
                            // high byte not 40-7f
                            // low bit reset
                            // N:1, C:3
                            switch (T)
                            {
                                case 2:
                                    return true;
                            }
                        }
                    }
                    break;

                case MachineType.ZXSpectrum128Plus2a:
                case MachineType.ZXSpectrum128Plus3:
                    // No contention occurs as the ULA only applies contention when the Z80 MREQ line is active
                    // (which is not during an IO operation)
                    break;
            }

            return false;
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

        #region Serialization

        public void SyncState(Serializer ser)
        {
            ser.BeginSection("CPUMonitor");
            ser.Sync("lastPortAddr", ref lastPortAddr);
            ser.Sync("NextMemReadContended", ref NextMemReadContended);
            ser.EndSection();
        }

        #endregion
    }
}
