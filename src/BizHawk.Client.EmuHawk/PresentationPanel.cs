using System.Drawing;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Bizware.Graphics;
using BizHawk.Bizware.Graphics.Controls;

namespace BizHawk.Client.EmuHawk
{
	/// <summary>
	/// Thinly wraps a GraphicsControl for EmuHawk's needs
	/// </summary>
	public class PresentationPanel
	{
		private readonly Config _config;

		private readonly Action<bool> _fullscreenToggleCallback;

		public PresentationPanel(
			Config config,
			IGL gl,
			Action<bool> fullscreenToggleCallback,
			MouseEventHandler onClick,
			MouseEventHandler onMove,
			MouseEventHandler onWheel)
		{
			_config = config;

			_fullscreenToggleCallback = fullscreenToggleCallback;

			GraphicsControl = GraphicsControlFactory.CreateGraphicsControl(gl);
			GraphicsControl.Dock = DockStyle.Fill;
			GraphicsControl.BackColor = Color.Black;

			// pass through these events to the form. we might need a more scalable solution for mousedown etc. for zapper and whatnot.
			// http://stackoverflow.com/questions/547172/pass-through-mouse-events-to-parent-control (HTTRANSPARENT)
			GraphicsControl.MouseClick += onClick;
			GraphicsControl.MouseDoubleClick += HandleFullscreenToggle;
			GraphicsControl.MouseMove += onMove;
			GraphicsControl.MouseWheel += onWheel;
		}

		private bool _isDisposed;

		public void Dispose()
		{
			if (_isDisposed)
			{
				return;
			}

			_isDisposed = true;
			GraphicsControl.Dispose();
		}

		// graphics resources
		public readonly GraphicsControl GraphicsControl;

		public Control Control => GraphicsControl;
		public static implicit operator Control(PresentationPanel self) { return self.GraphicsControl; }

		private void HandleFullscreenToggle(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
			{
				// allow suppression of the toggle.. but if shift is pressed, always do the toggle
				var allowSuppress = Control.ModifierKeys != Keys.Shift;
				if (_config.DispChromeAllowDoubleClickFullscreen || !allowSuppress)
				{
					_fullscreenToggleCallback(allowSuppress);
				}
			}
		}

		public bool Resized { get; set; }

		public Size NativeSize => GraphicsControl.ClientSize;
	}
}
