﻿using System;

namespace Jellyfish.Virtu
{
	[Flags]
	internal enum ScannerOptions { None = 0x0, AppleII = 0x1, Pal = 0x2 } // defaults to AppleIIe, Ntsc

	public interface IVideo
	{
		// ReSharper disable once UnusedMember.Global
		int[] GetVideoBuffer();
		// ReSharper disable once UnusedMember.Global
		void Reset();

		void DirtyCell(int addressOffset);
		void DirtyCellMixed(int addressOffset);
		void DirtyCellMixedText(int addressOffset);
		void DirtyScreen();
		int ReadFloatingBus();
		void SetCharSet();

		bool IsVBlank { get; }

		// ReSharper disable once UnusedMember.Global
		void Sync(IComponentSerializer ser);
	}

	public sealed partial class Video : IVideo
	{
		private readonly MachineEvents _events;
		private readonly IMemoryBus _memory;

		private readonly int[] _colorPalette = new int[ColorPaletteCount];

		private bool _isVBlank;
		private int[] _framebuffer = new int[560 * 384];
		private bool _isTextInversed;
		private ScannerOptions _scannerOptions;
		private int _cyclesPerVBlank;
		private int _cyclesPerVBlankPreset;
		private int _cyclesPerVSync;
		private int _cyclesPerFlash;
		private int _vCountPreset;
		private int _vLineLeaveVBlank;
		private ushort[] _charSet;
		private bool _isMonochrome;

		private bool[] _isCellDirty = new bool[Height * CellColumns + 1]; // includes sentinel

		public Video(MachineEvents events, IMemoryBus memory)
		{
			_events = events;
			_memory = memory;

			_events.AddEventDelegate(EventCallbacks.FlushRow, FlushRowEvent);
			_events.AddEventDelegate(EventCallbacks.InverseText, InverseTextEvent);
			_events.AddEventDelegate(EventCallbacks.LeaveVBlank, LeaveVBlankEvent);
			_events.AddEventDelegate(EventCallbacks.ResetVsync, ResetVSyncEvent);

			_flushRowMode = new Action<int>[]
			{
				FlushRowMode0, FlushRowMode1, FlushRowMode2, FlushRowMode3, FlushRowMode4, FlushRowMode5, FlushRowMode6, FlushRowMode7,
				FlushRowMode8, FlushRowMode9, FlushRowModeA, FlushRowModeB, FlushRowModeC, FlushRowModeD, FlushRowModeE, FlushRowModeF
			};

			unchecked
			{
				_colorBlack = (int)0xFF000000; // BGRA
				_colorDarkBlue = (int)0xFF000099;
				_colorDarkGreen = (int)0xFF117722;
				_colorMediumBlue = (int)0xFF0000FF;
				_colorBrown = (int)0xFF885500;
				_colorLightGrey = (int)0xFF99AAAA;
				_colorGreen = (int)0xFF00EE11;
				_colorAquamarine = (int)0xFF55FFAA;
				_colorDeepRed = (int)0xFFFF1111;
				_colorPurple = (int)0xFFDD00DD;
				_colorDarkGrey = (int)0xFF445555;
				_colorLightBlue = (int)0xFF33AAFF;
				_colorOrange = (int)0xFFFF4411;
				_colorPink = (int)0xFFFF9988;
				_colorYellow = (int)0xFFFFFF11;
				_colorWhite = (int)0xFFFFFFFF;
				_colorMonochrome = (int)0xFF00AA00;
			}

			SetPalette();

			IsMonochrome = false;
			ScannerOptions = ScannerOptions.None;

			_isVBlank = true;

			_events.AddEvent(_cyclesPerVBlankPreset, EventCallbacks.LeaveVBlank); // align flush events with scanner; assumes vcount preset at start of frame [3-15, 3-16]
			_events.AddEvent(_cyclesPerVSync, EventCallbacks.ResetVsync);
			_events.AddEvent(_cyclesPerFlash, EventCallbacks.InverseText);
		}

		public void Sync(IComponentSerializer ser)
		{
			if (ser.IsReader)
			{
				int option = 0;
				ser.Sync(nameof(_scannerOptions), ref option);
				_scannerOptions = (ScannerOptions)option;
			}
			else
			{
				int option = (int)_scannerOptions;
				ser.Sync(nameof(_scannerOptions), ref option);
			}

			ser.Sync(nameof(_isVBlank), ref _isVBlank);
			ser.Sync(nameof(_framebuffer), ref _framebuffer, false);
			ser.Sync(nameof(_isTextInversed), ref _isTextInversed);
			ser.Sync(nameof(_cyclesPerVBlank), ref _cyclesPerVBlank);
			ser.Sync(nameof(_cyclesPerVBlankPreset), ref _cyclesPerVBlankPreset);
			ser.Sync(nameof(_cyclesPerVSync), ref _cyclesPerVSync);
			ser.Sync(nameof(_cyclesPerFlash), ref _cyclesPerFlash);
			ser.Sync(nameof(_vCountPreset), ref _vCountPreset);
			ser.Sync(nameof(_vLineLeaveVBlank), ref _vLineLeaveVBlank);
			ser.Sync(nameof(_charSet), ref _charSet, false);
			ser.Sync(nameof(_isCellDirty), ref _isCellDirty, false);
			//ser.Sync(nameof(_isMonochrome), ref _isMonochrome);
			//TODO: does this affect sync?
		}

		// ReSharper disable once UnusedMember.Global
		public int[] GetVideoBuffer() => _framebuffer;

		public void Reset()
		{
			SetCharSet();
			DirtyScreen();
			FlushScreen();
		}

		void IVideo.DirtyCell(int addressOffset)
		{
			_isCellDirty[CellIndex[addressOffset]] = true;
		}

		void IVideo.DirtyCellMixed(int addressOffset)
		{
			int cellIndex = CellIndex[addressOffset];
			if (cellIndex < MixedCellIndex)
			{
				_isCellDirty[cellIndex] = true;
			}
		}

