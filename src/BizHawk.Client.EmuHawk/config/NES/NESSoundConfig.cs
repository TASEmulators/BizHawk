using BizHawk.Emulation.Cores.Nintendo.NES;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class NESSoundConfig : ToolFormBase
	{
		[RequiredService]
		private NES NES { get; set; }

		private NES.NESSettings _oldSettings;
		private NES.NESSettings _settings;

		public override void Restart()
		{
			NESSoundConfig_Load(null, null);
		}

		protected override string WindowTitleStatic => "NES Sound Channels";

		public NESSoundConfig()
		{
			InitializeComponent();

			// get baseline maxes from a default config object
			var d = new NES.NESSettings();
			trackBar1.Minimum = d.APU_vol;
		}

		private void NESSoundConfig_Load(object sender, EventArgs e)
		{
			_oldSettings = NES.GetSettings();
			_settings = _oldSettings.Clone();

			trackBar1.Value = _settings.APU_vol;
		}

		private void Ok_Click(object sender, EventArgs e)
		{
			Close();
		}

		private void Cancel_Click(object sender, EventArgs e)
		{
			// restore previous value
			NES.PutSettings(_oldSettings);
			Close();
		}

		private void TrackBar1_ValueChanged(object sender, EventArgs e)
		{
			label6.Text = trackBar1.Value.ToString();
			_settings.APU_vol = trackBar1.Value;
			NES.PutSettings(_settings);
		}
	}
}
