using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Reflection;

namespace BizHawk.MultiClient
{
	public partial class PathConfig : Form
	{
		//TODO:
		//Make all base path text boxes not allow  %recent%
		//All path text boxes should do some kind of error checking
		//config path under base, config will default to %exe%
		//Think of other modifiers (perhaps all environment paths?)
		//If enough modifiers, path boxes can do a pull down of suggestions when user types %

		//******************
		//Modifiers
		//%exe% - path of EXE
		//%recent% - most recent directory (windows environment path)
		//******************

		//******************
		//Relative path logic
		// . will always be relative to to a platform base
		//    unless it is a tools path or a platform base in which case it is relative to base
		//    base is always relative to exe
		//******************

		public PathConfig()
		{
			InitializeComponent();
		}

		private void SaveButton_Click(object sender, EventArgs e)
		{
			SaveSettings();
		}

		private void PathConfig_Load(object sender, EventArgs e)
		{
			RecentForROMs.Checked = Global.Config.UseRecentForROMs;
			BasePathBox.Text = Global.Config.BasePath;

			NESBaseBox.Text = Global.Config.BaseNES;
			NESROMsBox.Text = Global.Config.PathNESROMs;
			NESSavestatesBox.Text = Global.Config.PathNESSavestates;
			NESSaveRAMBox.Text = Global.Config.PathNESSaveRAM;
			NESScreenshotsBox.Text = Global.Config.PathNESScreenshots;
			NESCheatsBox.Text = Global.Config.PathNESCheats;
			NESPaletteBox.Text = Global.Config.PathNESPalette;

			Sega8BaseBox.Text = Global.Config.BaseSMS;
			Sega8ROMsBox.Text = Global.Config.PathSMSROMs;
			Sega8SavestatesBox.Text = Global.Config.PathSMSSavestates;
			Sega8SaveRAMBox.Text = Global.Config.PathSMSSaveRAM;
			Sega8ScreenshotsBox.Text = Global.Config.PathSMSScreenshots;
			Sega8CheatsBox.Text = Global.Config.PathSMSCheats;

			GGBaseBox.Text = Global.Config.BaseGG;
			GGROMBox.Text = Global.Config.PathGGROMs;
			GGSavestatesBox.Text = Global.Config.PathGGSavestates;
			GGSaveRAMBox.Text = Global.Config.PathGGSaveRAM;
			GGScreenshotsBox.Text = Global.Config.PathGGScreenshots;
			GGCheatsBox.Text = Global.Config.PathGGCheats;

			SGBaseBox.Text = Global.Config.BaseSG;
			SGROMsBox.Text = Global.Config.PathSGROMs;
			SGSavestatesBox.Text = Global.Config.PathSGSavestates;
			SGSaveRAMBox.Text = Global.Config.PathSGSaveRAM;
			SGScreenshotsBox.Text = Global.Config.PathSGScreenshots;
			SGCheatsBox.Text = Global.Config.PathSGCheats;

			PCEBaseBox.Text = Global.Config.BasePCE;
			PCEROMsBox.Text = Global.Config.PathPCEROMs;
			PCESavestatesBox.Text = Global.Config.PathPCESavestates;
			PCESaveRAMBox.Text = Global.Config.PathPCESaveRAM;
			PCEScreenshotsBox.Text = Global.Config.PathPCEScreenshots;
			PCECheatsBox.Text = Global.Config.PathPCECheats;

			GenesisBaseBox.Text = Global.Config.BaseGenesis;
			GenesisROMsBox.Text = Global.Config.PathGenesisROMs;
			GenesisSavestatesBox.Text = Global.Config.PathGenesisScreenshots;
			GenesisSaveRAMBox.Text = Global.Config.PathGenesisSaveRAM;
			GenesisScreenshotsBox.Text = Global.Config.PathGenesisScreenshots;
			GenesisCheatsBox.Text = Global.Config.PathGenesisCheats;

			GBBaseBox.Text = Global.Config.BaseGameboy;
			GBROMsBox.Text = Global.Config.PathGBROMs;
			GBSavestatesBox.Text = Global.Config.PathGBSavestates;
			GBSaveRAMBox.Text = Global.Config.PathGBSaveRAM;
			GBScreenshotsBox.Text = Global.Config.PathGBScreenshots;
			GBCheatsBox.Text = Global.Config.PathGBCheats;

			TI83BaseBox.Text = Global.Config.BaseTI83;
			TI83ROMsBox.Text = Global.Config.PathTI83ROMs;
			TI83SavestatesBox.Text = Global.Config.PathTI83Savestates;
			TI83SaveRAMBox.Text = Global.Config.PathTI83SaveRAM;
			TI83ScreenshotsBox.Text = Global.Config.PathTI83Screenshots;
			TI83CheatsBox.Text = Global.Config.PathTI83Cheats;

			MoviesBox.Text = Global.Config.MoviesPath;
			LuaBox.Text = Global.Config.LuaPath;
			WatchBox.Text = Global.Config.WatchPath;
			AVIBox.Text = Global.Config.AVIPath;

			PCEBiosBox.Text = Global.Config.PathPCEBios;
		}

