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
    public partial class RamWatch : Form
    {
        List<Watch> watchList = new List<Watch>();   
        
        public RamWatch()
        {
            InitializeComponent();
        }

        void AddNewWatch()
        {
        }

        void EditWatch()
        {
        }

        void RemoveWatch()
        {
        }

        void DuplicateWatch()
        {
        }

        void MoveUp()
        {
        }

        void MoveDown()
        {
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void newListToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void appendFileToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void autoLoadToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void saveWindowPositionToolStripMenuItem_Click(object sender, EventArgs e)
        {

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
            Watch watch1 = new Watch();
            watch1.notes = "Test";

            ListViewItem item1 = new ListViewItem(watch1.address.ToString(), 0);
            WatchListView.Items.Add(item1);
            
        }
    }
}
