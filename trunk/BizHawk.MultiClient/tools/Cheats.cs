using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.IO;
using System.Globalization;

using BizHawk.Emulation.Consoles.Nintendo.SNES;
using BizHawk.Emulation.Consoles.Nintendo;
using BizHawk.Emulation.Consoles.Sega;

namespace BizHawk.MultiClient
{
	public partial class Cheats : Form
	{
		private int defaultWidth;	//For saving the default size of the dialog, so the user can restore if desired
		private int defaultHeight;
		private int defaultNameWidth;
		private int defaultAddressWidth;
		private int defaultValueWidth;
		private int defaultCompareWidth;
		private int defaultDomainWidth;
		private int defaultOnWidth;

		private void ClearFields()
		{
			NameBox.Text = "";
			AddressBox.Text = "";
			ValueBox.Text = "";
			CompareBox.Text = "";
			PopulateMemoryDomainComboBox();
			AddressBox.MaxLength = GetNumDigits(Global.Emulator.MemoryDomains[0].Size - 1);
		}

		public void Restart()
		{
			NewCheatList(); //Should be run even if dialog isn't open so cheats system can work
			if (!IsHandleCreated || IsDisposed) return;
			ClearFields();
			ToggleGameGenieButtons();
		}

		public void UpdateValues()
		{
			if (!IsHandleCreated || IsDisposed) return;
			CheatListView.ItemCount = Global.CheatList.Count;
			DisplayCheatsList();
			CheatListView.Refresh();
		}

		public Cheats()
		{
			InitializeComponent();
			Closing += (o, e) => SaveConfigSettings();
			CheatListView.QueryItemText += CheatListView_QueryItemText;
			CheatListView.QueryItemBkColor += CheatListView_QueryItemBkColor;
			CheatListView.VirtualMode = true;
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			if (!Global.Config.CheatsAutoSaveOnClose)
			{
				if (!AskSave())
					e.Cancel = true;
			}
			base.OnClosing(e);
		}

		private void CheatListView_QueryItemBkColor(int index, int column, ref Color color)
		{
			if (index < Global.CheatList.Count)
			{
				if (Global.CheatList[index].Address < 0)
				{
					color = Color.DarkGray;
				}
				else if (Global.CheatList[index].IsEnabled)
				{
					color = Color.LightCyan;
				}
				else
				{
					color = BackColor;
				}
			}
		}

		private void CheatListView_QueryItemText(int index, int column, out string text)
		{
			text = "";
			if (Global.CheatList[index].IsSeparator)
			{
				return;
			}
			else if (column == 0) //Name
			{
				text = Global.CheatList[index].Name;
			}
			else if (column == 1) //Address
			{
				text = Global.CheatList.FormatAddress(Global.CheatList[index].Address);
			}
			else if (column == 2) //Value
			{
				if (Global.Config.Cheats_ValuesAsHex)
				{
					text = String.Format("{0:X2}", Global.CheatList[index].Value);
				}
				else
				{
					text = Global.CheatList[index].Value.ToString();
				}
			}
			else if (column == 3) //Compare
			{
				if (Global.Config.Cheats_ValuesAsHex)
				{
					text = String.Format("{0:X2}", Global.CheatList[index].Compare);
				}
				else
				{
					text = Global.CheatList[index].Compare.ToString();
				}
			}
			else if (column == 4) //Domain
			{
				text = Global.CheatList[index].Domain.Name;
			}
			else if (column == 5) //Enabled
			{
				if (Global.CheatList[index].IsEnabled)
				{
					text = "*";
				}
				else
				{
					text = "";
				}
			}
		}

		private int GetNumDigits(Int32 i)
		{
			if (i < 0x10000) return 4;
			if (i < 0x1000000) return 6;
			else return 8;
		}

		private void ToggleGameGenieButtons()
		{
			if ((Global.Emulator is NES) || (Global.Emulator is Genesis) || (Global.Emulator.SystemId == "GB") || (Global.Game.System == "GG") || (Global.Emulator is LibsnesCore))
			{
				toolStripButtonLoadGameGenie.Visible = true;
				toolStripSeparator7.Visible = true;
				openGameGenieEncoderDecoderToolStripMenuItem.Visible = true;
			}
			else
			{
				toolStripButtonLoadGameGenie.Visible = false;
				toolStripSeparator7.Visible = false;
				openGameGenieEncoderDecoderToolStripMenuItem.Visible = false;
			}
		}

		private void Cheats_Load(object sender, EventArgs e)
		{
			ToggleGameGenieButtons();
			LoadConfigSettings();
			PopulateMemoryDomainComboBox();
			AddressBox.MaxLength = GetNumDigits(Global.Emulator.MainMemory.Size - 1);
			DisplayCheatsList();
			CheatListView.Refresh();
			DoValuesAsHexOption();
			//Hacky Disabling if not a supported core
			switch (Global.Emulator.SystemId)
			{
				case "UglyBrokenCore":
					AddCheatGroup.Enabled = false;
					CheatListView.Enabled = false;
					MessageLabel.Text = Global.Emulator.SystemId + " not supported.";
					break;
			}
		}

