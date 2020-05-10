using System;

namespace BizHawk.Bizware.BizwareGL
{
	public class RenderTarget : IDisposable
	{
		public RenderTarget(IGL owner, object opaque, Texture2d tex)
		{
			Owner = owner;
			Opaque = opaque;
			Texture2d = tex;
			tex.IsUpsideDown = true;
		}

		public override string ToString()
		{
			return $"GL RT: {Texture2d.Width}x{Texture2d.Height}";
		}

		public object Opaque { get; }
		public IGL Owner { get; }
		public Texture2d Texture2d { get; }

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