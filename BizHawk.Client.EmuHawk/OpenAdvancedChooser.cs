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
		public string SuggestedExtensionFilter;

		public OpenAdvancedChooser(MainForm mainForm)
		{
			this.mainForm = mainForm;

			InitializeComponent();

			RefreshLibretroCore(true);
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
			if(mainForm.RunLibretroCoreChooser())
				RefreshLibretroCore(false);
		}

		LibRetroEmulator.RetroDescription CurrentDescription;
		void RefreshLibretroCore(bool bootstrap)
		{
			txtLibretroCore.Text = "";
			btnLibretroLaunchNoGame.Enabled = false;
			btnLibretroLaunchGame.Enabled = false;

			var core = Global.Config.LibretroCore;
			if (string.IsNullOrEmpty(core))
				return;

			txtLibretroCore.Text = core;
			CurrentDescription = null;

			//scan the current libretro core to see if it can be launched with NoGame,and other stuff
			try
			{
				//a stub corecomm. to reinforce that this won't touch the frontend at all!
				//LibRetroEmulator should be able to survive having this stub corecomm
				var coreComm = new BizHawk.Emulation.Common.CoreComm(null, null);
				using (var retro = new LibRetroEmulator(coreComm, core))
				{
					btnLibretroLaunchGame.Enabled = true;
					if (retro.Description.SupportsNoGame)
						btnLibretroLaunchNoGame.Enabled = true;

					//print descriptive information
					var descr = retro.Description;
					CurrentDescription = descr;
					Console.WriteLine("core name: {0} version {1}", descr.LibraryName, descr.LibraryVersion);
					Console.WriteLine("extensions: ", descr.ValidExtensions);
					Console.WriteLine("NeedsRomAsPath: {0}", descr.NeedsRomAsPath);
					Console.WriteLine("AcceptsArchives: {0}", descr.NeedsArchives);
					Console.WriteLine("SupportsNoGame: {0}", descr.SupportsNoGame);
					
					foreach (var v in descr.Variables.Values)
						Console.WriteLine(v);
				}
			}
			catch
			{
				if (!bootstrap)
					MessageBox.Show("Couldn't load the selected Libretro core for analysis. It won't be available.");
			}
		}

		private void btnLibretroLaunchGame_Click(object sender, EventArgs e)
		{
			//build a list of extensions suggested for use for this core
			StringWriter sw = new StringWriter();
			foreach(var ext in CurrentDescription.ValidExtensions.Split('|'))
				sw.Write("*.{0};",ext);
			var filter = sw.ToString();
			filter = filter.Substring(0,filter.Length-1);
			List<string> args = new List<string>();
			args.Add("Rom Files");
			if (!CurrentDescription.NeedsArchives)
				filter += ";%ARCH%";
			args.Add(filter);
			if (!CurrentDescription.NeedsArchives)
			{
				args.Add("Archive Files");
				args.Add("%ARCH%");
			}
			args.Add("All Files");
			args.Add("*.*");
			filter = MainForm.FormatFilter(args.ToArray());
			SuggestedExtensionFilter = filter;

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
