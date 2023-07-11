using System;
using BizHawk.Bizware.BizwareGL;
using BizHawk.Bizware.Graphics;

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

		private static readonly Lazy<GLManager> _lazyInstance = new(() => new());

		public static GLManager Instance => _lazyInstance.Value;

		public void ReleaseGLContext(object o)
		{
			var cr = (ContextRef)o;
			cr.GL.Dispose();
		}

		public ContextRef CreateGLContext(int majorVersion, int minorVersion, bool forwardCompatible)
		{
			var gl = new IGL_OpenGL(majorVersion, minorVersion, forwardCompatible);
			return new() { GL = gl };
		}

		public static ContextRef GetContextForGraphicsControl(GraphicsControl gc)
		{
			return new()
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
			if (cr == _activeContext)
			{
				// D3D9 needs a begin signal to set the swap chain to the next backbuffer
				if (cr.GL.DispMethodEnum is EDispMethod.D3D9)
				{
					cr.Gc.Begin();
				}

				return;
			}

			_activeContext = cr;

			if (cr.Gc != null)
			{
				cr.Gc.Begin();
			}
			else
			{
				if (cr.GL is IGL_OpenGL gl)
				{
					gl.MakeOffscreenContextCurrent();
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