		private void PopulateMemoryDomainComboBox()
		{
			DomainComboBox.Items.Clear();
			if (Global.Emulator.MemoryDomains.Count > 0)
			{
				foreach (MemoryDomain t in Global.Emulator.MemoryDomains)
				{
					string str = t.ToString();
					DomainComboBox.Items.Add(str);
				}
				DomainComboBox.SelectedIndex = 0;
			}
		}

		public void AddCheat(Cheat c)
		{
			if (c == null)
			{
				return;
			}
			Changes();
			Global.CheatList.Add(c);
			Global.OSD.AddMessage("Cheat added.");
			if (!IsHandleCreated || IsDisposed) return;
			DisplayCheatsList();
			CheatListView.Refresh();

			Global.MainForm.RamWatch1.UpdateValues();
			Global.MainForm.RamSearch1.UpdateValues();
			Global.MainForm.HexEditor1.UpdateValues();
		}

		public void RemoveCheat(Cheat c)
		{
			Changes();

			Global.CheatList.Remove(c.Domain, c.Address);

			Global.OSD.AddMessage("Cheat removed.");
			if (!IsHandleCreated || IsDisposed) return;
			DisplayCheatsList();
			CheatListView.Refresh();
			Global.MainForm.RamWatch1.UpdateValues();
			Global.MainForm.RamSearch1.UpdateValues();
			Global.MainForm.HexEditor1.UpdateValues();
		}

		public void LoadCheatFromRecent(string file)
		{
			bool z = true;

			if (Global.CheatList.Changes) z = AskSave();

			if (z)
			{
				bool r = LoadCheatFile(file, false);
				if (!r)
				{
					DialogResult result = MessageBox.Show("Could not open " + file + "\nRemove from list?", "File not found", MessageBoxButtons.YesNo, MessageBoxIcon.Error);
					if (result == DialogResult.Yes)
						Global.Config.RecentCheats.Remove(file);
				}
				DisplayCheatsList();
				Global.CheatList.Changes = false;
			}
		}

		private void recentToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			//Clear out recent Cheats list
			//repopulate it with an up to date list
			recentToolStripMenuItem.DropDownItems.Clear();

			if (Global.Config.RecentCheats.Empty)
			{
				var none = new ToolStripMenuItem {Enabled = false, Text = "None"};
				recentToolStripMenuItem.DropDownItems.Add(none);
			}
			else
			{
				for (int x = 0; x < Global.Config.RecentCheats.Count; x++)
				{
					string path = Global.Config.RecentCheats[x];
					var item = new ToolStripMenuItem {Text = path};
					item.Click += (o, ev) => LoadCheatFromRecent(path);
					recentToolStripMenuItem.DropDownItems.Add(item);
				}
			}

			recentToolStripMenuItem.DropDownItems.Add("-");

			var clearitem = new ToolStripMenuItem {Text = "&Clear"};
			clearitem.Click += (o, ev) => Global.Config.RecentCheats.Clear();
			recentToolStripMenuItem.DropDownItems.Add(clearitem);
		}

		private void LoadConfigSettings()
		{
			ColumnPositionSet();

			defaultWidth = Size.Width;     //Save these first so that the user can restore to its original size
			defaultHeight = Size.Height;
			defaultNameWidth = CheatListView.Columns[Global.Config.CheatsNameIndex].Width;
			defaultAddressWidth = CheatListView.Columns[Global.Config.CheatsAddressIndex].Width;
			defaultValueWidth = CheatListView.Columns[Global.Config.CheatsValueIndex].Width;
			defaultCompareWidth = CheatListView.Columns[Global.Config.CheatsCompareIndex].Width;
			defaultOnWidth = CheatListView.Columns[Global.Config.CheatsOnIndex].Width;
			defaultDomainWidth = CheatListView.Columns[Global.Config.CheatsDomainIndex].Width;

			if (Global.Config.CheatsSaveWindowPosition && Global.Config.CheatsWndx >= 0 && Global.Config.CheatsWndy >= 0)
			{
				Location = new Point(Global.Config.CheatsWndx, Global.Config.CheatsWndy);
			}

			if (Global.Config.CheatsWidth >= 0 && Global.Config.CheatsHeight >= 0)
			{
				Size = new Size(Global.Config.CheatsWidth, Global.Config.CheatsHeight);
			}

			if (Global.Config.CheatsNameWidth > 0)
			{
				CheatListView.Columns[Global.Config.CheatsNameIndex].Width = Global.Config.CheatsNameWidth;
			}
			if (Global.Config.CheatsAddressWidth > 0)
			{
				CheatListView.Columns[Global.Config.CheatsAddressIndex].Width = Global.Config.CheatsAddressWidth;
			}
			if (Global.Config.CheatsValueWidth > 0)
			{
				CheatListView.Columns[Global.Config.CheatsValueIndex].Width = Global.Config.CheatsValueWidth;
			}
			if (Global.Config.CheatsCompareWidth > 0)
			{
				CheatListView.Columns[Global.Config.CheatsValueIndex].Width = Global.Config.CheatsCompareWidth;
			}
			if (Global.Config.CheatsDomainWidth > 0)
			{
				CheatListView.Columns[Global.Config.CheatsDomainIndex].Width = Global.Config.CheatsDomainWidth;
			}
			if (Global.Config.CheatsOnWidth > 0)
			{
				CheatListView.Columns[Global.Config.CheatsOnIndex].Width = Global.Config.CheatsOnWidth;
			}
		}

