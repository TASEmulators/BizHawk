﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace Jellyfish.Virtu
{
	// ReSharper disable once UnusedMember.Global
	public enum MonitorType { Unknown, Standard, Enhanced }

	public interface IMemoryBus
	{
		// ReSharper disable once UnusedMemberInSuper.Global
		bool Lagged { get; }

		int ReadRomRegionE0FF(int address);
		int ReadRamMainRegion02BF(int address);
		int ReadRamAuxRegion02BF(int address);

		int Read(int address);
		int Peek(int address);
		int ReadOpcode(int address);
		int ReadZeroPage(int address);
		void WriteZeroPage(int address, int data);
		void Write(int address, int data);

		bool IsText { get; }
		bool IsMixed { get; }
		bool IsHires { get; }
		bool IsVideoPage2 { get; }
		bool IsCharSetAlternate { get; }

		int VideoMode { get; }
		MonitorType Monitor { get; }

		// ReSharper disable once UnusedMember.Global
		void Sync(IComponentSerializer ser);
	}

	// ReSharper disable once UnusedMember.Global
	public sealed partial class Memory : IMemoryBus
	{
		private IGamePort _gamePort;
		private ICassette _cassette;
		private IVideo _video;
		private ISlotClock _noSlotClock;

		private IPeripheralCard _slot1;
		private IPeripheralCard _slot2;
		private IPeripheralCard _slot3;
		private IPeripheralCard _slot4;
		private IPeripheralCard _slot5;
		private IPeripheralCard _slot7;

		private readonly byte[] _appleIIe;
		private readonly byte[][] _regionRead = new byte[RegionCount][];
		private readonly byte[][] _regionWrite = new byte[RegionCount][];
		private readonly Action<int, byte>[] _writeRegion = new Action<int, byte>[RegionCount];

		private bool _lagged;
		private int _state;
		private int _slotRegionC8CF;
		private byte[] _zeroPage;
		private byte[] _ramMainRegion0001 = new byte[0x0200];
		private byte[] _ramMainRegion02BF = new byte[0xBE00];
		private byte[] _ramMainBank1RegionD0DF = new byte[0x1000];
		private byte[] _ramMainBank2RegionD0DF = new byte[0x1000];
		private byte[] _ramMainRegionE0FF = new byte[0x2000];
		private byte[] _ramAuxRegion0001 = new byte[0x0200];
		private byte[] _ramAuxRegion02BF = new byte[0xBE00];
		private byte[] _ramAuxBank1RegionD0DF = new byte[0x1000];
		private byte[] _ramAuxBank2RegionD0DF = new byte[0x1000];
		private byte[] _ramAuxRegionE0FF = new byte[0x2000];

		private byte[] _romExternalRegionC1CF = new byte[0x0F00];
		private byte[] _romInternalRegionC1CF = new byte[0x0F00];
		private byte[] _romRegionD0DF = new byte[0x1000];
		private byte[] _romRegionE0FF = new byte[0x2000];

		// ReSharper disable once UnusedMember.Global
		public Memory(byte[] appleIIe)
		{
			_appleIIe = appleIIe;
			InitializeWriteDelegates();
		}

		public void Sync(IComponentSerializer ser)
		{
			ser.Sync(nameof(_lagged), ref _lagged);
			ser.Sync(nameof(_state), ref _state);
			ser.Sync(nameof(_slotRegionC8CF), ref _slotRegionC8CF);
			ser.Sync(nameof(_zeroPage), ref _zeroPage, false);

			ser.Sync(nameof(_ramMainRegion0001), ref _ramMainRegion0001, false);
			ser.Sync(nameof(_ramMainRegion02BF), ref _ramMainRegion02BF, false);
			ser.Sync(nameof(_ramMainBank1RegionD0DF), ref _ramMainBank1RegionD0DF, false);
			ser.Sync(nameof(_ramMainBank2RegionD0DF), ref _ramMainBank2RegionD0DF, false);
			ser.Sync(nameof(_ramMainRegionE0FF), ref _ramMainRegionE0FF, false);
			ser.Sync(nameof(_ramAuxRegion0001), ref _ramAuxRegion0001, false);
			ser.Sync(nameof(_ramAuxRegion02BF), ref _ramAuxRegion02BF, false);
			ser.Sync(nameof(_ramAuxBank1RegionD0DF), ref _ramAuxBank1RegionD0DF, false);
			ser.Sync(nameof(_ramAuxBank2RegionD0DF), ref _ramAuxBank2RegionD0DF, false);
			ser.Sync(nameof(_ramAuxRegionE0FF), ref _ramAuxRegionE0FF, false);
			ser.Sync(nameof(_romExternalRegionC1CF), ref _romExternalRegionC1CF, false);
			ser.Sync(nameof(_romInternalRegionC1CF), ref _romInternalRegionC1CF, false);
			ser.Sync(nameof(_romRegionD0DF), ref _romRegionD0DF, false);
			ser.Sync(nameof(_romRegionE0FF), ref _romRegionE0FF, false);

			if (ser.IsReader)
			{
				MapRegion0001();
				MapRegion02BF();
				MapRegionC0CF();
				MapRegionD0FF();
			}
		}

		// ReSharper disable once UnusedMember.Global
		public void Initialize(
			Keyboard keyboard,
			IGamePort gamePort,
			ICassette cassette,
			ISpeaker speaker,
			IVideo video,
			ISlotClock noSlotClock,
			IPeripheralCard slot1,
			IPeripheralCard slot2,
			IPeripheralCard slot3,
			IPeripheralCard slot4,
			IPeripheralCard slot5,
			IDiskIIController slot6,
			IPeripheralCard slot7)
		{
			Keyboard = keyboard;
			_gamePort = gamePort;
			_cassette = cassette;
			Speaker = speaker;
			_video = video;
			_noSlotClock = noSlotClock;

			_slot1 = slot1;
			_slot2 = slot2;
			_slot3 = slot3;
			_slot4 = slot4;
			_slot5 = slot5;
			DiskIIController = slot6;
			_slot7 = slot7;

			// TODO: this is a lazy and more complicated way to do this
			_romInternalRegionC1CF = _appleIIe
				.Skip(0x100)
				.Take(_romInternalRegionC1CF.Length)
				.ToArray();

			_romRegionD0DF = _appleIIe
				.Skip(0x100 + _romInternalRegionC1CF.Length)
				.Take(_romRegionD0DF.Length)
				.ToArray();

			_romRegionE0FF = _appleIIe
				.Skip(0x100 + _romInternalRegionC1CF.Length + _romRegionD0DF.Length)
				.Take(_romRegionE0FF.Length)
				.ToArray();

			if (ReadRomRegionE0FF(0xFBB3) == 0x06 && ReadRomRegionE0FF(0xFBBF) == 0xC1)
			{
				Monitor = MonitorType.Standard;
			}
			else if (ReadRomRegionE0FF(0xFBB3) == 0x06
				&& ReadRomRegionE0FF(0xFBBF) == 0x00
				&& ReadRomRegionE0FF(0xFBC0) == 0xE0)
			{
				Monitor = MonitorType.Enhanced;
			}
		}

		public bool Lagged { get => _lagged; set => _lagged = value; }

		private IList<IPeripheralCard> Slots => new List<IPeripheralCard>
		{
			null,
			_slot1,
			_slot2,
			_slot3,
			_slot4,
			_slot5,
			DiskIIController,
			_slot7
		};

		public IDiskIIController DiskIIController { get; private set; }

		public Keyboard Keyboard { get; private set; }

		public ISpeaker Speaker { get; private set; }

		private void InitializeWriteDelegates()
		{
			WriteRamModeBankRegion = new Action<int, byte>[Video.ModeCount][][];
			for (int mode = 0; mode < Video.ModeCount; mode++)
			{
				WriteRamModeBankRegion[mode] = new[]
				{
					new Action<int, byte>[RegionCount], new Action<int, byte>[RegionCount]
				};
			}
			WriteRamModeBankRegion[Video.Mode0][BankMain][Region0407] = WriteRamMode0MainRegion0407;
			WriteRamModeBankRegion[Video.Mode0][BankMain][Region080B] = WriteRamMode0MainRegion080B;
			WriteRamModeBankRegion[Video.Mode1][BankMain][Region0407] = WriteRamMode1MainRegion0407;
			WriteRamModeBankRegion[Video.Mode1][BankMain][Region080B] = WriteRamMode1MainRegion080B;
			WriteRamModeBankRegion[Video.Mode2][BankMain][Region0407] = WriteRamMode2MainRegion0407;
			WriteRamModeBankRegion[Video.Mode2][BankMain][Region080B] = WriteRamMode2MainRegion080B;
			WriteRamModeBankRegion[Video.Mode2][BankAux][Region0407] = WriteRamMode2AuxRegion0407;
			WriteRamModeBankRegion[Video.Mode2][BankAux][Region080B] = WriteRamMode2AuxRegion080B;
			WriteRamModeBankRegion[Video.Mode3][BankMain][Region0407] = WriteRamMode3MainRegion0407;
			WriteRamModeBankRegion[Video.Mode3][BankMain][Region080B] = WriteRamMode3MainRegion080B;
			WriteRamModeBankRegion[Video.Mode4][BankMain][Region0407] = WriteRamMode4MainRegion0407;
			WriteRamModeBankRegion[Video.Mode4][BankMain][Region080B] = WriteRamMode4MainRegion080B;
			WriteRamModeBankRegion[Video.Mode4][BankAux][Region0407] = WriteRamMode4AuxRegion0407;
			WriteRamModeBankRegion[Video.Mode4][BankAux][Region080B] = WriteRamMode4AuxRegion080B;
			WriteRamModeBankRegion[Video.Mode5][BankMain][Region203F] = WriteRamMode5MainRegion203F;
			WriteRamModeBankRegion[Video.Mode5][BankMain][Region405F] = WriteRamMode5MainRegion405F;
			WriteRamModeBankRegion[Video.Mode6][BankMain][Region0407] = WriteRamMode6MainRegion0407;
			WriteRamModeBankRegion[Video.Mode6][BankMain][Region080B] = WriteRamMode6MainRegion080B;
			WriteRamModeBankRegion[Video.Mode6][BankMain][Region203F] = WriteRamMode6MainRegion203F;
			WriteRamModeBankRegion[Video.Mode6][BankMain][Region405F] = WriteRamMode6MainRegion405F;
			WriteRamModeBankRegion[Video.Mode7][BankMain][Region0407] = WriteRamMode7MainRegion0407;
			WriteRamModeBankRegion[Video.Mode7][BankMain][Region080B] = WriteRamMode7MainRegion080B;
			WriteRamModeBankRegion[Video.Mode7][BankMain][Region203F] = WriteRamMode7MainRegion203F;
			WriteRamModeBankRegion[Video.Mode7][BankMain][Region405F] = WriteRamMode7MainRegion405F;
			WriteRamModeBankRegion[Video.Mode7][BankAux][Region0407] = WriteRamMode7AuxRegion0407;
			WriteRamModeBankRegion[Video.Mode7][BankAux][Region080B] = WriteRamMode7AuxRegion080B;
			WriteRamModeBankRegion[Video.Mode8][BankMain][Region0407] = WriteRamMode8MainRegion0407;
			WriteRamModeBankRegion[Video.Mode8][BankMain][Region080B] = WriteRamMode8MainRegion080B;
			WriteRamModeBankRegion[Video.Mode9][BankMain][Region0407] = WriteRamMode9MainRegion0407;
			WriteRamModeBankRegion[Video.Mode9][BankMain][Region080B] = WriteRamMode9MainRegion080B;
			WriteRamModeBankRegion[Video.Mode9][BankAux][Region0407] = WriteRamMode9AuxRegion0407;
			WriteRamModeBankRegion[Video.Mode9][BankAux][Region080B] = WriteRamMode9AuxRegion080B;
			WriteRamModeBankRegion[Video.ModeA][BankMain][Region0407] = WriteRamModeAMainRegion0407;
			WriteRamModeBankRegion[Video.ModeA][BankMain][Region080B] = WriteRamModeAMainRegion080B;
			WriteRamModeBankRegion[Video.ModeB][BankMain][Region0407] = WriteRamModeBMainRegion0407;
			WriteRamModeBankRegion[Video.ModeB][BankMain][Region080B] = WriteRamModeBMainRegion080B;
			WriteRamModeBankRegion[Video.ModeB][BankAux][Region0407] = WriteRamModeBAuxRegion0407;
			WriteRamModeBankRegion[Video.ModeB][BankAux][Region080B] = WriteRamModeBAuxRegion080B;
			WriteRamModeBankRegion[Video.ModeC][BankMain][Region203F] = WriteRamModeCMainRegion203F;
			WriteRamModeBankRegion[Video.ModeC][BankMain][Region405F] = WriteRamModeCMainRegion405F;
			WriteRamModeBankRegion[Video.ModeD][BankMain][Region203F] = WriteRamModeDMainRegion203F;
			WriteRamModeBankRegion[Video.ModeD][BankMain][Region405F] = WriteRamModeDMainRegion405F;
			WriteRamModeBankRegion[Video.ModeD][BankAux][Region203F] = WriteRamModeDAuxRegion203F;
			WriteRamModeBankRegion[Video.ModeD][BankAux][Region405F] = WriteRamModeDAuxRegion405F;
			WriteRamModeBankRegion[Video.ModeE][BankMain][Region0407] = WriteRamModeEMainRegion0407;
			WriteRamModeBankRegion[Video.ModeE][BankMain][Region080B] = WriteRamModeEMainRegion080B;
			WriteRamModeBankRegion[Video.ModeE][BankMain][Region203F] = WriteRamModeEMainRegion203F;
			WriteRamModeBankRegion[Video.ModeE][BankMain][Region405F] = WriteRamModeEMainRegion405F;
			WriteRamModeBankRegion[Video.ModeF][BankMain][Region0407] = WriteRamModeFMainRegion0407;
			WriteRamModeBankRegion[Video.ModeF][BankMain][Region080B] = WriteRamModeFMainRegion080B;
			WriteRamModeBankRegion[Video.ModeF][BankMain][Region203F] = WriteRamModeFMainRegion203F;
			WriteRamModeBankRegion[Video.ModeF][BankMain][Region405F] = WriteRamModeFMainRegion405F;
			WriteRamModeBankRegion[Video.ModeF][BankAux][Region0407] = WriteRamModeFAuxRegion0407;
			WriteRamModeBankRegion[Video.ModeF][BankAux][Region080B] = WriteRamModeFAuxRegion080B;
			WriteRamModeBankRegion[Video.ModeF][BankAux][Region203F] = WriteRamModeFAuxRegion203F;
			WriteRamModeBankRegion[Video.ModeF][BankAux][Region405F] = WriteRamModeFAuxRegion405F;

			_writeIoRegionC0C0 = WriteIoRegionC0C0; // cache delegates; avoids garbage
			_writeIoRegionC1C7 = WriteIoRegionC1C7;
			_writeIoRegionC3C3 = WriteIoRegionC3C3;
			_writeIoRegionC8CF = WriteIoRegionC8CF;
			_writeRomRegionD0FF = WriteRomRegionD0FF;
		}

		// ReSharper disable once UnusedMember.Global
		public void Reset() // [7-3]
		{
			ResetState(State80Col | State80Store | StateAltChrSet | StateAltZP | StateBank1 | StateHRamRd | StateHRamPreWrt | StateHRamWrt | // HRamWrt' [5-23]
				StateHires | StatePage2 | StateRamRd | StateRamWrt | StateIntCXRom | StateSlotC3Rom | StateIntC8Rom | StateAn0 | StateAn1 | StateAn2 | StateAn3);
			SetState(StateDRes); // An3' -> DRes [8-20]

			MapRegion0001();
			MapRegion02BF();
			MapRegionC0CF();
			MapRegionD0FF();
		}

		public int ReadOpcode(int address)
		{
			int region = PageRegion[address >> 8];
			var result = ((address & 0xF000) != 0xC000) ? _regionRead[region][address - RegionBaseAddress[region]] : ReadIoRegionC0CF(address);
			ExecuteCallback?.Invoke((uint)address);
			ReadCallback?.Invoke((uint)address);
			return result;
		}

		public int Read(int address)
		{
			int region = PageRegion[address >> 8];
			var result = (address & 0xF000) != 0xC000
				? _regionRead[region][address - RegionBaseAddress[region]]
				: ReadIoRegionC0CF(address);
			ReadCallback?.Invoke((uint)address);
			return result;
		}

		public int Peek(int address)
		{
			int region = PageRegion[address >> 8];
			return (address & 0xF000) != 0xC000
				? _regionRead[region][address - RegionBaseAddress[region]]
				: ReadIoRegionC0CF(address);
		}

		public int ReadZeroPage(int address)
		{
			ReadCallback?.Invoke((uint)address);
			return _zeroPage[address];
		}

		public void Write(int address, int data)
		{
			int region = PageRegion[address >> 8];
			if (_writeRegion[region] == null)
			{
				WriteCallback?.Invoke((uint)address);
				_regionWrite[region][address - RegionBaseAddress[region]] = (byte)data;
			}
			else
			{
				WriteCallback?.Invoke((uint)address);
				_writeRegion[region](address, (byte)data);
			}
		}

		public void WriteZeroPage(int address, int data)
		{
			WriteCallback?.Invoke((uint)address);
			_zeroPage[address] = (byte)data;
		}

		public byte PeekMainRam(int address)
		{
			return address switch
			{
				>= 0 and < 0x0200 => _ramMainRegion0001[address - 0],
				>= 0x0200 and < 0xC000 => _ramMainRegion02BF[address - 0x0200],
				>= 0xC000 and < 0xD000 => _ramMainBank1RegionD0DF[address - 0xC000],
				>= 0xD000 and < 0xE000 => _ramMainBank2RegionD0DF[address - 0xD000],
				>= 0xE000 and < 0x10000 => _ramMainRegionE0FF[address - 0xE000],
				_ => throw new InvalidOperationException($"{nameof(address)} out of range ({address})")
			};
		}

		public void PokeMainRam(int address, byte value)
		{
			switch (address)
			{
				case >= 0 and < 0x0200:
					_ramMainRegion0001[address - 0] = value;
					break;
				case >= 0x0200 and < 0xC000:
					_ramMainRegion02BF[address - 0x0200] = value;
					break;
				case >= 0xC000 and < 0xD000:
					_ramMainBank1RegionD0DF[address - 0xC000] = value;
					break;
				case >= 0xD000 and < 0xE000:
					_ramMainBank2RegionD0DF[address - 0xD000] = value;
					break;
				case >= 0xE000 and < 0x10000:
					_ramMainRegionE0FF[address - 0xE000] = value;
					break;
				default:
					throw new InvalidOperationException($"{nameof(address)} out of range ({address})");
			}
		}

		public byte PeekAuxRam(int address)
		{
			return address switch
			{
				>= 0 and < 0x0200 => _ramAuxRegion0001[address - 0],
				>= 0x0200 and < 0xC000 => _ramAuxRegion02BF[address - 0x0200],
				>= 0xC000 and < 0xD000 => _ramAuxBank1RegionD0DF[address - 0xC000],
				>= 0xD000 and < 0xE000 => _ramAuxBank2RegionD0DF[address - 0xD000],
				>= 0xE000 and < 0x10000 => _ramAuxRegionE0FF[address - 0xE000],
				_ => throw new InvalidOperationException($"{nameof(address)} out of range ({address})")
			};
		}

		public void PokeAuxRam(int address, byte value)
		{
			switch (address)
			{
				case >= 0 and < 0x0200:
					_ramAuxRegion0001[address - 0] = value;
					break;
				case >= 0x0200 and < 0xC000:
					_ramAuxRegion02BF[address - 0x0200] = value;
					break;
				case >= 0xC000 and < 0xD000:
					_ramAuxBank1RegionD0DF[address - 0xC000] = value;
					break;
				case >= 0xD000 and < 0xE000:
					_ramAuxBank2RegionD0DF[address - 0xD000] = value;
					break;
				case >= 0xE000 and < 0x10000:
					_ramAuxRegionE0FF[address - 0xE000] = value;
					break;
				default:
					throw new InvalidOperationException($"{nameof(address)} out of range ({address})");
			}
		}

		private int ReadIoRegionC0CF(int address)
		{
			switch (address & 0xFF00)
			{
				case 0xC000:
					return ReadIoRegionC0C0(address);

				case 0xC100:
				case 0xC200:
				case 0xC400:
				case 0xC500:
				case 0xC600:
				case 0xC700:
					return ReadIoRegionC1C7(address);

				case 0xC300:
					return ReadIoRegionC3C3(address);

				case 0xC800:
				case 0xC900:
				case 0xCA00:
				case 0xCB00:
				case 0xCC00:
				case 0xCD00:
				case 0xCE00:
				case 0xCF00:
					return ReadIoRegionC8CF(address);
			}

			return _video.ReadFloatingBus();
		}

		private int ReadIoRegionC0C0(int address)
		{
			if (0xC000 <= address && address <= 0xC00F || 0xC061 <= address && address <= 0xC067 || 0xC069 <= address && address <= 0xC06F)
			{
				Lagged = false;
				InputCallback?.Invoke();
			}

			switch (address)
			{
				case 0xC000:
				case 0xC001:
				case 0xC002:
				case 0xC003:
				case 0xC004:
				case 0xC005:
				case 0xC006:
				case 0xC007: // [7-15]
				case 0xC008:
				case 0xC009:
				case 0xC00A:
				case 0xC00B:
				case 0xC00C:
				case 0xC00D:
				case 0xC00E:
				case 0xC00F:
					return SetBit7(Keyboard.Latch, Keyboard.Strobe);

				case 0xC010:
					Keyboard.ResetStrobe();
					return SetBit7(Keyboard.Latch, Keyboard.IsAnyKeyDown);

				case 0xC011:
					return SetBit7(Keyboard.Latch, !IsHighRamBank1); // Bank1' [5-22]

				case 0xC012:
					return SetBit7(Keyboard.Latch, IsHighRamRead);

				case 0xC013:
					return SetBit7(Keyboard.Latch, IsRamReadAux);

				case 0xC014:
					return SetBit7(Keyboard.Latch, IsRamWriteAux);

				case 0xC015:
					return SetBit7(Keyboard.Latch, IsRomC1CFInternal);

				case 0xC016:
					return SetBit7(Keyboard.Latch, IsZeroPageAux);

				case 0xC017:
					return SetBit7(Keyboard.Latch, IsRomC3C3External);

				case 0xC018:
					return SetBit7(Keyboard.Latch, Is80Store);

				case 0xC019:
					return SetBit7(Keyboard.Latch, !_video.IsVBlank); // Vbl' [7-5]

				case 0xC01A:
					return SetBit7(Keyboard.Latch, IsText);

				case 0xC01B:
					return SetBit7(Keyboard.Latch, IsMixed);

				case 0xC01C:
					return SetBit7(Keyboard.Latch, IsPage2);

				case 0xC01D:
					return SetBit7(Keyboard.Latch, IsHires);

				case 0xC01E:
					return SetBit7(Keyboard.Latch, IsCharSetAlternate);

				case 0xC01F:
					return SetBit7(Keyboard.Latch, Is80Columns);

				case 0xC020:
				case 0xC021:
				case 0xC022:
				case 0xC023:
				case 0xC024:
				case 0xC025:
				case 0xC026:
				case 0xC027: // [7-8]
				case 0xC028:
				case 0xC029:
				case 0xC02A:
				case 0xC02B:
				case 0xC02C:
				case 0xC02D:
				case 0xC02E:
				case 0xC02F:
					_cassette.ToggleOutput();
					break;

				case 0xC030:
				case 0xC031:
				case 0xC032:
				case 0xC033:
				case 0xC034:
				case 0xC035:
				case 0xC036:
				case 0xC037: // [7-9]
				case 0xC038:
				case 0xC039:
				case 0xC03A:
				case 0xC03B:
				case 0xC03C:
				case 0xC03D:
				case 0xC03E:
				case 0xC03F:
					Speaker.ToggleOutput();
					break;

				case 0xC040:
				case 0xC041:
				case 0xC042:
				case 0xC043:
				case 0xC044:
				case 0xC045:
				case 0xC046:
				case 0xC047: // [2-18]
				case 0xC048:
				case 0xC049:
				case 0xC04A:
				case 0xC04B:
				case 0xC04C:
				case 0xC04D:
				case 0xC04E:
				case 0xC04F:
					break;

				case 0xC050:
				case 0xC051:
					SetText(TestBit(address, 0));
					break;

				case 0xC052:
				case 0xC053:
					SetMixed(TestBit(address, 0));
					break;

				case 0xC054:
				case 0xC055:
					SetPage2(TestBit(address, 0));
					break;

				case 0xC056:
				case 0xC057:
					SetHires(TestBit(address, 0));
					break;

				case 0xC058:
				case 0xC059:
					SetAnnunciator0(TestBit(address, 0));
					break;

				case 0xC05A:
				case 0xC05B:
					SetAnnunciator1(TestBit(address, 0));
					break;

				case 0xC05C:
				case 0xC05D:
					SetAnnunciator2(TestBit(address, 0));
					break;

				case 0xC05E:
				case 0xC05F:
					SetAnnunciator3(TestBit(address, 0));
					SetDoubleRes(!TestBit(address, 0));
					break;

				case 0xC060:
				case 0xC068: // [2-18, 7-5]
					return SetBit7(_video.ReadFloatingBus(), _cassette.ReadInput()); // [7-8]

				case 0xC061:
				case 0xC069:
					return SetBit7(_video.ReadFloatingBus(), _gamePort.ReadButton0());

				case 0xC062:
				case 0xC06A:
					return SetBit7(_video.ReadFloatingBus(), _gamePort.ReadButton1());

				case 0xC063:
				case 0xC06B:
					return SetBit7(_video.ReadFloatingBus(), _gamePort.ReadButton2());

				case 0xC064:
				case 0xC06C:
					return SetBit7(_video.ReadFloatingBus(), _gamePort.Paddle0Strobe);

				case 0xC065:
				case 0xC06D:
					return SetBit7(_video.ReadFloatingBus(), _gamePort.Paddle1Strobe);

				case 0xC066:
				case 0xC06E:
					return SetBit7(_video.ReadFloatingBus(), _gamePort.Paddle2Strobe);

				case 0xC067:
				case 0xC06F:
					return SetBit7(_video.ReadFloatingBus(), _gamePort.Paddle3Strobe);

				case 0xC070:
				case 0xC071:
				case 0xC072:
				case 0xC073:
				case 0xC074:
				case 0xC075:
				case 0xC076:
				case 0xC077:
				case 0xC078:
				case 0xC079:
				case 0xC07A:
				case 0xC07B:
				case 0xC07C:
				case 0xC07D:
				case 0xC07E:
				case 0xC07F:
					_gamePort.TriggerTimers();
					break;

				case 0xC080:
				case 0xC081:
				case 0xC082:
				case 0xC083:
				case 0xC084:
				case 0xC085:
				case 0xC086:
				case 0xC087: // slot0 [5-23]
				case 0xC088:
				case 0xC089:
				case 0xC08A:
				case 0xC08B:
				case 0xC08C:
				case 0xC08D:
				case 0xC08E:
				case 0xC08F:
					SetHighRam(address, true);
					break;

				case 0xC090:
				case 0xC091:
				case 0xC092:
				case 0xC093:
				case 0xC094:
				case 0xC095:
				case 0xC096:
				case 0xC097: // slot1
				case 0xC098:
				case 0xC099:
				case 0xC09A:
				case 0xC09B:
				case 0xC09C:
				case 0xC09D:
				case 0xC09E:
				case 0xC09F:
					return _slot1.ReadIoRegionC0C0(address);

				case 0xC0A0:
				case 0xC0A1:
				case 0xC0A2:
				case 0xC0A3:
				case 0xC0A4:
				case 0xC0A5:
				case 0xC0A6:
				case 0xC0A7: // slot2
				case 0xC0A8:
				case 0xC0A9:
				case 0xC0AA:
				case 0xC0AB:
				case 0xC0AC:
				case 0xC0AD:
				case 0xC0AE:
				case 0xC0AF:
					return _slot2.ReadIoRegionC0C0(address);

				case 0xC0B0:
				case 0xC0B1:
				case 0xC0B2:
				case 0xC0B3:
				case 0xC0B4:
				case 0xC0B5:
				case 0xC0B6:
				case 0xC0B7: // slot3
				case 0xC0B8:
				case 0xC0B9:
				case 0xC0BA:
				case 0xC0BB:
				case 0xC0BC:
				case 0xC0BD:
				case 0xC0BE:
				case 0xC0BF:
					return _slot3.ReadIoRegionC0C0(address);

				case 0xC0C0:
				case 0xC0C1:
				case 0xC0C2:
				case 0xC0C3:
				case 0xC0C4:
				case 0xC0C5:
				case 0xC0C6:
				case 0xC0C7: // slot4
				case 0xC0C8:
				case 0xC0C9:
				case 0xC0CA:
				case 0xC0CB:
				case 0xC0CC:
				case 0xC0CD:
				case 0xC0CE:
				case 0xC0CF:
					return _slot4.ReadIoRegionC0C0(address);

				case 0xC0D0:
				case 0xC0D1:
				case 0xC0D2:
				case 0xC0D3:
				case 0xC0D4:
				case 0xC0D5:
				case 0xC0D6:
				case 0xC0D7: // slot5
				case 0xC0D8:
				case 0xC0D9:
				case 0xC0DA:
				case 0xC0DB:
				case 0xC0DC:
				case 0xC0DD:
				case 0xC0DE:
				case 0xC0DF:
					return _slot5.ReadIoRegionC0C0(address);

				case 0xC0E0:
				case 0xC0E1:
				case 0xC0E2:
				case 0xC0E3:
				case 0xC0E4:
				case 0xC0E5:
				case 0xC0E6:
				case 0xC0E7: // slot6
				case 0xC0E8:
				case 0xC0E9:
				case 0xC0EA:
				case 0xC0EB:
				case 0xC0EC:
				case 0xC0ED:
				case 0xC0EE:
				case 0xC0EF:
					return DiskIIController.ReadIoRegionC0C0(address);

				case 0xC0F0:
				case 0xC0F1:
				case 0xC0F2:
				case 0xC0F3:
				case 0xC0F4:
				case 0xC0F5:
				case 0xC0F6:
				case 0xC0F7: // slot7
				case 0xC0F8:
				case 0xC0F9:
				case 0xC0FA:
				case 0xC0FB:
				case 0xC0FC:
				case 0xC0FD:
				case 0xC0FE:
				case 0xC0FF:
					return _slot7.ReadIoRegionC0C0(address);

				default:
					throw new ArgumentOutOfRangeException(nameof(address));
			}

			return _video.ReadFloatingBus();
		}

		private int ReadIoRegionC1C7(int address)
		{
			_slotRegionC8CF = (address >> 8) & 0x07;
			return IsRomC1CFInternal ? _romInternalRegionC1CF[address - 0xC100] : Slots[_slotRegionC8CF].ReadIoRegionC1C7(address);
		}

		private int ReadIoRegionC3C3(int address)
		{
			_slotRegionC8CF = 3;
			if (!IsRomC3C3External)
			{
				SetRomC8CF(true); // $C3XX sets IntC8Rom; inhibits I/O Strobe' [5-28, 7-21]
			}

			return IsRomC1CFInternal || !IsRomC3C3External
				? _noSlotClock.Read(address, _romInternalRegionC1CF[address - 0xC100])
				: _slot3.ReadIoRegionC1C7(address);
		}

		private int ReadIoRegionC8CF(int address)
		{
			if (address == 0xCFFF)
			{
				SetRomC8CF(false); // $CFFF resets IntC8Rom [5-28, 7-21]
			}
			return IsRomC1CFInternal || IsRomC8CFInternal
				? _noSlotClock.Read(address, _romInternalRegionC1CF[address - 0xC100])
				: Slots[_slotRegionC8CF].ReadIoRegionC8CF(address);
		}

		public int ReadRamMainRegion02BF(int address)
		{
			return _ramMainRegion02BF[address - 0x0200];
		}

		public int ReadRamAuxRegion02BF(int address)
		{
			return _ramAuxRegion02BF[address - 0x0200];
		}

		public int ReadRomRegionE0FF(int address)
		{
			return _romRegionE0FF[address - 0xE000];
		}

		private void WriteIoRegionC0C0(int address, byte data)
		{
			switch (address)
			{
				case 0xC000:
				case 0xC001: // [5-22]
					Set80Store(TestBit(address, 0));
					break;

				case 0xC002:
				case 0xC003:
					SetRamRead(TestBit(address, 0));
					break;

				case 0xC004:
				case 0xC005:
					SetRamWrite(TestBit(address, 0));
					break;

				case 0xC006:
				case 0xC007:
					SetRomC1CF(TestBit(address, 0));
					break;

				case 0xC008:
				case 0xC009:
					SetZeroPage(TestBit(address, 0));
					break;

				case 0xC00A:
				case 0xC00B:
					SetRomC3C3(TestBit(address, 0));
					break;

				case 0xC00C:
				case 0xC00D: // [7-5]
					Set80Columns(TestBit(address, 0));
					break;

				case 0xC00E:
				case 0xC00F:
					SetCharSet(TestBit(address, 0));
					break;

				case 0xC010:
				case 0xC011:
				case 0xC012:
				case 0xC013:
				case 0xC014:
				case 0xC015:
				case 0xC016:
				case 0xC017: // [7-15]
				case 0xC018:
				case 0xC019:
				case 0xC01A:
				case 0xC01B:
				case 0xC01C:
				case 0xC01D:
				case 0xC01E:
				case 0xC01F:
					Keyboard.ResetStrobe();
					break;

				case 0xC020:
				case 0xC021:
				case 0xC022:
				case 0xC023:
				case 0xC024:
				case 0xC025:
				case 0xC026:
				case 0xC027: // [7-8]
				case 0xC028:
				case 0xC029:
				case 0xC02A:
				case 0xC02B:
				case 0xC02C:
				case 0xC02D:
				case 0xC02E:
				case 0xC02F:
					_cassette.ToggleOutput();
					break;

				case 0xC030:
				case 0xC031:
				case 0xC032:
				case 0xC033:
				case 0xC034:
				case 0xC035:
				case 0xC036:
				case 0xC037: // [7-9]
				case 0xC038:
				case 0xC039:
				case 0xC03A:
				case 0xC03B:
				case 0xC03C:
				case 0xC03D:
				case 0xC03E:
				case 0xC03F:
					Speaker.ToggleOutput();
					break;

				case 0xC040:
				case 0xC041:
				case 0xC042:
				case 0xC043:
				case 0xC044:
				case 0xC045:
				case 0xC046:
				case 0xC047: // [2-18]
				case 0xC048:
				case 0xC049:
				case 0xC04A:
				case 0xC04B:
				case 0xC04C:
				case 0xC04D:
				case 0xC04E:
				case 0xC04F:
					break;

				case 0xC050:
				case 0xC051:
					SetText(TestBit(address, 0));
					break;

				case 0xC052:
				case 0xC053:
					SetMixed(TestBit(address, 0));
					break;

				case 0xC054:
				case 0xC055:
					SetPage2(TestBit(address, 0));
					break;

				case 0xC056:
				case 0xC057:
					SetHires(TestBit(address, 0));
					break;

				case 0xC058:
				case 0xC059:
					SetAnnunciator0(TestBit(address, 0));
					break;

				case 0xC05A:
				case 0xC05B:
					SetAnnunciator1(TestBit(address, 0));
					break;

				case 0xC05C:
				case 0xC05D:
					SetAnnunciator2(TestBit(address, 0));
					break;

				case 0xC05E:
				case 0xC05F:
					SetAnnunciator3(TestBit(address, 0));
					SetDoubleRes(!TestBit(address, 0));
					break;

				case 0xC060:
				case 0xC061:
				case 0xC062:
				case 0xC063:
				case 0xC064:
				case 0xC065:
				case 0xC066:
				case 0xC067: // [2-18, 7-5]
				case 0xC068:
				case 0xC069:
				case 0xC06A:
				case 0xC06B:
				case 0xC06C:
				case 0xC06D:
				case 0xC06E:
				case 0xC06F:
					break;

				case 0xC070:
				case 0xC071:
				case 0xC072:
				case 0xC073:
				case 0xC074:
				case 0xC075:
				case 0xC076:
				case 0xC077:
				case 0xC078:
				case 0xC079:
				case 0xC07A:
				case 0xC07B:
				case 0xC07C:
				case 0xC07D:
				case 0xC07E:
				case 0xC07F:
					_gamePort.TriggerTimers();
					break;

				case 0xC080:
				case 0xC081:
				case 0xC082:
				case 0xC083:
				case 0xC084:
				case 0xC085:
				case 0xC086:
				case 0xC087: // slot0 [5-23]
				case 0xC088:
				case 0xC089:
				case 0xC08A:
				case 0xC08B:
				case 0xC08C:
				case 0xC08D:
				case 0xC08E:
				case 0xC08F:
					SetHighRam(address, false);
					break;

				case 0xC090:
				case 0xC091:
				case 0xC092:
				case 0xC093:
				case 0xC094:
				case 0xC095:
				case 0xC096:
				case 0xC097: // slot1
				case 0xC098:
				case 0xC099:
				case 0xC09A:
				case 0xC09B:
				case 0xC09C:
				case 0xC09D:
				case 0xC09E:
				case 0xC09F:
					_slot1.WriteIoRegionC0C0(address, data);
					break;

				case 0xC0A0:
				case 0xC0A1:
				case 0xC0A2:
				case 0xC0A3:
				case 0xC0A4:
				case 0xC0A5:
				case 0xC0A6:
				case 0xC0A7: // slot2
				case 0xC0A8:
				case 0xC0A9:
				case 0xC0AA:
				case 0xC0AB:
				case 0xC0AC:
				case 0xC0AD:
				case 0xC0AE:
				case 0xC0AF:
					_slot2.WriteIoRegionC0C0(address, data);
					break;

				case 0xC0B0:
				case 0xC0B1:
				case 0xC0B2:
				case 0xC0B3:
				case 0xC0B4:
				case 0xC0B5:
				case 0xC0B6:
				case 0xC0B7: // slot3
				case 0xC0B8:
				case 0xC0B9:
				case 0xC0BA:
				case 0xC0BB:
				case 0xC0BC:
				case 0xC0BD:
				case 0xC0BE:
				case 0xC0BF:
					_slot3.WriteIoRegionC0C0(address, data);
					break;

				case 0xC0C0:
				case 0xC0C1:
				case 0xC0C2:
				case 0xC0C3:
				case 0xC0C4:
				case 0xC0C5:
				case 0xC0C6:
				case 0xC0C7: // slot4
				case 0xC0C8:
				case 0xC0C9:
				case 0xC0CA:
				case 0xC0CB:
				case 0xC0CC:
				case 0xC0CD:
				case 0xC0CE:
				case 0xC0CF:
					_slot4.WriteIoRegionC0C0(address, data);
					break;

				case 0xC0D0:
				case 0xC0D1:
				case 0xC0D2:
				case 0xC0D3:
				case 0xC0D4:
				case 0xC0D5:
				case 0xC0D6:
				case 0xC0D7: // slot5
				case 0xC0D8:
				case 0xC0D9:
				case 0xC0DA:
				case 0xC0DB:
				case 0xC0DC:
				case 0xC0DD:
				case 0xC0DE:
				case 0xC0DF:
					_slot5.WriteIoRegionC0C0(address, data);
					break;

				case 0xC0E0:
				case 0xC0E1:
				case 0xC0E2:
				case 0xC0E3:
				case 0xC0E4:
				case 0xC0E5:
				case 0xC0E6:
				case 0xC0E7: // slot6
				case 0xC0E8:
				case 0xC0E9:
				case 0xC0EA:
				case 0xC0EB:
				case 0xC0EC:
				case 0xC0ED:
				case 0xC0EE:
				case 0xC0EF:
					DiskIIController.WriteIoRegionC0C0(address, data);
					break;

				case 0xC0F0:
				case 0xC0F1:
				case 0xC0F2:
				case 0xC0F3:
				case 0xC0F4:
				case 0xC0F5:
				case 0xC0F6:
				case 0xC0F7: // slot7
				case 0xC0F8:
				case 0xC0F9:
				case 0xC0FA:
				case 0xC0FB:
				case 0xC0FC:
				case 0xC0FD:
				case 0xC0FE:
				case 0xC0FF:
					_slot7.WriteIoRegionC0C0(address, data);
					break;

				default:
					throw new ArgumentOutOfRangeException(nameof(address));
			}
		}

		private void WriteIoRegionC1C7(int address, byte data)
		{
			_slotRegionC8CF = (address >> 8) & 0x07;
			if (!IsRomC1CFInternal)
			{
				Slots[_slotRegionC8CF].WriteIoRegionC1C7(address, data);
			}
		}

		private void WriteIoRegionC3C3(int address, byte data)
		{
			_slotRegionC8CF = 3;
			if (!IsRomC3C3External)
			{
				SetRomC8CF(true); // $C3XX sets IntC8Rom; inhibits I/O Strobe' [5-28, 7-21]
			}
			if (IsRomC1CFInternal || !IsRomC3C3External)
			{
				_noSlotClock.Write(address);
			}
			else
			{
				_slot3.WriteIoRegionC1C7(address, data);
			}
		}

		private void WriteIoRegionC8CF(int address, byte data)
		{
			if (address == 0xCFFF)
			{
				SetRomC8CF(false); // $CFFF resets IntC8Rom [5-28, 7-21]
			}
			if (IsRomC1CFInternal || IsRomC8CFInternal)
			{
				_noSlotClock.Write(address);
			}
			else
			{
				Slots[_slotRegionC8CF].WriteIoRegionC8CF(address, data);
			}
		}

		private void WriteRamMode0MainRegion0407(int address, byte data)
		{
			if (_ramMainRegion02BF[address - 0x0200] != data)
			{
				_ramMainRegion02BF[address - 0x0200] = data;
				_video.DirtyCell(address - 0x0400); // lores page1
			}
		}

		private void WriteRamMode0MainRegion080B(int address, byte data)
		{
			if (_ramMainRegion02BF[address - 0x0200] != data)
			{
				_ramMainRegion02BF[address - 0x0200] = data;
				_video.DirtyCell(address - 0x0800); // lores page2
			}
		}

		private void WriteRamMode1MainRegion0407(int address, byte data)
		{
			if (_ramMainRegion02BF[address - 0x0200] != data)
			{
				_ramMainRegion02BF[address - 0x0200] = data;
				_video.DirtyCell(address - 0x0400); // text40 page1
			}
		}

		private void WriteRamMode1MainRegion080B(int address, byte data)
		{
			if (_ramMainRegion02BF[address - 0x0200] != data)
			{
				_ramMainRegion02BF[address - 0x0200] = data;
				_video.DirtyCell(address - 0x0800); // text40 page2
			}
		}

		private void WriteRamMode2MainRegion0407(int address, byte data)
		{
			if (_ramMainRegion02BF[address - 0x0200] != data)
			{
				_ramMainRegion02BF[address - 0x0200] = data;
				_video.DirtyCell(address - 0x0400); // text80 page1
			}
		}

		private void WriteRamMode2MainRegion080B(int address, byte data)
		{
			if (_ramMainRegion02BF[address - 0x0200] != data)
			{
				_ramMainRegion02BF[address - 0x0200] = data;
				_video.DirtyCell(address - 0x0800); // text80 page2
			}
		}

		private void WriteRamMode2AuxRegion0407(int address, byte data)
		{
			if (_ramAuxRegion02BF[address - 0x0200] != data)
			{
				_ramAuxRegion02BF[address - 0x0200] = data;
				_video.DirtyCell(address - 0x0400); // text80 page1
			}
		}

		private void WriteRamMode2AuxRegion080B(int address, byte data)
		{
			if (_ramAuxRegion02BF[address - 0x0200] != data)
			{
				_ramAuxRegion02BF[address - 0x0200] = data;
				_video.DirtyCell(address - 0x0800); // text80 page2
			}
		}

		private void WriteRamMode3MainRegion0407(int address, byte data)
		{
			if (_ramMainRegion02BF[address - 0x0200] != data)
			{
				_ramMainRegion02BF[address - 0x0200] = data;
				_video.DirtyCell(address - 0x0400); // lores & text40 page1
			}
		}

		private void WriteRamMode3MainRegion080B(int address, byte data)
		{
			if (_ramMainRegion02BF[address - 0x0200] != data)
			{
				_ramMainRegion02BF[address - 0x0200] = data;
				_video.DirtyCell(address - 0x0800); // lores & text40 page2
			}
		}

		private void WriteRamMode4MainRegion0407(int address, byte data)
		{
			if (_ramMainRegion02BF[address - 0x0200] != data)
			{
				_ramMainRegion02BF[address - 0x0200] = data;
				_video.DirtyCell(address - 0x0400); // lores & text80 page1
			}
		}

		private void WriteRamMode4MainRegion080B(int address, byte data)
		{
			if (_ramMainRegion02BF[address - 0x0200] != data)
			{
				_ramMainRegion02BF[address - 0x0200] = data;
				_video.DirtyCell(address - 0x0800); // lores & text80 page2
			}
		}

		private void WriteRamMode4AuxRegion0407(int address, byte data)
		{
			if (_ramAuxRegion02BF[address - 0x0200] != data)
			{
				_ramAuxRegion02BF[address - 0x0200] = data;
				_video.DirtyCellMixedText(address - 0x0400); // [lores &] text80 page1
			}
		}

		private void WriteRamMode4AuxRegion080B(int address, byte data)
		{
			if (_ramAuxRegion02BF[address - 0x0200] != data)
			{
				_ramAuxRegion02BF[address - 0x0200] = data;
				_video.DirtyCellMixedText(address - 0x0800); // [lores &] text80 page2
			}
		}

		private void WriteRamMode5MainRegion203F(int address, byte data)
		{
			if (_ramMainRegion02BF[address - 0x0200] != data)
			{
				_ramMainRegion02BF[address - 0x0200] = data;
				_video.DirtyCell(address - 0x2000); // hires page1
			}
		}

		private void WriteRamMode5MainRegion405F(int address, byte data)
		{
			if (_ramMainRegion02BF[address - 0x0200] != data)
			{
				_ramMainRegion02BF[address - 0x0200] = data;
				_video.DirtyCell(address - 0x4000); // hires page2
			}
		}

		private void WriteRamMode6MainRegion0407(int address, byte data)
		{
			if (_ramMainRegion02BF[address - 0x0200] != data)
			{
				_ramMainRegion02BF[address - 0x0200] = data;
				_video.DirtyCellMixedText(address - 0x0400); // [hires &] text40 page1
			}
		}

		private void WriteRamMode6MainRegion080B(int address, byte data)
		{
			if (_ramMainRegion02BF[address - 0x0200] != data)
			{
				_ramMainRegion02BF[address - 0x0200] = data;
				_video.DirtyCellMixedText(address - 0x0800); // [hires &] text40 page2
			}
		}

		private void WriteRamMode6MainRegion203F(int address, byte data)
		{
			if (_ramMainRegion02BF[address - 0x0200] != data)
			{
				_ramMainRegion02BF[address - 0x0200] = data;
				_video.DirtyCellMixed(address - 0x2000); // hires [& text40] page1
			}
		}

		private void WriteRamMode6MainRegion405F(int address, byte data)
		{
			if (_ramMainRegion02BF[address - 0x0200] != data)
			{
				_ramMainRegion02BF[address - 0x0200] = data;
				_video.DirtyCellMixed(address - 0x4000); // hires [& text40] page2
			}
		}

		private void WriteRamMode7MainRegion0407(int address, byte data)
		{
			if (_ramMainRegion02BF[address - 0x0200] != data)
			{
				_ramMainRegion02BF[address - 0x0200] = data;
				_video.DirtyCellMixedText(address - 0x0400); // [hires &] text80 page1
			}
		}

		private void WriteRamMode7MainRegion080B(int address, byte data)
		{
			if (_ramMainRegion02BF[address - 0x0200] != data)
			{
				_ramMainRegion02BF[address - 0x0200] = data;
				_video.DirtyCellMixedText(address - 0x0800); // [hires &] text80 page2
			}
		}

		private void WriteRamMode7MainRegion203F(int address, byte data)
		{
			if (_ramMainRegion02BF[address - 0x0200] != data)
			{
				_ramMainRegion02BF[address - 0x0200] = data;
				_video.DirtyCellMixed(address - 0x2000); // hires [& text80] page1
			}
		}

		private void WriteRamMode7MainRegion405F(int address, byte data)
		{
			if (_ramMainRegion02BF[address - 0x0200] != data)
			{
				_ramMainRegion02BF[address - 0x0200] = data;
				_video.DirtyCellMixed(address - 0x4000); // hires [& text80] page2
			}
		}

		private void WriteRamMode7AuxRegion0407(int address, byte data)
		{
			if (_ramAuxRegion02BF[address - 0x0200] != data)
			{
				_ramAuxRegion02BF[address - 0x0200] = data;
				_video.DirtyCellMixedText(address - 0x0400); // [hires &] text80 page1
			}
		}

		private void WriteRamMode7AuxRegion080B(int address, byte data)
		{
			if (_ramAuxRegion02BF[address - 0x0200] != data)
			{
				_ramAuxRegion02BF[address - 0x0200] = data;
				_video.DirtyCellMixedText(address - 0x0800); // [hires &] text80 page2
			}
		}

		private void WriteRamMode8MainRegion0407(int address, byte data)
		{
			if (_ramMainRegion02BF[address - 0x0200] != data)
			{
				_ramMainRegion02BF[address - 0x0200] = data;
				_video.DirtyCell(address - 0x0400); // 7mlores page1
			}
		}

		private void WriteRamMode8MainRegion080B(int address, byte data)
		{
			if (_ramMainRegion02BF[address - 0x0200] != data)
			{
				_ramMainRegion02BF[address - 0x0200] = data;
				_video.DirtyCell(address - 0x0800); // 7mlores page2
			}
		}

		private void WriteRamMode9MainRegion0407(int address, byte data)
		{
			if (_ramMainRegion02BF[address - 0x0200] != data)
			{
				_ramMainRegion02BF[address - 0x0200] = data;
				_video.DirtyCell(address - 0x0400); // dlores page1
			}
		}

		private void WriteRamMode9MainRegion080B(int address, byte data)
		{
			if (_ramMainRegion02BF[address - 0x0200] != data)
			{
				_ramMainRegion02BF[address - 0x0200] = data;
				_video.DirtyCell(address - 0x0800); // dlores page2
			}
		}

		private void WriteRamMode9AuxRegion0407(int address, byte data)
		{
			if (_ramAuxRegion02BF[address - 0x0200] != data)
			{
				_ramAuxRegion02BF[address - 0x0200] = data;
				_video.DirtyCell(address - 0x0400); // dlores page1
			}
		}

		private void WriteRamMode9AuxRegion080B(int address, byte data)
		{
			if (_ramAuxRegion02BF[address - 0x0200] != data)
			{
				_ramAuxRegion02BF[address - 0x0200] = data;
				_video.DirtyCell(address - 0x0800); // dlores page2
			}
		}

		private void WriteRamModeAMainRegion0407(int address, byte data)
		{
			if (_ramMainRegion02BF[address - 0x0200] != data)
			{
				_ramMainRegion02BF[address - 0x0200] = data;
				_video.DirtyCell(address - 0x0400); // 7mlores & text40 page1
			}
		}

		private void WriteRamModeAMainRegion080B(int address, byte data)
		{
			if (_ramMainRegion02BF[address - 0x0200] != data)
			{
				_ramMainRegion02BF[address - 0x0200] = data;
				_video.DirtyCell(address - 0x0800); // 7mlores & text40 page2
			}
		}

		private void WriteRamModeBMainRegion0407(int address, byte data)
		{
			if (_ramMainRegion02BF[address - 0x0200] != data)
			{
				_ramMainRegion02BF[address - 0x0200] = data;
				_video.DirtyCell(address - 0x0400); // dlores & text80 page1
			}
		}

		private void WriteRamModeBMainRegion080B(int address, byte data)
		{
			if (_ramMainRegion02BF[address - 0x0200] != data)
			{
				_ramMainRegion02BF[address - 0x0200] = data;
				_video.DirtyCell(address - 0x0800); // dlores & text80 page2
			}
		}

		private void WriteRamModeBAuxRegion0407(int address, byte data)
		{
			if (_ramAuxRegion02BF[address - 0x0200] != data)
			{
				_ramAuxRegion02BF[address - 0x0200] = data;
				_video.DirtyCell(address - 0x0400); // dlores & text80 page1
			}
		}

		private void WriteRamModeBAuxRegion080B(int address, byte data)
		{
			if (_ramAuxRegion02BF[address - 0x0200] != data)
			{
				_ramAuxRegion02BF[address - 0x0200] = data;
				_video.DirtyCell(address - 0x0800); // dlores & text80 page2
			}
		}

		private void WriteRamModeCMainRegion203F(int address, byte data)
		{
			if (_ramMainRegion02BF[address - 0x0200] != data)
			{
				_ramMainRegion02BF[address - 0x0200] = data;
				_video.DirtyCell(address - 0x2000); // ndhires page1
			}
		}

		private void WriteRamModeCMainRegion405F(int address, byte data)
		{
			if (_ramMainRegion02BF[address - 0x0200] != data)
			{
				_ramMainRegion02BF[address - 0x0200] = data;
				_video.DirtyCell(address - 0x4000); // ndhires page2
			}
		}

		private void WriteRamModeDMainRegion203F(int address, byte data)
		{
			if (_ramMainRegion02BF[address - 0x0200] != data)
			{
				_ramMainRegion02BF[address - 0x0200] = data;
				_video.DirtyCell(address - 0x2000); // dhires page1
			}
		}

		private void WriteRamModeDMainRegion405F(int address, byte data)
		{
			if (_ramMainRegion02BF[address - 0x0200] != data)
			{
				_ramMainRegion02BF[address - 0x0200] = data;
				_video.DirtyCell(address - 0x4000); // dhires page2
			}
		}

		private void WriteRamModeDAuxRegion203F(int address, byte data)
		{
			if (_ramAuxRegion02BF[address - 0x0200] != data)
			{
				_ramAuxRegion02BF[address - 0x0200] = data;
				_video.DirtyCell(address - 0x2000); // dhires page1
			}
		}

		private void WriteRamModeDAuxRegion405F(int address, byte data)
		{
			if (_ramAuxRegion02BF[address - 0x0200] != data)
			{
				_ramAuxRegion02BF[address - 0x0200] = data;
				_video.DirtyCell(address - 0x4000); // dhires page2
			}
		}

		private void WriteRamModeEMainRegion0407(int address, byte data)
		{
			if (_ramMainRegion02BF[address - 0x0200] != data)
			{
				_ramMainRegion02BF[address - 0x0200] = data;
				_video.DirtyCellMixedText(address - 0x0400); // [ndhires &] text40 page1
			}
		}

		private void WriteRamModeEMainRegion080B(int address, byte data)
		{
			if (_ramMainRegion02BF[address - 0x0200] != data)
			{
				_ramMainRegion02BF[address - 0x0200] = data;
				_video.DirtyCellMixedText(address - 0x0800); // [ndhires &] text40 page2
			}
		}

		private void WriteRamModeEMainRegion203F(int address, byte data)
		{
			if (_ramMainRegion02BF[address - 0x0200] != data)
			{
				_ramMainRegion02BF[address - 0x0200] = data;
				_video.DirtyCellMixed(address - 0x2000); // ndhires [& text40] page1
			}
		}

		private void WriteRamModeEMainRegion405F(int address, byte data)
		{
			if (_ramMainRegion02BF[address - 0x0200] != data)
			{
				_ramMainRegion02BF[address - 0x0200] = data;
				_video.DirtyCellMixed(address - 0x4000); // ndhires [& text40] page2
			}
		}

		private void WriteRamModeFMainRegion0407(int address, byte data)
		{
			if (_ramMainRegion02BF[address - 0x0200] != data)
			{
				_ramMainRegion02BF[address - 0x0200] = data;
				_video.DirtyCellMixedText(address - 0x0400); // [dhires &] text80 page1
			}
		}

		private void WriteRamModeFMainRegion080B(int address, byte data)
		{
			if (_ramMainRegion02BF[address - 0x0200] != data)
			{
				_ramMainRegion02BF[address - 0x0200] = data;
				_video.DirtyCellMixedText(address - 0x0800); // [dhires &] text80 page2
			}
		}

		private void WriteRamModeFMainRegion203F(int address, byte data)
		{
			if (_ramMainRegion02BF[address - 0x0200] != data)
			{
				_ramMainRegion02BF[address - 0x0200] = data;
				_video.DirtyCellMixed(address - 0x2000); // dhires [& text80] page1
			}
		}

		private void WriteRamModeFMainRegion405F(int address, byte data)
		{
			if (_ramMainRegion02BF[address - 0x0200] != data)
			{
				_ramMainRegion02BF[address - 0x0200] = data;
				_video.DirtyCellMixed(address - 0x4000); // dhires [& text80] page2
			}
		}

		private void WriteRamModeFAuxRegion0407(int address, byte data)
		{
			if (_ramAuxRegion02BF[address - 0x0200] != data)
			{
				_ramAuxRegion02BF[address - 0x0200] = data;
				_video.DirtyCellMixedText(address - 0x0400); // [dhires &] text80 page1
			}
		}

		private void WriteRamModeFAuxRegion080B(int address, byte data)
		{
			if (_ramAuxRegion02BF[address - 0x0200] != data)
			{
				_ramAuxRegion02BF[address - 0x0200] = data;
				_video.DirtyCellMixedText(address - 0x0800); // [dhires &] text80 page2
			}
		}

		private void WriteRamModeFAuxRegion203F(int address, byte data)
		{
			if (_ramAuxRegion02BF[address - 0x0200] != data)
			{
				_ramAuxRegion02BF[address - 0x0200] = data;
				_video.DirtyCellMixed(address - 0x2000); // dhires [& text80] page1
			}
		}

		private void WriteRamModeFAuxRegion405F(int address, byte data)
		{
			if (_ramAuxRegion02BF[address - 0x0200] != data)
			{
				_ramAuxRegion02BF[address - 0x0200] = data;
				_video.DirtyCellMixed(address - 0x4000); // dhires [& text80] page2
			}
		}

		private void WriteRomRegionD0FF(int address, byte data)
		{
		}

		private void MapRegion0001()
		{
			if (!IsZeroPageAux)
			{
				_regionRead[Region0001] = _ramMainRegion0001;
				_regionWrite[Region0001] = _ramMainRegion0001;
				_zeroPage = _ramMainRegion0001;
			}
			else
			{
				_regionRead[Region0001] = _ramAuxRegion0001;
				_regionWrite[Region0001] = _ramAuxRegion0001;
				_zeroPage = _ramAuxRegion0001;
			}
			_writeRegion[Region0001] = null;
		}

		private void MapRegion02BF()
		{
			if (!IsRamReadAux)
			{
				_regionRead[Region02BF] = _ramMainRegion02BF;
				_regionRead[Region080B] = _ramMainRegion02BF;
				_regionRead[Region405F] = _ramMainRegion02BF;
			}
			else
			{
				_regionRead[Region02BF] = _ramAuxRegion02BF;
				_regionRead[Region080B] = _ramAuxRegion02BF;
				_regionRead[Region405F] = _ramAuxRegion02BF;
			}
			int mode = VideoMode;
			if (!IsRamWriteAux)
			{
				_regionWrite[Region02BF] = _ramMainRegion02BF;
				_regionWrite[Region080B] = _ramMainRegion02BF;
				_regionWrite[Region405F] = _ramMainRegion02BF;
				_writeRegion[Region02BF] = null;
				_writeRegion[Region080B] = WriteRamModeBankRegion[mode][BankMain][Region080B];
				_writeRegion[Region405F] = WriteRamModeBankRegion[mode][BankMain][Region405F];
			}
			else
			{
				_regionWrite[Region02BF] = _ramAuxRegion02BF;
				_regionWrite[Region080B] = _ramAuxRegion02BF;
				_regionWrite[Region405F] = _ramAuxRegion02BF;
				_writeRegion[Region02BF] = null;
				_writeRegion[Region080B] = WriteRamModeBankRegion[mode][BankAux][Region080B];
				_writeRegion[Region405F] = WriteRamModeBankRegion[mode][BankAux][Region405F];
			}
			MapRegion0407();
			MapRegion203F();
		}

		private void MapRegion0407()
		{
			if (!IsRamReadAuxRegion0407)
			{
				_regionRead[Region0407] = _ramMainRegion02BF;
			}
			else
			{
				_regionRead[Region0407] = _ramAuxRegion02BF;
			}
			int mode = VideoMode;
			if (!IsRamWriteAuxRegion0407)
			{
				_regionWrite[Region0407] = _ramMainRegion02BF;
				_writeRegion[Region0407] = WriteRamModeBankRegion[mode][BankMain][Region0407];
			}
			else
			{
				_regionWrite[Region0407] = _ramAuxRegion02BF;
				_writeRegion[Region0407] = WriteRamModeBankRegion[mode][BankAux][Region0407];
			}
		}

		private void MapRegion203F()
		{
			if (!IsRamReadAuxRegion203F)
			{
				_regionRead[Region203F] = _ramMainRegion02BF;
			}
			else
			{
				_regionRead[Region203F] = _ramAuxRegion02BF;
			}
			int mode = VideoMode;
			if (!IsRamWriteAuxRegion203F)
			{
				_regionWrite[Region203F] = _ramMainRegion02BF;
				_writeRegion[Region203F] = WriteRamModeBankRegion[mode][BankMain][Region203F];
			}
			else
			{
				_regionWrite[Region203F] = _ramAuxRegion02BF;
				_writeRegion[Region203F] = WriteRamModeBankRegion[mode][BankAux][Region203F];
			}
		}

		private void MapRegionC0CF()
		{
			_regionRead[RegionC0C0] = null;
			if (IsRomC1CFInternal)
			{
				_regionRead[RegionC1C7] = _romInternalRegionC1CF;
				_regionRead[RegionC3C3] = _romInternalRegionC1CF;
				_regionRead[RegionC8CF] = _romInternalRegionC1CF;
			}
			else
			{
				_regionRead[RegionC1C7] = _romExternalRegionC1CF;
				_regionRead[RegionC3C3] = IsRomC3C3External ? _romExternalRegionC1CF : _romInternalRegionC1CF;
				_regionRead[RegionC8CF] = !IsRomC8CFInternal ? _romExternalRegionC1CF : _romInternalRegionC1CF;
			}
			_regionWrite[RegionC0C0] = null;
			_regionWrite[RegionC1C7] = null;
			_regionWrite[RegionC3C3] = null;
			_regionWrite[RegionC8CF] = null;
			_writeRegion[RegionC0C0] = _writeIoRegionC0C0;
			_writeRegion[RegionC1C7] = _writeIoRegionC1C7;
			_writeRegion[RegionC3C3] = _writeIoRegionC3C3;
			_writeRegion[RegionC8CF] = _writeIoRegionC8CF;
		}

		private void MapRegionD0FF()
		{
			if (IsHighRamRead)
			{
				if (!IsHighRamAux)
				{
					_regionRead[RegionD0DF] = IsHighRamBank1 ? _ramMainBank1RegionD0DF : _ramMainBank2RegionD0DF;
					_regionRead[RegionE0FF] = _ramMainRegionE0FF;
				}
				else
				{
					_regionRead[RegionD0DF] = IsHighRamBank1 ? _ramAuxBank1RegionD0DF : _ramAuxBank2RegionD0DF;
					_regionRead[RegionE0FF] = _ramAuxRegionE0FF;
				}
			}
			else
			{
				_regionRead[RegionD0DF] = _romRegionD0DF;
				_regionRead[RegionE0FF] = _romRegionE0FF;
			}
			if (IsHighRamWrite)
			{
				if (!IsHighRamAux)
				{
					_regionWrite[RegionD0DF] = IsHighRamBank1 ? _ramMainBank1RegionD0DF : _ramMainBank2RegionD0DF;
					_regionWrite[RegionE0FF] = _ramMainRegionE0FF;
				}
				else
				{
					_regionWrite[RegionD0DF] = IsHighRamBank1 ? _ramAuxBank1RegionD0DF : _ramAuxBank2RegionD0DF;
					_regionWrite[RegionE0FF] = _ramAuxRegionE0FF;
				}
				_writeRegion[RegionD0DF] = null;
				_writeRegion[RegionE0FF] = null;
			}
			else
			{
				_regionWrite[RegionD0DF] = null;
				_regionWrite[RegionE0FF] = null;
				_writeRegion[RegionD0DF] = _writeRomRegionD0FF;
				_writeRegion[RegionE0FF] = _writeRomRegionD0FF;
			}
		}

		private void Set80Columns(bool value)
		{
			if (!TestState(State80Col, value))
			{
				SetState(State80Col, value);
				MapRegion02BF();
				_video.DirtyScreen();
			}
		}

		private void Set80Store(bool value)
		{
			if (!TestState(State80Store, value))
			{
				SetState(State80Store, value);
				if (IsPage2) // [5-7, 8-19]
				{
					MapRegion02BF();
					_video.DirtyScreen();
				}
				else
				{
					MapRegion0407();
					MapRegion203F();
				}
			}
		}

		private void SetAnnunciator0(bool value)
		{
			SetState(StateAn0, value);
		}

		private void SetAnnunciator1(bool value)
		{
			SetState(StateAn1, value);
		}

		private void SetAnnunciator2(bool value)
		{
			SetState(StateAn2, value);
		}

		private void SetAnnunciator3(bool value)
		{
			SetState(StateAn3, value);
		}

		private void SetCharSet(bool value)
		{
			if (!TestState(StateAltChrSet, value))
			{
				SetState(StateAltChrSet, value);
				_video.SetCharSet();
			}
		}

		private void SetDoubleRes(bool value)
		{
			if (!TestState(StateDRes, value))
			{
				SetState(StateDRes, value);
				MapRegion02BF();
				_video.DirtyScreen();
			}
		}

		private void SetHighRam(int address, bool isRead)
		{
			SetState(StateBank1, TestBit(address, 3)); // A3 [5-22]
			SetState(StateHRamRd, TestMask(address, 0x3, 0x3) || TestMask(address, 0x3, 0x0)); // A0.A1+A0'.A1' [5-23] (5-22 misprint)
			if (TestBit(address, 0)) // A0 [5-23]
			{
				if (isRead && TestState(StateHRamPreWrt))
				{
					ResetState(StateHRamWrt); // HRamWrt' [5-23]
				}
			}
			else
			{
				SetState(StateHRamWrt);
			}
			SetState(StateHRamPreWrt, isRead && TestBit(address, 0)); // A0.R/W' [5-22]
			MapRegionD0FF();
		}

		private void SetHires(bool value)
		{
			if (!TestState(StateHires, value))
			{
				SetState(StateHires, value);
				if (!Is80Store) // [5-7, 8-19]
				{
					MapRegion02BF();
					_video.DirtyScreen();
				}
				else
				{
					MapRegion203F();
				}
			}
		}

		private void SetMixed(bool value)
		{
			if (!TestState(StateMixed, value))
			{
				SetState(StateMixed, value);
				MapRegion02BF();
				_video.DirtyScreen();
			}
		}

		private void SetPage2(bool value)
		{
			if (!TestState(StatePage2, value))
			{
				SetState(StatePage2, value);
				if (!Is80Store) // [5-7, 8-19]
				{
					MapRegion02BF();
					_video.DirtyScreen();
				}
				else
				{
					MapRegion0407();
					MapRegion203F();
				}
			}
		}

		private void SetRamRead(bool value)
		{
			if (!TestState(StateRamRd, value))
			{
				SetState(StateRamRd, value);
				MapRegion02BF();
			}
		}

		private void SetRamWrite(bool value)
		{
			if (!TestState(StateRamWrt, value))
			{
				SetState(StateRamWrt, value);
				MapRegion02BF();
			}
		}

		private void SetRomC1CF(bool value)
		{
			if (!TestState(StateIntCXRom, value))
			{
				SetState(StateIntCXRom, value);
				MapRegionC0CF();
			}
		}

		private void SetRomC3C3(bool value)
		{
			if (!TestState(StateSlotC3Rom, value))
			{
				SetState(StateSlotC3Rom, value);
				MapRegionC0CF();
			}
		}

		private void SetRomC8CF(bool value)
		{
			if (!TestState(StateIntC8Rom, value))
			{
				SetState(StateIntC8Rom, value);
				MapRegionC0CF();
			}
		}

		private void SetText(bool value)
		{
			if (!TestState(StateText, value))
			{
				SetState(StateText, value);
				MapRegion02BF();
				_video.DirtyScreen();
			}
		}

		private void SetZeroPage(bool value)
		{
			if (!TestState(StateAltZP, value))
			{
				SetState(StateAltZP, value);
				MapRegion0001();
				MapRegionD0FF();
			}
		}

		private static int SetBit7(int data, bool value)
		{
			return value ? data | 0x80 : data & 0x7F;
		}

		private static bool TestBit(int data, int bit)
		{
			return (data & (0x1 << bit)) != 0x0;
		}

		private static bool TestMask(int data, int mask, int value)
		{
			return (data & mask) == value;
		}

		private void ResetState(int mask)
		{
			_state &= ~mask;
		}

		private void SetState(int mask)
		{
			_state |= mask;
		}

		private void SetState(int mask, bool value)
		{
			if (value)
			{
				_state |= mask;
			}
			else
			{
				_state &= ~mask;
			}
		}

		private bool TestState(int mask)
		{
			return (_state & mask) != 0x0;
		}

		private bool TestState(int mask, bool value)
		{
			return ((_state & mask) != 0x0) == value;
		}

		private bool TestState(int mask, int value)
		{
			return (_state & mask) == value;
		}

		private bool Is80Columns => TestState(State80Col);
		private bool Is80Store => TestState(State80Store);
		public bool IsCharSetAlternate => TestState(StateAltChrSet);
		private bool IsHighRamAux => IsZeroPageAux;
		private bool IsHighRamBank1 => TestState(StateBank1);
		private bool IsHighRamRead => TestState(StateHRamRd);
		private bool IsHighRamWrite => !TestState(StateHRamWrt); // HRamWrt' [5-23]
		public bool IsHires => TestState(StateHires);
		public bool IsMixed => TestState(StateMixed);
		internal bool IsPage2 => TestState(StatePage2);
		internal bool IsRamReadAux => TestState(StateRamRd);
		internal bool IsRamReadAuxRegion0407 => Is80Store ? IsPage2 : IsRamReadAux;
		internal bool IsRamReadAuxRegion203F => TestState(State80Store | StateHires, State80Store | StateHires) ? IsPage2 : IsRamReadAux;
		internal bool IsRamWriteAux => TestState(StateRamWrt);
		internal bool IsRamWriteAuxRegion0407 => Is80Store ? IsPage2 : IsRamWriteAux;
		internal bool IsRamWriteAuxRegion203F => TestState(State80Store | StateHires, State80Store | StateHires) ? IsPage2 : IsRamWriteAux;
		internal bool IsRomC1CFInternal => TestState(StateIntCXRom);
		internal bool IsRomC3C3External => TestState(StateSlotC3Rom);
		internal bool IsRomC8CFInternal => TestState(StateIntC8Rom);
		public bool IsText => TestState(StateText);
		public bool IsVideoPage2 => TestState(State80Store | StatePage2, StatePage2); // 80Store inhibits video Page2 [5-7, 8-19]
		internal bool IsZeroPageAux => TestState(StateAltZP);

		public MonitorType Monitor { get; private set; }
		public int VideoMode => StateVideoMode[_state & StateVideo];

		private Action<int, byte> _writeIoRegionC0C0;
		private Action<int, byte> _writeIoRegionC1C7;
		private Action<int, byte> _writeIoRegionC3C3;
		private Action<int, byte> _writeIoRegionC8CF;
		private Action<int, byte> _writeRomRegionD0FF;

		public Action<uint> ReadCallback;
		public Action<uint> WriteCallback;
		public Action<uint> ExecuteCallback;
		public Action InputCallback;
	}
}
