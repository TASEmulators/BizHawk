using System;
using System.Collections.Generic;
using System.Linq;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Computers.Commodore64.Cartridge;
using BizHawk.Emulation.Cores.Computers.Commodore64.Media;

namespace BizHawk.Emulation.Cores.Computers.Commodore64
{
	[CoreAttributes(
		"C64Hawk",
		"SaxxonPike",
		isPorted: false,
		isReleased: false
		)]
	[ServiceNotApplicable(typeof(ISettable<,>))]
	public sealed partial class C64 : IEmulator, IRegionable
    {
        #region Internals

        [SaveState.DoNotSave]
        private readonly int _cyclesPerFrame;

        [SaveState.DoNotSave]
        public GameInfo Game;

        [SaveState.DoNotSave]
        public IEnumerable<byte[]> Roms { get; private set; }

        [SaveState.DoNotSave]
        private static readonly ControllerDefinition C64ControllerDefinition = new ControllerDefinition
        {
            Name = "Commodore 64 Controller",
            BoolButtons =
            {
                "P1 Up", "P1 Down", "P1 Left", "P1 Right", "P1 Button",
                "P2 Up", "P2 Down", "P2 Left", "P2 Right", "P2 Button",
                "Key Left Arrow", "Key 1", "Key 2", "Key 3", "Key 4", "Key 5", "Key 6", "Key 7", "Key 8", "Key 9", "Key 0", "Key Plus", "Key Minus", "Key Pound", "Key Clear/Home", "Key Insert/Delete",
                "Key Control", "Key Q", "Key W", "Key E", "Key R", "Key T", "Key Y", "Key U", "Key I", "Key O", "Key P", "Key At", "Key Asterisk", "Key Up Arrow", "Key Restore",
                "Key Run/Stop", "Key Lck", "Key A", "Key S", "Key D", "Key F", "Key G", "Key H", "Key J", "Key K", "Key L", "Key Colon", "Key Semicolon", "Key Equal", "Key Return",
                "Key Commodore", "Key Left Shift", "Key Z", "Key X", "Key C", "Key V", "Key B", "Key N", "Key M", "Key Comma", "Key Period", "Key Slash", "Key Right Shift", "Key Cursor Up/Down", "Key Cursor Left/Right",
                "Key Space",
                "Key F1", "Key F3", "Key F5", "Key F7"
            }
        };

        [SaveState.SaveWithName("Board")]
        private Motherboard _board;

        private int _frameCycles;

        #endregion

        #region Ctor

        public C64(CoreComm comm, IEnumerable<byte[]> roms, object settings, object syncSettings)
		{
			PutSyncSettings((C64SyncSettings)syncSettings ?? new C64SyncSettings());
			PutSettings((C64Settings)settings ?? new C64Settings());

			ServiceProvider = new BasicServiceProvider(this);
			InputCallbacks = new InputCallbackSystem();

		    CoreComm = comm;
		    Roms = roms;
            Init(SyncSettings.VicType, Settings.BorderType, SyncSettings.SidType, SyncSettings.TapeDriveType, SyncSettings.DiskDriveType);
			_cyclesPerFrame = _board.Vic.CyclesPerFrame;
			SetupMemoryDomains(_board.DiskDrive != null);
            _memoryCallbacks = new MemoryCallbackSystem();
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
				((BasicServiceProvider)ServiceProvider).Register<ISoundProvider>(_soundProvider);
			}

            DeterministicEmulation = true;

            ((BasicServiceProvider) ServiceProvider).Register<IVideoProvider>(_board.Vic);
            ((BasicServiceProvider) ServiceProvider).Register<IDriveLight>(this);
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (_board != null)
            {
                if (_board.TapeDrive != null)
                {
                    _board.TapeDrive.RemoveMedia();
                }
                if (_board.DiskDrive != null)
                {
                    _board.DiskDrive.RemoveMedia();
                }
                _board = null;
            }
        }

        #endregion

        #region IRegionable

        [SaveState.DoNotSave]
        public DisplayType Region
        {
            get;
            private set;
        }

        #endregion

        #region IEmulator

        [SaveState.DoNotSave]
        public CoreComm CoreComm { get; private set; }
        [SaveState.DoNotSave]
        public string SystemId { get { return "C64"; } }
        [SaveState.DoNotSave]
        public string BoardName { get { return null; } }
        [SaveState.SaveWithName("DeterministicEmulation")]
        public bool DeterministicEmulation { get; set; }
        [SaveState.SaveWithName("Frame")]
        public int Frame { get; set; }

		[SaveState.DoNotSave]
        public ControllerDefinition ControllerDefinition { get { return C64ControllerDefinition; } }
        [SaveState.DoNotSave]
        public IController Controller { get { return _board.Controller; } set { _board.Controller = value; } }
        [SaveState.DoNotSave]
        public IEmulatorServiceProvider ServiceProvider { get; private set; }

        public void ResetCounters()
        {
            Frame = 0;
            LagCount = 0;
            IsLagFrame = false;
            _frameCycles = 0;
        }

        // process frame
        public void FrameAdvance(bool render, bool rendersound)
        {
            do
            {
                DoCycle();
            }
            while (_frameCycles != 0);
        }

		#endregion

		#region ISoundProvider

		[SaveState.DoNotSave]
		public ISoundProvider _soundProvider { get; private set; }

		#endregion

		private void DoCycle()
		{
			if (_frameCycles == 0) {
				_board.InputRead = false;
				_board.PollInput();
				_board.Cpu.LagCycles = 0;
			}

            _board.Execute();
			_frameCycles++;

		    if (_frameCycles != _cyclesPerFrame)
		    {
		        return;
		    }

		    _board.Flush();
		    IsLagFrame = !_board.InputRead;

		    if (IsLagFrame)
		        LagCount++;
		    _frameCycles -= _cyclesPerFrame;
		    Frame++;
		}

		private byte[] GetFirmware(int length, params string[] names)
		{
		    var result = names.Select(n => CoreComm.CoreFileProvider.GetFirmware("C64", n, false)).FirstOrDefault(b => b != null && b.Length == length);
			if (result == null)
				throw new MissingFirmwareException(string.Format("At least one of these firmwares is required: {0}", string.Join(", ", names)));
			return result;
		}

		private void Init(VicType initRegion, BorderType borderType, SidType sidType, TapeDriveType tapeDriveType, DiskDriveType diskDriveType)
		{
            // Force certain drive types to be available depending on ROM type
		    foreach (var rom in Roms)
		    {
                switch (C64FormatFinder.GetFormat(rom))
                {
                    case C64Format.D64:
                    case C64Format.G64:
                    case C64Format.X64:
                        if (diskDriveType == DiskDriveType.None)
                            diskDriveType = DiskDriveType.Commodore1541;
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
                            diskDriveType = DiskDriveType.Commodore1541;
                        break;
                    default:
                        throw new Exception("The image format is not yet supported by the Commodore 64 core.");
                }
            }

            _board = new Motherboard(this, initRegion, borderType, sidType, tapeDriveType, diskDriveType);
			InitRoms(diskDriveType);
			_board.Init();

            // configure video
            CoreComm.VsyncDen = _board.Vic.CyclesPerFrame;
			CoreComm.VsyncNum = _board.Vic.CyclesPerSecond;
        }

		private void InitMedia()
		{
		    foreach (var rom in Roms)
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

		public void HardReset()
		{
		    InitMedia();
            _board.HardReset();
		}
	}
}
