#nullable enable

using System.Threading;
using System.Windows.Forms;

using Veldrid;

namespace BizHawk.Client.EmuHawk
{
	public sealed class VeldridSandbox : Form
	{
		private bool _isAlive = true;

		public VeldridSandbox()
		{
			VeldridCubeDemo demo = new() { Dock = DockStyle.Fill, VeldridBackend = GraphicsBackend.Vulkan };
			SuspendLayout();
			ClientSize = new(800, 450);
			Text = "DemoWindow";
			Load += (_, _) =>
			{
				new Thread(() =>
				{
					// just redraw at ~60 Hz while the window is focused
					while (_isAlive)
					{
						if (ContainsFocus)
						{
							demo.Invalidate();
							Thread.Sleep(17);
						}
						else
						{
							Thread.Sleep(100);
						}
					}
				}).Start();
			};
			Controls.Add(demo);
			ResumeLayout();
		}

		protected override void Dispose(bool disposing)
		{
			_isAlive = false;
			base.Dispose(disposing);
		}
	}
}
