using System;
using System.Collections.Generic;
using EMU7800.Core;

namespace BizHawk.Emulation
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

		public List<KeyValuePair<string, int>> GetCpuFlagsAndRegisters()
		{
			return new List<KeyValuePair<string, int>>
			{
				new KeyValuePair<string, int>("A", theMachine.CPU.A),
				new KeyValuePair<string, int>("P", theMachine.CPU.P),
				new KeyValuePair<string, int>("PC", theMachine.CPU.PC),
				new KeyValuePair<string, int>("S", theMachine.CPU.S),
				new KeyValuePair<string, int>("X", theMachine.CPU.X),
				new KeyValuePair<string, int>("Y", theMachine.CPU.Y),
				new KeyValuePair<string, int>("B", theMachine.CPU.fB ? 1 : 0),
				new KeyValuePair<string, int>("C", theMachine.CPU.fC ? 1 : 0),
				new KeyValuePair<string, int>("D", theMachine.CPU.fD ? 1 : 0),
				new KeyValuePair<string, int>("I", theMachine.CPU.fI ? 1 : 0),
				new KeyValuePair<string, int>("N", theMachine.CPU.fN ? 1 : 0),
				new KeyValuePair<string, int>("V", theMachine.CPU.fV ? 1 : 0),
				new KeyValuePair<string, int>("Z", theMachine.CPU.fZ ? 1 : 0),
			};
		}
	}
}
