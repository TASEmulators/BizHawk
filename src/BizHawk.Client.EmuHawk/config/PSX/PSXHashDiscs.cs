using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	[SpecializedTool("Hash Discs")] // puts this in Nymashock's VSystem menu (the generic one), though when opened that way it's not modal
	public partial class PSXHashDiscs : ToolFormBase
	{
		[RequiredService]
		public IRedumpDiscChecksumInfo _psx { get; set; }

		protected override string WindowTitleStatic { get; } = "PSX Disc Hasher";

		public PSXHashDiscs()
		{
			InitializeComponent();
			btnClose.Click += (_, _) => Close();
		}

		private void BtnHash_Click(object sender, EventArgs e)
		{
			txtHashes.Text = "";
			btnHash.Enabled = false;
			txtHashes.Text = _psx.CalculateDiscHashes();
			btnHash.Enabled = true;
		}
	}
}
