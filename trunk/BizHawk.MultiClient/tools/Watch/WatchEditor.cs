using System;
using System.Collections.Generic;
using System.Drawing;
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

			switch (_mode)
			{
				default:
				case Mode.New:
					SizeDropDown.SelectedItem = SizeDropDown.Items[0];
					break;
			}
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

			if (DomainDropDown.SelectedIndex == null)
			{
				DomainDropDown.SelectedItem = DomainDropDown.Items[0];
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

			switch (_mode)
			{
				default:
				case Mode.New:
					var domain = Global.Emulator.MemoryDomains.FirstOrDefault(d => d.Name == DomainDropDown.SelectedItem.ToString());
					var address = (AddressBox as HexTextBox).ToInt();
					var notes = NotesBox.Text;
					var type = Watch.StringToDisplayType(DisplayTypeDropDown.SelectedItem.ToString());
					var bigendian = BigEndianCheckBox.Checked;
					switch (SizeDropDown.SelectedIndex)
					{
						case 0:
							_watchList.Add(new DetailedByteWatch(domain, address)
								{
									Notes = notes,
									Type = type,
									BigEndian = bigendian,
								}
							);
							break;
						case 1:
							_watchList.Add(new DetailedWordWatch(domain, address)
								{
									Notes = notes,
									Type = type,
									BigEndian = bigendian,
								}
							);
							break;
						case 2:
							_watchList.Add(new DetailedDWordWatch(domain, address)
								{
									Notes = notes,
									Type = type,
									BigEndian = bigendian,
								}
							);
							break;
					}
					break;
				case Mode.Edit:
					break;
				case Mode.Duplicate:
					break;
			}

			Close();
		}

		private void DomainComboBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			SetAddressBoxProperties();
			SetBigEndianCheckBox();
		}

		private void SizeDropDown_SelectedIndexChanged(object sender, EventArgs e)
		{
			SetDisplayTypes();
			
		}

		#endregion

		
	}
}
