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
			{
				return;
			}

			if (!TryGet(out var lua))
			{
				return;
			}

			lua.DisposeInternal(_Reference, finalized);
		}

		protected void Dispose(bool disposeManagedResources)
		{
			if (_disposed)
			{
				return;
			}

			var finalized = !disposeManagedResources;
			if (_Reference != 0)
			{
				DisposeLuaReference(finalized);
			}

			_lua = null;
			_disposed = true;
		}

		public override bool Equals(object o)
		{
			if (o is not LuaBase reference)
			{
				return false;
			}

			return TryGet(out var lua) && lua.CompareRef(reference._Reference, _Reference);
		}

		public override int GetHashCode()
			=> _Reference;
	}
}