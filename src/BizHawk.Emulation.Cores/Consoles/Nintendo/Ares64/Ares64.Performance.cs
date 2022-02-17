using System;
using System.IO;
using System.Linq;

using BizHawk.BizInvoke;
using BizHawk.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Properties;
using BizHawk.Emulation.Cores.Waterbox;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.Ares64.Performance
{
	[PortedCore(CoreNames.Ares64Performance, "ares team, Near", "v126", "https://ares-emulator.github.io/", singleInstance: true, isReleased: false)]
	[ServiceNotApplicable(new[] { typeof(IDriveLight), })]
	public partial class Ares64 : IEmulator, IVideoProvider, ISoundProvider, IStatable, IInputPollable, ISaveRam, IRegionable
	{
		private static readonly LibAres64Performance _core;

		static Ares64()
		{
			var resolver = new DynamicLibraryImportResolver(
				OSTailoredCode.IsUnixHost ? "libares64.so" : "libares64.dll", hasLimitedLifetime: false);
			_core = BizInvoker.GetInvoker<LibAres64Performance>(resolver, CallingConventionAdapters.Native);
		}

		private readonly BasicServiceProvider _serviceProvider;

		public IEmulatorServiceProvider ServiceProvider => _serviceProvider;

		public int Frame { get; private set; }

		public int LagCount { get; set; }

		public bool IsLagFrame { get; set; }

		[FeatureNotImplemented]
		public IInputCallbackSystem InputCallbacks => throw new NotImplementedException();

		public string SystemId => VSystemID.Raw.N64;

		public bool DeterministicEmulation => false;

		public void ResetCounters()
		{
			Frame = 0;
			LagCount = 0;
			IsLagFrame = false;
		}

		public void Dispose() => _core.Deinit();

		[CoreConstructor(VSystemID.Raw.N64)]
		public Ares64(CoreLoadParameters<object, Ares64SyncSettings> lp)
		{
			if (lp.DeterministicEmulationRequested)
			{
				lp.Comm.ShowMessage("This core is not deterministic, switch over to the Ares (Accuracy) core for deterministic movie recordings. You have been warned!");
			}

			_serviceProvider = new(this);

			_syncSettings = lp.SyncSettings ?? new();

			int upscale = _syncSettings.EnableVulkan ? (int)_syncSettings.VulkanUpscale : 1;
			_videoBuffer = new int[640 * upscale * 576 * upscale];

			ControllerSettings = new[]
			{
				_syncSettings.P1Controller,
				_syncSettings.P2Controller,
				_syncSettings.P3Controller,
				_syncSettings.P4Controller,
			};

			N64Controller = CreateControllerDefinition(ControllerSettings);

			var rom = lp.Roms[0].RomData;

			Region = rom[0x3E] switch
			{
				0x44 or 0x46 or 0x49 or 0x50 or 0x53 or 0x55 or 0x58 or 0x59 => DisplayType.PAL,
				_ => DisplayType.NTSC,
			};

			var pal = Region == DisplayType.PAL;

			VsyncNumerator = pal ? 50 : 60000;
			VsyncDenominator = pal ? 1 : 1001;

			LibAres64.LoadFlags loadFlags = 0;
			if (_syncSettings.RestrictAnalogRange)
				loadFlags |= LibAres64.LoadFlags.RestrictAnalogRange;
			if (pal)
				loadFlags |= LibAres64.LoadFlags.Pal;
			if (_syncSettings.EnableVulkan)
				loadFlags |= LibAres64.LoadFlags.UseVulkan;
			if (_syncSettings.SuperSample)
				loadFlags |= LibAres64.LoadFlags.SuperSample;

			var pif = Util.DecompressGzipFile(new MemoryStream(pal ? Resources.PIF_PAL_ROM.Value : Resources.PIF_NTSC_ROM.Value));

			unsafe
			{
				fixed (byte* pifPtr = pif, romPtr = rom)
				{
					var loadData = new LibAres64.LoadData()
					{
						PifData = (IntPtr)pifPtr,
						PifLen = pif.Length,
						RomData = (IntPtr)romPtr,
						RomLen = rom.Length,
						VulkanUpscale = upscale,
					};
					if (!_core.Init(loadData, ControllerSettings, loadFlags))
					{
						throw new InvalidOperationException("Init returned false!");
					}
				}
			}

			ResetCounters();

			var areas = new LibWaterboxCore.MemoryArea[256];
			_core.GetMemoryAreas(areas);
			_memoryAreas = areas.Where(a => a.Data != IntPtr.Zero && a.Size != 0 && !a.Flags.HasFlag(LibWaterboxCore.MemoryDomainFlags.FunctionHook))
				.ToArray();

			var memoryDomains = _memoryAreas.Select(a => new WaterboxMemoryDomainPointer(a, _monitor)).ToList();
			var primaryDomain = memoryDomains
				.Where(md => md.Definition.Flags.HasFlag(LibWaterboxCore.MemoryDomainFlags.Primary))
				.Single();

			var mdl = new MemoryDomainList(
				memoryDomains.Cast<MemoryDomain>().ToList()
			)
			{
				MainMemory = primaryDomain
			};
			_serviceProvider.Register<IMemoryDomains>(mdl);

			_saveramAreas = memoryDomains
				.Where(md => md.Definition.Flags.HasFlag(LibWaterboxCore.MemoryDomainFlags.Saverammable))
				.ToArray();
			_saveramSize = (int)_saveramAreas.Sum(a => a.Size);
		}

		public DisplayType Region { get; }

		public ControllerDefinition ControllerDefinition => N64Controller;

		private ControllerDefinition N64Controller { get; }

		public LibAres64.ControllerType[] ControllerSettings { get; }

		private static ControllerDefinition CreateControllerDefinition(LibAres64.ControllerType[] controllerSettings)
		{
			var ret = new ControllerDefinition("Nintendo 64 Controller");
			for (int i = 0; i < 4; i++)
			{
				if (controllerSettings[i] != LibAres64.ControllerType.Unplugged)
				{
					ret.BoolButtons.Add($"P{i + 1} DPad U");
					ret.BoolButtons.Add($"P{i + 1} DPad D");
					ret.BoolButtons.Add($"P{i + 1} DPad L");
					ret.BoolButtons.Add($"P{i + 1} DPad R");
					ret.BoolButtons.Add($"P{i + 1} Start");
					ret.BoolButtons.Add($"P{i + 1} Z");
					ret.BoolButtons.Add($"P{i + 1} B");
					ret.BoolButtons.Add($"P{i + 1} A");
					ret.BoolButtons.Add($"P{i + 1} C Up");
					ret.BoolButtons.Add($"P{i + 1} C Down");
					ret.BoolButtons.Add($"P{i + 1} C Left");
					ret.BoolButtons.Add($"P{i + 1} C Right");
					ret.BoolButtons.Add($"P{i + 1} L");
					ret.BoolButtons.Add($"P{i + 1} R");
					ret.AddXYPair($"P{i + 1} {{0}} Axis", AxisPairOrientation.RightAndUp, (-128).RangeTo(127), 0);
					if (controllerSettings[i] == LibAres64.ControllerType.Rumblepak)
					{
						ret.HapticsChannels.Add($"P{i + 1} Rumble Pak");
					}
				}
			}
			ret.BoolButtons.Add("Reset");
			ret.BoolButtons.Add("Power");
			return ret.MakeImmutable();
		}

		private static LibAres64.Buttons GetButtons(IController controller, int num)
		{
			LibAres64.Buttons ret = 0;

			if (controller.IsPressed($"P{num} DPad U"))
				ret |= LibAres64.Buttons.UP;
			if (controller.IsPressed($"P{num} DPad D"))
				ret |= LibAres64.Buttons.DOWN;
			if (controller.IsPressed($"P{num} DPad L"))
				ret |= LibAres64.Buttons.LEFT;
			if (controller.IsPressed($"P{num} DPad R"))
				ret |= LibAres64.Buttons.RIGHT;
			if (controller.IsPressed($"P{num} B"))
				ret |= LibAres64.Buttons.B;
			if (controller.IsPressed($"P{num} A"))
				ret |= LibAres64.Buttons.A;
			if (controller.IsPressed($"P{num} C Up"))
				ret |= LibAres64.Buttons.C_UP;
			if (controller.IsPressed($"P{num} C Down"))
				ret |= LibAres64.Buttons.C_DOWN;
			if (controller.IsPressed($"P{num} C Left"))
				ret |= LibAres64.Buttons.C_LEFT;
			if (controller.IsPressed($"P{num} C Right"))
				ret |= LibAres64.Buttons.C_RIGHT;
			if (controller.IsPressed($"P{num} L"))
				ret |= LibAres64.Buttons.L;
			if (controller.IsPressed($"P{num} R"))
				ret |= LibAres64.Buttons.R;
			if (controller.IsPressed($"P{num} Z"))
				ret |= LibAres64.Buttons.Z;
			if (controller.IsPressed($"P{num} Start"))
				ret |= LibAres64.Buttons.START;

			return ret;
		}

		private LibWaterboxCore.FrameInfo FrameAdvancePrep(IController controller, bool render, bool rendersound)
		{
			for (int i = 0; i < 4; i++)
			{
				if (ControllerSettings[i] == LibAres64.ControllerType.Rumblepak)
				{
					controller.SetHapticChannelStrength($"P{i + 1} Rumble Pak", _core.GetRumbleStatus(i) ? int.MaxValue : 0);
				}
			}

			return new LibAres64.FrameInfo
			{
				P1Buttons = GetButtons(controller, 1),
				P1XAxis = (short)controller.AxisValue("P1 X Axis"),
				P1YAxis = (short)controller.AxisValue("P1 Y Axis"),

				P2Buttons = GetButtons(controller, 2),
				P2XAxis = (short)controller.AxisValue("P2 X Axis"),
				P2YAxis = (short)controller.AxisValue("P2 Y Axis"),

				P3Buttons = GetButtons(controller, 3),
				P3XAxis = (short)controller.AxisValue("P3 X Axis"),
				P3YAxis = (short)controller.AxisValue("P3 Y Axis"),

				P4Buttons = GetButtons(controller, 4),
				P4XAxis = (short)controller.AxisValue("P4 X Axis"),
				P4YAxis = (short)controller.AxisValue("P4 Y Axis"),

				Reset = controller.IsPressed("Reset"),
				Power = controller.IsPressed("Power"),
			};
		}

		public unsafe bool FrameAdvance(IController controller, bool render, bool rendersound = true)
		{
			_core.SetInputCallback(null);

			fixed (int* vp = _videoBuffer)
			fixed (short* sp = _soundBuffer)
			{
				var frame = FrameAdvancePrep(controller, render, rendersound);
				frame.VideoBuffer = (IntPtr)vp;
				frame.SoundBuffer = (IntPtr)sp;

				_core.FrameAdvance(frame);

				Frame++;
				if (IsLagFrame = frame.Lagged != 0)
					LagCount++;

				if (render)
				{
					BufferWidth = frame.Width;
					BufferHeight = frame.Height;
				}
				if (rendersound)
				{
					_numSamples = frame.Samples;
				}
				else
				{
					_numSamples = 0;
				}

				FrameAdvancePost();
			}

			return true;
		}

		private void FrameAdvancePost()
		{
			if (BufferWidth == 1 && BufferHeight == 1)
			{
				BufferWidth = 640;
				BufferHeight = 480;
				_blankFrame = true;
			}
			else
			{
				_blankFrame = false;
			}
		}

		public int[] GetVideoBuffer() => _blankFrame ? _blankBuffer : _videoBuffer;

		private bool _blankFrame;

		private readonly int[] _blankBuffer = new int[640 * 480];

		private readonly int[] _videoBuffer;

		public int VirtualWidth => 640;

		public int VirtualHeight => 480;

		public int BufferWidth { get; private set; }

		public int BufferHeight { get; private set; }

		public int VsyncNumerator { get; }

		public int VsyncDenominator { get; }

		public int BackgroundColor => unchecked((int)0xff000000);

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

		public void GetSamplesAsync(short[] samples) => throw new InvalidOperationException("Async mode is not supported.");

		public void DiscardSamples() {}

		private readonly short[] _soundBuffer = new short[2048 * 2];

		private int _numSamples;

		public bool CanProvideAsync => false;

		public SyncSoundMode SyncMode => SyncSoundMode.Sync;

		private byte[] _stateBuffer = new byte[0];

		public void SaveStateBinary(BinaryWriter writer)
		{
			var len = _core.SerializeSize();
			if (len != _stateBuffer.Length)
			{
				_stateBuffer = new byte[len];
			}
			_core.Serialize(_stateBuffer);
			writer.Write(_stateBuffer.Length);
			writer.Write(_stateBuffer);

			// other variables
			writer.Write(Frame);
			writer.Write(LagCount);
			writer.Write(IsLagFrame);
		}

		public void LoadStateBinary(BinaryReader reader)
		{
			var len = reader.ReadInt32();
			if (len != _core.SerializeSize())
			{
				throw new InvalidOperationException("Savestate size mismatch!");
			}
			if (len != _stateBuffer.Length)
			{
				_stateBuffer = new byte[len];
			}
			reader.Read(_stateBuffer, 0, len);
			if (!_core.Unserialize(_stateBuffer, len))
			{
				throw new Exception($"{nameof(_core.Unserialize)}() returned false!");
			}

			// other variables
			Frame = reader.ReadInt32();
			LagCount = reader.ReadInt32();
			IsLagFrame = reader.ReadBoolean();
		}

		private readonly LibWaterboxCore.MemoryArea[] _memoryAreas;

		private readonly WaterboxMemoryDomain[] _saveramAreas;
		private readonly int _saveramSize;

		public unsafe bool SaveRamModified
		{
			get
			{
				if (_saveramSize == 0)
					return false;
				var buff = new byte[4096];
				fixed (byte* bp = buff)
				{
					foreach (var area in _saveramAreas)
					{
						var stream = new MemoryDomainStream(area);
						int cmp = (area.Definition.Flags & LibWaterboxCore.MemoryDomainFlags.OneFilled) != 0 ? -1 : 0;
						while (true)
						{
							int nread = stream.Read(buff, 0, 4096);
							if (nread == 0)
								break;

							int* p = (int*)bp;
							int* pend = p + nread / sizeof(int);
							while (p < pend)
							{
								if (*p++ != cmp)
									return true;
							}
						}
					}
				}
				return false;
			}
		}

		public byte[] CloneSaveRam()
		{
			if (_saveramSize == 0)
				return null;
			var ret = new byte[_saveramSize];
			var dest = new MemoryStream(ret, true);
			foreach (var area in _saveramAreas)
			{
				new MemoryDomainStream(area).CopyTo(dest);
			}
			return ret;
		}

		public void StoreSaveRam(byte[] data)
		{
			if (data.Length != _saveramSize)
				throw new InvalidOperationException("Saveram size mismatch");
			var source = new MemoryStream(data, false);
			foreach (var area in _saveramAreas)
			{
				WaterboxUtils.CopySome(source, new MemoryDomainStream(area), area.Size);
			}
		}

		private readonly DummyMonitor _monitor = new();

		private class DummyMonitor : IMonitor
		{
			public void Enter() { }

			public void Exit() { }
		}
	}
}
