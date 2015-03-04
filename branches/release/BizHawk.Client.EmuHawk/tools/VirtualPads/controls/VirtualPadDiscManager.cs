using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Sony.PSX;

namespace BizHawk.Client.EmuHawk
{
	public partial class VirtualPadDiscManager : UserControl, IVirtualPadControl
	{
		public VirtualPadDiscManager(string[] buttonNames)
		{
			InitializeComponent();
			btnOpen.Name = buttonNames[0];
			btnClose.Name = buttonNames[1];
			_discSelectName = buttonNames[2];

			UpdateCoreAssociation();
		}

		string _discSelectName;
		object _ownerEmulator;
		public object OwnerEmulator
		{
			get { return _ownerEmulator; }
			set
			{
				_ownerEmulator = value;
				UpdateValues();
			}
		}

		object lastCoreOwner;

		void UpdateCoreAssociation()
		{
			if (lastCoreOwner == OwnerEmulator)
				return;

			lastCoreOwner = OwnerEmulator;

			if (!(OwnerEmulator is Octoshock))
				return;

			var psx = OwnerEmulator as Octoshock;
			List<string> buttons = new List<string>();
			buttons.Add("- NONE -");
			buttons.AddRange(psx.HackyDiscButtons);

			lvDiscs.Items.Clear();

			int idx = 0;
			foreach (var button in buttons)
			{
				var lvi = new ListViewItem();
				lvi.Text = idx.ToString();
				lvi.SubItems.Add(button);
				lvDiscs.Items.Add(lvi);
				idx++;
			}
		}


		#region IVirtualPadControl

		public void Clear()
		{
		}

		public void UpdateValues()
		{
			UpdateCoreAssociation();
			if (OwnerEmulator is Octoshock)
			{
				var psx = OwnerEmulator as Octoshock;
				bool eject = psx.CurrentTrayOpen;
				bool enableDiscs = eject;
				bool refreshDiscs = true;

				//special logic: if this is frame 0, we can begin in any state
				if (psx.Frame == 0)
				{
					lblTimeZero.Visible = true;
					btnOpen.Enabled = true;
					btnClose.Enabled = true;

					//if neither button is picked, start with 'closed' selected
					//(kind of a hack for the initial update)
					if (!btnClose.Checked && !btnOpen.Checked)
					{
						btnClose.Checked = true;
					}
					else
					{
						//while we're here, make sure this only happens the first time
						refreshDiscs = false;
					}

					enableDiscs = btnOpen.Checked;

					//since user hasnt ever needed to set the disc, make sure it's set here
					//UPDATE: do it below
					//Global.StickyXORAdapter.SetFloat(_discSelectName, psx.CurrentDiscIndexMounted);	
				}
				else
				{
					lblTimeZero.Visible = false;
					btnOpen.Enabled = !eject;
					btnClose.Enabled = eject;

					if (!btnOpen.Enabled) btnOpen.Checked = false;
					if (!btnClose.Enabled) btnClose.Checked = false;
				}

				//if we're not ejected, then the disc is frozen in the current configuration
				lvDiscs.Enabled = enableDiscs;
				if (!eject && refreshDiscs)
				{
					lvDiscs.SelectedIndices.Clear();
					lvDiscs.SelectedIndices.Add(psx.CurrentDiscIndexMounted);
				}
			}

			//make sure we try to keep something selected here, for clarity.
			//but maybe later we'll just make it so that unselecting means no disc and dont display the disc 0
			if (lvDiscs.SelectedIndices.Count == 0)
				lvDiscs.SelectedIndices.Add(0);
		}

		public void Set(IController controller)
		{
			//controller.GetFloat("Disc Select")
		}

		public bool ReadOnly { get; set; }

		#endregion //IVirtualPadControl

		private void groupBox1_Enter(object sender, EventArgs e)
		{

		}

		private void lvDiscs_SelectedIndexChanged(object sender, EventArgs e)
		{
			//not a valid way to fight unselection, it results in craptons of ping-ponging logic and eventual malfunction
			//if (lvDiscs.SelectedIndices.Count == 0)
			//  lvDiscs.SelectedIndices.Add(0);
			//Global.StickyXORAdapter.SetFloat(_discSelectName, lvDiscs.SelectedIndices[0]);

			//emergency measure: if no selection, set no disc
			if (lvDiscs.SelectedIndices.Count == 0)
			  Global.StickyXORAdapter.SetFloat(_discSelectName, 0);	
			else Global.StickyXORAdapter.SetFloat(_discSelectName, lvDiscs.SelectedIndices[0]);
		}

		private void btnClose_Click(object sender, EventArgs e)
		{
			if (lblTimeZero.Visible)
			{
				btnOpen.Checked = !btnClose.Checked;
				UpdateValues();
			}
		}

		private void btnOpen_Click(object sender, EventArgs e)
		{
			if (lblTimeZero.Visible)
			{
				btnClose.Checked = !btnOpen.Checked;
				UpdateValues();
			}
		}

	



	}
}
