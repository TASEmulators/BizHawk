using System;
using System.Windows.Forms;

namespace BizHawk.Bizware.BizwareGL
{
	/// <summary>
	/// a base class for deriving/wrapping from a IGraphicsControl.
	/// This is to work around the annoyance that we cant inherit from a control whose type is unknown (it would be delivered by the selected BizwareGL driver)
	/// and so we have to resort to composition and c# sucks and events suck.
	/// </summary>
	public class GraphicsControl : UserControl
	{
		public GraphicsControl(IGL owner)
		{
			IGL = owner;

			SetStyle(ControlStyles.Opaque, true);
			SetStyle(ControlStyles.UserPaint, true);
			SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			SetStyle(ControlStyles.UserMouse, true);

			//in case we need it
			//GLControl.GetType().GetMethod("SetStyle", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(GLControl, new object[] { System.Windows.Forms.ControlStyles.UserMouse, true });
			

			IGC = owner.Internal_CreateGraphicsControl();
			Managed = IGC as Control;
			Managed.Dock = DockStyle.Fill;
			Controls.Add(Managed);

			//pass through these events to the form. I tried really hard to find a better way, but there is none.
			//(dont use HTTRANSPARENT, it isnt portable, I would assume)
			Managed.MouseDoubleClick += (object sender, MouseEventArgs e) => OnMouseDoubleClick(e);
			Managed.MouseClick += (object sender, MouseEventArgs e) => OnMouseClick(e);
			Managed.MouseEnter += (object sender, EventArgs e) => OnMouseEnter(e);
			Managed.MouseLeave += (object sender, EventArgs e) => OnMouseLeave(e);
			Managed.MouseMove += (object sender, MouseEventArgs e) => OnMouseMove(e);

			//the GraphicsControl is occupying all of our area. So we pretty much never get paint events ourselves.
			//So lets capture its paint event and use it for ourselves (it doesnt know how to do anything, anyway)
			Managed.Paint += new PaintEventHandler(GraphicsControl_Paint);
		}

		/// <summary>
		/// If this is the main window, things may be special
		/// </summary>
		public bool MainWindow;

		void GraphicsControl_Paint(object sender, PaintEventArgs e)
		{
			OnPaint(e);
		}

		public readonly IGL IGL;

		IGraphicsControl IGC;
		Control Managed;

		//public virtual Control Control { get { return Managed; } } //do we need this anymore?
		public virtual void SetVsync(bool state) { IGC.SetVsync(state); }
		public virtual void SwapBuffers() { IGC.SwapBuffers(); }
		public virtual void Begin() { IGC.Begin(); }
		public virtual void End() { IGC.End(); }
	}
}