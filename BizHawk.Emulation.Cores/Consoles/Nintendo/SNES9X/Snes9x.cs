using System;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Waterbox;
using BizHawk.Common.BizInvoke;
using System.Runtime.InteropServices;
using System.IO;
using BizHawk.Common.BufferExtensions;
using System.ComponentModel;
using BizHawk.Common;
using System.Collections.Generic;
using System.Linq;

namespace BizHawk.Emulation.Cores.Nintendo.SNES9X
{
	[CoreAttributes("Snes9x", "", true, true, "5e0319ab3ef9611250efb18255186d0dc0d7e125", "https://github.com/snes9xgit/snes9x", false)]
	[ServiceNotApplicable(typeof(IDriveLight))]
	public class Snes9x : IEmulator, IVideoProvider, ISoundProvider, IStatable,
		ISettable<Snes9x.Settings, Snes9x.SyncSettings>,
		ISaveRam, IInputPollable, IRegionable
	{
		private LibSnes9x _core;
		private PeRunner _exe;

		[CoreConstructor("SNES")]
		public Snes9x(CoreComm comm, byte[] rom, Settings settings, SyncSettings syncSettings)
		{
			ServiceProvider = new BasicServiceProvider(this);
			CoreComm = comm;
			settings = settings ?? new Settings();
			syncSettings = syncSettings ?? new SyncSettings();

			_exe = new PeRunner(new PeRunnerOptions
			{
				Path = comm.CoreFileProvider.DllPath(),
				Filename = "snes9x.wbx",
				SbrkHeapSizeKB = 1024,
				SealedHeapSizeKB = 12 * 1024,
				InvisibleHeapSizeKB = 6 * 1024,
				PlainHeapSizeKB = 64
			});

			_core = BizInvoker.GetInvoker<LibSnes9x>(_exe, _exe);
			if (!_core.biz_init())
			{
				throw new InvalidOperationException("Init() failed");
			}
			if (!_core.biz_load_rom(rom, rom.Length))
			{
				throw new InvalidOperationException("LoadRom() failed");
			}
			_exe.Seal();

			if (_core.biz_is_ntsc())
			{
				Console.WriteLine("NTSC rom loaded");
				VsyncNumerator = 21477272;
				VsyncDenominator = 357366;
				Region = DisplayType.NTSC;
			}
			else
			{
				Console.WriteLine("PAL rom loaded");
				VsyncNumerator = 21281370;
				VsyncDenominator = 425568;
				Region = DisplayType.PAL;
			}

			_nsampTarget = (int)Math.Round(44100.0 * VsyncDenominator / VsyncNumerator);
			_nsampWarn = (int)Math.Round(1.05 * 44100.0 * VsyncDenominator / VsyncNumerator);

			_syncSettings = syncSettings;
			InitControllers();
			PutSettings(settings);
			InitMemoryDomains();
			InitSaveram();

			_inputCallback = InputCallbacks.Call;
		}

		#region controller

		private readonly short[] _inputState = new short[16 * 8];
		private List<ControlDefUnMerger> _cdums;
		private readonly List<IControlDevice> _controllers = new List<IControlDevice>();

		private void InitControllers()
		{
			_core.biz_set_port_devices(_syncSettings.LeftPort, _syncSettings.RightPort);

			switch (_syncSettings.LeftPort)
			{
				case LibSnes9x.LeftPortDevice.Joypad:
					_controllers.Add(new Joypad());
					break;
			}
			switch (_syncSettings.RightPort)
			{
				case LibSnes9x.RightPortDevice.Joypad:
					_controllers.Add(new Joypad());
					break;
				case LibSnes9x.RightPortDevice.Multitap:
					_controllers.Add(new Joypad());
					_controllers.Add(new Joypad());
					_controllers.Add(new Joypad());
					_controllers.Add(new Joypad());
					break;
				case LibSnes9x.RightPortDevice.Mouse:
					_controllers.Add(new Mouse());
					break;
				case LibSnes9x.RightPortDevice.SuperScope:
					_controllers.Add(new SuperScope());
					break;
				case LibSnes9x.RightPortDevice.Justifier:
					_controllers.Add(new Justifier());
					break;
			}

			ControllerDefinition = ControllerDefinitionMerger.GetMerged(
				_controllers.Select(c => c.Definition), out _cdums);

			// add buttons that the core itself will handle
			ControllerDefinition.BoolButtons.Add("Reset");
			ControllerDefinition.BoolButtons.Add("Power");
			ControllerDefinition.Name = "SNES Controller";
		}

		private void UpdateControls(IController c)
		{
			Array.Clear(_inputState, 0, 16 * 8);
			for (int i = 0, offset = 0; i < _controllers.Count; i++, offset += 16)
			{
				_controllers[i].ApplyState(_cdums[i].UnMerge(c), _inputState, offset);
			}
		}

		private interface IControlDevice
		{
			ControllerDefinition Definition { get; }
			void ApplyState(IController controller, short[] input, int offset);
		}

		private class Joypad : IControlDevice
		{
			private static readonly string[] Buttons =
			{
				"0B",
				"0Y",
				"0Select",
				"0Start",
				"0Up",
				"0Down",
				"0Left",
				"0Right",
				"0A",
				"0X",
				"0L",
				"0R"
			};

			private static int ButtonOrder(string btn)
			{
				var order = new Dictionary<string, int>
				{
					["0Up"] = 0,
					["0Down"] = 1,
					["0Left"] = 2,
					["0Right"] = 3,

					["0Select"] = 4,
					["0Start"] = 5,

					["0Y"] = 6,
					["0B"] = 7,

					["0X"] = 8,
					["0A"] = 9,

					["0L"] = 10,
					["0R"] = 11
				};

				return order[btn];
			}

			private static readonly ControllerDefinition _definition = new ControllerDefinition
			{
				BoolButtons = Buttons.OrderBy(ButtonOrder).ToList()
			};

			public ControllerDefinition Definition { get; } = _definition;

			public void ApplyState(IController controller, short[] input, int offset)
			{
				for (int i = 0; i < Buttons.Length; i++)
					input[offset + i] = (short)(controller.IsPressed(Buttons[i]) ? 1 : 0);
			}
		}

		private abstract class Analog : IControlDevice
		{
			public abstract ControllerDefinition Definition { get; }

			public void ApplyState(IController controller, short[] input, int offset)
			{
				foreach (var s in Definition.FloatControls)
					input[offset++] = (short)(controller.GetFloat(s));
				foreach (var s in Definition.BoolButtons)
					input[offset++] = (short)(controller.IsPressed(s) ? 1 : 0);
			}
		}

		private class Mouse : Analog
		{
			private static readonly ControllerDefinition _definition = new ControllerDefinition
			{
				BoolButtons = new List<string>
				{
					"0Mouse Left",
					"0Mouse Right"
				},
				FloatControls =
				{
					"0Mouse X",
					"0Mouse Y"
				},
				FloatRanges =
				{
					new[] { -127f, 0f, 127f },
					new[] { -127f, 0f, 127f }
				}
			};

			public override ControllerDefinition Definition => _definition;
		}

		private class SuperScope : Analog
		{
			private static readonly ControllerDefinition _definition = new ControllerDefinition
			{
				BoolButtons = new List<string>
				{
					"0Trigger",
					"0Cursor",
					"0Turbo",
					"0Pause"
				},
				FloatControls =
				{
					"0Scope X",
					"0Scope Y"
				},
				FloatRanges =
				{
					// snes9x is always in 224 mode
					new[] { 0f, 128f, 256f },
					new[] { 0f, 0f, 240f }
				}
			};

			public override ControllerDefinition Definition => _definition;
		}

		private class Justifier : Analog
		{
			private static readonly ControllerDefinition _definition = new ControllerDefinition
			{
				BoolButtons = new List<string>
				{
					"0Trigger",
					"0Start",
				},
				FloatControls =
				{
					"0Justifier X",
					"0Justifier Y",
				},
				FloatRanges =
				{
					// snes9x is always in 224 mode
					new[] { 0f, 128f, 256f },
					new[] { 0f, 0f, 240f },
				}
			};

			public override ControllerDefinition Definition => _definition;
		}

		public ControllerDefinition ControllerDefinition { get; private set; }

		#endregion

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

		public DisplayType Region { get; }

		public IEmulatorServiceProvider ServiceProvider { get; }

		public void FrameAdvance(IController controller, bool render, bool rendersound = true)
		{
			_core.biz_set_input_callback(InputCallbacks.Count > 0 ? _inputCallback : null);

			if (controller.IsPressed("Power"))
				_core.biz_hard_reset();
			else if (controller.IsPressed("Reset"))
				_core.biz_soft_reset();

			UpdateControls(controller);
			Frame++;
			LibSnes9x.frame_info frame = new LibSnes9x.frame_info();

			_core.biz_run(frame, _inputState);
			IsLagFrame = frame.padread == 0;
			if (IsLagFrame)
				LagCount++;
			using (_exe.EnterExit())
			{
				Blit(frame);
				Sblit(frame);
			}
		}

		public int Frame { get; private set; }

		public void ResetCounters()
		{
			Frame = 0;
		}

		public string SystemId { get { return "SNES"; } }
		public bool DeterministicEmulation { get { return true; } }
		public CoreComm CoreComm { get; private set; }

		#region IVideoProvider

		private static readonly int[] VirtualWidths = new[] { 293, 587, 587, 587 };  // 256 512 256 512
		private static readonly int[] VirtualHeights = new[] { 224, 448, 448, 448 }; // 224 224 448 448

		private unsafe void Blit(LibSnes9x.frame_info frame)
		{
			BufferWidth = frame.vwidth;
			BufferHeight = frame.vheight;

			int vinc = frame.vpitch / sizeof(ushort) - frame.vwidth;

			ushort* src = (ushort*)frame.vptr;
			fixed (int* _dst = _vbuff)
			{
				byte* dst = (byte*)_dst;

				for (int j = 0; j < frame.vheight; j++)
				{
					for (int i = 0; i < frame.vwidth; i++)
					{
						var c = *src++;

						*dst++ = (byte)(c << 3 & 0xf8 | c >> 2 & 7);
						*dst++ = (byte)(c >> 3 & 0xfa | c >> 9 & 3);
						*dst++ = (byte)(c >> 8 & 0xf8 | c >> 13 & 7);
						*dst++ = 0xff;
					}
					src += vinc;
				}
			}

			VirtualHeight = BufferHeight;
			VirtualWidth = BufferWidth;
			if (VirtualHeight * 2 < VirtualWidth)
				VirtualHeight *= 2;
			if (VirtualHeight > 240)
				VirtualWidth = 512;
			VirtualWidth = (int)Math.Round(VirtualWidth * 1.146);
		}

		private int[] _vbuff = new int[512 * 480];
		public int[] GetVideoBuffer() { return _vbuff; }
		public int VirtualWidth { get; private set; } = 293;
		public int VirtualHeight { get; private set; } = 224;
		public int BufferWidth { get; private set; } = 256;
		public int BufferHeight { get; private set; } = 224;
		public int BackgroundColor { get { return unchecked((int)0xff000000); } }

		public int VsyncNumerator
		{
			get;
		}

		public int VsyncDenominator
		{
			get;
		}

		#endregion

		#region ISoundProvider

		private void Sblit(LibSnes9x.frame_info frame)
		{
			Marshal.Copy(frame.sptr, _sbuff, 0, frame.slen * 2);
			_nsamp = frame.slen;
			if (_nsamp > _nsampWarn)
			{
				Console.WriteLine($"Warn: Long frame! {_nsamp} > {_nsampTarget}");
			}
		}

		private readonly int _nsampWarn;
		private readonly int _nsampTarget;

		private int _nsamp;
		private short[] _sbuff = new short[8192];

		public void GetSamplesSync(out short[] samples, out int nsamp)
		{
			samples = _sbuff;
			nsamp = _nsamp;
		}

		public void DiscardSamples()
		{
			// Nothing to do
		}

		public void SetSyncMode(SyncSoundMode mode)
		{
			if (mode == SyncSoundMode.Async)
			{
				throw new NotSupportedException("Async mode is not supported.");
			}
		}

		public bool CanProvideAsync
		{
			get { return false; }
		}

		public SyncSoundMode SyncMode
		{
			get { return SyncSoundMode.Sync; }
		}

		public void GetSamplesAsync(short[] samples)
		{
			throw new InvalidOperationException("Async mode is not supported.");
		}

		#endregion

		private LibSnes9x.InputCallback _inputCallback;

		public int LagCount { get; set; }
		public bool IsLagFrame { get; set; }

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
			_core.biz_set_input_callback(null);

			_core.biz_post_load_state();
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

		#region settings

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
			_settings = o;
			int s = 0;
			if (o.PlaySound0) s |= 1;
			if (o.PlaySound0) s |= 2;
			if (o.PlaySound0) s |= 4;
			if (o.PlaySound0) s |= 8;
			if (o.PlaySound0) s |= 16;
			if (o.PlaySound0) s |= 32;
			if (o.PlaySound0) s |= 64;
			if (o.PlaySound0) s |= 128;
			_core.biz_set_sound_channels(s);
			int l = 0;
			if (o.ShowBg0) l |= 1;
			if (o.ShowBg1) l |= 2;
			if (o.ShowBg2) l |= 4;
			if (o.ShowBg3) l |= 8;
			if (o.ShowWindow) l |= 32;
			if (o.ShowTransparency) l |= 64;
			if (o.ShowSprites0) l |= 256;
			if (o.ShowSprites1) l |= 512;
			if (o.ShowSprites2) l |= 1024;
			if (o.ShowSprites3) l |= 2048;
			_core.biz_set_layers(l);

			return false; // no reboot needed
		}

