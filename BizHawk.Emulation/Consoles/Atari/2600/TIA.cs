using System;
using System.Globalization;
using System.IO;
using BizHawk.Emulation.CPUs.M6507;
using System.Collections.Generic;

namespace BizHawk.Emulation.Consoles.Atari
{
	// Emulates the TIA
	public partial class TIA
	{
		Atari2600 core;

		byte hsyncCnt = 0;

		struct playerData
		{
			public byte grp;
			public byte dgrp;
			public byte color;
			public byte hPosCnt;
			public byte scanCnt;
			public byte HM;
			public bool reflect;
			public bool delay;
			public byte nusiz;
		};

		playerData player0;
		playerData player1;

		public TIA(Atari2600 core)
		{
			this.core = core;
		}

		// Execute TIA cycles

		// Every 4 cycles, increment the hsync counter
		// if in visible part of screen, parse playfield

	}
}