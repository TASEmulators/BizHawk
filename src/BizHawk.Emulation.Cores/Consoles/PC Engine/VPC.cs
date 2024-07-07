using BizHawk.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Components.H6280;

namespace BizHawk.Emulation.Cores.PCEngine
{
	// ------------------------------------------------------
	// HuC6202 Video Priority Controller
	// ------------------------------------------------------
	// Responsible for merging VDC1 and VDC2 data on the SuperGrafx.
	// Pretty much all documentation on the SuperGrafx courtesy of Charles MacDonald.

	public sealed class VPC : IVideoProvider
	{
		private readonly PCEngine PCE;
		public VDC VDC1;
		public VDC VDC2;
		public VCE VCE;
		public HuC6280 CPU;

		public byte[] Registers = { 0x11, 0x11, 0x00, 0x00, 0x00, 0x00, 0x00 };

		public int Window1Width => ((Registers[3] & 3) << 8) | Registers[2];
		public int Window2Width => ((Registers[5] & 3) << 8) | Registers[4];
		public int PriorityModeSlot0 => Registers[0] & 0x0F;
		public int PriorityModeSlot1 => (Registers[0] >> 4) & 0x0F;
		public int PriorityModeSlot2 => Registers[1] & 0x0F;
		public int PriorityModeSlot3 => (Registers[1] >> 4) & 0x0F;

		public VPC(PCEngine pce, VDC vdc1, VDC vdc2, VCE vce, HuC6280 cpu)
		{
			PCE = pce;
			VDC1 = vdc1;
			VDC2 = vdc2;
			VCE = vce;
			CPU = cpu;

			// latch initial video buffer
			FrameBuffer = vdc1.GetVideoBuffer();
			FrameWidth = vdc1.BufferWidth;
			FrameHeight = vdc1.BufferHeight;
		}

		public byte ReadVPC(int port)
		{
			port &= 0x0F;
			switch (port)
			{
				case 0x08: return Registers[0];
				case 0x09: return Registers[1];
				case 0x0A: return Registers[2];
				case 0x0B: return Registers[3];
				case 0x0C: return Registers[4];
				case 0x0D: return Registers[5];
				case 0x0E: return Registers[6];
				case 0x0F: return 0;
				default: return 0xFF;
			}
		}

		public void WriteVPC(int port, byte value)
		{
			port &= 0x0F;
			switch (port)
			{
				case 0x08: Registers[0] = value; break;
				case 0x09: Registers[1] = value; break;
				case 0x0A: Registers[2] = value; break;
				case 0x0B: Registers[3] = value; break;
				case 0x0C: Registers[4] = value; break;
				case 0x0D: Registers[5] = value; break;
				case 0x0E:
					// CPU Store Immediate VDC Select
					CPU.WriteVDC = (value & 1) == 0 ? VDC1.WriteVDC : VDC2.WriteVDC;
					Registers[6] = value;
					break;
			}
		}

		public void SyncState(Serializer ser)
		{
			ser.BeginSection(nameof(VPC));
			ser.Sync(nameof(Registers), ref Registers, false);
			ser.EndSection();

			if (ser.IsReader)
				WriteVPC(0x0E, Registers[6]);
		}

		// We use a single priority mode for the whole frame.
		// No commercial SGX games really use the 'window' features AFAIK.
		// And there are no homebrew SGX games I know of.
		// Maybe we'll emulate it in the native-code version.

		private const int RCR = 6;
		private const int BXR = 7;
		private const int BYR = 8;
		private const int VDW = 13;
		private const int DCR = 15;

		private int EffectivePriorityMode = 0;

		private int FrameHeight;
		private int FrameWidth;
		private int[] FrameBuffer;

		private readonly byte[] PriorityBuffer = new byte[512];
		private readonly byte[] InterSpritePriorityBuffer = new byte[512];