		private void SaveSettings()
		{
			Global.Config.UseRecentForROMs = RecentForROMs.Checked;
			Global.Config.BasePath = BasePathBox.Text;

			Global.Config.BaseNES = NESBaseBox.Text;
			Global.Config.PathNESROMs = NESROMsBox.Text;
			Global.Config.PathNESSavestates = NESSavestatesBox.Text;
			Global.Config.PathNESSaveRAM = NESSaveRAMBox.Text;
			Global.Config.PathNESScreenshots = NESScreenshotsBox.Text;
			Global.Config.PathNESCheats = NESCheatsBox.Text;
			Global.Config.PathNESPalette = NESPaletteBox.Text;

			Global.Config.BaseSMS = Sega8BaseBox.Text;
			Global.Config.PathSMSROMs = Sega8ROMsBox.Text;
			Global.Config.PathSMSSavestates = Sega8SavestatesBox.Text;
			Global.Config.PathSMSSaveRAM = Sega8SaveRAMBox.Text;
			Global.Config.PathSMSScreenshots = Sega8ScreenshotsBox.Text;
			Global.Config.PathSMSCheats = Sega8CheatsBox.Text;

			Global.Config.BaseGG = GGBaseBox.Text;
			Global.Config.PathGGROMs = GGROMBox.Text;
			Global.Config.PathGGSavestates = GGSavestatesBox.Text;
			Global.Config.PathGGSaveRAM = GGSaveRAMBox.Text;
			Global.Config.PathGGScreenshots = GGScreenshotsBox.Text;
			Global.Config.PathGGCheats = GGCheatsBox.Text;

			Global.Config.BaseSG = SGBaseBox.Text;
			Global.Config.PathSGROMs = SGROMsBox.Text;
			Global.Config.PathSGSavestates = SGSavestatesBox.Text;
			Global.Config.PathSGSaveRAM = SGSaveRAMBox.Text;
			Global.Config.PathSGScreenshots = SGScreenshotsBox.Text;
			Global.Config.PathSGCheats = SGCheatsBox.Text;

			Global.Config.BasePCE = PCEBaseBox.Text;
			Global.Config.PathPCEROMs = PCEROMsBox.Text;
			Global.Config.PathPCESavestates = PCESavestatesBox.Text;
			Global.Config.PathPCESaveRAM = PCESaveRAMBox.Text;
			Global.Config.PathPCEScreenshots = PCEScreenshotsBox.Text;
			Global.Config.PathPCECheats = PCECheatsBox.Text;

			Global.Config.BaseGenesis = GenesisBaseBox.Text;
			Global.Config.PathGenesisROMs = GenesisROMsBox.Text;
			Global.Config.PathGenesisScreenshots = GenesisSavestatesBox.Text;
			Global.Config.PathGenesisSaveRAM = GenesisSaveRAMBox.Text;
			Global.Config.PathGenesisScreenshots = GenesisScreenshotsBox.Text;
			Global.Config.PathGenesisCheats = GenesisCheatsBox.Text;

			Global.Config.BaseGameboy = GBBaseBox.Text;
			Global.Config.PathGBROMs = GBROMsBox.Text;
			Global.Config.PathGBSavestates = GBSavestatesBox.Text;
			Global.Config.PathGBSaveRAM = GBSaveRAMBox.Text;
			Global.Config.PathGBScreenshots = GBScreenshotsBox.Text;
			Global.Config.PathGBCheats = GBCheatsBox.Text;

			Global.Config.BaseTI83 = TI83BaseBox.Text;
			Global.Config.PathTI83ROMs = TI83ROMsBox.Text;
			Global.Config.PathTI83Savestates = TI83SavestatesBox.Text;
			Global.Config.PathTI83SaveRAM = TI83SaveRAMBox.Text;
			Global.Config.PathTI83Screenshots = TI83ScreenshotsBox.Text;
			Global.Config.PathTI83Cheats = TI83CheatsBox.Text;

			Global.Config.MoviesPath = MoviesBox.Text;
			Global.Config.LuaPath = LuaBox.Text;
			Global.Config.WatchPath = WatchBox.Text;
			Global.Config.AVIPath = AVIBox.Text;

			Global.Config.PathPCEBios = PCEBiosBox.Text;

			BasePathBox.Focus();
		}

