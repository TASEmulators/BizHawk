using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Globalization;

namespace BizHawk.MultiClient
{
	/// <summary>
	/// A winform designed to display ram address values of the user's choice
	/// </summary>
	public partial class RamWatch : Form
	{
		//TODO: 
		//When receiving a watch from a different domain, should something be done?
		//when sorting, "Prev as Change" option not taken into account
		//A GUI interface for setting the x,y coordinates of the ram watch display
		//Allow each watch to be on or off screen, and on its own x,y

		int defaultWidth;     //For saving the default size of the dialog, so the user can restore if desired
		int defaultHeight;
		int defaultAddressWidth;
		int defaultValueWidth;
		int defaultPrevWidth;
		int defaultChangeWidth;
		int defaultDiffWidth;
		int NotesWidth;

		string systemID = "NULL";
		MemoryDomain Domain = new MemoryDomain("NULL", 1, Endian.Little, addr => 0, (a, v) => { });
		List<Watch> Watches = new List<Watch>();
		string currentFile = "";
		bool changes = false;
		List<ToolStripMenuItem> domainMenuItems = new List<ToolStripMenuItem>();
		string addressFormatStr = "{0:X4}  ";

		string sortedCol;
		bool sortReverse;

		public void Restart()
		{
			if ((!this.IsHandleCreated || this.IsDisposed) && !Global.Config.DisplayRamWatch)
			{
				return;
			}

			if (currentFile.Length > 0)
			{
				LoadWatchFile(currentFile, false);
			}
			else
			{
				NewWatchList(true);
			}
		}

		public List<Watch> GetRamWatchList()
		{
			List<Watch> w = new List<Watch>();
			for (int x = 0; x < Watches.Count; x++)
			{
				w.Add(new Watch(Watches[x]));
			}
			return w;
		}

		public void DisplayWatchList()
		{
			WatchListView.ItemCount = Watches.Count;
		}

		public void UpdateValues()
		{
			if ((!this.IsHandleCreated || this.IsDisposed) && !Global.Config.DisplayRamWatch)
			{
				return;
			}

			for (int x = 0; x < Watches.Count; x++)
			{
				Watches[x].PeekAddress();
			}

			if (Global.Config.DisplayRamWatch)
			{
				for (int x = 0; x < Watches.Count; x++)
				{
					bool alert = Global.CheatList.IsActiveCheat(Domain, Watches[x].Address);
					Global.OSD.AddGUIText(Watches[x].ToString(),
						Global.Config.DispRamWatchx, (Global.Config.DispRamWatchy + (x * 14)), alert, Color.Black, Color.White, 0);
				}
			}

			if (!this.IsHandleCreated || this.IsDisposed) return;

			WatchListView.BlazingFast = true;
			WatchListView.Refresh();
			WatchListView.BlazingFast = false;

			
		}

		public void AddWatch(Watch w)
		{
			Watches.Add(w);
			Changes();
			UpdateValues();
			DisplayWatchList();
		}

		private void LoadConfigSettings()
		{
			ColumnPositionSet();

			defaultWidth = Size.Width;     //Save these first so that the user can restore to its original size
			defaultHeight = Size.Height;
			defaultAddressWidth = WatchListView.Columns[Global.Config.RamWatchAddressIndex].Width;
			defaultValueWidth = WatchListView.Columns[Global.Config.RamWatchValueIndex].Width;
			defaultPrevWidth = WatchListView.Columns[Global.Config.RamWatchPrevIndex].Width;
			defaultChangeWidth = WatchListView.Columns[Global.Config.RamWatchChangeIndex].Width;
			defaultDiffWidth = WatchListView.Columns[Global.Config.RamWatchDiffIndex].Width;
			NotesWidth = WatchListView.Columns[Global.Config.RamWatchNotesIndex].Width;


			if (Global.Config.RamWatchSaveWindowPosition && Global.Config.RamWatchWndx >= 0 && Global.Config.RamWatchWndy >= 0)
				Location = new Point(Global.Config.RamWatchWndx, Global.Config.RamWatchWndy);

			if (Global.Config.RamWatchWidth >= 0 && Global.Config.RamWatchHeight >= 0)
			{
				Size = new System.Drawing.Size(Global.Config.RamWatchWidth, Global.Config.RamWatchHeight);
			}
			SetPrevColumn(Global.Config.RamWatchShowPrevColumn);
			SetChangesColumn(Global.Config.RamWatchShowChangeColumn);
			SetDiffColumn(Global.Config.RamWatchShowDiffColumn);
			if (Global.Config.RamWatchAddressWidth > 0)
				WatchListView.Columns[Global.Config.RamWatchAddressIndex].Width = Global.Config.RamWatchAddressWidth;
			if (Global.Config.RamWatchValueWidth > 0)
				WatchListView.Columns[Global.Config.RamWatchValueIndex].Width = Global.Config.RamWatchValueWidth;
			if (Global.Config.RamWatchPrevWidth > 0)
				WatchListView.Columns[Global.Config.RamWatchPrevIndex].Width = Global.Config.RamWatchPrevWidth;
			if (Global.Config.RamWatchChangeWidth > 0)
				WatchListView.Columns[Global.Config.RamWatchChangeIndex].Width = Global.Config.RamWatchChangeWidth;
			if (Global.Config.RamWatchDiffWidth > 0)
				WatchListView.Columns[Global.Config.RamWatchDiffIndex].Width = Global.Config.RamWatchDiffWidth;
			if (Global.Config.RamWatchNotesWidth > 0)
				WatchListView.Columns[Global.Config.RamWatchNotesIndex].Width = Global.Config.RamWatchNotesWidth;
		}

