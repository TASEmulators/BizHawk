using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace BizHawk.MultiClient
{
	public partial class WatchEditor : Form
	{
		public enum Mode { New, Duplicate, Edit };
		
		private List<Watch> _watchList = new List<Watch>();
		private Mode _mode = Mode.New;
		private bool _loading = true;
		private string _addressFormatStr = "{0:X2}";

		public Mode EditorMode { get { return _mode; } }
		public List<Watch> Watches { get { return _watchList; } }
		public Point InitialLocation = new Point(0, 0);

		public WatchEditor()
		{
			InitializeComponent();
		}

		private void RamWatchNewWatch_Load(object sender, EventArgs e)
		{
			if (InitialLocation.X > 0 || InitialLocation.Y > 0)
			{
				Location = InitialLocation;
			}
			_loading = false;
			SetAddressBoxProperties();
		}

		public void SetWatch(MemoryDomain domain = null, List<Watch> watches = null, Mode mode = Mode.New)
		{
			if (watches != null)
			{
				_watchList.AddRange(watches);
			}
			SetTitle();
			DoMemoryDomainDropdown(domain ?? Global.Emulator.MainMemory);
		}

		private void SetTitle()
		{
			switch(_mode)
			{
				default:
				case WatchEditor.Mode.New:
					Text = "New Watch";
					break;
				case WatchEditor.Mode.Edit:
					Text = "Edit Watch" + (_watchList.Count > 1 ? "es" : "");
					break;
				case WatchEditor.Mode.Duplicate:
					Text = "Duplicate Watch";
					break;
			}
		}

		private void DoMemoryDomainDropdown(MemoryDomain startDomain)
		{
			DomainComboBox.Items.Clear();
			if (Global.Emulator.MemoryDomains.Count > 0)
			{
				foreach (MemoryDomain domain in Global.Emulator.MemoryDomains)
				{
					var result = DomainComboBox.Items.Add(domain.ToString());
					if (domain.Name == startDomain.Name)
					{
						DomainComboBox.SelectedIndex = result;
					}
				}
			}
		}

		private void SetAddressBoxProperties()
		{
			if (!_loading)
			{
				var domain = Global.Emulator.MemoryDomains.FirstOrDefault(d => d.Name == DomainComboBox.SelectedItem.ToString());
				if (domain != null)
				{
					AddressBox.MaxLength = IntHelpers.GetNumDigits(domain.Size - 1);
					_addressFormatStr = "{0:X" + AddressBox.MaxLength.ToString() + "}";
					AddressBox.Text = String.Format(_addressFormatStr, 0);
				}
			}
		}

		#region Events

		private void Cancel_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
			Close();
		}

		private void OK_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.OK;

			//TODO

			Close();
		}

		private void DomainComboBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			SetAddressBoxProperties();
		}

		#endregion
	}
}
