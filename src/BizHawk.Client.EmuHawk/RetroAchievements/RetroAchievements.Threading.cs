using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

using BizHawk.Common;
using BizHawk.Client.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class RetroAchievements
	{
		private readonly RAInterface.IsActiveDelegate _isActive;
		private readonly RAInterface.UnpauseDelegate _unpause;
		private readonly RAInterface.PauseDelegate _pause;
		private readonly RAInterface.RebuildMenuDelegate _rebuildMenu;
		private readonly RAInterface.EstimateTitleDelegate _estimateTitle;
		private readonly RAInterface.ResetEmulatorDelegate _resetEmulator;
		private readonly RAInterface.LoadROMDelegate _loadROM;

		private readonly Func<ToolStripItemCollection> _getRADropDownItems;
		private readonly RAInterface.MenuItem[] _menuItems = new RAInterface.MenuItem[40];

		private readonly Action _shutdownRACallback;

		private readonly ThreadLocal<bool> _isMainThread = new() { Value = true };
		private bool IsMainThread => _isMainThread.Value;

		private readonly Thread _dialogThread;
		private volatile bool _dialogThreadActive;
		private volatile IntPtr _nextDialog = IntPtr.Zero;
		private readonly AutoResetEvent _dialogThrottle = new(false);

		private volatile Action _nextDelegate;
		private readonly AutoResetEvent _delegateEventDone = new(false);

		private void DialogThreadProc()
		{
			while (_dialogThreadActive)
			{
				// we want to periodically run this thread to pump messages for RA's dialogs
				// but we may want to wake this thread up in case a new dialog is present
				// (or if we want to shutdown this thread)
				_dialogThrottle.WaitOne(5);

				if (_nextDialog != IntPtr.Zero)
				{
					RA.InvokeDialog(_nextDialog);
					_nextDialog = IntPtr.Zero;
				}

				while (ThreadHacks.PeekMessage(out var msg, IntPtr.Zero, 0, 0, ThreadHacks.PM_REMOVE))
				{
					ThreadHacks.TranslateMessage(ref msg);
					ThreadHacks.DispatchMessage(ref msg);
				}
			}
		}

		private void RebuildMenu()
		{
			var numItems = RA.GetPopupMenuItems(_menuItems);
			var tsmiddi = _getRADropDownItems();
			tsmiddi.Clear();
			{
				var tsi = new ToolStripMenuItem("Shutdown RetroAchievements");
				tsi.Click += (_, _) =>
				{
					RA.Shutdown();
					_dialogThreadActive = false;
					_dialogThrottle.Set();
					// block until dialog thread shuts down
					while (_dialogThread.IsAlive)
					{
					}
					_shutdownRACallback();
				};
				tsmiddi.Add(tsi);
				var tss = new ToolStripSeparator();
				tsmiddi.Add(tss);
			}
			for (int i = 0; i < numItems; i++)
			{
				if (_menuItems[i].Label != IntPtr.Zero)
				{
					var tsi = new ToolStripMenuItem(Marshal.PtrToStringUni(_menuItems[i].Label))
					{
						Checked = _menuItems[i].Checked != 0,
					};
					var id = _menuItems[i].ID;
					tsi.Click += (_, _) =>
					{
						if (_nextDialog != IntPtr.Zero) return; // recursive call? let's just ignore this

						_nextDialog = id;
						_dialogThrottle.Set();
						while (_nextDialog != IntPtr.Zero && _dialogThread.IsAlive)
						{
							// we need to message pump while the InvokeDialog is doing things
							// (although the other thread will pump that dialog's messages once the dialog is created)
							Application.DoEvents();
							HandleNextDelegate(); // don't let the dialog thread get stuck on a delegate
						}

						_mainForm.UpdateWindowTitle();

						// dialog thread died?
						if (!_dialogThread.IsAlive)
						{
							RA.Shutdown();
							_shutdownRACallback();
							throw new InvalidOperationException("RetroAchievements dialog thread died unexpectingly???");
						}
					};
					tsmiddi.Add(tsi);
				}
				else
				{
					var tss = new ToolStripSeparator();
					tsmiddi.Add(tss);
				}
			}
		}

		private void SendNextDelegate(Action nextDelegate)
		{
			if (IsMainThread)
			{
				nextDelegate();
			}
			else
			{
				_nextDelegate = nextDelegate;
				_delegateEventDone.WaitOne();
			}
		}

		private bool IsActiveCallback()
		{
			bool ret = false;
			SendNextDelegate(() => ret = !Emu.IsNull());
			return ret;
		}

		private void UnpauseCallback()
			=> SendNextDelegate(_mainForm.UnpauseEmulator);

		private void PauseCallback()
			=> SendNextDelegate(_mainForm.PauseEmulator);

		private void RebuildMenuCallback()
			=> SendNextDelegate(RebuildMenu);

		private void EstimateTitleCallback(IntPtr buffer)
			=> SendNextDelegate(() =>
			{
				var name = Encoding.UTF8.GetBytes(Game?.Name ?? "No Game Info Available");
				Marshal.Copy(name, 0, buffer, Math.Min(name.Length, 256));
			});

		private void ResetEmulatorCallback()
			=> SendNextDelegate(() => _mainForm.RebootCore());

		private void LoadROMCallback(string path)
			=> SendNextDelegate(() =>
			{
				_mainForm.LoadRom(path, new LoadRomArgs { OpenAdvanced = OpenAdvancedSerializer.ParseWithLegacy(path) });
			});

		private bool _isInDelegate = false;

		// ONLY CALL THIS ON THE MAIN THREAD
		private void HandleNextDelegate()
		{
			if (!_isInDelegate) // prevent recursion (issue for RebootCore -> Update -> HandleNextDelegate)
			{
				var nextDelegate = _nextDelegate;
				if (nextDelegate is not null)
				{
					_isInDelegate = true;
					nextDelegate();
					_isInDelegate = false;

					_nextDelegate = null;
					_delegateEventDone.Set();
				}
			}
		}
	}
}