		void IVideo.DirtyCellMixedText(int addressOffset)
		{
			int cellIndex = CellIndex[addressOffset];
			if (cellIndex >= MixedCellIndex)
			{
				_isCellDirty[cellIndex] = true;
			}
		}

		public void DirtyScreen()
		{
			for (int i = 0; i < Height * CellColumns; i++)
			{
				_isCellDirty[i] = true;
			}
		}

		private void DirtyScreenText()
		{
			if (_memory.IsText)
			{
				for (int i = 0; i < MixedHeight * CellColumns; i++)
				{
					_isCellDirty[i] = true;
				}
			}
			if (_memory.IsText || _memory.IsMixed)
			{
				for (int i = MixedHeight * CellColumns; i < Height * CellColumns; i++)
				{
					_isCellDirty[i] = true;
				}
			}
		}

		public int ReadFloatingBus() // [5-40]
		{
			// derive scanner counters from current cycles into frame; assumes hcount and vcount preset at start of frame [3-13, 3-15, 3-16]
			int cycles = _cyclesPerVSync - _events.FindEvent(EventCallbacks.ResetVsync);
			int hClock = cycles % CyclesPerHSync;
			int hCount = (hClock != 0) ? HCountPreset + hClock - 1 : 0;
			int vLine = cycles / CyclesPerHSync;
			int vCount = _vCountPreset + vLine;

			// derive scanner address [5-8]
			int address = ((vCount << 4) & 0x0380) | ((0x0068 + (hCount & 0x0038) + (((vCount >> 1) & 0x0060) | ((vCount >> 3) & 0x0018))) & 0x0078) | (hCount & 0x0007);
			if (_memory.IsHires && !(_memory.IsMixed && ((vCount & 0xA0) == 0xA0))) // hires but not actively mixed [5-13, 5-19]
			{
				address |= (_memory.IsVideoPage2 ? 0x4000 : 0x2000) | ((vCount << 10) & 0x1C00);
			}
			else
			{
				address |= _memory.IsVideoPage2 ? 0x0800 : 0x0400;
				if (((_scannerOptions & ScannerOptions.AppleII) != 0) && (hCount < HCountLeaveHBlank))
				{
					address |= 0x1000;
				}
			}

			return _memory.Read(address);
		}

		public void SetCharSet()
		{
			_charSet = !_memory.IsCharSetAlternate ? CharSetPrimary : (_memory.Monitor == MonitorType.Standard) ? CharSetSecondaryStandard : CharSetSecondaryEnhanced;
			DirtyScreenText();
		}

		private void DrawText40(int data, int x, int y)
		{
			int color = IsMonochrome ? ColorMono00 : ColorWhite00;
			int index = _charSet[data] * CharBitmapBytes;
			int inverseMask = (_isTextInversed && !_memory.IsCharSetAlternate && (0x40 <= data) && (data <= 0x7F)) ? 0x7F : 0x00;
			for (int i = 0; i < TextHeight; i++, y++)
			{
				data = CharBitmap[index + i] ^ inverseMask;
				SetPixel(x + 0, y, color | (data & 0x01));
				SetPixel(x + 1, y, color | (data & 0x01));
				SetPixel(x + 2, y, color | (data & 0x02));
				SetPixel(x + 3, y, color | (data & 0x02));
				SetPixel(x + 4, y, color | (data & 0x04));
				SetPixel(x + 5, y, color | (data & 0x04));
				SetPixel(x + 6, y, color | (data & 0x08));
				SetPixel(x + 7, y, color | (data & 0x08));
				SetPixel(x + 8, y, color | (data & 0x10));
				SetPixel(x + 9, y, color | (data & 0x10));
				SetPixel(x + 10, y, color | (data & 0x20));
				SetPixel(x + 11, y, color | (data & 0x20));
				SetPixel(x + 12, y, color | (data & 0x40));
				SetPixel(x + 13, y, color | (data & 0x40));
			}
		}

		private void DrawText80(int data, int x, int y)
		{
			int color = IsMonochrome ? ColorMono00 : ColorWhite00;
			int index = _charSet[data] * CharBitmapBytes;
			int mask = (_isTextInversed && !_memory.IsCharSetAlternate && (0x40 <= data) && (data <= 0x7F)) ? 0x7F : 0x00;
			for (int i = 0; i < TextHeight; i++, y++)
			{
				data = CharBitmap[index + i] ^ mask;
				SetPixel(x + 0, y, color | (data & 0x01));
				SetPixel(x + 1, y, color | (data & 0x02));
				SetPixel(x + 2, y, color | (data & 0x04));
				SetPixel(x + 3, y, color | (data & 0x08));
				SetPixel(x + 4, y, color | (data & 0x10));
				SetPixel(x + 5, y, color | (data & 0x20));
				SetPixel(x + 6, y, color | (data & 0x40));
			}
		}

