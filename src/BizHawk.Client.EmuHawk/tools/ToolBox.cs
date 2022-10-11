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
	[GenEmuServiceProp(typeof(IEmulator), "Emulator")]
	public partial class ToolBox : ToolFormBase
	{
		protected override string WindowTitleStatic => string.Empty;

		public ToolBox()
		{
			InitializeComponent();
			Icon = Properties.Resources.ToolBoxIcon;
		}

		private void ToolBox_Load(object sender, EventArgs e)
		{
			Location = new Point(
				Owner.Location.X + Owner.Size.Width,
				Owner.Location.Y
			);
		}

		public override void Restart()
		{
			SetTools();
			SetSize();

			ToolBoxStrip.Select();
			ToolBoxItems.First().Select();
		}

		private void SetTools()
		{
			ToolBoxStrip.Items.Clear();

			var tools = EmuHawk.ReflectionCache.Types
				.Where(t => typeof(IToolForm).IsAssignableFrom(t) && typeof(Form).IsAssignableFrom(t)
					&& !typeof(ToolBox).IsAssignableFrom(t)
					&& ServiceInjector.IsAvailable(Emulator.ServiceProvider, t)
					&& (VersionInfo.DeveloperBuild || !t.GetCustomAttributes(false).OfType<ToolAttribute>().Any(static a => !a.Released)));

			/*
			for (int i = 0; i < tools.Count(); i++)
			{
				Console.WriteLine(tools.ElementAt(i).FullName);
			}
			*/

			foreach (var t in tools)
			{
				if (t.FullName != "BizHawk.Client.EmuHawk.ToolFormBase")
				{
					//var instance = Activator.CreateInstance(t);

					var image_t = Properties.Resources.Logo;
					var text_t = "";

					if (t.FullName == "BizHawk.Client.EmuHawk.CoreFeatureAnalysis") { image_t = Properties.Resources.Logo; }
					if (t.FullName == "BizHawk.Client.EmuHawk.LogWindow")			{ image_t = Properties.Resources.CommandWindow; }
					if (t.FullName == "BizHawk.Client.EmuHawk.LuaConsole")			{ image_t = Properties.Resources.TextDocIcon; }
					if (t.FullName == "BizHawk.Client.EmuHawk.MacroInputTool")		{ image_t = Properties.Resources.TAStudioIcon; }
					if (t.FullName == "BizHawk.Client.EmuHawk.MultiDiskBundler")	{ image_t = Properties.Resources.DualIcon; }
					if (t.FullName == "BizHawk.Client.EmuHawk.VirtualpadTool")		{ image_t = Properties.Resources.GameControllerIcon; }
					if (t.FullName == "BizHawk.Client.EmuHawk.BasicBot")			{ image_t = Properties.Resources.BasicBot; }
					if (t.FullName == "BizHawk.Client.EmuHawk.CDL")					{ image_t = Properties.Resources.CdLoggerIcon; }
					if (t.FullName == "BizHawk.Client.EmuHawk.Cheats")				{ image_t = Properties.Resources.BugIcon; }
					if (t.FullName == "BizHawk.Client.EmuHawk.GenericDebugger")		{ image_t = Properties.Resources.BugIcon; }
					if (t.FullName == "BizHawk.Client.EmuHawk.GameShark")			{ image_t = Properties.Resources.SharkIcon; }
					if (t.FullName == "BizHawk.Client.EmuHawk.GBPrinterView")		{ image_t = Properties.Resources.GambatteIcon; }
					if (t.FullName == "BizHawk.Client.EmuHawk.GbGpuView")			{ image_t = Properties.Resources.GambatteIcon; }
					if (t.FullName == "BizHawk.Client.EmuHawk.HexEditor")			{ image_t = Properties.Resources.FreezeIcon; }
					if (t.FullName == "BizHawk.Client.EmuHawk.TAStudio")			{ image_t = Properties.Resources.TAStudioIcon; }
					if (t.FullName == "BizHawk.Client.EmuHawk.TraceLogger")			{ image_t = Properties.Resources.PencilIcon; }
					if (t.FullName == "BizHawk.Client.EmuHawk.RamSearch")			{ image_t = Properties.Resources.SearchIcon; }
					if (t.FullName == "BizHawk.Client.EmuHawk.RamWatch")			{ image_t = Properties.Resources.WatchIcon; }
					if (t.FullName == "BizHawk.Client.EmuHawk.NESSoundConfig")		{ image_t = Properties.Resources.NesControllerIcon; }
					if (t.FullName == "BizHawk.Client.EmuHawk.NESMusicRipper")		{ image_t = Properties.Resources.NesControllerIcon; }
					if (t.FullName == "BizHawk.Client.EmuHawk.NesPPU")				{ image_t = Properties.Resources.MonitorIcon; }
					if (t.FullName == "BizHawk.Client.EmuHawk.NESNameTableViewer")	{ image_t = Properties.Resources.MonitorIcon; }
					if (t.FullName == "BizHawk.Client.EmuHawk.SmsVdpViewer")		{ image_t = Properties.Resources.SmsIcon; }

					var tsb = new ToolStripButton
					{
						Image = image_t.ToBitmap(),
						Text = text_t,
						DisplayStyle = ToolStripItemDisplayStyle.Image
						//Image = ((Form)instance).Icon.ToBitmap(),
						//Text = ((Form)instance).Text,
						//DisplayStyle = ((Form)instance).ShowIcon ? ToolStripItemDisplayStyle.Image : ToolStripItemDisplayStyle.Text
					};

					tsb.Click += (o, e) =>
					{
						Tools.Load(t);
						//Close();
					};

					ToolBoxStrip.Items.Add(tsb);
				}
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
