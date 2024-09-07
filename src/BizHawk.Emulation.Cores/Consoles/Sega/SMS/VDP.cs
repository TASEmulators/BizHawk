using BizHawk.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Components.Z80A;

namespace BizHawk.Emulation.Cores.Sega.MasterSystem
{
	public enum VdpMode { SMS, GameGear }

	// Emulates the Texas Instruments TMS9918 VDP.
	public partial class VDP : IVideoProvider
	{
		// VDP State
		public byte[] VRAM = new byte[0x4000]; //16kb video RAM
		public byte[] CRAM; // SMS = 32 bytes, GG = 64 bytes CRAM
		public byte[] Registers = { 0x06, 0x80, 0xFF, 0xFF, 0xFF, 0xFF, 0xFB, 0xF0, 0x00, 0x00, 0xFF, 0x00, 0x00, 0x00, 0x00, 0x00 };
		public byte StatusByte;

		private const int Command_VramRead = 0x00;
		private const int Command_VramWrite = 0x40;
		private const int Command_RegisterWrite = 0x80;
		private const int Command_CramWrite = 0xC0;

		private bool VdpWaitingForLatchByte = true;
		private byte VdpLatch;
		private byte VdpBuffer;
		private ushort VdpAddress;
		private byte VdpCommand;
		private int TmsMode = 4;

		private bool VIntPending;
		private bool HIntPending;
		private int lineIntLinesRemaining;

		private readonly SMS Sms;
		private readonly VdpMode mode;
		public DisplayType DisplayType = DisplayType.NTSC;
		private readonly Z80A<SMS.CpuLink> Cpu;

		public bool SpriteLimit;
		public int IPeriod = 228;
		public VdpMode VdpMode => mode;

		public int FrameHeight = 192;
		public int ScanLine;
		public byte HCounter = 0x90;
		public int[] FrameBuffer = new int[256 * 192];
		public int[] GameGearFrameBuffer = new int[160 * 144];
		public int[] OverscanFrameBuffer = null;
		public int[] Backdrop_SL = new int[256];

		public bool Mode1Bit => (Registers[1] & 16) > 0;
		public bool Mode2Bit => (Registers[0] & 2) > 0;
		public bool Mode3Bit => (Registers[1] & 8) > 0;
		public bool Mode4Bit => (Registers[0] & 4) > 0;
		public bool ShiftSpritesLeft8Pixels => (Registers[0] & 8) > 0;
		public bool EnableLineInterrupts => (Registers[0] & 16) > 0;
		public bool LeftBlanking => (Registers[0] & 32) > 0;
		public bool HorizScrollLock => (Registers[0] & 64) > 0;
		public bool VerticalScrollLock => (Registers[0] & 128) > 0;
		public bool EnableDoubledSprites => (Registers[1] & 1) > 0;
		public bool EnableLargeSprites => (Registers[1] & 2) > 0;
		public bool EnableFrameInterrupts => (Registers[1] & 32) > 0;
		public bool DisplayOn => (Registers[1] & 64) > 0;
		public int SpriteAttributeTableBase => ((Registers[5] >> 1) << 8) & 0x3FFF;
		public int SpriteTileBase => (Registers[6] & 4) > 0 ? 256 : 0;
		public byte BackdropColor => (byte)(16 + (Registers[7] & 15));

		private int NameTableBase;
		private int ColorTableBase;
		private int PatternGeneratorBase;
		private int SpritePatternGeneratorBase;
		private int TmsPatternNameTableBase;
		private int TmsSpriteAttributeBase;

		// older versions fo the SMS VDP have a masking bit in register two that effects mirroring.
		// This is needed for Ys (JPN) in the status bar
		private int NameTableMaskBit;
		private bool JPN_Compat =false;

		// For SMS, the last 8 x-tiles are fixed if vertscroll (reg[0].bit(7)) is set, but on GG it must be
		// only the last 7 or Fray displays incorrectly
		private int lock_tile_start;

		// preprocessed state assist stuff.
		public int[] Palette = new int[32];
		public byte[] PatternBuffer = new byte[0x8000];

		private readonly byte[] ScanlinePriorityBuffer = new byte[256];
		private readonly byte[] SpriteCollisionBuffer = new byte[256];

		private static readonly byte[] SMSPalXlatTable = { 0, 85, 170, 255 };
		private static readonly byte[] GGPalXlatTable = { 0, 17, 34, 51, 68, 85, 102, 119, 136, 153, 170, 187, 204, 221, 238, 255 };