		private void DrawLores(int data, int x, int y)
		{
			if (IsMonochrome)
			{
				if ((x & 0x02) == 0x02) // odd cell
				{
					data = ((data << 2) & 0xCC) | ((data >> 2) & 0x33);
				}
				for (int i = 0; i < LoresHeight; i++, y++)
				{
					SetPixel(x + 0, y, data & 0x01);
					SetPixel(x + 1, y, data & 0x02);
					SetPixel(x + 2, y, data & 0x04);
					SetPixel(x + 3, y, data & 0x08);
					SetPixel(x + 4, y, data & 0x01);
					SetPixel(x + 5, y, data & 0x02);
					SetPixel(x + 6, y, data & 0x04);
					SetPixel(x + 7, y, data & 0x08);
					SetPixel(x + 8, y, data & 0x01);
					SetPixel(x + 9, y, data & 0x02);
					SetPixel(x + 10, y, data & 0x04);
					SetPixel(x + 11, y, data & 0x08);
					SetPixel(x + 12, y, data & 0x01);
					SetPixel(x + 13, y, data & 0x02);
				}
				for (int i = 0; i < LoresHeight; i++, y++)
				{
					SetPixel(x + 0, y, data & 0x10);
					SetPixel(x + 1, y, data & 0x20);
					SetPixel(x + 2, y, data & 0x40);
					SetPixel(x + 3, y, data & 0x80);
					SetPixel(x + 4, y, data & 0x10);
					SetPixel(x + 5, y, data & 0x20);
					SetPixel(x + 6, y, data & 0x40);
					SetPixel(x + 7, y, data & 0x80);
					SetPixel(x + 8, y, data & 0x10);
					SetPixel(x + 9, y, data & 0x20);
					SetPixel(x + 10, y, data & 0x40);
					SetPixel(x + 11, y, data & 0x80);
					SetPixel(x + 12, y, data & 0x10);
					SetPixel(x + 13, y, data & 0x20);
				}
			}
			else
			{
				int color = ColorLores[data & 0x0F];
				for (int i = 0; i < LoresHeight; i++, y++)
				{
					SetPixel(x + 0, y, color);
					SetPixel(x + 1, y, color);
					SetPixel(x + 2, y, color);
					SetPixel(x + 3, y, color);
					SetPixel(x + 4, y, color);
					SetPixel(x + 5, y, color);
					SetPixel(x + 6, y, color);
					SetPixel(x + 7, y, color);
					SetPixel(x + 8, y, color);
					SetPixel(x + 9, y, color);
					SetPixel(x + 10, y, color);
					SetPixel(x + 11, y, color);
					SetPixel(x + 12, y, color);
					SetPixel(x + 13, y, color);
				}
				color = ColorLores[data >> 4];
				for (int i = 0; i < LoresHeight; i++, y++)
				{
					SetPixel(x + 0, y, color);
					SetPixel(x + 1, y, color);
					SetPixel(x + 2, y, color);
					SetPixel(x + 3, y, color);
					SetPixel(x + 4, y, color);
					SetPixel(x + 5, y, color);
					SetPixel(x + 6, y, color);
					SetPixel(x + 7, y, color);
					SetPixel(x + 8, y, color);
					SetPixel(x + 9, y, color);
					SetPixel(x + 10, y, color);
					SetPixel(x + 11, y, color);
					SetPixel(x + 12, y, color);
					SetPixel(x + 13, y, color);
				}
			}
		}

		private void Draw7MLores(int data, int x, int y)
		{
			if (IsMonochrome)
			{
				if ((x & 0x02) == 0x02) // odd cell
				{
					data = ((data << 2) & 0xCC) | ((data >> 2) & 0x33);
				}
				for (int i = 0; i < LoresHeight; i++, y++)
				{
					SetPixel(x + 0, y, data & 0x01);
					SetPixel(x + 1, y, data & 0x01);
					SetPixel(x + 2, y, data & 0x02);
					SetPixel(x + 3, y, data & 0x02);
					SetPixel(x + 4, y, data & 0x04);
					SetPixel(x + 5, y, data & 0x04);
					SetPixel(x + 6, y, data & 0x08);
					SetPixel(x + 7, y, data & 0x08);
					SetPixel(x + 8, y, data & 0x01);
					SetPixel(x + 9, y, data & 0x01);
					SetPixel(x + 10, y, data & 0x02);
					SetPixel(x + 11, y, data & 0x02);
					SetPixel(x + 12, y, data & 0x04);
					SetPixel(x + 13, y, data & 0x04);
				}
				for (int i = 0; i < LoresHeight; i++, y++)
				{
					SetPixel(x + 0, y, data & 0x10);
					SetPixel(x + 1, y, data & 0x10);
					SetPixel(x + 2, y, data & 0x20);
					SetPixel(x + 3, y, data & 0x20);
					SetPixel(x + 4, y, data & 0x40);
					SetPixel(x + 5, y, data & 0x40);
					SetPixel(x + 6, y, data & 0x80);
					SetPixel(x + 7, y, data & 0x80);
					SetPixel(x + 8, y, data & 0x10);
					SetPixel(x + 9, y, data & 0x10);
					SetPixel(x + 10, y, data & 0x20);
					SetPixel(x + 11, y, data & 0x20);
					SetPixel(x + 12, y, data & 0x40);
					SetPixel(x + 13, y, data & 0x40);
				}
			}
			else
			{
				int color = Color7MLores[((x & 0x02) << 3) | (data & 0x0F)];
				for (int i = 0; i < LoresHeight; i++, y++)
				{
					SetPixel(x + 0, y, color);
					SetPixel(x + 1, y, color);
					SetPixel(x + 2, y, color);
					SetPixel(x + 3, y, color);
					SetPixel(x + 4, y, color);
					SetPixel(x + 5, y, color);
					SetPixel(x + 6, y, color);
					SetPixel(x + 7, y, color);
					SetPixel(x + 8, y, color);
					SetPixel(x + 9, y, color);
					SetPixel(x + 10, y, color);
					SetPixel(x + 11, y, color);
					SetPixel(x + 12, y, color);
					SetPixel(x + 13, y, color);
				}
				color = Color7MLores[((x & 0x02) << 3) | (data >> 4)];
				for (int i = 0; i < LoresHeight; i++, y++)
				{
					SetPixel(x + 0, y, color);
					SetPixel(x + 1, y, color);
					SetPixel(x + 2, y, color);
					SetPixel(x + 3, y, color);
					SetPixel(x + 4, y, color);
					SetPixel(x + 5, y, color);
					SetPixel(x + 6, y, color);
					SetPixel(x + 7, y, color);
					SetPixel(x + 8, y, color);
					SetPixel(x + 9, y, color);
					SetPixel(x + 10, y, color);
					SetPixel(x + 11, y, color);
					SetPixel(x + 12, y, color);
					SetPixel(x + 13, y, color);
				}
			}
		}

