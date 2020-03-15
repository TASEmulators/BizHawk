using System.Collections.Generic;

using BizHawk.Emulation.Common;
using BizHawk.Client.Common.InputAdapterExtensions;

namespace BizHawk.Client.Common
{
	public static class InputManager
	{
		public static void RewireInputChain()
		{
			Global.ControllerInputCoalescer.Clear();
			Global.ControllerInputCoalescer.Definition = Global.ActiveController.Definition;

			Global.UD_LR_ControllerAdapter.Source = Global.ActiveController.Or(Global.AutoFireController);

			Global.StickyXORAdapter.Source = Global.UD_LR_ControllerAdapter;
			Global.AutofireStickyXORAdapter.Source = Global.StickyXORAdapter;

			Global.MultitrackRewiringAdapter.Source = Global.AutofireStickyXORAdapter;
			Global.MovieInputSourceAdapter.Source = Global.MultitrackRewiringAdapter;
			Global.ControllerOutput.Source = Global.MovieOutputHardpoint;

			Global.MovieSession.MovieControllerAdapter.Definition = Global.MovieInputSourceAdapter.Definition;

			// connect the movie session before MovieOutputHardpoint if it is doing anything
			// otherwise connect the MovieInputSourceAdapter to it, effectively bypassing the movie session
			if (Global.MovieSession != null)
			{
				Global.MovieOutputHardpoint.Source = Global.MovieSession.MovieControllerAdapter;
			}
			else
			{
				Global.MovieOutputHardpoint.Source = Global.MovieInputSourceAdapter;
			}
		}

		public static void SyncControls(IEmulator emulator, Config config)
		{
			var def = emulator.ControllerDefinition;

			Global.ActiveController = BindToDefinition(def, config.AllTrollers, config.AllTrollersAnalog);
			Global.AutoFireController = BindToDefinitionAF(def, emulator, config.AllTrollersAutoFire);

			// allow propagating controls that are in the current controller definition but not in the prebaked one
			// these two lines shouldn't be required anymore under the new system?
			Global.ActiveController.ForceType(new ControllerDefinition(def));
			Global.ClickyVirtualPadController.Definition = new ControllerDefinition(def);
			RewireInputChain();
		}

		private static Controller BindToDefinition(ControllerDefinition def, IDictionary<string, Dictionary<string, string>> allBinds, IDictionary<string, Dictionary<string, AnalogBind>> analogBinds)
		{
			var ret = new Controller(def);
			if (allBinds.TryGetValue(def.Name, out var binds))
			{
				foreach (var btn in def.BoolButtons)
				{
					if (binds.TryGetValue(btn, out var bind))
					{
						ret.BindMulti(btn, bind);
					}
				}
			}

			if (analogBinds.TryGetValue(def.Name, out var aBinds))
			{
				foreach (var btn in def.FloatControls)
				{
					if (aBinds.TryGetValue(btn, out var bind))
					{
						ret.BindFloat(btn, bind);
					}
				}
			}

			return ret;
		}

		private static AutofireController BindToDefinitionAF(ControllerDefinition def, IEmulator emulator, IDictionary<string, Dictionary<string, string>> allBinds)
		{
			var ret = new AutofireController(def, emulator);
			if (allBinds.TryGetValue(def.Name, out var binds))
			{
				foreach (var btn in def.BoolButtons)
				{
					if (binds.TryGetValue(btn, out var bind))
					{
						ret.BindMulti(btn, bind);
					}
				}
			}

			return ret;
		}
	}
}