		public bool PutSyncSettings(SyncSettings o)
		{
			var ret = SyncSettings.NeedsReboot(_syncSettings, o);
			_syncSettings = o;
			return ret;
		}

		public class Settings
		{
			[DefaultValue(true)]
			[DisplayName("Enable Sound Channel 1")]
			public bool PlaySound0 { get; set; }

			[DefaultValue(true)]
			[DisplayName("Enable Sound Channel 2")]
			public bool PlaySound1 { get; set; }

			[DefaultValue(true)]
			[DisplayName("Enable Sound Channel 3")]
			public bool PlaySound2 { get; set; }

			[DefaultValue(true)]
			[DisplayName("Enable Sound Channel 4")]
			public bool PlaySound3 { get; set; }

			[DefaultValue(true)]
			[DisplayName("Enable Sound Channel 5")]
			public bool PlaySound4 { get; set; }

			[DefaultValue(true)]
			[DisplayName("Enable Sound Channel 6")]
			public bool PlaySound5 { get; set; }

			[DefaultValue(true)]
			[DisplayName("Enable Sound Channel 7")]
			public bool PlaySound6 { get; set; }

			[DefaultValue(true)]
			[DisplayName("Enable Sound Channel 8")]
			public bool PlaySound7 { get; set; }

			[DefaultValue(true)]
			[DisplayName("Enable Background Layer 1")]
			public bool ShowBg0 { get; set; }