		private void DrawDLores(int data, int x, int y)
		{
			if (IsMonochrome)
			{
				if ((x & 0x01) == 0x00) // even half cell
				{
					data = ((data << 1) & 0xEE) | ((data >> 3) & 0x11);
				}
				for (int i = 0; i < LoresHeight; i++, y++)
				{
					SetPixel(x + 0, y, data & 0x01);
					SetPixel(x + 1, y, data & 0x02);
					SetPixel(x + 2, y, data & 0x04);
					SetPixel(x + 3, y, data & 0x08);
					SetPixel(x + 4, y, data & 0x01);
					SetPixel(x + 5, y, data & 0x02);
					SetPixel(x + 6, y, data & 0x04);
				}
				for (int i = 0; i < LoresHeight; i++, y++)
				{
					SetPixel(x + 0, y, data & 0x10);
					SetPixel(x + 1, y, data & 0x20);
					SetPixel(x + 2, y, data & 0x40);
					SetPixel(x + 3, y, data & 0x80);
					SetPixel(x + 4, y, data & 0x10);
					SetPixel(x + 5, y, data & 0x20);
					SetPixel(x + 6, y, data & 0x40);
				}
			}
			else
			{
				int color = ColorDLores[((x & 0x01) << 4) | (data & 0x0F)];
				for (int i = 0; i < LoresHeight; i++, y++)
				{
					SetPixel(x + 0, y, color);
					SetPixel(x + 1, y, color);
					SetPixel(x + 2, y, color);
					SetPixel(x + 3, y, color);
					SetPixel(x + 4, y, color);
					SetPixel(x + 5, y, color);
					SetPixel(x + 6, y, color);
				}
				color = ColorDLores[((x & 0x01) << 4) | (data >> 4)];
				for (int i = 0; i < LoresHeight; i++, y++)
				{
					SetPixel(x + 0, y, color);
					SetPixel(x + 1, y, color);
					SetPixel(x + 2, y, color);
					SetPixel(x + 3, y, color);
					SetPixel(x + 4, y, color);
					SetPixel(x + 5, y, color);
					SetPixel(x + 6, y, color);
				}
			}
		}

		private void DrawHires(int address, int x, int y)
		{
			if (IsMonochrome)
			{
				int data = _memory.ReadRamMainRegion02BF(address);
				SetPixel(x + 0, y, data & 0x01);
				SetPixel(x + 1, y, data & 0x01);
				SetPixel(x + 2, y, data & 0x02);
				SetPixel(x + 3, y, data & 0x02);
				SetPixel(x + 4, y, data & 0x04);
				SetPixel(x + 5, y, data & 0x04);
				SetPixel(x + 6, y, data & 0x08);
				SetPixel(x + 7, y, data & 0x08);
				SetPixel(x + 8, y, data & 0x10);
				SetPixel(x + 9, y, data & 0x10);
				SetPixel(x + 10, y, data & 0x20);
				SetPixel(x + 11, y, data & 0x20);
				SetPixel(x + 12, y, data & 0x40);
				SetPixel(x + 13, y, data & 0x40);
			}
			else
			{
				//   3                   2                   1                   0
				// 1 0 9 8 7 6 5 4 3 2 1 0 9 8 7 6 5 4 3 2 1 0 9 8 7 6 5 4 3 2 1 0
				//
				//                 - - - - - - - - 0 0 0 0 0 0 0 0 + + + + + + + +
				//                 H           1 0 H 6 5 4 3 2 1 0 H           1 0

				int data = _memory.ReadRamMainRegion02BF(address) << 8;
				if (x < Width - CellWidth)
				{
					data |= _memory.ReadRamMainRegion02BF(address + 1);
					SetPixel(x + 14, y, ColorHires[((~x & 0x02) << 3) | ((data >> 4) & 0x08) | ((data << 1) & 0x06) | ((data >> 14) & 0x01)]);
					SetPixel(x + 15, y, ColorHires[((~x & 0x02) << 3) | ((data >> 4) & 0x08) | ((data << 1) & 0x06) | ((data >> 14) & 0x01)]);
				}
				if (x > 0)
				{
					data |= _memory.ReadRamMainRegion02BF(address - 1) << 16;
					SetPixel(x - 2, y, ColorHires[((~x & 0x02) << 3) | ((data >> 20) & 0x08) | ((data >> 6) & 0x04) | ((data >> 21) & 0x03)]);
					SetPixel(x - 1, y, ColorHires[((~x & 0x02) << 3) | ((data >> 20) & 0x08) | ((data >> 6) & 0x04) | ((data >> 21) & 0x03)]);
				}
				SetPixel(x + 0, y, ColorHires[((x & 0x02) << 3) | ((data >> 12) & 0x08) | ((data >> 7) & 0x06) | ((data >> 22) & 0x01)]);
				SetPixel(x + 1, y, ColorHires[((x & 0x02) << 3) | ((data >> 12) & 0x08) | ((data >> 7) & 0x06) | ((data >> 22) & 0x01)]);
				SetPixel(x + 2, y, ColorHires[((~x & 0x02) << 3) | ((data >> 12) & 0x08) | ((data >> 8) & 0x07)]);
				SetPixel(x + 3, y, ColorHires[((~x & 0x02) << 3) | ((data >> 12) & 0x08) | ((data >> 8) & 0x07)]);
				SetPixel(x + 4, y, ColorHires[((x & 0x02) << 3) | ((data >> 12) & 0x08) | ((data >> 9) & 0x07)]);
				SetPixel(x + 5, y, ColorHires[((x & 0x02) << 3) | ((data >> 12) & 0x08) | ((data >> 9) & 0x07)]);
				SetPixel(x + 6, y, ColorHires[((~x & 0x02) << 3) | ((data >> 12) & 0x08) | ((data >> 10) & 0x07)]);
				SetPixel(x + 7, y, ColorHires[((~x & 0x02) << 3) | ((data >> 12) & 0x08) | ((data >> 10) & 0x07)]);
				SetPixel(x + 8, y, ColorHires[((x & 0x02) << 3) | ((data >> 12) & 0x08) | ((data >> 11) & 0x07)]);
				SetPixel(x + 9, y, ColorHires[((x & 0x02) << 3) | ((data >> 12) & 0x08) | ((data >> 11) & 0x07)]);
				SetPixel(x + 10, y, ColorHires[((~x & 0x02) << 3) | ((data >> 12) & 0x0F)]);
				SetPixel(x + 11, y, ColorHires[((~x & 0x02) << 3) | ((data >> 12) & 0x0F)]);
				SetPixel(x + 12, y, ColorHires[((x & 0x02) << 3) | ((data >> 12) & 0x08) | ((data << 2) & 0x04) | ((data >> 13) & 0x03)]);
				SetPixel(x + 13, y, ColorHires[((x & 0x02) << 3) | ((data >> 12) & 0x08) | ((data << 2) & 0x04) | ((data >> 13) & 0x03)]);
			}
		}

