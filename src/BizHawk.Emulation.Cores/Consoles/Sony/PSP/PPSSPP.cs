using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using BizHawk.BizInvoke;
using BizHawk.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Properties;
using BizHawk.Emulation.DiscSystem;

namespace BizHawk.Emulation.Cores.Consoles.Sony.PSP
{
	[PortedCore(
		name: CoreNames.PPSSPP,
		author: "Henrik Rydgård et al",
		portedVersion: "2025.03.09 (ecbbadd)",
		portedUrl: "https://github.com/hrydgard/ppsspp",
		isReleased: false)]
	public partial class PPSSPP : IEmulator, IVideoProvider, ISoundProvider, IInputPollable, IStatable, ISettable<object, object>
	{
		private static DynamicLibraryImportResolver _resolver;
		private LibPPSSPP _libPPSSPP;
		private readonly List<IDiscAsset> _discAssets;

		[CoreConstructor(VSystemID.Raw.PSP)]
		public PPSSPP(CoreLoadParameters<object, object> lp)
		{
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
			ControllerDefinition = CreateControllerDefinition(_isMultidisc);

			// Registering service provider
			_serviceProvider = new(this);
			_serviceProvider.Register<IVideoProvider>(this);
			_serviceProvider.Register<ISoundProvider>(this);

			// Loading LibPPSSPP
			_resolver = new(OSTailoredCode.IsUnixHost ? "libppsspp.so" : "libppsspp.dll", hasLimitedLifetime: true);
			_libPPSSPP = BizInvoker.GetInvoker<LibPPSSPP>(_resolver, CallingConventionAdapters.Native);

			// Setting input callback
			_inputCallback = InputCallback;
			_libPPSSPP.SetInputCallback(_inputCallback);

			// Setting CD callbacks
			_libPPSSPP.SetCdCallbacks(_CDReadCallback, _CDSectorCountCallback);

			//// Pre-loading emulator resources

			// Getting compat.ini -- required to set game-specific compatibility flags
			string resourceName = "";

			resourceName = "compat.ini";
			var compatIniData = Resources.PPSSPP_COMPAT_INI.Value;
			if (!_libPPSSPP.loadResource(resourceName, compatIniData, compatIniData.Length)) throw new InvalidOperationException($"Could not load resource: {resourceName}");

			// Getting compat_vr.ini -- required to set more game-specific compatibility flags
			resourceName = "compatvr.ini";
			var compatvrIniData = Resources.PPSSPP_COMPATVR_INI.Value;
			if (!_libPPSSPP.loadResource(resourceName, compatvrIniData, compatvrIniData.Length)) throw new InvalidOperationException($"Could not load resource: {resourceName}");

			// Getting UI atlas font -- required to show console system text
			resourceName = "font_atlas.zim";
			var atlasZimData = Resources.PPSSPP_FONT_ATLAS_ZIM.Value;
			if (!_libPPSSPP.loadResource(resourceName, atlasZimData, atlasZimData.Length)) throw new InvalidOperationException($"Could not load resource: {resourceName}");

			// Getting UI atlas font metadata -- required to show console system text
			resourceName = "font_atlas.meta";
			var atlasMetadataData = Resources.PPSSPP_FONT_ATLAS_METADATA.Value;
			if (!_libPPSSPP.loadResource(resourceName, atlasMetadataData, atlasMetadataData.Length)) throw new InvalidOperationException($"Could not load resource: {resourceName}");

			// Getting ppge atlas font -- required to show console system text
			resourceName = "ppge_atlas.zim";
			var ppgeAtlasZimData = Resources.PPSSPP_PPGE_ATLAS_ZIM.Value;
			if (!_libPPSSPP.loadResource(resourceName, ppgeAtlasZimData, ppgeAtlasZimData.Length)) throw new InvalidOperationException($"Could not load resource: {resourceName}");

			// Getting ppge atlas font metadata -- required to show console system text
			resourceName = "ppge_atlas.meta";
			var ppgeAtlasMetadataData = Resources.PPSSPP_PPGE_ATLAS_METADATA.Value;
			if (!_libPPSSPP.loadResource(resourceName, ppgeAtlasMetadataData, ppgeAtlasMetadataData.Length)) throw new InvalidOperationException($"Could not load resource: {resourceName}");

			// Getting PPGe font -- required to show console system text
			resourceName = "PPGeFont.ttf";
			var PPGeFontData = Resources.PPGE_FONT_ROBOTO_CONDENSED.Value;
			if (!_libPPSSPP.loadResource(resourceName, PPGeFontData, PPGeFontData.Length)) throw new InvalidOperationException($"Could not load resource: {resourceName}");

			////////////// Initializing Core
			string cdName = _discAssets[0].DiscName;
			Console.WriteLine($"Launching Core with Game: '{cdName}'");
			if (!_libPPSSPP.Init(gameFile: cdName))
			{
				throw new InvalidOperationException("Core rejected the rom!");
			}
		}

		// Input callback
		private readonly LibPPSSPP.InputCallback _inputCallback;

		// CD Handling logic
		private bool _isMultidisc;
		private bool _discInserted = true;
		private readonly LibPPSSPP.CDReadCallback _CDReadCallback;
		private readonly LibPPSSPP.CDSectorCountCallback _CDSectorCountCallback;
		private int _discIndex;
		private readonly List<DiscSectorReader> _cdReaders = [ ];
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

		private void InputCallback()
		{
			IsLagFrame = false;
		}

		protected void SaveStateBinaryInternal(BinaryWriter writer)
		{
			writer.Write(_discIndex);
			writer.Write(_discInserted);
			writer.Write(DriveLightOn);
		}

		protected void LoadStateBinaryInternal(BinaryReader reader)
		{
			_discIndex = reader.ReadInt32();
			_discInserted = reader.ReadBoolean();
			DriveLightOn = reader.ReadBoolean();
		}
	}
}
