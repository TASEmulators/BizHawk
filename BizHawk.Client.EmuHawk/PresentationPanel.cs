using System;
using System.Drawing;
using sd=System.Drawing;
using sysdrawingfont=System.Drawing.Font;
using sysdrawing2d=System.Drawing.Drawing2D;
using System.IO;
using System.Threading;
using System.Windows.Forms;
#if WINDOWS
using SlimDX;
#endif

using BizHawk.Client.Common;
using BizHawk.Bizware.BizwareGL;

using OpenTK.Graphics.OpenGL;

namespace BizHawk.Client.EmuHawk
{
	/// <summary>
	/// Thinly wraps a BizwareGL.GraphicsControl for EmuHawk's needs
	/// </summary>
	public class PresentationPanel
	{
		public PresentationPanel()
		{
			GL = GlobalWin.GL;

			GraphicsControl = new GraphicsControl(GL);
			GraphicsControl.Dock = DockStyle.Fill;
			GraphicsControl.BackColor = Color.Black;

			//pass through these events to the form. we might need a more scalable solution for mousedown etc. for zapper and whatnot.
			//http://stackoverflow.com/questions/547172/pass-through-mouse-events-to-parent-control (HTTRANSPARENT)
			GraphicsControl.MouseDoubleClick += (o, e) => HandleFullscreenToggle(o, e);
			GraphicsControl.MouseClick += (o, e) => GlobalWin.MainForm.MainForm_MouseClick(o, e);
			GraphicsControl.MouseMove += (o, e) => GlobalWin.MainForm.MainForm_MouseMove(o, e);
		}

		bool IsDisposed = false;
		public void Dispose()
		{
			if (IsDisposed) return;
			IsDisposed = true;
			GraphicsControl.Dispose();
		}

		//graphics resources
		IGL GL;
		public GraphicsControl GraphicsControl;

		public Control Control { get { return GraphicsControl; } }
		public static implicit operator Control(PresentationPanel self) { return self.GraphicsControl; }

		private void HandleFullscreenToggle(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
			{
				//allow suppression of the toggle.. but if shift is pressed, always do the toggle
				bool allowSuppress = Control.ModifierKeys != Keys.Shift;
				if (Global.Config.DispChrome_AllowDoubleClickFullscreen || !allowSuppress)
				{
					GlobalWin.MainForm.ToggleFullscreen(allowSuppress);
				}
			}
		}

		public bool Resized { get; set; }

		public Size NativeSize { get { return GraphicsControl.ClientSize; } }
	}

	

	public interface IBlitterFont { }

}
