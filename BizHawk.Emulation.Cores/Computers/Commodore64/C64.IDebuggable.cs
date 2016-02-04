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
				{ "A", _board.Cpu.A },
				{ "X", _board.Cpu.X },
				{ "Y", _board.Cpu.Y },
				{ "S", _board.Cpu.S },
				{ "PC", _board.Cpu.Pc },
				{ "Flag C", _board.Cpu.FlagC },
				{ "Flag Z", _board.Cpu.FlagZ },
				{ "Flag I", _board.Cpu.FlagI },
				{ "Flag D", _board.Cpu.FlagD },
				{ "Flag B", _board.Cpu.FlagB },
				{ "Flag V", _board.Cpu.FlagV },
				{ "Flag N", _board.Cpu.FlagN },
				{ "Flag T", _board.Cpu.FlagT }
			};
		}

		public void SetCpuRegister(string register, int value)
		{
			switch (register)
			{
				default:
					throw new InvalidOperationException();
				case "A":
					_board.Cpu.A = (byte)value;
					break;
				case "X":
					_board.Cpu.X = (byte)value;
					break;
				case "Y":
					_board.Cpu.Y = (byte)value;
					break;
				case "S":
					_board.Cpu.S = (byte)value;
					break;
				case "PC":
					_board.Cpu.Pc = (ushort)value;
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
			while (_board.Cpu.AtInstructionStart())
			{
				DoCycle();
			}
			while (!_board.Cpu.AtInstructionStart())
			{
				DoCycle();
			}
		}

		private void StepOver()
		{
			var instruction = _board.Cpu.Peek(_board.Cpu.Pc);

			if (instruction == JSR)
			{
				var destination = _board.Cpu.Pc + JSRSize;
				while (_board.Cpu.Pc != destination)
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
			var instr = _board.Cpu.Peek(_board.Cpu.Pc);

			JSRCount = instr == JSR ? 1 : 0;

			var bailOutFrame = Frame + 1;

			while (true)
			{
				StepInto();
				instr = _board.Cpu.Peek(_board.Cpu.Pc);
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