		public void SaveConfigSettings()
		{
			ColumnPositionSet();
			Global.Config.RamWatchAddressWidth = WatchListView.Columns[Global.Config.RamWatchAddressIndex].Width;
			Global.Config.RamWatchValueWidth = WatchListView.Columns[Global.Config.RamWatchValueIndex].Width;
			Global.Config.RamWatchPrevWidth = WatchListView.Columns[Global.Config.RamWatchPrevIndex].Width;
			Global.Config.RamWatchChangeWidth = WatchListView.Columns[Global.Config.RamWatchChangeIndex].Width;
			Global.Config.RamWatchDiffWidth = WatchListView.Columns[Global.Config.RamWatchDiffIndex].Width;
			Global.Config.RamWatchNotesWidth = WatchListView.Columns[Global.Config.RamWatchNotesIndex].Width;

			Global.Config.RamWatchWndx = this.Location.X;
			Global.Config.RamWatchWndy = this.Location.Y;
			Global.Config.RamWatchWidth = this.Right - this.Left;
			Global.Config.RamWatchHeight = this.Bottom - this.Top;
		}

		public RamWatch()
		{
			InitializeComponent();
			WatchListView.QueryItemText += new QueryItemTextHandler(WatchListView_QueryItemText);
			WatchListView.QueryItemBkColor += new QueryItemBkColorHandler(WatchListView_QueryItemBkColor);
			WatchListView.VirtualMode = true;
			Closing += (o, e) => SaveConfigSettings();
			sortReverse = false;
			sortedCol = "";
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			if (!AskSave())
				e.Cancel = true;
			base.OnClosing(e);
		}
		
		private void WatchListView_QueryItemBkColor(int index, int column, ref Color color)
		{
			if (index >= Watches.Count)
			{
				return;
			}

			if (column == 0)
			{
				if (Watches[index].Type == Watch.TYPE.SEPARATOR)
				{
					color = this.BackColor;
				}
				if (Global.CheatList.IsActiveCheat(Domain, Watches[index].Address))
				{
					color = Color.LightCyan;
				}
			}
		}

		void WatchListView_QueryItemText(int index, int column, out string text)
		{
			text = "";

			if (Watches[index].Type == Watch.TYPE.SEPARATOR || index >= Watches.Count)
			{
				return;
			}

			if (column == 0)    //Address
			{
				text = Watches[index].Address.ToString(addressFormatStr);
			}
			if (column == 1) //Value
			{
				text = Watches[index].ValueString;
			}
			if (column == 2) //Prev
			{
				switch(Global.Config.RamWatchPrev_Type)
				{
					case 1:
						text = Watches[index].PrevString;
						break;
					case 2:
						text = Watches[index].LastChangeString;
						break;
				}
			}
			if (column == 3) //Change Counts
			{
				text = Watches[index].Changecount.ToString();
			}
			if (column == 4) //Diff
			{
				switch(Global.Config.RamWatchPrev_Type)
				{
					case 1:
						text = Watches[index].DiffToString(Watches[index].DiffPrev);
						break;
					case 2:
						text = Watches[index].DiffToString(Watches[index].DiffLastChange);
						break;
				}
			}
			if (column == 5) //Notes
			{
				text = Watches[index].Notes;
			}
		}

		public bool AskSave()
		{
			if (Global.Config.SupressAskSave) //User has elected to not be nagged
			{
				return true;
			}

			if (changes)
			{
				Global.Sound.StopSound();
				DialogResult result = MessageBox.Show("Save Changes?", "Ram Watch", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button3);
				Global.Sound.StartSound();
				if (result == DialogResult.Yes)
				{
					if (string.Compare(currentFile, "") == 0)
					{
						SaveAs();
					}
					else
						SaveWatchFile(currentFile);
					return true;
				}
				else if (result == DialogResult.No)
					return true;
				else if (result == DialogResult.Cancel)
					return false;
			}
			return true;
		}

		public void LoadWatchFromRecent(string file)
		{
			bool z = true;
			if (changes) z = AskSave();

			if (z)
			{
				bool r = LoadWatchFile(file, false);
				if (!r)
				{
					DialogResult result = MessageBox.Show("Could not open " + file + "\nRemove from list?", "File not found", MessageBoxButtons.YesNo, MessageBoxIcon.Error);
					if (result == DialogResult.Yes)
						Global.Config.RecentWatches.Remove(file);
				}
				DisplayWatchList();
				changes = false;
			}
		}

		private void NewWatchList(bool suppressAsk)
		{
			bool result = true;
			if (changes) result = AskSave();

			if (result == true || suppressAsk)
			{
				Watches.Clear();
				DisplayWatchList();
				UpdateWatchCount();
				currentFile = "";
				changes = false;
				MessageLabel.Text = "";
				sortReverse = false;
				sortedCol = "";
			}
		}

		private bool SaveWatchFile(string path)
		{
			return WatchCommon.SaveWchFile(path, Domain.Name, Watches);
		}

		private void UpdateWatchCount()
		{
			int count = 0;
			foreach (Watch w in Watches)
			{
				if (!(w.Type == Watch.TYPE.SEPARATOR))
				{
					count++;
				}
			}

			WatchCountLabel.Text = count.ToString() + (count == 1 ? " watch" : " watches");
		}

		public bool LoadWatchFile(string path, bool append)
		{
			string domain = "";
			bool result = WatchCommon.LoadWatchFile(path, append, Watches, out domain);

			if (result)
			{
				foreach (Watch w in Watches)
				{
					InitializeAddress(w);
				}
				if (!append)
				{
					currentFile = path;
				}
				changes = false;
				MessageLabel.Text = Path.GetFileNameWithoutExtension(path);
				UpdateWatchCount();
				Global.Config.RecentWatches.Add(path);
				SetMemoryDomain(WatchCommon.GetDomainPos(domain));
				return true;

			}
			else
				return false;
		}

		private Point GetPromptPoint()
		{
			Point p = new Point(WatchListView.Location.X, WatchListView.Location.Y);
			Point q = new Point();
			q = PointToScreen(p);
			return q;
		}

