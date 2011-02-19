using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

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
                searchList[x].value = Global.Emulator.MainMemory.PeekByte(searchList[x].address);
                //TODO: format based on asigned
                SearchListView.Items[x].SubItems[1].Text = searchList[x].value.ToString();
            }
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

        private void hackyAutoLoadToolStripMenuItem_Click(object sender, EventArgs e)
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

        private void StartNewSearch()
        {
            GetMemoryDomain();
            int startaddress = 0;
            if (Global.Emulator.SystemId == "PCE")
                startaddress = 0x1F0000;    //For now, until Emulator core functionality can better handle a prefix
            for (int x = 0; x < Global.Emulator.MainMemory.Size; x++)
            {
                searchList.Add(new Watch());
                searchList[x].address = x + startaddress;
                searchList[x].value = Global.Emulator.MainMemory.PeekByte(x);
            }
            DisplaySearchList();
        }

        private void DisplaySearchList()
        {
            SearchListView.Items.Clear();
            for (int x = 0; x < searchList.Count; x++)
            {
                ListViewItem item = new ListViewItem(String.Format("{0:X}", searchList[x].address));
                //TODO: if asigned.HeX, switch based on searchList.type
                item.SubItems.Add(string.Format("{0:X2}", searchList[x].value));
                SearchListView.Items.Add(item);
            }
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
    }
}