			[DefaultValue(true)]
			[DisplayName("Enable Background Layer 2")]
			public bool ShowBg1 { get; set; }

			[DefaultValue(true)]
			[DisplayName("Enable Background Layer 3")]
			public bool ShowBg2 { get; set; }

			[DefaultValue(true)]
			[DisplayName("Enable Background Layer 4")]
			public bool ShowBg3 { get; set; }

			[DefaultValue(true)]
			[DisplayName("Show Sprites Priority 1")]
			public bool ShowSprites0 { get; set; }

			[DefaultValue(true)]
			[DisplayName("Show Sprites Priority 2")]
			public bool ShowSprites1 { get; set; }

			[DefaultValue(true)]
			[DisplayName("Show Sprites Priority 3")]
			public bool ShowSprites2 { get; set; }

			[DefaultValue(true)]
			[DisplayName("Show Sprites Priority 4")]
			public bool ShowSprites3 { get; set; }

			[DefaultValue(true)]
			[DisplayName("Show Window")]
			public bool ShowWindow { get; set; }

			[DefaultValue(true)]
			[DisplayName("Show Transparency")]
			public bool ShowTransparency { get; set; }

			public Settings()
			{
				SettingsUtil.SetDefaultValues(this);
			}

			public Settings Clone()
			{
				return (Settings)MemberwiseClone();
			}
		}

