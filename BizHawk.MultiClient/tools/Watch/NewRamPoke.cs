using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace BizHawk.MultiClient
{
	public partial class NewRamPoke : Form
	{
		private List<Watch> _watchList = new List<Watch>();
		private bool _loading = true;
		private string _addressFormatStr = "{0:X2}";

		private bool _changedSize = false;
		private bool _changedDisplayType = false;

		public List<Watch> Watches { get { return _watchList; } }
		public Point InitialLocation = new Point(0, 0);

		public NewRamPoke()
		{
			InitializeComponent();
		}

		public void SetWatch(List<Watch> watches = null)
		{
			if (watches != null)
			{
				_watchList.AddRange(watches);
			}

			DoMemoryDomainDropdown(_watchList.Count == 1 ? _watchList[0].Domain : Global.Emulator.MainMemory);
			SetTitle();
		}

		private void DoMemoryDomainDropdown(MemoryDomain startDomain)
		{
			DomainDropDown.Items.Clear();
			if (Global.Emulator.MemoryDomains.Count > 0)
			{
				foreach (MemoryDomain domain in Global.Emulator.MemoryDomains)
				{
					var result = DomainDropDown.Items.Add(domain.ToString());
					if (domain.Name == startDomain.Name)
					{
						DomainDropDown.SelectedIndex = result;
					}
				}
			}
		}

		private void RamPoke_Load(object sender, EventArgs e)
		{
			if (InitialLocation.X > 0 || InitialLocation.Y > 0)
			{
				Location = InitialLocation;
			}

			if (_watchList.Count == 0)
			{
				DoMemoryDomainDropdown(Global.Emulator.MainMemory);
			}
			else
			{
				if (_watchList.Count > 1)
				{
					AddressBox.Enabled = false;
					AddressBox.Text = _watchList.Select(a => a.AddressString).Aggregate((addrStr, nextStr) => addrStr + ("," + nextStr));
					BigEndianCheckBox.ThreeState = true;
					DoMemoryDomainDropdown(Global.Emulator.MainMemory);
					DomainDropDown.Enabled = false;
					SetBigEndianCheckBox();
					//TODO: Mixed displaytypes are a problem for a value box!
					//TODO: Mixed size types are a problem
				}
				else
				{
					ValueHexLabel.Text = _watchList[0].Type == Watch.DisplayType.Hex ? "0x" : String.Empty;

					AddressBox.Text = _watchList[0].AddressString;

					switch (_watchList[0].Size)
					{
						case Watch.WatchSize.Byte:
							SizeDropDown.SelectedItem = SizeDropDown.Items[0];
							break;
						case Watch.WatchSize.Word:
							SizeDropDown.SelectedItem = SizeDropDown.Items[1];
							break;
						case Watch.WatchSize.DWord:
							SizeDropDown.SelectedItem = SizeDropDown.Items[2];
							break;
					}

					//TODO: type

					DoMemoryDomainDropdown(_watchList[0].Domain);
					SetBigEndianCheckBox();
				}
			}
		}

		private void SetAddressBoxProperties()
		{
			if (!_loading)
			{
				var domain = Global.Emulator.MemoryDomains.FirstOrDefault(d => d.Name == DomainDropDown.SelectedItem.ToString());
				if (domain != null)
				{
					AddressBox.MaxLength = IntHelpers.GetNumDigits(domain.Size - 1);
					_addressFormatStr = "{0:X" + AddressBox.MaxLength.ToString() + "}";
					AddressBox.Text = String.Format(_addressFormatStr, 0);
				}
			}
		}

		private void SetBigEndianCheckBox()
		{
			if (_watchList != null)
			{
				if (_watchList.Count > 1)
				{
					//Aggregate state
					var hasBig = _watchList.Any(x => x.BigEndian);
					var hasLittle = _watchList.Any(x => x.BigEndian == false);

					if (hasBig && hasLittle)
					{
						BigEndianCheckBox.Checked = true;
						BigEndianCheckBox.CheckState = CheckState.Indeterminate;
					}
					else if (hasBig)
					{
						BigEndianCheckBox.Checked = true;
					}
					else
					{
						BigEndianCheckBox.Checked = false;
					}
				}
				else if (_watchList.Count == 1)
				{
					BigEndianCheckBox.Checked = _watchList[0].BigEndian;
					return;
				}
			}

			var domain = Global.Emulator.MemoryDomains.FirstOrDefault(d => d.Name == DomainDropDown.SelectedItem.ToString());
			if (domain == null)
			{
				domain = Global.Emulator.MainMemory;
			}
			BigEndianCheckBox.Checked = domain.Endian == Endian.Big ? true : false;

			_loading = false;
			SetAddressBoxProperties();
		}

		private void SetTitle()
		{
			Text = "Ram Poke - " + _watchList[0].Domain;
		}

		private void SetDisplayTypes()
		{
			DisplayTypeDropDown.Items.Clear();
			switch (SizeDropDown.SelectedIndex)
			{
				default:
				case 0:
					DisplayTypeDropDown.Items.AddRange(ByteWatch.ValidTypes.ConvertAll<string>(e => Watch.DisplayTypeToString(e)).ToArray());
					break;
				case 1:
					DisplayTypeDropDown.Items.AddRange(WordWatch.ValidTypes.ConvertAll<string>(e => Watch.DisplayTypeToString(e)).ToArray());
					break;
				case 2:
					DisplayTypeDropDown.Items.AddRange(DWordWatch.ValidTypes.ConvertAll<string>(e => Watch.DisplayTypeToString(e)).ToArray());
					break;
			}
			DisplayTypeDropDown.SelectedItem = DisplayTypeDropDown.Items[0];
		}

		#region Events

		private void Cancel_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
			Close();
		}

		private void OK_Click(object sender, EventArgs e)
		{
			//TODO

			OutputLabel.Text = ValueBox.Text + " written to " + AddressBox.Text;
		}

		private void SizeDropDown_SelectedIndexChanged(object sender, EventArgs e)
		{
			SetDisplayTypes();
			_changedSize = true;

			if (!DisplayTypeDropDown.Enabled)
			{
				DisplayTypeDropDown.Enabled = true;
				_changedDisplayType = true;
			}
		}

		private void DisplayTypeDropDown_SelectedIndexChanged(object sender, EventArgs e)
		{
			_changedDisplayType = true;
		}

		private void DomainComboBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			SetAddressBoxProperties();
			SetBigEndianCheckBox();
			_changedSize = true;
			_changedDisplayType = true;
		}

		#endregion
	}
}
