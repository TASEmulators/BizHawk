using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.IO;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.NDS
{
	[Core("MelonDS", "Arisotura")]
	unsafe public partial class MelonDS : IEmulator
	{
		private BasicServiceProvider _serviceProvider;
		public IEmulatorServiceProvider ServiceProvider => _serviceProvider;

		public ControllerDefinition ControllerDefinition { get; private set; }

		public int Frame => GetFrameCount();

		public string SystemId => "NDS";

		public bool DeterministicEmulation => true;

		public CoreComm CoreComm { get; private set; }

		public void Dispose()
		{
			Deinit();
		}

		public bool FrameAdvance(IController controller, bool render, bool rendersound = true)
		{
			int buttons = (controller.IsPressed("A") ? 1 : 0) | (controller.IsPressed("B") ? 2 : 0)
				| (controller.IsPressed("Select") ? 4 : 0) | (controller.IsPressed("Start") ? 8 : 0)
				| (controller.IsPressed("Right") ? 0x10 : 0) | (controller.IsPressed("Left") ? 0x20 : 0)
				| (controller.IsPressed("Up") ? 0x40 : 0) | (controller.IsPressed("Down") ? 0x80 : 0)
				| (controller.IsPressed("R") ? 0x100 : 0) | (controller.IsPressed("L") ? 0x200 : 0)
				| (controller.IsPressed("X") ? 0x400 : 0) | (controller.IsPressed("Y") ? 0x800 : 0)
				| (controller.IsPressed("Touch") ? 0x2000 : 0)
				| (controller.IsPressed("Lid") ? 0x4000 : 0);
			FrameAdvance((short)buttons, (byte)controller.GetFloat("TouchX"), (byte)controller.GetFloat("TouchY"));
			return true;
		}

		public void ResetCounters()
		{
			_ResetCounters();
		}

		// debug path/build for easier testing
		//const string dllPath = "../../MelonDS/build/libmelonDS.dll";
		const string dllPath = "dll/libmelonDS.dll";

		[DllImport(dllPath)]
		private static extern bool Init();
		[DllImport(dllPath)]
		private static extern void Deinit();

		[DllImport(dllPath)]
		private static extern void LoadROM(byte* file, int fileSize);

		[DllImport(dllPath, EntryPoint = "ResetCounters")]
		private static extern void _ResetCounters();
		[DllImport(dllPath)]
		private static extern int GetFrameCount();

		[DllImport(dllPath)]
		private static extern void FrameAdvance(short buttons, byte touchX, byte touchY);

		[CoreConstructor("NDS")]
		public MelonDS(byte[] file, CoreComm comm, object settings, object syncsettings)
		{
			_serviceProvider = new BasicServiceProvider(this);
			ControllerDefinition = new ControllerDefinition();
			ControllerDefinition.Name = "NDS Controller";
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

			ControllerDefinition.BoolButtons.Add("Lid");

			ControllerDefinition.BoolButtons.Add("Touch");
			ControllerDefinition.FloatControls.Add("TouchX");
			ControllerDefinition.FloatRanges.Add(new ControllerDefinition.FloatRange(0, 128, 255));
			ControllerDefinition.FloatControls.Add("TouchY");
			ControllerDefinition.FloatRanges.Add(new ControllerDefinition.FloatRange(0, 96, 191));

			CoreComm = comm;
			CoreComm.NominalWidth = 256;
			CoreComm.NominalHeight = 192;

			SetUpFiles(syncsettings as MelonSyncSettings);

			if (!Init())
				throw new Exception("Failed to init NDS.");

			fixed (byte* f = file)
			{
				LoadROM(f, file.Length);
			}
		}

		/// <summary>
		/// MelonDS expects bios and firmware files at a specific location.
		/// </summary>
		private void SetUpFiles(MelonSyncSettings ss)
		{
			byte[] fwBytes;
			fwBytes = CoreComm.CoreFileProvider.GetFirmware("NDS", "bios7", false);
			if (fwBytes != null)
				File.WriteAllBytes("melon/bios7.bin", fwBytes);
			else
				File.Delete("melon/bios7.bin");

			fwBytes = CoreComm.CoreFileProvider.GetFirmware("NDS", "bios9", false);
			if (fwBytes != null)
				File.WriteAllBytes("melon/bios9.bin", fwBytes);
			else
				File.Delete("melon/bios9.bin");

			fwBytes = CoreComm.CoreFileProvider.GetFirmware("NDS", "firmware", false);
			if (fwBytes != null)
			{
				ss.userSettings.CopyTo(fwBytes, fwBytes.Length - 0x200); // NDS user settings are included in the firmware file.
				File.WriteAllBytes("melon/firmware.bin", fwBytes);
			}
			else
				File.Delete("melon/firmware.bin");
		}

		/// <summary>
		/// Creates a modified copy of the given firmware file, with the user settings erased.
		/// </summary>
		/// <returns>Returns a path to the new file.</returns>
		public static string CreateModifiedFirmware(string firmwarePath)
		{
			const string newPath = "melon/tohash.bin";
			byte[] bytes = File.ReadAllBytes(firmwarePath);

			// There are two regions for user settings
			int settingsLength = getUserSettingsLength();
			for (int i = bytes.Length - 0x200; i < bytes.Length - 0x200 + settingsLength; i++)
				bytes[i] = 0xFF;
			for (int i = bytes.Length - 0x100; i < bytes.Length - 0x100 + settingsLength; i++)
				bytes[i] = 0xFF;


			File.WriteAllBytes(newPath, bytes);
			return newPath;
		}
	}
}
