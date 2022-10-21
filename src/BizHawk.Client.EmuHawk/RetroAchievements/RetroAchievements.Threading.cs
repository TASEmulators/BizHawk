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

		private bool _isInDelegate = false;
		private volatile Action _nextDelegate;
		private readonly AutoResetEvent _delegateEventDone = new(false);

		private readonly SemaphoreSlim _memAccessCount = new(2, 2);
		private readonly AutoResetEvent _memAccessReady = new(false);
		private readonly AutoResetEvent _memAccessDone = new(false);

		private readonly SemaphoreSlim _asyncAccessCount = new(0, 2);
		private volatile bool _inRAFrame = false;

		private readonly RAMemGuard _memGuard;

		private struct RAMemGuard : IMonitor, IDisposable
		{
			private readonly SemaphoreSlim _count;
			private readonly AutoResetEvent _start;
			private readonly AutoResetEvent _end;
			private readonly Func<bool> _isNotMainThread;
			// this is more or less a hacky workaround from dialog thread access causing lockups during DoAchievementsFrame
			private readonly SemaphoreSlim _asyncCount;
			private readonly Func<bool> _needsLock;
			private readonly ThreadLocal<bool> _isLocked;
			private readonly Mutex _memMutex;

			public RAMemGuard(SemaphoreSlim count, AutoResetEvent start, AutoResetEvent end, Func<bool> isNotMainThread, SemaphoreSlim asyncCount, Func<bool> needsLock)
			{
				_count = count;
				_start = start;
				_end = end;
				_isNotMainThread = isNotMainThread;
				_asyncCount = asyncCount;
				_needsLock = needsLock;
				_isLocked = new();
				_memMutex = new();
			}

			public void Dispose()
			{
				_count.Dispose();
				_start.Dispose();
				_end.Dispose();
				_asyncCount.Dispose();
				_isLocked.Dispose();
				_memMutex.Dispose();
			}

			public void Enter()
			{
				if (_isNotMainThread())
				{
					_memMutex.WaitOne();
					_asyncCount.Release();

					if (_needsLock())
					{
						_count.Wait();
						_start.WaitOne();
						_isLocked.Value = true;
					}
				}
				else
				{
					// in some cases we may need to service out mem accesses currently stuck waiting
					// (if they're waiting here, they can't release the mutex)
					// we're the main thread, so it's safe to do this here
					while (!_memMutex.WaitOne(0))
					{
						while (_count.CurrentCount < 2)
						{
							_count.Release();
							_start.Set();
							_end.WaitOne();
						}
					}
				}
			}

			public void Exit()
			{
				if (_isNotMainThread())
				{
					if (_isLocked.Value)
					{
						_end.Set();
						_isLocked.Value = false;
					}

					_asyncCount.Wait();
				}

				_memMutex.ReleaseMutex();
			}
		}

		private void DialogThreadProc()
		{
			while (_dialogThreadActive)
			{
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

				Thread.Yield();
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
						while (_nextDialog != IntPtr.Zero && _dialogThread.IsAlive)
						{
							// we need to message pump while the InvokeDialog is doing things
							// (although the other thread will pump that dialog's messages once the dialog is created)
							Application.DoEvents();
							HandleNextDelegate(); // don't let the dialog thread get stuck on a delegate
							HandleMemAccess(); // or mem access
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

		// mem access sync technique
		// mem access count will be 2 to start, this indicates no threads are requesting to access memory
		// wait will decrement, release will incremented
		// so when another thread wants to read, it will call wait (ensuring the count is < 2)
		// and once the main thread is ready, it will call release (restoring the previous count)
		// mem access ready will ensure the other thread and main thread are actually synced (as count wait will not block)
		// mem access done will block the main thread until the other thread is done reading
		private void HandleMemAccess()
		{
			while (_memAccessCount.CurrentCount < 2)
			{
				_memAccessCount.Release();
				_memAccessReady.Set();
				_memAccessDone.WaitOne();
			}
		}
	}
}
