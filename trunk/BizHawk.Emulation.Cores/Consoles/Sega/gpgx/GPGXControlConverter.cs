using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.Sega.gpgx
{
	public class GPGXControlConverter
	{
		// this isn't all done

		struct CName
		{
			public string Name;
			public LibGPGX.INPUT_KEYS Key;
			public CName(string Name, LibGPGX.INPUT_KEYS Key)
			{
				this.Name = Name;
				this.Key = Key;
			}
		}

		static CName[] Genesis3 =
		{
			new CName("Up", LibGPGX.INPUT_KEYS.INPUT_UP),
			new CName("Down", LibGPGX.INPUT_KEYS.INPUT_DOWN),
			new CName("Left", LibGPGX.INPUT_KEYS.INPUT_LEFT),
			new CName("Right", LibGPGX.INPUT_KEYS.INPUT_RIGHT),
			new CName("B", LibGPGX.INPUT_KEYS.INPUT_B),
			new CName("C", LibGPGX.INPUT_KEYS.INPUT_C),
			new CName("A", LibGPGX.INPUT_KEYS.INPUT_A),
			new CName("Start", LibGPGX.INPUT_KEYS.INPUT_START),
		};

		static CName[] Genesis6 = 
		{
			new CName("Up", LibGPGX.INPUT_KEYS.INPUT_UP),
			new CName("Down", LibGPGX.INPUT_KEYS.INPUT_DOWN),
			new CName("Left", LibGPGX.INPUT_KEYS.INPUT_LEFT),
			new CName("Right", LibGPGX.INPUT_KEYS.INPUT_RIGHT),
			new CName("B", LibGPGX.INPUT_KEYS.INPUT_B),
			new CName("C", LibGPGX.INPUT_KEYS.INPUT_C),
			new CName("A", LibGPGX.INPUT_KEYS.INPUT_A),
			new CName("Start", LibGPGX.INPUT_KEYS.INPUT_START),
			new CName("Z", LibGPGX.INPUT_KEYS.INPUT_Z),
			new CName("Y", LibGPGX.INPUT_KEYS.INPUT_Y),
			new CName("X", LibGPGX.INPUT_KEYS.INPUT_X),
			new CName("Mode", LibGPGX.INPUT_KEYS.INPUT_MODE),
		};

		static CName[] Mouse =
		{
			new CName("Left", LibGPGX.INPUT_KEYS.INPUT_MOUSE_LEFT),
			new CName("Center", LibGPGX.INPUT_KEYS.INPUT_MOUSE_CENTER),
			new CName("Right", LibGPGX.INPUT_KEYS.INPUT_MOUSE_RIGHT),
		};

		static ControllerDefinition.FloatRange FullShort = new ControllerDefinition.FloatRange(-32767, 0, 32767);

		LibGPGX.InputData target = null;
		IController source = null;

		List<Action> Converts = new List<Action>();

		public ControllerDefinition ControllerDef { get; private set; }

		void AddToController(int idx, int player, IEnumerable<CName> Buttons)
		{
			foreach (var Button in Buttons)
			{
				string Name = string.Format("P{0} {1}", player, Button.Name);
				ControllerDef.BoolButtons.Add(Name);
				var ButtonFlag = Button.Key;
				Converts.Add(delegate()
				{
					if (source.IsPressed(Name))
						target.pad[idx] |= ButtonFlag;
				});
			}
		}

		void DoMouseAnalog(int idx, int player)
		{
			string NX = string.Format("P{0} X", player);
			string NY = string.Format("P{0} Y", player);
			ControllerDef.FloatControls.Add(NX);
			ControllerDef.FloatControls.Add(NY);
			ControllerDef.FloatRanges.Add(FullShort);
			ControllerDef.FloatRanges.Add(FullShort);
			Converts.Add(delegate()
			{
				target.analog[idx,0] = (short)source.GetFloat(NX);
				target.analog[idx,1] = (short)source.GetFloat(NY);
			});
		}

		public GPGXControlConverter(LibGPGX.InputData input)
		{
			Console.WriteLine("Genesis Controller report:");
			foreach (var e in input.system)
				Console.WriteLine("S:{0}", e);
			foreach (var e in input.dev)
				Console.WriteLine("D:{0}", e);

			int player = 1;

			ControllerDef = new ControllerDefinition();

			for (int i = 0; i < LibGPGX.MAX_DEVICES; i++)
			{
				switch (input.dev[i])
				{
					case LibGPGX.INPUT_DEVICE.DEVICE_PAD3B:
						AddToController(i, player, Genesis3);
						player++;
						break;
					case LibGPGX.INPUT_DEVICE.DEVICE_PAD6B:
						AddToController(i, player, Genesis6);
						player++;
						break;
					case LibGPGX.INPUT_DEVICE.DEVICE_MOUSE:
						AddToController(i, player, Mouse);
						DoMouseAnalog(i, player);
						player++;
						break;
				}
			}

			ControllerDef.Name = "GPGX Genesis Controller";
		}

		public void Convert(IController source, LibGPGX.InputData target)
		{
			this.source = source;
			this.target = target;
			target.ClearAllBools();
			foreach (var f in Converts)
				f();
			this.source = null;
			this.target = null;
		}

	}
}
