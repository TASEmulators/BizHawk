using BizHawk.Common;
using BizHawk.Common.BizInvoke;
using BizHawk.Common.BufferExtensions;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Waterbox;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.VB
{
	[CoreAttributes("Virtual Boyee", "Ryphecha", true, true, "0.9.44.1",
		"https://mednafen.github.io/releases/", false)]
	public class VirtualBoyee : IEmulator, IVideoProvider, ISoundProvider, IStatable,
		IInputPollable, ISaveRam, ISettable<VirtualBoyee.Settings, VirtualBoyee.SyncSettings>
	{
		private PeRunner _exe;
		private LibVirtualBoyee _boyee;

		[CoreConstructor("VB")]
		public VirtualBoyee(CoreComm comm, byte[] rom, Settings settings, SyncSettings syncSettings)
		{
			ServiceProvider = new BasicServiceProvider(this);
			CoreComm = comm;

			_settings = settings ?? new Settings();
			_syncSettings = syncSettings ?? new SyncSettings();
			// TODO: the way settings work in this core, changing the non-sync ones will invalidate savestates
			var nativeSettings = LibVirtualBoyee.NativeSettings.FromFrontendSettings(_settings, _syncSettings);

			_exe = new PeRunner(new PeRunnerOptions
			{
				Path = comm.CoreFileProvider.DllPath(),
				Filename = "vb.wbx",
				SbrkHeapSizeKB = 256,
				SealedHeapSizeKB = 4 * 1024,
				InvisibleHeapSizeKB = 256,
				PlainHeapSizeKB = 256
			});

			_boyee = BizInvoker.GetInvoker<LibVirtualBoyee>(_exe, _exe);

			if (!_boyee.Load(rom, rom.Length, nativeSettings))
			{
				throw new InvalidOperationException("Core rejected the rom");
			}

			_exe.Seal();

			_inputCallback = InputCallbacks.Call;
			InitMemoryDomains();
			InitSaveram();
		}

		private bool _disposed = false;

		public void Dispose()
		{
			if (!_disposed)
			{
				_exe.Dispose();
				_exe = null;
				_disposed = true;
			}
		}

		public IEmulatorServiceProvider ServiceProvider { get; private set; }

		public unsafe void FrameAdvance(IController controller, bool render, bool rendersound = true)
		{
			_boyee.SetInputCallback(InputCallbacks.Count > 0 ? _inputCallback : null);

			if (controller.IsPressed("Power"))
				_boyee.HardReset();

			fixed (int* vp = _videoBuffer)
			fixed (short* sp = _soundBuffer)
			{
				var spec = new LibVirtualBoyee.EmulateSpec
				{
					Pixels = (IntPtr)vp,
					SoundBuf = (IntPtr)sp,
					SoundBufMaxSize = _soundBuffer.Length / 2,
					Buttons = GetButtons(controller)
				};

				_boyee.Emulate(spec);
				BufferWidth = spec.DisplayRect.W;
				BufferHeight = spec.DisplayRect.H;
				_numSamples = spec.SoundBufSize;

				Frame++;

				IsLagFrame = spec.Lagged;
				if (IsLagFrame)
					LagCount++;
			}
		}

		public int Frame { get; private set; }

		public void ResetCounters()
		{
			Frame = 0;
		}

		public string SystemId { get { return "VB"; } }
		public bool DeterministicEmulation { get { return true; } }
		public CoreComm CoreComm { get; private set; }

		#region Controller

		private LibVirtualBoyee.Buttons GetButtons(IController c)
		{
			var ret = 0;
			var val = 1;
			foreach (var s in CoreButtons)
			{
				if (c.IsPressed(s))
					ret |= val;
				val <<= 1;
			}
			return (LibVirtualBoyee.Buttons)ret;
		}

		private static readonly string[] CoreButtons =
		{
			"A", "B", "R", "L",
			"R_Up", "R_Right",
			"L_Right", "L_Left", "L_Down", "L_Up",
			"Start", "Select", "R_Left", "R_Down"
		};

		private static readonly Dictionary<string, int> _buttonOrdinals = new Dictionary<string, int>
		{
			["L_Up"] = 1,
			["L_Down"] = 2,
			["L_Left"] = 3,
			["L_Right"] = 4,
			["R_Up"] = 5,
			["R_Down"] = 6,
			["R_Left"] = 7,
			["R_Right"] = 8,
			["B"] = 9,
			["A"] = 10,
			["L"] = 11,
			["R"] = 12,
			["Select"] = 13,
			["Start"] = 14
		};

		private static readonly ControllerDefinition VirtualBoyController = new ControllerDefinition
		{
			Name = "VirtualBoy Controller",
			BoolButtons = CoreButtons
				.OrderBy(b => _buttonOrdinals[b])
				.Concat(new[] { "Power" })
				.ToList()
		};

		public ControllerDefinition ControllerDefinition => VirtualBoyController;

		#endregion

		#region IVideoProvider

		private int[] _videoBuffer = new int[1024 * 1024];

		public int[] GetVideoBuffer()
		{
			return _videoBuffer;
		}

		public int VirtualWidth => BufferWidth;
		public int VirtualHeight => BufferWidth;

		public int BufferWidth { get; private set; } = 384;
		public int BufferHeight { get; private set; } = 224;

		public int VsyncNumerator { get; private set; } = 20000000;

		public int VsyncDenominator { get; private set; } = 397824;

		public int BackgroundColor => unchecked((int)0xff000000);

		#endregion

		#region ISoundProvider

		private short[] _soundBuffer = new short[16384];
		private int _numSamples;

		public void SetSyncMode(SyncSoundMode mode)
		{
			if (mode == SyncSoundMode.Async)
			{
				throw new NotSupportedException("Async mode is not supported.");
			}
		}

		public void GetSamplesSync(out short[] samples, out int nsamp)
		{
			samples = _soundBuffer;
			nsamp = _numSamples;
		}

		public void GetSamplesAsync(short[] samples)
		{
			throw new InvalidOperationException("Async mode is not supported.");
		}

		public void DiscardSamples()
		{
		}

		public bool CanProvideAsync => false;

		public SyncSoundMode SyncMode => SyncSoundMode.Sync;

		#endregion

		public int LagCount { get; set; }
		public bool IsLagFrame { get; set; }

		private LibVirtualBoyee.InputCallback _inputCallback;

		public IInputCallbackSystem InputCallbacks { get; } = new InputCallbackSystem();

		#region IStatable

		public bool BinarySaveStatesPreferred
		{
			get { return true; }
		}

		public void SaveStateText(TextWriter writer)
		{
			var temp = SaveStateBinary();
			temp.SaveAsHexFast(writer);
			// write extra copy of stuff we don't use
			writer.WriteLine("Frame {0}", Frame);
		}

		public void LoadStateText(TextReader reader)
		{
			string hex = reader.ReadLine();
			byte[] state = new byte[hex.Length / 2];
			state.ReadFromHexFast(hex);
			LoadStateBinary(new BinaryReader(new MemoryStream(state)));
		}

		public void LoadStateBinary(BinaryReader reader)
		{
			_exe.LoadStateBinary(reader);
			// other variables
			Frame = reader.ReadInt32();
			LagCount = reader.ReadInt32();
			IsLagFrame = reader.ReadBoolean();
			// any managed pointers that we sent to the core need to be resent now!
			_boyee.SetInputCallback(null);
		}

		public void SaveStateBinary(BinaryWriter writer)
		{
			_exe.SaveStateBinary(writer);
			// other variables
			writer.Write(Frame);
			writer.Write(LagCount);
			writer.Write(IsLagFrame);
		}

		public byte[] SaveStateBinary()
		{
			var ms = new MemoryStream();
			var bw = new BinaryWriter(ms);
			SaveStateBinary(bw);
			bw.Flush();
			ms.Close();
			return ms.ToArray();
		}

		#endregion

		#region Memory Domains

		private unsafe void InitMemoryDomains()
		{
			var domains = new List<MemoryDomain>();

			var domainInfo = new[]
			{
				new { name = "WRAM", area = LibVirtualBoyee.MemoryArea.Wram, writable = true },
				new { name = "CARTRAM", area = LibVirtualBoyee.MemoryArea.Sram, writable = true },
				new { name = "ROM", area = LibVirtualBoyee.MemoryArea.Rom, writable = false }
			};

			foreach (var a in domainInfo)
			{
				IntPtr ptr = IntPtr.Zero;
				int size = 0;

				_boyee.GetMemoryArea(a.area, ref ptr, ref size);

				if (ptr != IntPtr.Zero && size > 0)
				{
					domains.Add(new MemoryDomainIntPtrMonitor(a.name, MemoryDomain.Endian.Little,
						ptr, size, a.writable, 4, _exe));
				}
			}
			(ServiceProvider as BasicServiceProvider).Register<IMemoryDomains>(new MemoryDomainList(domains));
		}

		#endregion

		#region ISettable

		public class SyncSettings
		{
			[DefaultValue(false)]
			[Description("Reduce input latency.  Works with all known commercial games, may have homebrew issues.")]
			public bool InstantReadHack { get; set; }
			[DefaultValue(false)]
			[Description("Disable parallax for rendering.")]
			public bool DisableParallax { get; set; }

			public SyncSettings Clone()
			{
				return (SyncSettings)MemberwiseClone();
			}

			public static bool NeedsReboot(SyncSettings x, SyncSettings y)
			{
				return !DeepEquality.DeepEquals(x, y);
			}

			public SyncSettings()
			{
				SettingsUtil.SetDefaultValues(this);
			}
		}

		public class Settings
		{
			public enum ThreeDeeModes : int
			{
				Anaglyph = 0,
				CyberScope = 1,
				SideBySide = 2,
				//OverUnder,
				VerticalInterlaced = 4,
				HorizontalInterlaced = 5
			}

			[DefaultValue(ThreeDeeModes.Anaglyph)]
			[Description("How to display the 3d image.  Use whichever method works with your VR hardware.")]
			public ThreeDeeModes ThreeDeeMode { get; set; }

			[DefaultValue(false)]
			[Description("Swap the left and right views.")]
			public bool SwapViews { get; set; }

			public enum AnaglyphPresets : int
			{
				Custom,
				RedBlue,
				RedCyan,
				RedElectricCyan,
				RedGreen,
				GreenMagneto,
				YellowBlue
			}

			[DefaultValue(AnaglyphPresets.RedBlue)]
			[Description("Color preset for Anaglyph mode.")]
			public AnaglyphPresets AnaglyphPreset { get; set; }

			[DefaultValue(typeof(Color), "Green")]
			[Description("Left anaglyph color.  Ignored unless Preset is Custom.")]
			public Color AnaglyphCustomLeftColor { get; set; }
			[DefaultValue(typeof(Color), "Purple")]
			[Description("Right anaglyph color.  Ignored unless Preset is Custom.")]
			public Color AnaglyphCustomRightColor { get; set; }

			[DefaultValue(typeof(Color), "White")]
			[Description("Display color for all of the non-anaglyph modes.  Real hardware was red, but other colors may be easier on your eyes.")]
			public Color NonAnaglyphColor { get; set; }

			[DefaultValue(1750)]
			[Range(1000, 2000)]
			[Description("LED gamma ramp.  Range of 1000 to 2000")]
			public int LedOnScale { get; set; }

			[DefaultValue(2)]
			[Range(1, 10)]
			public int InterlacePrescale { get; set; }

			[DefaultValue(0)]
			[Range(0, 1024)]
			[Description("How many pixels to put between views in Side By Side mode")]
			public int SideBySideSeparation { get; set; }

			public Settings Clone()
			{
				return (Settings)MemberwiseClone();
			}

			public static bool NeedsReboot(Settings x, Settings y)
			{
				return !DeepEquality.DeepEquals(x, y);
			}

			public Settings()
			{
				SettingsUtil.SetDefaultValues(this);
			}
		}

		private Settings _settings;
		private SyncSettings _syncSettings;

		public Settings GetSettings()
		{
			return _settings.Clone();
		}

		public SyncSettings GetSyncSettings()
		{
			return _syncSettings.Clone();
		}

		public bool PutSettings(Settings o)
		{
			var ret = Settings.NeedsReboot(_settings, o);
			_settings = o;
			return ret;
		}

		public bool PutSyncSettings(SyncSettings o)
		{
			var ret = SyncSettings.NeedsReboot(_syncSettings, o);
			_syncSettings = o;
			return ret;
		}

		#endregion

		#region ISaveRam

		private const int SaveramSize = 65536;
		private IntPtr _saveRamPtr;

		private void InitSaveram()
		{
			int unused = 0;
			_boyee.GetMemoryArea(LibVirtualBoyee.MemoryArea.Sram, ref _saveRamPtr, ref unused);
		}

		public unsafe bool SaveRamModified
		{
			get
			{
				using (_exe.EnterExit())
				{
					int* p = (int*)_saveRamPtr;
					int* pend = p + SaveramSize / sizeof(int);

					while (p < pend)
					{
						if (*p++ != 0)
							return true;
					}
				}
				return false;
			}
		}

		public byte[] CloneSaveRam()
		{
			using (_exe.EnterExit())
			{
				var ret = new byte[SaveramSize];
				Marshal.Copy(_saveRamPtr, ret, 0, SaveramSize);
				return ret;
			}
		}

		public void StoreSaveRam(byte[] data)
		{
			using (_exe.EnterExit())
			{
				if (data.Length != SaveramSize)
					throw new InvalidOperationException("Saveram size mismatch");
				Marshal.Copy(data, 0, _saveRamPtr, SaveramSize);
			}
		}

		#endregion ISaveRam
	}
}
