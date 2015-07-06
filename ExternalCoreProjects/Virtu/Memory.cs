using System;
using System.Linq;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using Jellyfish.Library;
using Jellyfish.Virtu.Services;
using Newtonsoft.Json;

namespace Jellyfish.Virtu
{
	public enum MonitorType { Unknown, Standard, Enhanced };

	public sealed partial class Memory : MachineComponent
	{
		public Memory() { }
		public Memory(Machine machine, byte[] appleIIe) :
			base(machine)
		{
			_appleIIe = appleIIe;
			WriteRamModeBankRegion = new Action<int, byte>[Video.ModeCount][][];
			for (int mode = 0; mode < Video.ModeCount; mode++)
			{
				WriteRamModeBankRegion[mode] = new Action<int, byte>[BankCount][]
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

		private byte[] _appleIIe;

		public override void Initialize()
		{
			_keyboard = Machine.Keyboard;
			_gamePort = Machine.GamePort;
			_cassette = Machine.Cassette;
			_speaker = Machine.Speaker;
			_video = Machine.Video;
			_noSlotClock = Machine.NoSlotClock;

			// TODO: this is a lazy and more compicated way to do this
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

			if ((ReadRomRegionE0FF(0xFBB3) == 0x06) && (ReadRomRegionE0FF(0xFBBF) == 0xC1))
			{
				Monitor = MonitorType.Standard;
			}
			else if ((ReadRomRegionE0FF(0xFBB3) == 0x06) && (ReadRomRegionE0FF(0xFBBF) == 0x00) && (ReadRomRegionE0FF(0xFBC0) == 0xE0))
			{
				Monitor = MonitorType.Enhanced;
			}
		}

		public override void Reset() // [7-3]
		{
			ResetState(State80Col | State80Store | StateAltChrSet | StateAltZP | StateBank1 | StateHRamRd | StateHRamPreWrt | StateHRamWrt | // HRamWrt' [5-23]
				StateHires | StatePage2 | StateRamRd | StateRamWrt | StateIntCXRom | StateSlotC3Rom | StateIntC8Rom | StateAn0 | StateAn1 | StateAn2 | StateAn3);
			SetState(StateDRes); // An3' -> DRes [8-20]

			MapRegion0001();
			MapRegion02BF();
			MapRegionC0CF();
			MapRegionD0FF();
		}

		public void LoadPrg(Stream stream)
		{
			if (stream == null)
			{
				throw new ArgumentNullException("stream");
			}

			int startAddress = stream.ReadWord();
			SetWarmEntry(startAddress); // assumes autostart monitor
			Load(stream, startAddress);
		}

		public void LoadXex(Stream stream)
		{
			if (stream == null)
			{
				throw new ArgumentNullException("stream");
			}

			const int Marker = 0xFFFF;
			int marker = stream.ReadWord(); // mandatory marker
			if (marker != Marker)
			{
				throw new InvalidOperationException(string.Format("Marker ${0:X04} not found.", Marker));
			}
			int startAddress = stream.ReadWord();
			int endAddress = stream.ReadWord();
			SetWarmEntry(startAddress); // assumes autostart monitor

			do
			{
				if (startAddress > endAddress)
				{
					throw new InvalidOperationException(string.Format("Invalid address range ${0:X04}-${1:X04}.", startAddress, endAddress));
				}
				Load(stream, startAddress, endAddress - startAddress + 1);
				marker = stream.ReadWord(optional: true); // optional marker
				startAddress = (marker != Marker) ? marker : stream.ReadWord(optional: true);
				endAddress = stream.ReadWord(optional: true);
			}
			while ((startAddress >= 0) && (endAddress >= 0));
		}

		#region Core Read & Write
		public int ReadOpcode(int address)
		{
			int region = PageRegion[address >> 8];
			var result = ((address & 0xF000) != 0xC000) ? _regionRead[region][address - RegionBaseAddress[region]] : ReadIoRegionC0CF(address);
			if (ExecuteCallback != null)
			{
				ExecuteCallback((uint)address);
			}
			if (ReadCallback != null)
			{
				ReadCallback((uint)address);
			}
			return result;
		}

		public int Read(int address)
		{
			int region = PageRegion[address >> 8];
			var result = ((address & 0xF000) != 0xC000) ? _regionRead[region][address - RegionBaseAddress[region]] : ReadIoRegionC0CF(address);
			if (ReadCallback != null)
			{
				ReadCallback((uint)address);
			}
			return result;
		}

		public int Peek(int address)
		{
			int region = PageRegion[address >> 8];
			return ((address & 0xF000) != 0xC000) ? _regionRead[region][address - RegionBaseAddress[region]] : ReadIoRegionC0CF(address);
		}

		public int ReadZeroPage(int address)
		{
			if (ReadCallback != null)
			{
				ReadCallback((uint)address);
			}
			return _zeroPage[address];
		}

		public void Write(int address, int data)
		{
			int region = PageRegion[address >> 8];
			if (_writeRegion[region] == null)
			{
				if (WriteCallback != null)
				{
					WriteCallback((uint)address);
				}
				_regionWrite[region][address - RegionBaseAddress[region]] = (byte)data;
			}
			else
			{
				if (WriteCallback != null)
				{
					WriteCallback((uint)address);
				}
				_writeRegion[region](address, (byte)data);
			}
		}

		public void WriteZeroPage(int address, int data)
		{
			if (WriteCallback != null)
			{
				WriteCallback((uint)address);
			}
			_zeroPage[address] = (byte)data;
		}
		#endregion

		#region Read Actions

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
			if ((0xC000 <= address && address <= 0xC00F) || (0xC061 <= address && address <= 0xC067) || (0xC069 <= address && address <= 0xC06F))
			{
				Machine.Lagged = false;
				if (InputCallback != null)
				{
					InputCallback();
				}
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
					return SetBit7(_keyboard.Latch, _keyboard.Strobe);

				case 0xC010:
					_keyboard.ResetStrobe();
					return SetBit7(_keyboard.Latch, _keyboard.IsAnyKeyDown);

				case 0xC011:
					return SetBit7(_keyboard.Latch, !IsHighRamBank1); // Bank1' [5-22]

				case 0xC012:
					return SetBit7(_keyboard.Latch, IsHighRamRead);

				case 0xC013:
					return SetBit7(_keyboard.Latch, IsRamReadAux);

				case 0xC014:
					return SetBit7(_keyboard.Latch, IsRamWriteAux);

				case 0xC015:
					return SetBit7(_keyboard.Latch, IsRomC1CFInternal);

				case 0xC016:
					return SetBit7(_keyboard.Latch, IsZeroPageAux);

				case 0xC017:
					return SetBit7(_keyboard.Latch, IsRomC3C3External);

				case 0xC018:
					return SetBit7(_keyboard.Latch, Is80Store);

				case 0xC019:
					return SetBit7(_keyboard.Latch, !_video.IsVBlank); // Vbl' [7-5]

				case 0xC01A:
					return SetBit7(_keyboard.Latch, IsText);

				case 0xC01B:
					return SetBit7(_keyboard.Latch, IsMixed);

				case 0xC01C:
					return SetBit7(_keyboard.Latch, IsPage2);

				case 0xC01D:
					return SetBit7(_keyboard.Latch, IsHires);

				case 0xC01E:
					return SetBit7(_keyboard.Latch, IsCharSetAlternate);

				case 0xC01F:
					return SetBit7(_keyboard.Latch, Is80Columns);

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
					_speaker.ToggleOutput();
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
					return Machine.Slot1.ReadIoRegionC0C0(address);

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
					return Machine.Slot2.ReadIoRegionC0C0(address);

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
					return Machine.Slot3.ReadIoRegionC0C0(address);

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
					return Machine.Slot4.ReadIoRegionC0C0(address);

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
					return Machine.Slot5.ReadIoRegionC0C0(address);

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
					return Machine.Slot6.ReadIoRegionC0C0(address);

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
					return Machine.Slot7.ReadIoRegionC0C0(address);

				default:
					throw new ArgumentOutOfRangeException("address");
			}

			return _video.ReadFloatingBus();
		}

		private int ReadIoRegionC1C7(int address)
		{
			_slotRegionC8CF = (address >> 8) & 0x07;
			return IsRomC1CFInternal ? _romInternalRegionC1CF[address - 0xC100] : Machine.Slots[_slotRegionC8CF].ReadIoRegionC1C7(address);
		}

		private int ReadIoRegionC3C3(int address)
		{
			_slotRegionC8CF = 3;
			if (!IsRomC3C3External)
			{
				SetRomC8CF(true); // $C3XX sets IntC8Rom; inhibits I/O Strobe' [5-28, 7-21]
			}
			return (IsRomC1CFInternal || !IsRomC3C3External) ? _noSlotClock.Read(address, _romInternalRegionC1CF[address - 0xC100]) : Machine.Slot3.ReadIoRegionC1C7(address);
		}

		private int ReadIoRegionC8CF(int address)
		{
			if (address == 0xCFFF)
			{
				SetRomC8CF(false); // $CFFF resets IntC8Rom [5-28, 7-21]
			}
			return (IsRomC1CFInternal || IsRomC8CFInternal) ? _noSlotClock.Read(address, _romInternalRegionC1CF[address - 0xC100]) : Machine.Slots[_slotRegionC8CF].ReadIoRegionC8CF(address);
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

		#endregion

		#region Write Actions

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
					_keyboard.ResetStrobe();
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
					_speaker.ToggleOutput();
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
					Machine.Slot1.WriteIoRegionC0C0(address, data);
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
					Machine.Slot2.WriteIoRegionC0C0(address, data);
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
					Machine.Slot3.WriteIoRegionC0C0(address, data);
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
					Machine.Slot4.WriteIoRegionC0C0(address, data);
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
					Machine.Slot5.WriteIoRegionC0C0(address, data);
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
					Machine.Slot6.WriteIoRegionC0C0(address, data);
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
					Machine.Slot7.WriteIoRegionC0C0(address, data);
					break;

				default:
					throw new ArgumentOutOfRangeException("address");
			}
		}

