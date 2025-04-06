using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using BizHawk.BizInvoke;
using BizHawk.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.DiscSystem;

namespace BizHawk.Emulation.Cores.Consoles.Sony.PSP
{
	[PortedCore(
		name: CoreNames.PPSSPP,
		author: "Henrik Rydgård et al",
		portedVersion: "2025.03.09 (ecbbadd)",
		portedUrl: "https://github.com/hrydgard/ppsspp",
		isReleased: false)]
	public partial class PPSSPP : IEmulator, IVideoProvider, ISoundProvider, IInputPollable, IStatable, ISettable<PPSSPP.Settings, PPSSPP.SyncSettings>
	{
		private static DynamicLibraryImportResolver _resolver;
		private LibPPSSPP _libPPSSPP;
		private readonly List<IDiscAsset> _discAssets;

		[CoreConstructor(VSystemID.Raw.PSP)]
		public PPSSPP(CoreLoadParameters<Settings, SyncSettings> lp) 
		{
			Console.WriteLine("In PPSSP Core");

			// Assigning configuration
			_settings = lp.Settings;
			_syncSettings = lp.SyncSettings;

			DriveLightEnabled = true;
			_discAssets = lp.Discs;

			// If no discs loaded, then there's nothing to emulate
			if (_discAssets.Count == 0) throw new InvalidOperationException("No CDs provided for emulation");
			_isMultidisc = _discAssets.Count > 1;

			_CDReadCallback = CDRead;
			_CDSectorCountCallback = CDSectorCount;
			_discIndex = 0;
			foreach (var disc in _discAssets) _cdReaders.Add(new(disc.DiscData));

			Console.WriteLine($"[CD] Sector count: {_discAssets[0].DiscData.Session1.LeadoutLBA}");
			_syncSettings = lp.SyncSettings ?? new();
			ControllerDefinition = CreateControllerDefinition(_syncSettings, _isMultidisc);

			// Registering service provider
			_serviceProvider = new(this);
			_serviceProvider.Register<IVideoProvider>(this);
			_serviceProvider.Register<ISoundProvider>(this);

			
			// Loading LibPPSSPP
			_resolver?.Dispose();
			_resolver = new(OSTailoredCode.IsUnixHost ? "libppsspp.so" : "libppsspp.dll", hasLimitedLifetime: true);
			_libPPSSPP = BizInvoker.GetInvoker<LibPPSSPP>(_resolver, CallingConventionAdapters.Native);

			// Setting CD callbacks
			_libPPSSPP.SetCdCallbacks(_CDReadCallback, _CDSectorCountCallback);

			////////////// Initializing Core
			string cdName = _discAssets[0].DiscName;
			Console.WriteLine($"Launching Core with Game: '{cdName}'");
			if (!_libPPSSPP.Init(gameFile: cdName))
			{
				throw new InvalidOperationException("Core rejected the rom!");
			}
		}

		// CD Handling logic
		private bool _isMultidisc;
		private bool _discInserted = true;
		private readonly LibPPSSPP.CDReadCallback _CDReadCallback;
		private readonly LibPPSSPP.CDSectorCountCallback _CDSectorCountCallback;
		private int _discIndex;
		private readonly List<DiscSectorReader> _cdReaders = new List<DiscSectorReader>();
		private static int CD_SECTOR_SIZE = 2048;
		private readonly byte[] _sectorBuffer = new byte[CD_SECTOR_SIZE];

		private void SelectNextDisc()
		{
			_discIndex++;
			if (_discIndex == _discAssets.Count) _discIndex = 0;
			Console.WriteLine($"Selected CDROM {_discIndex}: {_discAssets[_discIndex].DiscName}");
		}

		private void SelectPrevDisc()
		{
			_discIndex--;
			if (_discIndex < 0) _discIndex = _discAssets.Count - 1;
			Console.WriteLine($"Selected CDROM {_discIndex}: {_discAssets[_discIndex].DiscName}");
		}

		private void CDRead(int lba, IntPtr dest)
		{
			if (_discIndex < _discAssets.Count)
			{
				_cdReaders[_discIndex].ReadLBA_2048(lba, _sectorBuffer, 0);
				Marshal.Copy(_sectorBuffer, 0, dest, CD_SECTOR_SIZE);
			}
			DriveLightOn = true;
		}

		private int CDSectorCount()
		{
			if (_discIndex < _discAssets.Count) return _discAssets[_discIndex].DiscData.Session1.LeadoutLBA;
			return -1;
		}

		protected LibPPSSPP.FrameInfo FrameAdvancePrep(IController controller, bool render, bool rendersound)
		{
			var fi = new LibPPSSPP.FrameInfo();

			// Disc management
			if (_isMultidisc)
			{
				if (controller.IsPressed("Next Disc")) SelectNextDisc();
				if (controller.IsPressed("Prev Disc")) SelectPrevDisc();
			}

			fi.input.Down  = controller.IsPressed($"P1 {JoystickButtons.Down  }") ? 1 : 0;
			fi.input.Left  = controller.IsPressed($"P1 {JoystickButtons.Left  }") ? 1 : 0;
			fi.input.Right  = controller.IsPressed($"P1 {JoystickButtons.Right  }") ? 1 : 0;
			fi.input.Start  = controller.IsPressed($"P1 {JoystickButtons.Start  }") ? 1 : 0;
			fi.input.Select  = controller.IsPressed($"P1 {JoystickButtons.Select  }") ? 1 : 0;
			fi.input.ButtonSquare  = controller.IsPressed($"P1 {JoystickButtons.ButtonSquare  }") ? 1 : 0;
			fi.input.ButtonTriangle  = controller.IsPressed($"P1 {JoystickButtons.ButtonTriangle }") ? 1 : 0;
			fi.input.ButtonCircle  = controller.IsPressed($"P1 {JoystickButtons.ButtonCircle  }") ? 1 : 0;
			fi.input.ButtonCross  = controller.IsPressed($"P1 {JoystickButtons.ButtonCross  }") ? 1 : 0;
			fi.input.ButtonLTrigger  = controller.IsPressed($"P1 {JoystickButtons.ButtonLTrigger }") ? 1 : 0;
			fi.input.ButtonRTrigger  = controller.IsPressed($"P1 {JoystickButtons.ButtonRTrigger }") ? 1 : 0;
			fi.input.RightAnalogX  = controller.AxisValue($"P1 {JoystickAxes.RightAnalogX  }");
			fi.input.RightAnalogY  = controller.AxisValue($"P1 {JoystickAxes.RightAnalogY  }");
			fi.input.LeftAnalogX  = controller.AxisValue($"P1 {JoystickAxes.LeftAnalogX  }");
			fi.input.LeftAnalogY = controller.AxisValue($"P1 {JoystickAxes.LeftAnalogY}");

			DriveLightOn = false;
			

			return fi;
		}

		protected void SaveStateBinaryInternal(BinaryWriter writer)
		{
			writer.Write(_discIndex);
			writer.Write(_discInserted);
		}

		protected void LoadStateBinaryInternal(BinaryReader reader)
		{
			_discIndex = reader.ReadInt32();
			_discInserted = reader.ReadBoolean();
		}

	}
}