		private void DrawNDHires(int address, int x, int y)
		{
			if (IsMonochrome)
			{
				int data = _memory.ReadRamMainRegion02BF(address);
				SetPixel(x + 0, y, data & 0x01);
				SetPixel(x + 1, y, data & 0x01);
				SetPixel(x + 2, y, data & 0x02);
				SetPixel(x + 3, y, data & 0x02);
				SetPixel(x + 4, y, data & 0x04);
				SetPixel(x + 5, y, data & 0x04);
				SetPixel(x + 6, y, data & 0x08);
				SetPixel(x + 7, y, data & 0x08);
				SetPixel(x + 8, y, data & 0x10);
				SetPixel(x + 9, y, data & 0x10);
				SetPixel(x + 10, y, data & 0x20);
				SetPixel(x + 11, y, data & 0x20);
				SetPixel(x + 12, y, data & 0x40);
				SetPixel(x + 13, y, data & 0x40);
			}
			else
			{
				//   3                   2                   1                   0
				// 1 0 9 8 7 6 5 4 3 2 1 0 9 8 7 6 5 4 3 2 1 0 9 8 7 6 5 4 3 2 1 0
				//
				//                 - - - - - - - - 0 0 0 0 0 0 0 0 + + + + + + + +
				//                 X           1 0 X 6 5 4 3 2 1 0 X           1 0

				int data = _memory.ReadRamMainRegion02BF(address) << 8;
				if (x < Width - CellWidth)
				{
					data |= _memory.ReadRamMainRegion02BF(address + 1);
					SetPixel(x + 14, y, ColorHires[((~x & 0x02) << 3) | ((data << 1) & 0x06) | ((data >> 14) & 0x01)]);
					SetPixel(x + 15, y, ColorHires[((~x & 0x02) << 3) | ((data << 1) & 0x06) | ((data >> 14) & 0x01)]);
				}
				if (x > 0)
				{
					data |= _memory.ReadRamMainRegion02BF(address - 1) << 16;
					SetPixel(x - 2, y, ColorHires[((~x & 0x02) << 3) | ((data >> 6) & 0x04) | ((data >> 21) & 0x03)]);
					SetPixel(x - 1, y, ColorHires[((~x & 0x02) << 3) | ((data >> 6) & 0x04) | ((data >> 21) & 0x03)]);
				}
				SetPixel(x + 0, y, ColorHires[((x & 0x02) << 3) | ((data >> 7) & 0x06) | ((data >> 22) & 0x01)]);
				SetPixel(x + 1, y, ColorHires[((x & 0x02) << 3) | ((data >> 7) & 0x06) | ((data >> 22) & 0x01)]);
				SetPixel(x + 2, y, ColorHires[((~x & 0x02) << 3) | ((data >> 8) & 0x07)]);
				SetPixel(x + 3, y, ColorHires[((~x & 0x02) << 3) | ((data >> 8) & 0x07)]);
				SetPixel(x + 4, y, ColorHires[((x & 0x02) << 3) | ((data >> 9) & 0x07)]);
				SetPixel(x + 5, y, ColorHires[((x & 0x02) << 3) | ((data >> 9) & 0x07)]);
				SetPixel(x + 6, y, ColorHires[((~x & 0x02) << 3) | ((data >> 10) & 0x07)]);
				SetPixel(x + 7, y, ColorHires[((~x & 0x02) << 3) | ((data >> 10) & 0x07)]);
				SetPixel(x + 8, y, ColorHires[((x & 0x02) << 3) | ((data >> 11) & 0x07)]);
				SetPixel(x + 9, y, ColorHires[((x & 0x02) << 3) | ((data >> 11) & 0x07)]);
				SetPixel(x + 10, y, ColorHires[((~x & 0x02) << 3) | ((data >> 12) & 0x07)]);
				SetPixel(x + 11, y, ColorHires[((~x & 0x02) << 3) | ((data >> 12) & 0x07)]);
				SetPixel(x + 12, y, ColorHires[((x & 0x02) << 3) | ((data << 2) & 0x04) | ((data >> 13) & 0x03)]);
				SetPixel(x + 13, y, ColorHires[((x & 0x02) << 3) | ((data << 2) & 0x04) | ((data >> 13) & 0x03)]);
			}
		}