		private void WriteIoRegionC1C7(int address, byte data)
		{
			_slotRegionC8CF = (address >> 8) & 0x07;
			if (!IsRomC1CFInternal)
			{
				Machine.Slots[_slotRegionC8CF].WriteIoRegionC1C7(address, data);
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
				Machine.Slot3.WriteIoRegionC1C7(address, data);
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
				Machine.Slots[_slotRegionC8CF].WriteIoRegionC8CF(address, data);
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

		#endregion

		#region Softswitch Actions
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
		#endregion

		private void Load(Stream stream, int startAddress)
		{
			DebugService.WriteMessage("Loading memory ${0:X04}", startAddress);
			int address = startAddress;
			if (address < 0x0200)
			{
				address += stream.ReadBlock(_ramMainRegion0001, address, minCount: 0);
			}
			if ((0x0200 <= address) && (address < 0xC000))
			{
				address += stream.ReadBlock(_ramMainRegion02BF, address - 0x0200, minCount: 0);
			}
			if ((0xC000 <= address) && (address < 0xD000))
			{
				address += stream.ReadBlock(_ramMainBank1RegionD0DF, address - 0xC000, minCount: 0);
			}
			if ((0xD000 <= address) && (address < 0xE000))
			{
				address += stream.ReadBlock(_ramMainBank2RegionD0DF, address - 0xD000, minCount: 0);
			}
			if (0xE000 <= address)
			{
				address += stream.ReadBlock(_ramMainRegionE0FF, address - 0xE000, minCount: 0);
			}
			if (address > startAddress)
			{
				DebugService.WriteMessage("Loaded memory ${0:X04}-${1:X04} (${2:X04})", startAddress, address - 1, address - startAddress);
			}
		}

		private void Load(Stream stream, int startAddress, int length)
		{
			DebugService.WriteMessage("Loading memory ${0:X04}-${1:X04} (${2:X04})", startAddress, startAddress + length - 1, length);
			int address = startAddress;
			if (address < 0x0200)
			{
				address += stream.ReadBlock(_ramMainRegion0001, address, ref length);
			}
			if ((0x0200 <= address) && (address < 0xC000))
			{
				address += stream.ReadBlock(_ramMainRegion02BF, address - 0x0200, ref length);
			}
			if ((0xC000 <= address) && (address < 0xD000))
			{
				address += stream.ReadBlock(_ramMainBank1RegionD0DF, address - 0xC000, ref length);
			}
			if ((0xD000 <= address) && (address < 0xE000))
			{
				address += stream.ReadBlock(_ramMainBank2RegionD0DF, address - 0xD000, ref length);
			}
			if (0xE000 <= address)
			{
				address += stream.ReadBlock(_ramMainRegionE0FF, address - 0xE000, ref length);
			}
		}

		private void SetWarmEntry(int address)
		{
			_ramMainRegion02BF[0x03F2 - 0x0200] = (byte)(address & 0xFF);
			_ramMainRegion02BF[0x03F3 - 0x0200] = (byte)(address >> 8);
			_ramMainRegion02BF[0x03F4 - 0x0200] = (byte)((address >> 8) ^ 0xA5);
		}

		private static int SetBit7(int data, bool value)
		{
			return value ? (data | 0x80) : (data & 0x7F);
		}

		private static bool TestBit(int data, int bit)
		{
			return ((data & (0x1 << bit)) != 0x0);
		}

		private static bool TestMask(int data, int mask, int value)
		{
			return ((data & mask) == value);
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
			return ((_state & mask) != 0x0);
		}

		private bool TestState(int mask, bool value)
		{
			return (((_state & mask) != 0x0) == value);
		}

		private bool TestState(int mask, int value)
		{
			return ((_state & mask) == value);
		}

		public bool Is80Columns { get { return TestState(State80Col); } }
		public bool Is80Store { get { return TestState(State80Store); } }
		public bool IsAnnunciator0 { get { return TestState(StateAn0); } }
		public bool IsAnnunciator1 { get { return TestState(StateAn1); } }
		public bool IsAnnunciator2 { get { return TestState(StateAn2); } }
		public bool IsAnnunciator3 { get { return TestState(StateAn3); } }
		public bool IsCharSetAlternate { get { return TestState(StateAltChrSet); } }
		public bool IsDoubleRes { get { return TestState(StateDRes); } }
		public bool IsHighRamAux { get { return IsZeroPageAux; } }
		public bool IsHighRamBank1 { get { return TestState(StateBank1); } }
		public bool IsHighRamRead { get { return TestState(StateHRamRd); } }
		public bool IsHighRamWrite { get { return !TestState(StateHRamWrt); } } // HRamWrt' [5-23]
		public bool IsHires { get { return TestState(StateHires); } }
		public bool IsMixed { get { return TestState(StateMixed); } }
		public bool IsPage2 { get { return TestState(StatePage2); } }
		public bool IsRamReadAux { get { return TestState(StateRamRd); } }
		public bool IsRamReadAuxRegion0407 { get { return Is80Store ? IsPage2 : IsRamReadAux; } }
		public bool IsRamReadAuxRegion203F { get { return TestState(State80Store | StateHires, State80Store | StateHires) ? IsPage2 : IsRamReadAux; } }
		public bool IsRamWriteAux { get { return TestState(StateRamWrt); } }
		public bool IsRamWriteAuxRegion0407 { get { return Is80Store ? IsPage2 : IsRamWriteAux; } }
		public bool IsRamWriteAuxRegion203F { get { return TestState(State80Store | StateHires, State80Store | StateHires) ? IsPage2 : IsRamWriteAux; } }
		public bool IsRomC1CFInternal { get { return TestState(StateIntCXRom); } }
		public bool IsRomC3C3External { get { return TestState(StateSlotC3Rom); } }
		public bool IsRomC8CFInternal { get { return TestState(StateIntC8Rom); } }
		public bool IsText { get { return TestState(StateText); } }
		public bool IsVideoPage2 { get { return TestState(State80Store | StatePage2, StatePage2); } } // 80Store inhibits video Page2 [5-7, 8-19]
		public bool IsZeroPageAux { get { return TestState(StateAltZP); } }

		public MonitorType Monitor { get; private set; }
		public int VideoMode { get { return StateVideoMode[_state & StateVideo]; } }

		private Action<int, byte> _writeIoRegionC0C0;
		private Action<int, byte> _writeIoRegionC1C7;
		private Action<int, byte> _writeIoRegionC3C3;
		private Action<int, byte> _writeIoRegionC8CF;
		private Action<int, byte> _writeRomRegionD0FF;

		[JsonIgnore]
		public Action<uint> ReadCallback;

		[JsonIgnore]
		public Action<uint> WriteCallback;

		[JsonIgnore]
		public Action<uint> ExecuteCallback;

		[JsonIgnore]
		public Action InputCallback;

		private Keyboard _keyboard;
		private GamePort _gamePort;
		private Cassette _cassette;
		private Speaker _speaker;
		private Video _video;
		private NoSlotClock _noSlotClock;

		private int _state;
		private int _slotRegionC8CF;

		private byte[] _zeroPage;
		private byte[][] _regionRead = new byte[RegionCount][];
		private byte[][] _regionWrite = new byte[RegionCount][];
		private Action<int, byte>[] _writeRegion = new Action<int, byte>[RegionCount];

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
	}
}
