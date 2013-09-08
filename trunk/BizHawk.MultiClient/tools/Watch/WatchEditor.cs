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

		private bool _changedSize = false;
		private bool _changedDisplayType = false;

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
				case Mode.Duplicate:
				case Mode.Edit:
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
					int x = DisplayTypeDropDown.Items.IndexOf(Watch.DisplayTypeToString(_watchList[0].Type));
					DisplayTypeDropDown.SelectedItem = DisplayTypeDropDown.Items[x];

					if (_watchList.Count > 1)
					{
						NotesBox.Enabled = false;
						NotesBox.Text = "";

						AddressBox.Enabled = false;
						AddressBox.Text = _watchList.Select(a => a.AddressString).Aggregate((addrStr, nextStr) => addrStr + ("," + nextStr));

						BigEndianCheckBox.ThreeState = true;

						if (_watchList.Select(s => s.Size).Distinct().Count() > 1)
						{
							DisplayTypeDropDown.Enabled = false;
						}
					}
					else
					{
						NotesBox.Text = (_watchList[0] as iWatchEntryDetails).Notes;
						AddressBox.Text = _watchList[0].AddressString;
					}

					SetBigEndianCheckBox();
					DomainDropDown.Enabled = false;
					break;
			}
		}

		public void SetWatch(MemoryDomain domain, List<Watch> watches = null, Mode mode = Mode.New)
		{
			if (watches != null)
			{
				_watchList.AddRange(watches);
			}
			_mode = mode;
			DoMemoryDomainDropdown(domain ?? Global.Emulator.MainMemory);
			SetTitle();
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
					DoEdit();
					break;
				case Mode.Duplicate:
					var tempWatchList = new List<Watch>();
					tempWatchList.AddRange(_watchList);
					_watchList.Clear();
					foreach (var watch in tempWatchList)
					{
						var newWatch = Watch.GenerateWatch(watch.Domain, watch.Address.Value, watch.Size, details: true);
						newWatch.Type = watch.Type;
						(newWatch as iWatchEntryDetails).Notes = (watch as iWatchEntryDetails).Notes;
						_watchList.Add(watch);
					}
					DoEdit();
					break;
			}

			Close();
		}

		private void DoEdit()
		{
			if (_changedSize = true)
			{
				for(int i = 0; i < _watchList.Count; i++)
				{
					Watch.WatchSize size = Watch.WatchSize.Byte;
					switch(SizeDropDown.SelectedIndex)
					{
						case 0:
							size = Watch.WatchSize.Byte;
							break;
						case 1:
							size = Watch.WatchSize.Word;
							break;
						case 2:
							size = Watch.WatchSize.DWord;
							break;
					}
					_watchList[i] = Watch.GenerateWatch(_watchList[i].Domain, _watchList[i].Address.Value, size, details: true);
				}
			}
			if (_changedDisplayType)
			{
				_watchList.ForEach(x => x.Type = Watch.StringToDisplayType(DisplayTypeDropDown.SelectedItem.ToString()));
			}
			if (!(BigEndianCheckBox.CheckState == CheckState.Indeterminate))
			{
				_watchList.ForEach(x => x.BigEndian = BigEndianCheckBox.Checked);
			}

			if (_watchList.Count == 1)
			{
				(_watchList[0] as iWatchEntryDetails).Notes = NotesBox.Text;
			}
		}

		private void DomainComboBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			SetAddressBoxProperties();
			SetBigEndianCheckBox();
			_changedSize = true;
			_changedDisplayType = true;
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

		#endregion
	}
}
