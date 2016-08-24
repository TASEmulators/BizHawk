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
			new CName("A", LibGPGX.INPUT_KEYS.INPUT_A),
			new CName("B", LibGPGX.INPUT_KEYS.INPUT_B),
			new CName("C", LibGPGX.INPUT_KEYS.INPUT_C),
			new CName("Start", LibGPGX.INPUT_KEYS.INPUT_START),
		};

		static CName[] Genesis6 = 
		{
			new CName("Up", LibGPGX.INPUT_KEYS.INPUT_UP),
			new CName("Down", LibGPGX.INPUT_KEYS.INPUT_DOWN),
			new CName("Left", LibGPGX.INPUT_KEYS.INPUT_LEFT),
			new CName("Right", LibGPGX.INPUT_KEYS.INPUT_RIGHT),
			new CName("A", LibGPGX.INPUT_KEYS.INPUT_A),
			new CName("B", LibGPGX.INPUT_KEYS.INPUT_B),
			new CName("C", LibGPGX.INPUT_KEYS.INPUT_C),
			new CName("Start", LibGPGX.INPUT_KEYS.INPUT_START),
			new CName("X", LibGPGX.INPUT_KEYS.INPUT_X),
			new CName("Y", LibGPGX.INPUT_KEYS.INPUT_Y),
			new CName("Z", LibGPGX.INPUT_KEYS.INPUT_Z),
			new CName("Mode", LibGPGX.INPUT_KEYS.INPUT_MODE),
		};

		static CName[] Mouse =
		{
			new CName("Mouse Left", LibGPGX.INPUT_KEYS.INPUT_MOUSE_LEFT),
			new CName("Mouse Center", LibGPGX.INPUT_KEYS.INPUT_MOUSE_CENTER),
			new CName("Mouse Right", LibGPGX.INPUT_KEYS.INPUT_MOUSE_RIGHT),
			new CName("Mouse Start", LibGPGX.INPUT_KEYS.INPUT_MOUSE_START),
		};

		static CName[] Lightgun =
		{
			new CName("Lightgun Trigger", LibGPGX.INPUT_KEYS.INPUT_MENACER_TRIGGER),
			new CName("Lightgun Start", LibGPGX.INPUT_KEYS.INPUT_MENACER_START),
		};

		static CName[] Activator = 
		{
			new CName("1L", LibGPGX.INPUT_KEYS.INPUT_ACTIVATOR_1L),
			new CName("1U", LibGPGX.INPUT_KEYS.INPUT_ACTIVATOR_1U),
			new CName("2L", LibGPGX.INPUT_KEYS.INPUT_ACTIVATOR_2L),
			new CName("2U", LibGPGX.INPUT_KEYS.INPUT_ACTIVATOR_2U),
			new CName("3L", LibGPGX.INPUT_KEYS.INPUT_ACTIVATOR_3L),
			new CName("3U", LibGPGX.INPUT_KEYS.INPUT_ACTIVATOR_3U),
			new CName("4L", LibGPGX.INPUT_KEYS.INPUT_ACTIVATOR_4L),
			new CName("4U", LibGPGX.INPUT_KEYS.INPUT_ACTIVATOR_4U),
			new CName("5L", LibGPGX.INPUT_KEYS.INPUT_ACTIVATOR_5L),
			new CName("5U", LibGPGX.INPUT_KEYS.INPUT_ACTIVATOR_5U),
			new CName("6L", LibGPGX.INPUT_KEYS.INPUT_ACTIVATOR_6L),
			new CName("6U", LibGPGX.INPUT_KEYS.INPUT_ACTIVATOR_6U),
			new CName("7L", LibGPGX.INPUT_KEYS.INPUT_ACTIVATOR_7L),
			new CName("7U", LibGPGX.INPUT_KEYS.INPUT_ACTIVATOR_7U),
			new CName("8L", LibGPGX.INPUT_KEYS.INPUT_ACTIVATOR_8L),
			new CName("8U", LibGPGX.INPUT_KEYS.INPUT_ACTIVATOR_8U),
		};

		static CName[] XEA1P =
		{
			new CName("XE A", LibGPGX.INPUT_KEYS.INPUT_XE_A),
			new CName("XE B", LibGPGX.INPUT_KEYS.INPUT_XE_B),
			new CName("XE C", LibGPGX.INPUT_KEYS.INPUT_XE_C),
			new CName("XE D", LibGPGX.INPUT_KEYS.INPUT_XE_D),
			new CName("XE Start", LibGPGX.INPUT_KEYS.INPUT_XE_START),
			new CName("XE Select", LibGPGX.INPUT_KEYS.INPUT_XE_SELECT),
			new CName("XE E1", LibGPGX.INPUT_KEYS.INPUT_XE_E1),
			new CName("XE E2", LibGPGX.INPUT_KEYS.INPUT_XE_E2),
		};

		static ControllerDefinition.FloatRange MouseRange = new ControllerDefinition.FloatRange(-256, 0, 255);
		// lightgun needs to be transformed to match the current screen resolution
		static ControllerDefinition.FloatRange LightgunRange = new ControllerDefinition.FloatRange(0, 5000, 10000);

		static ControllerDefinition.FloatRange XEA1PRange = new ControllerDefinition.FloatRange(-128, 0, 127);

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
			string NX = string.Format("P{0} Mouse X", player);
			string NY = string.Format("P{0} Mouse Y", player);
			ControllerDef.FloatControls.Add(NX);
			ControllerDef.FloatControls.Add(NY);
			ControllerDef.FloatRanges.Add(MouseRange);
			ControllerDef.FloatRanges.Add(MouseRange);
			Converts.Add(delegate()
			{
				target.analog[(2 * idx) + 0] = (short)source.GetFloat(NX);
				target.analog[(2 * idx) + 1] = (short)source.GetFloat(NY);
			});
		}

		void DoLightgunAnalog(int idx, int player)
		{
			string NX = string.Format("P{0} Lightgun X", player);
			string NY = string.Format("P{0} Lightgun Y", player);
			ControllerDef.FloatControls.Add(NX);
			ControllerDef.FloatControls.Add(NY);
			ControllerDef.FloatRanges.Add(LightgunRange);
			ControllerDef.FloatRanges.Add(LightgunRange);
			Converts.Add(delegate()
			{
				target.analog[(2 * idx) + 0] = (short)(source.GetFloat(NX) / 10000.0f * (ScreenWidth - 1));
				target.analog[(2 * idx) + 1] = (short)(source.GetFloat(NY) / 10000.0f * (ScreenHeight - 1));
			});
		}

		void DoXEA1PAnalog(int idx, int player)
		{
			string NX = string.Format("P{0} Stick X", player);
			string NY = string.Format("P{0} Stick Y", player);
			string NZ = string.Format("P{0} Stick Z", player);
			ControllerDef.FloatControls.Add(NX);
			ControllerDef.FloatControls.Add(NY);
			ControllerDef.FloatControls.Add(NZ);
			ControllerDef.FloatRanges.Add(XEA1PRange);
			ControllerDef.FloatRanges.Add(XEA1PRange);
			ControllerDef.FloatRanges.Add(XEA1PRange);
			Converts.Add(delegate()
			{
				target.analog[(2 * idx) + 0] = (short)(source.GetFloat(NX));
				target.analog[(2 * idx) + 1] = (short)(source.GetFloat(NY));
				// +2 is correct in how gpgx internally does this
				target.analog[(2 * idx) + 2] = (short)(source.GetFloat(NZ));
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

			ControllerDef.BoolButtons.Add("Power");
			ControllerDef.BoolButtons.Add("Reset");

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
					case LibGPGX.INPUT_DEVICE.DEVICE_NONE:
						break;
					case LibGPGX.INPUT_DEVICE.DEVICE_LIGHTGUN:
						// supports menacers and justifiers
						AddToController(i, player, Lightgun);
						DoLightgunAnalog(i, player);
						player++;
						break;
					case LibGPGX.INPUT_DEVICE.DEVICE_PAD2B:
					case LibGPGX.INPUT_DEVICE.DEVICE_PADDLE:
					case LibGPGX.INPUT_DEVICE.DEVICE_SPORTSPAD:
					case LibGPGX.INPUT_DEVICE.DEVICE_TEREBI:
						throw new Exception("Master System only device?  Something went wrong.");
					case LibGPGX.INPUT_DEVICE.DEVICE_ACTIVATOR:
						AddToController(i, player, Activator);
						player++;
						break;
					case LibGPGX.INPUT_DEVICE.DEVICE_XE_A1P:
						AddToController(i, player, XEA1P);
						DoXEA1PAnalog(i, player);
						player++;
						break;
					case LibGPGX.INPUT_DEVICE.DEVICE_PICO:
						// PICO isn't finished on the unmanaged side either
						throw new Exception("Sega PICO not implemented yet!");
					default:
						throw new Exception("Unknown Genesis control device!  Something went wrong.");
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

		/// <summary>
		/// must be set for proper lightgun operation
		/// </summary>
		public int ScreenWidth { get; set; }
		/// <summary>
		/// must be set for proper lightgun operation
		/// </summary>
		public int ScreenHeight { get; set; }

	}
}
