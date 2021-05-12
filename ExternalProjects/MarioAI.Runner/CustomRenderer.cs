using BizHawk.Bizware.BizwareGL;
using BizHawk.Client.EmuHawk;
using System.Windows.Forms;
using BizHawk.Bizware.DirectX;
using BizHawk.Bizware.OpenTK3;
using BizHawk.Client.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Nintendo.N64;
using System;

namespace MarioAI.Runner
{
	public partial class CustomRenderer : Form
	{
		private readonly IVideoProvider videoProvider;

		private Timer timer = new Timer();

		public PresentationPanel Panel { get; private set; }

		public DisplayManager DisplayManager { get; private set; }

		public N64 Emulator { get; set; }

		public CustomRenderer(Config config, IGL igl, N64 emulator, InputManager inputManager)
		{
			InitializeComponent();

			this.Panel = new PresentationPanel(config, igl, null, null, null, null);

			this.Controls.Add(this.Panel);
			this.Controls.SetChildIndex(this.Panel, 0);

			this.timer.Interval = 50;
			this.timer.Tick += Timer_Tick;

			this.Shown += CustomRenderer_Shown;

			this.DisplayManager = new DisplayManager(
				config, //woher die default values?
				emulator,
				inputManager,
				null, //todo moviesession,
				igl,
				this.Panel,
				() => false
			);
		}

		private void Timer_Tick(object sender, System.EventArgs e)
		{
			this.Render();
		}

		private void CustomRenderer_Shown(object sender, System.EventArgs e)
		{
			this.timer.Start();
		}

		private void Render()
		{
			var videoprovider = this.Emulator.AsVideoProvider();

			this.DisplayManager.RenderVideoProvider(this.Emulator.VideoProvider);

			Console.WriteLine("Render()");
		}

		private void InitializeComponent()
		{
			// 
			// AITools
			// 
			this.ClientSize = new System.Drawing.Size(303, 220);
			this.Name = "CustomRenderer";
			this.Text = "Custom Renderer";
			this.ResumeLayout(false);
			this.PerformLayout();
		}
	}
}
