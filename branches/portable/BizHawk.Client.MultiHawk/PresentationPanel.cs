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

namespace BizHawk.Client.MultiHawk
{
	/// <summary>
	/// Thinly wraps a BizwareGL.GraphicsControl for EmuHawk's needs
	/// </summary>
	public class PresentationPanel
	{
		public PresentationPanel(Form parent, IGL gl)
		{
			GL = gl;

			GraphicsControl = new GraphicsControl(GL);
			GraphicsControl.Dock = DockStyle.Fill;
			GraphicsControl.BackColor = Color.Black;

			//pass through these events to the form. we might need a more scalable solution for mousedown etc. for zapper and whatnot.
			//http://stackoverflow.com/questions/547172/pass-through-mouse-events-to-parent-control (HTTRANSPARENT)
			
			// TODO
			//GraphicsControl.MouseClick += (o, e) => GlobalWin.MainForm.MainForm_MouseClick(o, e);
		}

		bool IsDisposed = false;
		public void Dispose()
		{
			if (IsDisposed) return;
			IsDisposed = true;
			GraphicsControl.Dispose();
		}

		//graphics resources
		public IGL GL { get; set; }
		public GraphicsControl GraphicsControl;

		public Control Control { get { return GraphicsControl; } }
		public static implicit operator Control(PresentationPanel self) { return self.GraphicsControl; }

		public bool Resized { get; set; }

		public Size NativeSize { get { return GraphicsControl.ClientSize; } }
	}

	

	public interface IBlitterFont { }

}