		private void AddNewWatch()
		{

			RamWatchNewWatch r = new RamWatchNewWatch();
			r.location = GetPromptPoint();
			r.SetEndian(Domain.Endian);
			Global.Sound.StopSound();
			r.ShowDialog();
			Global.Sound.StartSound();
			if (r.userSelected == true)
			{
				InitializeAddress(r.watch);
				Watches.Add(r.watch);
				Changes();
				UpdateWatchCount();
				DisplayWatchList();
			}
		}

		private void InitializeAddress(Watch w)
		{
			w.Domain = Domain;
			w.PeekAddress();
			w.Prev = w.Value;
			w.Original = w.Value;
			w.LastChange = w.Value;
			w.LastSearch = w.Value;
			w.Changecount = 0;
		}

		void Changes()
		{
			changes = true;
			MessageLabel.Text = Path.GetFileName(currentFile) + " *";
		}

		void EditWatchObject(int pos)
		{
			RamWatchNewWatch r = new RamWatchNewWatch();
			r.location = GetPromptPoint();
			r.SetToEditWatch(Watches[pos], "Edit Watch");
			Global.Sound.StopSound();
			r.ShowDialog();
			Global.Sound.StartSound();

			if (r.userSelected == true)
			{
				Changes();
				Watches[pos] = r.watch;
				DisplayWatchList();
			}
		}

		void EditWatch()
		{
			ListView.SelectedIndexCollection indexes = WatchListView.SelectedIndices;
			if (indexes.Count > 0)
				EditWatchObject(indexes[0]);
			UpdateValues();
		}

		void RemoveWatch()
		{
			Changes();
			ListView.SelectedIndexCollection indexes = WatchListView.SelectedIndices;
			if (indexes.Count > 0)
			{
				foreach (int index in indexes)
				{
					Watches.Remove(Watches[indexes[0]]); //index[0] used since each iteration will make this the correct list index
				}
				indexes.Clear();
				DisplayWatchList();
			}
			UpdateValues();
			UpdateWatchCount();
		}

		void DuplicateWatch()
		{
			ListView.SelectedIndexCollection indexes = WatchListView.SelectedIndices;
			if (indexes.Count > 0)
			{
				RamWatchNewWatch r = new RamWatchNewWatch();
				r.location = GetPromptPoint();
				int x = indexes[0];
				r.SetToEditWatch(Watches[x], "Duplicate Watch");

				Global.Sound.StopSound();
				r.ShowDialog();
				Global.Sound.StartSound();

				if (r.userSelected == true)
				{
					InitializeAddress(r.watch);
					Changes();
					Watches.Add(r.watch);
					DisplayWatchList();
				}
			}
			UpdateValues();
			UpdateWatchCount();
		}

		void MoveUp()
		{
			if (WatchListView.SelectedIndices.Count == 0)
				return;
			ListView.SelectedIndexCollection indexes = WatchListView.SelectedIndices;
			if (indexes[0] == 0)
				return;
			Watch temp = new Watch();
			if (indexes.Count == 0) return;
			foreach (int index in indexes)
			{
				temp = Watches[index];
				Watches.Remove(Watches[index]);
				Watches.Insert(index - 1, temp);

				//Note: here it will get flagged many times redundantly potentially, 
				//but this avoids it being flagged falsely when the user did not select an index
				Changes();
			}
			List<int> i = new List<int>();
			for (int z = 0; z < indexes.Count; z++)
			{
				i.Add(indexes[z] - 1);
			}

			WatchListView.SelectedIndices.Clear();
			for (int z = 0; z < i.Count; z++)
			{
				WatchListView.SelectItem(i[z], true);
			}

			DisplayWatchList();
		}

		void MoveDown()
		{
			ListView.SelectedIndexCollection indexes = WatchListView.SelectedIndices;
			Watch temp = new Watch();
			if (indexes.Count == 0) return;
			foreach (int index in indexes)
			{
				temp = Watches[index];

				if (index < Watches.Count - 1)
				{

					Watches.Remove(Watches[index]);
					Watches.Insert(index + 1, temp);

				}

				//Note: here it will get flagged many times redundantly potnetially, 
				//but this avoids it being flagged falsely when the user did not select an index
				Changes();
			}

			List<int> i = new List<int>();
			for (int z = 0; z < indexes.Count; z++)
				i.Add(indexes[z] + 1);

			WatchListView.SelectedIndices.Clear();
			for (int z = 0; z < i.Count; z++)
				WatchListView.SelectItem(i[z], true);

			DisplayWatchList();
		}

		private void exitToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (!AskSave())
				return;

			this.Close();
		}

		private void newListToolStripMenuItem_Click(object sender, EventArgs e)
		{
			NewWatchList(false);
		}

		private FileInfo GetFileFromUser()
		{
			var ofd = new OpenFileDialog();
			if (currentFile.Length > 0)
				ofd.FileName = Path.GetFileNameWithoutExtension(currentFile);
			ofd.InitialDirectory = PathManager.MakeAbsolutePath(Global.Config.WatchPath, "");
			ofd.Filter = "Watch Files (*.wch)|*.wch|All Files|*.*";
			ofd.RestoreDirectory = true;

			Global.Sound.StopSound();
			var result = ofd.ShowDialog();
			Global.Sound.StartSound();
			if (result != DialogResult.OK)
				return null;
			var file = new FileInfo(ofd.FileName);
			return file;
		}

		private void OpenWatchFile()
		{
			var file = GetFileFromUser();
			if (file != null)
			{
				bool r = true;
				if (changes) r = AskSave();
				if (r)
				{
					LoadWatchFile(file.FullName, false);
					DisplayWatchList();
				}
			}
		}

		private void openToolStripMenuItem_Click(object sender, EventArgs e)
		{
			OpenWatchFile();
		}

