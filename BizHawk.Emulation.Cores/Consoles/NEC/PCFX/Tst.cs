using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Consoles.Sega.Saturn;
using BizHawk.Emulation.Cores.Waterbox;
using BizHawk.Emulation.DiscSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace BizHawk.Emulation.Cores.Consoles.NEC.PCFX
{
	[CoreAttributes("T. S. T.", "Mednafen Team", true, false, "0.9.44.1",
		"https://mednafen.github.io/releases/", false)]
	public class Tst : WaterboxCore, IDriveLight
	{
		private static readonly DiscSectorReaderPolicy _diskPolicy = new DiscSectorReaderPolicy
		{
			DeinterleavedSubcode = false
		};

		private LibTst _core;
		private Disc[] _disks;
		private DiscSectorReader[] _diskReaders;
		private LibSaturnus.CDTOCCallback _cdTocCallback;
		private LibSaturnus.CDSectorCallback _cdSectorCallback;
		private TstControllerDeck _controllerDeck;

		[CoreConstructor("PCFX")]
		public Tst(CoreComm comm, byte[] rom)
			: base(comm, new Configuration())
		{
			throw new InvalidOperationException("To load a PC-FX game, please load the CUE file and not the BIN file.");
		}

		public Tst(CoreComm comm, IEnumerable<Disc> disks)
			: base(comm, new Configuration
			{
				DefaultFpsNumerator = 7159091,
				DefaultFpsDenominator = 455 * 263,
				DefaultWidth = 256,
				DefaultHeight = 232,
				MaxWidth = 1024,
				MaxHeight = 480,
				MaxSamples = 2048,
				SystemId = "PCFX"
			})
		{
			var bios = comm.CoreFileProvider.GetFirmware("PCFX", "BIOS", true);
			if (bios.Length != 1024 * 1024)
				throw new InvalidOperationException("Wrong size BIOS file!");

			_disks = disks.ToArray();
			_diskReaders = disks.Select(d => new DiscSectorReader(d) { Policy = _diskPolicy }).ToArray();
			_cdTocCallback = CDTOCCallback;
			_cdSectorCallback = CDSectorCallback;

			_core = PreInit<LibTst>(new PeRunnerOptions
			{
				Filename = "pcfx.wbx",
				SbrkHeapSizeKB = 1024,
				SealedHeapSizeKB = 4 * 1024,
				InvisibleHeapSizeKB = 256,
				PlainHeapSizeKB = 256,
				MmapHeapSizeKB = 32 * 1024
			});

			SetCdCallbacks();
			if (!_core.Init(_disks.Length, bios))
				throw new InvalidOperationException("Core rejected the CDs!");
			ClearCdCallbacks();

			PostInit();
			SetCdCallbacks();
			_controllerDeck = new TstControllerDeck(new[] { ControllerType.Gamepad, ControllerType.Gamepad });
			ControllerDefinition = _controllerDeck.Definition;
		}

		public override int VirtualWidth => VirtualHeight > 240 ? 586 : 293;

		protected override void LoadStateBinaryInternal(BinaryReader reader)
		{
			SetCdCallbacks();
		}

		protected override LibWaterboxCore.FrameInfo FrameAdvancePrep(IController controller, bool render, bool rendersound)
		{
			DriveLightOn = false;
			var ret = new LibTst.FrameInfo();
			var controls = _controllerDeck.GetData(controller);
			ret.Port1Buttons = controls[0];
			ret.Port2Buttons = controls[1];
			ret.ConsoleButtons = controls[2];
			return ret;
		}

		private void CDTOCCallback(int disk, [In, Out]LibSaturnus.TOC t)
		{
			Saturnus.SetupTOC(t, _disks[disk].TOC);
		}
		private void CDSectorCallback(int disk, int lba, IntPtr dest)
		{
			var buff = new byte[2448];
			_diskReaders[disk].ReadLBA_2448(lba, buff, 0);
			Marshal.Copy(buff, 0, dest, 2448);
			DriveLightOn = true;
		}

		private void SetCdCallbacks()
		{
			_core.SetCDCallbacks(_cdTocCallback, _cdSectorCallback);
		}
		private void ClearCdCallbacks()
		{
			_core.SetCDCallbacks(null, null);
		}

		public bool DriveLightEnabled => true;
		public bool DriveLightOn { get; private set; }
	}
}
