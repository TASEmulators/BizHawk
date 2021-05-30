using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class ToolBox : ToolFormBase
	{
		[RequiredService]
		private IEmulator Emulator { get; set; }

		protected override string WindowTitleStatic => string.Empty;

		public ToolBox()
		{
			InitializeComponent();
			Icon = Properties.Resources.ToolBoxIcon;
		}

		private void ToolBox_Load(object sender, EventArgs e)
		{
			if (OSTailoredCode.IsUnixHost)
			{
				Close();
				return;
			}
			Location = new Point(
				Owner.Location.X + Owner.Size.Width,
				Owner.Location.Y
			);
		}

		public override void Restart()
		{
			if (OSTailoredCode.IsUnixHost) return;
			SetTools();
			SetSize();

			ToolBoxStrip.Select();
			ToolBoxItems.First().Select();
		}

		private void SetTools()
		{
			ToolBoxStrip.Items.Clear();

			var tools = EmuHawk.ReflectionCache.Types
				.Where(t => typeof(IToolForm).IsAssignableFrom(t))
				.Where(t => typeof(Form).IsAssignableFrom(t))
				.Where(t => !typeof(ToolBox).IsAssignableFrom(t))
				.Where(t => ServiceInjector.IsAvailable(Emulator.ServiceProvider, t))
				.Where(t => VersionInfo.DeveloperBuild || !t.GetCustomAttributes(false).OfType<ToolAttribute>().Any(a => !a.Released));

			foreach (var t in tools)
			{
				var wasLoaded = Tools.Has(t);
				var instance = (Form) Tools.Load(t, focus: false);
				var tsb = new ToolStripButton
				{
					Image = instance.Icon.ToBitmap(),
					Text = instance.Text,
					DisplayStyle = instance.ShowIcon ? ToolStripItemDisplayStyle.Image : ToolStripItemDisplayStyle.Text
				};
				if (!wasLoaded) instance.Dispose();
				tsb.Click += (o, e) =>
				{
					if (wasLoaded) instance.Focus(); // instance refers to already opened tool, focus it
					else Tools.Load(t); // instance was new and has been disposed by now
					Close();
				};
				ToolBoxStrip.Items.Add(tsb);
			}
		}

		private void SetSize()
		{
			var rows = (int)Math.Ceiling(ToolBoxItems.Count() / 4.0);
			Height = 30 + (rows * 30);
		}

		// Provide LINQ capabilities to an outdated form collection
		private IEnumerable<ToolStripItem> ToolBoxItems => ToolBoxStrip.Items.Cast<ToolStripItem>();

		protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
		{
			if (keyData == Keys.Escape)
			{
				Close();
				return true;
			}

			return base.ProcessCmdKey(ref msg, keyData);
		}
	}
}
