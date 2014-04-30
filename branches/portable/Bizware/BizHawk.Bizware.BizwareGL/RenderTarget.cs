using System;
using System.Collections.Generic;

namespace BizHawk.Bizware.BizwareGL
{
	public class RenderTarget : IDisposable
	{
		public RenderTarget(IGL owner, IntPtr handle, Texture2d tex)
		{
			Owner = owner;
			Id = handle;
			Texture2d = tex;
			tex.IsUpsideDown = true;
		}

		public override string ToString()
		{
			return string.Format("GL RT: {0}x{1}", Texture2d.Width, Texture2d.Height);
		}

		public IntPtr Id { get; private set; }
		public IGL Owner { get; private set; }
		public Texture2d Texture2d { get; private set; }

		public void Unbind()
		{
			Owner.BindRenderTarget(null);
		}

		public void Bind()
		{
			Owner.BindRenderTarget(this);
		}

		public void Dispose()
		{
			Owner.FreeRenderTarget(this);
		}
	}
}