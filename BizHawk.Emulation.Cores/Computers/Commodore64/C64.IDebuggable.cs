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
				{ "A", _board.cpu.A },
				{ "X", _board.cpu.X },
				{ "Y", _board.cpu.Y },
				{ "S", _board.cpu.S },
				{ "PC", _board.cpu.PC },
				{ "Flag C", _board.cpu.FlagC },
				{ "Flag Z", _board.cpu.FlagZ },
				{ "Flag I", _board.cpu.FlagI },
				{ "Flag D", _board.cpu.FlagD },
				{ "Flag B", _board.cpu.FlagB },
				{ "Flag V", _board.cpu.FlagV },
				{ "Flag N", _board.cpu.FlagN },
				{ "Flag T", _board.cpu.FlagT }
			};
		}

		public void SetCpuRegister(string register, int value)
		{
			switch (register)
			{
				default:
					throw new InvalidOperationException();
				case "A":
					_board.cpu.A = (byte)value;
					break;
				case "X":
					_board.cpu.X = (byte)value;
					break;
				case "Y":
					_board.cpu.Y = (byte)value;
					break;
				case "S":
					_board.cpu.S = (byte)value;
					break;
				case "PC":
					_board.cpu.PC = (ushort)value;
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
			while (_board.cpu.AtInstructionStart())
			{
				DoCycle();
			}
			while (!_board.cpu.AtInstructionStart())
			{
				DoCycle();
			}
		}

		private void StepOver()
		{
			var instruction = _board.cpu.Peek(_board.cpu.PC);

			if (instruction == JSR)
			{
				var destination = _board.cpu.PC + JSRSize;
				while (_board.cpu.PC != destination)
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
			var instr = _board.cpu.Peek(_board.cpu.PC);

			JSRCount = instr == JSR ? 1 : 0;

			var bailOutFrame = Frame + 1;

			while (true)
			{
				StepInto();
				instr = _board.cpu.Peek(_board.cpu.PC);
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
