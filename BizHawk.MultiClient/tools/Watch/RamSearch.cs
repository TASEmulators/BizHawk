using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Globalization;

namespace BizHawk.MultiClient
{
	/// <summary>
	/// A winform designed to search through ram values
	/// </summary>
	public partial class RamSearch : Form
	{
		//TODO:
		//Go To Address (Ctrl+G) feature
		//Multiple undo levels (List<List<string>> UndoLists)

		private string systemID = "NULL";
		private List<Watch_Legacy> Searches = new List<Watch_Legacy>();
		private HistoryCollection SearchHistory = new HistoryCollection(enabled:true);
		private bool IsAWeededList = false; //For deciding whether the weeded list is relevant (0 size could mean all were removed in a legit preview
		private readonly List<ToolStripMenuItem> domainMenuItems = new List<ToolStripMenuItem>();
		private MemoryDomain Domain = new MemoryDomain("NULL", 1, Endian.Little, addr => 0, (a, v) => { });

		public enum SCompareTo { PREV, VALUE, ADDRESS, CHANGES };
		public enum SOperator { LESS, GREATER, LESSEQUAL, GREATEREQUAL, EQUAL, NOTEQUAL, DIFFBY };
		public enum SSigned { SIGNED, UNSIGNED, HEX };

		//Reset window position item
		private int defaultWidth;       //For saving the default size of the dialog, so the user can restore if desired
		private int defaultHeight;
		private int defaultAddressWidth;
		private int defaultValueWidth;
		private int defaultPrevWidth;
		private int defaultChangesWidth;
		private string currentFile = "";
		private string addressFormatStr = "{0:X4}  ";
		private bool sortReverse;
		private string sortedCol;
		private bool forcePreviewClear = false;

		public void SaveConfigSettings()
		{
			ColumnPositionSet();
			Global.Config.RamSearchAddressWidth = SearchListView.Columns[Global.Config.RamSearchAddressIndex].Width;
			Global.Config.RamSearchValueWidth = SearchListView.Columns[Global.Config.RamSearchValueIndex].Width;
			Global.Config.RamSearchPrevWidth = SearchListView.Columns[Global.Config.RamSearchPrevIndex].Width;
			Global.Config.RamSearchChangesWidth = SearchListView.Columns[Global.Config.RamSearchChangesIndex].Width;

			Global.Config.RamSearchWndx = Location.X;
			Global.Config.RamSearchWndy = Location.Y;
			Global.Config.RamSearchWidth = Right - Left;
			Global.Config.RamSearchHeight = Bottom - Top;
		}

		public RamSearch()
		{
			SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			SetStyle(ControlStyles.UserPaint, true);
			SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
			InitializeComponent();
			Closing += (o, e) => SaveConfigSettings();
		}

		public void UpdateValues()
		{
			if (!IsHandleCreated || IsDisposed) return;

			if (Searches.Count > 8)
			{
				SearchListView.BlazingFast = true;
			}

			sortReverse = false;
			sortedCol = "";

			if (!Global.Config.RamSearchFastMode)
			{
				for (int x = Searches.Count - 1; x >= 0; x--)
				{
					Searches[x].PeekAddress();
				}
			}

			if (AutoSearchCheckBox.Checked)
			{
				DoSearch();
			}
			else if (Global.Config.RamSearchPreviewMode)
			{
				DoPreview();
			}

			SearchListView.Refresh();
			SearchListView.BlazingFast = false;
		}

		private void RamSearch_Load(object sender, EventArgs e)
		{
			LoadConfigSettings();
			SetMemoryDomainMenu();
		}

		private void SetEndian()
		{
			if (Domain.Endian == Endian.Big)
			{
				SetBigEndian();
			}
			else
			{
				SetLittleEndian();
			}
		}

		private void LoadConfigSettings()
		{
			ColumnPositionSet();

			defaultWidth = Size.Width;     //Save these first so that the user can restore to its original size
			defaultHeight = Size.Height;
			defaultAddressWidth = SearchListView.Columns[Global.Config.RamSearchAddressIndex].Width;
			defaultValueWidth = SearchListView.Columns[Global.Config.RamSearchValueIndex].Width;
			defaultPrevWidth = SearchListView.Columns[Global.Config.RamSearchPrevIndex].Width;
			defaultChangesWidth = SearchListView.Columns[Global.Config.RamSearchChangesIndex].Width;

			SetEndian();

			if (Global.Config.RamSearchSaveWindowPosition && Global.Config.RamSearchWndx >= 0 && Global.Config.RamSearchWndy >= 0)
				Location = new Point(Global.Config.RamSearchWndx, Global.Config.RamSearchWndy);

			if (Global.Config.RamSearchWidth >= 0 && Global.Config.RamSearchHeight >= 0)
			{
				Size = new Size(Global.Config.RamSearchWidth, Global.Config.RamSearchHeight);
			}

			if (Global.Config.RamSearchAddressWidth > 0)
				SearchListView.Columns[Global.Config.RamSearchAddressIndex].Width = Global.Config.RamSearchAddressWidth;
			if (Global.Config.RamSearchValueWidth > 0)
				SearchListView.Columns[Global.Config.RamSearchValueIndex].Width = Global.Config.RamSearchValueWidth;
			if (Global.Config.RamSearchPrevWidth > 0)
				SearchListView.Columns[Global.Config.RamSearchPrevIndex].Width = Global.Config.RamSearchPrevWidth;
			if (Global.Config.RamSearchChangesWidth > 0)
				SearchListView.Columns[Global.Config.RamSearchChangesIndex].Width = Global.Config.RamSearchChangesWidth;
		}

		private void SetMemoryDomainMenu()
		{
			memoryDomainsToolStripMenuItem.DropDownItems.Clear();
			if (Global.Emulator.MemoryDomains.Count > 0)
			{
				for (int x = 0; x < Global.Emulator.MemoryDomains.Count; x++)
				{
					string str = Global.Emulator.MemoryDomains[x].ToString();
					var item = new ToolStripMenuItem { Text = str };
					{
						int z = x;
						item.Click += (o, ev) => SetMemoryDomainNew(z);
					}
					if (x == 0)
					{
						SetMemoryDomainNew(x);
					}
					memoryDomainsToolStripMenuItem.DropDownItems.Add(item);
					domainMenuItems.Add(item);
				}
			}
			else
				memoryDomainsToolStripMenuItem.Enabled = false;
		}

		public void Restart()
		{
			if (!IsHandleCreated || IsDisposed) return;
			SetMemoryDomainMenu();  //Calls Start New Search
		}

		private void SetMemoryDomainNew(int pos)
		{
			SetMemoryDomain(pos);
			SetEndian();
			StartNewSearch();
		}

		private void SetMemoryDomain(int pos)
		{
			if (pos < Global.Emulator.MemoryDomains.Count)  //Sanity check
			{
				Domain = Global.Emulator.MemoryDomains[pos];
			}
			SetPlatformAndMemoryDomainLabel();
			addressFormatStr = "X" + GetNumDigits(Domain.Size - 1).ToString();
		}

		private void SetTotal()
		{
			int x = Searches.Count;
			string str;
			if (x == 1)
				str = " address";
			else
				str = " addresses";
			TotalSearchLabel.Text = String.Format("{0:n0}", x) + str;
		}

		private void OpenSearchFile()
		{
			var file = GetFileFromUser();
			if (file != null)
			{
				LoadSearchFile(file.FullName, false, Searches);
				DisplaySearchList();
			}
		}

		private void openToolStripMenuItem_Click(object sender, EventArgs e)
		{
			OpenSearchFile();
		}

		private void openToolStripButton_Click(object sender, EventArgs e)
		{
			OpenSearchFile();
		}

		private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			SaveAs();
		}