		public void ExecFrame(bool render)
		{
			// Determine the effective priority mode.
			if (Window1Width < 0x40 && Window2Width < 0x40)
				EffectivePriorityMode = PriorityModeSlot3 >> 2;
			else if (Window2Width > 512)
				EffectivePriorityMode = PriorityModeSlot1 >> 2;
			else
			{
				Console.WriteLine("Unsupported VPC window settings");
				EffectivePriorityMode = 0;
			}

			// Latch frame dimensions and framebuffer, for purely dumb reasons
			FrameWidth = VDC1.BufferWidth;
			FrameHeight = VDC1.BufferHeight;
			FrameBuffer = VDC1.GetVideoBuffer();

			int ScanLine = 0;
			int ActiveDisplayStartLine = VDC1.DisplayStartLine;

			while (true)
			{
				VDC1.ScanLine = ScanLine;
				VDC2.ScanLine = ScanLine;
				
				int VBlankLine = ActiveDisplayStartLine + VDC1.Registers[VDW] + 1;
				if (VBlankLine > 261)
					VBlankLine = 261;
				VDC1.ActiveLine = ScanLine - ActiveDisplayStartLine;
				VDC2.ActiveLine = VDC1.ActiveLine;
				bool InActiveDisplay = (ScanLine >= ActiveDisplayStartLine) && (ScanLine < VBlankLine);

				if (ScanLine == ActiveDisplayStartLine)
				{
					VDC1.RCRCounter = 0x40;
					VDC2.RCRCounter = 0x40;
				}

				if (ScanLine == VBlankLine)
				{
					VDC1.UpdateSpriteAttributeTable();
					VDC2.UpdateSpriteAttributeTable();
				}

				if (VDC1.RCRCounter == (VDC1.Registers[RCR] & 0x3FF))
				{
					if (VDC1.RasterCompareInterruptEnabled)
					{
						VDC1.StatusByte |= VDC.StatusRasterCompare;
						CPU.IRQ1Assert = true;
					}
				}

				if (VDC2.RCRCounter == (VDC2.Registers[RCR] & 0x3FF))
				{
					if (VDC2.RasterCompareInterruptEnabled)
					{
						VDC2.StatusByte |= VDC.StatusRasterCompare;
						CPU.IRQ1Assert = true;
					}
				}

				CPU.Execute(24);

				if (InActiveDisplay)
				{
					if (ScanLine == ActiveDisplayStartLine)
					{
						VDC1.BackgroundY = VDC1.Registers[BYR];
						VDC2.BackgroundY = VDC2.Registers[BYR];
					}
					else
					{
						VDC1.BackgroundY++;
						VDC1.BackgroundY &= 0x01FF;
						VDC2.BackgroundY++;
						VDC2.BackgroundY &= 0x01FF;
					}
				}

				CPU.Execute(VDC1.HBlankCycles - 24);

				if (InActiveDisplay)
				{
					if (render) RenderScanLine();
				}


				if (ScanLine == VBlankLine && VDC1.VBlankInterruptEnabled)
					VDC1.StatusByte |= VDC.StatusVerticalBlanking;

				if (ScanLine == VBlankLine && VDC2.VBlankInterruptEnabled)
					VDC2.StatusByte |= VDC.StatusVerticalBlanking;

				if (ScanLine == VBlankLine + 4 && VDC1.SatDmaPerformed)
				{
					VDC1.SatDmaPerformed = false;
					if ((VDC1.Registers[DCR] & 1) > 0)
						VDC1.StatusByte |= VDC.StatusVramSatDmaComplete;
				}

				if (ScanLine == VBlankLine + 4 && VDC2.SatDmaPerformed)
				{
					VDC2.SatDmaPerformed = false;
					if ((VDC2.Registers[DCR] & 1) > 0)
						VDC2.StatusByte |= VDC.StatusVramSatDmaComplete;
				}

				CPU.Execute(2);

				if ((VDC1.StatusByte & (VDC.StatusVerticalBlanking | VDC.StatusVramSatDmaComplete)) != 0)
					CPU.IRQ1Assert = true;

				if ((VDC2.StatusByte & (VDC.StatusVerticalBlanking | VDC.StatusVramSatDmaComplete)) != 0)
					CPU.IRQ1Assert = true;

				CPU.Execute(455 - VDC1.HBlankCycles - 2);

				if (!InActiveDisplay && VDC1.DmaRequested)
					VDC1.RunDmaForScanline();

				if (!InActiveDisplay && VDC2.DmaRequested)
					VDC2.RunDmaForScanline();

				VDC1.RCRCounter++;
				VDC2.RCRCounter++;
				ScanLine++;

				if (ScanLine == VCE.NumberOfScanlines)
					break;
			}
		}

