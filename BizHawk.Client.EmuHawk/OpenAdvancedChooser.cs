using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using BizHawk.Emulation.Cores;
using BizHawk.Client.Common;

//these match strings from OpenAdvance. should we make them constants in there?

namespace BizHawk.Client.EmuHawk
{
	public partial class OpenAdvancedChooser : Form
	{
		MainForm mainForm;

		public enum Command
		{
			RetroLaunchNoGame, RetroLaunchGame,
			ClassicLaunchGame
		}

		public Command Result;

		public OpenAdvancedChooser(MainForm mainForm)
		{
			this.mainForm = mainForm;

			InitializeComponent();

			RefreshLibretroCore();
		}

		private void btnOK_Click(object sender, EventArgs e)
		{
			DialogResult = System.Windows.Forms.DialogResult.OK;
			Close();
		}

		private void btnCancel_Click(object sender, EventArgs e)
		{
			DialogResult = System.Windows.Forms.DialogResult.Cancel;
			Close();
		}

		private void btnSetLibretroCore_Click(object sender, EventArgs e)
		{
			mainForm.RunLibretroCoreChooser();
			RefreshLibretroCore();
		}

		void RefreshLibretroCore()
		{
			txtLibretroCore.Text = "";
			btnLibretroLaunchNoGame.Enabled = false;
			btnLibretroLaunchGame.Enabled = false;

			var core = Global.Config.LibretroCore;
			if (string.IsNullOrEmpty(core))
				return;

			txtLibretroCore.Text = core;
			btnLibretroLaunchGame.Enabled = true;

			//scan the current libretro core to see if it can be launched with NoGame
			try
			{
				using (var retro = new LibRetroEmulator(new BizHawk.Emulation.Common.CoreComm(null, null), core))
				{
					if (retro.EnvironmentInfo.SupportNoGame)
						btnLibretroLaunchNoGame.Enabled = true;
				}
			}
			catch { }
		}

		private void btnLibretroLaunchGame_Click(object sender, EventArgs e)
		{
			Result = Command.RetroLaunchGame;
			DialogResult = System.Windows.Forms.DialogResult.OK;
			Close();
		}

		private void btnClassicLaunchGame_Click(object sender, EventArgs e)
		{
			Result = Command.ClassicLaunchGame;
			DialogResult = System.Windows.Forms.DialogResult.OK;
			Close();
		}

		private void btnLibretroLaunchNoGame_Click(object sender, EventArgs e)
		{
			Result = Command.RetroLaunchNoGame;
			DialogResult = System.Windows.Forms.DialogResult.OK;
			Close();
		}
	}
}
