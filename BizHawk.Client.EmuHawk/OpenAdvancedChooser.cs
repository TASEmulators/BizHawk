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
using BizHawk.Emulation.Cores.Libretro;
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

		RetroDescription CurrentDescription;
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
				//OLD COMMENTS:
				////a stub corecomm. to reinforce that this won't touch the frontend at all!
				////LibRetroEmulator should be able to survive having this stub corecomm
				//NEW COMMENTS:
				//nope, we need to navigate to the dll path. this was a bad idea anyway. so many dlls get loaded, something to resolve them is needed
				var coreComm = new BizHawk.Emulation.Common.CoreComm(null, null);
				CoreFileProvider.SyncCoreCommInputSignals(coreComm);
				using (var retro = new LibretroCore(coreComm, core))
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
			catch (Exception ex)
			{
				if (!bootstrap)
				{
					MessageBox.Show("Couldn't load the selected Libretro core for analysis. It won't be available.\n\nError:\n\n" + ex.ToString());
				}
			}
		}

		private void btnLibretroLaunchGame_Click(object sender, EventArgs e)
		{
			//build a list of extensions suggested for use for this core
			StringWriter sw = new StringWriter();
			foreach(var ext in CurrentDescription.ValidExtensions.Split('|'))
				sw.Write("*.{0};",ext);
			var filter = sw.ToString();
			filter = filter.Substring(0,filter.Length-1); //remove last semicolon
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
			DialogResult =  DialogResult.OK;
			Close();
		}

		private void btnClassicLaunchGame_Click(object sender, EventArgs e)
		{
			Result = Command.ClassicLaunchGame;
			DialogResult = DialogResult.OK;
			Close();
		}

		private void btnLibretroLaunchNoGame_Click(object sender, EventArgs e)
		{
			Result = Command.RetroLaunchNoGame;
			DialogResult = DialogResult.OK;
			Close();
		}

		private void txtLibretroCore_DragEnter(object sender, DragEventArgs e)
		{
			if (e.Data.GetDataPresent(DataFormats.FileDrop))
			{
				var filePaths = (string[])e.Data.GetData(DataFormats.FileDrop);
				if (Path.GetExtension(filePaths[0]).ToUpper() == ".DLL")
				{
					e.Effect = DragDropEffects.Copy;
					return;
				}
			}

			e.Effect = DragDropEffects.None;
		}

		private void txtLibretroCore_DragDrop(object sender, DragEventArgs e)
		{
			var filePaths = (string[])e.Data.GetData(DataFormats.FileDrop);
			Global.Config.LibretroCore = filePaths[0];
			RefreshLibretroCore(false);
		}
	}
}