		public VDP(SMS sms, Z80A<SMS.CpuLink> cpu, VdpMode mode, DisplayType displayType, bool region_compat)
		{
			Sms = sms;
			Cpu = cpu;
			this.mode = mode;
			if (mode == VdpMode.SMS) CRAM = new byte[32];
			if (mode == VdpMode.GameGear) CRAM = new byte[64];
			DisplayType = displayType;
			if (mode == VdpMode.SMS) { JPN_Compat = region_compat; }
			NameTableBase = CalcNameTableBase();

			lock_tile_start = mode == VdpMode.SMS ? 24 : 25;
		}

		public byte ReadData()
		{
			VdpWaitingForLatchByte = true;
			byte value = VdpBuffer;
			VdpBuffer = VRAM[VdpAddress & 0x3FFF];
			VdpAddress++;
			return value;
		}

		public byte ReadVdpStatus()
		{
			VdpWaitingForLatchByte = true;
			byte returnValue = StatusByte;
			StatusByte &= 0x1F;
			HIntPending = false;
			VIntPending = false;
			Cpu.FlagI = false;
			return returnValue;
		}

		public byte ReadVLineCounter()
		{
			if (DisplayType == DisplayType.NTSC)
			{
				if (FrameHeight == 192)
					return VLineCounterTableNTSC192[ScanLine];
				if (FrameHeight == 224)
					return VLineCounterTableNTSC224[ScanLine];
				return VLineCounterTableNTSC240[ScanLine];
			}
			else
			{ // PAL
				if (FrameHeight == 192)
					return VLineCounterTablePAL192[ScanLine];
				if (FrameHeight == 224)
					return VLineCounterTablePAL224[ScanLine];
				return VLineCounterTablePAL240[ScanLine];
			}
		}

		public byte ReadHLineCounter()
		{
			return HCounter;
		}

		public void WriteVdpControl(byte value)
		{
			if (VdpWaitingForLatchByte)
			{
				VdpLatch = value;
				VdpWaitingForLatchByte = false;
				VdpAddress = (ushort)((VdpAddress & 0xFF00) | value);
				return;
			}

			VdpWaitingForLatchByte = true;
			VdpAddress = (ushort)(((value & 63) << 8) | VdpLatch);
			switch (value & 0xC0)
			{
				case 0x00: // read VRAM
					VdpCommand = Command_VramRead;
					VdpBuffer = VRAM[VdpAddress & 0x3FFF];
					VdpAddress++;
					break;
				case 0x40: // write VRAM
					VdpCommand = Command_VramWrite;
					break;
				case 0x80: // VDP register write
					VdpCommand = Command_RegisterWrite;
					int reg = value & 0x0F;
					WriteRegister(reg, VdpLatch);
					break;
				case 0xC0: // write CRAM / modify palette
					VdpCommand = Command_CramWrite;
					break;
			}
		}

		public void WriteVdpData(byte value)
		{
			VdpWaitingForLatchByte = true;
			VdpBuffer = value;
			if (VdpCommand == Command_CramWrite)
			{
				// Write Palette / CRAM
				int mask = VdpMode == VdpMode.SMS ? 0x1F : 0x3F;
				CRAM[VdpAddress & mask] = value;
				UpdatePrecomputedPalette();
			}
			else
			{
				// Write VRAM and update pre-computed pattern buffer. 
				UpdatePatternBuffer((ushort)(VdpAddress & 0x3FFF), value);
				VRAM[VdpAddress & 0x3FFF] = value;
			}
			VdpAddress++;
		}

		public void UpdatePrecomputedPalette()
		{
			if (mode == VdpMode.SMS)
			{
				for (int i = 0; i < 32; i++)
				{
					byte value = CRAM[i];
					byte r = SMSPalXlatTable[(value & 0x03)];
					byte g = SMSPalXlatTable[(value & 0x0C) >> 2];
					byte b = SMSPalXlatTable[(value & 0x30) >> 4];
					Palette[i] = Colors.ARGB(r, g, b);
				}
			}
			else
			{ // GameGear
				for (int i = 0; i < 32; i++)
				{
					ushort value = (ushort)((CRAM[(i * 2) + 1] << 8) | CRAM[(i * 2) + 0]);
					byte r = GGPalXlatTable[(value & 0x000F)];
					byte g = GGPalXlatTable[(value & 0x00F0) >> 4];
					byte b = GGPalXlatTable[(value & 0x0F00) >> 8];
					Palette[i] = Colors.ARGB(r, g, b);
				}
			}
		}

