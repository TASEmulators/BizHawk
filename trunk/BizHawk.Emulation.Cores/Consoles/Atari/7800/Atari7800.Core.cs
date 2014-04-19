using System;
using System.Collections.Generic;
using EMU7800.Core;

namespace BizHawk.Emulation.Cores.Atari.Atari7800
{
	partial class Atari7800
	{
		public byte[] rom;
		//Bios7800 NTSC_BIOS;
		//Bios7800 PAL_BIOS;
		public byte[] hsbios;
		public byte[] bios;
		Cart cart;
		MachineBase theMachine;
		EMU7800.Win.GameProgram GameInfo;
		public byte[] hsram = new byte[2048];

		public Dictionary<string, int> GetCpuFlagsAndRegisters()
		{
			return new Dictionary<string, int>
			{
				{ "A", theMachine.CPU.A },
				{ "P", theMachine.CPU.P },
				{ "PC", theMachine.CPU.PC },
				{ "S", theMachine.CPU.S },
				{ "X", theMachine.CPU.X },
				{ "Y", theMachine.CPU.Y },
				{ "Flag B", theMachine.CPU.fB ? 1 : 0 },
				{ "Flag C", theMachine.CPU.fC ? 1 : 0 },
				{ "Flag D", theMachine.CPU.fD ? 1 : 0 },
				{ "Flag I", theMachine.CPU.fI ? 1 : 0 },
				{ "Flag N", theMachine.CPU.fN ? 1 : 0 },
				{ "Flag V", theMachine.CPU.fV ? 1 : 0 },
				{ "Flag Z", theMachine.CPU.fZ ? 1 : 0 }
			};
		}
	}
}
