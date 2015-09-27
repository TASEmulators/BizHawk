using System;
using System.Collections.Generic;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Computers.Commodore64
{
	public partial class C64 : IDebuggable
	{
		public IDictionary<string, RegisterValue> GetCpuFlagsAndRegisters()
		{
			return new Dictionary<string, RegisterValue>
			{
				{ "A", board.cpu.A },
				{ "X", board.cpu.X },
				{ "Y", board.cpu.Y },
				{ "S", board.cpu.S },
				{ "PC", board.cpu.PC },
				{ "Flag C", board.cpu.FlagC },
				{ "Flag Z", board.cpu.FlagZ },
				{ "Flag I", board.cpu.FlagI },
				{ "Flag D", board.cpu.FlagD },
				{ "Flag B", board.cpu.FlagB },
				{ "Flag V", board.cpu.FlagV },
				{ "Flag N", board.cpu.FlagN },
				{ "Flag T", board.cpu.FlagT }
			};
		}

		public void SetCpuRegister(string register, int value)
		{
			switch (register)
			{
				default:
					throw new InvalidOperationException();
				case "A":
					board.cpu.A = (byte)value;
					break;
				case "X":
					board.cpu.X = (byte)value;
					break;
				case "Y":
					board.cpu.Y = (byte)value;
					break;
				case "S":
					board.cpu.S = (byte)value;
					break;
				case "PC":
					board.cpu.PC = (ushort)value;
					break;
			}
		}

        public bool CanStep(StepType type)
        {
            switch (type)
            {
                case StepType.Into:
                case StepType.Over:
                case StepType.Out:
                    return true;
                default:
                    return false;
            }
        }


        public void Step(StepType type)
        {
            switch (type)
            {
                case StepType.Into:
                    StepInto();
                    break;
                case StepType.Out:
                    StepOut();
                    break;
                case StepType.Over:
                    StepOver();
                    break;
            }
        }

        private void StepInto()
        {
            while (board.cpu.AtInstructionStart())
            {
                DoCycle();
            }
            while (!board.cpu.AtInstructionStart())
            {
                DoCycle();
            }
        }

        private void StepOver()
        {
            var instruction = board.cpu.Peek(board.cpu.PC);

            if (instruction == JSR)
            {
                var destination = board.cpu.PC + JSRSize;
                while (board.cpu.PC != destination)
                {
                    StepInto();
                }
            }
            else
            {
                StepInto();
            }
        }

        private void StepOut()
        {
            var instr = board.cpu.Peek(board.cpu.PC);

            JSRCount = instr == JSR ? 1 : 0;

            var bailOutFrame = Frame + 1;

            while (true)
            {
                StepInto();
                instr = board.cpu.Peek(board.cpu.PC);
                if (instr == JSR)
                {
                    JSRCount++;
                }
                else if ((instr == RTS || instr == RTI) && JSRCount <= 0)
                {
                    StepInto();
                    JSRCount = 0;
                    break;
                }
                else if (instr == RTS || instr == RTI)
                {
                    JSRCount--;
                }
                else //Emergency Bailout Logic
                {
                    if (Frame == bailOutFrame)
                    {
                        break;
                    }
                }
            }
        }

        private int JSRCount = 0;

        private const byte JSR = 0x20;
        private const byte RTI = 0x40;
        private const byte RTS = 0x60;

        private const byte JSRSize = 3;

        public IMemoryCallbackSystem MemoryCallbacks { get; private set; }
    }
}
