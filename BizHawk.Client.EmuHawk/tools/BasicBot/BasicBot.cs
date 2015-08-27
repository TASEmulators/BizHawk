using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

using BizHawk.Emulation.Common;
using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class BasicBot : Form , IToolFormAutoConfig
	{
		[RequiredService]
		private IEmulator Emulator { get; set; }

		[RequiredService]
		private IStatable StatableCore { get; set; }

		[RequiredService]
		private IMemoryDomains MemoryDomains { get; set; }

		[ConfigPersist]
		public BasicBotSettings Settings { get; set; }

		public class BasicBotSettings
		{

		}

		public BasicBot()
		{
			InitializeComponent();
		}

		private void BasicBot_Load(object sender, EventArgs e)
		{
			int starty = 0;
			int accumulatedy = 0;
			int lineHeight = 30;
			int marginLeft = 15;
			foreach (var button in Emulator.ControllerDefinition.BoolButtons)
			{
				var control = new BotControlsRow
				{
					ButtonName = button,
					Probability = 0.0,
					Location = new Point(marginLeft, starty + accumulatedy)
				};

				ControlProbabilityPanel.Controls.Add(control);
				accumulatedy += lineHeight;
			}
		}

		#region IToolForm Implementation

		public bool UpdateBefore { get { return true; } }

		public void UpdateValues()
		{

		}

		public void FastUpdate()
		{

		}

		public void Restart()
		{

		}

		public bool AskSaveChanges()
		{
			return true; // TODO
		}

		#endregion

		private void exitToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Close();
		}

		private Dictionary<string, double> ControlProbabilities
		{
			get
			{
				return ControlProbabilityPanel.Controls
					.OfType<BotControlsRow>()
					.ToDictionary(tkey => tkey.ButtonName, tvalue => tvalue.Probability);
			}
		}

		private void RunBtn_Click(object sender, EventArgs e)
		{
			var intialState = StatableCore.SaveStateBinary();

			bool oldCountingSetting = false;
			if (Global.MovieSession.Movie.IsRecording)
			{
				oldCountingSetting = Global.MovieSession.Movie.IsCountingRerecords;
				Global.MovieSession.Movie.IsCountingRerecords = false;
			}

			if (Global.MovieSession.Movie.IsRecording)
			{
				Global.MovieSession.Movie.IsCountingRerecords = oldCountingSetting;
			}
		}
	}
}
