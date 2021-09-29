using System;
using System.Runtime.InteropServices;
using System.IO;

using BizHawk.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Properties;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.NDS
{
	[PortedCore(CoreNames.MelonDS, "Arisotura", "0.8.2", "http://melonds.kuribo64.net/", singleInstance: true, isReleased: false)]
	public unsafe partial class MelonDS : IEmulator
	{
		private readonly BasicServiceProvider _serviceProvider;
		public IEmulatorServiceProvider ServiceProvider => _serviceProvider;

		public ControllerDefinition ControllerDefinition { get; }

		public int Frame => GetFrameCount();

		public string SystemId => "NDS";

		public bool DeterministicEmulation { get; }

		internal CoreComm CoreComm { get; }

		private bool _disposed;
		public void Dispose()
		{
			if (!_disposed)
			{
				Deinit();
				_resampler.Dispose();
				_disposed = true;
			}
		}

		public bool FrameAdvance(IController controller, bool render, bool renderSound = true)
		{
			if (controller.IsPressed("Power"))
			{
				Reset();
			}
			int buttons = (controller.IsPressed("A") ? 1 : 0) | (controller.IsPressed("B") ? 2 : 0)
				| (controller.IsPressed("Select") ? 4 : 0) | (controller.IsPressed("Start") ? 8 : 0)
				| (controller.IsPressed("Right") ? 0x10 : 0) | (controller.IsPressed("Left") ? 0x20 : 0)
				| (controller.IsPressed("Up") ? 0x40 : 0) | (controller.IsPressed("Down") ? 0x80 : 0)
				| (controller.IsPressed("R") ? 0x100 : 0) | (controller.IsPressed("L") ? 0x200 : 0)
				| (controller.IsPressed("X") ? 0x400 : 0) | (controller.IsPressed("Y") ? 0x800 : 0)
				| (controller.IsPressed("Touch") ? 0x2000 : 0)
				| (controller.IsPressed("LidOpen") ? 0x4000 : 0) | (controller.IsPressed("LidClose") ? 0x8000 : 0);
			FrameAdvance((uint)buttons, (byte)controller.AxisValue("TouchX"), (byte)controller.AxisValue("TouchY"));
			_getNewBuffer = true;
			return true;
		}

		public void ResetCounters()
		{
			_ResetCounters();
		}

		// debug path/build for easier testing
		//const string dllPath = "../melonds/build/libcore.dll";
		private const string dllPath = "dll/libmelonds.dll";

		[DllImport(dllPath, EntryPoint = "melonds_create")]
		private static extern bool Init(int consoletype, bool directboot);
		[DllImport(dllPath, EntryPoint = "melonds_destroy")]
		private static extern void Deinit();
		[DllImport(dllPath, EntryPoint = "melonds_loadallfiles")]
		private static extern bool LoadAllFiles(
			byte[] arm7biosdata, uint arm7bioslength,
			byte[] arm9biosdata, uint arm9bioslength,
			byte[] dsfirmwaredata, uint dsfirmwarelength,
			byte[] dldisddata, uint dldisdlength,
			byte[] arm7ibiosdata, uint arm7ibioslength,
			byte[] arm9ibiosdata, uint arm9ibioslength,
			byte[] dsifirmwaredata, uint dsifirmwarelength,
			byte[] dsinanddata, uint dsinandlength,
			byte[] dsisddata, uint dsisdlength,
			byte[] gbaromfiledata, uint gbaromfilelength,
			byte[] gbasramfiledata, uint gbasramfilelength,
			byte[] romfiledata, uint romfilelength);
		[DllImport(dllPath, EntryPoint = "melonds_reset")]
		private static extern void Reset();

		[DllImport(dllPath, EntryPoint = "melonds_resetcounters")]
		private static extern void _ResetCounters();
		[DllImport(dllPath, EntryPoint = "melonds_getframecount")]
		private static extern int GetFrameCount();

		[DllImport(dllPath, EntryPoint = "melonds_frameadvance")]
		private static extern void FrameAdvance(uint buttons, byte touchX, byte touchY);

		[CoreConstructor("NDS")]
		public MelonDS(byte[] file, CoreComm comm, MelonSettings settings, MelonSyncSettings syncSettings, bool deterministic)
		{
			_serviceProvider = new BasicServiceProvider(this);
			ControllerDefinition = new ControllerDefinition { Name = "NDS Controller" };
			ControllerDefinition.BoolButtons.Add("Left");
			ControllerDefinition.BoolButtons.Add("Right");
			ControllerDefinition.BoolButtons.Add("Up");
			ControllerDefinition.BoolButtons.Add("Down");
			ControllerDefinition.BoolButtons.Add("A");
			ControllerDefinition.BoolButtons.Add("B");
			ControllerDefinition.BoolButtons.Add("X");
			ControllerDefinition.BoolButtons.Add("Y");
			ControllerDefinition.BoolButtons.Add("L");
			ControllerDefinition.BoolButtons.Add("R");
			ControllerDefinition.BoolButtons.Add("Start");
			ControllerDefinition.BoolButtons.Add("Select");

			ControllerDefinition.BoolButtons.Add("LidOpen");
			ControllerDefinition.BoolButtons.Add("LidClose");
			ControllerDefinition.BoolButtons.Add("Power");

			ControllerDefinition.BoolButtons.Add("Touch");
			ControllerDefinition.AddXYPair("Touch{0}", AxisPairOrientation.RightAndUp, 0.RangeTo(255), 128, 0.RangeTo(191), 96); //TODO verify direction against hardware

			CoreComm = comm;
			DeterministicEmulation = deterministic;
			_resampler = new SpeexResampler(SpeexResampler.Quality.QUALITY_DEFAULT, 32768, 44100, 32768, 44100);

			_settings = settings ?? new MelonSettings();
			_syncSettings = syncSettings ?? new MelonSyncSettings();

			byte[] arm7;
			byte[] arm9;
			byte[] dsfirmware;
			byte[] dldisd;
			byte[] arm7i;
			byte[] arm9i;
			byte[] dsifirmware;
			byte[] dsinand;
			byte[] dsisd;
			byte[] gbaromfile;
			byte[] gbasramfile;
			// DS bioses are loaded in both DS and DSi mode
			if (_syncSettings.UseRealDSBIOS)
			{
				arm7 = CoreComm.CoreFileProvider.GetFirmwareOrThrow(new("NDS", "bios7"), "Cannot find real arm7 bios, change sync settings to boot without real bios");
				arm9 = CoreComm.CoreFileProvider.GetFirmwareOrThrow(new("NDS", "bios9"), "Cannot find real arm9 bios, change sync settings to boot without real bios");
				dsfirmware = CoreComm.CoreFileProvider.GetFirmwareOrThrow(new("NDS", "firmware"), "Cannot find real ds firmware, change sync settings to boot without real firmware");
			}
			else
			{
				arm7 = Util.DecompressGzipFile(new MemoryStream(Resources.DRASTIC_BIOS_ARM7.Value));
				arm9 = Util.DecompressGzipFile(new MemoryStream(Resources.DRASTIC_BIOS_ARM9.Value));
				dsfirmware = null; // fake firmware will be used
			}
			if (_syncSettings.UseDSi)
			{
				arm7i = CoreComm.CoreFileProvider.GetFirmwareOrThrow(new("NDS", "bios7i"));
				arm9i = CoreComm.CoreFileProvider.GetFirmwareOrThrow(new("NDS", "bios9i"));
				dsifirmware = CoreComm.CoreFileProvider.GetFirmwareOrThrow(new("NDS", "firmwarei"));
				dsinand = CoreComm.CoreFileProvider.GetFirmwareOrThrow(new("NDS", "nand"));
				dsisd = CoreComm.CoreFileProvider.GetFirmware(new("NDS", "sd"));
				dldisd = null;
				gbaromfile = null;
				gbasramfile = null;
			}
			else
			{
				arm7i = null;
				arm9i = null;
				dsifirmware = null;
				dsinand = null;
				dsisd = null;
				dldisd = _syncSettings.UseDLDI ? CoreComm.CoreFileProvider.GetFirmware(new("NDS", "dldi")) : null;
				gbaromfile = null;// CoreComm.CoreFileProvider.GetFirmware(new("NDS", "gbarom"));
				gbasramfile = null;// CoreComm.CoreFileProvider.GetFirmware(new("NDS", "gbasram"));
			}

			LoadAllFiles(arm7, (uint)arm7.Length, arm9, (uint)arm9.Length, dsfirmware, dsfirmware == null ? 0 : (uint)dsfirmware.Length, dldisd, dldisd == null ? 0 : (uint)dldisd.Length,
						 arm7i, arm7i == null ? 0 : (uint)arm7i.Length, arm9i, arm9i == null ? 0 : (uint)arm9i?.Length, dsifirmware, dsifirmware == null ? 0 : (uint)dsifirmware.Length, dsinand, dsinand == null ? 0 : (uint)dsinand.Length, dsisd, dsisd == null ? 0 : (uint)dsisd.Length,
						 gbaromfile, gbaromfile == null ? 0 : (uint)gbasramfile.Length, gbasramfile, gbasramfile == null ? 0 : (uint)gbasramfile.Length, file, (uint)file.Length);

			if (!Init(0, !_syncSettings.BootToFirmware || !_syncSettings.UseRealDSBIOS))
			{
				throw new Exception("Failed to init NDS. Bad ROM?");
			}

			PutSettings(_settings);
			PutSyncSettings(_syncSettings);

			InitMemoryDomains();
		}

		/// <summary>
		/// Creates a modified copy of the given firmware file, with the user settings erased.
		/// </summary>
		/// <returns>Returns a path to the new file.</returns>
		public static string CreateModifiedFirmware(string firmwarePath)
		{
			Directory.CreateDirectory("melon");

			const string newPath = "melon/tohash.bin";
			byte[] bytes = File.ReadAllBytes(firmwarePath);

			// There are two regions for user settings
			int settingsLength = GetUserSettingsLength();
			for (int i = bytes.Length - 0x200; i < bytes.Length - 0x200 + settingsLength; i++)
				bytes[i] = 0xFF;
			for (int i = bytes.Length - 0x100; i < bytes.Length - 0x100 + settingsLength; i++)
				bytes[i] = 0xFF;


			File.WriteAllBytes(newPath, bytes);
			return newPath;
		}
	}
}
