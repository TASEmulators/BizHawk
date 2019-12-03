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
		public enum Mode { New, Duplicate, Edit }

		public Emu.IMemoryDomains MemoryDomains { get; set; }

		private Mode _mode = Mode.New;
		private bool _loading = true;

		private bool _changedSize;
		private bool _changedDisplayType;

		public List<Watch> Watches { get; } = new List<Watch>();

		public Point InitialLocation { get; set;  } = new Point(0, 0);

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
					switch (Watches[0].Size)
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

					var index = DisplayTypeDropDown.Items.IndexOf(Watch.DisplayTypeToString(Watches[0].Type));
					DisplayTypeDropDown.SelectedItem = DisplayTypeDropDown.Items[index];

					if (Watches.Count > 1)
					{
						NotesBox.Enabled = false;
						NotesBox.Text = "";

						AddressBox.Enabled = false;
						AddressBox.Text = Watches.Select(a => a.AddressString).Aggregate((addrStr, nextStr) => $"{addrStr},{nextStr}");

						BigEndianCheckBox.ThreeState = true;

						if (Watches.Select(s => s.Size).Distinct().Count() > 1)
						{
							DisplayTypeDropDown.Enabled = false;
						}
					}
					else
					{
						NotesBox.Text = Watches[0].Notes;
						AddressBox.SetFromLong(Watches[0].Address);
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
				Watches.AddRange(watches);
			}

			_mode = mode;

			DomainDropDown.Items.Clear();
			DomainDropDown.Items.AddRange(MemoryDomains
				.Select(d => d.ToString())
				.Cast<object>()
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
					Text = $"Edit {(Watches.Count == 1 ? "Watch" : "Watches")}";
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
					foreach (DisplayType t in ByteWatch.ValidTypes)
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

			DisplayTypeDropDown.SelectedItem = DisplayTypeDropDown.Items.Contains(oldType)
				? oldType
				: DisplayTypeDropDown.Items[0];
		}

		private void SetBigEndianCheckBox()
		{
			if (Watches.Count > 1)
			{
				// Aggregate state
				var hasBig = Watches.Any(x => x.BigEndian);
				var hasLittle = Watches.Any(x => x.BigEndian == false);

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
			else if (Watches.Count == 1)
			{
				BigEndianCheckBox.Checked = Watches[0].BigEndian;
				return;
			}

			var domain = MemoryDomains.FirstOrDefault(d => d.Name == DomainDropDown.SelectedItem.ToString())
				?? MemoryDomains.MainMemory;
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
					var bigEndian = BigEndianCheckBox.Checked;
					switch (SizeDropDown.SelectedIndex)
					{
						case 0:
							Watches.Add(Watch.GenerateWatch(domain, address, WatchSize.Byte, type, bigEndian, notes));
							break;
						case 1:
							Watches.Add(Watch.GenerateWatch(domain, address, WatchSize.Word, type, bigEndian, notes));
							break;
						case 2:
							Watches.Add(Watch.GenerateWatch(domain, address, WatchSize.DWord, type, bigEndian, notes));
							break;
					}

					break;
				case Mode.Edit:
					DoEdit();
					break;
				case Mode.Duplicate:
					var tempWatchList = new List<Watch>();
					tempWatchList.AddRange(Watches);
					Watches.Clear();
					foreach (var watch in tempWatchList)
					{
						Watches.Add(Watch.GenerateWatch(
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
			if (Watches.Count == 1)
			{
				Watches[0].Notes = NotesBox.Text;
			}

			if (_changedSize)
			{
				for (var i = 0; i < Watches.Count; i++)
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

					Watches[i] = Watch.GenerateWatch(
						Watches[i].Domain,
						Watches.Count == 1 ? AddressBox.ToRawInt() ?? 0 : Watches[i].Address,
						size,
						Watches[i].Type,
						Watches[i].BigEndian,
						Watches[i].Notes);
				}
			}

			if (_changedDisplayType)
			{
				Watches.ForEach(x => x.Type = Watch.StringToDisplayType(DisplayTypeDropDown.SelectedItem.ToString()));
			}

			if (BigEndianCheckBox.CheckState != CheckState.Indeterminate)
			{
				Watches.ForEach(x => x.BigEndian = BigEndianCheckBox.Checked);
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