		public class SyncSettings
		{
			[DefaultValue(LibSnes9x.LeftPortDevice.Joypad)]
			[DisplayName("Left Port")]
			[Description("Specifies the controller type plugged into the left controller port on the console")]
			public LibSnes9x.LeftPortDevice LeftPort { get; set; }

			[DefaultValue(LibSnes9x.RightPortDevice.Joypad)]
			[DisplayName("Right Port")]
			[Description("Specifies the controller type plugged into the right controller port on the console")]
			public LibSnes9x.RightPortDevice RightPort { get; set; }

			public SyncSettings()
			{
				SettingsUtil.SetDefaultValues(this);
			}

			public SyncSettings Clone()
			{
				return (SyncSettings)MemberwiseClone();
			}

			public static bool NeedsReboot(SyncSettings x, SyncSettings y)
			{
				// the core can handle dynamic plugging and unplugging, but that changes
				// the controllerdefinition, and we're not ready for that
				return !DeepEquality.DeepEquals(x, y);
			}
		}

		#endregion

		#region Memory Domains

		private unsafe void InitMemoryDomains()
		{
			var native = new LibSnes9x.memory_area();
			var domains = new List<MemoryDomain>();

			var names = new[] { "CARTRAM", "CARTRAM B", "RTC", "WRAM", "VRAM" };
			int index = 0;
			foreach (var s in names)
			{
				_core.biz_get_memory_area(index++, native);
				if (native.ptr != IntPtr.Zero && native.size > 0)
				{
					domains.Add(new MemoryDomainIntPtrMonitor(s, MemoryDomain.Endian.Little,
						native.ptr, native.size, true, 2, _exe));
				}
			}
			(ServiceProvider as BasicServiceProvider).Register<IMemoryDomains>(new MemoryDomainList(domains)
			{
				MainMemory = domains.Single(d => d.Name == "WRAM")
			});
		}

