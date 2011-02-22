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
    /// A winform designed to search through ram values
    /// </summary>
    public partial class RamSearch : Form
    {
        //TODO:
        //Window position gets saved but doesn't load properly
        //Implement definitions of Previous value
        //Multiple memory domains
        //Option to remove current Ram Watch list from search list
        //Option to always remove Ram Watch list from search list
        //Truncate from file in File menu (and toolstrip?)
        //When a new ROM is loaded - run Start new Search (or just clear list?)
        //Save Dialog - user cancelling crashes, same for Ram Search

        string systemID = "NULL";
        List<Watch> searchList = new List<Watch>();
        List<Watch> undoList = new List<Watch>();
        List<Watch> weededList = new List<Watch>();  //When addresses are weeded out, the new list goes here, before going into searchList
        List<Watch> prevList = new List<Watch>();

        public enum SCompareTo { PREV, VALUE, ADDRESS, CHANGES };
        public enum SOperator { LESS, GREATER, LESSEQUAL, GREATEREQUAL, EQUAL, NOTEQUAL, DIFFBY };
        public enum SSigned { SIGNED, UNSIGNED, HEX };

        //Reset window position item
        int defaultWidth;       //For saving the default size of the dialog, so the user can restore if desired
        int defaultHeight;
        string currentSearchFile = "";
        
        public RamSearch()
        {
            InitializeComponent();
        }

        public void UpdateValues()
        {
            for (int x = 0; x < searchList.Count; x++)
            {
                searchList[x].prev = searchList[x].value;
                searchList[x].PeekAddress(Global.Emulator.MainMemory);
                                
                if (searchList[x].prev != searchList[x].value)
                    searchList[x].changecount++;
  
            }
            if (AutoSearchCheckBox.Checked)
                DoSearch();
            else if (Global.Config.RamSearchPreviewMode)
                DoPreview();
            SearchListView.Refresh();
        }

        private void RamSearch_Load(object sender, EventArgs e)
        {
            defaultWidth = this.Size.Width;     //Save these first so that the user can restore to its original size
            defaultHeight = this.Size.Height;

            if (Global.Emulator.MainMemory.Endian == Endian.Big)
            {
                bigEndianToolStripMenuItem.Checked = true;
                littleEndianToolStripMenuItem.Checked = false;
            }
            else
            {
                bigEndianToolStripMenuItem.Checked = false;
                littleEndianToolStripMenuItem.Checked = true;
            }

            StartNewSearch();
            
            if (Global.Config.RamSearchWndx >= 0 && Global.Config.RamSearchWndy >= 0)
                this.Location = new Point(Global.Config.RamSearchWndx, Global.Config.RamSearchWndy);

            if (Global.Config.RamSearchWidth >= 0 && Global.Config.RamSearchHeight >= 0)
            {
                this.Size = new System.Drawing.Size(Global.Config.RamSearchWidth, Global.Config.RamSearchHeight);
            }
        }

        private void SetTotal()
        {
            int x = searchList.Count;
            string str;
            if (x == 1)
                str = " address";
            else
                str = " addresses";
            TotalSearchLabel.Text = x.ToString() + str;
        }

        private void OpenSearchFile()
        {
            var file = GetFileFromUser();
            if (file != null)
            {
                LoadSearchFile(file.FullName, false);
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
            this.Hide();
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
        }

        private void PreviousValueRadio_CheckedChanged(object sender, EventArgs e)
        {
            if (PreviousValueRadio.Checked)
            {
                SpecificValueBox.Enabled = false;
                SpecificAddressBox.Enabled = false;
                NumberOfChangesBox.Enabled = false;
            }
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
                if (!Global.MainForm.RamWatch1.IsHandleCreated || Global.MainForm.RamWatch1.IsDisposed)
                {
                    Global.MainForm.RamWatch1 = new RamWatch();
                    Global.MainForm.RamWatch1.Show();
                }
                else
                {
                    Global.MainForm.RamWatch1.Focus();
                }
                for (int x = 0; x < indexes.Count; x++)
                    Global.MainForm.RamWatch1.AddWatch(searchList[indexes[x]]);
            }
        }

        private void WatchtoolStripButton1_Click(object sender, EventArgs e)
        {
            AddToRamWatch();
        }

        private void RamSearch_LocationChanged(object sender, EventArgs e)
        {
            Global.Config.RamSearchWndx = this.Location.X;
            Global.Config.RamSearchWndy = this.Location.Y;
        }

        private void RamSearch_Resize(object sender, EventArgs e)
        {
            Global.Config.RamSearchWidth = this.Right - this.Left;
            Global.Config.RamSearchHeight = this.Bottom - this.Top;
        }

        private void restoreOriginalWindowSizeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Size = new System.Drawing.Size(defaultWidth, defaultHeight);
        }

        private void NewSearchtoolStripButton_Click(object sender, EventArgs e)
        {
            StartNewSearch();
        }

        private asigned GetDataType()
        {
            if (unsignedToolStripMenuItem.Checked)
                return asigned.UNSIGNED;
            if (signedToolStripMenuItem.Checked)
                return asigned.SIGNED;
            if (hexadecimalToolStripMenuItem.Checked)
                return asigned.HEX;

            return asigned.UNSIGNED;    //Just in case
        }

        private atype GetDataSize()
        {
            if (byteToolStripMenuItem.Checked)
                return atype.BYTE;
            if (bytesToolStripMenuItem.Checked)
                return atype.WORD;
            if (dWordToolStripMenuItem1.Checked)
                return atype.DWORD;

            return atype.BYTE;
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
            searchList.Clear();
            undoList.Clear();
            GetMemoryDomain();
            int startaddress = 0;
            if (Global.Emulator.SystemId == "PCE")
                startaddress = 0x1F0000;    //For now, until Emulator core functionality can better handle a prefix
            int count = 0;
            int divisor = 1;

            if (!includeMisalignedToolStripMenuItem.Checked)
            {
                switch (GetDataSize())
                {
                    case atype.WORD:
                        divisor = 2;
                        break;
                    case atype.DWORD:
                        divisor = 4;
                        break;
                    default:
                        divisor = 1;
                        break;
                }
            }
            
            for (int x = 0; x < ((Global.Emulator.MainMemory.Size / divisor)-1); x++)
            {
                searchList.Add(new Watch());
                searchList[x].address = count + startaddress;
                searchList[x].type = GetDataSize();
                searchList[x].bigendian = GetBigEndian();
                searchList[x].signed = GetDataType();
                searchList[x].PeekAddress(Global.Emulator.MainMemory);
                searchList[x].prev = searchList[x].value;
                if (includeMisalignedToolStripMenuItem.Checked)
                    count++;
                else
                {
                    switch (GetDataSize())
                    {
                        case atype.BYTE:
                            count++;
                            break;
                        case atype.WORD:
                            count += 2;
                            break;
                        case atype.DWORD:
                            count += 4;
                            break;
                    }
                }
                
            }
            OutputLabel.Text = "New search started";
            DisplaySearchList();
        }

        private void DisplaySearchList()
        {
			SearchListView.ItemCount = searchList.Count;
            SetTotal();
        }

        private void newSearchToolStripMenuItem_Click(object sender, EventArgs e)
        {
            StartNewSearch();
        }

        private void GetMemoryDomain()
        {
            string memoryDomain = "Main memory";
            systemID = Global.Emulator.SystemId;
            MemDomainLabel.Text = systemID + " " + memoryDomain;
        }

        private Point GetPromptPoint()
        {

            Point p = new Point(SearchListView.Location.X, SearchListView.Location.Y);
            Point q = new Point();
            q = PointToScreen(p);
            return q;
        }

        private void PokeAddress()
        {
            ListView.SelectedIndexCollection indexes = SearchListView.SelectedIndices;
            RamPoke p = new RamPoke();

            if (indexes.Count > 0)
                p.SetWatchObject(searchList[indexes[0]]);
            p.location = GetPromptPoint();
            p.ShowDialog();
        }

        private void PoketoolStripButton1_Click(object sender, EventArgs e)
        {
            PokeAddress();
        }

        private string MakeAddressString(int num)
        {
            if (num == 1)
                return "1 address";
            else
                return num.ToString() + " addresses";
        }

        private void RemoveAddresses()
        {
            ListView.SelectedIndexCollection indexes = SearchListView.SelectedIndices;
            if (indexes.Count > 0)
            {
                SaveUndo();
                OutputLabel.Text = MakeAddressString(indexes.Count) + " removed";
                for (int x = 0; x < indexes.Count; x++)
                {
                    searchList.Remove(searchList[indexes[x]-x]);
                }
                DisplaySearchList();
            }
        }

        private void cutToolStripButton_Click(object sender, EventArgs e)
        {
            RemoveAddresses();
        }

        /// <summary>
        /// Saves the current search list to the undo list
        /// This function should be called before any destructive operation to the list!
        /// </summary>
        private void SaveUndo()
        {
            undoList = new List<Watch>(searchList);
        }

        private void DoUndo()
        {
            if (undoList.Count > 0)
            {
                OutputLabel.Text = MakeAddressString(undoList.Count - searchList.Count) + " restored";
                searchList = new List<Watch>(undoList);
                undoList.Clear();
                DisplaySearchList();
            }
        }

        private void UndotoolStripButton_Click(object sender, EventArgs e)
        {
            DoUndo();
        }

		private void SearchListView_QueryItemBkColor(int index, int column, ref Color color)
		{
            //if (index % 2 == 0) color = Color.White; else color = Color.Pink;



            if (weededList.Contains(searchList[index]))
            {
                color = Color.Pink;
            }
            else
                color = Color.White;
            //TODO: make background pink on items that would be removed if search button were clicked
		}

		private void SearchListView_QueryItemText(int index, int column, out string text)
		{
			text = "";
            if (column == 0)
            {
                text = searchList[index].address.ToString("X");
            }
            if (column == 1)
            {
                if (searchList[index].signed == asigned.UNSIGNED)
                    text = searchList[index].value.ToString();
                else if (searchList[index].signed == asigned.SIGNED)
                    text = ((sbyte)searchList[index].value).ToString();
                else if (searchList[index].signed == asigned.HEX)
                    text = searchList[index].value.ToString("X");

            }
            if (column == 2)
            {
                if (searchList[index].signed == asigned.UNSIGNED)
                    text = searchList[index].prev.ToString();
                else if (searchList[index].signed == asigned.SIGNED)
                    text = ((sbyte)searchList[index].prev).ToString();
                else if (searchList[index].signed == asigned.HEX)
                    text = searchList[index].prev.ToString("X");
            }
            if (column == 3)
            {
                text = searchList[index].changecount.ToString();
            }
		}

		private void SearchListView_QueryItemIndent(int index, out int itemIndent)
		{
			itemIndent = 0;
		}

		private void SearchListView_QueryItemImage(int index, int column, out int imageIndex)
		{
			imageIndex = -1;
		}

        private void ClearChangeCounts()
        {
            SaveUndo();
            for (int x = 0; x < searchList.Count; x++)
                searchList[x].changecount = 0;
            DisplaySearchList();
            OutputLabel.Text = "Change counts cleared";
        }

        private void ClearChangeCountstoolStripButton_Click(object sender, EventArgs e)
        {
            ClearChangeCounts();
        }

        private void UndotoolStripButton_Click_1(object sender, EventArgs e)
        {
            DoUndo();
        }

        private void ReplaceSearchListWithWeedOutList()
        {
            searchList = new List<Watch>(weededList);
            weededList.Clear();
        }

        private void DoPreview()
        {
            if (Global.Config.RamSearchPreviewMode)
            {
                weededList.Clear();
                if (GenerateWeedOutList())
                {
                    DisplaySearchList();
                    OutputLabel.Text = MakeAddressString(weededList.Count) + "would be removed";
                }
            }
        }

        private void DoSearch()
        {
            //TODO: if already previewed, don't generate the list again, perhaps a bool?
            if (GenerateWeedOutList())
            {
                SaveUndo();
                OutputLabel.Text = MakeAddressString(searchList.Count - weededList.Count) + " removed";
                ReplaceSearchListWithWeedOutList();
                DisplaySearchList();
            }
            else
                OutputLabel.Text = "Search failed.";
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
            return searchList[pos].prev;    //TODO: return value based on user choice
        }

        private bool DoPreviousValue()
        {
            switch (GetOperator())
            {
                case SOperator.LESS:
                    for (int x = 0; x < searchList.Count; x++)
                    {
                        if (searchList[x].value < GetPreviousValue(x))
                            weededList.Add(searchList[x]);
                    }
                    break;
                case SOperator.GREATER:
                    for (int x = 0; x < searchList.Count; x++)
                    {
                        if (searchList[x].value > GetPreviousValue(x))
                            weededList.Add(searchList[x]);
                    }
                    break;
                case SOperator.LESSEQUAL:
                    for (int x = 0; x < searchList.Count; x++)
                    {
                        if (searchList[x].value <= GetPreviousValue(x))
                            weededList.Add(searchList[x]);
                    }
                    break;
                case SOperator.GREATEREQUAL:
                    for (int x = 0; x < searchList.Count; x++)
                    {
                        if (searchList[x].value >= GetPreviousValue(x))
                            weededList.Add(searchList[x]);
                    }
                    break;
                case SOperator.EQUAL:
                    for (int x = 0; x < searchList.Count; x++)
                    {
                        if (searchList[x].value == GetPreviousValue(x))
                            weededList.Add(searchList[x]);
                    }
                    break;
                case SOperator.NOTEQUAL:
                    for (int x = 0; x < searchList.Count; x++)
                    {
                        if (searchList[x].value != GetPreviousValue(x))
                            weededList.Add(searchList[x]);
                    }
                    break;
                case SOperator.DIFFBY:
                    int diff = GetDifferentBy();
                    if (diff < 0) return false;
                    for (int x = 0; x < searchList.Count; x++)
                    {
                        if (searchList[x].value == GetPreviousValue(x) + diff || searchList[x].value == GetPreviousValue(x) - diff)
                            weededList.Add(searchList[x]);
                    }
                    break;
            }
            return true;
        }

        private bool DoSpecificValue()
        {
            int value = GetSpecificValue();
            if (value < 0)
            {
                MessageBox.Show("Missing or invalid value", "Invalid value", MessageBoxButtons.OK, MessageBoxIcon.Error);
                SpecificValueBox.Focus();
                SpecificValueBox.SelectAll();
                return false;
            }
            switch (GetOperator())
            {
                case SOperator.LESS:
                    for (int x = 0; x < searchList.Count; x++)
                    {
                        if (searchList[x].value < value)
                            weededList.Add(searchList[x]);
                    }
                    break;
                case SOperator.GREATER:
                    for (int x = 0; x < searchList.Count; x++)
                    {
                        if (searchList[x].value > value)
                            weededList.Add(searchList[x]);
                    }
                    break;
                case SOperator.LESSEQUAL:
                    for (int x = 0; x < searchList.Count; x++)
                    {
                        if (searchList[x].value <= value)
                            weededList.Add(searchList[x]);
                    }
                    break;
                case SOperator.GREATEREQUAL:
                    for (int x = 0; x < searchList.Count; x++)
                    {
                        if (searchList[x].value >= value)
                            weededList.Add(searchList[x]);
                    }
                    break;
                case SOperator.EQUAL:
                    for (int x = 0; x < searchList.Count; x++)
                    {
                        if (searchList[x].value == value)
                            weededList.Add(searchList[x]);
                    }
                    break;
                case SOperator.NOTEQUAL:
                    for (int x = 0; x < searchList.Count; x++)
                    {
                        if (searchList[x].value != value)
                            weededList.Add(searchList[x]);
                    }
                    break;
                case SOperator.DIFFBY:
                    int diff = GetDifferentBy();
                    if (diff < 0) return false;
                    for (int x = 0; x < searchList.Count; x++)
                    {
                        if (searchList[x].value == value + diff || searchList[x].value == value - diff)
                            weededList.Add(searchList[x]);
                    }
                    break;
            }
            return true;
        }

        private int GetSpecificValue()
        {
            if (SpecificValueBox.Text == "") return 0;
            bool i = InputValidate.IsValidSignedNumber(SpecificValueBox.Text);
            if (!i) return -1;

            return int.Parse(SpecificValueBox.Text);
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
                    for (int x = 0; x < searchList.Count; x++)
                    {
                        if (searchList[x].address < address)
                            weededList.Add(searchList[x]);
                    }
                    break;
                case SOperator.GREATER:
                    for (int x = 0; x < searchList.Count; x++)
                    {
                        if (searchList[x].address > address)
                            weededList.Add(searchList[x]);
                    }
                    break;
                case SOperator.LESSEQUAL:
                    for (int x = 0; x < searchList.Count; x++)
                    {
                        if (searchList[x].address <= address)
                            weededList.Add(searchList[x]);
                    }
                    break;
                case SOperator.GREATEREQUAL:
                    for (int x = 0; x < searchList.Count; x++)
                    {
                        if (searchList[x].address >= address)
                            weededList.Add(searchList[x]);
                    }
                    break;
                case SOperator.EQUAL:
                    for (int x = 0; x < searchList.Count; x++)
                    {
                        if (searchList[x].address == address)
                            weededList.Add(searchList[x]);
                    }
                    break;
                case SOperator.NOTEQUAL:
                    for (int x = 0; x < searchList.Count; x++)
                    {
                        if (searchList[x].address != address)
                            weededList.Add(searchList[x]);
                    }
                    break;
                case SOperator.DIFFBY:
                    {
                        int diff = GetDifferentBy();
                        if (diff < 0) return false;
                        for (int x = 0; x < searchList.Count; x++)
                        {
                            if (searchList[x].address == address + diff || searchList[x].address == address - diff)
                                weededList.Add(searchList[x]);
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
                    for (int x = 0; x < searchList.Count; x++)
                    {
                        if (searchList[x].changecount < changes)
                            weededList.Add(searchList[x]);
                    }
                    break;
                case SOperator.GREATER:
                    for (int x = 0; x < searchList.Count; x++)
                    {
                        if (searchList[x].changecount > changes)
                            weededList.Add(searchList[x]);
                    }
                    break;
                case SOperator.LESSEQUAL:
                    for (int x = 0; x < searchList.Count; x++)
                    {
                        if (searchList[x].changecount <= changes)
                            weededList.Add(searchList[x]);
                    }
                    break;
                case SOperator.GREATEREQUAL:
                    for (int x = 0; x < searchList.Count; x++)
                    {
                        if (searchList[x].changecount >= changes)
                            weededList.Add(searchList[x]);
                    }
                    break;
                case SOperator.EQUAL:
                    for (int x = 0; x < searchList.Count; x++)
                    {
                        if (searchList[x].changecount == changes)
                            weededList.Add(searchList[x]);
                    }
                    break;
                case SOperator.NOTEQUAL:
                    for (int x = 0; x < searchList.Count; x++)
                    {
                        if (searchList[x].changecount != changes)
                            weededList.Add(searchList[x]);
                    }
                    break;
                case SOperator.DIFFBY:
                    int diff = GetDifferentBy();
                    if (diff < 0) return false;
                    for (int x = 0; x < searchList.Count; x++)
                    {
                        if (searchList[x].address == changes + diff || searchList[x].address == changes - diff)
                            weededList.Add(searchList[x]);
                    }
                    break;
            }
            return true;
        }

        private void ConvertListDataType(asigned s)
        {
            for (int x = 0; x < searchList.Count; x++)
                searchList[x].signed = s;
        }

        private void signedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            unsignedToolStripMenuItem.Checked = false;
            signedToolStripMenuItem.Checked = true;
            hexadecimalToolStripMenuItem.Checked = false;
            ConvertListDataType(asigned.SIGNED);
            DisplaySearchList();
        }
                
        private void unsignedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            unsignedToolStripMenuItem.Checked = true;
            signedToolStripMenuItem.Checked = false;
            hexadecimalToolStripMenuItem.Checked = false;
            ConvertListDataType(asigned.UNSIGNED);
            DisplaySearchList();
        }

        private void hexadecimalToolStripMenuItem_Click(object sender, EventArgs e)
        {
            unsignedToolStripMenuItem.Checked = false;
            signedToolStripMenuItem.Checked = false;
            hexadecimalToolStripMenuItem.Checked = true;
            ConvertListDataType(asigned.HEX);
            DisplaySearchList();
        }

        private void SearchListView_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            ListView.SelectedIndexCollection indexes = SearchListView.SelectedIndices;
            if (indexes.Count > 0)
            {
                AddToRamWatch();
            }
        }

        private void byteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            byteToolStripMenuItem.Checked = true;
            bytesToolStripMenuItem.Checked = false;
            dWordToolStripMenuItem1.Checked = false;
        }

        private void bytesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            byteToolStripMenuItem.Checked = false;
            bytesToolStripMenuItem.Checked = true;
            dWordToolStripMenuItem1.Checked = false;
        }

        private void dWordToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            byteToolStripMenuItem.Checked = false;
            bytesToolStripMenuItem.Checked = false;
            dWordToolStripMenuItem1.Checked = true;
        }

        private void bigEndianToolStripMenuItem_Click(object sender, EventArgs e)
        {
            bigEndianToolStripMenuItem.Checked = true;
            littleEndianToolStripMenuItem.Checked = false;
        }

        private void littleEndianToolStripMenuItem_Click(object sender, EventArgs e)
        {
            bigEndianToolStripMenuItem.Checked = false;
            littleEndianToolStripMenuItem.Checked = true;
        }

        private void AutoSearchCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (AutoSearchCheckBox.Checked)
                AutoSearchCheckBox.BackColor = Color.Pink;
            else
                AutoSearchCheckBox.BackColor = this.BackColor;
        }

        private void SpecificValueBox_Leave(object sender, EventArgs e)
        {
            SpecificValueBox.Text = SpecificValueBox.Text.Replace(" ", "");
            if (!InputValidate.IsValidSignedNumber(SpecificValueBox.Text))
            {
                SpecificValueBox.Focus();
                SpecificValueBox.SelectAll();
                ToolTip t = new ToolTip();
                t.Show("Must be a valid decimal value", SpecificValueBox, 5000);
            }
        }

        private void SpecificAddressBox_Leave(object sender, EventArgs e)
        {
            SpecificAddressBox.Text = SpecificAddressBox.Text.Replace(" ", "");
            if (!InputValidate.IsValidHexNumber(SpecificAddressBox.Text))
            {
                SpecificAddressBox.Focus();
                SpecificAddressBox.SelectAll();
                ToolTip t = new ToolTip();
                t.Show("Must be a valid hexadecimal value", SpecificAddressBox, 5000);
            }
        }

        private void NumberOfChangesBox_Leave(object sender, EventArgs e)
        {
            NumberOfChangesBox.Text = NumberOfChangesBox.Text.Replace(" ", "");
            if (!InputValidate.IsValidUnsignedNumber(NumberOfChangesBox.Text))
            {
                NumberOfChangesBox.Focus();
                NumberOfChangesBox.SelectAll();
                ToolTip t = new ToolTip();
                t.Show("Must be a valid unsigned decimal value", NumberOfChangesBox, 5000);
            }
        }

        private void DifferentByBox_Leave(object sender, EventArgs e)
        {
            DifferentByBox.Text = DifferentByBox.Text.Replace(" ", "");
            if (!InputValidate.IsValidUnsignedNumber(DifferentByBox.Text))
            {
                DifferentByBox.Focus();
                DifferentByBox.SelectAll();
                ToolTip t = new ToolTip();
                t.Show("Must be a valid unsigned decimal value", DifferentByBox, 5000);
            }
        }

        private bool SaveSearchFile(string path)
        {
            var file = new FileInfo(path);

            using (StreamWriter sw = new StreamWriter(path))
            {
                string str = "";

                for (int x = 0; x < searchList.Count; x++)
                {
                    str += string.Format("{0:X4}", searchList[x].address) + "\t";
                    str += searchList[x].GetTypeByChar().ToString() + "\t";
                    str += searchList[x].GetSignedByChar().ToString() + "\t";

                    if (searchList[x].bigendian == true)
                        str += "1\t";
                    else
                        str += "0\t";

                    str += searchList[x].notes + "\n";
                }

                sw.WriteLine(str);
            }
            return true;
        }

        private FileInfo GetSaveFileFromUser()
        {
            var sfd = new SaveFileDialog();
            sfd.InitialDirectory = Global.Config.LastRomPath;
            sfd.Filter = "Watch Files (*.wch)|*.wch|All Files|*.*";
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

        public void SaveAs()
        {
            var file = GetSaveFileFromUser();
            if (file != null)
            {
                SaveSearchFile(file.FullName);
                currentSearchFile = file.FullName;
            }
            OutputLabel.Text = Path.GetFileName(currentSearchFile) + " saved.";
        }

        private void LoadSearchFromRecent(string file)
        {
            bool r = LoadSearchFile(file, false);
            if (!r)
            {
                DialogResult result = MessageBox.Show("Could not open " + file + "\nRemove from list?", "File not found", MessageBoxButtons.YesNo, MessageBoxIcon.Error);
                if (result == DialogResult.Yes)
                    Global.Config.RecentSearches.Remove(file);
            }
            DisplaySearchList();
        }

        public int HowMany(string str, char c)
        {
            int count = 0;
            for (int x = 0; x < str.Length; x++)
            {
                if (str[x] == c)
                    count++;
            }
            return count;
        }

        bool LoadSearchFile(string path, bool append)
        {
            int y, z;
            var file = new FileInfo(path);
            if (file.Exists == false) return false;

            using (StreamReader sr = file.OpenText())
            {
                if (!append)
                    currentSearchFile = path;

                int count = 0;
                string s = "";
                string temp = "";

                if (append == false)
                    searchList.Clear();  //Wipe existing list and read from file

                while ((s = sr.ReadLine()) != null)
                {
                    //parse each line and add to watchList

                    //.wch files from other emulators start with a number representing the number of watch, that line can be discarded here
                    //Any properly formatted line couldn't possibly be this short anyway, this also takes care of any garbage lines that might be in a file
                    if (s.Length < 5) continue;

                    z = HowMany(s, '\t');
                    if (z == 5)
                    {
                        //If 5, then this is a .wch file format made from another emulator, the first column (watch position) is not needed here
                        y = s.IndexOf('\t') + 1;
                        s = s.Substring(y, s.Length - y);   //5 digit value representing the watch position number
                    }
                    else if (z != 4)
                        continue;   //If not 4, something is wrong with this line, ignore it
                    count++;
                    Watch w = new Watch();

                    temp = s.Substring(0, s.IndexOf('\t'));
                    w.address = int.Parse(temp, NumberStyles.HexNumber);

                    y = s.IndexOf('\t') + 1;
                    s = s.Substring(y, s.Length - y);   //Type
                    w.SetTypeByChar(s[0]);

                    y = s.IndexOf('\t') + 1;
                    s = s.Substring(y, s.Length - y);   //Signed
                    w.SetSignedByChar(s[0]);

                    y = s.IndexOf('\t') + 1;
                    s = s.Substring(y, s.Length - y);   //Endian
                    y = Int16.Parse(s[0].ToString());
                    if (y == 0)
                        w.bigendian = false;
                    else
                        w.bigendian = true;

                    //w.notes = s.Substring(2, s.Length - 2);   //User notes

                    searchList.Add(w);
                }

                Global.Config.RecentSearches.Add(file.FullName);
                OutputLabel.Text = Path.GetFileName(file.FullName);
                //Update the number of watches
                SetTotal();
            }

            return true;
        }

        private void recentToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
        {
            //Clear out recent Roms list
            //repopulate it with an up to date list
            recentToolStripMenuItem.DropDownItems.Clear();

            if (Global.Config.RecentSearches.IsEmpty())
            {
                recentToolStripMenuItem.DropDownItems.Add("None");
            }
            else
            {
                for (int x = 0; x < Global.Config.RecentSearches.Length(); x++)
                {
                    string path = Global.Config.RecentSearches.GetRecentFileByPosition(x);
                    var item = new ToolStripMenuItem();
                    item.Text = path;
                    item.Click += (o, ev) => LoadSearchFromRecent(path);
                    recentToolStripMenuItem.DropDownItems.Add(item);
                }
            }

            recentToolStripMenuItem.DropDownItems.Add("-");

            var clearitem = new ToolStripMenuItem();
            clearitem.Text = "&Clear";
            clearitem.Click += (o, ev) => Global.Config.RecentSearches.Clear();
            recentToolStripMenuItem.DropDownItems.Add(clearitem);

            var auto = new ToolStripMenuItem();
            auto.Text = "&Auto-Load";
            auto.Click += (o, ev) => UpdateAutoLoadRamSearch();
            if (Global.Config.AutoLoadRamSearch == true)
                auto.Checked = true;
            else
                auto.Checked = false;
            recentToolStripMenuItem.DropDownItems.Add(auto);
        }

        private void UpdateAutoLoadRamSearch()
        {
            autoLoadToolStripMenuItem.Checked = Global.Config.AutoLoadRamSearch ^= true;
        }

        private void appendFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var file = GetFileFromUser();
            if (file != null)
                LoadSearchFile(file.FullName, true);
            DisplaySearchList();
        }

        private FileInfo GetFileFromUser()
        {
            var ofd = new OpenFileDialog();
            ofd.InitialDirectory = Global.Config.LastRomPath;
            ofd.Filter = "Watch Files (*.wch)|*.wch|All Files|*.*";
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

        private void includeMisalignedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            includeMisalignedToolStripMenuItem.Checked ^= true;
        }

        private void saveWindowPositionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Global.Config.RamSearchSaveWindowPosition ^= true;            
        }

        private void optionsToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
        {
            saveWindowPositionToolStripMenuItem.Checked = Global.Config.RamSearchSaveWindowPosition;
            previewModeToolStripMenuItem.Checked = Global.Config.RamSearchPreviewMode;
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
            if (string.Compare(currentSearchFile, "") == 0) SaveAs();
            SaveSearchFile(currentSearchFile);
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
            if (searchList.Count == 0)
                searchToolStripMenuItem.Enabled = false;
            else
                searchToolStripMenuItem.Enabled = true;

            if (undoList.Count == 0)
                UndotoolStripButton.Enabled = false;
            else
                UndotoolStripButton.Enabled = true;

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
        }

        private void sinceLastChangeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Global.Config.RamSearchPreviousAs = 1;
        }

        private void sinceLastFrameToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Global.Config.RamSearchPreviousAs = 2;
        }

        private void definePreviousValueToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
        {
            switch (Global.Config.RamSearchPreviousAs)
            {
                case 0: //Since last Search
                    sinceLastSearchToolStripMenuItem.Checked = true;
                    sinceLastChangeToolStripMenuItem.Checked = false;
                    sinceLastFrameToolStripMenuItem.Checked = false;
                    break;
                case 1: //Since last Change
                    sinceLastSearchToolStripMenuItem.Checked = false;
                    sinceLastChangeToolStripMenuItem.Checked = true;
                    sinceLastFrameToolStripMenuItem.Checked = false;
                    break;
                case 2: //Since last Frame
                    sinceLastSearchToolStripMenuItem.Checked = false;
                    sinceLastChangeToolStripMenuItem.Checked = true;
                    sinceLastFrameToolStripMenuItem.Checked = false;
                    break;
                default://Default to last search
                    sinceLastSearchToolStripMenuItem.Checked = true;
                    sinceLastChangeToolStripMenuItem.Checked = false;
                    sinceLastFrameToolStripMenuItem.Checked = false;
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
    }


}
