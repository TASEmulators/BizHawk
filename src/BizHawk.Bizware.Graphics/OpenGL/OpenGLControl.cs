using System;
using System.Windows.Forms;

using BizHawk.Bizware.BizwareGL;
using BizHawk.Common;

namespace BizHawk.Bizware.Graphics
{
	internal class OpenGLControl : Control, IGraphicsControl
	{
		private readonly bool _openGL3;
		private readonly Action _contextChangeCallback;

		public SDL2OpenGLContext Context { get; private set; }

		public RenderTargetWrapper RenderTargetWrapper
		{
			get => throw new NotImplementedException();
			set => throw new NotImplementedException();
		}

		public OpenGLControl(bool openGL3, Action contextChangeCallback)
		{
			_openGL3 = openGL3;
			_contextChangeCallback = contextChangeCallback;

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

				return cp;
			}
		}

		protected override void OnHandleCreated(EventArgs e)
		{
			base.OnHandleCreated(e);
			Context = new(Handle, _openGL3 ? 3 : 2, 0, false, false);
			_contextChangeCallback();
		}

		protected override void OnHandleDestroyed(EventArgs e)
		{
			base.OnHandleDestroyed(e);

			if (Context.IsCurrent)
			{
				_contextChangeCallback();
			}

			Context.Dispose();
			Context = null;
		}

		private void MakeContextCurrent()
		{
			if (IsDisposed)
			{
				throw new ObjectDisposedException(nameof(OpenGLControl));
			}

			if (Context is null)
			{
				CreateControl();
			}
			else
			{
				if (!Context.IsCurrent)
				{
					Context.MakeContextCurrent();
					_contextChangeCallback();
				}
			}
		}

		public void SetVsync(bool state)
		{
			MakeContextCurrent();
			Context.SetVsync(state);
		}

		public void Begin()
		{
			MakeContextCurrent();
		}

		public void End()
		{
			SDL2OpenGLContext.MakeNoneCurrent();
			_contextChangeCallback();
		}

		public void SwapBuffers()
		{
			MakeContextCurrent();
			Context.SwapBuffers();
		}
	}
}