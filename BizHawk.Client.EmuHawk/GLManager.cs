using System;
using BizHawk.Bizware.BizwareGL;


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

		public static GLManager Instance { get; private set; }

		Bizware.BizwareGL.Drivers.OpenTK.IGL_TK MainContext;

		public static void CreateInstance(Bizware.BizwareGL.Drivers.OpenTK.IGL_TK mainContext)
		{
			if (Instance != null) throw new InvalidOperationException("Attempt to create more than one GLManager");
			Instance = new GLManager();
			Instance.MainContext = mainContext;
		}

		public void ReleaseGLContext(object o)
		{
			ContextRef cr = (ContextRef)o;
			cr.gl.Dispose();
		}

		//[System.Runtime.InteropServices.DllImport("opengl32.dll")]
		//bool wglShareLists(IntPtr hglrc1, IntPtr hglrc2);

		public ContextRef CreateGLContext(int major_version, int minor_version, bool forward_compatible)
		{
			var gl = new Bizware.BizwareGL.Drivers.OpenTK.IGL_TK(major_version, minor_version, forward_compatible);
			var ret = new ContextRef { gl = gl };
			return ret;
		}

		public ContextRef GetContextForGraphicsControl(GraphicsControl gc)
		{
			return new ContextRef
			{
				gc = gc,
				gl = gc.IGL
			};
		}

		/// <summary>
		/// This might not be a GL implementation. If it isnt GL, then setting it as active context is just NOP
		/// </summary>
		public ContextRef GetContextForIGL(IGL gl)
		{
			return new ContextRef
			{
				gl = gl
			};
		}

		ContextRef ActiveContext;

		public void Invalidate()
		{
			ActiveContext = null;
		}

		public void Activate(ContextRef cr)
		{
			bool begun = false;

			//this needs a begin signal to set the swap chain to the next backbuffer
			if (cr.gl is BizHawk.Bizware.BizwareGL.Drivers.SlimDX.IGL_SlimDX9)
			{
				cr.gc.Begin();
				begun = true;
			}

			if (cr == ActiveContext)
				return;

			ActiveContext = cr;
			if (cr.gc != null)
			{
				//TODO - this is checking the current context inside to avoid an extra NOP context change. make this optional or remove it, since we're tracking it here
				if(!begun)
					cr.gc.Begin();
			}
			else if (cr.gl != null)
			{
				if(cr.gl is BizHawk.Bizware.BizwareGL.Drivers.OpenTK.IGL_TK)
					((BizHawk.Bizware.BizwareGL.Drivers.OpenTK.IGL_TK)cr.gl).MakeDefaultCurrent();
			}
		}

		public void Deactivate()
		{
			//this is here for future use and tracking purposes.. however.. instead of relying on this, we should just make sure we always activate what we need before we use it
		}

		public class ContextRef
		{
			public IGL gl;
			public GraphicsControl gc;
		}
	}
}