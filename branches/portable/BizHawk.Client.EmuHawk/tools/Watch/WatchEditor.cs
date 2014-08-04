using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
    public partial class WatchEditor : Form, IHasShowDialog
	{
		public enum Mode { New, Duplicate, Edit };
		
		private readonly List<Watch> _watchList = new List<Watch>();
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
						AddressBox.SetFromRawInt(_watchList[0].Address ?? 0);
					}

					SetBigEndianCheckBox();
					DomainDropDown.Enabled = false;
					break;
			}
		}

		public void SetWatch(MemoryDomain domain, IEnumerable<Watch> watches = null, Mode mode = Mode.New)
		{
			if (watches != null)
			{
				_watchList.AddRange(watches);
			}

			_mode = mode;

			DomainDropDown.Items.Clear();
			DomainDropDown.Items.AddRange(Global.Emulator.MemoryDomains
				.Select(d => d.ToString())
				.ToArray());
			DomainDropDown.SelectedItem = Global.Emulator.MemoryDomains.MainMemory.ToString();

			SetTitle();
		}

		private void SetTitle()
		{
			switch(_mode)
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
				var domain = Global.Emulator.MemoryDomains.FirstOrDefault(d => d.Name == DomainDropDown.SelectedItem.ToString());
				if (domain != null)
				{
					AddressBox.SetHexProperties(domain.Size);
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
					DisplayTypeDropDown.Items.AddRange(ByteWatch.ValidTypes.ConvertAll(e => Watch.DisplayTypeToString(e)).ToArray());
					break;
				case 1:
					DisplayTypeDropDown.Items.AddRange(WordWatch.ValidTypes.ConvertAll(e => Watch.DisplayTypeToString(e)).ToArray());
					break;
				case 2:
					DisplayTypeDropDown.Items.AddRange(DWordWatch.ValidTypes.ConvertAll(e => Watch.DisplayTypeToString(e)).ToArray());
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

			var domain = Global.Emulator.MemoryDomains.FirstOrDefault(d => d.Name == DomainDropDown.SelectedItem.ToString()) ??
						 Global.Emulator.MemoryDomains.MainMemory;
			BigEndianCheckBox.Checked = domain.EndianType == MemoryDomain.Endian.Big;
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
					var domain = Global.Emulator.MemoryDomains.FirstOrDefault(d => d.Name == DomainDropDown.SelectedItem.ToString());
					var address = AddressBox.ToRawInt() ?? 0;
					var notes = NotesBox.Text;
					var type = Watch.StringToDisplayType(DisplayTypeDropDown.SelectedItem.ToString());
					var bigendian = BigEndianCheckBox.Checked;
					switch (SizeDropDown.SelectedIndex)
					{
						case 0:
							_watchList.Add(new ByteWatch(domain, address, type, bigendian, notes));
							break;
						case 1:
							_watchList.Add(new WordWatch(domain, address, type, bigendian, notes));
							break;
						case 2:
							_watchList.Add(new DWordWatch(domain, address, type, bigendian, notes));
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
								watch.Address ?? 0,
								watch.Size,
								watch.Type,
								watch.Notes,
								watch.BigEndian));
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
					var size = Watch.WatchSize.Byte;
					switch (SizeDropDown.SelectedIndex)
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

					_watchList[i] = Watch.GenerateWatch(
						_watchList[i].Domain,
						_watchList.Count == 1 ? AddressBox.ToRawInt() ?? 0 : _watchList[i].Address ?? 0,
						size,
						_watchList[i].Type,
						_watchList[i].Notes,
						_watchList[i].BigEndian
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
