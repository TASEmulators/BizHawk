using System;
using System.Collections.Generic;
using System.IO;
using BizHawk.Emulation.CPUs.M6502;
using BizHawk.Emulation.Consoles.Atari;
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
	}
}