		private void RenderScanLine()
		{
			if (PCE.Settings.BottomLine <= VDC1.ActiveLine + VDC1.ViewStartLine
				|| VDC1.ActiveLine + VDC1.ViewStartLine < PCE.Settings.TopLine)
			{
				return;
			}
			InitializeScanLine(VDC1.ActiveLine);

			switch (EffectivePriorityMode)
			{
				case 0:
					RenderBackgroundScanline(VDC1, 12, PCE.Settings.ShowBG1);
					RenderBackgroundScanline(VDC2, 2, PCE.Settings.ShowBG2);
					RenderSpritesScanline(VDC1, 11, 14, PCE.Settings.ShowOBJ1);
					RenderSpritesScanline(VDC2, 1, 3, PCE.Settings.ShowOBJ2);
					break;
				case 1:
					RenderBackgroundScanline(VDC1, 12, PCE.Settings.ShowBG1);
					RenderBackgroundScanline(VDC2, 2, PCE.Settings.ShowBG2);
					RenderSpritesScanline(VDC1, 11, 14, PCE.Settings.ShowOBJ1);
					RenderSpritesScanline(VDC2, 1, 13, PCE.Settings.ShowOBJ2);
					break;
			}
		}

		private void InitializeScanLine(int scanline)
		{
			// Clear priority buffer
			Array.Clear(PriorityBuffer, 0, FrameWidth);
			// Initialize scanline to background color
			for (int i = 0; i < FrameWidth; i++)
				FrameBuffer[((scanline + VDC1.ViewStartLine) * FrameWidth) + i] = VCE.Palette[256];
		}

		private unsafe void RenderBackgroundScanline(VDC vdc, byte priority, bool show)
		{
			if (!vdc.BackgroundEnabled)
				return;

			// per-line parameters
			int vertLine = vdc.BackgroundY;
			vertLine %= vdc.BatHeight * 8;
			int yTile = (vertLine / 8);
			int yOfs = vertLine % 8;
			int xScroll = vdc.Registers[BXR] & 0x3FF;
			int BatRowMask = vdc.BatWidth - 1;

			fixed (ushort* VRAMptr = vdc.VRAM)
			fixed (int* PALptr = VCE.Palette)
			fixed (byte* Patternptr = vdc.PatternBuffer)
			fixed (int* FBptr = FrameBuffer)
			fixed (byte* Priortyptr = PriorityBuffer)
			{
				// pointer to the BAT and the framebuffer for this line
				ushort* BatRow = VRAMptr + yTile * vdc.BatWidth;
				int* dst = FBptr + (vdc.ActiveLine + vdc.ViewStartLine - PCE.Settings.TopLine) * FrameWidth;

				// parameters that change per tile
				ushort BatEnt;
				int tileNo, paletteNo, paletteBase;
				byte* src;

				// calculate tile number and offset for first tile
				int xTile = (xScroll >> 3) & BatRowMask;
				int xOfs = xScroll & 7;

				// update per-tile parameters for first tile
				BatEnt = BatRow[xTile];
				tileNo = BatEnt & 2047;
				paletteNo = BatEnt >> 12;
				paletteBase = paletteNo * 16;
				src = Patternptr + (tileNo << 6 | yOfs << 3 | xOfs);

				for (int x = 0; x < FrameWidth; x++)
				{
					if (Priortyptr[x] < priority)
					{
						byte c = *src;
						if (c != 0)
						{
							dst[x] = show ? PALptr[paletteBase + c] : PALptr[0];
							Priortyptr[x] = priority;
						}
					}
					xOfs++;
					src++;
					if (xOfs == 8)
					{
						// update tile number
						xOfs = 0;
						xTile++;
						xTile &= BatRowMask;
						// update per-tile parameters
						BatEnt = BatRow[xTile];
						tileNo = BatEnt & 2047;
						paletteNo = BatEnt >> 12;
						paletteBase = paletteNo * 16;
						src = Patternptr + (tileNo << 6 | yOfs << 3 | xOfs);
					}
				}
			}
		}

