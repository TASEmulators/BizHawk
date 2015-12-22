using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

using BizHawk.Emulation.Common.IEmulatorExtensions;
using BizHawk.Emulation.Cores.Calculators;
using BizHawk.Emulation.Cores.Consoles.Sega.gpgx;
using BizHawk.Emulation.Cores.Nintendo.GBA;
using BizHawk.Emulation.Cores.Nintendo.NES;
using BizHawk.Emulation.Cores.Nintendo.SNES;
using BizHawk.Emulation.Cores.PCEngine;
using BizHawk.Emulation.Cores.Sega.MasterSystem;

using BizHawk.Client.Common;
using System.Reflection;

namespace BizHawk.Client.EmuHawk
{
	public partial class ToolBox : Form, IToolForm
	{
		public ToolBox()
		{
			InitializeComponent();
		}

		private void ToolBox_Load(object sender, EventArgs e)
		{
			Location = new Point(
				Owner.Location.X + Owner.Size.Width,
				Owner.Location.Y
			);
		}

		public bool AskSaveChanges() { return true;  }
		public bool UpdateBefore { get { return false; } }
		public void UpdateValues() { }

		public void FastUpdate()
		{
			// Do nothing
		}

		public void Restart()
		{
			SetTools();
			SetSize();

			ToolBoxStrip.Select();
			ToolBoxItems.First().Select();
		}

		private void SetTools()
		{
			ToolBoxStrip.Items.Clear();

			foreach (var t in Assembly.GetAssembly(GetType()).GetTypes())
			{
				if (!typeof(IToolForm).IsAssignableFrom(t))
					continue;
				if (!typeof(Form).IsAssignableFrom(t))
					continue;
				if (typeof(ToolBox).IsAssignableFrom(t))  //yo dawg i head you like toolboxes
					continue;
				if (VersionInfo.DeveloperBuild && t.GetCustomAttributes(false).OfType<ToolAttributes>().Any(a => !a.Released))
					continue;
				if (t == typeof(GBGameGenie)) // Hack, this tool is specific to a system id and a sub-system (gb and gg) we have no reasonable way to declare a dependency like that
					continue;
				if (!BizHawk.Emulation.Common.ServiceInjector.IsAvailable(Global.Emulator.ServiceProvider, t))
					continue;

				var instance = Activator.CreateInstance(t);

				var tsb = new ToolStripButton
				{
					Image = (instance as Form).Icon.ToBitmap(),
					Text = (instance as Form).Text,
					DisplayStyle = (instance as Form).ShowIcon ? ToolStripItemDisplayStyle.Image : ToolStripItemDisplayStyle.Text
				};

				tsb.Click += (o, e) =>
				{
					GlobalWin.Tools.Load(t);
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
		private IEnumerable<ToolStripItem> ToolBoxItems
		{
			get
			{
				return ToolBoxStrip.Items.Cast<ToolStripItem>();
			}
		}

		protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
		{
			if (keyData == Keys.Escape)
			{
				Close();
				return true;
			}
			else
			{
				return base.ProcessCmdKey(ref msg, keyData);
			}
		}
	}
}
