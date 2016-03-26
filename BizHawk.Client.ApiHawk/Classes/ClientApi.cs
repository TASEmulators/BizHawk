using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using BizHawk.Client.Common;
using BizHawk.Emulation.Cores.Nintendo.Gameboy;
using BizHawk.Emulation.Cores.PCEngine;
using BizHawk.Emulation.Cores.Sega.MasterSystem;

namespace BizHawk.Client.ApiHawk
{
	/// <summary>
	/// This class contains some methods that
	/// interract with BizHawk client
	/// </summary>
	public static class ClientApi
	{
		#region Fields

		private static readonly Assembly clientAssembly;
		private static readonly object clientMainForm;
		internal static readonly BizHawkSystemIdToEnumConverter SystemIdConverter = new BizHawkSystemIdToEnumConverter();
		internal static readonly JoypadStringToEnumConverter JoypadConverter = new JoypadStringToEnumConverter();

		public static event EventHandler RomLoaded;

		private static List<Joypad> allJoypads;

		#endregion

		#region cTor(s)

		static ClientApi()
		{
			clientAssembly = Assembly.GetEntryAssembly();
			clientMainForm = clientAssembly.GetType("BizHawk.Client.EmuHawk.GlobalWin").GetField("MainForm").GetValue(null);
		}

		#endregion

		#region Methods

		#region Public
		/// <summary>
		/// THE FrameAdvance stuff
		/// </summary>
		public static void DoFrameAdvance()
		{
			Type reflectClass = clientAssembly.GetType("BizHawk.Client.EmuHawk.MainForm");
			MethodInfo method = reflectClass.GetMethod("FrameAdvance");
			method.Invoke(clientMainForm, null);
		}

		/// <summary>
		/// THE FrameAdvance stuff
		/// Auto unpause emulation
		/// </summary>
		public static void DoFrameAdvanceAndUnpause()
		{
			Type reflectClass = clientAssembly.GetType("BizHawk.Client.EmuHawk.MainForm");
			MethodInfo method = reflectClass.GetMethod("FrameAdvance");
			method.Invoke(clientMainForm, null);
			method = reflectClass.GetMethod("UnpauseEmulator");
			method.Invoke(clientMainForm, null);
		}

		/// <summary>
		/// Gets a <see cref="Joypad"/> for specified player
		/// </summary>
		/// <param name="player">Player (one based) you want current inputs</param>
		/// <returns>A <see cref="Joypad"/> populated with current inputs</returns>
		/// <exception cref="IndexOutOfRangeException">Raised when you specify a player less than 1 or greater than maximum allows (see SystemInfo class to get this information)</exception>
		public static Joypad GetInput(int player)
		{
			if (player < 1 || player > RunningSystem.MaxControllers)
			{
				throw new IndexOutOfRangeException(string.Format("{0} does not support {1} controller(s)", RunningSystem.DisplayName, player));
			}
			else
			{
				GetAllInputs();
				return allJoypads[player - 1];
			}
		}

		/// <summary>
		/// Raise when a rom is successfully Loaded
		/// </summary>
		public static void OnRomLoaded()
		{
			if (RomLoaded != null)
			{
				RomLoaded(null, EventArgs.Empty);
			}

			allJoypads = new List<Joypad>(RunningSystem.MaxControllers);
			for (int i = 1; i <= RunningSystem.MaxControllers; i++)
			{
				allJoypads.Add(new Joypad(RunningSystem, i));
			}
		}


		/// <summary>
		/// Sets the extra padding added to the 'native' surface so that you can draw HUD elements in predictable placements
		/// </summary>
		/// <param name="left">Left padding</param>
		/// <param name="top">Top padding</param>
		/// <param name="right">Right padding</param>
		/// <param name="bottom">Bottom padding</param>
		public static void SetExtraPadding(int left, int top, int right, int bottom)
		{
			Type emuLuaLib = clientAssembly.GetType("BizHawk.Client.EmuHawk.EmuHawkLuaLibrary");
			MethodInfo paddingMethod = emuLuaLib.GetMethod("SetClientExtraPadding");
			paddingMethod.Invoke(paddingMethod, new object[] { left, top, right, bottom });
		}

		/// <summary>
		/// Sets the extra padding added to the 'native' surface so that you can draw HUD elements in predictable placements
		/// </summary>
		/// <param name="left">Left padding</param>
		public static void SetExtraPadding(int left)
		{
			SetExtraPadding(left, 0, 0, 0);
		}

		/// <summary>
		/// Sets the extra padding added to the 'native' surface so that you can draw HUD elements in predictable placements
		/// </summary>
		/// <param name="left">Left padding</param>
		/// <param name="top">Top padding</param>
		public static void SetExtraPadding(int left, int top)
		{
			SetExtraPadding(left, top, 0, 0);
		}

