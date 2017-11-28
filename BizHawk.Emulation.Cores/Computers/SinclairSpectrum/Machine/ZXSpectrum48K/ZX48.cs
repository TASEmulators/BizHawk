using BizHawk.Emulation.Cores.Components.Z80A;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
    public class ZX48 : SpectrumBase
    {
        #region Construction

        /// <summary>
        /// Main constructor
        /// </summary>
        /// <param name="spectrum"></param>
        /// <param name="cpu"></param>
        public ZX48(ZXSpectrum spectrum, Z80A cpu, byte[] file)
        {
            Spectrum = spectrum;
            CPU = cpu;

            // init addressable memory from ROM and RAM banks
            /*
            Memory.Add(0, ROM0);
            Memory.Add(1, RAM0);
            Memory.Add(2, RAM1);
            Memory.Add(3, RAM2);
            */
            ReInitMemory();

            //RAM = new byte[0x4000 + 0xC000];

            InitScreenConfig();
            InitScreen();

            ResetULACycle();

            BuzzerDevice = new Buzzer(this);
            BuzzerDevice.Init(44100, UlaFrameCycleCount);

            KeyboardDevice = new Keyboard48(this);

            TapeProvider = new DefaultTapeProvider(file);

            TapeDevice = new Tape(TapeProvider);
            TapeDevice.Init(this);
        }

        #endregion

        #region MemoryMapping

        /* 48K Spectrum has NO memory paging
         * 
         *  0xffff +--------+
                   | Bank 2 |
                   |        |
                   |        |
                   |        |
            0xc000 +--------+
                   | Bank 1 |
                   |        |
                   |        |
                   |        |
            0x8000 +--------+
                   | Bank 0 |
                   |        |
                   |        |
                   | screen |
            0x4000 +--------+
                   | ROM 0  |
                   |        |
                   |        |
                   |        |
            0x0000 +--------+
        */

        /// <summary>
        /// Simulates reading from the bus (no contention)
        /// Paging should be handled here
        /// </summary>
        /// <param name="addr"></param>
        /// <returns></returns>
        public override byte ReadBus(ushort addr)
        {
            int divisor = addr / 0x4000;
            // paging logic goes here

            var bank = Memory[divisor];
            var index = addr % 0x4000;
            return bank[index];
        }

        /// <summary>
        /// Simulates writing to the bus (no contention)
        /// Paging should be handled here
        /// </summary>
        /// <param name="addr"></param>
        /// <param name="value"></param>
        public override void WriteBus(ushort addr, byte value)
        {
            int divisor = addr / 0x4000;
            // paging logic goes here

            var bank = Memory[divisor];
            var index = addr % 0x4000;
            bank[index] = value;
        }

        /// <summary>
        /// Reads a byte of data from a specified memory address
        /// (with memory contention if appropriate)
        /// </summary>
        /// <param name="addr"></param>
        /// <returns></returns>
        public override byte ReadMemory(ushort addr)
        {
            var data = ReadBus(addr);
            if ((addr & 0xC000) == 0x4000)
            {
                // addr is in RAM not ROM - apply memory contention if neccessary
                var delay = GetContentionValue(CurrentFrameCycle);
                CPU.TotalExecutedCycles += delay;
            }
            return data;
        }

        /// <summary>
        /// Writes a byte of data to a specified memory address
        /// (with memory contention if appropriate)
        /// </summary>
        /// <param name="addr"></param>
        /// <param name="value"></param>
        public override void WriteMemory(ushort addr, byte value)
        {
            if (addr < 0x4000)
            {
                // Do nothing - we cannot write to ROM
                return;
            }
            else if (addr < 0xC000)
            {
                // possible contended RAM
                var delay = GetContentionValue(CurrentFrameCycle);
                CPU.TotalExecutedCycles += delay;
            }

            WriteBus(addr, value);
        }

        public override void ReInitMemory()
        {
            if (Memory.ContainsKey(0))
                Memory[0] = ROM0;
            else
                Memory.Add(0, ROM0);

            if (Memory.ContainsKey(1))
                Memory[1] = RAM0;
            else
                Memory.Add(1, RAM0);

            if (Memory.ContainsKey(2))
                Memory[2] = RAM1;
            else
                Memory.Add(2, RAM1);

            if (Memory.ContainsKey(3))
                Memory[3] = RAM2;
            else
                Memory.Add(3, RAM2);

            if (Memory.ContainsKey(4))
                Memory[4] = RAM3;
            else
                Memory.Add(4, RAM3);
        }


        #endregion


    }
}