		private void exitToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Hide();
		}

		private void SpecificValueRadio_CheckedChanged(object sender, EventArgs e)
		{
			if (SpecificValueRadio.Checked)
			{
				if (SpecificValueBox.Text == "") SpecificValueBox.Text = "0";
				SpecificValueBox.Enabled = true;
				SpecificAddressBox.Enabled = false;
				NumberOfChangesBox.Enabled = false;
				SpecificValueBox.Focus();
				SpecificValueBox.SelectAll();
			}
			DoPreview();
		}

		private void PreviousValueRadio_CheckedChanged(object sender, EventArgs e)
		{
			if (PreviousValueRadio.Checked)
			{
				SpecificValueBox.Enabled = false;
				SpecificAddressBox.Enabled = false;
				NumberOfChangesBox.Enabled = false;
			}
			DoPreview();
		}

		private void SpecificAddressRadio_CheckedChanged(object sender, EventArgs e)
		{
			if (SpecificAddressRadio.Checked)
			{
				if (SpecificAddressBox.Text == "") SpecificAddressBox.Text = "0";
				SpecificValueBox.Enabled = false;
				SpecificAddressBox.Enabled = true;
				NumberOfChangesBox.Enabled = false;
				SpecificAddressBox.Focus();
				SpecificAddressBox.SelectAll();
			}
			DoPreview();
		}

		private void NumberOfChangesRadio_CheckedChanged(object sender, EventArgs e)
		{
			if (NumberOfChangesRadio.Checked)
			{
				if (NumberOfChangesBox.Text == "") NumberOfChangesBox.Text = "0";
				SpecificValueBox.Enabled = false;
				SpecificAddressBox.Enabled = false;
				NumberOfChangesBox.Enabled = true;
				NumberOfChangesBox.Focus();
				NumberOfChangesBox.SelectAll();
			}
		}

		private void DifferentByRadio_CheckedChanged(object sender, EventArgs e)
		{
			if (DifferentByRadio.Checked)
			{
				if (DifferentByBox.Text == "0") DifferentByBox.Text = "0";
				DifferentByBox.Enabled = true;
				DoPreview();
			}
			else
				DifferentByBox.Enabled = false;
			DifferentByBox.Focus();
			DifferentByBox.SelectAll();
		}

		private void AddToRamWatch()
		{
			ListView.SelectedIndexCollection indexes = SearchListView.SelectedIndices;

			if (indexes.Count > 0)
			{
				Global.MainForm.LoadRamWatch(true);
				for (int x = 0; x < indexes.Count; x++)
				{
					Global.MainForm.NewRamWatch1.AddOldWatch(Searches[indexes[x]]);
				}
			}
		}

		private void restoreOriginalWindowSizeToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Size = new Size(defaultWidth, defaultHeight);
			Global.Config.RamSearchAddressIndex = 0;
			Global.Config.RamSearchValueIndex = 1;
			Global.Config.RamSearchPrevIndex = 2;
			Global.Config.RamSearchChangesIndex = 3;
			ColumnPositionSet();

			SearchListView.Columns[0].Width = defaultAddressWidth;
			SearchListView.Columns[1].Width = defaultValueWidth;
			SearchListView.Columns[2].Width = defaultPrevWidth;
			SearchListView.Columns[3].Width = defaultChangesWidth;
		}

		private void NewSearchtoolStripButton_Click(object sender, EventArgs e)
		{
			StartNewSearch();
		}

		private Watch_Legacy.DISPTYPE GetDataType()
		{
			if (unsignedToolStripMenuItem.Checked)
			{
				return Watch_Legacy.DISPTYPE.UNSIGNED;
			}
			else if (signedToolStripMenuItem.Checked)
			{
				return Watch_Legacy.DISPTYPE.SIGNED;
			}
			else if (hexadecimalToolStripMenuItem.Checked)
			{
				return Watch_Legacy.DISPTYPE.HEX;
			}
			else
			{
				return Watch_Legacy.DISPTYPE.UNSIGNED;    //Just in case
			}
		}

		private Watch_Legacy.TYPE GetDataSize()
		{
			if (byteToolStripMenuItem.Checked)
			{
				return Watch_Legacy.TYPE.BYTE;
			}
			else if (bytesToolStripMenuItem.Checked)
			{
				return Watch_Legacy.TYPE.WORD;
			}
			else if (dWordToolStripMenuItem1.Checked)
			{
				return Watch_Legacy.TYPE.DWORD;
			}
			else
			{
				return Watch_Legacy.TYPE.BYTE;
			}
		}

		private bool GetBigEndian()
		{
			if (bigEndianToolStripMenuItem.Checked)
				return true;
			else
				return false;
		}

		private void StartNewSearch()
		{
			useUndoHistoryToolStripMenuItem.Checked = true;
			if (Global.Emulator.SystemId == "N64")
			{
				byteToolStripMenuItem.Checked = false;
				bytesToolStripMenuItem.Checked = false;
				dWordToolStripMenuItem1.Checked = true;
				useUndoHistoryToolStripMenuItem.Checked = false;
				Global.Config.RamSearchFastMode = true;
				
			}
			IsAWeededList = false;
			SearchHistory.Clear();
			Searches.Clear();
			SetPlatformAndMemoryDomainLabel();
			int count = 0;
			int divisor = 1;

			if (!includeMisalignedToolStripMenuItem.Checked)
			{
				switch (GetDataSize())
				{
					case Watch_Legacy.TYPE.WORD:
						divisor = 2;
						break;
					case Watch_Legacy.TYPE.DWORD:
						divisor = 4;
						break;
				}
			}

			for (int x = 0; x <= ((Domain.Size / divisor) - 1); x++)
			{
				Searches.Add(new Watch_Legacy());
				Searches[x].Address = count;
				Searches[x].Type = GetDataSize();
				Searches[x].BigEndian = GetBigEndian();
				Searches[x].Signed = GetDataType();
				Searches[x].Domain = Domain;
				Searches[x].PeekAddress();
				Searches[x].Prev = Searches[x].Value;
				Searches[x].Original = Searches[x].Value;
				Searches[x].LastChange = Searches[x].Value;
				Searches[x].LastSearch = Searches[x].Value;
				Searches[x].Changecount = 0;
				if (includeMisalignedToolStripMenuItem.Checked)
					count++;
				else
				{
					switch (GetDataSize())
					{
						case Watch_Legacy.TYPE.BYTE:
							count++;
							break;
						case Watch_Legacy.TYPE.WORD:
							count += 2;
							break;
						case Watch_Legacy.TYPE.DWORD:
							count += 4;
							break;
					}
				}

			}
			if (Global.Config.RamSearchAlwaysExcludeRamWatch)
				ExcludeRamWatchList();
			SetSpecificValueBoxMaxLength();
			MessageLabel.Text = "New search started";
			sortReverse = false;
			sortedCol = "";
			DisplaySearchList();
			SearchHistory = new HistoryCollection(Searches, useUndoHistoryToolStripMenuItem.Checked);
			UpdateUndoRedoToolItems();
		}

		private void DisplaySearchList()
		{
			SearchListView.ItemCount = Searches.Count;
			SetTotal();
		}

		private void newSearchToolStripMenuItem_Click(object sender, EventArgs e)
		{
			StartNewSearch();
		}

		private void SetPlatformAndMemoryDomainLabel()
		{
			string memoryDomain = Domain.ToString();
			systemID = Global.Emulator.SystemId;
			MemDomainLabel.Text = systemID + " " + memoryDomain;
		}

		private Point GetPromptPoint()
		{
			return PointToScreen(new Point(SearchListView.Location.X, SearchListView.Location.Y));
		}

		private void PokeAddress()
		{
			ListView.SelectedIndexCollection indexes = SearchListView.SelectedIndices;
			if (indexes.Count > 0)
			{
				Global.Sound.StopSound();
				var poke = new RamPoke();
				var watch = Watch.ConvertLegacyWatch(Searches[indexes[0]]);
				poke.SetWatch(new List<Watch> { watch });
				poke.InitialLocation = GetPromptPoint();
				poke.ShowDialog();
				UpdateValues();
				Global.Sound.StartSound();
			}
		}

		private void PoketoolStripButton1_Click(object sender, EventArgs e)
		{
			PokeAddress();
		}

		private string MakeAddressString(int num)
		{
			if (num == 1)
				return " 1 address";
			else if (num < 10)
				return " " + num.ToString() + " addresses";
			else
				return num.ToString() + " addresses";
		}

		private void RemoveAddresses()
		{
			ListView.SelectedIndexCollection indexes = SearchListView.SelectedIndices;
			if (indexes.Count > 0)
			{
				MessageLabel.Text = MakeAddressString(indexes.Count) + " removed";
				for (int x = 0; x < indexes.Count; x++)
				{
					Searches.Remove(Searches[indexes[x] - x]);
				}
				indexes.Clear();
				DisplaySearchList();

				SearchHistory.AddState(Searches);
				UpdateUndoRedoToolItems();
			}
		}

		private void cutToolStripButton_Click(object sender, EventArgs e)
		{
			RemoveAddresses();
		}

		private void UpdateUndoRedoToolItems()
		{
			UndotoolStripButton.Enabled = SearchHistory.CanUndo;
			RedotoolStripButton2.Enabled = SearchHistory.CanRedo;
		}

		private void DoUndo()
		{
			if (SearchHistory.CanUndo)
			{
				int oldVal = Searches.Count;
				Searches = SearchHistory.Undo();
				int newVal = Searches.Count;
				MessageLabel.Text = MakeAddressString(newVal - oldVal) + " restored";
				UpdateUndoRedoToolItems();
				DisplaySearchList();
			}
		}

		private void DoRedo()
		{
			if (SearchHistory.CanRedo)
			{
				int oldVal = Searches.Count;
				Searches = SearchHistory.Redo();
				int newVal = Searches.Count;
				MessageLabel.Text = MakeAddressString(newVal - oldVal) + " removed";
				UpdateUndoRedoToolItems();
				DisplaySearchList();
			}
		}

		private void SearchListView_QueryItemBkColor(int index, int column, ref Color color)
		{
			if (IsAWeededList && column == 0 && Searches[index].Deleted)
			{
				if (color == Color.Pink)
				{
					return;
				}
				else if (Global.CheatList.IsActiveCheat(Domain, Searches[index].Address))
				{
					if (forcePreviewClear)
					{
						color = Color.LightCyan;
					}
					else if (color == Color.Lavender)
					{
						return;
					}
					else
					{
						color = Color.Lavender;
					}
				}
				else
				{
					if (forcePreviewClear)
					{
						color = Color.White;
					}
					else if (color == Color.Pink)
					{
						return;
					}
					else
					{
						color = Color.Pink;
					}
				}
			}
			else if (Global.CheatList.IsActiveCheat(Domain, Searches[index].Address))
			{
				if (color == Color.LightCyan)
				{
					return;
				}
				else
				{
					color = Color.LightCyan;
				}
			}
			else
			{
				if (color == Color.White)
				{
					return;
				}
				else
				{
					color = Color.White;
				}
			}
		}

		private void SearchListView_QueryItemText(int index, int column, out string text)
		{
			if (column == 0)
			{
				text = Searches[index].Address.ToString(addressFormatStr);
			}
			else if (column == 1)
			{
				text = Searches[index].ValueString;
			}
			else if (column == 2)
			{
				switch (Global.Config.RamSearchPreviousAs)
				{
					case 0:
						text = Searches[index].LastSearchString;
						break;
					case 1:
						text = Searches[index].OriginalString;
						break;
					default:
					case 2:
						text = Searches[index].PrevString;
						break;
					case 3:
						text = Searches[index].LastChangeString;
						break;
				}
			}
			else if (column == 3)
			{
				text = Searches[index].Changecount.ToString();
			}
			else
			{
				text = "";
			}
		}

		private void ClearChangeCounts()
		{
			foreach (Watch_Legacy t in Searches)
			{
				t.Changecount = 0;
			}
			DisplaySearchList();
			MessageLabel.Text = "Change counts cleared";

			SearchHistory.AddState(Searches);
			UpdateUndoRedoToolItems();
		}

		private void ClearChangeCountstoolStripButton_Click(object sender, EventArgs e)
		{
			ClearChangeCounts();
		}

		private void UndotoolStripButton_Click_1(object sender, EventArgs e)
		{
			DoUndo();
		}

		private void DoPreview()
		{
			if (Global.Config.RamSearchPreviewMode)
			{
				forcePreviewClear = false;
				GenerateWeedOutList();
			}
		}

		private void TrimWeededList()
		{
			Searches = Searches.Where(x => x.Deleted == false).ToList();
		}

		private void DoSearch()
		{
			if (Global.Config.RamSearchFastMode)
			{
				for (int x = Searches.Count - 1; x >= 0; x--)
				{
					Searches[x].PeekAddress();
				}
			}

			if (GenerateWeedOutList())
			{
				MessageLabel.Text = MakeAddressString(Searches.Count(x => x.Deleted)) + " removed";
				TrimWeededList();
				UpdateLastSearch();
				DisplaySearchList();
				SearchHistory.AddState(Searches);
				UpdateUndoRedoToolItems();
			}
			else
			{
				MessageLabel.Text = "Search failed.";
			}
		}

		private void toolStripButton1_Click(object sender, EventArgs e)
		{
			DoSearch();
		}

		private SCompareTo GetCompareTo()
		{
			if (PreviousValueRadio.Checked)
				return SCompareTo.PREV;
			if (SpecificValueRadio.Checked)
				return SCompareTo.VALUE;
			if (SpecificAddressRadio.Checked)
				return SCompareTo.ADDRESS;
			if (NumberOfChangesRadio.Checked)
				return SCompareTo.CHANGES;

			return SCompareTo.PREV; //Just in case
		}

		private SOperator GetOperator()
		{
			if (LessThanRadio.Checked)
				return SOperator.LESS;
			if (GreaterThanRadio.Checked)
				return SOperator.GREATER;
			if (LessThanOrEqualToRadio.Checked)
				return SOperator.LESSEQUAL;
			if (GreaterThanOrEqualToRadio.Checked)
				return SOperator.GREATEREQUAL;
			if (EqualToRadio.Checked)
				return SOperator.EQUAL;
			if (NotEqualToRadio.Checked)
				return SOperator.NOTEQUAL;
			if (DifferentByRadio.Checked)
				return SOperator.DIFFBY;

			return SOperator.LESS; //Just in case
		}

		private bool GenerateWeedOutList()
		{
			//Switch based on user criteria
			//Generate search list
			//Use search list to generate a list of flagged address (for displaying pink)
			IsAWeededList = true;
			switch (GetCompareTo())
			{
				case SCompareTo.PREV:
					return DoPreviousValue();
				case SCompareTo.VALUE:
					return DoSpecificValue();
				case SCompareTo.ADDRESS:
					return DoSpecificAddress();
				case SCompareTo.CHANGES:
					return DoNumberOfChanges();
				default:
					return false;
			}
		}

		private int GetPreviousValue(int pos)
		{
			switch (Global.Config.RamSearchPreviousAs)
			{
				case 0:
					return Searches[pos].LastSearch;
				case 1:
					return Searches[pos].Original;
				default:
				case 2:
					return Searches[pos].Prev;
				case 3:
					return Searches[pos].LastChange;
			}
		}

		private bool DoPreviousValue()
		{
			switch (GetOperator())
			{
				case SOperator.LESS:
					for (int x = 0; x < Searches.Count; x++)
					{
						int previous = GetPreviousValue(x);
						if (Searches[x].Signed == Watch_Legacy.DISPTYPE.SIGNED)
						{
							if (Searches[x].SignedVal(Searches[x].Value) < Searches[x].SignedVal(previous))
							{
								Searches[x].Deleted = false;
							}
							else
							{
								Searches[x].Deleted = true;
							}
						}
						else
						{
							if (Searches[x].UnsignedVal(Searches[x].Value) < Searches[x].UnsignedVal(previous))
							{
								Searches[x].Deleted = false;
							}
							else
							{
								Searches[x].Deleted = true;
							}
						}
					}
					break;
				case SOperator.GREATER:
					for (int x = 0; x < Searches.Count; x++)
					{
						int previous = GetPreviousValue(x);
						if (Searches[x].Signed == Watch_Legacy.DISPTYPE.SIGNED)
						{
							if (Searches[x].SignedVal(Searches[x].Value) > Searches[x].SignedVal(previous))
							{
								Searches[x].Deleted = false;
							}
							else
							{
								Searches[x].Deleted = true;
							}
						}
						else
						{
							if (Searches[x].UnsignedVal(Searches[x].Value) > Searches[x].UnsignedVal(previous))
							{
								Searches[x].Deleted = false;
							}
							else
							{
								Searches[x].Deleted = true;
							}
						}
					}
					break;
				case SOperator.LESSEQUAL:
					for (int x = 0; x < Searches.Count; x++)
					{
						int previous = GetPreviousValue(x);
						if (Searches[x].Signed == Watch_Legacy.DISPTYPE.SIGNED)
						{
							if (Searches[x].SignedVal(Searches[x].Value) <= Searches[x].SignedVal(previous))
							{
								Searches[x].Deleted = false;
							}
							else
							{
								Searches[x].Deleted = true;
							}
						}
						else
						{
							if (Searches[x].UnsignedVal(Searches[x].Value) <= Searches[x].UnsignedVal(previous))
							{
								Searches[x].Deleted = false;
							}
							else
							{
								Searches[x].Deleted = true;
							}
						}
					}
					break;
				case SOperator.GREATEREQUAL:
					for (int x = 0; x < Searches.Count; x++)
					{
						int previous = GetPreviousValue(x);
						if (Searches[x].Signed == Watch_Legacy.DISPTYPE.SIGNED)
						{
							if (Searches[x].SignedVal(Searches[x].Value) >= Searches[x].SignedVal(previous))
							{
								Searches[x].Deleted = false;
							}
							else
							{
								Searches[x].Deleted = true;
							}
						}
						else
						{
							if (Searches[x].UnsignedVal(Searches[x].Value) >= Searches[x].UnsignedVal(previous))
							{
								Searches[x].Deleted = false;
							}
							else
							{
								Searches[x].Deleted = true;
							}
						}
					}
					break;
				case SOperator.EQUAL:
					for (int x = 0; x < Searches.Count; x++)
					{
						int previous = GetPreviousValue(x);
						if (Searches[x].Signed == Watch_Legacy.DISPTYPE.SIGNED)
						{
							if (Searches[x].SignedVal(Searches[x].Value) == Searches[x].SignedVal(previous))
							{
								Searches[x].Deleted = false;
							}
							else
							{
								Searches[x].Deleted = true;
							}
						}
						else
						{
							if (Searches[x].UnsignedVal(Searches[x].Value) == Searches[x].UnsignedVal(previous))
							{
								Searches[x].Deleted = false;
							}
							else
							{
								Searches[x].Deleted = true;
							}
						}
					}
					break;
				case SOperator.NOTEQUAL:
					for (int x = 0; x < Searches.Count; x++)
					{
						int previous = GetPreviousValue(x);
						if (Searches[x].Signed == Watch_Legacy.DISPTYPE.SIGNED)
						{
							if (Searches[x].SignedVal(Searches[x].Value) != Searches[x].SignedVal(previous))
							{
								Searches[x].Deleted = false;
							}
							else
							{
								Searches[x].Deleted = true;
							}
						}
						else
						{
							if (Searches[x].UnsignedVal(Searches[x].Value) != Searches[x].UnsignedVal(previous))
							{
								Searches[x].Deleted = false;
							}
							else
							{
								Searches[x].Deleted = true;
							}
						}
					}
					break;
				case SOperator.DIFFBY:
					int diff = GetDifferentBy();
					if (diff < 0) return false;
					for (int x = 0; x < Searches.Count; x++)
					{
						int previous = GetPreviousValue(x);
						if (Searches[x].Signed == Watch_Legacy.DISPTYPE.SIGNED)
						{
							if (Searches[x].SignedVal(Searches[x].Value) == Searches[x].SignedVal(previous) + diff || Searches[x].SignedVal(Searches[x].Value) == Searches[x].SignedVal(previous) - diff)
							{
								Searches[x].Deleted = false;
							}
							else
							{
								Searches[x].Deleted = true;
							}
						}
						else
						{
							if (Searches[x].UnsignedVal(Searches[x].Value) == Searches[x].UnsignedVal(previous) + diff || Searches[x].UnsignedVal(Searches[x].Value) == Searches[x].UnsignedVal(previous) - diff)
							{
								Searches[x].Deleted = false;
							}
							else
							{
								Searches[x].Deleted = true;
							}
						}
					}
					break;
			}
			return true;
		}

		private void ValidateSpecificValue(int? value)
		{
			if (value == null)
			{
				MessageBox.Show("Missing or invalid value", "Invalid value", MessageBoxButtons.OK, MessageBoxIcon.Error);
				SpecificValueBox.Text = "0";
				SpecificValueBox.Focus();
				SpecificValueBox.SelectAll();
			}
		}
		private bool DoSpecificValue()
		{
			int? value = GetSpecificValue();
			ValidateSpecificValue(value);
			if (value == null)
				return false;
			switch (GetOperator())
			{
				case SOperator.LESS:
					foreach (Watch_Legacy t in Searches)
					{
						if (t.Signed == Watch_Legacy.DISPTYPE.SIGNED)
						{
							if (t.SignedVal(t.Value) < t.SignedVal((int)value))
							{
								t.Deleted = false;
							}
							else
							{
								t.Deleted = true;
							}
						}
						else
						{
							if (t.UnsignedVal(t.Value) < t.UnsignedVal((int)value))
							{
								t.Deleted = false;
							}
							else
							{
								t.Deleted = true;
							}
						}
					}
					break;
				case SOperator.GREATER:
					foreach (Watch_Legacy t in Searches)
					{
						if (t.Signed == Watch_Legacy.DISPTYPE.SIGNED)
						{
							if (t.SignedVal(t.Value) > t.SignedVal((int)value))
							{
								t.Deleted = false;
							}
							else
							{
								t.Deleted = true;
							}
						}
						else
						{
							if (t.UnsignedVal(t.Value) > t.UnsignedVal((int)value))
							{
								t.Deleted = false;
							}
							else
							{
								t.Deleted = true;
							}
						}
					}
					break;
				case SOperator.LESSEQUAL:
					foreach (Watch_Legacy t in Searches)
					{
						if (t.Signed == Watch_Legacy.DISPTYPE.SIGNED)
						{
							if (t.SignedVal(t.Value) <= t.SignedVal((int)value))
							{
								t.Deleted = false;
							}
							else
							{
								t.Deleted = true;
							}
						}
						else
						{
							if (t.UnsignedVal(t.Value) <= t.UnsignedVal((int)value))
							{
								t.Deleted = false;
							}
							else
							{
								t.Deleted = true;
							}
						}
					}
					break;
				case SOperator.GREATEREQUAL:
					foreach (Watch_Legacy t in Searches)
					{
						if (t.Signed == Watch_Legacy.DISPTYPE.SIGNED)
						{
							if (t.SignedVal(t.Value) >= t.SignedVal((int)value))
							{
								t.Deleted = false;
							}
							else
							{
								t.Deleted = true;
							}
						}
						else
						{
							if (t.UnsignedVal(t.Value) >= t.UnsignedVal((int)value))
							{
								t.Deleted = false;
							}
							else
							{
								t.Deleted = true;
							}
						}
					}
					break;
				case SOperator.EQUAL:
					foreach (Watch_Legacy t in Searches)
					{
						if (t.Signed == Watch_Legacy.DISPTYPE.SIGNED)
						{
							if (t.SignedVal(t.Value) == t.SignedVal((int)value))
							{
								t.Deleted = false;
							}
							else
							{
								t.Deleted = true;

							}
						}
						else
						{
							if (t.UnsignedVal(t.Value) == t.UnsignedVal((int)value))
							{
								t.Deleted = false;
							}
							else
							{
								t.Deleted = true;
							}
						}
					}
					break;
				case SOperator.NOTEQUAL:
					foreach (Watch_Legacy t in Searches)
					{
						if (t.Signed == Watch_Legacy.DISPTYPE.SIGNED)
						{
							if (t.SignedVal(t.Value) != t.SignedVal((int)value))
							{
								t.Deleted = false;
							}
							else
							{
								t.Deleted = true;
							}
						}
						else
						{
							if (t.UnsignedVal(t.Value) != t.UnsignedVal((int)value))
							{
								t.Deleted = false;
							}
							else
							{
								t.Deleted = true;
							}
						}
					}
					break;
				case SOperator.DIFFBY:
					int diff = GetDifferentBy();
					if (diff < 0) return false;
					foreach (Watch_Legacy t in Searches)
					{
						if (t.Signed == Watch_Legacy.DISPTYPE.SIGNED)
						{
							if (t.SignedVal(t.Value) == t.SignedVal((int)value) + diff || t.SignedVal(t.Value) == t.SignedVal((int)value) - diff)
							{
								t.Deleted = false;
							}
							else
							{
								t.Deleted = true;
							}
						}
						else
						{
							if (t.UnsignedVal(t.Value) == t.UnsignedVal((int)value) + diff || t.UnsignedVal(t.Value) == t.UnsignedVal((int)value) - diff)
							{
								t.Deleted = false;
							}
							else
							{
								t.Deleted = true;
							}
						}
					}
					break;
			}
			return true;
		}

		private int? GetSpecificValue()
		{
			if (SpecificValueBox.Text == "" || SpecificValueBox.Text == "-") return 0;
			bool i;
			switch (GetDataType())
			{
				case Watch_Legacy.DISPTYPE.UNSIGNED:
					i = InputValidate.IsValidUnsignedNumber(SpecificValueBox.Text);
					if (!i)
						return null;
					return (int)Int64.Parse(SpecificValueBox.Text); //Note: 64 to be safe since 4 byte values can be entered
				case Watch_Legacy.DISPTYPE.SIGNED:
					i = InputValidate.IsValidSignedNumber(SpecificValueBox.Text);
					if (!i)
						return null;
					int value = (int)Int64.Parse(SpecificValueBox.Text);
					switch (GetDataSize())
					{
						case Watch_Legacy.TYPE.BYTE:
							return (byte)value;
						case Watch_Legacy.TYPE.WORD:
							return (ushort)value;
						case Watch_Legacy.TYPE.DWORD:
							return (int)(uint)value;
					}
					return value;
				case Watch_Legacy.DISPTYPE.HEX:
					i = InputValidate.IsValidHexNumber(SpecificValueBox.Text);
					if (!i)
						return null;
					return (int)Int64.Parse(SpecificValueBox.Text, NumberStyles.HexNumber);
			}
			return null;
		}

		private int GetSpecificAddress()
		{
			if (SpecificAddressBox.Text == "") return 0;
			bool i = InputValidate.IsValidHexNumber(SpecificAddressBox.Text);
			if (!i) return -1;

			return int.Parse(SpecificAddressBox.Text, NumberStyles.HexNumber);
		}

		private int GetDifferentBy()
		{
			if (DifferentByBox.Text == "") return 0;
			bool i = InputValidate.IsValidUnsignedNumber(DifferentByBox.Text);
			if (!i)
			{
				MessageBox.Show("Missing or invalid Different By value", "Invalid value", MessageBoxButtons.OK, MessageBoxIcon.Error);
				DifferentByBox.Focus();
				DifferentByBox.SelectAll();
				return -1;
			}
			else
				return int.Parse(DifferentByBox.Text);
		}

		private bool DoSpecificAddress()
		{
			int address = GetSpecificAddress();
			if (address < 0)
			{
				MessageBox.Show("Missing or invalid address", "Invalid address", MessageBoxButtons.OK, MessageBoxIcon.Error);
				SpecificAddressBox.Focus();
				SpecificAddressBox.SelectAll();
				return false;
			}
			switch (GetOperator())
			{
				case SOperator.LESS:
					foreach (Watch_Legacy t in Searches)
					{
						if (t.Address < address)
						{
							t.Deleted = false;
						}
						else
						{
							t.Deleted = true;
						}
					}
					break;
				case SOperator.GREATER:
					foreach (Watch_Legacy t in Searches)
					{
						if (t.Address > address)
						{
							t.Deleted = false;
						}
						else
						{
							t.Deleted = true;
						}
					}
					break;
				case SOperator.LESSEQUAL:
					foreach (Watch_Legacy t in Searches)
					{
						if (t.Address <= address)
						{
							t.Deleted = false;
						}
						else
						{
							t.Deleted = true;
						}
					}
					break;
				case SOperator.GREATEREQUAL:
					foreach (Watch_Legacy t in Searches)
					{
						if (t.Address >= address)
						{
							t.Deleted = false;
						}
						else
						{
							t.Deleted = true;
						}
					}
					break;
				case SOperator.EQUAL:
					foreach (Watch_Legacy t in Searches)
					{
						if (t.Address == address)
						{
							t.Deleted = false;
						}
						else
						{
							t.Deleted = true;
						}
					}
					break;
				case SOperator.NOTEQUAL:
					foreach (Watch_Legacy t in Searches)
					{
						if (t.Address != address)
						{
							t.Deleted = false;
						}
						else
						{
							t.Deleted = true;
						}
					}
					break;
				case SOperator.DIFFBY:
					{
						int diff = GetDifferentBy();
						if (diff < 0) return false;
						foreach (Watch_Legacy t in Searches)
						{
							if (t.Address == address + diff || t.Address == address - diff)
							{
								t.Deleted = false;
							}
							else
							{
								t.Deleted = true;
							}
						}
					}
					break;
			}
			return true;
		}

		private int GetSpecificChanges()
		{
			if (NumberOfChangesBox.Text == "") return 0;
			bool i = InputValidate.IsValidUnsignedNumber(NumberOfChangesBox.Text);
			if (!i) return -1;

			return int.Parse(NumberOfChangesBox.Text);
		}

		private bool DoNumberOfChanges()
		{
			int changes = GetSpecificChanges();
			if (changes < 0)
			{
				MessageBox.Show("Missing or invalid number of changes", "Invalid number", MessageBoxButtons.OK, MessageBoxIcon.Error);
				NumberOfChangesBox.Focus();
				NumberOfChangesBox.SelectAll();
				return false;
			}
			switch (GetOperator())
			{
				case SOperator.LESS:
					foreach (Watch_Legacy t in Searches)
					{
						if (t.Changecount < changes)
						{
							t.Deleted = false;
						}
						else
						{
							t.Deleted = true;
						}
					}
					break;
				case SOperator.GREATER:
					foreach (Watch_Legacy t in Searches)
					{
						if (t.Changecount > changes)
						{
							t.Deleted = false;
						}
						else
						{
							t.Deleted = true;
						}
					}
					break;
				case SOperator.LESSEQUAL:
					foreach (Watch_Legacy t in Searches)
					{
						if (t.Changecount <= changes)
						{
							t.Deleted = false;
						}
						else
						{
							t.Deleted = true;
						}
					}
					break;
				case SOperator.GREATEREQUAL:
					foreach (Watch_Legacy t in Searches)
					{
						if (t.Changecount >= changes)
						{
							t.Deleted = false;
						}
						else
						{
							t.Deleted = true;
						}
					}
					break;
				case SOperator.EQUAL:
					foreach (Watch_Legacy t in Searches)
					{
						if (t.Changecount == changes)
						{
							t.Deleted = false;
						}
						else
						{
							t.Deleted = true;
						}
					}
					break;
				case SOperator.NOTEQUAL:
					foreach (Watch_Legacy t in Searches)
					{
						if (t.Changecount != changes)
						{
							t.Deleted = false;
						}
						else
						{
							t.Deleted = true;
						}
					}
					break;
				case SOperator.DIFFBY:
					int diff = GetDifferentBy();
					if (diff < 0) return false;
					foreach (Watch_Legacy t in Searches)
					{
						if (t.Address == changes + diff || t.Address == changes - diff)
						{
							t.Deleted = false;
						}
						else
						{
							t.Deleted = true;
						}
					}
					break;
			}
			return true;
		}

		private void ConvertListsDataType(Watch_Legacy.DISPTYPE s)
		{
			foreach (Watch_Legacy t in Searches)
			{
				t.Signed = s;
			}

			foreach (List<Watch_Legacy> state in SearchHistory.History)
			{
				foreach (Watch_Legacy watch in state)
				{
					watch.Signed = s;
				}
			}

			SetSpecificValueBoxMaxLength();
			sortReverse = false;
			sortedCol = "";
			DisplaySearchList();
		}

		private void ConvertListsDataSize(Watch_Legacy.TYPE s, bool bigendian)
		{
			ConvertDataSize(s, ref Searches);

			//TODO
			//for (int i = 0; i < SearchHistory.History.Count; i++)
			//{
			//    ConvertDataSize(s, bigendian, ref SearchHistory.History[i]);
			//}

			SetSpecificValueBoxMaxLength();
			sortReverse = false;
			sortedCol = "";
			DisplaySearchList();
		}

		private void ConvertDataSize(Watch_Legacy.TYPE s, ref List<Watch_Legacy> list)
		{
			List<Watch_Legacy> converted = new List<Watch_Legacy>();
			int divisor = 1;
			if (!includeMisalignedToolStripMenuItem.Checked)
			{
				switch (s)
				{
					case Watch_Legacy.TYPE.WORD:
						divisor = 2;
						break;
					case Watch_Legacy.TYPE.DWORD:
						divisor = 4;
						break;
				}
			}
			foreach (Watch_Legacy t in list)
				if (t.Address % divisor == 0)
				{
					int changes = t.Changecount;
					t.Type = s;
					t.BigEndian = GetBigEndian();
					t.PeekAddress();
					t.Prev = t.Value;
					t.Original = t.Value;
					t.LastChange = t.Value;
					t.LastSearch = t.Value;
					t.Changecount = changes;
					converted.Add(t);
				}
			list = converted;
		}

		private void unsignedToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Watch_Legacy specificValue = new Watch_Legacy();
			int? value = GetSpecificValue();
			ValidateSpecificValue(value);
			if (value != null) specificValue.Value = (int)value;
			specificValue.Signed = Watch_Legacy.DISPTYPE.UNSIGNED;
			specificValue.Type = GetDataSize();
			string converted = specificValue.ValueString;
			unsignedToolStripMenuItem.Checked = true;
			signedToolStripMenuItem.Checked = false;
			hexadecimalToolStripMenuItem.Checked = false;
			SpecificValueBox.Text = converted;
			ConvertListsDataType(Watch_Legacy.DISPTYPE.UNSIGNED);
		}

		private void signedToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Watch_Legacy specificValue = new Watch_Legacy();
			int? value = GetSpecificValue();
			ValidateSpecificValue(value);
			if (value != null) specificValue.Value = (int)value;
			specificValue.Signed = Watch_Legacy.DISPTYPE.SIGNED;
			specificValue.Type = GetDataSize();
			string converted = specificValue.ValueString;
			unsignedToolStripMenuItem.Checked = false;
			signedToolStripMenuItem.Checked = true;
			hexadecimalToolStripMenuItem.Checked = false;
			SpecificValueBox.Text = converted;
			ConvertListsDataType(Watch_Legacy.DISPTYPE.SIGNED);
		}

		private void hexadecimalToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Watch_Legacy specificValue = new Watch_Legacy();
			int? value = GetSpecificValue();
			ValidateSpecificValue(value);
			if (value != null) specificValue.Value = (int)value;
			specificValue.Signed = Watch_Legacy.DISPTYPE.HEX;
			specificValue.Type = GetDataSize();
			string converted = specificValue.ValueString;
			unsignedToolStripMenuItem.Checked = false;
			signedToolStripMenuItem.Checked = false;
			hexadecimalToolStripMenuItem.Checked = true;
			SpecificValueBox.Text = converted;
			ConvertListsDataType(Watch_Legacy.DISPTYPE.HEX);
		}

		private void SearchListView_MouseDoubleClick(object sender, MouseEventArgs e)
		{
			ListView.SelectedIndexCollection indexes = SearchListView.SelectedIndices;
			if (indexes.Count > 0)
			{
				AddToRamWatch();
			}
		}

		private void SetSpecificValueBoxMaxLength()
		{
			switch (GetDataType())
			{
				case Watch_Legacy.DISPTYPE.UNSIGNED:
					switch (GetDataSize())
					{
						case Watch_Legacy.TYPE.BYTE:
							SpecificValueBox.MaxLength = 3;
							break;
						case Watch_Legacy.TYPE.WORD:
							SpecificValueBox.MaxLength = 5;
							break;
						case Watch_Legacy.TYPE.DWORD:
							SpecificValueBox.MaxLength = 10;
							break;
						default:
							SpecificValueBox.MaxLength = 10;
							break;
					}
					break;
				case Watch_Legacy.DISPTYPE.SIGNED:
					switch (GetDataSize())
					{
						case Watch_Legacy.TYPE.BYTE:
							SpecificValueBox.MaxLength = 4;
							break;
						case Watch_Legacy.TYPE.WORD:
							SpecificValueBox.MaxLength = 6;
							break;
						case Watch_Legacy.TYPE.DWORD:
							SpecificValueBox.MaxLength = 11;
							break;
						default:
							SpecificValueBox.MaxLength = 11;
							break;
					}
					break;
				case Watch_Legacy.DISPTYPE.HEX:
					switch (GetDataSize())
					{
						case Watch_Legacy.TYPE.BYTE:
							SpecificValueBox.MaxLength = 2;
							break;
						case Watch_Legacy.TYPE.WORD:
							SpecificValueBox.MaxLength = 4;
							break;
						case Watch_Legacy.TYPE.DWORD:
							SpecificValueBox.MaxLength = 8;
							break;
						default:
							SpecificValueBox.MaxLength = 8;
							break;
					}
					break;
				default:
					SpecificValueBox.MaxLength = 11;
					break;
			}
		}

		private void byteToolStripMenuItem_Click(object sender, EventArgs e)
		{
			byteToolStripMenuItem.Checked = true;
			bytesToolStripMenuItem.Checked = false;
			dWordToolStripMenuItem1.Checked = false;
			ConvertListsDataSize(Watch_Legacy.TYPE.BYTE, GetBigEndian());
		}

		private void bytesToolStripMenuItem_Click(object sender, EventArgs e)
		{
			byteToolStripMenuItem.Checked = false;
			bytesToolStripMenuItem.Checked = true;
			dWordToolStripMenuItem1.Checked = false;
			ConvertListsDataSize(Watch_Legacy.TYPE.WORD, GetBigEndian());
		}

		private void dWordToolStripMenuItem1_Click(object sender, EventArgs e)
		{
			byteToolStripMenuItem.Checked = false;
			bytesToolStripMenuItem.Checked = false;
			dWordToolStripMenuItem1.Checked = true;
			ConvertListsDataSize(Watch_Legacy.TYPE.DWORD, GetBigEndian());
		}

		private void includeMisalignedToolStripMenuItem_Click_1(object sender, EventArgs e)
		{
			includeMisalignedToolStripMenuItem.Checked ^= true;
			if (!includeMisalignedToolStripMenuItem.Checked)
				ConvertListsDataSize(GetDataSize(), GetBigEndian());
		}

		private void SetLittleEndian()
		{
			bigEndianToolStripMenuItem.Checked = false;
			littleEndianToolStripMenuItem.Checked = true;
			ConvertListsDataSize(GetDataSize(), false);
		}

		private void SetBigEndian()
		{
			bigEndianToolStripMenuItem.Checked = true;
			littleEndianToolStripMenuItem.Checked = false;
			ConvertListsDataSize(GetDataSize(), true);
		}

		private void bigEndianToolStripMenuItem_Click(object sender, EventArgs e)
		{
			SetBigEndian();
		}

		private void littleEndianToolStripMenuItem_Click(object sender, EventArgs e)
		{
			SetLittleEndian();
		}

		private void AutoSearchCheckBox_CheckedChanged(object sender, EventArgs e)
		{
			if (AutoSearchCheckBox.Checked)
			{
				AutoSearchCheckBox.BackColor = Color.Pink;
			}
			else
			{
				AutoSearchCheckBox.BackColor = BackColor;
			}
		}

		private void SpecificValueBox_Leave(object sender, EventArgs e)
		{
			DoPreview();
		}

		private void SpecificAddressBox_Leave(object sender, EventArgs e)
		{
			DoPreview();
		}

		private void NumberOfChangesBox_Leave(object sender, EventArgs e)
		{
			DoPreview();
		}

		private void DifferentByBox_Leave(object sender, EventArgs e)
		{
			if (!InputValidate.IsValidUnsignedNumber(DifferentByBox.Text))  //Actually the only way this could happen is from putting dashes after the first character
			{
				DifferentByBox.Focus();
				DifferentByBox.SelectAll();
				ToolTip t = new ToolTip();
				t.Show("Must be a valid unsigned decimal value", DifferentByBox, 5000);
				return;
			}
			DoPreview();
		}

		private void SaveSearchFile(string path)
		{
			WatchCommon.SaveWchFile(path, Domain.Name, Searches);
		}

		public void SaveAs()
		{
			var file = WatchCommon.GetSaveFileFromUser(currentFile);
			if (file != null)
			{
				SaveSearchFile(file.FullName);
				currentFile = file.FullName;
				MessageLabel.Text = Path.GetFileName(currentFile) + " saved.";
				Global.Config.RecentSearches.Add(currentFile);
			}
		}

		private void LoadSearchFromRecent(string path)
		{
			if (!LoadSearchFile(path, false, Searches))
			{
				Global.Config.RecentSearches.HandleLoadError(path);
			}
			else
			{
				DisplaySearchList();
			}
		}

		bool LoadSearchFile(string path, bool append, List<Watch_Legacy> list)
		{
			string domain;
			bool result = WatchCommon.LoadWatchFile(path, append, list, out domain);

			if (result)
			{
				if (!append)
				{
					currentFile = path;
				}

				MessageLabel.Text = Path.GetFileNameWithoutExtension(path);
				SetTotal();
				Global.Config.RecentSearches.Add(path);
				SetMemoryDomain(WatchCommon.GetDomainPos(domain));
				return true;
			}
			else
			{
				return false;
			}
		}

		private void recentToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			recentToolStripMenuItem.DropDownItems.Clear();
			recentToolStripMenuItem.DropDownItems.AddRange(Global.Config.RecentSearches.GenerateRecentMenu(LoadSearchFromRecent));
		}

		private void appendFileToolStripMenuItem_Click(object sender, EventArgs e)
		{
			var file = GetFileFromUser();
			if (file != null)
				LoadSearchFile(file.FullName, true, Searches);
			DisplaySearchList();
		}

		private FileInfo GetFileFromUser()
		{
			var ofd = new OpenFileDialog();
			if (currentFile.Length > 0)
				ofd.FileName = Path.GetFileNameWithoutExtension(currentFile);
			ofd.InitialDirectory = PathManager.MakeAbsolutePath(Global.Config.PathEntries.WatchPath, null);
			ofd.Filter = "Watch Files (*.wch)|*.wch|All Files|*.*";
			ofd.RestoreDirectory = true;
			if (currentFile.Length > 0)
				ofd.FileName = Path.GetFileNameWithoutExtension(currentFile);
			Global.Sound.StopSound();
			var result = ofd.ShowDialog();
			Global.Sound.StartSound();
			if (result != DialogResult.OK)
				return null;
			var file = new FileInfo(ofd.FileName);
			return file;
		}

		private void saveWindowPositionToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.RamSearchSaveWindowPosition ^= true;
		}

		private void optionsToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			fastModeToolStripMenuItem.Checked = Global.Config.RamSearchFastMode;
			saveWindowPositionToolStripMenuItem.Checked = Global.Config.RamSearchSaveWindowPosition;
			previewModeToolStripMenuItem.Checked = Global.Config.RamSearchPreviewMode;
			alwaysExcludeRamSearchListToolStripMenuItem.Checked = Global.Config.RamSearchAlwaysExcludeRamWatch;
			autoloadDialogToolStripMenuItem.Checked = Global.Config.RecentSearches.AutoLoad;
		}

		private void searchToolStripMenuItem1_Click(object sender, EventArgs e)
		{
			DoSearch();
		}

		private void clearChangeCountsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			ClearChangeCounts();
		}

		private void undoToolStripMenuItem_Click_1(object sender, EventArgs e)
		{
			DoUndo();
		}

		private void removeSelectedToolStripMenuItem_Click(object sender, EventArgs e)
		{
			RemoveAddresses();
		}

		private void saveToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (string.Compare(currentFile, "") == 0)
				SaveAs();
			else
				SaveSearchFile(currentFile);
		}

		private void addSelectedToRamWatchToolStripMenuItem_Click(object sender, EventArgs e)
		{
			AddToRamWatch();
		}

		private void pokeAddressToolStripMenuItem_Click(object sender, EventArgs e)
		{
			PokeAddress();
		}

		private void searchToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			clearUndoHistoryToolStripMenuItem.Enabled = SearchHistory.HasHistory;
			searchToolStripMenuItem.Enabled = Searches.Any();
			undoToolStripMenuItem.Enabled = SearchHistory.CanUndo;
			redoToolStripMenuItem.Enabled = SearchHistory.CanRedo;

			ListView.SelectedIndexCollection indexes = SearchListView.SelectedIndices;

			if (indexes.Count == 0)
			{
				removeSelectedToolStripMenuItem.Enabled = false;
				addSelectedToRamWatchToolStripMenuItem.Enabled = false;
				pokeAddressToolStripMenuItem.Enabled = false;
			}
			else
			{
				removeSelectedToolStripMenuItem.Enabled = true;
				addSelectedToRamWatchToolStripMenuItem.Enabled = true;
				pokeAddressToolStripMenuItem.Enabled = true;
			}
		}

		private void sinceLastSearchToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.RamSearchPreviousAs = 0;
			sortReverse = false;
			sortedCol = "";
			DisplaySearchList();
		}

		private void originalValueToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.RamSearchPreviousAs = 1;
			sortReverse = false;
			sortedCol = "";
			DisplaySearchList();
		}

		private void sinceLastFrameToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.RamSearchPreviousAs = 2;
			sortReverse = false;
			sortedCol = "";
			DisplaySearchList();
		}

		private void sinceLastChangeToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.RamSearchPreviousAs = 3;
			sortReverse = false;
			sortedCol = "";
			DisplaySearchList();
		}

		private void definePreviousValueToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			switch (Global.Config.RamSearchPreviousAs)
			{
				case 0: //Since last Search
					sinceLastSearchToolStripMenuItem.Checked = true;
					originalValueToolStripMenuItem.Checked = false;
					sinceLastFrameToolStripMenuItem.Checked = false;
					sinceLastChangeToolStripMenuItem.Checked = false;
					break;
				case 1: //Original value (since Start new search)
					sinceLastSearchToolStripMenuItem.Checked = false;
					originalValueToolStripMenuItem.Checked = true;
					sinceLastFrameToolStripMenuItem.Checked = false;
					sinceLastChangeToolStripMenuItem.Checked = false;
					break;
				default:
				case 2: //Since last Frame
					sinceLastSearchToolStripMenuItem.Checked = false;
					originalValueToolStripMenuItem.Checked = false;
					sinceLastFrameToolStripMenuItem.Checked = true;
					sinceLastChangeToolStripMenuItem.Checked = false;
					break;
				case 3: //Since last Change
					sinceLastSearchToolStripMenuItem.Checked = false;
					originalValueToolStripMenuItem.Checked = false;
					sinceLastFrameToolStripMenuItem.Checked = false;
					sinceLastChangeToolStripMenuItem.Checked = true;
					break;
			}
		}

		private void LessThanRadio_CheckedChanged(object sender, EventArgs e)
		{
			if (!DifferentByRadio.Checked) DoPreview();
		}

		private void previewModeToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.RamSearchPreviewMode ^= true;
		}

		private void SpecificValueBox_TextChanged(object sender, EventArgs e)
		{
			DoPreview();
		}

		private void SpecificValueBox_KeyPress(object sender, KeyPressEventArgs e)
		{
			if (e.KeyChar == '\b') return;

			switch (GetDataType())
			{
				case Watch_Legacy.DISPTYPE.UNSIGNED:
					if (!InputValidate.IsValidUnsignedNumber(e.KeyChar))
					{
						e.Handled = true;
					}
					break;
				case Watch_Legacy.DISPTYPE.SIGNED:
					if (!InputValidate.IsValidSignedNumber(e.KeyChar))
					{
						e.Handled = true;
					}
					break;
				case Watch_Legacy.DISPTYPE.HEX:
					if (!InputValidate.IsValidHexNumber(e.KeyChar))
					{
						e.Handled = true;
					}
					break;
			}
		}

		private void NumberOfChangesBox_KeyPress(object sender, KeyPressEventArgs e)
		{
			if (e.KeyChar == '\b') return;

			if (!InputValidate.IsValidUnsignedNumber(e.KeyChar))
				e.Handled = true;
		}

		private void DifferentByBox_KeyPress(object sender, KeyPressEventArgs e)
		{
			if (e.KeyChar == '\b') return;

			if (!InputValidate.IsValidUnsignedNumber(e.KeyChar))
				e.Handled = true;
		}

		private void SpecificAddressBox_TextChanged(object sender, EventArgs e)
		{
			DoPreview();
		}

		private void NumberOfChangesBox_TextChanged(object sender, EventArgs e)
		{
			DoPreview();
		}

		private void DifferentByBox_TextChanged(object sender, EventArgs e)
		{
			DoPreview();
		}

		private void TruncateFromFileToolStripMenuItem_Click(object sender, EventArgs e)
		{
			TruncateFromFile();
		}

		private void DoTruncate()
		{
			MessageLabel.Text = MakeAddressString(Searches.Count(x => x.Deleted)) + " removed";
			TrimWeededList();
			UpdateLastSearch();
			DisplaySearchList();
			SearchHistory.AddState(Searches);
			UpdateUndoRedoToolItems();
		}

		private void TruncateFromFile()
		{
			//TODO: what about byte size? Think about the implications of this
			var file = GetFileFromUser();
			if (file != null)
			{
				List<Watch_Legacy> temp = new List<Watch_Legacy>();
				LoadSearchFile(file.FullName, false, temp);
				TruncateList(temp.Select(watch => watch.Address));
				DoTruncate();

			}
		}

		private void ClearWeeded()
		{
			foreach (Watch_Legacy watch in Searches)
			{
				watch.Deleted = false;
			}
		}


		private void TruncateList(IEnumerable<int> toRemove)
		{
			ClearWeeded();
			foreach (int addr in toRemove)
			{
				var first_or_default = Searches.FirstOrDefault(x => x.Address == addr);
				if (first_or_default != null)
				{
					first_or_default.Deleted = true;
				}
			}
			DoTruncate();
		}

		/// <summary>
		/// Removes Ram Watch list from the search list
		/// </summary>
		private void ExcludeRamWatchList()
		{
			TruncateList(Global.MainForm.NewRamWatch1.AddressList);
		}

		private void TruncateFromFiletoolStripButton2_Click(object sender, EventArgs e)
		{
			TruncateFromFile();
		}

		private void excludeRamWatchListToolStripMenuItem_Click(object sender, EventArgs e)
		{
			ExcludeRamWatchList();
		}

		private void ExcludeRamWatchtoolStripButton2_Click(object sender, EventArgs e)
		{
			ExcludeRamWatchList();
		}

		private void alwaysExcludeRamSearchListToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.RamSearchAlwaysExcludeRamWatch ^= true;
		}

		private void CopyValueToPrev()
		{
			foreach (Watch_Legacy t in Searches)
			{
				t.LastSearch = t.Value;
				t.Original = t.Value;
				t.Prev = t.Value;
				t.LastChange = t.Value;
			}
			DisplaySearchList();
			DoPreview();
		}

		private void UpdateLastSearch()
		{
			foreach (Watch_Legacy t in Searches)
			{
				t.LastSearch = t.Value;
			}
		}

		private void SetCurrToPrevtoolStripButton2_Click(object sender, EventArgs e)
		{
			CopyValueToPrev();
		}

		private void copyValueToPrevToolStripMenuItem_Click(object sender, EventArgs e)
		{
			CopyValueToPrev();
		}

		private void startNewSearchToolStripMenuItem_Click(object sender, EventArgs e)
		{
			StartNewSearch();
		}

		private void searchToolStripMenuItem2_Click(object sender, EventArgs e)
		{
			DoSearch();
		}

		private int GetNumDigits(Int32 i)
		{
			//if (i == 0) return 0;
			//if (i < 0x10) return 1;
			//if (i < 0x100) return 2;
			//if (i < 0x1000) return 3; //adelikat: commenting these out because I decided that regardless of domain, 4 digits should be the minimum
			if (i < 0x10000) return 4;
			if (i < 0x100000) return 5;
			if (i < 0x1000000) return 6;
			if (i < 0x10000000) return 7;
			else return 8;
		}

		private void FreezeAddressToolStrip_Click(object sender, EventArgs e)
		{
			FreezeAddress();
		}

		private void UnfreezeAddress()
		{
			ListView.SelectedIndexCollection indexes = SearchListView.SelectedIndices;
			if (indexes.Count > 0)
			{
				for (int i = 0; i < indexes.Count; i++)
				{
					switch (Searches[indexes[i]].Type)
					{
						case Watch_Legacy.TYPE.BYTE:
							Global.CheatList.Remove(Domain, Searches[indexes[i]].Address);
							break;
						case Watch_Legacy.TYPE.WORD:
							Global.CheatList.Remove(Domain, Searches[indexes[i]].Address);
							Global.CheatList.Remove(Domain, Searches[indexes[i]].Address + 1);
							break;
						case Watch_Legacy.TYPE.DWORD:
							Global.CheatList.Remove(Domain, Searches[indexes[i]].Address);
							Global.CheatList.Remove(Domain, Searches[indexes[i]].Address + 1);
							Global.CheatList.Remove(Domain, Searches[indexes[i]].Address + 2);
							Global.CheatList.Remove(Domain, Searches[indexes[i]].Address + 3);
							break;
					}
				}

				UpdateValues();
				Global.MainForm.HexEditor1.UpdateValues();
				Global.MainForm.NewRamWatch1.UpdateValues();
				Global.MainForm.Cheats_UpdateValues();
			}
		}

		private void FreezeAddress()
		{
			ListView.SelectedIndexCollection indexes = SearchListView.SelectedIndices;
			if (indexes.Count > 0)
			{
				for (int i = 0; i < indexes.Count; i++)
				{
					switch (Searches[indexes[i]].Type)
					{
						case Watch_Legacy.TYPE.BYTE:
							Cheat c = new Cheat("", Searches[indexes[i]].Address, (byte)Searches[indexes[i]].Value,
								true, Domain);
							Global.MainForm.Cheats1.AddCheat(c);
							break;
						case Watch_Legacy.TYPE.WORD:
							{
								byte low = (byte)(Searches[indexes[i]].Value / 256);
								byte high = (byte)(Searches[indexes[i]].Value);
								int a1 = Searches[indexes[i]].Address;
								int a2 = Searches[indexes[i]].Address + 1;
								if (Searches[indexes[i]].BigEndian)
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
						case Watch_Legacy.TYPE.DWORD:
							{
								byte HIWORDhigh = (byte)(Searches[indexes[i]].Value / 0x1000000);
								byte HIWORDlow = (byte)(Searches[indexes[i]].Value / 0x10000);
								byte LOWORDhigh = (byte)(Searches[indexes[i]].Value / 0x100);
								byte LOWORDlow = (byte)(Searches[indexes[i]].Value);
								int a1 = Searches[indexes[i]].Address;
								int a2 = Searches[indexes[i]].Address + 1;
								int a3 = Searches[indexes[i]].Address + 2;
								int a4 = Searches[indexes[i]].Address + 3;
								if (Searches[indexes[i]].BigEndian)
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

				UpdateValues();
				Global.MainForm.HexEditor1.UpdateValues();
				Global.MainForm.NewRamWatch1.UpdateValues();
				Global.MainForm.Cheats_UpdateValues();
			}
		}

		private void freezeAddressToolStripMenuItem_Click(object sender, EventArgs e)
		{
			FreezeAddress();
		}

		private void contextMenuStrip1_Opening(object sender, CancelEventArgs e)
		{
			ListView.SelectedIndexCollection indexes = SearchListView.SelectedIndices;
			if (indexes.Count == 0)
			{
				removeSelectedToolStripMenuItem1.Visible = false;
				addToRamWatchToolStripMenuItem.Visible = false;
				pokeAddressToolStripMenuItem1.Visible = false;
				freezeAddressToolStripMenuItem1.Visible = false;
				toolStripSeparator14.Visible = false;
				clearPreviewToolStripMenuItem.Visible = false;
			}
			else
			{
				for (int i = 0; i < contextMenuStrip1.Items.Count; i++)
				{
					contextMenuStrip1.Items[i].Visible = true;
				}

				if (indexes.Count == 1)
				{
					if (Global.CheatList.IsActiveCheat(Domain, Searches[indexes[0]].Address))
					{
						freezeAddressToolStripMenuItem1.Text = "&Unfreeze address";
						freezeAddressToolStripMenuItem1.Image = Properties.Resources.Unfreeze;
					}
					else
					{
						freezeAddressToolStripMenuItem1.Text = "&Freeze address";
						freezeAddressToolStripMenuItem1.Image = Properties.Resources.Freeze;
					}
				}
				else
				{
					bool allCheats = true;
					foreach (int i in indexes)
					{
						if (!Global.CheatList.IsActiveCheat(Domain, Searches[i].Address))
						{
							allCheats = false;
						}
					}

					if (allCheats)
					{
						freezeAddressToolStripMenuItem1.Text = "&Unfreeze address";
						freezeAddressToolStripMenuItem1.Image = Properties.Resources.Unfreeze;
					}
					else
					{
						freezeAddressToolStripMenuItem1.Text = "&Freeze address";
						freezeAddressToolStripMenuItem1.Image = Properties.Resources.Freeze;
					}
				}


				toolStripSeparator14.Visible = Global.Config.RamSearchPreviewMode;
				clearPreviewToolStripMenuItem.Visible = Global.Config.RamSearchPreviewMode;
			}

			unfreezeAllToolStripMenuItem.Visible = Global.CheatList.HasActiveCheats;
		}

		private void removeSelectedToolStripMenuItem1_Click(object sender, EventArgs e)
		{
			RemoveAddresses();
		}

		private void addToRamWatchToolStripMenuItem_Click(object sender, EventArgs e)
		{
			AddToRamWatch();
		}

		private void pokeAddressToolStripMenuItem1_Click(object sender, EventArgs e)
		{
			PokeAddress();
		}

		private void freezeAddressToolStripMenuItem1_Click(object sender, EventArgs e)
		{
			if (sender.ToString().Contains("Unfreeze"))
			{
				UnfreezeAddress();
			}
			else
			{
				FreezeAddress();
			}
		}

		private void CheckDomainMenuItems()
		{
			foreach (ToolStripMenuItem t in domainMenuItems)
			{
				if (Domain.Name == t.Text)
				{
					t.Checked = true;
				}
				else
				{
					t.Checked = false;
				}
			}
		}

		private void memoryDomainsToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			CheckDomainMenuItems();
		}

		private void SearchListView_ColumnReordered(object sender, ColumnReorderedEventArgs e)
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

			if (Global.Config.RamSearchAddressIndex >= lowIndex && Global.Config.RamSearchAddressIndex <= highIndex)
				Global.Config.RamSearchAddressIndex += changeIndex;
			if (Global.Config.RamSearchValueIndex >= lowIndex && Global.Config.RamSearchValueIndex <= highIndex)
				Global.Config.RamSearchValueIndex += changeIndex;
			if (Global.Config.RamSearchPrevIndex >= lowIndex && Global.Config.RamSearchPrevIndex <= highIndex)
				Global.Config.RamSearchPrevIndex += changeIndex;
			if (Global.Config.RamSearchChangesIndex >= lowIndex && Global.Config.RamSearchChangesIndex <= highIndex)
				Global.Config.RamSearchChangesIndex += changeIndex;

			if (header.Text == "Address")
				Global.Config.RamSearchAddressIndex = e.NewDisplayIndex;
			else if (header.Text == "Value")
				Global.Config.RamSearchValueIndex = e.NewDisplayIndex;
			else if (header.Text == "Prev")
				Global.Config.RamSearchPrevIndex = e.NewDisplayIndex;
			else if (header.Text == "Changes")
				Global.Config.RamSearchChangesIndex = e.NewDisplayIndex;
		}

		private void ColumnPositionSet()
		{
			List<ColumnHeader> columnHeaders = new List<ColumnHeader>();
			int i;
			for (i = 0; i < SearchListView.Columns.Count; i++)
			{
				columnHeaders.Add(SearchListView.Columns[i]);
			}

			SearchListView.Columns.Clear();

			i = 0;
			do
			{
				string column = "";
				if (Global.Config.RamSearchAddressIndex == i)
					column = "Address";
				else if (Global.Config.RamSearchValueIndex == i)
					column = "Value";
				else if (Global.Config.RamSearchPrevIndex == i)
					column = "Prev";
				else if (Global.Config.RamSearchChangesIndex == i)
					column = "Changes";

				for (int k = 0; k < columnHeaders.Count(); k++)
				{
					if (columnHeaders[k].Text == column)
					{
						SearchListView.Columns.Add(columnHeaders[k]);
						columnHeaders.Remove(columnHeaders[k]);
						break;
					}
				}
				i++;
			} while (columnHeaders.Any());
		}

		private void RamSearch_DragEnter(object sender, DragEventArgs e)
		{
			e.Effect = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
		}

		private void RamSearch_DragDrop(object sender, DragEventArgs e)
		{
			string[] filePaths = (string[])e.Data.GetData(DataFormats.FileDrop);
			if (Path.GetExtension(filePaths[0]) == (".wch"))
			{
				LoadSearchFile(filePaths[0], false, Searches);
				DisplaySearchList();
			}
		}

		private void OrderColumn(int columnToOrder)
		{
			string columnName = SearchListView.Columns[columnToOrder].Text;
			if (sortedCol.CompareTo(columnName) != 0)
				sortReverse = false;
			Searches.Sort((x, y) => x.CompareTo(y, columnName, (Watch_Legacy.PREVDEF)Global.Config.RamSearchPreviousAs) * (sortReverse ? -1 : 1));
			sortedCol = columnName;
			sortReverse = !(sortReverse);
			SearchListView.Refresh();
		}

		private void SearchListView_ColumnClick(object sender, ColumnClickEventArgs e)
		{
			OrderColumn(e.Column);
		}

		private void RedotoolStripButton2_Click(object sender, EventArgs e)
		{
			DoRedo();
		}

		private void WatchtoolStripButton1_Click_1(object sender, EventArgs e)
		{
			AddToRamWatch();
		}

		private void SearchListView_Enter(object sender, EventArgs e)
		{
			SearchListView.Refresh();
		}

		private void RamSearch_Activated(object sender, EventArgs e)
		{
			SearchListView.Refresh();
		}

		private void SearchListView_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Delete && !e.Control && !e.Alt && !e.Shift)
			{
				RemoveAddresses();
			}
			else if (e.KeyCode == Keys.A && e.Control && !e.Alt && !e.Shift) //Select All
			{
				for (int x = 0; x < Searches.Count; x++)
				{
					SearchListView.SelectItem(x, true);
				}
			}
			else if (e.KeyCode == Keys.C && e.Control && !e.Alt && !e.Shift) //Copy
			{
				ListView.SelectedIndexCollection indexes = SearchListView.SelectedIndices;
				if (indexes.Count > 0)
				{
					StringBuilder sb = new StringBuilder();
					foreach (int index in indexes)
					{
						for (int i = 0; i < SearchListView.Columns.Count; i++)
						{
							if (SearchListView.Columns[i].Width > 0)
							{
								sb.Append(GetColumnValue(i, index));
								sb.Append('\t');
							}
						}
						sb.Remove(sb.Length - 1, 1);
						sb.Append('\n');
					}

					if (!String.IsNullOrWhiteSpace(sb.ToString()))
					{
						Clipboard.SetDataObject(sb.ToString());
					}
				}
			}
		}

		private string GetColumnValue(int column, int watch_index)
		{
			switch (SearchListView.Columns[column].Text.ToLower())
			{
				default:
					return "";
				case "address":
					return Searches[watch_index].Address.ToString(addressFormatStr);
				case "value":
					return Searches[watch_index].ValueString;
				case "prev":
					switch (Global.Config.RamWatchPrev_Type)
					{
						case 1:
							return Searches[watch_index].PrevString;
						case 2:
							return Searches[watch_index].LastChangeString;
						default:
							return "";
					}
				case "changes":
					return Searches[watch_index].Changecount.ToString();
				case "diff":
					switch (Global.Config.RamWatchPrev_Type)
					{
						case 1:
							return Searches[watch_index].DiffPrevString;
						case 2:
							return Searches[watch_index].DiffLastChangeString;
						default:
							return "";
					}
				case "domain":
					return Searches[watch_index].Domain.Name;
				case "notes":
					return Searches[watch_index].Notes;
			}
		}

		private void redoToolStripMenuItem_Click(object sender, EventArgs e)
		{
			DoRedo();
		}

		private void viewInHexEditorToolStripMenuItem_Click(object sender, EventArgs e)
		{
			ListView.SelectedIndexCollection indexes = SearchListView.SelectedIndices;
			if (indexes.Count > 0)
			{
				Global.MainForm.LoadHexEditor();
				Global.MainForm.HexEditor1.SetDomain(Searches[indexes[0]].Domain);
				Global.MainForm.HexEditor1.GoToAddress(Searches[indexes[0]].Address);
			}
		}

		private void autoloadDialogToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.RecentSearches.AutoLoad ^= true;
		}

		private void unfreezeAllToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.MainForm.Cheats1.RemoveAllCheats();
			UpdateValues();

			Global.MainForm.NewRamWatch1.UpdateValues();
			Global.MainForm.HexEditor1.UpdateValues();
			Global.MainForm.Cheats_UpdateValues();
		}

		private void alwaysOnTopToolStripMenuItem_Click(object sender, EventArgs e)
		{
			alwaysOnTopToolStripMenuItem.Checked = alwaysOnTopToolStripMenuItem.Checked == false;
			this.TopMost = alwaysOnTopToolStripMenuItem.Checked;
		}

		private void clearUndoHistoryToolStripMenuItem_Click(object sender, EventArgs e)
		{
			SearchHistory.Clear();
		}

		private void fastModeToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.RamSearchFastMode ^= true;
			Global.Config.RamSearchPreviewMode = !Global.Config.RamSearchFastMode;
			if (Global.Config.RamSearchFastMode && Global.Config.RamSearchPreviousAs > 1)
			{
				Global.Config.RamSearchPreviousAs = 0;
			}
		}

		private void useUndoHistoryToolStripMenuItem_Click(object sender, EventArgs e)
		{
			useUndoHistoryToolStripMenuItem.Checked ^= true;
			SearchHistory = new HistoryCollection(Searches, useUndoHistoryToolStripMenuItem.Checked);
		}

		private void clearPreviewToolStripMenuItem_Click(object sender, EventArgs e)
		{
			forcePreviewClear = true;
			SearchListView.Refresh();
		}
	}
}