		private void Cancel_Click(object sender, EventArgs e)
		{
			this.Close();
		}

		private void OK_Click(object sender, EventArgs e)
		{
			SaveSettings();
			this.Close();
		}

		private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
		{
			//TODO: make base text box Controls[0] so this will focus on it
			//tabControl1.TabPages[tabControl1.SelectedIndex].Controls[0].Focus(); 
		}

		private void RecentForROMs_CheckedChanged(object sender, EventArgs e)
		{
			Global.Config.UseRecentForROMs = RecentForROMs.Checked;
			if (RecentForROMs.Checked)
			{
				NESROMsBox.Enabled = false;
				BrowseNESROMs.Enabled = false;
				Sega8ROMsBox.Enabled = false;
				Sega8BrowseROMs.Enabled = false;
				GGROMBox.Enabled = false;
				GGROMsDescription.Enabled = false;
				SGROMsBox.Enabled = false;
				SGROMsDescription.Enabled = false;
				GenesisROMsBox.Enabled = false;
				GenesisBrowseROMs.Enabled = false;
				PCEROMsBox.Enabled = false;
				PCEBrowseROMs.Enabled = false;
				GBROMsBox.Enabled = false;
				GBBrowseROMs.Enabled = false;
				TI83ROMsBox.Enabled = false;
				TI83BrowseROMs.Enabled = false;

				NESROMsDescription.Enabled = false;
				Sega8ROMsDescription.Enabled = false;
				GenesisROMsDescription.Enabled = false;
				PCEROMsDescription.Enabled = false;
				GBROMsDescription.Enabled = false;
				TI83ROMsDescription.Enabled = false;
			}
			else
			{
				NESROMsBox.Enabled = true;
				BrowseNESROMs.Enabled = true;
				Sega8ROMsBox.Enabled = true;
				Sega8BrowseROMs.Enabled = true;
				GGROMBox.Enabled = true;
				GGROMsDescription.Enabled = true;
				SGROMsBox.Enabled = true;
				SGROMsDescription.Enabled = true;
				GenesisROMsBox.Enabled = true;
				GenesisBrowseROMs.Enabled = true;
				PCEROMsBox.Enabled = true;
				PCEBrowseROMs.Enabled = true;
				GBROMsBox.Enabled = true;
				GBBrowseROMs.Enabled = true;
				TI83ROMsBox.Enabled = true;
				TI83BrowseROMs.Enabled = true;

				NESROMsDescription.Enabled = true;
				Sega8ROMsDescription.Enabled = true;
				GenesisROMsDescription.Enabled = true;
				PCEROMsDescription.Enabled = true;
				GBROMsDescription.Enabled = true;
				TI83ROMsDescription.Enabled = true;
			}
		}

