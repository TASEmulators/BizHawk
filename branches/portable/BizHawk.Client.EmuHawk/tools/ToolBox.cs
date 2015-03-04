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
			var availableTools = Assembly
				.GetAssembly(typeof(IToolForm))
				.GetTypes()
				.Where(t => typeof(IToolForm).IsAssignableFrom(t))
				.Where(t => typeof(Form).IsAssignableFrom(t))
				.Where(t => !(typeof(ToolBox).IsAssignableFrom(t)))
				.Where(t => VersionInfo.DeveloperBuild ? true : !(t.GetCustomAttributes(false)
					.OfType<ToolAttributes>().Any(a => !a.Released)))
				.Where(t => !(t == typeof(GBGameGenie))) // Hack, this tool is specific to a system id and a sub-system (gb and gg) we have no reasonable way to declare a dependency like that
				.Where(t => BizHawk.Emulation.Common.ServiceInjector.IsAvailable(Global.Emulator.ServiceProvider, t))
				.Select(t => Activator.CreateInstance(t))
				.Select(instance => new
				{
					Type = instance.GetType(),
					Instance = instance,
					Icon = (instance as Form).Icon.ToBitmap(),
					Text = (instance as Form).Text,
					ShowIcon = (instance as Form).ShowIcon
				})
				.ToList();

			foreach (var tool in availableTools)
			{
				var t = new ToolStripButton
				{
					Image = tool.Icon,
					Text = tool.Text,
					DisplayStyle = tool.ShowIcon ? ToolStripItemDisplayStyle.Image : ToolStripItemDisplayStyle.Text
				};

				t.Click += (o, e) =>
				{
					GlobalWin.Tools.Load(tool.Type);
					Close();
				};

				ToolBoxStrip.Items.Add(t);
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