		public void SaveConfigSettings()
		{
			ColumnPositionSet();
			Global.Config.CheatsWndx = Location.X;
			Global.Config.CheatsWndy = Location.Y;
			Global.Config.CheatsWidth = Right - Left;
			Global.Config.CheatsHeight = Bottom - Top;

			Global.Config.CheatsNameWidth = CheatListView.Columns[Global.Config.CheatsNameIndex].Width;
			Global.Config.CheatsAddressWidth = CheatListView.Columns[Global.Config.CheatsAddressIndex].Width;
			Global.Config.CheatsValueWidth = CheatListView.Columns[Global.Config.CheatsValueIndex].Width;
			Global.Config.CheatsCompareWidth = CheatListView.Columns[Global.Config.CheatsCompareIndex].Width;
			Global.Config.CheatsOnWidth = CheatListView.Columns[Global.Config.CheatsOnIndex].Width;
			Global.Config.CheatsDomainWidth = CheatListView.Columns[Global.Config.CheatsDomainIndex].Width;
		}

		public void DisplayCheatsList()
		{
			UpdateNumberOfCheats();
			CheatListView.ItemCount = Global.CheatList.Count;
		}

		private void MoveUp()
		{
			ListView.SelectedIndexCollection indexes = CheatListView.SelectedIndices;
			if (indexes[0] == 0)
				return;
			if (indexes.Count == 0) return;
			foreach (int index in indexes)
			{
				Cheat temp = Global.CheatList[index];
				Global.CheatList.Remove(Global.CheatList[index]);
				Global.CheatList.Insert(index - 1, temp);

				//Note: here it will get flagged many times redundantly potentially, 
				//but this avoids it being flagged falsely when the user did not select an index
				Changes();
			}
			List<int> i = new List<int>();
			for (int z = 0; z < indexes.Count; z++)
			{
				i.Add(indexes[z] - 1);
			}

			CheatListView.SelectedIndices.Clear();
			foreach (int t in i)
			{
				CheatListView.SelectItem(t, true);
			}

			DisplayCheatsList();
		}

		private void MoveDown()
		{
			ListView.SelectedIndexCollection indexes = CheatListView.SelectedIndices;
			if (indexes.Count == 0) return;
			foreach (int index in indexes)
			{
				Cheat temp = Global.CheatList[index];

				if (index < Global.CheatList.Count - 1)
				{
					Global.CheatList.Remove(Global.CheatList[index]);
					Global.CheatList.Insert(index + 1, temp);
				}

				//Note: here it will get flagged many times redundantly potnetially, 
				//but this avoids it being flagged falsely when the user did not select an index
				Changes();
			}

			List<int> i = new List<int>();
			for (int z = 0; z < indexes.Count; z++)
			{
				i.Add(indexes[z] + 1);
			}

			CheatListView.SelectedIndices.Clear();
			//for (int z = 0; z < i.Count; z++)
			//CheatListView.SelectItem(i[z], true); //TODO

			DisplayCheatsList();
		}

		private void toolStripButtonMoveUp_Click(object sender, EventArgs e)
		{
			MoveUp();
		}

		private void moveUpToolStripMenuItem_Click(object sender, EventArgs e)
		{
			MoveUp();
		}

		private void toolStripButtonMoveDown_Click(object sender, EventArgs e)
		{
			MoveDown();
		}

		private void moveDownToolStripMenuItem_Click(object sender, EventArgs e)
		{
			MoveDown();
		}

		void Changes()
		{
			Global.CheatList.Changes = true;
			MessageLabel.Text = Path.GetFileName(Global.CheatList.CurrentCheatFile) + " *";
		}

