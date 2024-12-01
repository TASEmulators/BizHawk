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
		private static readonly Lazy<Image> IconMissingIcon = new(() => Properties.Resources.Logo.ToBitmap());

		public static Icon ToolIcon
			=> Properties.Resources.ToolBoxIcon;

		private static readonly Lazy<IReadOnlyCollection<Type>> ToolTypes = new(() => EmuHawk.ReflectionCache.Types
			.Where(static t => typeof(IToolForm).IsAssignableFrom(t) && typeof(Form).IsAssignableFrom(t)
#if DEBUG // these tools are simply not compiled in Release config
				&& t.Namespace is not "BizHawk.Client.EmuHawk.ForDebugging"
#endif
				&& (VersionInfo.DeveloperBuild
					|| !t.GetCustomAttributes(false).OfType<ToolAttribute>().Any(static a => !a.Released)))
			.Except(new[] { typeof(ToolBox), typeof(ToolFormBase) }).ToList());

		[RequiredService]
		private IEmulator Emulator { get; set; }

		protected override string WindowTitleStatic => string.Empty;

		public ToolBox()
		{
			InitializeComponent();
			Icon = ToolIcon;
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
			foreach (var t in ToolTypes.Value)
			{
				if (!ServiceInjector.IsAvailable(Emulator.ServiceProvider, t)) continue;
				var (icon, name) = Tools.GetIconAndNameFor(t);
				ToolStripButton tsb = new() {
					DisplayStyle = ToolStripItemDisplayStyle.Image,
					Image = icon ?? IconMissingIcon.Value,
					Text = name,
				};
				tsb.Click += (_, _) => Tools.Load(t);
				ToolBoxStrip.Items.Add(tsb);
			}
			foreach (var tsi in ((MainForm) MainForm).ExtToolManager.ToolStripItems) //TODO nicer encapsulation
			{
				if (!tsi.Enabled) continue;
				ToolStripButton tsb = new() {
					DisplayStyle = ToolStripItemDisplayStyle.Image,
					Image = tsi.Image ?? IconMissingIcon.Value,
					Text = tsi.Text,
				};
				var info = (ExternalToolManager.MenuItemInfo) tsi.Tag;
				tsb.Click += (_, _) => info.TryLoad();
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
