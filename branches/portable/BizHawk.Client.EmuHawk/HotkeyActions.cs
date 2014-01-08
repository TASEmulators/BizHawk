using System;
using System.Collections.Generic;

namespace BizHawk.Client.EmuHawk
{
	public class HotkeyActions
	{
		private MainForm _mf;
		private Dictionary<string, Action> _hotkeys = new Dictionary<string, Action>();

		public HotkeyActions(MainForm mf)
		{
			_mf = mf;

			_hotkeys = new System.Collections.Generic.Dictionary<string,Action>
			{
				{ "Pause", _mf.TogglePause },
				{ "Soft Reset", _mf.SoftReset },
				{ "Hard Reset", _mf.HardReset },
				{ "Clear Autohold", _mf.ClearAutohold },
			};
		}

		public bool CheckHotkey(string key)
		{
			if (_hotkeys.ContainsKey(key))
			{
				_hotkeys[key]();
				return true;
			}

			return false;
		}
	}
}