		private FileInfo GetSaveFileFromUser()
		{
			var sfd = new SaveFileDialog();
			if (Global.CheatList.CurrentCheatFile.Length > 0)
				sfd.FileName = Path.GetFileNameWithoutExtension(Global.CheatList.CurrentCheatFile);
			else if (!(Global.Emulator is NullEmulator))
				sfd.FileName = PathManager.FilesystemSafeName(Global.Game);
			sfd.InitialDirectory = Global.CheatList.CheatsPath;
			sfd.Filter = "Cheat Files (*.cht)|*.cht|All Files|*.*";
			sfd.RestoreDirectory = true;
			Global.Sound.StopSound();
			var result = sfd.ShowDialog();
			Global.Sound.StartSound();
			if (result != DialogResult.OK)
				return null;
			var file = new FileInfo(sfd.FileName);
			Global.Config.LastRomPath = file.DirectoryName;
			return file;
		}

		private void SaveAs()
		{
			var file = GetSaveFileFromUser();
			if (file != null)
			{
				Global.CheatList.SaveCheatFile(file.FullName);
				Global.CheatList.CurrentCheatFile = file.FullName;
				MessageLabel.Text = Path.GetFileName(Global.CheatList.CurrentCheatFile) + " saved.";
				Global.Config.RecentCheats.Add(Global.CheatList.CurrentCheatFile);
			}
		}

		public bool AskSave()
		{
			if (Global.Config.SupressAskSave) //User has elected to not be nagged
			{
				return true;
			}

			if (Global.CheatList.Changes)
			{
				DialogResult result = MessageBox.Show("Save Changes?", "Cheats", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button3);

				if (result == DialogResult.Yes)
				{
					//TOOD: Do quicksave if filename, else save as
					if (String.CompareOrdinal(Global.CheatList.CurrentCheatFile, "") == 0)
					{
						SaveAs();
					}
					else
						Global.CheatList.SaveCheatFile(Global.CheatList.CurrentCheatFile);
					return true;
				}
				else if (result == DialogResult.No)
					return true;
				else if (result == DialogResult.Cancel)
					return false;
			}
			return true;
		}

		private void NewCheatList()
		{
			bool result = true;
			if (Global.CheatList.Changes) result = AskSave();

			if (result)
			{
				Global.CheatList.Clear();
				DisplayCheatsList();
				Global.CheatList.CurrentCheatFile = "";
				Global.CheatList.Changes = false;
				MessageLabel.Text = "";
			}
		}

		private void newToolStripMenuItem_Click(object sender, EventArgs e)
		{
			NewCheatList();
		}

		private void newToolStripButton_Click(object sender, EventArgs e)
		{
			NewCheatList();
		}

		private void saveToolStripButton_Click(object sender, EventArgs e)
		{
			Save();
		}

		private void Save()
		{
			if (String.IsNullOrEmpty(Global.CheatList.CurrentCheatFile))
			{
				Global.CheatList.CurrentCheatFile = Global.CheatList.DefaultFilename;
			}

			Global.CheatList.SaveCheatFile(Global.CheatList.CurrentCheatFile);
			MessageLabel.Text = Path.GetFileName(Global.CheatList.CurrentCheatFile) + " saved.";
		}