		private void DrawDHiresA(int address, int x, int y)
		{
			if (IsMonochrome)
			{
				if ((x & 0x2) == 0x00) // even cell
				{
					int data = ((_memory.ReadRamMainRegion02BF(address) << 7) & 0x80) | (_memory.ReadRamAuxRegion02BF(address) & 0x7F);
					SetPixel(x + 0, y, data & 0x01);
					SetPixel(x + 1, y, data & 0x02);
					SetPixel(x + 2, y, data & 0x04);
					SetPixel(x + 3, y, data & 0x08);
					SetPixel(x + 4, y, data & 0x10);
					SetPixel(x + 5, y, data & 0x20);
					SetPixel(x + 6, y, data & 0x40);
					SetPixel(x + 7, y, data & 0x80);
				}
				else
				{
					int data = ((_memory.ReadRamMainRegion02BF(address) << 9) & 0xE00) | ((_memory.ReadRamAuxRegion02BF(address) << 2) & 0x1FC) |
						((_memory.ReadRamMainRegion02BF(address - 1) >> 5) & 0x003);
					SetPixel(x - 2, y, data & 0x01);
					SetPixel(x - 1, y, data & 0x02);
					SetPixel(x + 0, y, data & 0x04);
					SetPixel(x + 1, y, data & 0x08);
					SetPixel(x + 2, y, data & 0x10);
					SetPixel(x + 3, y, data & 0x20);
					SetPixel(x + 4, y, data & 0x40);
					SetPixel(x + 5, y, data & 0x80);
					SetPixel(x + 6, y, (data >> 8) & 0x01);
					SetPixel(x + 7, y, (data >> 8) & 0x02);
					SetPixel(x + 8, y, (data >> 8) & 0x04);
					SetPixel(x + 9, y, (data >> 8) & 0x08);
				}
			}
			else
			{
				if ((x & 0x2) == 0x00) // even cell
				{
					int data = ((_memory.ReadRamMainRegion02BF(address) << 7) & 0x80) | (_memory.ReadRamAuxRegion02BF(address) & 0x7F);
					SetPixel(x + 0, y, ColorDHires0 | (data & 0x0F));
					SetPixel(x + 1, y, ColorDHires0 | (data & 0x0F));
					SetPixel(x + 2, y, ColorDHires0 | (data & 0x0F));
					SetPixel(x + 3, y, ColorDHires0 | (data & 0x0F));
					SetPixel(x + 4, y, ColorDHires0 | (data >> 4));
					SetPixel(x + 5, y, ColorDHires0 | (data >> 4));
					SetPixel(x + 6, y, ColorDHires0 | (data >> 4));
					SetPixel(x + 7, y, ColorDHires0 | (data >> 4));
				}
				else
				{
					int data = ((_memory.ReadRamMainRegion02BF(address) << 9) & 0xE00) | ((_memory.ReadRamAuxRegion02BF(address) << 2) & 0x1FC) |
						((_memory.ReadRamMainRegion02BF(address - 1) >> 5) & 0x003);
					SetPixel(x - 2, y, ColorDHires0 | (data & 0x0F));
					SetPixel(x - 1, y, ColorDHires0 | (data & 0x0F));
					SetPixel(x + 0, y, ColorDHires0 | (data & 0x0F));
					SetPixel(x + 1, y, ColorDHires0 | (data & 0x0F));
					SetPixel(x + 2, y, ColorDHires0 | ((data >> 4) & 0x0F));
					SetPixel(x + 3, y, ColorDHires0 | ((data >> 4) & 0x0F));
					SetPixel(x + 4, y, ColorDHires0 | ((data >> 4) & 0x0F));
					SetPixel(x + 5, y, ColorDHires0 | ((data >> 4) & 0x0F));
					SetPixel(x + 6, y, ColorDHires0 | (data >> 8));
					SetPixel(x + 7, y, ColorDHires0 | (data >> 8));
					SetPixel(x + 8, y, ColorDHires0 | (data >> 8));
					SetPixel(x + 9, y, ColorDHires0 | (data >> 8));
				}
			}
		}

		private void DrawDHiresM(int address, int x, int y)
		{
			if (IsMonochrome)
			{
				if ((x & 0x2) == 0x02) // odd cell
				{
					int data = ((_memory.ReadRamMainRegion02BF(address) << 1) & 0xFE) | ((_memory.ReadRamAuxRegion02BF(address) >> 6) & 0x01);
					SetPixel(x + 6, y, data & 0x01);
					SetPixel(x + 7, y, data & 0x02);
					SetPixel(x + 8, y, data & 0x04);
					SetPixel(x + 9, y, data & 0x08);
					SetPixel(x + 10, y, data & 0x10);
					SetPixel(x + 11, y, data & 0x20);
					SetPixel(x + 12, y, data & 0x40);
					SetPixel(x + 13, y, data & 0x80);
				}
				else
				{
					int data = ((_memory.ReadRamAuxRegion02BF(address + 1) << 10) & 0xC00) | ((_memory.ReadRamMainRegion02BF(address) << 3) & 0x3F8) |
						((_memory.ReadRamAuxRegion02BF(address) >> 4) & 0x007);
					SetPixel(x + 4, y, data & 0x01);
					SetPixel(x + 5, y, data & 0x02);
					SetPixel(x + 6, y, data & 0x04);
					SetPixel(x + 7, y, data & 0x08);
					SetPixel(x + 8, y, data & 0x10);
					SetPixel(x + 9, y, data & 0x20);
					SetPixel(x + 10, y, data & 0x40);
					SetPixel(x + 11, y, data & 0x80);
					SetPixel(x + 12, y, (data >> 8) & 0x01);
					SetPixel(x + 13, y, (data >> 8) & 0x02);
					SetPixel(x + 14, y, (data >> 8) & 0x04);
					SetPixel(x + 15, y, (data >> 8) & 0x08);
				}
			}
			else
			{
				if ((x & 0x2) == 0x02) // odd cell
				{
					int data = ((_memory.ReadRamMainRegion02BF(address) << 1) & 0xFE) | ((_memory.ReadRamAuxRegion02BF(address) >> 6) & 0x01);
					SetPixel(x + 6, y, ColorDHires0 | (data & 0x0F));
					SetPixel(x + 7, y, ColorDHires0 | (data & 0x0F));
					SetPixel(x + 8, y, ColorDHires0 | (data & 0x0F));
					SetPixel(x + 9, y, ColorDHires0 | (data & 0x0F));
					SetPixel(x + 10, y, ColorDHires0 | (data >> 4));
					SetPixel(x + 11, y, ColorDHires0 | (data >> 4));
					SetPixel(x + 12, y, ColorDHires0 | (data >> 4));
					SetPixel(x + 13, y, ColorDHires0 | (data >> 4));
				}
				else
				{
					int data = ((_memory.ReadRamAuxRegion02BF(address + 1) << 10) & 0xC00) | ((_memory.ReadRamMainRegion02BF(address) << 3) & 0x3F8) |
						((_memory.ReadRamAuxRegion02BF(address) >> 4) & 0x007);
					SetPixel(x + 4, y, ColorDHires0 | (data & 0x0F));
					SetPixel(x + 5, y, ColorDHires0 | (data & 0x0F));
					SetPixel(x + 6, y, ColorDHires0 | (data & 0x0F));
					SetPixel(x + 7, y, ColorDHires0 | (data & 0x0F));
					SetPixel(x + 8, y, ColorDHires0 | ((data >> 4) & 0x0F));
					SetPixel(x + 9, y, ColorDHires0 | ((data >> 4) & 0x0F));
					SetPixel(x + 10, y, ColorDHires0 | ((data >> 4) & 0x0F));
					SetPixel(x + 11, y, ColorDHires0 | ((data >> 4) & 0x0F));
					SetPixel(x + 12, y, ColorDHires0 | (data >> 8));
					SetPixel(x + 13, y, ColorDHires0 | (data >> 8));
					SetPixel(x + 14, y, ColorDHires0 | (data >> 8));
					SetPixel(x + 15, y, ColorDHires0 | (data >> 8));
				}
			}
		}