		private static readonly byte[] heightTable = { 16, 32, 64, 64 };

		private void RenderSpritesScanline(VDC vdc, byte lowPriority, byte highPriority, bool show)
		{
			if (!vdc.SpritesEnabled)
				return;

			// clear inter-sprite priority buffer
			Array.Clear(InterSpritePriorityBuffer, 0, FrameWidth);

			var testRange = 0.MutableRangeTo(vdc.ActiveLine + 1);
			for (int i = 0; i < 64; i++)
			{
				int y = (vdc.SpriteAttributeTable[(i * 4) + 0] & 1023) - 64;
				int x = (vdc.SpriteAttributeTable[(i * 4) + 1] & 1023) - 32;
				ushort flags = vdc.SpriteAttributeTable[(i * 4) + 3];
				byte height = heightTable[(flags >> 12) & 3];
				testRange.Start = vdc.ActiveLine - height;
				if (!y.StrictlyBoundedBy(testRange)) continue;

				int patternNo = (((vdc.SpriteAttributeTable[(i * 4) + 2]) >> 1) & 0x1FF);
				int paletteBase = 256 + ((flags & 15) * 16);
				int width = (flags & 0x100) == 0 ? 16 : 32;
				bool priority = (flags & 0x80) != 0;
				bool hflip = (flags & 0x0800) != 0;
				bool vflip = (flags & 0x8000) != 0;

				if (width == 32)
					patternNo &= 0x1FE;

				int yofs;
				if (!vflip)
				{
					yofs = (vdc.ActiveLine - y) & 15;
					if (height == 32)
					{
						patternNo &= 0x1FD;
						if (vdc.ActiveLine - y >= 16)
						{
							y += 16;
							patternNo += 2;
						}
					}
					else if (height == 64)
					{
						patternNo &= 0x1F9;
						if (vdc.ActiveLine - y >= 48)
						{
							y += 48;
							patternNo += 6;
						}
						else if (vdc.ActiveLine - y >= 32)
						{
							y += 32;
							patternNo += 4;
						}
						else if (vdc.ActiveLine - y >= 16)
						{
							y += 16;
							patternNo += 2;
						}
					}
				}
				else // vflip == true
				{
					yofs = 15 - ((vdc.ActiveLine - y) & 15);
					if (height == 32)
					{
						patternNo &= 0x1FD;
						if (vdc.ActiveLine - y < 16)
						{
							y += 16;
							patternNo += 2;
						}
					}
					else if (height == 64)
					{
						patternNo &= 0x1F9;
						if (vdc.ActiveLine - y < 16)
						{
							y += 48;
							patternNo += 6;
						}
						else if (vdc.ActiveLine - y < 32)
						{
							y += 32;
							patternNo += 4;
						}
						else if (vdc.ActiveLine - y < 48)
						{
							y += 16;
							patternNo += 2;
						}
					}
				}
				if (!hflip)
				{
					if (x + width > 0 && y + height > 0)
					{
						for (int xs = x >= 0 ? x : 0; xs < x + 16 && xs >= 0 && xs < FrameWidth; xs++)
						{
							byte pixel = vdc.SpriteBuffer[(patternNo * 256) + (yofs * 16) + (xs - x)];
							if (pixel != 0 && InterSpritePriorityBuffer[xs] == 0)
							{
								InterSpritePriorityBuffer[xs] = 1;
								byte myPriority = priority ? highPriority : lowPriority;
								if (PriorityBuffer[xs] < myPriority)
								{
									if (show) FrameBuffer[((vdc.ActiveLine + vdc.ViewStartLine - PCE.Settings.TopLine) * FrameWidth) + xs] = VCE.Palette[paletteBase + pixel];
									PriorityBuffer[xs] = myPriority;
								}
							}
						}
					}
					if (width == 32)
					{
						patternNo++;
						x += 16;
						for (int xs = x >= 0 ? x : 0; xs < x + 16 && xs >= 0 && xs < FrameWidth; xs++)
						{
							byte pixel = vdc.SpriteBuffer[(patternNo * 256) + (yofs * 16) + (xs - x)];
							if (pixel != 0 && InterSpritePriorityBuffer[xs] == 0)
							{
								InterSpritePriorityBuffer[xs] = 1;
								byte myPriority = priority ? highPriority : lowPriority;
								if (PriorityBuffer[xs] < myPriority)
								{
									if (show) FrameBuffer[((vdc.ActiveLine + vdc.ViewStartLine - PCE.Settings.TopLine) * FrameWidth) + xs] = VCE.Palette[paletteBase + pixel];
									PriorityBuffer[xs] = myPriority;
								}
							}
						}
					}
				}
				else
				{ // hflip = true
					if (x + width > 0 && y + height > 0)
					{
						if (width == 32)
							patternNo++;
						for (int xs = x >= 0 ? x : 0; xs < x + 16 && xs >= 0 && xs < FrameWidth; xs++)
						{
							byte pixel = vdc.SpriteBuffer[(patternNo * 256) + (yofs * 16) + 15 - (xs - x)];
							if (pixel != 0 && InterSpritePriorityBuffer[xs] == 0)
							{
								InterSpritePriorityBuffer[xs] = 1;
								byte myPriority = priority ? highPriority : lowPriority;
								if (PriorityBuffer[xs] < myPriority)
								{
									if (show) FrameBuffer[((vdc.ActiveLine + vdc.ViewStartLine - PCE.Settings.TopLine) * FrameWidth) + xs] = VCE.Palette[paletteBase + pixel];
									PriorityBuffer[xs] = myPriority;
								}
							}
						}
						if (width == 32)
						{
							patternNo--;
							x += 16;
							for (int xs = x >= 0 ? x : 0; xs < x + 16 && xs >= 0 && xs < FrameWidth; xs++)
							{
								byte pixel = vdc.SpriteBuffer[(patternNo * 256) + (yofs * 16) + 15 - (xs - x)];
								if (pixel != 0 && InterSpritePriorityBuffer[xs] == 0)
								{
									InterSpritePriorityBuffer[xs] = 1;
									byte myPriority = priority ? highPriority : lowPriority;
									if (PriorityBuffer[xs] < myPriority)
									{
										if (show) FrameBuffer[((vdc.ActiveLine + vdc.ViewStartLine - PCE.Settings.TopLine) * FrameWidth) + xs] = VCE.Palette[paletteBase + pixel];
										PriorityBuffer[xs] = myPriority;
									}
								}
							}
						}
					}
				}
			}
		}

		// IVideoProvider implementation
		public int[] GetVideoBuffer() => FrameBuffer;
		public int VirtualWidth => FrameWidth;
		public int VirtualHeight => FrameHeight;
		public int BufferWidth => FrameWidth;
		public int BufferHeight => FrameHeight;
		public int BackgroundColor => VCE.Palette[0];

		public int VsyncNumerator
			=> NullVideo.DefaultVsyncNum; //TODO precise numbers or confirm the default is okay

		public int VsyncDenominator
			=> NullVideo.DefaultVsyncDen; //TODO precise numbers or confirm the default is okay
	}
}
