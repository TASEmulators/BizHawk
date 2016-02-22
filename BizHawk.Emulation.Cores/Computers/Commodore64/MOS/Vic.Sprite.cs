using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Computers.Commodore64.MOS
{
	public sealed partial class Vic
	{
		private sealed class Sprite
		{
			public bool CollideData;
			public bool CollideSprite;
			public int Color;
			public bool Display;
			public bool Dma;
			public bool Enable;
			public int Loaded;
			public int Mc;
			public int Mcbase;
			public bool Multicolor;
			public bool MulticolorCrunch;
			public int Pointer;
			public bool Priority;
			public bool ShiftEnable;
			public int Sr;
			public int X;
			public bool XCrunch;
			public bool XExpand;
			public int Y;
			public bool YCrunch;
			public bool YExpand;

			public void HardReset()
			{
				CollideData = false;
				CollideSprite = false;
				Color = 0;
				Display = false;
				Dma = false;
				Enable = false;
				Mc = 0;
				Mcbase = 0;
				Multicolor = false;
				MulticolorCrunch = false;
				Pointer = 0;
				Priority = false;
				ShiftEnable = false;
				Sr = 0;
				X = 0;
				XCrunch = false;
				XExpand = false;
				Y = 0;
				YCrunch = false;
				YExpand = false;
			}

			public void SyncState(Serializer ser)
			{
				SaveState.SyncObject(ser, this);
			}
		}
	}
}
