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

        string systemID = "NULL";
        List<Watch> searchList = new List<Watch>();
        List<Watch> undoList = new List<Watch>();
        List<Watch> weededList = new List<Watch>();  //When addresses are weeded out, the new list goes here, before going into searchList

        public enum SCompareTo { PREV, VALUE, ADDRESS, CHANGES };
        public enum SOperator { LESS, GREATER, LESSEQUAL, GREATEREQUAL, EQUAL, NOTEQUAL, DIFFBY, MODULUS };
        public enum SSigned { SIGNED, UNSIGNED, HEX };

        //Reset window position item
        int defaultWidth;       //For saving the default size of the dialog, so the user can restore if desired
        int defaultHeight;
        
        public RamSearch()
        {
            InitializeComponent();
        }

        public void UpdateValues()
        {
            //TODO: update based on atype
            for (int x = 0; x < searchList.Count; x++)
            {
                searchList[x].prev = searchList[x].value;
                //TODO: format based on asigned
                searchList[x].value = Global.Emulator.MainMemory.PeekByte(searchList[x].address);
                
                if (searchList[x].prev != searchList[x].value)
                    searchList[x].changecount++;
  
            }
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

        }

        private void SaveAs()
        {

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
                SpecificValueBox.Enabled = true;
                SpecificAddressBox.Enabled = false;
                NumberOfChangesBox.Enabled = false;
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
                SpecificValueBox.Enabled = false;
                SpecificAddressBox.Enabled = true;
                NumberOfChangesBox.Enabled = false;
            }
        }

        private void NumberOfChangesRadio_CheckedChanged(object sender, EventArgs e)
        {
            if (NumberOfChangesRadio.Checked)
            {
                SpecificValueBox.Enabled = false;
                SpecificAddressBox.Enabled = false;
                NumberOfChangesBox.Enabled = true;
            }
        }

        private void DifferentByRadio_CheckedChanged(object sender, EventArgs e)
        {
            if (DifferentByRadio.Checked)
                DifferentByBox.Enabled = true;
            else
                DifferentByBox.Enabled = false;
        }

        private void ModuloRadio_CheckedChanged(object sender, EventArgs e)
        {
            if (ModuloRadio.Checked)
                ModuloBox.Enabled = true;
            else
                ModuloBox.Enabled = false;
        }

        private void AddToRamWatch()
        {
            ListView.SelectedIndexCollection indexes = SearchListView.SelectedIndices;

            if (indexes.Count > 0)
            {
                if (!Global.MainForm.RamWatch1.IsDisposed)
                {
                    Global.MainForm.RamWatch1.Focus();
                }
                else
                {
                    Global.MainForm.RamWatch1 = new RamWatch();
                    Global.MainForm.RamWatch1.Show();
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
            
            for (int x = 0; x < (Global.Emulator.MainMemory.Size / divisor); x++)
            {
                searchList.Add(new Watch());
                searchList[x].address = count + startaddress;
                switch (GetDataSize())
                {
                    case atype.BYTE:
                        searchList[x].prev = searchList[x].value = Global.Emulator.MainMemory.PeekByte(count);
                        searchList[x].bigendian = GetBigEndian();   //Pointless in 1 byte, but might as well
                        searchList[x].signed = GetDataType();
                        searchList[x].type = atype.BYTE;
                        count++;
                        break;
                    case atype.WORD:
                        if (GetBigEndian())
                        {
                            searchList[x].prev = searchList[x].value = ((Global.Emulator.MainMemory.PeekByte(searchList[x].address) * 256) +
                                    Global.Emulator.MainMemory.PeekByte((searchList[x + 1].address) + 1));
                        }
                        else
                        {
                            searchList[x].prev = searchList[x].value = (Global.Emulator.MainMemory.PeekByte(searchList[x].address) +
                                   (Global.Emulator.MainMemory.PeekByte((searchList[x].address) + 1) * 256));
                        }
                        searchList[x].bigendian = GetBigEndian();   //Pointless in 1 byte, but might as well
                        searchList[x].signed = GetDataType();
                        searchList[x].type = atype.BYTE;
                        count += 2;
                        break;
                    case atype.DWORD:
                        //TODO
                        break;
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
            string memoryDomain = "Main memory"; //TODO: multiple memory domains
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

        private void RemoveAddresses()
        {
            ListView.SelectedIndexCollection indexes = SearchListView.SelectedIndices;
            if (indexes.Count > 0)
            {
                SaveUndo();
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

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            if (GenerateWeedOutList())
            {
                SaveUndo();
                OutputLabel.Text = (searchList.Count - weededList.Count).ToString() + " addresses removed";  //TODO: address if only 1
                ReplaceSearchListWithWeedOutList();
                DisplaySearchList();
            }
            //TODO: else notify the user something went wrong?

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
            if (ModuloRadio.Checked)
                return SOperator.MODULUS;

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

        private bool DoPreviousValue()
        {
            switch (GetOperator())
            {
                case SOperator.LESS:
                    break;
                case SOperator.GREATER:
                    break;
                case SOperator.LESSEQUAL:
                    break;
                case SOperator.GREATEREQUAL:
                    break;
                case SOperator.EQUAL:
                    break;
                case SOperator.NOTEQUAL:
                    break;
                case SOperator.DIFFBY:
                    break;
                case SOperator.MODULUS:
                    break;
            }
            return false;
        }

        private bool DoSpecificValue()
        {
            switch (GetOperator())
            {
                case SOperator.LESS:
                    break;
                case SOperator.GREATER:
                    break;
                case SOperator.LESSEQUAL:
                    break;
                case SOperator.GREATEREQUAL:
                    break;
                case SOperator.EQUAL:
                    break;
                case SOperator.NOTEQUAL:
                    break;
                case SOperator.DIFFBY:
                    break;
                case SOperator.MODULUS:
                    break;
            }
            return false;
        }

        private int GetSpecificAddress()
        {
            bool i = InputValidate.IsValidHexNumber(SpecificAddressBox.Text);
            if (!i) return -1;

            return int.Parse(SpecificAddressBox.Text.ToUpper().Trim(), NumberStyles.HexNumber);
        }

        private int GetDifferentBy()
        {
            bool i = InputValidate.IsValidUnsignedNumber(DifferentByBox.Text);
            if (!i) return -1;

            return int.Parse(DifferentByBox.Text.ToUpper().Trim());
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
                        if (diff < 0)
                        {
                            MessageBox.Show("Missing or invalid Different By value", "Invalid value", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            DifferentByBox.Focus();
                            DifferentByBox.SelectAll();
                            return false;
                        }
                        for (int x = 0; x < searchList.Count; x++)
                        {
                            if (searchList[x].address == address + diff || searchList[x].address == address - diff)
                                weededList.Add(searchList[x]);
                        }
                    }
                    break;
                case SOperator.MODULUS:
                    //TODO
                    break;
            }
            return true;
        }

        private int GetSpecificChanges()
        {
            bool i = InputValidate.IsValidUnsignedNumber(NumberOfChangesBox.Text);
            if (!i) return -1;

            return int.Parse(NumberOfChangesBox.Text.ToUpper().Trim());
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
                    if (diff < 0)
                    {
                        MessageBox.Show("Missing or invalid Different By value", "Invalid value", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        DifferentByBox.Focus();
                        DifferentByBox.SelectAll();
                        return false;
                    }
                    for (int x = 0; x < searchList.Count; x++)
                    {
                        if (searchList[x].address == changes + diff || searchList[x].address == changes - diff)
                            weededList.Add(searchList[x]);
                    }
                    break;
                case SOperator.MODULUS:
                    break;
            }
            return true;
        }

        private void signedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            unsignedToolStripMenuItem.Checked = false;
            signedToolStripMenuItem.Checked = true;
            hexadecimalToolStripMenuItem.Checked = false;
        }
                
        private void unsignedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            unsignedToolStripMenuItem.Checked = true;
            signedToolStripMenuItem.Checked = false;
            hexadecimalToolStripMenuItem.Checked = false;
        }

        private void hexadecimalToolStripMenuItem_Click(object sender, EventArgs e)
        {
            unsignedToolStripMenuItem.Checked = false;
            signedToolStripMenuItem.Checked = false;
            hexadecimalToolStripMenuItem.Checked = true;
        }

        private void hackyAutoLoadToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            if (Global.Config.AutoLoadRamSearch == true)
            {
                Global.Config.AutoLoadRamSearch = false;
                hackyAutoLoadToolStripMenuItem.Checked = false;
            }
            else
            {
                Global.Config.AutoLoadRamSearch = true;
                hackyAutoLoadToolStripMenuItem.Checked = true;
            }
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
    }
}
