using System.Collections.Generic;
using System.Linq;

using BizHawk.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Computers.Commodore64.Cartridge;
using BizHawk.Emulation.Cores.Computers.Commodore64.Media;

namespace BizHawk.Emulation.Cores.Computers.Commodore64
{
	[Core(CoreNames.C64Hawk, "SaxxonPike")]
	public sealed partial class C64 : IEmulator, IRegionable, IBoardInfo, IRomInfo
	{
		[CoreConstructor(VSystemID.Raw.C64)]
		public C64(CoreLoadParameters<C64Settings, C64SyncSettings> lp)
		{
			PutSyncSettings(lp.SyncSettings ?? new C64SyncSettings());
			PutSettings(lp.Settings ?? new C64Settings());

			var ser = new BasicServiceProvider(this);
			ServiceProvider = ser;

			CoreComm = lp.Comm;
			_roms = lp.Roms.Select(r => r.RomData).ToList();
			_currentDisk = 0;
			RomSanityCheck();

			Init(SyncSettings.VicType, Settings.BorderType, SyncSettings.SidType, SyncSettings.TapeDriveType, SyncSettings.DiskDriveType);
			_cyclesPerFrame = _board.Vic.CyclesPerFrame;
			_memoryCallbacks = new MemoryCallbackSystem(new[] { "System Bus" });

			if (_board.DiskDrive != null)
			{
				_board.DiskDrive.InitSaveRam(_roms.Count);
				ser.Register<ISaveRam>(_board.DiskDrive);
			}

			InitMedia(_roms[_currentDisk]);
			HardReset();

			switch (SyncSettings.VicType)
			{
				case VicType.Ntsc:
				case VicType.Drean:
				case VicType.NtscOld:
					Region = DisplayType.NTSC;
					break;
				case VicType.Pal:
					Region = DisplayType.PAL;
					break;
			}

			if (_board.Sid != null)
			{
				_soundProvider = new DCFilter(_board.Sid, 512);
				ser.Register<ISoundProvider>(_soundProvider);
			}

			ser.Register<IVideoProvider>(_board.Vic);
			ser.Register<IDriveLight>(this);

			_tracer = new TraceBuffer(_board.Cpu.TraceHeader);
			ser.Register<ITraceable>(_tracer);
			ser.Register<IStatable>(new StateSerializer(SyncState));

			if (_board.CartPort.IsConnected)
			{
				var first = _roms[0]; // There are no multi-cart cart games, so just hardcode first
				RomDetails = $"{lp.Game.Name}\r\n{SHA1Checksum.ComputePrefixedHex(first)}\r\n{MD5Checksum.ComputePrefixedHex(first)}\r\nMapper Impl \"{_board.CartPort.CartridgeType}\"";
			}

			SetupMemoryDomains();
		}

		public void ExecFetch(ushort addr)
		{
			if (_memoryCallbacks.HasExecutes)
			{
				uint flags = (uint)(MemoryCallbackFlags.CPUZero | MemoryCallbackFlags.AccessExecute);
				_memoryCallbacks.CallMemoryCallbacks(addr, 0, flags, "System Bus");
			}
		}

		private CoreComm CoreComm { get; }

		public string RomDetails { get; }

		// Currently we will require at least one rom.  If multiple they MUST be all the same media type in the same format
		// Given a good enough use case we could in theory expand those requirements, but for now we need a sanity check
		private void RomSanityCheck()
		{
			if (_roms.Count == 0)
			{
				throw new NotSupportedException("Currently, a Rom is required to run this core.");
			}

			var formats = _roms.Select(C64FormatFinder.GetFormat);

			HashSet<C64Format> uniqueFormats = new HashSet<C64Format>();

			foreach (var format in formats)
			{
				uniqueFormats.Add(format);
			}

			if (uniqueFormats.Count > 1)
			{
				throw new NotSupportedException("Currently Roms must all be of the same type.");
			}
		}

