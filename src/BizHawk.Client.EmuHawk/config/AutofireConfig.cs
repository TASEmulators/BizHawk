#nullable enable

using System;
using System.Windows.Forms;

using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class AutofireConfig : Form, IAutofireConfigDialogViewAdapter
	{
		private AutofireConfigDialogModel? _m;

		public bool ConsiderLag
		{
			get => cbConsiderLag.Checked;
			set => cbConsiderLag.Checked = value;
		}

		public int PatternOff
		{
			get => (int) nudPatternOff.Value;
			set => nudPatternOff.Value = value;
		}

		public int PatternOn
		{
			get => (int) nudPatternOn.Value;
			set => nudPatternOn.Value = value;
		}

		public AutofireConfig(
			Config config,
			AutofireController afController,
			AutoFireStickyXorAdapter afStickyXORAdapter)
		{
			_m = new(this, afController, afStickyXORAdapter, config);
			InitializeComponent();
			Icon = Properties.Resources.LightningIcon;
		}

		private void AutofireConfig_Load(object sender, EventArgs e)
			=> _m?.BeforeShow();

		private void btnDialogOK_Click(object sender, EventArgs e)
		{
			_m?.BeforeClose(true);
			DialogResult = DialogResult.OK;
			Close();
		}

		private void btnDialogCancel_Click(object sender, EventArgs e)
		{
			_m?.BeforeClose(false);
			DialogResult = DialogResult.Cancel;
			Close();
		}

		protected override void Dispose(bool disposing)
		{
			_m = null; // trying to give GC a hand with the circular reference -- does this help at all? --yoshi
			base.Dispose(disposing);
		}
	}
}
