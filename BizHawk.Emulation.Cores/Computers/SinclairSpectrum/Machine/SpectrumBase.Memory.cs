using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
    /// <summary>
    /// The abstract class that all emulated models will inherit from
    /// * Memory *
    /// </summary>
    public abstract partial class SpectrumBase
    {
        /// <summary>
        /// Byte array of total system memory (ROM + RAM + paging)
        /// </summary>
        public byte[] RAM { get; set; }

        /// <summary>
        /// Reads a byte of data from a specified memory address
        /// (with memory contention if appropriate)
        /// </summary>
        /// <param name="addr"></param>
        /// <returns></returns>
        public virtual byte ReadMemory(ushort addr)
        {
            var data = RAM[addr];
            if ((addr & 0xC000) == 0x4000)
            {
                // addr is in RAM not ROM - apply memory contention if neccessary
                var delay = GetContentionValue(CurrentFrameCycle);
                CPU.TotalExecutedCycles += delay;
            }
            return data;
        }

        /// <summary>
        /// Reads a byte of data from a specified memory address
        /// (with no memory contention)
        /// </summary>
        /// <param name="addr"></param>
        /// <returns></returns>
        public virtual byte PeekMemory(ushort addr)
        {
            var data = RAM[addr];
            return data;
        }

        /// <summary>
        /// Writes a byte of data to a specified memory address
        /// (with memory contention if appropriate)
        /// </summary>
        /// <param name="addr"></param>
        /// <param name="value"></param>
        public virtual void WriteMemory(ushort addr, byte value)
        {
            if (addr < 0x4000)
            {
                // Do nothing - we cannot write to ROM
                return;
            }
            else if (addr < 0xC000)
            {
                if (!CPU.IFF1)
                {

                }
                // possible contended RAM
                var delay = GetContentionValue(CurrentFrameCycle);
                CPU.TotalExecutedCycles += delay;
            }
            else
            {
                // uncontended RAM - do nothing
            }

                /*

            // Check whether memory is ROM or RAM
            switch (addr & 0xC000)
            {
                case 0x0000:
                    // Do nothing - we cannot write to ROM
                    return;
                case 0x4000:
                    // Address is RAM - apply contention if neccessary
                    var delay = GetContentionValue(_frameCycles);
                    CPU.TotalExecutedCycles += delay;
                    break;
            }    
            */        
            RAM[addr] = value;
        }

        /// <summary>
        /// Writes a byte of data to a specified memory address
        /// (without contention)
        /// </summary>
        /// <param name="addr"></param>
        /// <param name="value"></param>
        public virtual void PokeMemory(ushort addr, byte value)
        {
            if (addr < 0x4000)
            {
                // Do nothing - we cannot write to ROM
                return;
            }
            
            RAM[addr] = value;
        }

        /// <summary>
        /// Fills memory from buffer
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="startAddress"></param>
        public virtual void FillMemory(byte[] buffer, ushort startAddress)
        {
            buffer?.CopyTo(RAM, startAddress);
        }

        /// <summary>
        /// ULA reads the memory at the specified address
        /// (No memory contention)
        /// </summary>
        /// <param name="addr"></param>
        /// <returns></returns>
        public virtual byte FetchScreenMemory(ushort addr)
        {
            var value = RAM[(addr & 0x3FFF) + 0x4000];
            return value;
        }

        /// <summary>
        /// Returns the memory contention value for the specified T-State (cycle)
        /// The ZX Spectrum memory access is contended when the ULA is accessing the lower 16k of RAM
        /// </summary>
        /// <param name="Cycle"></param>
        /// <returns></returns>
        public virtual byte GetContentionValue(int cycle)
        {
            var val = _renderingCycleTable[cycle % UlaFrameCycleCount].ContentionDelay;
            return val;
        }
    }
}
