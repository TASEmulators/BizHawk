using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

using BizHawk.Client.Common;
using Emu = BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class WatchEditor : Form
	{
		public enum Mode { New, Duplicate, Edit };

		private readonly List<Watch> _watchList = new List<Watch>();

		public Emu.IMemoryDomains MemoryDomains { get; set; }

		private Mode _mode = Mode.New;
		private bool _loading = true;

		private bool _changedSize;
		private bool _changedDisplayType;

		public Mode EditorMode { get { return _mode; } }
		public List<Watch> Watches { get { return _watchList; } }
		public Point InitialLocation = new Point(0, 0);

		public WatchEditor()
		{
			_changedDisplayType = false;
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
					switch (MemoryDomains.First().WordSize)
					{
						default:
						case 1:
							SizeDropDown.SelectedItem = SizeDropDown.Items[0];
							break;
						case 2:
							SizeDropDown.SelectedItem = SizeDropDown.Items[1];
							break;
						case 4:
							SizeDropDown.SelectedItem = SizeDropDown.Items[2];
							break;
					}
					break;
				case Mode.Duplicate:
				case Mode.Edit:
					switch (_watchList[0].Size)
					{
						case WatchSize.Byte:
							SizeDropDown.SelectedItem = SizeDropDown.Items[0];
							break;
						case WatchSize.Word:
							SizeDropDown.SelectedItem = SizeDropDown.Items[1];
							break;
						case WatchSize.DWord:
							SizeDropDown.SelectedItem = SizeDropDown.Items[2];
							break;
					}

					var index = DisplayTypeDropDown.Items.IndexOf(Watch.DisplayTypeToString(_watchList[0].Type));
					DisplayTypeDropDown.SelectedItem = DisplayTypeDropDown.Items[index];

					if (_watchList.Count > 1)
					{
						NotesBox.Enabled = false;
						NotesBox.Text = String.Empty;

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
						NotesBox.Text = _watchList[0].Notes;
						AddressBox.SetFromLong(_watchList[0].Address);
					}

					SetBigEndianCheckBox();
					DomainDropDown.Enabled = false;
					break;
			}
		}

		public void SetWatch(Emu.MemoryDomain domain, IEnumerable<Watch> watches = null, Mode mode = Mode.New)
		{
			if (watches != null)
			{
				_watchList.AddRange(watches);
			}

			_mode = mode;

			DomainDropDown.Items.Clear();
			DomainDropDown.Items.AddRange(MemoryDomains
				.Select(d => d.ToString())
				.ToArray());
			DomainDropDown.SelectedItem = domain.ToString();

			SetTitle();
		}

		private void SetTitle()
		{
			switch (_mode)
			{
				default:
				case Mode.New:
					Text = "New Watch";
					break;
				case Mode.Edit:
					Text = "Edit Watch" + (_watchList.Count > 1 ? "es" : "");
					break;
				case Mode.Duplicate:
					Text = "Duplicate Watch";
					break;
			}
		}

		private void SetAddressBoxProperties()
		{
			if (!_loading)
			{
				var domain = MemoryDomains.FirstOrDefault(d => d.Name == DomainDropDown.SelectedItem.ToString());
				if (domain != null)
				{
					AddressBox.SetHexProperties(domain.Size);
				}
			}
		}

		private void SetDisplayTypes()
		{
			string oldType = DisplayTypeDropDown.Text;
			DisplayTypeDropDown.Items.Clear();
			switch (SizeDropDown.SelectedIndex)
			{
				default:
				case 0:
					foreach(DisplayType t in ByteWatch.ValidTypes)
					{
						DisplayTypeDropDown.Items.Add(Watch.DisplayTypeToString(t));
                    }
					break;
				case 1:
					foreach (DisplayType t in WordWatch.ValidTypes)
					{
						DisplayTypeDropDown.Items.Add(Watch.DisplayTypeToString(t));
					}
					break;
				case 2:
					foreach (DisplayType t in DWordWatch.ValidTypes)
					{
						DisplayTypeDropDown.Items.Add(Watch.DisplayTypeToString(t));
					}
					break;
			}

			if (DisplayTypeDropDown.Items.Contains(oldType))
				DisplayTypeDropDown.SelectedItem = oldType;
			else
				DisplayTypeDropDown.SelectedItem = DisplayTypeDropDown.Items[0];
		}

		private void SetBigEndianCheckBox()
		{
			if (_watchList != null)
			{
				if (_watchList.Count > 1)
				{
					// Aggregate state
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

			var domain = MemoryDomains.FirstOrDefault(d => d.Name == DomainDropDown.SelectedItem.ToString()) ??
						 MemoryDomains.MainMemory;
			BigEndianCheckBox.Checked = domain.EndianType == Emu.MemoryDomain.Endian.Big;
		}

		#region Events

		private void Cancel_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
			Close();
		}

		private void Ok_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.OK;

			switch (_mode)
			{
				default:
				case Mode.New:
					var domain = MemoryDomains.FirstOrDefault(d => d.Name == DomainDropDown.SelectedItem.ToString());
					var address = AddressBox.ToLong() ?? 0;
					var notes = NotesBox.Text;
					var type = Watch.StringToDisplayType(DisplayTypeDropDown.SelectedItem.ToString());
					var bigendian = BigEndianCheckBox.Checked;
					switch (SizeDropDown.SelectedIndex)
					{
						case 0:
							_watchList.Add(Watch.GenerateWatch(domain, address, WatchSize.Byte, type, bigendian, notes));
							break;
						case 1:
							_watchList.Add(Watch.GenerateWatch(domain, address, WatchSize.Word, type, bigendian, notes));
							break;
						case 2:
							_watchList.Add(Watch.GenerateWatch(domain, address, WatchSize.DWord, type, bigendian, notes));
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
						_watchList.Add(Watch.GenerateWatch(
								watch.Domain,
								watch.Address,
								watch.Size,
								watch.Type,
								watch.BigEndian,
								watch.Notes));
					}

					DoEdit();
					break;
			}

			Close();
		}

		private void DoEdit()
		{
			if (_watchList.Count == 1)
			{
				_watchList[0].Notes = NotesBox.Text;
			}

			if (_changedSize)
			{
				for (var i = 0; i < _watchList.Count; i++)
				{
					var size = WatchSize.Byte;
					switch (SizeDropDown.SelectedIndex)
					{
						case 0:
							size = WatchSize.Byte;
							break;
						case 1:
							size = WatchSize.Word;
							break;
						case 2:
							size = WatchSize.DWord;
							break;
					}

					_watchList[i] = Watch.GenerateWatch(
						_watchList[i].Domain,
						_watchList.Count == 1 ? AddressBox.ToRawInt() ?? 0 : _watchList[i].Address,
						size,
						_watchList[i].Type,						
						_watchList[i].BigEndian,
						_watchList[i].Notes
                    );
				}
			}

			if (_changedDisplayType)
			{
				_watchList.ForEach(x => x.Type = Watch.StringToDisplayType(DisplayTypeDropDown.SelectedItem.ToString()));
			}

			if (BigEndianCheckBox.CheckState != CheckState.Indeterminate)
			{
				_watchList.ForEach(x => x.BigEndian = BigEndianCheckBox.Checked);
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
