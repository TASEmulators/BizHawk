using System.Windows.Forms;

using BizHawk.Common;

namespace BizHawk.Bizware.Graphics.Controls
{
	internal sealed class OpenGLControl : GraphicsControl
	{
		// workaround a bug with proprietary Nvidia drivers resulting in some BadMatch on setting context to current
		// seems they don't like whatever depth values mono's winforms ends up setting
		// mono's winforms seems to try to copy from the "parent" window, so we need to create a "friendly" window
		private static readonly Lazy<IntPtr> _x11GLParent = new(() =>
		{
			// ugh, i hate this, seems just returning an x11 window handle is not good enough (unlike on windows where handing some native HWND is good enough)
			// instead it has to have been created with mono's internal "Hwnd" class
			var hwnd = typeof(Control).Assembly.CreateInstance("System.Windows.Forms.Hwnd");
			if (hwnd == null)
			{
				Console.WriteLine("Couldn't find System.Windows.Forms.Hwnd");
				return IntPtr.Zero;
			}

			var clientWindowProp = hwnd.GetType().GetProperty("ClientWindow");
			if (clientWindowProp == null)
			{
				Console.WriteLine("Couldn't find ClientWindow prop");
				return IntPtr.Zero;
			}

			var x11Window = SDL2OpenGLContext.CreateDummyX11ParentWindow(3, 2, true);
			clientWindowProp.SetValue(hwnd, x11Window);
			return x11Window;
		});

		private readonly Action _initGLState;
		private SDL2OpenGLContext _context;

		public OpenGLControl(Action initGLState)
		{
			_initGLState = initGLState;

			// according to OpenTK, these are the styles we want to set
			SetStyle(ControlStyles.Opaque, true);
			SetStyle(ControlStyles.UserPaint, true);
			SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			SetStyle(ControlStyles.UserMouse, true);
			DoubleBuffered = false;
		}

		protected override CreateParams CreateParams
		{
			get
			{
				const int CS_VREDRAW = 0x1;
				const int CS_HREDRAW = 0x2;
				const int CS_OWNDC = 0x20;

				var cp = base.CreateParams;
				if (!OSTailoredCode.IsUnixHost)
				{
					// According to OpenTK, this is necessary for OpenGL on windows
					cp.ClassStyle |= CS_VREDRAW | CS_HREDRAW | CS_OWNDC;
				}
				else
				{
					// workaround buggy proprietary Nvidia drivers
					cp.Parent = _x11GLParent.Value;
				}

				return cp;
			}
		}

		protected override void OnHandleCreated(EventArgs e)
		{
			base.OnHandleCreated(e);
			_context = new(Handle, 3, 2, true);
			_initGLState();
		}

		protected override void OnHandleDestroyed(EventArgs e)
		{
			base.OnHandleDestroyed(e);
			_context.Dispose();
			_context = null;
		}

		private void MakeContextCurrent()
		{
			if (IsDisposed)
			{
				throw new ObjectDisposedException(nameof(OpenGLControl));
			}

			if (_context is null)
			{
				CreateControl();
			}
			else
			{
				_context.MakeContextCurrent();
			}
		}

		public override void AllowTearing(bool state)
		{
			// not controllable
		}

		public override void SetVsync(bool state)
		{
			MakeContextCurrent();
			_context.SetVsync(state);
		}

		public override void Begin()
			=> MakeContextCurrent();

		public override void End()
			=> SDL2OpenGLContext.MakeNoneCurrent();

		public override void SwapBuffers()
		{
			MakeContextCurrent();
			_context.SwapBuffers();
		}
	}
}
