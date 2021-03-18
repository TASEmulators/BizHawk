using System;
using BizHawk.Bizware.BizwareGL;
using BizHawk.Bizware.DirectX;
using BizHawk.Bizware.OpenTK3;

namespace BizHawk.Client.EmuHawk
{
	/// <summary>
	/// This singleton class manages OpenGL contexts, in an effort to minimize context changes.
	/// </summary>
	public class GLManager : IDisposable
	{
		private GLManager()
		{
		}

		public void Dispose()
		{
		}

		private static readonly Lazy<GLManager> _lazyInstance = new Lazy<GLManager>(() => new GLManager());

		public static GLManager Instance => _lazyInstance.Value;

		public void ReleaseGLContext(object o)
		{
			var cr = (ContextRef)o;
			cr.GL.Dispose();
		}

		public ContextRef CreateGLContext(int majorVersion, int minorVersion, bool forwardCompatible)
		{
			var gl = new IGL_TK(majorVersion, minorVersion, forwardCompatible);
			var ret = new ContextRef { GL = gl };
			return ret;
		}

		public ContextRef GetContextForGraphicsControl(GraphicsControl gc)
		{
			return new ContextRef
			{
				Gc = gc,
				GL = gc.IGL
			};
		}

		private ContextRef _activeContext;

		public void Invalidate()
		{
			_activeContext = null;
		}

		public void Activate(ContextRef cr)
		{
			bool begun = false;

			//this needs a begin signal to set the swap chain to the next backbuffer
			if (cr.GL is IGL_SlimDX9)
			{
				cr.Gc.Begin();
				begun = true;
			}

			if (cr == _activeContext)
			{
				return;
			}

			_activeContext = cr;
			if (cr.Gc != null)
			{
				//TODO - this is checking the current context inside to avoid an extra NOP context change. make this optional or remove it, since we're tracking it here
				if (!begun)
				{
					cr.Gc.Begin();
				}
			}
			else
			{
				if (cr.GL is IGL_TK tk)
				{
					tk.MakeDefaultCurrent();
				}
			}
		}

		public void Deactivate()
		{
			//this is here for future use and tracking purposes.. however.. instead of relying on this, we should just make sure we always activate what we need before we use it
		}

		public class ContextRef
		{
			public IGL GL { get; set; }
			public GraphicsControl Gc { get; set; }
		}
	}
}