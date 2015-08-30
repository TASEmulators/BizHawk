using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
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

		private bool _isBotting = false;

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
			if (_isBotting)
			{
				if (Global.Emulator.Frame >= _targetFrame)
				{
					StatableCore.LoadStateBinary(new BinaryReader(new MemoryStream(_initialState.ToArray())));
				}

				PressButtons();
			}
        }

		public void FastUpdate()
		{
			if (_isBotting)
			{

			}
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

		private int _targetFrame = 0;
		private byte[] _initialState = null;

		private void RunBtn_Click(object sender, EventArgs e)
		{
			_isBotting = true;
			ControlsBox.Enabled = false;
            RunBtn.Enabled = false;
			StopBtn.Enabled = true;
			

			bool oldCountingSetting = false;
			if (Global.MovieSession.Movie.IsRecording)
			{
				oldCountingSetting = Global.MovieSession.Movie.IsCountingRerecords;
				Global.MovieSession.Movie.IsCountingRerecords = false;
			}

			_initialState = StatableCore.SaveStateBinary(); ;
			_targetFrame = Global.Emulator.Frame + (int)FrameLengthNumeric.Value; 

			if (GlobalWin.MainForm.EmulatorPaused)
			{
				GlobalWin.MainForm.UnpauseEmulator();
				// TODO: speed!
			}

			if (Global.MovieSession.Movie.IsRecording)
			{
				Global.MovieSession.Movie.IsCountingRerecords = oldCountingSetting;
			}
		}

		private void StopBtn_Click(object sender, EventArgs e)
		{
			RunBtn.Enabled = true;
			StopBtn.Enabled = false;
			_isBotting = false;
			_targetFrame = 0;
			_initialState = null;
            ControlsBox.Enabled = true;
			_targetFrame = 0;
        }

		private void PressButtons()
		{
			var rand = new Random((int)DateTime.Now.Ticks);
			foreach (var button in Emulator.ControllerDefinition.BoolButtons)
			{
				double probability = ControlProbabilities[button];
				bool pressed = !(rand.Next(100) < probability);
				Global.ClickyVirtualPadController.SetBool(button, pressed);
			}
		}
	}
}
