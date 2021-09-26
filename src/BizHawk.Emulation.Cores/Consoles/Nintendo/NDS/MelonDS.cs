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

		public bool DeterministicEmulation => true;

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
		//const string dllPath = "../../MelonDS/build/libmelonDS.dll";
		private const string dllPath = "dll/libmelonDS.dll";

		[DllImport(dllPath, EntryPoint = "melonds_create")]
		private static extern bool Init(int consoletype, byte* dsromfiledata, int dsromfilelength, bool directboot);
		[DllImport(dllPath, EntryPoint = "melonds_destroy")]
		private static extern void Deinit();
		[DllImport(dllPath, EntryPoint = "melonds_loaddsbios")]
		private static extern bool LoadDsBIOS(byte[] arm7biosdata, int arm7bioslength, byte[] arm9biosdata, int arm9bioslength);
		[DllImport(dllPath, EntryPoint = "melonds_reset")]
		private static extern void Reset();

		[DllImport(dllPath, EntryPoint = "melonds_resetcounters")]
		private static extern void _ResetCounters();
		[DllImport(dllPath, EntryPoint = "melonds_getframecount")]
		private static extern int GetFrameCount();
		[DllImport(dllPath, EntryPoint = "melonds_setframecount")]
		private static extern void SetFrameCount(uint count);

		[DllImport(dllPath, EntryPoint = "melonds_frameadvance")]
		private static extern void FrameAdvance(uint buttons, byte touchX, byte touchY);

		[CoreConstructor("NDS")]
		public MelonDS(byte[] file, CoreComm comm, MelonSettings settings, MelonSyncSettings syncSettings)
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
			_resampler = new SpeexResampler(SpeexResampler.Quality.QUALITY_DEFAULT, 32768, 44100, 32768, 44100);

			SetUpFiles();

			_settings = settings ?? new MelonSettings();
			_syncSettings = syncSettings ?? new MelonSyncSettings();

			byte[] arm7;
			byte[] arm9;
			if (!_syncSettings.UseRealBIOS)
			{
				arm7 = CoreComm.CoreFileProvider.GetFirmwareOrThrow(new("NDS", "bios7"), "Cannot find real arm7 bios, change sync settings to boot without real bios");
				arm9 = CoreComm.CoreFileProvider.GetFirmwareOrThrow(new("NDS", "bios9"), "Cannot find real arm9 bios, change sync settings to boot without real bios");
			}
			else
			{
				arm7 = Util.DecompressGzipFile(new MemoryStream(Resources.DRASTIC_BIOS_ARM7.Value));
				arm9 = Util.DecompressGzipFile(new MemoryStream(Resources.DRASTIC_BIOS_ARM9.Value));
			}
			LoadDsBIOS(arm7, arm7.Length, arm9, arm9.Length);

			PutSettings(_settings);
			PutSyncSettings(_syncSettings);

			fixed (byte* f = file)
			{
				if (!Init(0, f, file.Length, !_syncSettings.BootToFirmware))
				{
					throw new Exception("Failed to init NDS.");
				}
			}
			InitMemoryDomains();
		}

		/// <summary>
		/// MelonDS expects bios and firmware files at a specific location.
		/// This should never be called without an accompanying call to PutSyncSettings.
		/// </summary>
		private void SetUpFiles()
		{
			Directory.CreateDirectory("melon");

			byte[] fwBytes;
			bool missingAny = false;

			fwBytes = CoreComm.CoreFileProvider.GetFirmware(new("NDS", "firmware"));
			if (fwBytes != null)
				File.WriteAllBytes("melon/firmware.bin", fwBytes);
			else
			{
				File.Delete("melon/firmware.bin");
				missingAny = true;
			}

			if (missingAny)
				CoreComm.Notify("NDS bios and firmware files are recommended; at least one is missing.");
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
