using System;
using System.Collections.Generic;

namespace BizHawk.Bizware.BizwareGL
{
	/// <summary>
	/// Just a lifecycle-managed wrapper around shader handles
	/// </summary>
	public class Shader : IDisposable
	{
		public Shader(IGL owner, IntPtr id)
		{
			Owner = owner;
			Id = id;
		}

		public IGL Owner { get; private set; }
		public IntPtr Id { get; private set; }
		public bool Disposed { get; private set; }

		public void Dispose()
		{
			if (Disposed) return;
			Disposed = true;
			Owner.FreeShader(Id);
			Id = Owner.GetEmptyHandle();
		}
	}
}