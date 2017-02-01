using System;
using System.Collections.Generic;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Atari.Atari2600
{
	public partial class Atari2600 : IDebuggable
	{
		public IDictionary<string, RegisterValue> GetCpuFlagsAndRegisters()
		{
			return new Dictionary<string, RegisterValue>
			{
				{ "A", Cpu.A },
				{ "X", Cpu.X },
				{ "Y", Cpu.Y },
				{ "S", Cpu.S },
				{ "PC", Cpu.PC },

				{ "Flag C", Cpu.FlagC },
				{ "Flag Z", Cpu.FlagZ },
				{ "Flag I", Cpu.FlagI },
				{ "Flag D", Cpu.FlagD },

				{ "Flag B", Cpu.FlagB },
				{ "Flag V", Cpu.FlagV },
				{ "Flag N", Cpu.FlagN },
				{ "Flag T", Cpu.FlagT }
			};
		}

		public void SetCpuRegister(string register, int value)
		{
			switch (register)
			{
				default:
					throw new InvalidOperationException();
				case "A":
					Cpu.A = (byte)value;
					break;
				case "X":
					Cpu.X = (byte)value;
					break;
				case "Y":
					Cpu.Y = (byte)value;
					break;
				case "S":
					Cpu.S = (byte)value;
					break;
				case "PC":
					Cpu.PC = (ushort)value;
					break;
				case "Flag I":
					Cpu.FlagI = value > 0;
					break;
			}
		}

		public IMemoryCallbackSystem MemoryCallbacks { get; private set; }

		public bool CanStep(StepType type)
		{
			switch (type)
			{
				case StepType.Into:
				case StepType.Out:
				case StepType.Over:
					return true;
				default:
					return false;
			}
		}

		[FeatureNotImplemented]
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

		public int TotalExecutedCycles
		{
			get { return Cpu.TotalExecutedCycles; }
		}

		private void StepInto()
		{
			do
			{
				CycleAdvance();
			} while (!Cpu.AtStart);
		}

		private void StepOver()
		{
			var instruction = Cpu.PeekMemory(Cpu.PC);

			if (instruction == JSR)
			{
				var destination = Cpu.PC + opsize[JSR];
				while(Cpu.PC != destination)
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
			var instruction = Cpu.PeekMemory(Cpu.PC);

			JSRCount = instruction == JSR ? 1 : 0;

			var bailOutFrame = Frame + 1;
			while (true)
			{
				StepInto();
				var instr = Cpu.PeekMemory(Cpu.PC);
				if (instr == JSR)
				{
					JSRCount++;
				}
				else if (instr == RTS && JSRCount <= 0)
				{
					StepInto();
					JSRCount = 0;
					break;
				}
				else if (instr == RTS)
				{
					JSRCount--;
				}
				else // Emergency bail out logic
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
		private const byte RTS = 0x60;

		//the opsize table is used to quickly grab the instruction sizes (in bytes)
		private readonly byte[] opsize = new byte[]
		{
		/*0x00*/	1,2,0,0,0,2,2,0,1,2,1,0,0,3,3,0,
		/*0x10*/	2,2,0,0,0,2,2,0,1,3,0,0,0,3,3,0,
		/*0x20*/	3,2,0,0,2,2,2,0,1,2,1,0,3,3,3,0,
		/*0x30*/	2,2,0,0,0,2,2,0,1,3,0,0,0,3,3,0,
		/*0x40*/	1,2,0,0,0,2,2,0,1,2,1,0,3,3,3,0,
		/*0x50*/	2,2,0,0,0,2,2,0,1,3,0,0,0,3,3,0,
		/*0x60*/	1,2,0,0,0,2,2,0,1,2,1,0,3,3,3,0,
		/*0x70*/	2,2,0,0,0,2,2,0,1,3,0,0,0,3,3,0,
		/*0x80*/	0,2,0,0,2,2,2,0,1,0,1,0,3,3,3,0,
		/*0x90*/	2,2,0,0,2,2,2,0,1,3,1,0,0,3,0,0,
		/*0xA0*/	2,2,2,0,2,2,2,0,1,2,1,0,3,3,3,0,
		/*0xB0*/	2,2,0,0,2,2,2,0,1,3,1,0,3,3,3,0,
		/*0xC0*/	2,2,0,0,2,2,2,0,1,2,1,0,3,3,3,0,
		/*0xD0*/	2,2,0,0,0,2,2,0,1,3,0,0,0,3,3,0,
		/*0xE0*/	2,2,0,0,2,2,2,0,1,2,1,0,3,3,3,0,
		/*0xF0*/	2,2,0,0,0,2,2,0,1,3,0,0,0,3,3,0
		};


		/*the optype table is a quick way to grab the addressing mode for any 6502 opcode
		//
		//  0 = Implied\Accumulator\Immediate\Branch\NULL
		//  1 = (Indirect,X)
		//  2 = Zero Page
		//  3 = Absolute
		//  4 = (Indirect),Y
		//  5 = Zero Page,X
		//  6 = Absolute,Y
		//  7 = Absolute,X
		//  8 = Zero Page,Y
		*/
		private readonly byte[] optype = new byte[]
		{
		/*0x00*/	0,1,0,0,0,2,2,0,0,0,0,0,0,3,3,0,
		/*0x10*/	0,4,0,0,0,5,5,0,0,6,0,0,0,7,7,0,
		/*0x20*/	0,1,0,0,2,2,2,0,0,0,0,0,3,3,3,0,
		/*0x30*/	0,4,0,0,0,5,5,0,0,6,0,0,0,7,7,0,
		/*0x40*/	0,1,0,0,0,2,2,0,0,0,0,0,0,3,3,0,
		/*0x50*/	0,4,0,0,0,5,5,0,0,6,0,0,0,7,7,0,
		/*0x60*/	0,1,0,0,0,2,2,0,0,0,0,0,3,3,3,0,
		/*0x70*/	0,4,0,0,0,5,5,0,0,6,0,0,0,7,7,0,
		/*0x80*/	0,1,0,0,2,2,2,0,0,0,0,0,3,3,3,0,
		/*0x90*/	0,4,0,0,5,5,8,0,0,6,0,0,0,7,0,0,
		/*0xA0*/	0,1,0,0,2,2,2,0,0,0,0,0,3,3,3,0,
		/*0xB0*/	0,4,0,0,5,5,8,0,0,6,0,0,7,7,6,0,
		/*0xC0*/	0,1,0,0,2,2,2,0,0,0,0,0,3,3,3,0,
		/*0xD0*/	0,4,0,0,0,5,5,0,0,6,0,0,0,7,7,0,
		/*0xE0*/	0,1,0,0,2,2,2,0,0,0,0,0,3,3,3,0,
		/*0xF0*/	0,4,0,0,0,5,5,0,0,6,0,0,0,7,7,0
		};

		#region Currently Unused Debug hooks

		private void ScanlineAdvance()
		{
			StartFrameCond();
			int currentLine = _tia.LineCount;
			while (_tia.LineCount == currentLine)
				Cycle();
			FinishFrameCond();
		}

		private void CycleAdvance()
		{
			StartFrameCond();
			Cycle();
			FinishFrameCond();
		}

		private int CurrentScanLine
		{
			get { return _tia.LineCount; }
		}

		private bool IsVsync
		{
			get { return _tia.IsVSync; }
		}

		private bool IsVBlank
		{
			get { return _tia.IsVBlank; }
		}

		#endregion
	}
}
