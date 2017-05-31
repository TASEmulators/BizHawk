using BizHawk.Common.BizInvoke;
using BizHawk.Common.BufferExtensions;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Waterbox;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BizHawk.Emulation.Cores.Consoles.SNK
{
	[CoreAttributes("NeoPop", "Thomas Klausner", true, false, "0.9.44.1", 
		"https://mednafen.github.io/releases/", false)]
	public class NeoGeoPort : IEmulator, IVideoProvider, ISoundProvider, IStatable, IInputPollable
	{
		private PeRunner _exe;
		private LibNeoGeoPort _neopop;

		[CoreConstructor("NGP")]
		public NeoGeoPort(CoreComm comm, byte[] rom)
		{
			ServiceProvider = new BasicServiceProvider(this);
			CoreComm = comm;

			_exe = new PeRunner(new PeRunnerOptions
			{
				Path = comm.CoreFileProvider.DllPath(),
				Filename = "ngp.wbx",
				SbrkHeapSizeKB = 256,
				SealedHeapSizeKB = 10 * 1024, // must be a bit larger than twice the ROM size
				InvisibleHeapSizeKB = 4,
				PlainHeapSizeKB = 4
			});

			_neopop = BizInvoker.GetInvoker<LibNeoGeoPort>(_exe, _exe);

			if (!_neopop.LoadSystem(rom, rom.Length, 1))
			{
				throw new InvalidOperationException("Core rejected the rom");
			}

			_exe.Seal();

			_inputCallback = InputCallbacks.Call;
			InitMemoryDomains();
		}

		public unsafe void FrameAdvance(IController controller, bool render, bool rendersound = true)
		{
			_neopop.SetInputCallback(InputCallbacks.Count > 0 ? _inputCallback : null);

			if (controller.IsPressed("Power"))
				_neopop.HardReset();

			fixed (int* vp = _videoBuffer)
			fixed (short* sp = _soundBuffer)
			{
				var spec = new LibNeoGeoPort.EmulateSpec
				{
					Pixels = (IntPtr)vp,
					SoundBuff = (IntPtr)sp,
					SoundBufMaxSize = _soundBuffer.Length / 2,
					Buttons = GetButtons(controller),
					SkipRendering = render ? 0 : 1
				};

				_neopop.FrameAdvance(spec);
				_numSamples = spec.SoundBufSize;

				Frame++;

				IsLagFrame = spec.Lagged != 0;
				if (IsLagFrame)
					LagCount++;
			}
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

		public int Frame { get; private set; }
		public int LagCount { get; set; }
		public bool IsLagFrame { get; set; }
		public IInputCallbackSystem InputCallbacks { get; } = new InputCallbackSystem();
		private LibNeoGeoPort.InputCallback _inputCallback;

		public void ResetCounters()
		{
			Frame = 0;
		}

		public IEmulatorServiceProvider ServiceProvider { get; private set; }
		public string SystemId { get { return "NGP"; } }
		public bool DeterministicEmulation { get { return true; } }
		public CoreComm CoreComm { get; }

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
			_neopop.SetInputCallback(null);
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

		#region Controller

		private static int GetButtons(IController c)
		{
			var ret = 0;
			var val = 1;
			foreach (var s in CoreButtons)
			{
				if (c.IsPressed(s))
					ret |= val;
				val <<= 1;
			}
			return ret;
		}

		private static readonly string[] CoreButtons =
		{
			"Up", "Down", "Left", "Right", "A", "B", "Option"
		};

		private static readonly Dictionary<string, int> ButtonOrdinals = new Dictionary<string, int>
		{
			["Up"] = 1,
			["Down"] = 2,
			["Left"] = 3,
			["Right"] = 4,
			["B"] = 9,
			["A"] = 10,
			["R"] = 11,
			["L"] = 12,
			["Option"] = 13
		};

		private static readonly ControllerDefinition NeoGeoPortableController = new ControllerDefinition
		{
			Name = "NeoGeo Portable Controller",
			BoolButtons = CoreButtons
				.OrderBy(b => ButtonOrdinals[b])
				.Concat(new[] { "Power" })
				.ToList()
		};

		public ControllerDefinition ControllerDefinition => NeoGeoPortableController;

		#endregion

		#region IVideoProvider

		private int[] _videoBuffer = new int[160 * 152];

		public int[] GetVideoBuffer()
		{
			return _videoBuffer;
		}

		public int VirtualWidth => 160;
		public int VirtualHeight => 152;
		public int BufferWidth => 160;
		public int BufferHeight => 152;
		public int VsyncNumerator { get; private set; } = 6144000;
		public int VsyncDenominator { get; private set; } = 515 * 198;
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

		#region Memory Domains

		private unsafe void InitMemoryDomains()
		{
			var domains = new List<MemoryDomain>();

			var domainNames = new[] { "RAM", "ROM", "ORIGINAL ROM" };

			foreach (var a in domainNames.Select((s, i) => new { s, i }))
			{
				IntPtr ptr = IntPtr.Zero;
				int size = 0;
				bool writable = false;

				_neopop.GetMemoryArea(a.i, ref ptr, ref size, ref writable);

				if (ptr != IntPtr.Zero && size > 0)
				{
					domains.Add(new MemoryDomainIntPtrMonitor(a.s, MemoryDomain.Endian.Little,
						ptr, size, writable, 4, _exe));
				}
			}
			(ServiceProvider as BasicServiceProvider).Register<IMemoryDomains>(new MemoryDomainList(domains));
		}

		#endregion
	}
}