		private void FlushRowMode0(int y)
		{
			int address = (_memory.IsVideoPage2 ? 0x0800 : 0x0400) + AddressOffset[y];
			for (int x = 0; x < CellColumns; x++)
			{
				if (_isCellDirty[CellColumns * y + x])
				{
					_isCellDirty[CellColumns * y + x] = false;
					DrawLores(_memory.ReadRamMainRegion02BF(address + x), CellWidth * x, y); // lores
				}
			}
		}

		private void FlushRowMode1(int y)
		{
			int address = (_memory.IsVideoPage2 ? 0x0800 : 0x0400) + AddressOffset[y];
			for (int x = 0; x < CellColumns; x++)
			{
				if (_isCellDirty[CellColumns * y + x])
				{
					_isCellDirty[CellColumns * y + x] = false;
					DrawText40(_memory.ReadRamMainRegion02BF(address + x), CellWidth * x, y); // text40
				}
			}
		}

		private void FlushRowMode2(int y)
		{
			int address = (_memory.IsVideoPage2 ? 0x0800 : 0x0400) + AddressOffset[y];
			for (int x = 0; x < 2 * CellColumns; x += 2)
			{
				if (_isCellDirty[CellColumns * y + x / 2])
				{
					_isCellDirty[CellColumns * y + x / 2] = false;
					DrawText80(_memory.ReadRamAuxRegion02BF(address + x / 2), CellWidth / 2 * (x + 0), y); // text80
					DrawText80(_memory.ReadRamMainRegion02BF(address + x / 2), CellWidth / 2 * (x + 1), y);
				}
			}
		}

		private void FlushRowMode3(int y)
		{
			if (y < MixedHeight)
			{
				FlushRowMode0(y); // lores
			}
			else
			{
				FlushRowMode1(y); // text40
			}
		}

		private void FlushRowMode4(int y)
		{
			if (y < MixedHeight)
			{
				FlushRowMode0(y); // lores
			}
			else
			{
				FlushRowMode2(y); // text80
			}
		}

		private void FlushRowMode5(int y)
		{
			int address = _memory.IsVideoPage2 ? 0x4000 : 0x2000;
			for (int i = 0; i < CellHeight; i++, y++)
			{
				for (int x = 0; x < CellColumns; x++)
				{
					if (_isCellDirty[CellColumns * y + x])
					{
						_isCellDirty[CellColumns * y + x] = false;
						DrawHires(address + AddressOffset[y] + x, CellWidth * x, y); // hires
					}
				}
			}
		}

		private void FlushRowMode6(int y)
		{
			if (y < MixedHeight)
			{
				FlushRowMode5(y); // hires
			}
			else
			{
				FlushRowMode1(y); // text40
			}
		}

		private void FlushRowMode7(int y)
		{
			if (y < MixedHeight)
			{
				FlushRowMode5(y); // hires
			}
			else
			{
				FlushRowMode2(y); // text80
			}
		}

		private void FlushRowMode8(int y)
		{
			int address = (_memory.IsVideoPage2 ? 0x0800 : 0x0400) + AddressOffset[y];
			for (int x = 0; x < CellColumns; x++)
			{
				if (_isCellDirty[CellColumns * y + x])
				{
					_isCellDirty[CellColumns * y + x] = false;
					Draw7MLores(_memory.ReadRamMainRegion02BF(address + x), CellWidth * x, y); // 7mlores
				}
			}
		}

		private void FlushRowMode9(int y)
		{
			int address = (_memory.IsVideoPage2 ? 0x0800 : 0x0400) + AddressOffset[y];
			for (int x = 0; x < 2 * CellColumns; x += 2)
			{
				if (_isCellDirty[CellColumns * y + x / 2])
				{
					_isCellDirty[CellColumns * y + x / 2] = false;
					DrawDLores(_memory.ReadRamAuxRegion02BF(address + x / 2), CellWidth / 2 * (x + 0), y); // dlores
					DrawDLores(_memory.ReadRamMainRegion02BF(address + x / 2), CellWidth / 2 * (x + 1), y);
				}
			}
		}

		private void FlushRowModeA(int y)
		{
			if (y < MixedHeight)
			{
				FlushRowMode8(y); // 7mlores
			}
			else
			{
				FlushRowMode1(y); // text40
			}
		}

		private void FlushRowModeB(int y)
		{
			if (y < MixedHeight)
			{
				FlushRowMode9(y); // dlores
			}
			else
			{
				FlushRowMode2(y); // text80
			}
		}

		private void FlushRowModeC(int y)
		{
			int address = _memory.IsVideoPage2 ? 0x4000 : 0x2000;
			for (int i = 0; i < CellHeight; i++, y++)
			{
				for (int x = 0; x < CellColumns; x++)
				{
					if (_isCellDirty[CellColumns * y + x])
					{
						_isCellDirty[CellColumns * y + x] = false;
						DrawNDHires(address + AddressOffset[y] + x, CellWidth * x, y); // ndhires
					}
				}
			}
		}

		private void FlushRowModeD(int y)
		{
			int address = _memory.IsVideoPage2 ? 0x4000 : 0x2000;
			for (int i = 0; i < CellHeight; i++, y++)
			{
				for (int x = 0; x < CellColumns; x++)
				{
					if (_isCellDirty[CellColumns * y + x])
					{
						_isCellDirty[CellColumns * y + x] = false;
						DrawDHiresA(address + AddressOffset[y] + x, CellWidth * x, y); // dhires
						DrawDHiresM(address + AddressOffset[y] + x, CellWidth * x, y);
					}
				}
			}
		}