		private void BrowseFolder(TextBox box, string Name)
		{
			FolderBrowserDialog f = new FolderBrowserDialog();
			f.Description = "Set the directory for " + Name;
			f.SelectedPath = PathManager.MakeAbsolutePath(box.Text, "");
			DialogResult result = f.ShowDialog();
			if (result == DialogResult.OK)
				box.Text = f.SelectedPath;
		}

		private void BrowseFolder(TextBox box, string Name, string System)
		{
			FolderBrowserEx f = new FolderBrowserEx();
			f.Description = "Set the directory for " + Name;
			f.SelectedPath = PathManager.MakeAbsolutePath(box.Text, System);
			DialogResult result = f.ShowDialog();
			if (result == DialogResult.OK)
				box.Text = f.SelectedPath;
		}

		private void BrowseWatch_Click(object sender, EventArgs e)
		{
			BrowseFolder(WatchBox, WatchDescription.Text);
		}

		private void BrowseBase_Click(object sender, EventArgs e)
		{
			BrowseFolder(BasePathBox, BaseDescription.Text);
		}

		private void BrowseAVI_Click(object sender, EventArgs e)
		{
			BrowseFolder(AVIBox, AVIDescription.Text);
		}

		private void BrowseLua_Click(object sender, EventArgs e)
		{
			BrowseFolder(LuaBox, LuaDescription.Text);
		}

		private void BrowseMovies_Click(object sender, EventArgs e)
		{
			BrowseFolder(MoviesBox, MoviesDescription.Text);
		}

		private void BrowseNESBase_Click(object sender, EventArgs e)
		{
			BrowseFolder(NESBaseBox, NESBaseDescription.Text);
		}

		private void BrowseNESROMs_Click(object sender, EventArgs e)
		{
			BrowseFolder(NESROMsBox, NESROMsDescription.Text, "NES");
		}

		private void BrowseNESSavestates_Click(object sender, EventArgs e)
		{
			BrowseFolder(NESSavestatesBox, NESSavestatesDescription.Text, "NES");
		}

		private void BrowseNESSaveRAM_Click(object sender, EventArgs e)
		{
			BrowseFolder(NESSaveRAMBox, NESSaveRAMDescription.Text, "NES");
		}

		private void BrowseNESScreenshots_Click(object sender, EventArgs e)
		{
			BrowseFolder(NESScreenshotsBox, NESScreenshotsDescription.Text, "NES");
		}

		private void NESBrowseCheats_Click(object sender, EventArgs e)
		{
			BrowseFolder(NESCheatsBox, NESCheatsDescription.Text, "NES");
		}

		private void Sega8BrowseBase_Click(object sender, EventArgs e)
		{
			BrowseFolder(Sega8BaseBox, Sega8BaseDescription.Text, "SMS");
		}

		private void Sega8BrowseROMs_Click(object sender, EventArgs e)
		{
			BrowseFolder(Sega8ROMsBox, Sega8ROMsDescription.Text, "SMS");
		}

		private void Sega8BrowseSavestates_Click(object sender, EventArgs e)
		{
			BrowseFolder(Sega8SavestatesBox, Sega8SavestatesDescription.Text, "SMS");
		}

		private void Sega8BrowseSaveRAM_Click(object sender, EventArgs e)
		{
			BrowseFolder(Sega8SaveRAMBox, Sega8SaveRAMDescription.Text, "SMS");
		}

