using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.NDS
{
	[Core("DeSmuME", "DeSmuME Team")]
	unsafe class DeSmuME : IEmulator
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
			DeInit_NDS();
		}

		public bool FrameAdvance(IController controller, bool render, bool rendersound = true)
		{
			int buttons = (controller.IsPressed("Left") ? 1 : 0) | (controller.IsPressed("Right") ? 2 : 0)
				| (controller.IsPressed("Up") ? 4 : 0) | (controller.IsPressed("Down") ? 8 : 0)
				| (controller.IsPressed("A") ? 0x10 : 0) | (controller.IsPressed("B") ? 0x20 : 0)
				| (controller.IsPressed("X") ? 0x40 : 0) | (controller.IsPressed("Y") ? 0x80 : 0)
				| (controller.IsPressed("L") ? 0x100 : 0) | (controller.IsPressed("R") ? 0x200 : 0)
				| (controller.IsPressed("Start") ? 0x400 : 0) | (controller.IsPressed("Select") ? 0x800 : 0)
				| (controller.IsPressed("Debug") ? 0x1000 : 0) | (controller.IsPressed("Lid") ? 0x2000 : 0)
				| (controller.IsPressed("Touch") ? 0x4000 : 0);
			SetInput((short)buttons, (byte)controller.GetFloat("TouchX"), (byte)controller.GetFloat("TouchY"));
			FrameAdvance();
			return true;
		}

		public void ResetCounters()
		{
			throw new NotImplementedException();
		}

		// debug path/build for easier testing
		const string dllPath = "../../desmume/desmume/src/frontend/windows/__bins/DesHawk-VS2017-x64-Debug.dll";
		//const string dllPath = "DesHawk.dll";

		[DllImport(dllPath)]
		public static extern void Init_NDS();
		[DllImport(dllPath)]
		public static extern void DeInit_NDS();

		[DllImport(dllPath)]
		public static extern int GetFrameCount();

		[DllImport(dllPath)]
		public static extern bool LoadROM(byte* file, int fileSize);

		[DllImport(dllPath)]
		public static extern void SetInput(short buttons, byte touchX, byte touchY);
		[DllImport(dllPath)]
		public static extern void FrameAdvance();

		[CoreConstructor("NDS")]
		public DeSmuME(byte[] file)
		{
			_serviceProvider = new BasicServiceProvider(this);
			ControllerDefinition = new ControllerDefinition();
			ControllerDefinition.Name = "NDS";
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

			ControllerDefinition.BoolButtons.Add("Debug");
			ControllerDefinition.BoolButtons.Add("Lid");

			ControllerDefinition.BoolButtons.Add("Touch");
			ControllerDefinition.FloatControls.Add("TouchX");
			ControllerDefinition.FloatRanges.Add(new ControllerDefinition.FloatRange(0, 128, 255));
			ControllerDefinition.FloatControls.Add("TouchY");
			ControllerDefinition.FloatRanges.Add(new ControllerDefinition.FloatRange(0, 96, 191));

			CoreComm = new CoreComm(null, null);
			CoreComm.NominalWidth = 256;
			CoreComm.NominalHeight = 192;

			Init_NDS();

			fixed (byte* f = file)
			{
				LoadROM(f, file.Length);
			}
		}
	}
}