		public int CalcNameTableBase()
		{
			if (JPN_Compat) { NameTableMaskBit = 0xFBFF + ((Registers[2] & 1) << 10); }
			if (FrameHeight == 192)
				return 1024 * (Registers[2] & 0x0E);
			return (1024 * (Registers[2] & 0x0C)) + 0x0700;
		}

		private void CheckVideoMode()
		{
			if (!Mode4Bit) // check old TMS modes
			{
				if (Mode1Bit) TmsMode = 1;
				else if (Mode2Bit) TmsMode = 2;
				else if (Mode3Bit) TmsMode = 3;
				else TmsMode = 0;
			}

			else if (Mode4Bit && Mode2Bit) // if Mode4 and Mode2 set, then check extension modes
			{
				TmsMode = 4;
				switch (Registers[1] & 0x18)
				{
					case 0x00:
					case 0x18: // 192-line mode
						if (FrameHeight != 192)
						{
							FrameHeight = 192;
							FrameBuffer = new int[256 * 192];
							NameTableBase = CalcNameTableBase();
						}
						break;
					case 0x10: // 224-line mode
						if (FrameHeight != 224)
						{
							FrameHeight = 224;
							FrameBuffer = new int[256 * 224];
							NameTableBase = CalcNameTableBase();
						}
						break;
					case 0x08: // 240-line mode
						if (FrameHeight != 240)
						{
							FrameHeight = 240;
							FrameBuffer = new int[256 * 240];
							NameTableBase = CalcNameTableBase();
						}
						break;
				}
			}

			else
			{ // default to standard 192-line mode4
				TmsMode = 4;
				if (FrameHeight != 192)
				{
					FrameHeight = 192;
					FrameBuffer = new int[256 * 192];
					NameTableBase = CalcNameTableBase();
				}
			}
		}

		private void WriteRegister(int reg, byte data)
		{
			Registers[reg] = data;

			switch (reg)
			{
				case 0: // Mode Control Register 1
					CheckVideoMode();
					Cpu.FlagI = (EnableLineInterrupts && HIntPending);
					Cpu.FlagI |= (EnableFrameInterrupts && VIntPending);
					break;
				case 1: // Mode Control Register 2
					CheckVideoMode();
					Cpu.FlagI = (EnableFrameInterrupts && VIntPending);
					Cpu.FlagI |= (EnableLineInterrupts && HIntPending);
					break;
				case 2: // Name Table Base Address
					NameTableBase = CalcNameTableBase();
					TmsPatternNameTableBase = (Registers[2] << 10) & 0x3C00;
					break;
				case 3: // Color Table Base Address
					ColorTableBase = (Registers[3] << 6) & 0x3FC0;
					break;
				case 4: // Pattern Generator Base Address
					PatternGeneratorBase = (Registers[4] << 11) & 0x3800;
					break;
				case 5: // Sprite Attribute Table Base Address
					// ??? should I move from my property to precalculated?
					TmsSpriteAttributeBase = (Registers[5] << 7) & 0x3F80;
					break;
				case 6: // Sprite Pattern Generator Base Adderss 
					SpritePatternGeneratorBase = (Registers[6] << 11) & 0x3800;
					break;
			}

		}

		private static readonly byte[] pow2 = { 1, 2, 4, 8, 16, 32, 64, 128 };

		private void UpdatePatternBuffer(ushort address, byte value)
		{
			// writing one byte affects 8 pixels due to stupid planar storage.
			for (int i = 0; i < 8; i++)
			{
				byte colorBit = pow2[address % 4];
				byte sourceBit = pow2[7 - i];
				ushort dest = (ushort)(((address & 0xFFFC) * 2) + i);
				if ((value & sourceBit) > 0) // setting bit
					PatternBuffer[dest] |= colorBit;
				else // clearing bit
					PatternBuffer[dest] &= (byte)~colorBit;
			}
		}

		public void ProcessFrameInterrupt()
		{
			if (ScanLine == FrameHeight + 1)
			{
				StatusByte |= 0x80;
				VIntPending = true;
			}

			if (VIntPending && EnableFrameInterrupts)
			{
				Cpu.FlagI = true;
			}
				
		}

