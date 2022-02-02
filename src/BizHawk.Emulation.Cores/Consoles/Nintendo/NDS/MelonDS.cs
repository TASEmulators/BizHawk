using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

using BizHawk.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Properties;
using BizHawk.Emulation.Cores.Waterbox;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.NDS
{
	[PortedCore(CoreNames.MelonDS, "Arisotura", "0.9.3", "http://melonds.kuribo64.net/")]
	[ServiceNotApplicable(new[] { typeof(IDriveLight), typeof(IRegionable) })]
	public partial class NDS : WaterboxCore
	{
		private readonly LibMelonDS _core;
		private readonly NDSDisassembler _disassembler;
		private SpeexResampler _resampler;

		[CoreConstructor(VSystemID.Raw.NDS)]
		public NDS(CoreLoadParameters<NDSSettings, NDSSyncSettings> lp)
			: base(lp.Comm, new Configuration
			{
				DefaultWidth = 256,
				DefaultHeight = 384,
				MaxWidth = 256,
				MaxHeight = 384,
				MaxSamples = 1024,
				DefaultFpsNumerator = 33513982,
				DefaultFpsDenominator = 560190,
				SystemId = VSystemID.Raw.NDS,
			})
		{
			_syncSettings = lp.SyncSettings ?? new NDSSyncSettings();
			_settings = lp.Settings ?? new NDSSettings();

			var roms = lp.Roms.Select(r => r.RomData).ToList();

			if (roms.Count > (_syncSettings.UseDSi ? 1 : 3))
			{
				throw new InvalidOperationException("Wrong number of ROMs!");
			}

			bool gbacartpresent = roms.Count > 1;
			bool gbasrampresent = roms.Count == 3;

			_tracecb = MakeTrace;

			_core = PreInit<LibMelonDS>(new WaterboxOptions
			{
				Filename = "melonDS.wbx",
				SbrkHeapSizeKB = 2 * 1024,
				SealedHeapSizeKB = 4,
				InvisibleHeapSizeKB = 4 * 1024,
				PlainHeapSizeKB = 4,
				MmapHeapSizeKB = 1024 * 1024,
				SkipCoreConsistencyCheck = CoreComm.CorePreferences.HasFlag(CoreComm.CorePreferencesFlags.WaterboxCoreConsistencyCheck),
				SkipMemoryConsistencyCheck = CoreComm.CorePreferences.HasFlag(CoreComm.CorePreferencesFlags.WaterboxMemoryConsistencyCheck),
			}, new Delegate[] { _tracecb });

			var bios7 = _syncSettings.UseDSi || _syncSettings.UseRealBIOS
				? CoreComm.CoreFileProvider.GetFirmwareOrThrow(new("NDS", "bios7"))
				: null;

			var bios9 = _syncSettings.UseDSi || _syncSettings.UseRealBIOS
				? CoreComm.CoreFileProvider.GetFirmwareOrThrow(new("NDS", "bios9"))
				: null;

			var bios7i = _syncSettings.UseDSi
				? CoreComm.CoreFileProvider.GetFirmwareOrThrow(new("NDS", "bios7i"))
				: null;

			var bios9i = _syncSettings.UseDSi
				? CoreComm.CoreFileProvider.GetFirmwareOrThrow(new("NDS", "bios9i"))
				: null;

			var nand = _syncSettings.UseDSi
				? CoreComm.CoreFileProvider.GetFirmwareOrThrow(new("NDS", "nand"))
				: null;

			var fw = _syncSettings.UseDSi
				? CoreComm.CoreFileProvider.GetFirmwareOrThrow(new("NDS", "firmwarei"))
				: CoreComm.CoreFileProvider.GetFirmware(new("NDS", "firmware"));

			var tmd = _syncSettings.UseDSi && _syncSettings.LoadDSiWare
				? GetTMDData(roms[0])
				: null;

			bool skipfw = _syncSettings.SkipFirmware || !_syncSettings.UseRealBIOS || fw == null;

			LibMelonDS.LoadFlags flags = LibMelonDS.LoadFlags.NONE;

			if (_syncSettings.UseRealBIOS || _syncSettings.UseDSi)
				flags |= LibMelonDS.LoadFlags.USE_REAL_BIOS;
			if (skipfw && !_syncSettings.UseDSi)
				flags |= LibMelonDS.LoadFlags.SKIP_FIRMWARE;
			if (gbacartpresent)
				flags |= LibMelonDS.LoadFlags.GBA_CART_PRESENT;
			if (_settings.AccurateAudioBitrate)
				flags |= LibMelonDS.LoadFlags.ACCURATE_AUDIO_BITRATE;
			if (_syncSettings.FirmwareOverride || lp.DeterministicEmulationRequested)
				flags |= LibMelonDS.LoadFlags.FIRMWARE_OVERRIDE;
			if (_syncSettings.UseDSi)
				flags |= LibMelonDS.LoadFlags.IS_DSI;
			if (_syncSettings.UseDSi && _syncSettings.LoadDSiWare)
				flags |= LibMelonDS.LoadFlags.LOAD_DSIWARE;

			var fwSettings = new LibMelonDS.FirmwareSettings();
			var name = Encoding.UTF8.GetBytes(_syncSettings.FirmwareUsername);
			fwSettings.FirmwareUsernameLength = name.Length;
			fwSettings.FirmwareLanguage = _syncSettings.FirmwareLanguage;
			if (_syncSettings.FirmwareStartUp == NDSSyncSettings.StartUp.AutoBoot) fwSettings.FirmwareLanguage |= (NDSSyncSettings.Language)0x40;
			fwSettings.FirmwareBirthdayMonth = _syncSettings.FirmwareBirthdayMonth;
			fwSettings.FirmwareBirthdayDay = _syncSettings.FirmwareBirthdayDay;
			fwSettings.FirmwareFavouriteColour = _syncSettings.FirmwareFavouriteColour;
			var message = _syncSettings.FirmwareMessage.Length != 0 ? Encoding.UTF8.GetBytes(_syncSettings.FirmwareMessage) : new byte[1] { 0 };
			fwSettings.FirmwareMessageLength = message.Length;

			var loadData = new LibMelonDS.LoadData
			{
				DsRomLength = roms[0].Length,
				GbaRomLength = gbacartpresent ? roms[1].Length : 0,
				GbaRamLength = gbasrampresent ? roms[2].Length : 0,
				NandLength = nand?.Length ?? 0,
			};
			if (_syncSettings.UseRealBIOS || _syncSettings.UseDSi)
			{
				_exe.AddReadonlyFile(bios7, "bios7.rom");
				_exe.AddReadonlyFile(bios9, "bios9.rom");
			}
			if (_syncSettings.UseDSi)
			{
				_exe.AddReadonlyFile(bios7i, "bios7i.rom");
				_exe.AddReadonlyFile(bios9i, "bios9i.rom");
				if (_syncSettings.LoadDSiWare)
				{
					_exe.AddReadonlyFile(roms[0], "dsiware.rom");
				}
			}
			if (fw != null)
			{
				if (_syncSettings.UseDSi || NDSFirmware.MaybeWarnIfBadFw(fw, CoreComm))
				{
					if (_syncSettings.FirmwareOverride || lp.DeterministicEmulationRequested)
					{
						NDSFirmware.SanitizeFw(fw);
					}
				}
				_exe.AddReadonlyFile(fw, _syncSettings.UseDSi ? "firmwarei.bin" : "firmware.bin");
			}

			unsafe
			{
				fixed (byte*
					dsRomPtr = roms[0],
					gbaRomPtr = gbacartpresent ? roms[1] : null,
					gbaRamPtr = gbasrampresent ? roms[2] : null,
					nandPtr = nand,
					tmdPtr = tmd,
					namePtr = name,
					messagePtr = message)
				{
					loadData.DsRomData = (IntPtr)dsRomPtr;
					loadData.GbaRomData = (IntPtr)gbaRomPtr;
					loadData.GbaRamData = (IntPtr)gbaRamPtr;
					loadData.NandData = (IntPtr)nandPtr;
					loadData.TmdData = (IntPtr)tmdPtr;
					fwSettings.FirmwareUsername = (IntPtr)namePtr;
					fwSettings.FirmwareMessage = (IntPtr)messagePtr;
					if (!_core.Init(flags, loadData, fwSettings))
					{
						throw new InvalidOperationException("Init returned false!");
					}
				}
			}

			if (fw != null)
			{
				_exe.RemoveReadonlyFile(_syncSettings.UseDSi ? "firmwarei.bin" : "firmware.bin");
			}

			if (_syncSettings.UseDSi && _syncSettings.LoadDSiWare)
			{
				_exe.RemoveReadonlyFile("dsiware.rom");
			}

			PostInit();

			((MemoryDomainList)this.AsMemoryDomains()).SystemBus = new NDSSystemBus(this.AsMemoryDomains()["ARM9 System Bus"], this.AsMemoryDomains()["ARM7 System Bus"]);

			DeterministicEmulation = lp.DeterministicEmulationRequested || (!_syncSettings.UseRealTime);
			InitializeRtc(_syncSettings.InitialTime);

			_resampler = new SpeexResampler(SpeexResampler.Quality.QUALITY_DEFAULT, 32768, 44100, 32768, 44100, null, this);
			_serviceProvider.Register<ISoundProvider>(_resampler);

			_disassembler = new NDSDisassembler();
			_serviceProvider.Register<IDisassemblable>(_disassembler);

			const string TRACE_HEADER = "ARM9+ARM7: PC, opcode, registers (r0, r1, r2, r3, r4, r5, r6, r7, r8, r9, r10, r11, r12, r13, r14, r15, Cy, CpuMode)";
			Tracer = new TraceBuffer(TRACE_HEADER);
			_serviceProvider.Register(Tracer);
		}

		public override void Dispose()
		{
			base.Dispose();
			if (_resampler != null)
			{
				_resampler.Dispose();
				_resampler = null;
			}
		}

		private static byte[] GetTMDData(byte[] ware)
		{
			ulong titleId = 0;
			for (int i = 0; i < 8; i++)
			{
				titleId <<= 8;
				titleId |= ware[0x230 + 8 - i];
			}
			using var zip = new ZipArchive(new MemoryStream(Util.DecompressGzipFile(new MemoryStream(Resources.TMDS.Value))), ZipArchiveMode.Read, false);
			using var tmd = zip.GetEntry($"{titleId}.tmd").Open();
			var ret = new byte[tmd.Length];
			tmd.Read(ret, 0, (int)tmd.Length);
			return ret;
		}

		public override ControllerDefinition ControllerDefinition => NDSController;

		public static readonly ControllerDefinition NDSController = new ControllerDefinition("NDS Controller")
		{
			BoolButtons =
			{
				"Up", "Down", "Left", "Right", "Start", "Select", "B", "A", "Y", "X", "L", "R", "LidOpen", "LidClose", "Touch", "Power"
			}
		}.AddXYPair("Touch {0}", AxisPairOrientation.RightAndUp, 0.RangeTo(255), 128, 0.RangeTo(191), 96)
			.AddAxis("Mic Volume", (0).RangeTo(100), 0)
			.AddAxis("GBA Light Sensor", 0.RangeTo(10), 0)
			.MakeImmutable();

		private LibMelonDS.Buttons GetButtons(IController c)
		{
			LibMelonDS.Buttons b = 0;
			if (c.IsPressed("Up"))
				b |= LibMelonDS.Buttons.UP;
			if (c.IsPressed("Down"))
				b |= LibMelonDS.Buttons.DOWN;
			if (c.IsPressed("Left"))
				b |= LibMelonDS.Buttons.LEFT;
			if (c.IsPressed("Right"))
				b |= LibMelonDS.Buttons.RIGHT;
			if (c.IsPressed("Start"))
				b |= LibMelonDS.Buttons.START;
			if (c.IsPressed("Select"))
				b |= LibMelonDS.Buttons.SELECT;
			if (c.IsPressed("B"))
				b |= LibMelonDS.Buttons.B;
			if (c.IsPressed("A"))
				b |= LibMelonDS.Buttons.A;
			if (c.IsPressed("Y"))
				b |= LibMelonDS.Buttons.Y;
			if (c.IsPressed("X"))
				b |= LibMelonDS.Buttons.X;
			if (c.IsPressed("L"))
				b |= LibMelonDS.Buttons.L;
			if (c.IsPressed("R"))
				b |= LibMelonDS.Buttons.R;
			if (c.IsPressed("LidOpen"))
				b |= LibMelonDS.Buttons.LIDOPEN;
			if (c.IsPressed("LidClose"))
				b |= LibMelonDS.Buttons.LIDCLOSE;
			if (c.IsPressed("Touch"))
				b |= LibMelonDS.Buttons.TOUCH;
			if (c.IsPressed("Power"))
				b |= LibMelonDS.Buttons.POWER;

			return b;
		}

		private bool _renderSound;

		protected override LibWaterboxCore.FrameInfo FrameAdvancePrep(IController controller, bool render, bool rendersound)
		{
			_renderSound = rendersound;
			_core.SetTraceCallback(Tracer.IsEnabled() ? _tracecb : null);
			return new LibMelonDS.FrameInfo
			{
				Time = GetRtcTime(!DeterministicEmulation),
				Keys = GetButtons(controller),
				TouchX = (byte)controller.AxisValue("Touch X"),
				TouchY = (byte)controller.AxisValue("Touch Y"),
				MicVolume = (byte)controller.AxisValue("Mic Volume"),
				GBALightSensor = (byte)controller.AxisValue("GBA Light Sensor"),
			};
		}

		protected override void FrameAdvancePost()
		{
			// the core SHOULD produce 547 or 548 samples each frame
			// however, it seems in some cases (first few frames on power on and lid closed) it doesn't for some reason
			// hack around it here
			if (_numSamples < 547 && _renderSound)
			{
				for (int i = _numSamples * 2; i < (547 * 2); i++)
				{
					_soundBuffer[i] = 0;
				}
				_numSamples = 547;
			}
		}

		// omega hack
		public class NDSSystemBus : MemoryDomain
		{
			private readonly MemoryDomain Arm9Bus;
			private readonly MemoryDomain Arm7Bus;

			public NDSSystemBus(MemoryDomain arm9, MemoryDomain arm7)
			{
				Name = "System Bus";
				Size = 1L << 32;
				WordSize = 4;
				EndianType = Endian.Little;
				Writable = false;

				Arm9Bus = arm9;
				Arm7Bus = arm7;
			}

			public bool UseArm9 { get; set; } = true;

			public override byte PeekByte(long addr) => UseArm9 ? Arm9Bus.PeekByte(addr) : Arm7Bus.PeekByte(addr);

			public override void PokeByte(long addr, byte val) => throw new InvalidOperationException();
		}
	}
}
