using System;
using System.Collections.Generic;

namespace BizHawk.Emulation.Sound
{
    // We process TIMER writes, and also track BUSY status, immediately when writes come in.
    // All other writes are queued up with a timestamp, so that we can sift through them when 
    // we're rendering audio for the frame.
    
    public partial class YM2612
    {
        byte PartSelect;
        byte RegisterSelect;
        bool DacEnable;
        byte DacValue;

        Queue<QueuedCommand> commands = new Queue<QueuedCommand>();

/*
I might share a little quirk I discovered a few days ago, while I was working through a list of random tests unrelated to the envelope generator. I was running a test to check whether
parts 1 and 2 had a separate address register, IE, whether you could write one address to $A00000 and another to $A00002, then write to each data port, and have the two writes go to two 
different register addresses in each part. What I found was that not only do parts 1 and 2 not have a separate address register, they don't even have a separate data port. 

It turns out that writing to an address register stores both the written address, and the part number of the address register you wrote to. You can then write to either the data port at
$A00001, or the data port at $A00003, and the write will go to the register number you wrote, within the part of the address register you wrote to. This means you can, for example, write
an address to $A00000, then write the data to $A00003, and the data will in fact be written to the part 1 register block, not the part 2 register block. 

The simpler implementation would seem to be only storing the 8-bit address data that was written, and use the data port that received the write to determine which part to write to, but
it isn't implemented this way. Writing to the address register stores 9 bits of data, indicating both the target register address, and the part number. I don't think any emulator does 
this correctly right now.
*/
        public byte ReadStatus(int clock)
        {
            UpdateTimers(clock);

            byte retval = 0;
            if (TimerATripped) retval |= 1;
            if (TimerBTripped) retval |= 2;
            return retval;
        }

        public void Write(int addr, byte value, int clock)
        {
            UpdateTimers(clock);

            if (addr == 0)
            {
                PartSelect = 1;
                RegisterSelect = value;
                return;
            }
            else if (addr == 2)
            {
                PartSelect = 2;
                RegisterSelect = value;
                return;
            }

            if (PartSelect == 1)
            {
                if (RegisterSelect == 0x24) { WriteTimerA_MSB_24(value, clock); return; }
                if (RegisterSelect == 0x25) { WriteTimerA_LSB_25(value, clock); return; }
                if (RegisterSelect == 0x26) { WriteTimerB_26(value, clock); return; }
                if (RegisterSelect == 0x27) { WriteTimerControl_27(value, clock); } // don't return on this one; we process immediately AND enqueue command for port $27.
            }

            var cmd = new QueuedCommand { Part = PartSelect, Register = RegisterSelect, Data = value, Clock = clock-frameStartClock };
            commands.Enqueue(cmd);
        }

        void WriteCommand(QueuedCommand cmd)
        {
            if (cmd.Part == 1)
                Part1_WriteRegister(cmd.Register, cmd.Data);
            else
                Part2_WriteRegister(cmd.Register, cmd.Data);
        }

        // information on TIMER is on pg 6
        void Part1_WriteRegister(byte register, byte value)
        {
            switch (register)
            {
                //case 0x22: Console.WriteLine("LFO Control {0:X2}", value); break;
                case 0x24: break; // Timer A MSB, handled immediately
                case 0x25: break; // Timer A LSB, handled immediately
                case 0x26: break; // Timer B, handled immediately
                //case 0x27: Console.WriteLine("$27: Ch3 Mode / Timer Control {0:X2}", value); break; // determines if CH3 has 1 frequency or 4 frequencies.
                //case 0x28: Console.WriteLine("Operator Key On/Off Ctrl {0:X2}", value); break;
                case 0x2A: DacValue = value; break;
                case 0x2B: DacEnable = (value & 0x80) != 0; break;
                case 0x2C: throw new Exception("something wrote to ym2612 port $2C!"); //http://forums.sonicretro.org/index.php?showtopic=28589

                // TODO bleh the situation with op2/op3 confusing
                // "In MAME OPN emulation code register pairs for multi-frequency mode are A6A2, ACA8, AEAA, ADA9".

                //D7 - operator, which frequency defined by A6A2 
                //D6 - .. ACA8 
                //D5 - .. AEAA 
                //D4 - .. ADA9
                //Where D7=op4, D6=op3, D5=op2, and D4=op1. That matches the YM2608 document. At least that's confirmed then. 

                // PG4 has some info on frquency calculations

                default:
/*                    if (register >= 0x30 && register < 0xA0)
                        Console.WriteLine("P1 FM Channel data write");
                    else
                        Console.WriteLine("P1 REG {0:X2} WRITE {1:X2}", register, value); */
                    break;
            }
        }

        void Part2_WriteRegister(byte register, byte value)
        {
            // NOTE. Only first bank has multi-frequency CSM/Special mode. This mode can't work on CH6.

            /*if (register >= 0x30 && register < 0xA0)
                Console.WriteLine("P2 FM Channel data write");
            else
                Console.WriteLine("P2 REG {0:X2} WRITE {1:X2}", register, value);*/
        }

        public class QueuedCommand
        {
            public byte Part;
            public byte Register;
            public byte Data;
            public int Clock;
        }
    }
}