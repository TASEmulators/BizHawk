using System.Windows.Forms;
using BizHawk.Bizware.BizwareGL;
using OpenTK;
using OpenTK.Graphics;

namespace BizHawk.Client.EmuHawk
{
	internal class GLControlWrapper : GLControl, IGraphicsControl
	{
		// Note: In order to work around bugs in OpenTK which sometimes do things to a context without making that context active first...
		// we are going to push and pop the context before doing stuff
		public GLControlWrapper(IGL_TK owner)
			: base(GraphicsMode.Default, 2, 0, GraphicsContextFlags.Default)
		{
			_owner = owner;
			_glControl = this;
		}

		private readonly GLControl _glControl;
		private readonly IGL_TK _owner;

		public Control Control => this;

		public void SetVsync(bool state)
		{
			_glControl.MakeCurrent();
			_glControl.VSync = state;
		}

		public void Begin()
		{
			if (!_glControl.Context.IsCurrent)
			{
				_owner.MakeContextCurrent(_glControl.Context, _glControl.WindowInfo);
			}
		}

		public void End()
		{
			_owner.MakeDefaultCurrent();
		}

		public new void SwapBuffers()
		{
			if (!_glControl.Context.IsCurrent)
			{
				MakeCurrent();
			}

			base.SwapBuffers();
		}
	}
}