		// IBoardInfo
		public string BoardName
		{
			get
			{
				if (_board.CartPort.IsConnected)
				{
					return _board.CartPort.CartridgeType;
				}

				if (_board.TapeDrive != null)
				{
					return "Tape Drive";
				}

				if (_board.DiskDrive != null)
				{
					return "Disk Drive"; // TODO: drive types?
				}

				return "";
			}
		}

		// IRegionable
		public DisplayType Region { get; }

		private readonly int _cyclesPerFrame;

		private readonly List<byte[]> _roms;

		private static readonly ControllerDefinition C64ControllerDefinition = new ControllerDefinition("Commodore 64 Controller")
		{
			BoolButtons =
			{
				"P1 Up", "P1 Down", "P1 Left", "P1 Right", "P1 Button",
				"P2 Up", "P2 Down", "P2 Left", "P2 Right", "P2 Button",
				"Key Left Arrow", "Key 1", "Key 2", "Key 3", "Key 4", "Key 5", "Key 6", "Key 7", "Key 8", "Key 9", "Key 0", "Key Plus", "Key Minus", "Key Pound", "Key Clear/Home", "Key Insert/Delete",
				"Key Control", "Key Q", "Key W", "Key E", "Key R", "Key T", "Key Y", "Key U", "Key I", "Key O", "Key P", "Key At", "Key Asterisk", "Key Up Arrow", "Key Restore",
				"Key Run/Stop", "Key Lck", "Key A", "Key S", "Key D", "Key F", "Key G", "Key H", "Key J", "Key K", "Key L", "Key Colon", "Key Semicolon", "Key Equal", "Key Return",
				"Key Commodore", "Key Left Shift", "Key Z", "Key X", "Key C", "Key V", "Key B", "Key N", "Key M", "Key Comma", "Key Period", "Key Slash", "Key Right Shift", "Key Cursor Up/Down", "Key Cursor Left/Right",
				"Key Space",
				"Key F1", "Key F3", "Key F5", "Key F7",
				"Previous Disk", "Next Disk",
				"Power", "Reset"
			}
		}.MakeImmutable();

		private Motherboard _board;

		private int _frameCycles;

		private int _frame;
		private readonly ITraceable _tracer;
		
		// Power stuff
		private bool _powerPressed;
		private bool _resetPressed;

		// Disk stuff
		private bool _nextPressed;
		private bool _prevPressed;
		private int _currentDisk;
		public int CurrentDisk => _currentDisk;
		public int DiskCount => _roms.Count;

		private void IncrementDisk()
		{
			_board.DiskDrive.SaveDeltas();
			_currentDisk++;
			if (CurrentDisk >= _roms.Count)
			{
				_currentDisk = 0;
			}

			InitDisk();
		}

		private void DecrementDisk()
		{
			_board.DiskDrive.SaveDeltas();
			_currentDisk--;
			if (_currentDisk < 0)
			{
				_currentDisk = _roms.Count - 1;
			}

			InitDisk();
		}

		private void InitDisk()
		{
			InitMedia(_roms[_currentDisk]);
			_board.DiskDrive.LoadDeltas();
		}

		public void SetDisk(int discNum)
		{
			if (_currentDisk != discNum)
			{
				_board.DiskDrive.SaveDeltas();
				_currentDisk = discNum;
				InitDisk();
			}
		}

		/**********************************************/

		private ISoundProvider _soundProvider;

		private void DoCycle()
		{
			if (_frameCycles == 0)
			{
				_board.InputRead = false;
				_board.PollInput();
			}

			_board.Execute();
			_frameCycles++;

			if (_frameCycles != _cyclesPerFrame)
			{
				return;
			}

			_board.Flush();
			_isLagFrame = !_board.InputRead;

			if (_isLagFrame)
			{
				_lagCount++;
			}

			_frameCycles -= _cyclesPerFrame;
			_frame++;
		}

