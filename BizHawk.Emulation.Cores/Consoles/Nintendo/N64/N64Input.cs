using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Nintendo.N64.NativeApi;

namespace BizHawk.Emulation.Cores.Nintendo.N64
{
	internal class N64Input
	{
		private mupen64plusInputApi api;
		public CoreComm CoreComm { get; private set; }
		public IController Controller { get; set; }

		public bool LastFrameInputPolled { get; set; }
		public bool ThisFrameInputPolled { get; set; }
		public ControllerDefinition ControllerDefinition { get { return N64ControllerDefinition; } }

		public static readonly ControllerDefinition N64ControllerDefinition = new ControllerDefinition
		{
			Name = "Nintento 64 Controller",
			BoolButtons =
			{
				"P1 A Up", "P1 A Down", "P1 A Left", "P1 A Right", "P1 DPad U", "P1 DPad D", "P1 DPad L", "P1 DPad R", "P1 Start", "P1 Z", "P1 B", "P1 A", "P1 C Up", "P1 C Down", "P1 C Right", "P1 C Left", "P1 L", "P1 R", 
				//"P2 A Up", "P2 A Down", "P2 A Left", "P2 A Right", "P2 DPad U", "P2 DPad D", "P2 DPad L", "P2 DPad R", "P2 Start", "P2 Z", "P2 B", "P2 A", "P2 C Up", "P2 C Down", "P2 C Right", "P2 C Left", "P2 L", "P2 R", 
				//"P3 A Up", "P3 A Down", "P3 A Left", "P3 A Right", "P3 DPad U", "P3 DPad D", "P3 DPad L", "P3 DPad R", "P3 Start", "P3 Z", "P3 B", "P3 A", "P3 C Up", "P3 C Down", "P3 C Right", "P3 C Left", "P3 L", "P3 R", 
				//"P4 A Up", "P4 A Down", "P4 A Left", "P4 A Right", "P4 DPad U", "P4 DPad D", "P4 DPad L", "P4 DPad R", "P4 Start", "P4 Z", "P4 B", "P4 A", "P4 C Up", "P4 C Down", "P4 C Right", "P4 C Left", "P4 L", "P4 R", 
				"Reset", "Power"
			},
			FloatControls =
			{
				"P1 X Axis", "P1 Y Axis",
				//"P2 X Axis", "P2 Y Axis",
				//"P3 X Axis", "P3 Y Axis",
				//"P4 X Axis", "P4 Y Axis"
			},
			FloatRanges =
			{
				new[] {-128.0f, 0.0f, 127.0f},
				new[] {127.0f, 0.0f, -128.0f},
				new[] {-128.0f, 0.0f, 127.0f},
				new[] {127.0f, 0.0f, -128.0f},
				new[] {-128.0f, 0.0f, 127.0f},
				new[] {127.0f, 0.0f, -128.0f},
				new[] {-128.0f, 0.0f, 127.0f},
				new[] {127.0f, 0.0f, -128.0f}
			},
			AxisConstraints =
			{
				new ControllerDefinition.AxisConstraint { Class = "Natural Circle", Type = ControllerDefinition.AxisConstraintType.Circular, Params = new object[] {"P1 X Axis", "P1 Y Axis", 127.0f} }
			}
		};

		private readonly IInputPollable _emuCore;

		public N64Input(IInputPollable emuCore, mupen64plusApi core, CoreComm comm, N64SyncSettings.N64ControllerSettings[] controllerSettings)
		{
			_emuCore = emuCore;
			api = new mupen64plusInputApi(core);
			CoreComm = comm;

			api.SetM64PInputCallback(new mupen64plusInputApi.InputCallback(GetControllerInput));

			core.VInterrupt += ShiftInputPolledBools;
			for (int i = 0; i < controllerSettings.Length; ++i)
			{
				SetControllerConnected(i, controllerSettings[i].IsConnected);
				SetControllerPakType(i, controllerSettings[i].PakType);
			}
		}