		private void saveToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (string.Compare(currentFile, "") == 0)
			{
				SaveAs();
			}
			else if (changes)
			{
				SaveWatchFile(currentFile);
				MessageLabel.Text = Path.GetFileName(currentFile) + " saved.";
			}
		}

		private void SaveAs()
		{
			var file = WatchCommon.GetSaveFileFromUser(currentFile);
			if (file != null)
			{
				SaveWatchFile(file.FullName);
				currentFile = file.FullName;
				MessageLabel.Text = Path.GetFileName(currentFile) + " saved.";
				Global.Config.RecentWatches.Add(file.FullName);
			}
		}

		private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			SaveAs();
		}

		private void appendFileToolStripMenuItem_Click(object sender, EventArgs e)
		{
			var file = GetFileFromUser();
			if (file != null)
				LoadWatchFile(file.FullName, true);
			DisplayWatchList();
			Changes();
		}

		private void autoLoadToolStripMenuItem_Click(object sender, EventArgs e)
		{
			UpdateAutoLoadRamWatch();
		}

		private void newWatchToolStripMenuItem_Click(object sender, EventArgs e)
		{
			AddNewWatch();
		}

		private void editWatchToolStripMenuItem_Click(object sender, EventArgs e)
		{
			EditWatch();
		}

		private void removeWatchToolStripMenuItem_Click(object sender, EventArgs e)
		{
			RemoveWatch();
		}

		private void duplicateWatchToolStripMenuItem_Click(object sender, EventArgs e)
		{
			DuplicateWatch();
		}

		private void moveUpToolStripMenuItem_Click(object sender, EventArgs e)
		{
			MoveUp();
		}

		private void moveDownToolStripMenuItem_Click(object sender, EventArgs e)
		{
			MoveDown();
		}

		private void RamWatch_Load(object sender, EventArgs e)
		{
			LoadConfigSettings();
			SetMemoryDomainMenu();
		}

		private void filesToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			if (Global.Config.AutoLoadRamWatch == true)
				autoLoadToolStripMenuItem.Checked = true;
			else
				autoLoadToolStripMenuItem.Checked = false;

			if (!changes)
			{
				saveToolStripMenuItem.Enabled = false;
			}
			else
			{
				saveToolStripMenuItem.Enabled = true;
			}
		}

		private void UpdateAutoLoadRamWatch()
		{
			autoLoadToolStripMenuItem.Checked = Global.Config.AutoLoadRamWatch ^= true;
		}
		private void recentToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			//Clear out recent Roms list
			//repopulate it with an up to date list
			recentToolStripMenuItem.DropDownItems.Clear();

			if (Global.Config.RecentWatches.IsEmpty())
			{
				var none = new ToolStripMenuItem();
				none.Enabled = false;
				none.Text = "None";
				recentToolStripMenuItem.DropDownItems.Add(none);
			}
			else
			{
				for (int x = 0; x < Global.Config.RecentWatches.Length(); x++)
				{
					string path = Global.Config.RecentWatches.GetRecentFileByPosition(x);
					var item = new ToolStripMenuItem();
					item.Text = path;
					item.Click += (o, ev) => LoadWatchFromRecent(path);
					recentToolStripMenuItem.DropDownItems.Add(item);
				}
			}

			recentToolStripMenuItem.DropDownItems.Add("-");

			var clearitem = new ToolStripMenuItem();
			clearitem.Text = "&Clear";
			clearitem.Click += (o, ev) => Global.Config.RecentWatches.Clear();
			recentToolStripMenuItem.DropDownItems.Add(clearitem);

			var auto = new ToolStripMenuItem();
			auto.Text = "&Auto-Load";
			auto.Click += (o, ev) => UpdateAutoLoadRamWatch();
			if (Global.Config.AutoLoadRamWatch == true)
				auto.Checked = true;
			else
				auto.Checked = false;
			recentToolStripMenuItem.DropDownItems.Add(auto);
		}

		private void WatchListView_AfterLabelEdit(object sender, LabelEditEventArgs e)
		{
			if (e.Label == null) //If no change
				return;
			string Str = e.Label.ToUpper().Trim();
			int index = e.Item;

			if (InputValidate.IsValidHexNumber(Str))
			{
				Watches[e.Item].Address = int.Parse(Str, NumberStyles.HexNumber);
				EditWatchObject(index);
			}
			else
			{
				MessageBox.Show("Invalid number!"); //TODO: More parameters and better message
				WatchListView.Items[index].Text = Watches[index].Address.ToString(); //TODO: Why doesn't the list view update to the new value? It won't until something else changes
			}
		}

		private void restoreWindowSizeToolStripMenuItem_Click(object sender, EventArgs e)
		{
			this.Size = new System.Drawing.Size(defaultWidth, defaultHeight);

			Global.Config.RamWatchAddressIndex = 0;
			Global.Config.RamWatchValueIndex = 1;
			Global.Config.RamWatchPrevIndex = 2;
			Global.Config.RamWatchChangeIndex = 3;
			Global.Config.RamWatchDiffIndex = 4;
			Global.Config.RamWatchNotesIndex = 5;
			ColumnPositionSet();

			showPreviousValueToolStripMenuItem.Checked = false;
			Global.Config.RamWatchShowPrevColumn = false;
			showChangeCountsToolStripMenuItem.Checked = true;
			Global.Config.RamWatchShowChangeColumn = true;
			Global.Config.RamWatchShowDiffColumn = false;
			WatchListView.Columns[0].Width = 60;
			WatchListView.Columns[1].Width = 59;
			WatchListView.Columns[2].Width = 0;
			WatchListView.Columns[3].Width = 55;
			WatchListView.Columns[4].Width = 0;
			WatchListView.Columns[5].Width = 128;
			Global.Config.DisplayRamWatch = false;
			Global.Config.RamWatchSaveWindowPosition = false;
		}

		private void newToolStripButton_Click(object sender, EventArgs e)
		{
			NewWatchList(false);
		}

		private void openToolStripButton_Click(object sender, EventArgs e)
		{
			OpenWatchFile();
		}

		private void saveToolStripButton_Click(object sender, EventArgs e)
		{
			if (changes && currentFile.Length > 0)
			{
				SaveWatchFile(currentFile);
			}
			else
			{
				SaveAs();
			}
		}

		private void InsertSeparator()
		{
			Changes();
			Watch w = new Watch();
			w.Type = Watch.TYPE.SEPARATOR;

			ListView.SelectedIndexCollection indexes = WatchListView.SelectedIndices;
			int x;
			if (indexes.Count > 0)
			{
				x = indexes[0];
				if (indexes[0] > 0)
					Watches.Insert(indexes[0], w);
			}
			else
			{
				Watches.Add(w);
			}
			DisplayWatchList();
		}

		private void cutToolStripButton_Click(object sender, EventArgs e)
		{
			RemoveWatch();
		}

		private void NewWatchStripButton1_Click(object sender, EventArgs e)
		{
			AddNewWatch();
		}

		private void MoveUpStripButton1_Click(object sender, EventArgs e)
		{
			MoveUp();
		}

		private void MoveDownStripButton1_Click(object sender, EventArgs e)
		{
			MoveDown();
		}

		private void EditWatchToolStripButton1_Click(object sender, EventArgs e)
		{
			EditWatch();
		}

		private void DuplicateWatchToolStripButton_Click(object sender, EventArgs e)
		{
			DuplicateWatch();
		}

		private void toolStripButton1_Click(object sender, EventArgs e)
		{
			InsertSeparator();
		}

		private void insertSeparatorToolStripMenuItem_Click(object sender, EventArgs e)
		{
			InsertSeparator();
		}

		private void PoketoolStripButton2_Click(object sender, EventArgs e)
		{
			PokeAddress();
		}

		private void PokeAddress()
		{
			ListView.SelectedIndexCollection indexes = WatchListView.SelectedIndices;
			Global.Sound.StopSound();
			RamPoke p = new RamPoke();
			Global.Sound.StartSound();
			if (indexes.Count > 0)
			{
				p.SetWatchObject(Watches[indexes[0]]);
			}
			p.location = GetPromptPoint();
			p.ShowDialog();
			UpdateValues();
			
		}

		private void pokeAddressToolStripMenuItem_Click(object sender, EventArgs e)
		{
			PokeAddress();
		}

		private void watchesToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			ListView.SelectedIndexCollection indexes = WatchListView.SelectedIndices;
			if (indexes.Count > 0)
			{
				editWatchToolStripMenuItem.Enabled = true;
				duplicateWatchToolStripMenuItem.Enabled = true;
				removeWatchToolStripMenuItem.Enabled = true;
				moveUpToolStripMenuItem.Enabled = true;
				moveDownToolStripMenuItem.Enabled = true;
				pokeAddressToolStripMenuItem.Enabled = true;
				freezeAddressToolStripMenuItem.Enabled = true;
			}
			else
			{
				editWatchToolStripMenuItem.Enabled = false;
				duplicateWatchToolStripMenuItem.Enabled = false;
				removeWatchToolStripMenuItem.Enabled = false;
				moveUpToolStripMenuItem.Enabled = false;
				moveDownToolStripMenuItem.Enabled = false;
				pokeAddressToolStripMenuItem.Enabled = false;
				freezeAddressToolStripMenuItem.Enabled = false;
			}
		}

		private void editToolStripMenuItem_Click(object sender, EventArgs e)
		{
			EditWatch();
		}

		private void removeToolStripMenuItem_Click(object sender, EventArgs e)
		{
			RemoveWatch();
		}

		private void duplicateToolStripMenuItem_Click(object sender, EventArgs e)
		{
			DuplicateWatch();
		}

		private void pokeToolStripMenuItem_Click(object sender, EventArgs e)
		{
			PokeAddress();
		}

		private void insertSeperatorToolStripMenuItem_Click(object sender, EventArgs e)
		{
			InsertSeparator();
		}

		private void moveUpToolStripMenuItem1_Click(object sender, EventArgs e)
		{
			MoveUp();
		}

		private void moveDownToolStripMenuItem1_Click(object sender, EventArgs e)
		{
			MoveDown();
		}

		private void contextMenuStrip1_Opening(object sender, CancelEventArgs e)
		{
			ListView.SelectedIndexCollection indexes = WatchListView.SelectedIndices;
			if (indexes.Count == 0)
			{
				contextMenuStrip1.Items[0].Visible = false;
				contextMenuStrip1.Items[1].Visible = false;
				contextMenuStrip1.Items[2].Visible = false;
				contextMenuStrip1.Items[3].Visible = false;
				contextMenuStrip1.Items[4].Visible = false;
				contextMenuStrip1.Items[5].Visible = false;
				contextMenuStrip1.Items[6].Visible = false;
				contextMenuStrip1.Items[8].Visible = false;
				contextMenuStrip1.Items[9].Visible = false;

			}
			else
			{
				for (int x = 0; x < contextMenuStrip1.Items.Count; x++)
					contextMenuStrip1.Items[x].Visible = true;

				if (indexes.Count == 1)
				{
					if (Global.CheatList.IsActiveCheat(Domain, Watches[indexes[0]].Address))
					{
						contextMenuStrip1.Items[4].Text = "&Unfreeze address";
						contextMenuStrip1.Items[4].Image =
							BizHawk.MultiClient.Properties.Resources.Unfreeze;
					}
					else
					{
						contextMenuStrip1.Items[4].Text = "&Freeze address";
						contextMenuStrip1.Items[4].Image =
							BizHawk.MultiClient.Properties.Resources.Freeze;
					}
				}
			}

			if (Global.Config.RamWatchShowChangeColumn)
				contextMenuStrip1.Items[11].Text = "Hide change counts";
			else
				contextMenuStrip1.Items[11].Text = "Show change counts";

			if (Global.Config.RamWatchShowPrevColumn)
				contextMenuStrip1.Items[12].Text = "Hide previous value";
			else
				contextMenuStrip1.Items[12].Text = "Show previous value";

			if (Global.Config.RamWatchShowDiffColumn)
				contextMenuStrip1.Items[13].Text = "Hide difference value";
			else
				contextMenuStrip1.Items[13].Text = "Show difference value";
		}

		private void WatchListView_MouseDoubleClick(object sender, MouseEventArgs e)
		{
			ListView.SelectedIndexCollection indexes = WatchListView.SelectedIndices;
			if (indexes.Count > 0)
			{
				EditWatch();
			}
		}

		private void RamWatch_DragDrop(object sender, DragEventArgs e)
		{
			string[] filePaths = (string[])e.Data.GetData(DataFormats.FileDrop);
			if (Path.GetExtension(filePaths[0]) == (".wch"))
			{
				LoadWatchFile(filePaths[0], false);
				DisplayWatchList();
			}
		}

		private void RamWatch_DragEnter(object sender, DragEventArgs e)
		{
			e.Effect = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None; string[] filePaths = (string[])e.Data.GetData(DataFormats.FileDrop);
		}

		private void ClearChangeCounts()
		{
			for (int x = 0; x < Watches.Count; x++)
				Watches[x].Changecount = 0;
			DisplayWatchList();
			MessageLabel.Text = "Change counts cleared";
		}

		private void ClearChangeCountstoolStripButton_Click(object sender, EventArgs e)
		{
			ClearChangeCounts();
		}

		private void clearChangeCountsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			ClearChangeCounts();
		}



		private void showChangeCountsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.RamWatchShowChangeColumn ^= true;
			SetChangesColumn(Global.Config.RamWatchShowChangeColumn);
		}

		private void SetDiffColumn(bool show)
		{
			Global.Config.RamWatchShowDiffColumn = show;
			diffToolStripMenuItem.Checked = show;
			if (show)
				WatchListView.Columns[Global.Config.RamWatchDiffIndex].Width = 59;
			else
				WatchListView.Columns[Global.Config.RamWatchDiffIndex].Width = 0;

		}
		private void SetChangesColumn(bool show)
		{
			Global.Config.RamWatchShowChangeColumn = show;
			showChangeCountsToolStripMenuItem.Checked = show;
			if (show)
				WatchListView.Columns[Global.Config.RamWatchChangeIndex].Width = 54;
			else
				WatchListView.Columns[Global.Config.RamWatchChangeIndex].Width = 0;
		}

		private void SetPrevColumn(bool show)
		{
			Global.Config.RamWatchShowPrevColumn = show;
			showPreviousValueToolStripMenuItem.Checked = show;
			if (show)
				WatchListView.Columns[Global.Config.RamWatchPrevIndex].Width = 59;
			else
				WatchListView.Columns[Global.Config.RamWatchPrevIndex].Width = 0;
		}

		private void showPreviousValueToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.RamWatchShowPrevColumn ^= true;
			SetPrevColumn(Global.Config.RamWatchShowPrevColumn);
		}

		private void optionsToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			displayWatchesOnScreenToolStripMenuItem.Checked = Global.Config.DisplayRamWatch;
			saveWindowPositionToolStripMenuItem.Checked = Global.Config.RamWatchSaveWindowPosition;
		}

		private void viewInHexEditorToolStripMenuItem_Click(object sender, EventArgs e)
		{
			ListView.SelectedIndexCollection indexes = WatchListView.SelectedIndices;
			if (indexes.Count > 0)
			{
				Global.MainForm.LoadHexEditor();
				Global.MainForm.HexEditor1.GoToAddress(Watches[indexes[0]].Address);
			}
		}

		private int GetNumDigits(Int32 i)
		{
			//if (i == 0) return 0;
			//if (i < 0x10) return 1;
			//if (i < 0x100) return 2;
			//if (i < 0x1000) return 3; //adelikat: commenting these out because I decided that regardless of domain, 4 digits should be the minimum
			if (i < 0x10000) return 4;
			if (i < 0x1000000) return 6;
			else return 8;
		}

		private void freezeToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (sender.ToString().Contains("Unfreeze"))
				UnfreezeAddress();
			else
				FreezeAddress();
		}

		private int WORDGetLowerByte(int value)
		{
			return value / 256;
		}

		private int WORDGetUpperByte(int value)
		{
			return value >> 2;
		}

		private void FreezeAddress()
		{
			ListView.SelectedIndexCollection indexes = WatchListView.SelectedIndices;
			if (indexes.Count > 0)
			{
				for (int i = 0; i < indexes.Count; i++)
				{
					switch (Watches[indexes[i]].Type)
					{
						case Watch.TYPE.BYTE:
							Cheat c = new Cheat("", Watches[indexes[i]].Address, (byte)Watches[indexes[i]].Value,
								true, Domain);
							Global.MainForm.Cheats1.AddCheat(c);
							break;
						case Watch.TYPE.WORD:
							{
								byte low = (byte)(Watches[indexes[i]].Value / 256);
								byte high = (byte)(Watches[indexes[i]].Value);
								int a1 = Watches[indexes[i]].Address;
								int a2 = Watches[indexes[i]].Address + 1;
								if (Watches[indexes[i]].BigEndian)
								{
									Cheat c1 = new Cheat("", a1, low, true, Domain);
									Cheat c2 = new Cheat("", a2, high, true, Domain);
									Global.MainForm.Cheats1.AddCheat(c1);
									Global.MainForm.Cheats1.AddCheat(c2);
								}
								else
								{
									Cheat c1 = new Cheat("", a1, high, true, Domain);
									Cheat c2 = new Cheat("", a2, low, true, Domain);
									Global.MainForm.Cheats1.AddCheat(c1);
									Global.MainForm.Cheats1.AddCheat(c2);
								}
							}
							break;
						case Watch.TYPE.DWORD:
							{
								byte HIWORDhigh = (byte)(Watches[indexes[i]].Value / 0x1000000);
								byte HIWORDlow = (byte)(Watches[indexes[i]].Value / 0x10000);
								byte LOWORDhigh = (byte)(Watches[indexes[i]].Value / 0x100);
								byte LOWORDlow = (byte)(Watches[indexes[i]].Value);
								int a1 = Watches[indexes[i]].Address;
								int a2 = Watches[indexes[i]].Address + 1;
								int a3 = Watches[indexes[i]].Address + 2;
								int a4 = Watches[indexes[i]].Address + 3;
								if (Watches[indexes[i]].BigEndian)
								{
									Cheat c1 = new Cheat("", a1, HIWORDhigh, true, Domain);
									Cheat c2 = new Cheat("", a2, HIWORDlow, true, Domain);
									Cheat c3 = new Cheat("", a3, LOWORDhigh, true, Domain);
									Cheat c4 = new Cheat("", a4, LOWORDlow, true, Domain);
									Global.MainForm.Cheats1.AddCheat(c1);
									Global.MainForm.Cheats1.AddCheat(c2);
									Global.MainForm.Cheats1.AddCheat(c3);
									Global.MainForm.Cheats1.AddCheat(c4);
								}
								else
								{
									Cheat c1 = new Cheat("", a1, LOWORDlow, true, Domain);
									Cheat c2 = new Cheat("", a2, LOWORDhigh, true, Domain);
									Cheat c3 = new Cheat("", a3, HIWORDlow, true, Domain);
									Cheat c4 = new Cheat("", a4, HIWORDhigh, true, Domain);
									Global.MainForm.Cheats1.AddCheat(c1);
									Global.MainForm.Cheats1.AddCheat(c2);
									Global.MainForm.Cheats1.AddCheat(c3);
									Global.MainForm.Cheats1.AddCheat(c4);
								}
							}
							break;
					}
				}
			}
		}

		private void UnfreezeAddress()
		{
			ListView.SelectedIndexCollection indexes = WatchListView.SelectedIndices;
			if (indexes.Count > 0)
			{
				for (int i = 0; i < indexes.Count; i++)
				{
					switch (Watches[indexes[i]].Type)
					{
						case Watch.TYPE.BYTE:
							Global.CheatList.Remove(Domain, Watches[indexes[i]].Address);
							break;
						case Watch.TYPE.WORD:
							Global.CheatList.Remove(Domain, Watches[indexes[i]].Address);
							Global.CheatList.Remove(Domain, Watches[indexes[i]].Address + 1);
							break;
						case Watch.TYPE.DWORD:
							Global.CheatList.Remove(Domain, Watches[indexes[i]].Address);
							Global.CheatList.Remove(Domain, Watches[indexes[i]].Address + 1);
							Global.CheatList.Remove(Domain, Watches[indexes[i]].Address + 2);
							Global.CheatList.Remove(Domain, Watches[indexes[i]].Address + 3);
							break;
					}
				}
			}
		}

		private void freezeAddressToolStripMenuItem_Click(object sender, EventArgs e)
		{
			FreezeAddress();
		}

		private void FreezetoolStripButton2_Click(object sender, EventArgs e)
		{
			FreezeAddress();
		}

		private void SetPlatformAndMemoryDomainLabel()
		{
			string memoryDomain = Domain.ToString();
			systemID = Global.Emulator.SystemId;
			MemDomainLabel.Text = systemID + " " + memoryDomain;
		}

		private void SetMemoryDomain(int pos)
		{
			if (pos < Global.Emulator.MemoryDomains.Count)  //Sanity check
			{
				Domain = Global.Emulator.MemoryDomains[pos];
			}
			addressFormatStr = "X" + GetNumDigits(Domain.Size - 1).ToString();
			SetPlatformAndMemoryDomainLabel();
			Update();
		}

		private void SetMemoryDomainMenu()
		{
			memoryDomainsToolStripMenuItem.DropDownItems.Clear();
			if (Global.Emulator.MemoryDomains.Count > 0)
			{
				for (int x = 0; x < Global.Emulator.MemoryDomains.Count; x++)
				{
					string str = Global.Emulator.MemoryDomains[x].ToString();
					var item = new ToolStripMenuItem();
					item.Text = str;
					{
						int z = x;
						item.Click += (o, ev) => SetMemoryDomain(z);
					}
					if (x == 0)
					{
						SetMemoryDomain(x);
					}
					memoryDomainsToolStripMenuItem.DropDownItems.Add(item);
					domainMenuItems.Add(item);
				}
			}
			else
				memoryDomainsToolStripMenuItem.Enabled = false;
		}

		private void CheckDomainMenuItems()
		{
			for (int x = 0; x < domainMenuItems.Count; x++)
			{
				if (Domain.Name == domainMenuItems[x].Text)
					domainMenuItems[x].Checked = true;
				else
					domainMenuItems[x].Checked = false;
			}
		}

		private void memoryDomainsToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			CheckDomainMenuItems();
		}

		private void ColumnReorder(object sender, ColumnReorderedEventArgs e)
		{
			ColumnHeader header = e.Header;

			int lowIndex = 0;
			int highIndex = 0;
			int changeIndex = 0;
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

			if (Global.Config.RamWatchAddressIndex >= lowIndex && Global.Config.RamWatchAddressIndex <= highIndex)
				Global.Config.RamWatchAddressIndex += changeIndex;
			if (Global.Config.RamWatchValueIndex >= lowIndex && Global.Config.RamWatchValueIndex <= highIndex)
				Global.Config.RamWatchValueIndex += changeIndex;
			if (Global.Config.RamWatchPrevIndex >= lowIndex && Global.Config.RamWatchPrevIndex <= highIndex)
				Global.Config.RamWatchPrevIndex += changeIndex;
			if (Global.Config.RamWatchChangeIndex >= lowIndex && Global.Config.RamWatchChangeIndex <= highIndex)
				Global.Config.RamWatchChangeIndex += changeIndex;
			if (Global.Config.RamWatchDiffIndex >= lowIndex && Global.Config.RamWatchDiffIndex <= highIndex)
				Global.Config.RamWatchDiffIndex += changeIndex;
			if (Global.Config.RamWatchNotesIndex >= lowIndex && Global.Config.RamWatchNotesIndex <= highIndex)
				Global.Config.RamWatchNotesIndex += changeIndex;

			if (header.Text == "Address")
				Global.Config.RamWatchAddressIndex = e.NewDisplayIndex;
			else if (header.Text == "Value")
				Global.Config.RamWatchValueIndex = e.NewDisplayIndex;
			else if (header.Text == "Prev")
				Global.Config.RamWatchPrevIndex = e.NewDisplayIndex;
			else if (header.Text == "Changes")
				Global.Config.RamWatchChangeIndex = e.NewDisplayIndex;
			else if (header.Text == "Diff")
				Global.Config.RamWatchDiffIndex = e.NewDisplayIndex;
			else if (header.Text == "Notes")
				Global.Config.RamWatchNotesIndex = e.NewDisplayIndex;
		}

		private void ColumnPositionSet()
		{
			List<ColumnHeader> columnHeaders = new List<ColumnHeader>();
			for (int i = 0; i < WatchListView.Columns.Count; i++)
			{
				columnHeaders.Add(WatchListView.Columns[i]);
			}

			WatchListView.Columns.Clear();

			List<KeyValuePair<int, string>> columnSettings = new List<KeyValuePair<int, string>>();
			columnSettings.Add(new KeyValuePair<int,string>(Global.Config.RamWatchAddressIndex, "Address"));
			columnSettings.Add(new KeyValuePair<int, string>(Global.Config.RamWatchValueIndex, "Value"));
			columnSettings.Add(new KeyValuePair<int, string>(Global.Config.RamWatchPrevIndex, "Prev"));
			columnSettings.Add(new KeyValuePair<int, string>(Global.Config.RamWatchChangeIndex, "Changes"));
			columnSettings.Add(new KeyValuePair<int, string>(Global.Config.RamWatchDiffIndex, "Diff"));
			columnSettings.Add(new KeyValuePair<int, string>(Global.Config.RamWatchNotesIndex, "Notes"));

			columnSettings = columnSettings.OrderBy(s => s.Key).ToList();
		

			for (int i = 0; i < columnSettings.Count; i++)
			{
				for (int j = 0; j < columnHeaders.Count; j++)
				{
					if (columnSettings[i].Value == columnHeaders[j].Text)
					{
						WatchListView.Columns.Add(columnHeaders[j]);
					}
				}
			}
		}

		private void OrderColumn(int columnToOrder)
		{
			string columnName = WatchListView.Columns[columnToOrder].Text;
			if (sortedCol.CompareTo(columnName) != 0)
				sortReverse = false;
			Watches.Sort((x, y) => x.CompareTo(y, columnName, Global.Config.RamWatchPrev_Type == 1 ? Watch.PREVDEF.LASTFRAME : Watch.PREVDEF.LASTCHANGE) * (sortReverse ? -1 : 1));
			sortedCol = columnName;
			sortReverse = !(sortReverse);
			WatchListView.Refresh();
		}

		private void WatchListView_ColumnClick(object sender, ColumnClickEventArgs e)
		{
			OrderColumn(e.Column);
		}

		private void saveWindowPositionToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.RamWatchSaveWindowPosition ^= true;
		}

		private void RamWatch_Enter(object sender, EventArgs e)
		{
			WatchListView.Focus();
		}

		private void selectAllToolStripMenuItem_Click(object sender, EventArgs e)
		{
			SelectAll();
		}

		private void SelectAll()
		{
			for (int x = 0; x < Watches.Count; x++)
				WatchListView.SelectItem(x, true);
		}

		private void RamWatch_Activated(object sender, EventArgs e)
		{
			WatchListView.Refresh();
		}

		private void displayWatchesOnScreenToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.DisplayRamWatch ^= true;

			if (!Global.Config.DisplayRamWatch)
				Global.OSD.ClearGUIText();
			else
				UpdateValues();
		}

		private void WatchListView_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Delete && !e.Control && !e.Alt && !e.Shift)
			{
				RemoveWatch();
			}
			else if (e.KeyCode == Keys.A && e.Control && !e.Alt && !e.Shift) //Select All
			{
				for (int x = 0; x < Watches.Count; x++)
				{
					WatchListView.SelectItem(x, true);
				}
			}
		}

		private void showPreviousValueToolStripMenuItem_Click_1(object sender, EventArgs e)
		{
			Global.Config.RamWatchShowPrevColumn ^= true;
			SetPrevColumn(Global.Config.RamWatchShowPrevColumn);
		}

		private void showChangeCountsToolStripMenuItem_Click_1(object sender, EventArgs e)
		{
			Global.Config.RamWatchShowChangeColumn ^= true;
			SetChangesColumn(Global.Config.RamWatchShowChangeColumn);
		}

		private void diffToolStripMenuItem_Click(object sender, EventArgs e)
		{
			ShowDifference();
		}

		private void viewToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			showPreviousValueToolStripMenuItem.Checked = Global.Config.RamWatchShowPrevColumn;
			showChangeCountsToolStripMenuItem.Checked = Global.Config.RamWatchShowChangeColumn;
			diffToolStripMenuItem.Checked = Global.Config.RamWatchShowDiffColumn;
		}

		private void showDifferenceToolStripMenuItem_Click(object sender, EventArgs e)
		{
			ShowDifference();
		}

		private void ShowDifference()
		{
			Global.Config.RamWatchShowDiffColumn ^= true;
			SetDiffColumn(Global.Config.RamWatchShowDiffColumn);
		}

		private void previousFrameToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.RamWatchPrev_Type = 1;
			WatchListView.Refresh();
		}

		private void lastChangeToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.RamWatchPrev_Type = 2;
			WatchListView.Refresh();
		}

		private void definePreviousValueAsToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			switch (Global.Config.RamWatchPrev_Type)
			{
				case 1:
					previousFrameToolStripMenuItem.Checked = true;
					lastChangeToolStripMenuItem.Checked = false;
					break;
				case 2:
					previousFrameToolStripMenuItem.Checked = false;
					lastChangeToolStripMenuItem.Checked = true;
					break;
			}
		}
	}
}