		private byte[] GetFirmware(int length, params string[] names)
		{
			var result = names.Select(n => CoreComm.CoreFileProvider.GetFirmware(new("C64", n))).FirstOrDefault(b => b != null && b.Length == length);
			if (result == null)
			{
				throw new MissingFirmwareException($"At least one of these firmware options is required: {string.Join(", ", names)}");
			}

			return result;
		}

		private void Init(VicType initRegion, BorderType borderType, SidType sidType, TapeDriveType tapeDriveType, DiskDriveType diskDriveType)
		{
			// Force certain drive types to be available depending on ROM type
			var rom = _roms[0];

			switch (C64FormatFinder.GetFormat(rom))
			{
				case C64Format.D64:
				case C64Format.G64:
				case C64Format.X64:
					if (diskDriveType == DiskDriveType.None)
					{
						diskDriveType = DiskDriveType.Commodore1541;
					}

					break;
				case C64Format.T64:
				case C64Format.TAP:
					if (tapeDriveType == TapeDriveType.None)
					{
						tapeDriveType = TapeDriveType.Commodore1530;
					}

					break;
				case C64Format.CRT:
					// Nothing required.
					break;
				case C64Format.Unknown:
					if (rom.Length >= 0xFE00)
					{
						throw new Exception("The image format is not known, and too large to be used as a PRG.");
					}

					if (diskDriveType == DiskDriveType.None)
					{
						diskDriveType = DiskDriveType.Commodore1541;
					}

					break;
				default:
					throw new Exception("The image format is not yet supported by the Commodore 64 core.");
			}

			_board = new Motherboard(this, initRegion, borderType, sidType, tapeDriveType, diskDriveType);
			InitRoms(diskDriveType);
			_board.Init();
		}

		private void InitMedia(byte[] rom)
		{
			switch (C64FormatFinder.GetFormat(rom))
			{
				case C64Format.D64:
					var d64 = D64.Read(rom);
					if (d64 != null)
					{
						_board.DiskDrive.InsertMedia(d64);
					}
					break;
				case C64Format.G64:
					var g64 = G64.Read(rom);
					if (g64 != null)
					{
						_board.DiskDrive.InsertMedia(g64);
					}
					break;
				case C64Format.CRT:
					var cart = CartridgeDevice.Load(rom);
					if (cart != null)
					{
						_board.CartPort.Connect(cart);
					}
					break;
				case C64Format.TAP:
					var tape = Tape.Load(rom);
					if (tape != null)
					{
						_board.TapeDrive.Insert(tape);
					}
					break;
				case C64Format.Unknown:
					var prgDisk = new DiskBuilder
					{
						Entries = new List<DiskBuilder.Entry>
						{
							new DiskBuilder.Entry
							{
								Closed = true,
								Data = rom,
								Locked = false,
								Name = "PRG",
								RecordLength = 0,
								Type = DiskBuilder.FileType.Program
							}
						}
					}.Build();
					if (prgDisk != null)
					{
						_board.DiskDrive.InsertMedia(prgDisk);
					}
					break;
			}
		}

		private void InitRoms(DiskDriveType diskDriveType)
		{
			_board.BasicRom.Flash(GetFirmware(0x2000, "Basic"));
			_board.KernalRom.Flash(GetFirmware(0x2000, "Kernal"));
			_board.CharRom.Flash(GetFirmware(0x1000, "Chargen"));

			switch (diskDriveType)
			{
				case DiskDriveType.Commodore1541:
					_board.DiskDrive.DriveRom.Flash(GetFirmware(0x4000, "Drive1541"));
					break;
				case DiskDriveType.Commodore1541II:
					_board.DiskDrive.DriveRom.Flash(GetFirmware(0x4000, "Drive1541II"));
					break;
			}
		}

		private void HardReset()
		{
			_board.HardReset();
		}

		private void SoftReset()
		{
			_board.SoftReset();
		}
	}
}
