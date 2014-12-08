using System;
using System.Collections.Generic;

namespace BizHawk.Bizware.BizwareGL
{
	/// <summary>
	/// Just a lifecycle-managed wrapper around shader handles
	/// </summary>
	public class Shader : IDisposable
	{
		public Shader(IGL owner, IntPtr id, bool available)
		{
			Owner = owner;
			Id = id;
			Available = available;
		}

		public IGL Owner { get; private set; }
		public IntPtr Id { get; private set; }
		public bool Disposed { get; private set; }
		public bool Available { get; private set; }
		public object Opaque;

		public void Dispose()
		{
			if (Disposed) return;
			Disposed = true;
			Owner.FreeShader(Id);
			Id = Owner.GetEmptyHandle();
		}
	}
}