		public void ProcessLineInterrupt()
		{
			if (ScanLine <= FrameHeight)
			{
				if (lineIntLinesRemaining-- <= 0)
				{
					HIntPending = true;
					if (EnableLineInterrupts)
					{
						Cpu.FlagI = true;
					}
					lineIntLinesRemaining = Registers[0x0A];
				}
				return;
			}
			// else we're outside the active display period
			lineIntLinesRemaining = Registers[0x0A];
		}

		internal void RenderCurrentScanline(bool render)
		{
			if (ScanLine < FrameHeight)
			{
				Backdrop_SL[ScanLine] = Palette[(byte)(16 + (Registers[7] & 15))];
			}
			
			// only mode 4 supports frameskip. deal with it
			if (TmsMode == 4)
			{
				if (render)
					RenderBackgroundCurrentLine(Sms.Settings.DispBG);

				if (EnableDoubledSprites)
					RenderSpritesCurrentLineDoubleSize(Sms.Settings.DispOBJ & render);
				else
					RenderSpritesCurrentLine(Sms.Settings.DispOBJ & render);

				RenderLineBlanking(render);
			}
			else if (TmsMode == 2)
			{
				RenderBackgroundM2(Sms.Settings.DispBG);
				RenderTmsSprites(Sms.Settings.DispOBJ);
			}
			else if (TmsMode == 0)
			{
				RenderBackgroundM0(Sms.Settings.DispBG);
				RenderTmsSprites(Sms.Settings.DispOBJ);
			}
		}

		public void SyncState(Serializer ser)
		{
			ser.BeginSection(nameof(VDP));
			ser.Sync(nameof(StatusByte), ref StatusByte);
			ser.Sync("WaitingForLatchByte", ref VdpWaitingForLatchByte);
			ser.Sync("Latch", ref VdpLatch);
			ser.Sync("ReadBuffer", ref VdpBuffer);
			ser.Sync(nameof(VdpAddress), ref VdpAddress);
			ser.Sync("Command", ref VdpCommand);
			ser.Sync(nameof(HIntPending), ref HIntPending);
			ser.Sync(nameof(VIntPending), ref VIntPending);
			ser.Sync("LineIntLinesRemaining", ref lineIntLinesRemaining);
			ser.Sync(nameof(Registers), ref Registers, false);
			ser.Sync(nameof(CRAM), ref CRAM, false);
			ser.Sync(nameof(VRAM), ref VRAM, false);
			ser.Sync(nameof(HCounter), ref HCounter);
			ser.Sync(nameof(Backdrop_SL), ref Backdrop_SL, false);
			ser.EndSection();

			if (ser.IsReader)
			{
				for (int i = 0; i < Registers.Length; i++)
					WriteRegister(i, Registers[i]);
				for (ushort i = 0; i < VRAM.Length; i++)
					UpdatePatternBuffer(i, VRAM[i]);
				UpdatePrecomputedPalette();
			}
		}

		public int[] GetVideoBuffer()
		{
			if (mode == VdpMode.SMS && Sms.Settings.DisplayOverscan)
			{
				if (OverscanFrameBuffer == null)
					ProcessOverscan();
				return OverscanFrameBuffer;
			}
			if (mode == VdpMode.SMS || Sms.Settings.ShowClippedRegions)
				return FrameBuffer;
			return GameGearFrameBuffer;
		}

		public int VirtualWidth
		{ 
			get
			{
				if (mode == VdpMode.SMS && Sms.Settings.DisplayOverscan)
					return OverscanFrameWidth;
				if (mode == VdpMode.SMS)
					return 293;
				if (Sms.Settings.ShowClippedRegions)
					return 256;
				return 160;
			}
		}
		public int VirtualHeight => BufferHeight;

		public int BufferWidth
		{
			get
			{
				if (mode == VdpMode.SMS && Sms.Settings.DisplayOverscan)
					return OverscanFrameWidth;
				if (mode == VdpMode.SMS || Sms.Settings.ShowClippedRegions)
					return 256;
				return 160; // GameGear
			}
		}

		public int BufferHeight
		{
			get
			{
				if (mode == VdpMode.SMS && Sms.Settings.DisplayOverscan)
					return OverscanFrameHeight;
				if (mode == VdpMode.SMS || Sms.Settings.ShowClippedRegions)
					return FrameHeight;
				return 144; // GameGear
			}
		}

		public int BackgroundColor => unchecked((int)0xFF000000);

		public int VsyncNumerator => DisplayType == DisplayType.NTSC ? 60 : 50;

		public int VsyncDenominator => 1;
	}
}
