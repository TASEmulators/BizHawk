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
	public abstract class Swappable : IMonitor
	{
		/// <summary>
		/// start address, or 0 if we don't need to be swapped
		/// </summary>
		private ulong _lockkey = 0;

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
			public Swappable Loaded;
		}

		private static readonly ConcurrentDictionary<ulong, LockInfo> LockInfos = new ConcurrentDictionary<ulong, LockInfo>();

		static Swappable()
		{
			LockInfos.GetOrAdd(0, new LockInfo()); // any errant attempt to lock when ShouldMonitor == false will result in NRE
		}

		/// <summary>
		/// acquire lock and swap this into memory
		/// </summary>
		public void Enter()
		{
			var li = LockInfos.GetOrAdd(_lockkey, new LockInfo { Sync = new object() });
			Monitor.Enter(li.Sync);
			if (li.Loaded != this)
			{
				if (li.Loaded != null)
					li.Loaded.DeactivateInternal();
				li.Loaded = null;
				ActivateInternal();
				li.Loaded = this;
			}
		}

		/// <summary>
		/// release lock
		/// </summary>
		public void Exit()
		{
			var li = LockInfos.GetOrAdd(_lockkey, new LockInfo { Sync = new object() });
			Monitor.Exit(li.Sync);
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

	}
}
