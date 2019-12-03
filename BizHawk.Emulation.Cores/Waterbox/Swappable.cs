using BizHawk.Common;
using BizHawk.Common.BizInvoke;
using BizHawk.Common.BufferExtensions;
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
		/// start address
		/// </summary>
		private uint _lockkey;

		/// <summary>
		/// the the relevant lockinfo for this core
		/// </summary>
		private LockInfo _currentLockInfo;

		/// <summary>
		/// everything to swap in for context switches
		/// </summary>
		private List<MemoryBlockBase> _memoryBlocks = new List<MemoryBlockBase>();

		/// <summary>
		/// an informative name for each memory block:  used for debugging purposes
		/// </summary>
		private List<string> _memoryBlockNames = new List<string>();

		protected void AddMemoryBlock(MemoryBlockBase block, string name)
		{
			_memoryBlocks.Add(block);
			_memoryBlockNames.Add(name);
		}

		protected void PurgeMemoryBlocks()
		{
			_memoryBlocks = null;
		}

		protected void Initialize(ulong startAddress)
		{
			// any Swappables in the same 4G range are assumed to conflict
			var lockkey = (uint)(startAddress >> 32);

			_lockkey = lockkey;
			if (lockkey == 0)
				throw new NullReferenceException();
			_currentLockInfo = LockInfos.GetOrAdd(_lockkey, new LockInfo { Sync = new object() });
		}

		private class LockInfo
		{
			public object Sync;
			private WeakReference LoadedRef = new WeakReference(null);
#if DEBUG
			/// <summary>
			/// recursive lock count
			/// </summary>
			public int LockCount;
#endif
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

		private static readonly ConcurrentDictionary<uint, LockInfo> LockInfos = new ConcurrentDictionary<uint, LockInfo>();

		/// <summary>
		/// acquire lock and swap this into memory
		/// </summary>
		public void Enter()
		{
			Monitor.Enter(_currentLockInfo.Sync);
#if DEBUG
			if (_currentLockInfo.LockCount++ != 0 && _currentLockInfo.Loaded != this)
				throw new InvalidOperationException("Woops!");
#endif
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
#if DEBUG
			// when debugging, if we're releasing the lock then deactivate
			if (_currentLockInfo.LockCount-- == 1)
			{
				if (_currentLockInfo.Loaded != this)
					throw new InvalidOperationException("Woops!");
				DeactivateInternal();
				_currentLockInfo.Loaded = null;
			}
#endif
			Monitor.Exit(_currentLockInfo.Sync);
		}

		private void DeactivateInternal()
		{
			foreach (var m in _memoryBlocks)
				m.Deactivate();
		}

		private void ActivateInternal()
		{
			foreach (var m in _memoryBlocks)
				m.Activate();
		}
		
		public void PrintDebuggingInfo()
		{
			using (this.EnterExit())
			{
				foreach (var a in _memoryBlocks.Zip(_memoryBlockNames, (m, s) => new { m, s }))
				{
					Console.WriteLine($"{a.m.FullHash().BytesToHexString()}: {a.s}");
				}
			}
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
