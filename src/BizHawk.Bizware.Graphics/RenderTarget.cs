using System;

namespace BizHawk.Bizware.Graphics
{
	public class RenderTarget : IDisposable
	{
		public RenderTarget(IGL owner, object opaque, ITexture2D tex)
		{
			Owner = owner;
			Opaque = opaque;
			Texture2D = tex;
		}

		public override string ToString()
		{
			return $"GL RT: {Texture2D.Width}x{Texture2D.Height}";
		}

		public object Opaque { get; }
		public IGL Owner { get; }
		public ITexture2D Texture2D { get; }

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