		private void Sega8BrowseScreenshots_Click(object sender, EventArgs e)
		{
			BrowseFolder(Sega8ScreenshotsBox, Sega8ScreenshotsDescription.Text, "SMS");
		}

		private void Sega8BrowseCheats_Click(object sender, EventArgs e)
		{
			BrowseFolder(Sega8CheatsBox, Sega8CheatsDescription.Text, "SMS");
		}

		private void GenesisBrowseBase_Click(object sender, EventArgs e)
		{
			BrowseFolder(GenesisBaseBox, GenesisBaseDescription.Text);
		}

		private void GenesisBrowseROMs_Click(object sender, EventArgs e)
		{
			BrowseFolder(GenesisROMsBox, GenesisROMsDescription.Text, "GEN");
		}

		private void GenesisBrowseSavestates_Click(object sender, EventArgs e)
		{
			BrowseFolder(GenesisSavestatesBox, GenesisSavestatesDescription.Text, "GEN");
		}

		private void GenesisBrowseSaveRAM_Click(object sender, EventArgs e)
		{
			BrowseFolder(GenesisSaveRAMBox, GenesisSaveRAMDescription.Text, "GEN");
		}

		private void GenesisBrowseScreenshots_Click(object sender, EventArgs e)
		{
			BrowseFolder(GenesisScreenshotsBox, GenesisScreenshotsDescription.Text, "GEN");
		}

		private void GenesisBrowseCheats_Click(object sender, EventArgs e)
		{
			BrowseFolder(GenesisCheatsBox, GenesisCheatsDescription.Text, "GEN");
		}

		private void PCEBrowseBase_Click(object sender, EventArgs e)
		{
			BrowseFolder(PCEBaseBox, PCEBaseDescription.Text);
		}

		private void PCEBrowseROMs_Click(object sender, EventArgs e)
		{
			BrowseFolder(PCEROMsBox, PCEROMsDescription.Text, "PCE");
		}

		private void PCEBrowseSavestates_Click(object sender, EventArgs e)
		{
			BrowseFolder(PCESavestatesBox, PCESavestatesDescription.Text, "PCE");
		}

		private void PCEBrowseSaveRAM_Click(object sender, EventArgs e)
		{
			BrowseFolder(PCESaveRAMBox, PCESaveRAMDescription.Text, "PCE");
		}

		private void PCEBrowseScreenshots_Click(object sender, EventArgs e)
		{
			BrowseFolder(PCEScreenshotsBox, PCEScreenshotsDescription.Text, "PCE");
		}

		private void PCEBrowseCheats_Click(object sender, EventArgs e)
		{
			BrowseFolder(PCECheatsBox, PCECheatsDescription.Text, "PCE");
		}

		private void GBBrowseBase_Click(object sender, EventArgs e)
		{
			BrowseFolder(GBBaseBox, GBBaseDescription.Text);
		}

		private void GBBrowseROMs_Click(object sender, EventArgs e)
		{
			BrowseFolder(GBROMsBox, GBROMsDescription.Text, "GB");
		}

		private void GBBrowseSavestates_Click(object sender, EventArgs e)
		{
			BrowseFolder(GBSavestatesBox, GBSavestatesDescription.Text, "GB");
		}

		private void GBBrowseSaveRAM_Click(object sender, EventArgs e)
		{
			BrowseFolder(GBSaveRAMBox, GBSaveRAMDescription.Text, "GB");
		}

		private void GBBrowseScreenshots_Click(object sender, EventArgs e)
		{
			BrowseFolder(GBScreenshotsBox, GBScreenshotsDescription.Text, "GB");
		}

		private void GBBrowseCheats_Click(object sender, EventArgs e)
		{
			BrowseFolder(GBCheatsBox, GBCheatsDescription.Text, "GB");
		}

		private void TI83BrowseBase_Click(object sender, EventArgs e)
		{
			BrowseFolder(TI83BaseBox, TI83BaseDescription.Text);
		}