		/// <summary>
		/// Sets the extra padding added to the 'native' surface so that you can draw HUD elements in predictable placements
		/// </summary>
		/// <param name="left">Left padding</param>
		/// <param name="top">Top padding</param>
		/// <param name="right">Right padding</param>
		public static void SetExtraPadding(int left, int top, int right)
		{
			SetExtraPadding(left, top, right, 0);
		}


		/// <summary>
		/// Set inputs in specified <see cref="Joypad"/> to specified player
		/// </summary>
		/// <param name="player">Player (one based) whom inputs must be set</param>
		/// <param name="joypad"><see cref="Joypad"/> with inputs</param>
		/// <exception cref="IndexOutOfRangeException">Raised when you specify a player less than 1 or greater than maximum allows (see SystemInfo class to get this information)</exception>
		public static void SetInput(int player, Joypad joypad)
		{
			if (player < 1 || player > RunningSystem.MaxControllers)
			{
				throw new IndexOutOfRangeException(string.Format("{0} does not support {1} controller(s)", RunningSystem.DisplayName, player));
			}
			else
			{
				allJoypads[player - 1] = joypad;				
				Parallel.ForEach<JoypadButton>((IEnumerable<JoypadButton>)Enum.GetValues(typeof(JoypadButton)), button =>
				{
					if (joypad.Inputs.HasFlag(button))
					{
						//joypadAdaptor[string.Format("P{0} {1}", player, JoypadConverter.ConvertBack(button, RunningSystem))] = true;
						//joypadAdaptor.S
						Global.LuaAndAdaptor.SetButton(string.Format("P{0} {1}", player, JoypadConverter.ConvertBack(button, RunningSystem)), true);
						Global.ActiveController.Overrides(Global.LuaAndAdaptor);
					}
				}
				);
			}
		}


		/// <summary>
		/// Resume the emulation
		/// </summary>
		public static void UnpauseEmulation()
		{
			Type reflectClass = clientAssembly.GetType("BizHawk.Client.EmuHawk.MainForm");
			MethodInfo method = reflectClass.GetMethod("UnpauseEmulator");
			method.Invoke(clientMainForm, null);
		}
		#endregion Public

		/// <summary>
		/// Gets all current inputs for each joypad and store
		/// them in <see cref="Joypad"/> class collection
		/// </summary>
		private static void GetAllInputs()
		{
			AutoFireStickyXorAdapter joypadAdaptor = Global.AutofireStickyXORAdapter;

			IEnumerable<string> pressedButtons = from button in joypadAdaptor.Type.BoolButtons
												 where joypadAdaptor[button]
												 select button;

			foreach (Joypad j in allJoypads)
			{
				j.ClearInputs();
			}

			Parallel.ForEach<string>(pressedButtons, button =>
			{
				int player;
				if (int.TryParse(button.Substring(1, 2), out player))
				{
					allJoypads[player - 1].AddInput(JoypadConverter.Convert(button.Substring(3)));
				}
			});

			if ((RunningSystem.AvailableButtons & JoypadButton.AnalogStick) == JoypadButton.AnalogStick)
			{
				//joypadAdaptor.GetFloat();
			}
		}

		#endregion

		#region Properties

		/// <summary>
		/// Gets current emulated system
		/// </summary>
		public static SystemInfo RunningSystem
		{
			get
			{
				switch (Global.Emulator.SystemId)
				{
					case "PCE":
						if (((PCEngine)Global.Emulator).Type == NecSystemType.TurboGrafx)
						{
							return SystemInfo.PCE;
						}
						else if (((PCEngine)Global.Emulator).Type == NecSystemType.SuperGrafx)
						{
							return SystemInfo.SGX;
						}
						else
						{
							return SystemInfo.PCECD;
						}

					case "SMS":
						if (((SMS)Global.Emulator).IsSG1000)
						{
							return SystemInfo.SG;
						}
						else if (((SMS)Global.Emulator).IsGameGear)
						{
							return SystemInfo.GG;
						}
						else
						{
							return SystemInfo.SMS;
						}

					case "GB":
						if (Global.Emulator is Gameboy)
						{
							return SystemInfo.GB;
						}
						else if (Global.Emulator is GBColors)
						{
							return SystemInfo.GBC;
						}
						else
						{
							return SystemInfo.DualGB;
						}

					default:
						return SystemInfo.FindByCoreSystem(SystemIdConverter.Convert(Global.Emulator.SystemId));
				}
			}
		}

		#endregion
	}
}
