using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace BizHawk.MultiClient
{
	public partial class NewRamWatch : Form
	{
        private WatchList Watches = new WatchList();
        private string systemID = "NULL";

        public NewRamWatch()
		{
			InitializeComponent();
		}

        public bool AskSave()
        {
            if (Global.Config.SupressAskSave) //User has elected to not be nagged
            {
                return true;
            }

            if (Watches.Changes)
            {
                Global.Sound.StopSound();
                DialogResult result = MessageBox.Show("Save Changes?", "Ram Watch", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button3);
                Global.Sound.StartSound();
                if (result == DialogResult.Yes)
                {
                    Watches.Save();
                }
                else if (result == DialogResult.No)
                {
                    return true;
                }
                else if (result == DialogResult.Cancel)
                {
                    return false;
                }
            }

            return true;
        }

        private void DisplayWatches()
        {
            WatchListView.ItemCount = Watches.ItemCount;
        }

        private void UpdateWatchCount()
        {
            int count = Watches.WatchCount;
            WatchCountLabel.Text = count.ToString() + (count == 1 ? " watch" : " watches");
        }

        private void SetMemoryDomain(int pos)
        {
            if (pos < Global.Emulator.MemoryDomains.Count)  //Sanity check
            {
                Watches.Domain = Global.Emulator.MemoryDomains[pos];
            }
            SetPlatformAndMemoryDomainLabel();
            Update();
        }

        private void SetPlatformAndMemoryDomainLabel()
        {
            string memoryDomain = Watches.Domain.ToString();
            systemID = Global.Emulator.SystemId;
            MemDomainLabel.Text = systemID + " " + memoryDomain;
        }

        #region Winform Events

        private void NewRamWatch_Load(object sender, EventArgs e)
        {

        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!AskSave())
            {
                return;
            }
            else
            {
                Close();
            }
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var file = WatchCommon.GetFileFromUser(Watches.CurrentFileName);
            if (file != null)
            {
                bool result = true;
                if (Watches.Changes)
                {
                    result = AskSave();
                }

                if (result)
                {
                    Watches.Load(file.FullName, details: true, append: false);
                    DisplayWatches();
                    MessageLabel.Text = Path.GetFileNameWithoutExtension(Watches.CurrentFileName);
                    UpdateWatchCount();
                    Global.Config.RecentWatches.Add(Watches.CurrentFileName);
                    SetMemoryDomain(WatchCommon.GetDomainPos(Watches.Domain.ToString()));
                }
            }
        }

        #endregion
    }
}
