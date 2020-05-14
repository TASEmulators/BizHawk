using System;
using System.Collections.Generic;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Sony.PSX;

namespace BizHawk.Client.EmuHawk
{
	public partial class VirtualPadDiscManager : UserControl, IVirtualPadControl
	{
		public VirtualPadDiscManager(IReadOnlyList<string> buttonNames)
		{
			InitializeComponent();
			btnOpen.Name = buttonNames[0];
			btnClose.Name = buttonNames[1];
			_discSelectName = buttonNames[2];

			UpdateCoreAssociation();
		}

		private readonly string _discSelectName;
		private object _lastCoreOwner;
		private object _ownerEmulator;

		public object OwnerEmulator
		{
			get => _ownerEmulator;
			set
			{
				_ownerEmulator = value;
				UpdateValues();
			}
		}

		private void UpdateCoreAssociation()
		{
			if (_lastCoreOwner == OwnerEmulator)
			{
				return;
			}

			_lastCoreOwner = OwnerEmulator;

			if (!(OwnerEmulator is Octoshock psx))
			{
				return;
			}

			var buttons = new List<string> { "- NONE -" };
			buttons.AddRange(psx.HackyDiscButtons);

			lvDiscs.Items.Clear();

			int idx = 0;
			foreach (var button in buttons)
			{
				var lvi = new ListViewItem { Text = idx.ToString() };
				lvi.SubItems.Add(button);
				lvDiscs.Items.Add(lvi);
				idx++;
			}
		}




		public void Clear()
		{
		}

		public void UpdateValues()
		{
			UpdateCoreAssociation();
			if (OwnerEmulator is Octoshock psx)
			{
				bool eject = psx.CurrentTrayOpen;
				bool enableDiscs = eject;
				bool refreshDiscs = true;

				//special logic: if this is frame 0, we can begin in any state
				if (psx.Frame == 0)
				{
					lblTimeZero.Visible = true;
					btnOpen.Enabled = true;
					btnClose.Enabled = true;

					// if neither button is picked, start with 'closed' selected
					// (kind of a hack for the initial update)
					if (!btnClose.Checked && !btnOpen.Checked)
					{
						btnClose.Checked = true;
					}
					else
					{
						// while we're here, make sure this only happens the first time
						refreshDiscs = false;
					}

					enableDiscs = btnOpen.Checked;
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

			// make sure we try to keep something selected here, for clarity.
			// but maybe later we'll just make it so that unselecting means no disc and don't display the disc 0
			if (lvDiscs.SelectedIndices.Count == 0)
				lvDiscs.SelectedIndices.Add(0);
		}

		public void Set(IController controller)
		{
			//controller.AxisValue("Disc Select")
		}

		public bool ReadOnly { get; set; }



		private void lvDiscs_SelectedIndexChanged(object sender, EventArgs e)
		{
			// emergency measure: if no selection, set no disc
			Global.InputManager.StickyXorAdapter.SetAxis(_discSelectName, lvDiscs.SelectedIndices.Count == 0 ? 0 : lvDiscs.SelectedIndices[0]);
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