		#endregion

		#region ISaveRam

		private void InitSaveram()
		{
			for (int i = 0; i < 2; i++) // SRAM A, SRAM B, RTC
			{
				var native = new LibSnes9x.memory_area();
				_core.biz_get_memory_area(i, native);
				if (native.ptr != IntPtr.Zero && native.size > 0)
					_saveramMemoryAreas.Add(native);
			}
			_saveramSize = _saveramMemoryAreas.Sum(a => a.size);
		}

		private readonly List<LibSnes9x.memory_area> _saveramMemoryAreas = new List<LibSnes9x.memory_area>();

		private int _saveramSize;

		public bool SaveRamModified => _saveramSize > 0;

		public byte[] CloneSaveRam()
		{
			using (_exe.EnterExit())
			{
				var ret = new byte[_saveramSize];
				var offset = 0;
				foreach (var area in _saveramMemoryAreas)
				{
					Marshal.Copy(area.ptr, ret, offset, area.size);
					offset += area.size;
				}
				return ret;
			}
		}

		public void StoreSaveRam(byte[] data)
		{
			using (_exe.EnterExit())
			{
				if (data.Length != _saveramSize)
					throw new InvalidOperationException("Saveram size mismatch");
				var offset = 0;
				foreach (var area in _saveramMemoryAreas)
				{
					Marshal.Copy(data, offset, area.ptr, area.size);
					offset += area.size;
				}
			}
		}

		#endregion
	}
}
