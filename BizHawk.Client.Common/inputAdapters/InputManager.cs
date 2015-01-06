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
			Global.ControllerInputCoalescer.Type = Global.ActiveController.Type;

			Global.UD_LR_ControllerAdapter.Source = Global.ActiveController.Or(Global.AutoFireController);

			Global.StickyXORAdapter.Source = Global.UD_LR_ControllerAdapter;
			Global.AutofireStickyXORAdapter.Source = Global.StickyXORAdapter;

			Global.MultitrackRewiringAdapter.Source = Global.AutofireStickyXORAdapter;
			Global.MovieInputSourceAdapter.Source = Global.MultitrackRewiringAdapter;
			Global.ControllerOutput.Source = Global.MovieOutputHardpoint;

			Global.Emulator.Controller = Global.ControllerOutput;
			Global.MovieSession.MovieControllerAdapter.Type = Global.MovieInputSourceAdapter.Type;

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

		public static void SyncControls()
		{
			var def = Global.Emulator.ControllerDefinition;

			Global.ActiveController = BindToDefinition(def, Global.Config.AllTrollers, Global.Config.AllTrollersAnalog);
			Global.AutoFireController = BindToDefinitionAF(def, Global.Config.AllTrollersAutoFire);

			// allow propogating controls that are in the current controller definition but not in the prebaked one
			// these two lines shouldn't be required anymore under the new system?
			Global.ActiveController.ForceType(new ControllerDefinition(Global.Emulator.ControllerDefinition));
			Global.ClickyVirtualPadController.Type = new ControllerDefinition(Global.Emulator.ControllerDefinition);
			RewireInputChain();
		}

		private static Controller BindToDefinition(ControllerDefinition def, IDictionary<string, Dictionary<string, string>> allbinds, IDictionary<string, Dictionary<string, Config.AnalogBind>> analogbinds)
		{
			var ret = new Controller(def);
			Dictionary<string, string> binds;
			if (allbinds.TryGetValue(def.Name, out binds))
			{
				foreach (var cbutton in def.BoolButtons)
				{
					string bind;
					if (binds.TryGetValue(cbutton, out bind))
					{
						ret.BindMulti(cbutton, bind);
					}
				}
			}

			Dictionary<string, Config.AnalogBind> abinds;
			if (analogbinds.TryGetValue(def.Name, out abinds))
			{
				foreach (var cbutton in def.FloatControls)
				{
					Config.AnalogBind bind;
					if (abinds.TryGetValue(cbutton, out bind))
					{
						ret.BindFloat(cbutton, bind);
					}
				}
			}

			return ret;
		}

		private static AutofireController BindToDefinitionAF(ControllerDefinition def, IDictionary<string, Dictionary<string, string>> allbinds)
		{
			var ret = new AutofireController(def);
			Dictionary<string, string> binds;
			if (allbinds.TryGetValue(def.Name, out binds))
			{
				foreach (var cbutton in def.BoolButtons)
				{
					string bind;
					if (binds.TryGetValue(cbutton, out bind))
					{
						ret.BindMulti(cbutton, bind);
					}
				}
			}

			return ret;
		}
	}
}