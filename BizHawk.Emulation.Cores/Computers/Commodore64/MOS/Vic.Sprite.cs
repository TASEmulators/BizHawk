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
			public int Index;
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

			public Sprite(int index)
			{
				Index = index;
			}

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
				ser.Sync(nameof(CollideData), ref CollideData);
				ser.Sync(nameof(CollideSprite), ref CollideSprite);
				ser.Sync(nameof(Color), ref Color);
				ser.Sync(nameof(Display), ref Display);
				ser.Sync(nameof(Dma), ref Dma);
				ser.Sync(nameof(Enable), ref Enable);
				ser.Sync(nameof(Loaded), ref Loaded);
				ser.Sync(nameof(Mc), ref Mc);
				ser.Sync(nameof(Mcbase), ref Mcbase);
				ser.Sync(nameof(Multicolor), ref Multicolor);
				ser.Sync(nameof(MulticolorCrunch), ref MulticolorCrunch);
				ser.Sync(nameof(Pointer), ref Pointer);
				ser.Sync(nameof(Priority), ref Priority);
				ser.Sync(nameof(ShiftEnable), ref ShiftEnable);
				ser.Sync(nameof(Sr), ref Sr);
				ser.Sync(nameof(X), ref X);
				ser.Sync(nameof(XCrunch), ref XCrunch);
				ser.Sync(nameof(XExpand), ref XExpand);
				ser.Sync(nameof(Y), ref Y);
				ser.Sync(nameof(YCrunch), ref YCrunch);
				ser.Sync(nameof(YExpand), ref YExpand);
			}
		}
	}
}
