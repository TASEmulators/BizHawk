using System.Drawing;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Bizware.BizwareGL;

namespace BizHawk.Client.EmuHawk
{
	/// <summary>
	/// Thinly wraps a BizwareGL.GraphicsControl for EmuHawk's needs
	/// </summary>
	public class PresentationPanel
	{
		private readonly MainForm _mainForm;
		private readonly Config _config;

		public PresentationPanel(MainForm mainForm, Config config, IGL gl)
		{
			_mainForm = mainForm;
			_config = config;

			GraphicsControl = new GraphicsControl(gl)
			{
				Dock = DockStyle.Fill,
				BackColor = Color.Black
			};

			// pass through these events to the form. we might need a more scalable solution for mousedown etc. for zapper and whatnot.
			// http://stackoverflow.com/questions/547172/pass-through-mouse-events-to-parent-control (HTTRANSPARENT)
			GraphicsControl.MouseDoubleClick += HandleFullscreenToggle;
			GraphicsControl.MouseClick += (o, e) => _mainForm.MainForm_MouseClick(o, e);
			GraphicsControl.MouseMove += (o, e) => _mainForm.MainForm_MouseMove(o, e);
			GraphicsControl.MouseWheel += (o, e) => _mainForm.MainForm_MouseWheel(o, e);
		}

		private bool _isDisposed;
		public void Dispose()
		{
			if (_isDisposed) return;
			_isDisposed = true;
			GraphicsControl.Dispose();
		}

		//graphics resources
		public GraphicsControl GraphicsControl;

		public Control Control => GraphicsControl;
		public static implicit operator Control(PresentationPanel self) { return self.GraphicsControl; }

		private void HandleFullscreenToggle(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
			{
				// allow suppression of the toggle.. but if shift is pressed, always do the toggle
				bool allowSuppress = Control.ModifierKeys != Keys.Shift;
				if (_config.DispChrome_AllowDoubleClickFullscreen || !allowSuppress)
				{
					_mainForm.ToggleFullscreen(allowSuppress);
				}
			}
		}

		public bool Resized { get; set; }

		public Size NativeSize => GraphicsControl.ClientSize;
	}

	public interface IBlitterFont { }
}
