using System;
using System.Collections.Generic;

namespace BizHawk.Bizware.BizwareGL
{
	public class RenderTarget
	{
		public RenderTarget(IGL owner, IntPtr handle, Texture2d tex)
		{
			Owner = owner;
			Id = handle;
			Texture2d = tex;
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
	}
}