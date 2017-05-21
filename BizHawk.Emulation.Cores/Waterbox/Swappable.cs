using BizHawk.Common;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace BizHawk.Emulation.Cores.Waterbox
{
	/// <summary>
	/// represents an object that can be swapped in and out of memory to compete with other objects in the same memory
	/// not suited for general purpose stuff
	/// </summary>
	public abstract class Swappable : IMonitor, IDisposable
	{
		/// <summary>
		/// start address, or 0 if we don't need to be swapped
		/// </summary>
		private ulong _lockkey = 0;

		/// <summary>
		/// the the relevant lockinfo for this core
		/// </summary>
		private LockInfo _currentLockInfo;

		/// <summary>
		/// everything to swap in for context switches
		/// </summary>
		private List<MemoryBlock> _memoryBlocks = new List<MemoryBlock>();

		protected void AddMemoryBlock(MemoryBlock block)
		{
			_memoryBlocks.Add(block);
		}

		protected void PurgeMemoryBlocks()
		{
			_memoryBlocks = null;
		}

		protected void Initialize(ulong lockkey)
		{
			_lockkey = lockkey;
			if (lockkey != 0)
			{
				_currentLockInfo = LockInfos.GetOrAdd(_lockkey, new LockInfo { Sync = new object() });
			}
		}

		/// <summary>
		/// true if the IMonitor should be used for native calls
		/// </summary>
		public bool ShouldMonitor { get { return _lockkey != 0; } }

		// any Swappable is assumed to conflict with any other Swappable at the same base address,
		// but not any other starting address.  so don't put them too close together!

		private class LockInfo
		{
			public object Sync;
			private WeakReference LoadedRef = new WeakReference(null);
			public Swappable Loaded
			{
				get
				{
					// if somehow an object died without being disposed,
					// the MemoryBlock finalizer will have unloaded the memory
					// and so we can treat it as if no Swappable was attached
					return (Swappable)LoadedRef.Target;
				}
				set
				{
					LoadedRef.Target = value;
				}
			}
		}

		private static readonly ConcurrentDictionary<ulong, LockInfo> LockInfos = new ConcurrentDictionary<ulong, LockInfo>();

		/// <summary>
		/// acquire lock and swap this into memory
		/// </summary>
		public void Enter()
		{
			Monitor.Enter(_currentLockInfo.Sync);
			if (_currentLockInfo.Loaded != this)
			{
				if (_currentLockInfo.Loaded != null)
					_currentLockInfo.Loaded.DeactivateInternal();
				_currentLockInfo.Loaded = null;
				ActivateInternal();
				_currentLockInfo.Loaded = this;
			}
		}

		/// <summary>
		/// release lock
		/// </summary>
		public void Exit()
		{
			Monitor.Exit(_currentLockInfo.Sync);
		}

		private void DeactivateInternal()
		{
			Console.WriteLine("Swappable DeactivateInternal {0}", GetHashCode());
			foreach (var m in _memoryBlocks)
				m.Deactivate();
		}

		private void ActivateInternal()
		{
			Console.WriteLine("Swappable ActivateInternal {0}", GetHashCode());
			foreach (var m in _memoryBlocks)
				m.Activate();
		}

		private bool _disposed = false;

		protected virtual void Dispose(bool disposing)
		{
			if (!_disposed)
			{
				if (disposing)
				{
					lock (_currentLockInfo.Sync)
					{
						if (_currentLockInfo.Loaded == this)
						{
							DeactivateInternal();
							_currentLockInfo.Loaded = null;
						}
						_currentLockInfo = null;
					}
				}
				_disposed = true;
			}
		}

		public void Dispose()
		{
			Dispose(true);
		}
	}
}