		public void ShiftInputPolledBools()
		{
			LastFrameInputPolled = ThisFrameInputPolled;
			ThisFrameInputPolled = false;
		}

		private const sbyte _maxAnalogX = 127;
		private const sbyte _minAnalogX = -127;
		private const sbyte _maxAnalogY = 127;
		private const sbyte _minAnalogY = -127;

		/// <summary>
		/// Translates controller input from EmuHawk into
		/// N64 controller data
		/// </summary>
		/// <param name="i">Id of controller to update and shove</param>
		public int GetControllerInput(int i)
		{
			_emuCore.InputCallbacks.Call();
			ThisFrameInputPolled = true;

			// Analog stick right = +X
			// Analog stick up = +Y
			string p = "P" + (i + 1);
			sbyte x;
			if (Controller.IsPressed(p + " A Left"))
			{
				x = _minAnalogX;
			}
			else if (Controller.IsPressed(p + " A Right"))
			{
				x = _maxAnalogX;
			}
			else
			{
				x = (sbyte)Controller.GetFloat(p + " X Axis");
			}

			sbyte y;
			if (Controller.IsPressed(p + " A Up"))
			{
				y = _maxAnalogY;
			}
			else if (Controller.IsPressed(p + " A Down"))
			{
				y = _minAnalogY;
			}
			else
			{
				y = (sbyte)Controller.GetFloat(p + " Y Axis");
			}

			int value = ReadController(i + 1);
			value |= (x & 0xFF) << 16;
			value |= (y & 0xFF) << 24;
			return value;
		}

		/// <summary>
		/// Read all buttons from a controller and translate them
		/// into a form the N64 understands
		/// </summary>
		/// <param name="num">Number of controller to translate</param>
		/// <returns>Bitlist of pressed buttons</returns>
		public int ReadController(int num)
		{
			int buttons = 0;

			if (Controller.IsPressed("P" + num + " DPad R")) buttons |= (1 << 0);
			if (Controller.IsPressed("P" + num + " DPad L")) buttons |= (1 << 1);
			if (Controller.IsPressed("P" + num + " DPad D")) buttons |= (1 << 2);
			if (Controller.IsPressed("P" + num + " DPad U")) buttons |= (1 << 3);
			if (Controller.IsPressed("P" + num + " Start")) buttons |= (1 << 4);
			if (Controller.IsPressed("P" + num + " Z")) buttons |= (1 << 5);
			if (Controller.IsPressed("P" + num + " B")) buttons |= (1 << 6);
			if (Controller.IsPressed("P" + num + " A")) buttons |= (1 << 7);
			if (Controller.IsPressed("P" + num + " C Right")) buttons |= (1 << 8);
			if (Controller.IsPressed("P" + num + " C Left")) buttons |= (1 << 9);
			if (Controller.IsPressed("P" + num + " C Down")) buttons |= (1 << 10);
			if (Controller.IsPressed("P" + num + " C Up")) buttons |= (1 << 11);
			if (Controller.IsPressed("P" + num + " R")) buttons |= (1 << 12);
			if (Controller.IsPressed("P" + num + " L")) buttons |= (1 << 13);

			return buttons;
		}

		/// <summary>
		/// Sets the controller pak of the controller to the specified type
		/// </summary>
		/// <param name="controller">Id of the controller</param>
		/// <param name="type">Type to which the controller pak is set. Currently only NO_PAK and MEMORY_CARD are supported</param>
		public void SetControllerPakType(int controller, N64SyncSettings.N64ControllerSettings.N64ControllerPakType type)
		{
			api.SetM64PControllerPakType(controller, type);
		}

		/// <summary>
		/// Sets the connection status of the controller
		/// </summary>
		/// <param name="controller">Id of the controller to connect or disconnect</param>
		/// <param name="connectionStatus">New status of the controller connection</param>
		public void SetControllerConnected(int controller, bool connectionStatus)
		{
			api.SetM64PControllerConnected(controller, connectionStatus);
		}
	}
}