		private void FlushRowModeE(int y)
		{
			if (y < MixedHeight)
			{
				FlushRowModeC(y); // ndhires
			}
			else
			{
				FlushRowMode1(y); // text40
			}
		}

		private void FlushRowModeF(int y)
		{
			if (y < MixedHeight)
			{
				FlushRowModeD(y); // dhires
			}
			else
			{
				FlushRowMode2(y); // text80
			}
		}

		private void FlushRowEvent()
		{
			int y = (_cyclesPerVSync - _cyclesPerVBlankPreset - _events.FindEvent(EventCallbacks.ResetVsync)) / CyclesPerHSync;

			_flushRowMode[_memory.VideoMode](y - CellHeight); // in arrears

			if (y < Height)
			{
				_events.AddEvent(CyclesPerFlush, EventCallbacks.FlushRow);
			}
			else
			{
				_isVBlank = true;
				_events.AddEvent(_cyclesPerVBlank, EventCallbacks.LeaveVBlank);
			}
		}

		private void FlushScreen()
		{
			var flushRowMode = _flushRowMode[_memory.VideoMode];
			for (int y = 0; y < Height; y += CellHeight)
			{
				flushRowMode(y);
			}
		}

		private void InverseTextEvent()
		{
			_isTextInversed = !_isTextInversed;
			DirtyScreenText();
			_events.AddEvent(_cyclesPerFlash, EventCallbacks.InverseText);
		}

		private void LeaveVBlankEvent()
		{
			_isVBlank = false;
			_events.AddEvent(CyclesPerFlush, EventCallbacks.FlushRow);
		}

		private void ResetVSyncEvent()
		{
			_events.AddEvent(_cyclesPerVSync, EventCallbacks.ResetVsync);
		}

		private void SetPalette()
		{
			_colorPalette[ColorMono00] = _colorBlack;
			_colorPalette[ColorMono01] = _colorMonochrome;
			_colorPalette[ColorMono02] = _colorMonochrome;
			_colorPalette[ColorMono04] = _colorMonochrome;
			_colorPalette[ColorMono08] = _colorMonochrome;
			_colorPalette[ColorMono10] = _colorMonochrome;
			_colorPalette[ColorMono20] = _colorMonochrome;
			_colorPalette[ColorMono40] = _colorMonochrome;
			_colorPalette[ColorMono80] = _colorMonochrome;

			_colorPalette[ColorWhite00] = _colorBlack;
			_colorPalette[ColorWhite01] = _colorWhite;
			_colorPalette[ColorWhite02] = _colorWhite;
			_colorPalette[ColorWhite04] = _colorWhite;
			_colorPalette[ColorWhite08] = _colorWhite;
			_colorPalette[ColorWhite10] = _colorWhite;
			_colorPalette[ColorWhite20] = _colorWhite;
			_colorPalette[ColorWhite40] = _colorWhite;
			_colorPalette[ColorWhite80] = _colorWhite;

			_colorPalette[ColorDHires0] = _colorBlack;
			_colorPalette[ColorDHires1] = _colorDarkBlue;
			_colorPalette[ColorDHires2] = _colorDarkGreen;
			_colorPalette[ColorDHires3] = _colorMediumBlue;
			_colorPalette[ColorDHires4] = _colorBrown;
			_colorPalette[ColorDHires5] = _colorLightGrey;
			_colorPalette[ColorDHires6] = _colorGreen;
			_colorPalette[ColorDHires7] = _colorAquamarine;
			_colorPalette[ColorDHires8] = _colorDeepRed;
			_colorPalette[ColorDHires9] = _colorPurple;
			_colorPalette[ColorDHiresA] = _colorDarkGrey;
			_colorPalette[ColorDHiresB] = _colorLightBlue;
			_colorPalette[ColorDHiresC] = _colorOrange;
			_colorPalette[ColorDHiresD] = _colorPink;
			_colorPalette[ColorDHiresE] = _colorYellow;
			_colorPalette[ColorDHiresF] = _colorWhite;

			DirtyScreen();
		}

		private void SetPixel(int x, int y, int color)
		{
			int i = 560 * (2 * y) + x;
			_framebuffer[i] = _framebuffer[i + 560] = _colorPalette[color];
		}

		private void SetScanner()
		{
			if ((_scannerOptions & ScannerOptions.Pal) != 0)
			{
				_vCountPreset = VCountPresetPal;
				_vLineLeaveVBlank = VLineLeaveVBlankPal;
			}
			else
			{
				_vCountPreset = VCountPresetNtsc;
				_vLineLeaveVBlank = VLineLeaveVBlankNtsc;
			}

			_cyclesPerVBlank = (_vLineLeaveVBlank - VLineEnterVBlank) * CyclesPerHSync;
			_cyclesPerVBlankPreset = (_vLineLeaveVBlank - VLineTriggerPreset) * CyclesPerHSync; // cycles during vblank after vcount preset [3-15, 3-16]
			_cyclesPerVSync = _vLineLeaveVBlank * CyclesPerHSync;
			_cyclesPerFlash = VSyncsPerFlash * _cyclesPerVSync;
		}

		public bool IsMonochrome
		{
			get => _isMonochrome;
			set { _isMonochrome = value; DirtyScreen(); }
		}

		internal ScannerOptions ScannerOptions
		{
			get => _scannerOptions;
			set { _scannerOptions = value; SetScanner(); }
		}

		public bool IsVBlank => _isVBlank;

		private readonly int _colorBlack;
		private readonly int _colorDarkBlue;
		private readonly int _colorDarkGreen;
		private readonly int _colorMediumBlue;
		private readonly int _colorBrown;
		private readonly int _colorLightGrey;
		private readonly int _colorGreen;
		private readonly int _colorAquamarine;
		private readonly int _colorDeepRed;
		private readonly int _colorPurple;
		private readonly int _colorDarkGrey;
		private readonly int _colorLightBlue;
		private readonly int _colorOrange;
		private readonly int _colorPink;
		private readonly int _colorYellow;
		private readonly int _colorWhite;
		private readonly int _colorMonochrome;
	}
}