		private void saveToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Save();
		}

		private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			SaveAs();
		}

		private FileInfo GetFileFromUser()
		{
			var ofd = new OpenFileDialog();
			if (Global.CheatList.CurrentCheatFile.Length > 0)
				ofd.FileName = Path.GetFileNameWithoutExtension(Global.CheatList.CurrentCheatFile);
			ofd.InitialDirectory = Global.CheatList.CheatsPath;
			ofd.Filter = "Cheat Files (*.cht)|*.cht|All Files|*.*";
			ofd.RestoreDirectory = true;

			Global.Sound.StopSound();
			var result = ofd.ShowDialog();
			Global.Sound.StartSound();
			if (result != DialogResult.OK)
				return null;
			var file = new FileInfo(ofd.FileName);
			Global.Config.LastRomPath = file.DirectoryName;
			return file;
		}

		

		public bool LoadCheatFile(string path, bool append)
		{
			Global.CheatList.LoadCheatFile(path, append);
			UpdateNumberOfCheats();
			MessageLabel.Text = Path.GetFileName(Global.CheatList.CurrentCheatFile);
			DisplayCheatsList();
			return true;
		}

		

		private void OpenCheatFile()
		{
			var file = GetFileFromUser();
			if (file != null)
			{
				bool r = true;
				if (Global.CheatList.Changes) r = AskSave();
				if (r)
				{
					LoadCheatFile(file.FullName, false);
				}
			}
		}

		private void openToolStripMenuItem_Click(object sender, EventArgs e)
		{
			OpenCheatFile();
		}

		private void openToolStripButton_Click(object sender, EventArgs e)
		{
			OpenCheatFile();
		}

		private void InsertSeparator()
		{
			Changes();
			Cheat c = new Cheat {Address = -1};

			ListView.SelectedIndexCollection indexes = CheatListView.SelectedIndices;
			if (indexes.Count > 0)
			{
				if (indexes[0] > 0)
				{
					Global.CheatList.Insert(indexes[0], c);
				}
			}
			else
			{
				Global.CheatList.Add(c);
			}
			DisplayCheatsList();
			CheatListView.Refresh();
		}

		private void toolStripButtonSeparator_Click(object sender, EventArgs e)
		{
			InsertSeparator();
		}

		private void insertSeparatorToolStripMenuItem_Click(object sender, EventArgs e)
		{
			InsertSeparator();
		}

		private Cheat MakeCheat()
		{
			if (String.IsNullOrWhiteSpace(AddressBox.Text) || String.IsNullOrWhiteSpace(ValueBox.Text))
			{
				return null;
			}
			
			Cheat c = new Cheat {Name = NameBox.Text};

			try
			{
				c.Address = int.Parse(AddressBox.Text, NumberStyles.HexNumber);
				c.Value = (byte)(int.Parse(ValueBox.Text, Global.Config.Cheats_ValuesAsHex ? NumberStyles.HexNumber : NumberStyles.Integer));
				if (String.IsNullOrWhiteSpace(CompareBox.Text))
				{
					c.Compare = null;
				}
				else
				{
					c.Compare = (byte)(int.Parse(CompareBox.Text, Global.Config.Cheats_ValuesAsHex ? NumberStyles.HexNumber : NumberStyles.Integer));
				}
				c.Domain = Global.Emulator.MemoryDomains[DomainComboBox.SelectedIndex];
				c.Enable();
			}
			catch
			{
				return null;
			}
			return c;
		}

		private void AddCheatButton_Click(object sender, EventArgs e)
		{
			AddCheat(MakeCheat());
		}

		private void addCheatToolStripMenuItem_Click(object sender, EventArgs e)
		{
			AddCheat(MakeCheat());
		}

		private void RemoveCheat()
		{
			if (Global.CheatList.Count == 0) return;
			Changes();
			ListView.SelectedIndexCollection indexes = CheatListView.SelectedIndices;
			if (indexes.Count > 0)
			{
				foreach (int index in indexes)
				{
					Global.CheatList.Remove(Global.CheatList[indexes[0]]); //index[0] used since each iteration will make this the correct list index
				}
				indexes.Clear();
				DisplayCheatsList();
			}
		}

		private void cutToolStripButton_Click(object sender, EventArgs e)
		{
			RemoveCheat();
		}

		private void removeCheatToolStripMenuItem_Click(object sender, EventArgs e)
		{
			RemoveCheat();
		}

		private void UpdateNumberOfCheats()
		{
			string message = "";
			int active = Global.CheatList.ActiveCheatCount;

			int c = Global.CheatList.Count;
			if (c == 1)
			{
				message += c.ToString() + " cheat (" + active.ToString() + " active)";
			}
			else if (c == 0)
			{
				message += c.ToString() + " cheats";
			}
			else
			{
				message += c.ToString() + " cheats (" + active.ToString() + " active)";
			}

			NumCheatsLabel.Text = message;
		}

		private void saveWindowPositionToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.CheatsSaveWindowPosition ^= true;
		}

		private void optionsToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			saveWindowPositionToolStripMenuItem.Checked = Global.Config.CheatsSaveWindowPosition;
			CheatsOnOffLoadToolStripMenuItem.Checked = Global.Config.DisableCheatsOnLoad;
			autoloadDialogToolStripMenuItem.Checked = Global.Config.AutoLoadCheats;
			LoadCheatFileByGameToolStripMenuItem.Checked = Global.Config.LoadCheatFileByGame;
			saveCheatsOnCloseToolStripMenuItem.Checked = Global.Config.CheatsAutoSaveOnClose;
			showValuesAsHexToolStripMenuItem.Checked = Global.Config.Cheats_ValuesAsHex;
		}

		private void DuplicateCheat()
		{
			ListView.SelectedIndexCollection indexes = CheatListView.SelectedIndices;
			if (indexes.Count > 0)
			{
				Cheat c = new Cheat();
				int x = indexes[0];
				c.Name = Global.CheatList[x].Name;
				c.Address = Global.CheatList[x].Address;
				c.Value = Global.CheatList[x].Value;
				Changes();
				Global.CheatList.Add(c);
				DisplayCheatsList();
			}
		}

		private void copyToolStripButton_Click(object sender, EventArgs e)
		{
			DuplicateCheat();
		}

		private void duplicateToolStripMenuItem_Click(object sender, EventArgs e)
		{
			DuplicateCheat();
		}

		private void Toggle()
		{
			ListView.SelectedIndexCollection indexes = CheatListView.SelectedIndices;
			if (indexes.Count > 0)
			{
				for (int x = 0; x < indexes.Count; x++)
				{
					if (Global.CheatList[indexes[x]].IsEnabled)
					{
						Global.CheatList[indexes[x]].Disable();
					}
					else
					{
						try
						{
							Global.CheatList[indexes[x]].Enable();
						}
						catch
						{
							Global.CheatList.NotSupportedError();
						}
					}
				}
				CheatListView.Refresh();

				Global.MainForm.RamWatch1.UpdateValues();
				Global.MainForm.RamSearch1.UpdateValues();
				Global.MainForm.HexEditor1.UpdateValues();
			}
			UpdateNumberOfCheats();
		}

		private void CheatListView_DoubleClick(object sender, EventArgs e)
		{
			Toggle();
		}

		private void DoHexorInt(TextBox box, byte? value)
		{
			if (value == null)
			{
				return;
			}
			else if (Global.Config.Cheats_ValuesAsHex)
			{
				box.Text = String.Format("{0:X2}", value);
			}
			else
			{
				box.Text = value.ToString();
			}
		}

		private void CheatListView_Click(object sender, EventArgs e)
		{
			ListView.SelectedIndexCollection indexes = CheatListView.SelectedIndices;
			if (indexes.Count > 0)
			{
				NameBox.Text = Global.CheatList[indexes[0]].Name;
				AddressBox.Text = Global.CheatList.FormatAddress(Global.CheatList[indexes[0]].Address);
				DoHexorInt(ValueBox, Global.CheatList[indexes[0]].Value);
				DoHexorInt(CompareBox, Global.CheatList[indexes[0]].Compare);
				
				SetDomainSelection(Global.CheatList[indexes[0]].Domain.ToString());
				CheatListView.Refresh();
			}
		}

		private void EditButton_Click(object sender, EventArgs e)
		{
			ListView.SelectedIndexCollection indexes = CheatListView.SelectedIndices;
			if (indexes.Count > 0)
			{
				if (AddressBox.Text.Length > 0 && ValueBox.Text.Length > 0)
				{
					Global.CheatList.Remove(Global.CheatList[indexes[0]]);
					Global.CheatList.Add(MakeCheat());
					Changes();
				}
				CheatListView.Refresh();
			}
		}

		private void CheatListView_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (CheatListView.SelectedIndices.Count > 0)
			{
				EditButton.Enabled = true;
			}
			else
			{
				EditButton.Enabled = false;
			}
		}

		private void AddressBox_TextChanged(object sender, EventArgs e)
		{
			if (AddressBox.Text.Length > 0 && ValueBox.Text.Length > 0)
			{
				AddCheatButton.Enabled = true;
			}
			else
			{
				AddCheatButton.Enabled = false;
			}
		}

		private void ValueBox_TextChanged(object sender, EventArgs e)
		{
			if (AddressBox.Text.Length > 0 && ValueBox.Text.Length > 0)
			{
				AddCheatButton.Enabled = true;
			}
			else
			{
				AddCheatButton.Enabled = false;
			}
		}

		private void NameBox_TextChanged(object sender, EventArgs e)
		{
			if (AddressBox.Text.Length > 0 && ValueBox.Text.Length > 0)
			{
				AddCheatButton.Enabled = true;
			}
			else
			{
				AddCheatButton.Enabled = false;
			}
		}

		private void AddressBox_KeyPress(object sender, KeyPressEventArgs e)
		{
			if (e.KeyChar == '\b' || e.KeyChar == 22 || e.KeyChar == 1 || e.KeyChar == 3)
			{
				return;
			}
			else if (!InputValidate.IsValidHexNumber(e.KeyChar))
			{
				e.Handled = true;
			}
		}

		private void ValueBox_KeyPress(object sender, KeyPressEventArgs e)
		{
			if (e.KeyChar == '\b' || e.KeyChar == 22 || e.KeyChar == 1 || e.KeyChar == 3)
			{
				return;
			}
			else
			{
				if (Global.Config.Cheats_ValuesAsHex)
				{
					if (!InputValidate.IsValidHexNumber(e.KeyChar))
					{
						e.Handled = true;
					}
				}
				else
				{
					if (!InputValidate.IsValidUnsignedNumber(e.KeyChar))
					{
						e.Handled = true;
					}
				}
			}
		}

		private void CheatListView_AfterLabelEdit(object sender, LabelEditEventArgs e)
		{
			if (e.Label == null) //If no change
			{
				return;
			}

			string Str = e.Label;
			Global.CheatList[e.Item].Name = Str;
		}

		private void restoreWindowSizeToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Size = new Size(defaultWidth, defaultHeight);
			Global.Config.CheatsNameIndex = 0;
			Global.Config.CheatsAddressIndex = 1;
			Global.Config.CheatsValueIndex = 2;
			Global.Config.CheatsCompareIndex = 3;
			Global.Config.CheatsOnIndex = 4;
			Global.Config.CheatsDomainIndex = 5;

			Global.Config.Cheats_ValuesAsHex = true;

			ColumnPositionSet();
			CheatListView.Columns[0].Width = defaultNameWidth;
			CheatListView.Columns[1].Width = defaultAddressWidth;
			CheatListView.Columns[2].Width = defaultValueWidth;
			CheatListView.Columns[3].Width = defaultCompareWidth;
			CheatListView.Columns[4].Width = defaultOnWidth;
			CheatListView.Columns[5].Width = defaultDomainWidth;
		}

		private void exitToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Close();
		}

		private void appendFileToolStripMenuItem_Click(object sender, EventArgs e)
		{
			var file = GetFileFromUser();
			if (file != null)
			{
				LoadCheatFile(file.FullName, true);
			}
			DisplayCheatsList();
			Changes();
		}

		private void fileToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			if (!Global.CheatList.Changes)
			{
				saveToolStripMenuItem.Enabled = false;
			}
			else
			{
				saveToolStripMenuItem.Enabled = true;
			}
		}

		private void CheatsOnOffLoadToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.DisableCheatsOnLoad ^= true;
		}

		public void DisableAllCheats()
		{
			if (Global.CheatList.Any())
			{
				Global.OSD.AddMessage("All cheats disabled.");
			}

			Global.CheatList.DisableAll();

			MemoryPulse.Clear();
			CheatListView.Refresh();
			UpdateNumberOfCheats();
		}

		public void RemoveAllCheats()
		{
			Global.CheatList.Clear();
			MemoryPulse.Clear();
			if (!IsHandleCreated || IsDisposed) return;
			CheatListView.Refresh();
			UpdateNumberOfCheats();
			Global.OSD.AddMessage("All cheats removed");
		}

		private void disableAllCheatsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			DisableAllCheats();
		}

		private void disableAllCheatsToolStripMenuItem1_Click(object sender, EventArgs e)
		{
			DisableAllCheats();
		}

		private void toggleToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Toggle();
		}

		private void removeSelectedToolStripMenuItem_Click(object sender, EventArgs e)
		{
			RemoveCheat();
		}

		private void autoloadDialogToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.AutoLoadCheats ^= true;
		}

		private void LoadCheatFileByGameToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.LoadCheatFileByGame ^= true;
		}

		private void saveCheatsOnCloseToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.CheatsAutoSaveOnClose ^= true;
		}

		private void ColumnReorder(object sender, ColumnReorderedEventArgs e)
		{
			ColumnHeader header = e.Header;

			int lowIndex;
			int highIndex;
			int changeIndex;
			if (e.NewDisplayIndex > e.OldDisplayIndex)
			{
				changeIndex = -1;
				highIndex = e.NewDisplayIndex;
				lowIndex = e.OldDisplayIndex;
			}
			else
			{
				changeIndex = 1;
				highIndex = e.OldDisplayIndex;
				lowIndex = e.NewDisplayIndex;
			}


			if (Global.Config.CheatsNameIndex >= lowIndex && Global.Config.CheatsNameIndex <= highIndex)
			{
				Global.Config.CheatsNameIndex += changeIndex;
			}
			if (Global.Config.CheatsAddressIndex >= lowIndex && Global.Config.CheatsAddressIndex <= highIndex)
			{
				Global.Config.CheatsAddressIndex += changeIndex;
			}

			if (Global.Config.CheatsValueIndex >= lowIndex && Global.Config.CheatsValueIndex <= highIndex)
			{
				Global.Config.CheatsValueIndex += changeIndex;
			}

			if (Global.Config.CheatsCompareIndex >= lowIndex && Global.Config.CheatsCompareIndex <= highIndex)
			{
				Global.Config.CheatsCompareIndex += changeIndex;
			}

			if (Global.Config.CheatsDomainIndex >= lowIndex && Global.Config.CheatsDomainIndex <= highIndex)
			{
				Global.Config.CheatsDomainIndex += changeIndex;
			}

			if (Global.Config.CheatsOnIndex >= lowIndex && Global.Config.CheatsOnIndex <= highIndex)
			{
				Global.Config.CheatsOnIndex += changeIndex;
			}

			if (header.Text == "Name")
			{
				Global.Config.CheatsNameIndex = e.NewDisplayIndex;
			}
			else if (header.Text == "Address")
			{
				Global.Config.CheatsAddressIndex = e.NewDisplayIndex;
			}
			else if (header.Text == "Value")
			{
				Global.Config.CheatsValueIndex = e.NewDisplayIndex;
			}
			else if (header.Text == "Compare")
			{
				Global.Config.CheatsCompareIndex = e.NewDisplayIndex;
			}
			else if (header.Text == "On")
			{
				Global.Config.CheatsOnIndex = e.NewDisplayIndex;
			}
			else if (header.Text == "Domain")
			{
				Global.Config.CheatsDomainIndex = e.NewDisplayIndex;
			}
		}

		private void ColumnPositionSet()
		{
			List<ColumnHeader> columnHeaders = new List<ColumnHeader>();
			
			for (int i = 0; i < CheatListView.Columns.Count; i++)
			{
				columnHeaders.Add(CheatListView.Columns[i]);
			}

			CheatListView.Columns.Clear();

			List<KeyValuePair<int, string>> columnSettings = new List<KeyValuePair<int, string>>
				{
					new KeyValuePair<int, string>(Global.Config.RamWatchAddressIndex, "Name"),
					new KeyValuePair<int, string>(Global.Config.RamWatchAddressIndex, "Address"),
					new KeyValuePair<int, string>(Global.Config.RamWatchAddressIndex, "Value"),
					new KeyValuePair<int, string>(Global.Config.RamWatchAddressIndex, "Compare"),
					new KeyValuePair<int, string>(Global.Config.RamWatchAddressIndex, "Domain"),
					new KeyValuePair<int, string>(Global.Config.RamWatchAddressIndex, "On")
				};

			columnSettings = columnSettings.OrderBy(s => s.Key).ToList();

			foreach (KeyValuePair<int, string> t in columnSettings)
			{
				foreach (ColumnHeader t1 in columnHeaders)
				{
					if (t.Value == t1.Text)
					{
						CheatListView.Columns.Add(t1);
					}
				}
			}
		}

		private void Cheats_DragEnter(object sender, DragEventArgs e)
		{
			e.Effect = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
		}

		private void Cheats_DragDrop(object sender, DragEventArgs e)
		{
			string[] filePaths = (string[])e.Data.GetData(DataFormats.FileDrop);
			if (Path.GetExtension(filePaths[0]) == (".cht"))
			{
				LoadCheatFile(filePaths[0], false);
				DisplayCheatsList();
			}
		}

		private void cheatsToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
		{

			ListView.SelectedIndexCollection indexes = CheatListView.SelectedIndices;
			if (indexes.Count > 0)
			{
				moveDownToolStripMenuItem.Enabled = false;
				moveUpToolStripMenuItem.Enabled = false;
				removeCheatToolStripMenuItem.Enabled = false;
				duplicateToolStripMenuItem.Enabled = false;
			}
			else
			{
				moveDownToolStripMenuItem.Enabled = true;
				moveUpToolStripMenuItem.Enabled = true;
				removeCheatToolStripMenuItem.Enabled = true;
				duplicateToolStripMenuItem.Enabled = true;
			}
		}

		private void selectAllToolStripMenuItem_Click(object sender, EventArgs e)
		{
			for (int x = 0; x < Global.CheatList.Count; x++)
			{
				CheatListView.SelectItem(x, true);
			}
		}

		private void CheatListView_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Delete && !e.Control && !e.Alt && !e.Shift)
			{
				RemoveCheat();
			}
			else if (e.KeyCode == Keys.A && e.Control && !e.Alt && !e.Shift) //Select All
			{
				for (int x = 0; x < Global.CheatList.Count; x++)
				{
					CheatListView.SelectItem(x, true);
				}
			}
		}

		private void SetDomainSelection(string domainStr)
		{
			//Counts should always be the same, but just in case, let's check
			int max;
			if (Global.Emulator.MemoryDomains.Count < DomainComboBox.Items.Count)
			{
				max = Global.Emulator.MemoryDomains.Count;
			}
			else
			{
				max = DomainComboBox.Items.Count;
			}

			for (int x = 0; x < max; x++)
			{
				if (domainStr == DomainComboBox.Items[x].ToString())
				{
					DomainComboBox.SelectedIndex = x;
				}
			}
		}

		private void CompareBox_KeyPress(object sender, KeyPressEventArgs e)
		{
			if (e.KeyChar == '\b' || e.KeyChar == 22 || e.KeyChar == 1 || e.KeyChar == 3)
			{
				return;
			}
			else if (!InputValidate.IsValidHexNumber(e.KeyChar))
			{
				e.Handled = true;
			}
		}

		private void openGameGenieEncoderDecoderToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.MainForm.LoadGameGenieEC();
		}

		private void toolStripButton1_Click(object sender, EventArgs e)
		{
			Global.MainForm.LoadGameGenieEC();
		}

		private void showValuesAsHexToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.Cheats_ValuesAsHex ^= true;
			DoValuesAsHexOption();
		}

		private void DoValuesAsHexOption()
		{
			ValueBox.Text = "";
			CompareBox.Text = "";

			if (Global.Config.Cheats_ValuesAsHex)
			{
				label5.Visible = true;
				label7.Visible = true;
				ValueBox.MaxLength = 2;
				CompareBox.MaxLength = 2;
			}
			else
			{
				label5.Visible = false;
				label7.Visible = false;
				ValueBox.MaxLength = 3;
				CompareBox.MaxLength = 3;
			}

			CheatListView.Refresh();
		}
	}
}
