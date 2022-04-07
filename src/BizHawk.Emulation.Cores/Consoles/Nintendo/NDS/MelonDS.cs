using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BizHawk.BizInvoke;
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

			IsDSi = _syncSettings.UseDSi;

			var roms = lp.Roms.Select(r => r.RomData).ToList();

			if (roms.Count > (IsDSi ? 1 : 3))
			{
				throw new InvalidOperationException("Wrong number of ROMs!");
			}

			IsDSiWare = IsDSi && RomIsWare(roms[0]);

			bool gbacartpresent = roms.Count > 1;
			bool gbasrampresent = roms.Count == 3;

			InitMemoryCallbacks();
			_tracecb = MakeTrace;
			_threadstartcb = ThreadStartCallback;

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
			}, new Delegate[] { _readcb, _writecb, _execcb, _tracecb, _threadstartcb });

			var bios7 = IsDSi || _syncSettings.UseRealBIOS
				? CoreComm.CoreFileProvider.GetFirmwareOrThrow(new("NDS", "bios7"))
				: null;

			var bios9 = IsDSi || _syncSettings.UseRealBIOS
				? CoreComm.CoreFileProvider.GetFirmwareOrThrow(new("NDS", "bios9"))
				: null;

			var bios7i = IsDSi
				? CoreComm.CoreFileProvider.GetFirmwareOrThrow(new("NDS", "bios7i"))
				: null;

			var bios9i = IsDSi
				? CoreComm.CoreFileProvider.GetFirmwareOrThrow(new("NDS", "bios9i"))
				: null;

			var nand = IsDSi
				? CoreComm.CoreFileProvider.GetFirmwareOrThrow(new("NDS", "nand"))
				: null;

			var fw = IsDSi
				? CoreComm.CoreFileProvider.GetFirmwareOrThrow(new("NDS", "firmwarei"))
				: CoreComm.CoreFileProvider.GetFirmware(new("NDS", "firmware"));

			var tmd = IsDSi && IsDSiWare
				? GetTMDData(roms[0])
				: null;

			bool skipfw = _syncSettings.SkipFirmware || !_syncSettings.UseRealBIOS || fw == null;

			LibMelonDS.LoadFlags loadFlags = LibMelonDS.LoadFlags.NONE;

			if (_syncSettings.UseRealBIOS || IsDSi)
				loadFlags |= LibMelonDS.LoadFlags.USE_REAL_BIOS;
			if (skipfw && !IsDSi)
				loadFlags |= LibMelonDS.LoadFlags.SKIP_FIRMWARE;
			if (gbacartpresent)
				loadFlags |= LibMelonDS.LoadFlags.GBA_CART_PRESENT;
			if (_syncSettings.FirmwareOverride || lp.DeterministicEmulationRequested)
				loadFlags |= LibMelonDS.LoadFlags.FIRMWARE_OVERRIDE;
			if (IsDSi)
				loadFlags |= LibMelonDS.LoadFlags.IS_DSI;
			if (IsDSi && IsDSiWare)
				loadFlags |= LibMelonDS.LoadFlags.LOAD_DSIWARE;
			if (_syncSettings.ThreadedRendering)
				loadFlags |= LibMelonDS.LoadFlags.THREADED_RENDERING;

			var fwSettings = new LibMelonDS.FirmwareSettings();
			var name = Encoding.UTF8.GetBytes(_syncSettings.FirmwareUsername);
			fwSettings.FirmwareUsernameLength = name.Length;
			fwSettings.FirmwareLanguage = _syncSettings.FirmwareLanguage;
			if (!IsDSi && _syncSettings.FirmwareStartUp == NDSSyncSettings.StartUp.AutoBoot) fwSettings.FirmwareLanguage |= (NDSSyncSettings.Language)0x40;
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
				AudioBitrate = _settings.AudioBitrate,
			};
			if (_syncSettings.UseRealBIOS || IsDSi)
			{
				_exe.AddReadonlyFile(bios7, "bios7.rom");
				_exe.AddReadonlyFile(bios9, "bios9.rom");
			}
			if (IsDSi)
			{
				_exe.AddReadonlyFile(bios7i, "bios7i.rom");
				_exe.AddReadonlyFile(bios9i, "bios9i.rom");
				if (IsDSiWare)
				{
					_exe.AddReadonlyFile(roms[0], "dsiware.rom");
				}
			}
			if (fw != null)
			{
				if (IsDSi || NDSFirmware.MaybeWarnIfBadFw(fw, CoreComm)) // fw checks dont work on dsi firmware, don't bother
				{
					if (_syncSettings.FirmwareOverride || lp.DeterministicEmulationRequested)
					{
						NDSFirmware.SanitizeFw(fw);
					}
				}
				_exe.AddReadonlyFile(fw, IsDSi ? "firmwarei.bin" : "firmware.bin");
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
					if (!_core.Init(loadFlags, ref loadData, ref fwSettings))
					{
						throw new InvalidOperationException("Init returned false!");
					}
				}
			}

			if (fw != null)
			{
				_exe.RemoveReadonlyFile(IsDSi ? "firmwarei.bin" : "firmware.bin");
			}

			if (IsDSi && IsDSiWare)
			{
				_exe.RemoveReadonlyFile("dsiware.rom");
			}

			PostInit();

			((MemoryDomainList)this.AsMemoryDomains()).SystemBus = new NDSSystemBus(this.AsMemoryDomains()["ARM9 System Bus"], this.AsMemoryDomains()["ARM7 System Bus"]);

			DeterministicEmulation = lp.DeterministicEmulationRequested || (!_syncSettings.UseRealTime);
			InitializeRtc(_syncSettings.InitialTime);

			_frameThreadPtr = _core.GetFrameThreadProc();
			if (_frameThreadPtr != IntPtr.Zero)
			{
				Console.WriteLine($"Setting up waterbox thread for 0x{_frameThreadPtr:X16}");
				_frameThreadStart = CallingConventionAdapters.GetWaterboxUnsafeUnwrapped().GetDelegateForFunctionPointer<Action>(_frameThreadPtr);
				_core.SetThreadStartCallback(_threadstartcb);
			}

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
			_resampler?.Dispose();
			_resampler = null;
		}

		private static bool RomIsWare(byte[] file)
		{
			uint lowerWareTitleId = 0;
			for (int i = 0; i < 4; i++)
			{
				lowerWareTitleId <<= 8;
				lowerWareTitleId |= file[0x237 - i];
			}
			return lowerWareTitleId == 0x00030004;
		}

		private static byte[] GetTMDData(byte[] ware)
		{
			ulong titleId = 0;
			for (int i = 0; i < 8; i++)
			{
				titleId <<= 8;
				titleId |= ware[0x237 - i];
			}
			using var zip = new ZipArchive(new MemoryStream(Util.DecompressGzipFile(new MemoryStream(Resources.TMDS.Value))), ZipArchiveMode.Read, false);
			using var tmd = zip.GetEntry($"{titleId:x16}.tmd").Open();
			var ret = new byte[tmd.Length];
			tmd.Read(ret, 0, (int)tmd.Length);
			return ret;
		}

		// todo: wire this up w/ frontend
		public byte[] GetNAND()
		{
			int length = _core.GetNANDSize();

			if (length > 0)
			{
				var ret = new byte[length];
				_core.GetNANDData(ret);
				return ret;
			}

			return new byte[0];
		}

		public bool IsDSi { get; }

		public bool IsDSiWare { get; }

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

		protected override LibWaterboxCore.FrameInfo FrameAdvancePrep(IController controller, bool render, bool rendersound)
		{
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

		private readonly IntPtr _frameThreadPtr;
		private readonly Action _frameThreadStart;
		private readonly LibMelonDS.ThreadStartCallback _threadstartcb;

		private Task _frameThreadProcActive;

		private void ThreadStartCallback()
		{
			if (_frameThreadProcActive != null)
			{
				throw new InvalidOperationException("Attempted to start render thread twice");
			}
			_frameThreadProcActive = Task.Run(_frameThreadStart);
		}

		protected override void FrameAdvancePost()
		{
			_frameThreadProcActive?.Wait();
			_frameThreadProcActive = null;
		}

		protected override void LoadStateBinaryInternal(BinaryReader reader)
		{
			_core.ResetCaches();
			SetMemoryCallbacks();
			if (_frameThreadPtr != _core.GetFrameThreadProc())
			{
				throw new InvalidOperationException("_frameThreadPtr mismatch");
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
