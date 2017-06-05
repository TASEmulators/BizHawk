using BizHawk.Common.BizInvoke;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Waterbox;
using BizHawk.Emulation.DiscSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace BizHawk.Emulation.Cores.Consoles.Sega.Saturn
{
	[CoreAttributes("Saturnus", "Ryphecha", true, false, "0.9.44.1",
		"https://mednafen.github.io/releases/", false)]
	public class Saturnus : IEmulator, IVideoProvider, ISoundProvider, IInputPollable
	{
		private PeRunner _exe;
		private LibSaturnus _core;
		private Disc[] _disks;
		private DiscSectorReader[] _diskReaders;
		private bool _isPal;
		private SaturnusControllerDeck _controllerDeck;

		public Saturnus(CoreComm comm, IEnumerable<Disc> disks)
		{
			ServiceProvider = new BasicServiceProvider(this);
			CoreComm = comm;

			_disks = disks.ToArray();
			_diskReaders = disks.Select(d => new DiscSectorReader(d)).ToArray();
			InitCallbacks();

			_exe = new PeRunner(new PeRunnerOptions
			{
				Path = comm.CoreFileProvider.DllPath(),
				Filename = "ss.wbx",
				SbrkHeapSizeKB = 32 * 1024,
				SealedHeapSizeKB = 32 * 1024,
				InvisibleHeapSizeKB = 32 * 1024,
				MmapHeapSizeKB = 32 * 1024,
				PlainHeapSizeKB = 32 * 1024,
				StartAddress = LibSaturnus.StartAddress
			});
			_core = BizInvoker.GetInvoker<LibSaturnus>(_exe, _exe);

			SetFirmwareCallbacks();
			SetCdCallbacks();
			if (!_core.Init(_disks.Length))
				throw new InvalidOperationException("Core rejected the disks!");
			ClearAllCallbacks();

			_controllerDeck = new SaturnusControllerDeck(new[] { false, false },
				new[] { SaturnusControllerDeck.Device.Gamepad, SaturnusControllerDeck.Device.None },
				_core);
			ControllerDefinition = _controllerDeck.Definition;
			ControllerDefinition.Name = "Saturn Controller";
			ControllerDefinition.BoolButtons.Add("Power");
			ControllerDefinition.BoolButtons.Add("Reset"); // not yet hooked up

			_exe.Seal();
			SetCdCallbacks();
			_core.SetDisk(0, false);
		}

		public unsafe void FrameAdvance(IController controller, bool render, bool rendersound = true)
		{
			if (controller.IsPressed("Power"))
				_core.HardReset();

			fixed (short* _sp = _soundBuffer)
			fixed (int* _vp = _videoBuffer)
			fixed (byte* _cp = _controllerDeck.Poll(controller))
			{
				var info = new LibSaturnus.FrameAdvanceInfo
				{
					SoundBuf = (IntPtr)_sp,
					SoundBufMaxSize = _soundBuffer.Length / 2,
					Pixels = (IntPtr)_vp,
					Controllers = (IntPtr)_cp
				};

				_core.FrameAdvance(info);
				Frame++;
				_numSamples = info.SoundBufSize;
				BufferWidth = info.Width;
				BufferHeight = info.Height;
			}
		}

		private bool _disposed = false;

		public void Dispose()
		{
			if (!_disposed)
			{
				_exe.Dispose();
				_exe = null;
				_core = null;
				_disposed = true;
			}
		}

		public int Frame { get; private set; }
		public int LagCount { get; set; }
		public bool IsLagFrame { get; set; }
		public IInputCallbackSystem InputCallbacks { get; } = new InputCallbackSystem();

		public void ResetCounters()
		{
			Frame = 0;
		}

		public IEmulatorServiceProvider ServiceProvider { get; private set; }
		public string SystemId { get { return "SAT"; } }
		public bool DeterministicEmulation { get; private set; }
		public CoreComm CoreComm { get; }
		public ControllerDefinition ControllerDefinition { get; }

		#region Callbacks

		private LibSaturnus.FirmwareSizeCallback _firmwareSizeCallback;
		private LibSaturnus.FirmwareDataCallback _firmwareDataCallback;
		private LibSaturnus.CDTOCCallback _cdTocCallback;
		private LibSaturnus.CDSectorCallback _cdSectorCallback;

		private void InitCallbacks()
		{
			_firmwareSizeCallback = FirmwareSize;
			_firmwareDataCallback = FirmwareData;
			_cdTocCallback = CDTOCCallback;
			_cdSectorCallback = CDSectorCallback;
		}

		private void SetFirmwareCallbacks()
		{
			_core.SetFirmwareCallbacks(_firmwareSizeCallback, _firmwareDataCallback);
		}
		private void SetCdCallbacks()
		{
			_core.SetCDCallbacks(_cdTocCallback, _cdSectorCallback);
		}
		private void ClearAllCallbacks()
		{
			_core.SetFirmwareCallbacks(null, null);
			_core.SetCDCallbacks(null, null);
		}

		private string TranslateFirmwareName(string filename)
		{
			switch (filename)
			{
				case "ss.cart.kof95_path":
				case "ss.cart.ultraman_path":
					// this may be moved to an xmlloader thing, and not the firmware interface
					throw new NotImplementedException();
				case "BIOS_J":
				case "BIOS_A":
					return "J";
				case "BIOS_E":
					_isPal = true;
					return "E";
				case "BIOS_U":
					return "U";
				default:
					throw new InvalidOperationException("Unknown BIOS file");
			}
		}
		private byte[] GetFirmware(string filename)
		{
			return CoreComm.CoreFileProvider.GetFirmware("SAT", TranslateFirmwareName(filename), true);
		}

		private int FirmwareSize(string filename)
		{
			return GetFirmware(filename).Length;
		}
		private void FirmwareData(string filename, IntPtr dest)
		{
			var data = GetFirmware(filename);
			Marshal.Copy(data, 0, dest, data.Length);
		}

		private void CDTOCCallback(int disk, [In, Out]LibSaturnus.TOC t)
		{
			// everything that's not commented, we're sure about
			var tin = _disks[disk].TOC;
			t.FirstTrack = tin.FirstRecordedTrackNumber;
			t.LastTrack = tin.LastRecordedTrackNumber;
			t.DiskType = (int)tin.Session1Format;
			for (int i = 0; i < 101; i++)
			{
				t.Tracks[i].Adr = 1; // ????
				t.Tracks[i].Lba = tin.TOCItems[i].LBA;
				t.Tracks[i].Control = (int)tin.TOCItems[i].Control;
				t.Tracks[i].Valid = tin.TOCItems[i].Exists ? 1 : 0;
			}
		}
		private void CDSectorCallback(int disk, int lba, IntPtr dest)
		{
			Console.WriteLine("servicing " + lba);
			var buff = new byte[2448];
			_diskReaders[disk].ReadLBA_2448(lba, buff, 0);
			Marshal.Copy(buff, 0, dest, 2448);
		}

		#endregion

		#region IVideoProvider

		private int[] _videoBuffer = new int[1024 * 1024];

		public int[] GetVideoBuffer()
		{
			return _videoBuffer;
		}

		private const int PalFpsNum = 1734687500;
		private const int PalFpsDen = 61 * 455 * 1251;
		private const int NtscFpsNum = 1746818182; // 1746818181.8
		private const int NtscFpsDen = 61 * 455 * 1051;

		public int VirtualWidth => BufferWidth; // TODO
		public int VirtualHeight => BufferHeight; // TODO
		public int BufferWidth { get; private set; } = 320;
		public int BufferHeight { get; private set; } = 240;
		public int VsyncNumerator => _isPal ? PalFpsNum : NtscFpsNum;
		public int VsyncDenominator => _isPal ? PalFpsDen : NtscFpsDen;
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
	}
}