		private void TI83BrowseROMs_Click(object sender, EventArgs e)
		{
			BrowseFolder(TI83ROMsBox, TI83ROMsDescription.Text, "TI83");
		}

		private void TI83BrowseSavestates_Click(object sender, EventArgs e)
		{
			BrowseFolder(TI83SavestatesBox, TI83SavestatesDescription.Text, "TI83");
		}

		private void TI83BrowseSaveRAM_Click(object sender, EventArgs e)
		{
			BrowseFolder(TI83SaveRAMBox, TI83SaveRAMDescription.Text, "TI83");
		}

		private void TI83BrowseScreenshots_Click(object sender, EventArgs e)
		{
			BrowseFolder(TI83ScreenshotsBox, TI83ScreenshotsDescription.Text, "TI83");
		}

		private void TI83BrowseBox_Click(object sender, EventArgs e)
		{
			BrowseFolder(TI83CheatsBox, TI83CheatsDescription.Text, "TI83");
		}

		private void GGBrowseBase_Click(object sender, EventArgs e)
		{
			BrowseFolder(GGBaseBox, GGBaseDescription.Text);
		}

		private void GGBrowseROMs_Click(object sender, EventArgs e)
		{
			BrowseFolder(GGROMBox, GGROMsDescription.Text, "GG");
		}

		private void GGBrowseSavestates_Click(object sender, EventArgs e)
		{
			BrowseFolder(GGSavestatesBox, GGSavestatesDescription.Text, "GG");
		}

		private void GGBrowseSaveRAM_Click(object sender, EventArgs e)
		{
			BrowseFolder(GGSaveRAMBox, GGSaveRAMDescription.Text, "GG");
		}

		private void GGBrowseScreenshots_Click(object sender, EventArgs e)
		{
			BrowseFolder(GGScreenshotsBox, GGScreenshotsDescription.Text, "GG");
		}

		private void GGBrowseCheats_Click(object sender, EventArgs e)
		{
			BrowseFolder(GGCheatsBox, GGCheatsDescription.Text, "GG");
		}

		private void SGBrowseBase_Click(object sender, EventArgs e)
		{
			BrowseFolder(SGBaseBox, SGBaseDescription.Text);
		}

		private void SGROMsBrowse_Click(object sender, EventArgs e)
		{
			BrowseFolder(SGROMsBox, SGROMsDescription.Text, "SG");
		}

		private void SGBrowseSavestates_Click(object sender, EventArgs e)
		{
			BrowseFolder(SGSavestatesBox, SGSavestatesDescription.Text, "SG");
		}

		private void SGBrowseSaveRAM_Click(object sender, EventArgs e)
		{
			BrowseFolder(SGSaveRAMBox, SGSaveRAMDescription.Text, "SG");
		}

		private void SGBrowseScreenshots_Click(object sender, EventArgs e)
		{
			BrowseFolder(SGScreenshotsBox, SGScreenshotsDescription.Text, "SG");
		}

		private void SGBrowseCheats_Click(object sender, EventArgs e)
		{
			BrowseFolder(SGCheatsBox, SGCheatsDescription.Text, "SG");
		}

		private void NESBrowsePalette_Click(object sender, EventArgs e)
		{
			BrowseFolder(NESPaletteBox, NESPaletteDescription.Text, "NES");
		}

		private void PCEBrowseBios_Click(object sender, EventArgs e)
		{
			OpenFileDialog ofd = new OpenFileDialog();
			ofd.InitialDirectory = Path.GetDirectoryName(Global.Config.PathPCEBios);
			ofd.Filter = "PCE CD BIOS (*.pce)|*.pce|All Files|*.*";
			ofd.FileName = Path.GetFileName(Global.Config.PathPCEBios);

			var result = ofd.ShowDialog();
			if (result != DialogResult.OK)
				return;

			if (File.Exists(ofd.FileName) == false)
				return;

			PCEBiosBox.Text = ofd.FileName;
		}
	}
}
