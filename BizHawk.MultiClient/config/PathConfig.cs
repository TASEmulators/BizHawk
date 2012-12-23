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

		//also....... this isnt really scalable. we need some more fancy system probably which is data-driven

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

			INTVBaseBox.Text = Global.Config.BaseINTV;
			INTVRomsBox.Text = Global.Config.PathINTVROMs;
			INTVSavestatesBox.Text = Global.Config.PathINTVSavestates;
			INTVSaveRAMBox.Text = Global.Config.PathINTVSaveRAM; ;
			INTVScreenshotsBox.Text = Global.Config.PathINTVScreenshots;
			INTVCheatsBox.Text = Global.Config.PathINTVCheats;
			//INTVEROMBox.Text = Global.Config.PathINTVEROM;
			//INTVGROMBox.Text = Global.Config.PathINTVGROM;
			
			NESBaseBox.Text = Global.Config.BaseNES;
			NESROMsBox.Text = Global.Config.PathNESROMs;
			NESSavestatesBox.Text = Global.Config.PathNESSavestates;
			NESSaveRAMBox.Text = Global.Config.PathNESSaveRAM;
			NESScreenshotsBox.Text = Global.Config.PathNESScreenshots;
			NESCheatsBox.Text = Global.Config.PathNESCheats;
			NESPaletteBox.Text = Global.Config.PathNESPalette;
			//NESFDSBiosBox.Text = Global.Config.PathFDSBios;

			SNESBaseBox.Text = Global.Config.BaseSNES;
			SNESROMsBox.Text = Global.Config.PathSNESROMs;
			SNESSavestatesBox.Text = Global.Config.PathSNESSavestates;
			SNESSaveRAMBox.Text = Global.Config.PathSNESSaveRAM;
			SNESScreenshotsBox.Text = Global.Config.PathSNESScreenshots;
			SNESCheatsBox.Text = Global.Config.PathSNESCheats;
			//SNESFirmwaresBox.Text = Global.Config.PathSNESFirmwares;

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
			GBPalettesBox.Text = Global.Config.PathGBPalettes;

			GBABaseBox.Text = Global.Config.BaseGBA;
			GBAROMsBox.Text = Global.Config.PathGBAROMs;
			GBASavestatesBox.Text = Global.Config.PathGBASavestates;
			GBASaveRAMBox.Text = Global.Config.PathGBASaveRAM;
			GBAScreenshotsBox.Text = Global.Config.PathGBAScreenshots;
			GBACheatsBox.Text = Global.Config.PathGBACheats;
			//GBAFirmwaresBox.Text = Global.Config.PathGBABIOS;

			TI83BaseBox.Text = Global.Config.BaseTI83;
			TI83ROMsBox.Text = Global.Config.PathTI83ROMs;
			TI83SavestatesBox.Text = Global.Config.PathTI83Savestates;
			TI83SaveRAMBox.Text = Global.Config.PathTI83SaveRAM;
			TI83ScreenshotsBox.Text = Global.Config.PathTI83Screenshots;
			TI83CheatsBox.Text = Global.Config.PathTI83Cheats;

			Atari2600BaseBox.Text = Global.Config.BaseAtari2600;
			Atari2600ROMsBox.Text = Global.Config.PathAtari2600ROMs;
			Atari2600SavestatesBox.Text = Global.Config.PathAtari2600Savestates;
			Atari2600ScreenshotsBox.Text = Global.Config.PathAtari2600Screenshots;
			Atari2600CheatsBox.Text = Global.Config.PathAtari2600Cheats;

			Atari7800BaseBox.Text = Global.Config.BaseAtari7800;
			Atari7800ROMsBox.Text = Global.Config.PathAtari7800ROMs;
			Atari7800SavestatesBox.Text = Global.Config.PathAtari7800Savestates;
			Atari7800SaveRAMBox.Text = Global.Config.PathAtari7800SaveRAM;
			Atari7800ScreenshotsBox.Text = Global.Config.PathAtari7800Screenshots;
			Atari7800CheatsBox.Text = Global.Config.PathAtari7800Cheats;
			//Atari7800FirmwaresBox.Text = Global.Config.PathAtari7800Firmwares;

			C64BaseBox.Text = Global.Config.BaseC64;
			C64ROMsBox.Text = Global.Config.PathC64ROMs;
			C64SavestatesBox.Text = Global.Config.PathC64Savestates;
			C64ScreenshotsBox.Text = Global.Config.PathC64Screenshots;
			C64CheatsBox.Text = Global.Config.PathC64Cheats;
			//C64FirmwaresBox.Text = Global.Config.PathC64Firmwares;

			COLBaseBox.Text = Global.Config.BaseCOL;
			COLROMsBox.Text = Global.Config.PathCOLROMs;
			COLSavestatesBox.Text = Global.Config.PathCOLSavestates;
			COLScreenshotsBox.Text = Global.Config.PathCOLScreenshots;
			COLCheatsBox.Text = Global.Config.PathCOLCheats;
			//COLBiosBox.Text = Global.Config.PathCOLBios;

			MoviesBox.Text = Global.Config.MoviesPath;
			MovieBackupsBox.Text = Global.Config.MoviesBackupPath;
			LuaBox.Text = Global.Config.LuaPath;
			WatchBox.Text = Global.Config.WatchPath;
			AVIBox.Text = Global.Config.AVIPath;
			LogBox.Text = Global.Config.LogPath;
			textBoxFirmware.Text = Global.Config.FirmwaresPath;

			PCEBIOSBox.Text = Global.Config.FilenamePCEBios;
			FDSBIOSBox.Text = Global.Config.FilenameFDSBios;

			SetTabByPlatform();

			if (!Global.MainForm.INTERIM)
			{
				tabControl1.Controls.Remove(tabPageIntellivision);
				tabControl1.Controls.Remove(tabPageC64);
				tabControl1.Controls.Remove(tabPageGBA);
				tabControl1.Controls.Remove(tabPageAtari7800);
			}
		}

		private void SetTabByPlatform()
		{
			switch (Global.Game.System)
			{
				case "NES":
					tabControl1.SelectTab(tabPageNES);
					break;
				case "SNES":
				case "SGB":
					tabControl1.SelectTab(tabPageSNES);
					break;
				case "SMS":
					tabControl1.SelectTab(tabPageSMS);
					break;
				case "SG":
					tabControl1.SelectTab(tabPageSG1000);
					break;
				case "GG":
					tabControl1.SelectTab(tabPageGGear);
					break;
				case "GEN":
					tabControl1.SelectTab(tabPageGenesis);
					break;
				case "PCE":
				case "PCECD":
				case "SGX":
					tabControl1.SelectTab(tabPagePCE);
					break;
				case "GB":
				case "GBC":
					tabControl1.SelectTab(tabPageGameboy);
					break;
				case "TI83":
					tabControl1.SelectTab(tabPageTI83);
					break;
				case "A26":
					tabControl1.SelectTab(tabPageAtari2600);
					break;
				case "A78":
					tabControl1.SelectTab(tabPageAtari7800);
					break;
				case "INTV":
					tabControl1.SelectTab(tabPageIntellivision);
					break;
				case "C64":
					tabControl1.SelectTab(tabPageC64);
					break;
				case "Coleco":
					tabControl1.SelectTab(tabPageColeco);
					break;
				case "GBA":
					tabControl1.SelectTab(tabPageGBA);
					break;
				case "NULL":
					tabControl1.SelectTab(tabPageTools);
					break;
			}
		}

		private void SaveSettings()
		{
			Global.Config.UseRecentForROMs = RecentForROMs.Checked;
			Global.Config.BasePath = BasePathBox.Text;

			Global.Config.BaseINTV = INTVBaseBox.Text;
			Global.Config.PathINTVROMs = INTVRomsBox.Text;
			Global.Config.PathINTVSavestates = INTVSavestatesBox.Text;
			Global.Config.PathINTVScreenshots = INTVScreenshotsBox.Text;
			Global.Config.PathINTVCheats = INTVCheatsBox.Text;
			//Global.Config.PathINTVEROM = INTVEROMBox.Text;
			//Global.Config.PathINTVGROM = INTVGROMBox.Text;

			Global.Config.BaseNES = NESBaseBox.Text;
			Global.Config.PathNESROMs = NESROMsBox.Text;
			Global.Config.PathNESSavestates = NESSavestatesBox.Text;
			Global.Config.PathNESSaveRAM = NESSaveRAMBox.Text;
			Global.Config.PathNESScreenshots = NESScreenshotsBox.Text;
			Global.Config.PathNESCheats = NESCheatsBox.Text;
			Global.Config.PathNESPalette = NESPaletteBox.Text;

			Global.Config.BaseSNES = SNESBaseBox.Text;
			Global.Config.PathSNESROMs = SNESROMsBox.Text;
			Global.Config.PathSNESSavestates = SNESSavestatesBox.Text;
			Global.Config.PathSNESSaveRAM = SNESSaveRAMBox.Text;
			Global.Config.PathSNESScreenshots = SNESScreenshotsBox.Text;
			Global.Config.PathSNESCheats = SNESCheatsBox.Text;
			//Global.Config.PathSNESFirmwares = SNESFirmwaresBox.Text;

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
			Global.Config.PathGBPalettes = GBPalettesBox.Text;

			Global.Config.BaseGBA = GBABaseBox.Text;
			Global.Config.PathGBAROMs = GBAROMsBox.Text;
			Global.Config.PathGBASavestates = GBASavestatesBox.Text;
			Global.Config.PathGBASaveRAM = GBASaveRAMBox.Text;
			Global.Config.PathGBAScreenshots = GBAScreenshotsBox.Text;
			Global.Config.PathGBACheats = GBACheatsBox.Text;
			//Global.Config.PathGBABIOS = GBAFirmwaresBox.Text;

			Global.Config.BaseTI83 = TI83BaseBox.Text;
			Global.Config.PathTI83ROMs = TI83ROMsBox.Text;
			Global.Config.PathTI83Savestates = TI83SavestatesBox.Text;
			Global.Config.PathTI83SaveRAM = TI83SaveRAMBox.Text;
			Global.Config.PathTI83Screenshots = TI83ScreenshotsBox.Text;
			Global.Config.PathTI83Cheats = TI83CheatsBox.Text;

			Global.Config.BaseAtari2600 = Atari2600BaseBox.Text;
			Global.Config.PathAtari2600ROMs = Atari2600ROMsBox.Text;
			Global.Config.PathAtari2600Savestates = Atari2600SavestatesBox.Text;
			Global.Config.PathAtari2600Screenshots = Atari2600ScreenshotsBox.Text;
			Global.Config.PathAtari2600Cheats = Atari2600CheatsBox.Text;

			Global.Config.BaseAtari7800 = Atari7800BaseBox.Text;
			Global.Config.PathAtari7800ROMs = Atari7800ROMsBox.Text;
			Global.Config.PathAtari7800Savestates = Atari7800SavestatesBox.Text;
			Global.Config.PathAtari7800SaveRAM = Atari7800SaveRAMBox.Text;
			Global.Config.PathAtari7800Screenshots = Atari7800ScreenshotsBox.Text;
			Global.Config.PathAtari7800Cheats = Atari7800CheatsBox.Text;
			//Global.Config.PathAtari7800Firmwares = Atari7800FirmwaresBox.Text;

			Global.Config.BaseC64 = C64BaseBox.Text;
			Global.Config.PathC64ROMs = C64ROMsBox.Text;
			Global.Config.PathC64Savestates = C64SavestatesBox.Text;
			Global.Config.PathC64Screenshots = C64ScreenshotsBox.Text;
			Global.Config.PathC64Cheats = C64CheatsBox.Text;
			//Global.Config.PathC64Firmwares = C64FirmwaresBox.Text;

			Global.Config.BaseCOL = COLBaseBox.Text;
			Global.Config.PathCOLROMs = COLROMsBox.Text;
			Global.Config.PathCOLSavestates = COLSavestatesBox.Text;
			Global.Config.PathCOLScreenshots = COLScreenshotsBox.Text;
			Global.Config.PathCOLCheats = COLCheatsBox.Text;
			//Global.Config.PathCOLBios = COLBiosBox.Text;

			Global.Config.MoviesPath = MoviesBox.Text;
			Global.Config.MoviesBackupPath = MovieBackupsBox.Text;
			Global.Config.LuaPath = LuaBox.Text;
			Global.Config.WatchPath = WatchBox.Text;
			Global.Config.AVIPath = AVIBox.Text;
			Global.Config.LogPath = LogBox.Text;
			Global.Config.FirmwaresPath = textBoxFirmware.Text;

			Global.Config.FilenamePCEBios = PCEBIOSBox.Text;
			Global.Config.FilenameFDSBios = FDSBIOSBox.Text;

			BasePathBox.Focus();
			Global.MainForm.UpdateStatusSlots();
		}

		private void Cancel_Click(object sender, EventArgs e)
		{
			Global.OSD.AddMessage("Path config aborted");
			this.Close();
		}

		private void OK_Click(object sender, EventArgs e)
		{
			SaveSettings();
			Global.OSD.AddMessage("Path settings saved");
			this.Close();
		}

		private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
		{
			tabControl1.TabPages[tabControl1.SelectedIndex].Controls[0].Focus(); 
		}

		private void RecentForROMs_CheckedChanged(object sender, EventArgs e)
		{
			Global.Config.UseRecentForROMs = RecentForROMs.Checked;

			NESROMsBox.Enabled = !RecentForROMs.Checked;
			NESBrowseROMs.Enabled = !RecentForROMs.Checked;
			NESROMsDescription.Enabled = !RecentForROMs.Checked;

			SNESBrowseROMs.Enabled = !RecentForROMs.Checked;
			SNESROMsBox.Enabled = !RecentForROMs.Checked;
			SNESROMsDescription.Enabled = !RecentForROMs.Checked;

			Sega8ROMsDescription.Enabled = !RecentForROMs.Checked;
			Sega8ROMsBox.Enabled = !RecentForROMs.Checked;
			Sega8BrowseROMs.Enabled = !RecentForROMs.Checked;

			SGROMsBox.Enabled = !RecentForROMs.Checked;
			SGBrowseROMs.Enabled = !RecentForROMs.Checked;
			SGROMsDescription.Enabled = !RecentForROMs.Checked;

			GGROMBox.Enabled = !RecentForROMs.Checked;
			GGROMsDescription.Enabled = !RecentForROMs.Checked;
			GGBrowseROMs.Enabled = !RecentForROMs.Checked;

			GenesisROMsBox.Enabled = !RecentForROMs.Checked;
			GenesisBrowseROMs.Enabled = !RecentForROMs.Checked;
			GenesisROMsDescription.Enabled = !RecentForROMs.Checked;

			PCEROMsBox.Enabled = !RecentForROMs.Checked;
			PCEBrowseROMs.Enabled = !RecentForROMs.Checked;
			PCEROMsDescription.Enabled = !RecentForROMs.Checked;

			GBROMsBox.Enabled = !RecentForROMs.Checked;
			GBBrowseROMs.Enabled = !RecentForROMs.Checked;
			GBROMsDescription.Enabled = !RecentForROMs.Checked;

			GBAROMsBox.Enabled = !RecentForROMs.Checked;
			GBABrowseROMs.Enabled = !RecentForROMs.Checked;
			GBAROMsDescription.Enabled = !RecentForROMs.Checked;

			TI83ROMsBox.Enabled = !RecentForROMs.Checked;
			TI83BrowseROMs.Enabled = !RecentForROMs.Checked;
			TI83ROMsDescription.Enabled = !RecentForROMs.Checked;

			Atari2600ROMsBox.Enabled = !RecentForROMs.Checked;
			Atari2600BrowseROMs.Enabled = !RecentForROMs.Checked;
			Atari2600ROMsDescription.Enabled = !RecentForROMs.Checked;

			Atari7800ROMsBox.Enabled = !RecentForROMs.Checked;
			Atari7800BrowseROMs.Enabled = !RecentForROMs.Checked;
			Atari7800ROMsDescription.Enabled = !RecentForROMs.Checked;

			C64ROMsBox.Enabled = !RecentForROMs.Checked;
			C64BrowseROMs.Enabled = !RecentForROMs.Checked;
			C64ROMsDescription.Enabled = !RecentForROMs.Checked;

			COLROMsBox.Enabled = !RecentForROMs.Checked;
			COLBrowseROMs.Enabled = !RecentForROMs.Checked;
			COLROMsDescription.Enabled = !RecentForROMs.Checked;

			INTVRomsBox.Enabled = !RecentForROMs.Checked;
			INTVBrowseROMs.Enabled = !RecentForROMs.Checked;
			INTVROMsDescription.Enabled = !RecentForROMs.Checked;
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
			folderBrowserDialog1.Description = "Set the directory for " + Name;
			folderBrowserDialog1.SelectedPath = PathManager.MakeAbsolutePath(box.Text, System);
			DialogResult result = folderBrowserDialog1.ShowDialog();
			if (result == DialogResult.OK)
			{
				box.Text = folderBrowserDialog1.SelectedPath;
			}
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

		private void BrowseAtariBase_Click(object sender, EventArgs e)
		{
			BrowseFolder(Atari2600BaseBox, Atari2600BaseDescription.Text);
		}

		private void BrowseAtariROMs_Click(object sender, EventArgs e)
		{
			BrowseFolder(Atari2600ROMsBox, Atari2600ROMsDescription.Text, "A26");
		}

		private void BrowseAtariSavestates_Click(object sender, EventArgs e)
		{
			BrowseFolder(Atari2600SavestatesBox, Atari2600SavestatesDescription.Text, "A26");
		}

		private void BrowseAtariScreenshots_Click(object sender, EventArgs e)
		{
			BrowseFolder(Atari2600ScreenshotsBox, Atari2600ScreenshotsDescription.Text, "A26");
		}

		private void AtariBrowseCheats_Click(object sender, EventArgs e)
		{
			BrowseFolder(Atari2600CheatsBox, Atari2600CheatsDescription.Text, "A26");
		}

		private void INTVBrowseBase_Click(object sender, EventArgs e)
		{
			BrowseFolder(INTVBaseBox, INTVBaseDescription.Text);
		}

		private void INTVBrowseROMs_Click(object sender, EventArgs e)
		{
			BrowseFolder(INTVRomsBox, INTVROMsDescription.Text, "INTV");
		}

		private void INTVBrowseSavestates_Click(object sender, EventArgs e)
		{
			BrowseFolder(INTVSavestatesBox, INTVSavestatesDescription.Text, "INTV");
		}

		private void INTVBrowseSaveRAM_Click(object sender, EventArgs e)
		{
			BrowseFolder(INTVSaveRAMBox, INTVSaveRAMDescription.Text, "INTV");
		}

		private void INTVBrowseScreenshots_Click(object sender, EventArgs e)
		{
			BrowseFolder(INTVScreenshotsBox, INTVScreenshotsDescription.Text, "INTV");
		}

		private void INTVBrowseCheats_Click(object sender, EventArgs e)
		{
			BrowseFolder(INTVCheatsBox, INTVCheatsDescription.Text, "INTV");
		}

		void BrowseForBios(string filter, string config, string system, TextBox tb)
		{
			
			OpenFileDialog ofd = new OpenFileDialog();
			
			if (!String.IsNullOrWhiteSpace(config))
			{
				ofd.InitialDirectory = PathManager.MakeAbsolutePath(Path.GetDirectoryName(config), system);
			}
			else
			{
				ofd.InitialDirectory = PathManager.MakeAbsolutePath(Global.Config.BaseCOL, "Coleco");
			}

			ofd.Filter = filter;
			ofd.FileName = Path.GetFileName(config);

			var result = ofd.ShowDialog();
			if (result != DialogResult.OK)
				return;


			if (File.Exists(ofd.FileName) == false)
				return;

			tb.Text = ofd.FileName;
		}


		/*private void INTVBrowseEROM_Click(object sender, EventArgs e)
		{
			BrowseForBios(
				"Intellivision EROM (*.bin; *.int)|*.bin;*.int|All Files|*.*",
				 Global.Config.PathINTVEROM, "INTV", 
				INTVEROMBox);
		}*/

		/*private void INTVBroseGROM_Click(object sender, EventArgs e)
		{
			BrowseForBios(
				"Intellivision GROM (*.bin; *.int)|*.bin;*.int|All Files|*.*",
				 Global.Config.PathINTVGROM, "INTV",
				INTVGROMBox);
		}*/

		private void BrowseMovieBackups_Click(object sender, EventArgs e)
		{
			BrowseFolder(MovieBackupsBox, MovieBackupsDescription.Text);
		}

		private void GBBrowsePalettes_Click(object sender, EventArgs e)
		{
			BrowseFolder(GBPalettesBox, GBPalettesDescription.Text);
		}

		private void BrowseSNESBase_Click(object sender, EventArgs e)
		{
			BrowseFolder(SNESBaseBox, SNESBaseDescription.Text);
		}

		private void BrowseSNESROMs_Click(object sender, EventArgs e)
		{
			BrowseFolder(SNESROMsBox, SNESROMsDescription.Text, "SNES");
		}

		private void BrowseSNESSavestates_Click(object sender, EventArgs e)
		{
			BrowseFolder(SNESSavestatesBox, SNESSavestatesDescription.Text, "SNES");
		}

		private void button5_Click(object sender, EventArgs e)
		{
			BrowseFolder(SNESSaveRAMBox, SNESSaveRAMDescription.Text, "SNES");
		}

		private void SNESBrowseCheats_Click(object sender, EventArgs e)
		{
			BrowseFolder(SNESCheatsBox, SNESCheatsDescription.Text, "SNES");
		}

		/*private void SNESBrowseFirmwares_Click(object sender, EventArgs e)
		{
			BrowseFolder(SNESFirmwaresBox, SNESFirmwaresDescription.Text);
		}*/

		private void BrowseLog_Click(object sender, EventArgs e)
		{
			BrowseFolder(LogBox, LogDescription.Text);
		}

		private void button1_Click(object sender, EventArgs e)
		{
			new PathInfo().Show();
		}

		private void BrowseSNESSaveRAM_Click(object sender, EventArgs e)
		{
			BrowseFolder(SNESSaveRAMBox, SNESSaveRAMDescription.Text);
		}

		/*private void NESBrowseFDSBios_Click(object sender, EventArgs e)
		{
			BrowseForBios(
				"ROM files (*.rom)|*.rom|All Files|*.*",
				 Global.Config.PathFDSBios, "NES",
				NESFDSBiosBox);
		}*/

		private void SNESFirmwaresDescription_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			System.Diagnostics.Process.Start("http://tasvideos.org/Bizhawk/Firmwares.html");
		}

		private void BrowseC64Base_Click(object sender, EventArgs e)
		{
			BrowseFolder(C64BaseBox, C64BaseDescription.Text);
		}

		private void BrowseC64ROMs_Click(object sender, EventArgs e)
		{
			BrowseFolder(NESROMsBox, NESROMsDescription.Text, "C64");
		}

		private void BrowseC64Savestates_Click(object sender, EventArgs e)
		{
			BrowseFolder(NESSavestatesBox, NESSavestatesDescription.Text, "C64");
		}

		private void BrowseC64Screenshots_Click(object sender, EventArgs e)
		{
			BrowseFolder(C64ScreenshotsBox, C64ScreenshotsDescription.Text, "C64");
		}

		private void C64BrowseCheats_Click(object sender, EventArgs e)
		{
			BrowseFolder(C64CheatsBox, C64CheatsDescription.Text, "C64");
		}

		/*private void C64BrowseFirmwares_Click(object sender, EventArgs e)
		{
			BrowseFolder(C64FirmwaresBox, C64FirmwaresDescription.Text, "C64");
		}*/

		private void C64FirmwaresDescription_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			System.Diagnostics.Process.Start("http://tasvideos.org/Bizhawk/Firmwares.html");
		}

		private void BrowseCOLBase_Click(object sender, EventArgs e)
		{
			BrowseFolder(COLBaseBox, COLBaseDescription.Text);
		}

		private void COLBrowseROMs_Click(object sender, EventArgs e)
		{
			BrowseFolder(COLROMsBox, COLROMsDescription.Text, "Coleco");
		}

		private void BrowseCOLSavestates_Click(object sender, EventArgs e)
		{
			BrowseFolder(COLSavestatesBox, COLSavestatesDescription.Text, "Coleco");
		}

		private void BrowseCOLScreenshots_Click(object sender, EventArgs e)
		{
			BrowseFolder(COLScreenshotsBox, COLScreenshotsDescription.Text, "Coleco");
		}

		private void COLBrowseCheats_Click(object sender, EventArgs e)
		{
			BrowseFolder(COLScreenshotsBox, COLScreenshotsDescription.Text, "Coleco");
		}

		/*private void COLBrowseBios_Click(object sender, EventArgs e)
		{
			BrowseForBios(
				"ROM files (*.bin)|*.bin|All Files|*.*",
				 Global.Config.PathCOLBios, "Coleco",
				COLBiosBox);
		}*/

		private void GBABrowseBase_Click(object sender, EventArgs e)
		{
			BrowseFolder(GBABaseBox, GBABaseDescription.Text);
		}

		private void GBABrowseROMs_Click(object sender, EventArgs e)
		{
			BrowseFolder(GBAROMsBox, GBAROMsDescription.Text, "GBA");
		}

		private void GBABrowseSavestates_Click(object sender, EventArgs e)
		{
			BrowseFolder(GBASavestatesBox, GBASavestatesDescription.Text, "GBA");
		}

		private void GBABrowseSaveRAM_Click(object sender, EventArgs e)
		{
			BrowseFolder(GBASaveRAMBox, GBASaveRAMDescription.Text, "GBA");
		}

		private void GBABrowseScreenshots_Click(object sender, EventArgs e)
		{
			BrowseFolder(GBAScreenshotsBox, GBAScreenshotsDescription.Text, "GBA");
		}

		private void GBABrowseCheats_Click(object sender, EventArgs e)
		{
			BrowseFolder(GBACheatsBox, GBACheatsDescription.Text, "GBA");
		}

		/*private void GBABrowseFirmwares_Click(object sender, EventArgs e)
		{
			BrowseForBios(
				"GBA BIOS (*.rom)|*.rom|All Files|*.*",
				 Global.Config.PathGBABIOS, "GBA",
				GBAFirmwaresBox);
		}*/

		private void Atari78FirmwaresDescription_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			System.Diagnostics.Process.Start("http://tasvideos.org/Bizhawk/Firmwares.html");
		}

		private void BrowseAtari7800Base_Click(object sender, EventArgs e)
		{
			BrowseFolder(Atari7800BaseBox, Atari7800BaseDescription.Text);
			
		}

		private void Atari7800BrowseROMs_Click(object sender, EventArgs e)
		{
			BrowseFolder(Atari7800ROMsBox, Atari7800ROMsDescription.Text, "A78");
		}

		private void BrowseAtari7800Savestates_Click(object sender, EventArgs e)
		{
			BrowseFolder(Atari7800SavestatesBox, Atari7800SavestatesDescription.Text, "A78");
		}

		private void BrowseAtari7800Screenshots_Click(object sender, EventArgs e)
		{
			BrowseFolder(Atari7800ScreenshotsBox, Atari7800ScreenshotsDescription.Text, "A78");
		}

		private void Atari7800BrowseCheats_Click(object sender, EventArgs e)
		{
			BrowseFolder(Atari7800CheatsBox, Atari7800CheatsDescription.Text, "A78");
		}

		/*private void Atari7800BrowseFirmwares_Click(object sender, EventArgs e)
		{
			BrowseFolder(Atari7800FirmwaresBox, Atari7800FirmwaresDescription.Text, "A78");
		}*/

		private void BrowseAtari7800SaveRAM_Click(object sender, EventArgs e)
		{
			BrowseFolder(Atari7800SaveRAMBox, Atari7800SaveRAMsDescription.Text, "A78");
		}

		private void buttonFirmware_Click(object sender, EventArgs e)
		{
			BrowseFolder(textBoxFirmware, labelFirmware.Text);
		}

		private void button2_Click(object sender, EventArgs e)
		{
			OpenFileDialog ofd = new OpenFileDialog();
			ofd.InitialDirectory = PathManager.MakeAbsolutePath(Global.Config.FirmwaresPath, "");
			ofd.Filter = "BIOS Files (*.bin)|*.bin|All Files|*.*";
			ofd.RestoreDirectory = false;
			DialogResult result = ofd.ShowDialog();
			if (result == DialogResult.OK)
			{
				var file = new FileInfo(ofd.FileName);
				PCEBIOSBox.Text = file.Name;
			}
		}

		private void NESBrowseFDSBIOS_Click(object sender, EventArgs e)
		{
			OpenFileDialog ofd = new OpenFileDialog();
			ofd.InitialDirectory = PathManager.MakeAbsolutePath(Global.Config.FirmwaresPath, "");
			ofd.Filter = "FDS BIOS Files (*.rom)|*.rom|All Files|*.*";
			ofd.RestoreDirectory = false;
			DialogResult result = ofd.ShowDialog();
			if (result == DialogResult.OK)
			{
				var file = new FileInfo(ofd.FileName);
				FDSBIOSBox.Text = file.Name;
			}
		}
	}
}
