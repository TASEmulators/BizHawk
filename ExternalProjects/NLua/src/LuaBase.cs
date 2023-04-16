using System;

namespace NLua
{
	/// <summary>
	/// Base class to provide consistent disposal flow across lua objects. Uses code provided by Yves Duhoux and suggestions by Hans Schmeidenbacher and Qingrui Li 
	/// </summary>
	public abstract class LuaBase : IDisposable
	{
		private bool _disposed;
		protected readonly int _Reference;
		internal Lua _lua;

		protected bool TryGet(out Lua lua)
		{
			if (_lua.State == null)
			{
				lua = null;
				return false;
			}

			lua = _lua;
			return true;
		}

		protected LuaBase(int reference, Lua lua)
		{
			_lua = lua;
			_Reference = reference;
		}

		~LuaBase()
		{
			Dispose(false);
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		internal void DisposeLuaReference(bool finalized)
		{
			if (_lua == null)
				return;
			Lua lua;
			if (!TryGet(out lua))
				return;

			lua.DisposeInternal(_Reference, finalized);
		}

		public virtual void Dispose(bool disposeManagedResources)
		{
			if (_disposed)
				return;

			bool finalized = !disposeManagedResources;

			if (_Reference != 0)
			{
				DisposeLuaReference(finalized);
			}

			_lua = null;
			_disposed = true;
		}

		public override bool Equals(object o)
		{
			var reference = o as LuaBase;
			if (reference == null)
				return false;

			Lua lua;
			if (!TryGet(out lua))
				return false;

			return lua.CompareRef(reference._Reference, _Reference);
		}

		public override int GetHashCode()
		{
			return _Reference;
		}
	}
}