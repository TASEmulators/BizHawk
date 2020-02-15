using System;
using System.Linq;
using System.Windows.Forms;

using BizHawk.Client.Common;

using Emu = BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class CheatEdit : UserControl
	{
		public Emu.IMemoryDomains MemoryDomains { get; set; }

		public CheatEdit()
		{
			InitializeComponent();
			AddressBox.Nullable = false;
			ValueBox.Nullable = false;
		}

		#region Privates

		private const string HexInd = "0x";

		private Cheat _cheat;
		private bool _loading;
		private bool _editMode;

		private Action _addCallback;
		private Action _editCallback;

		private void CheatEdit_Load(object sender, EventArgs e)
		{
			Restart();
		}

		public void Restart()
		{
			if (MemoryDomains != null) // the designer needs this check
			{
				DomainDropDown.Items.Clear();
				DomainDropDown.Items.AddRange(MemoryDomains
					.Where(d => d.Writable)
					.Select(d => d.ToString())
					.ToArray());

				DomainDropDown.SelectedItem = MemoryDomains.HasSystemBus
					? MemoryDomains.SystemBus.ToString()
					: MemoryDomains.MainMemory.ToString();
			}

			SetFormToDefault();
		}

		private void SetFormToCheat()
		{
			_loading = true;
			SetSizeSelected(_cheat.Size);
			PopulateTypeDropdown();
			SetTypeSelected(_cheat.Type);
			SetDomainSelected(_cheat.Domain);

			AddressBox.SetHexProperties(_cheat.Domain.Size);

			ValueBox.ByteSize =
				CompareBox.ByteSize =
				_cheat.Size;

			ValueBox.Type =
				CompareBox.Type =
				_cheat.Type;

			ValueHexIndLabel.Text =
				CompareHexIndLabel.Text =
				_cheat.Type == DisplayType.Hex ? HexInd : "";

			BigEndianCheckBox.Checked = _cheat.BigEndian ?? false;

			NameBox.Text = _cheat.Name;
			AddressBox.Text = _cheat.AddressStr;
			ValueBox.Text = _cheat.ValueStr;
			CompareBox.Text = _cheat.Compare.HasValue ? _cheat.CompareStr : "";

			if (_cheat.ComparisonType.Equals(Cheat.CompareType.None))
			{
				CompareTypeDropDown.SelectedIndex = 0;
			}
			else
			{
				CompareTypeDropDown.SelectedIndex = (int)_cheat.ComparisonType - 1;
			}

			CheckFormState();
			if (!_cheat.Compare.HasValue)
			{
				CompareBox.Text = ""; // Necessary hack until WatchValueBox.ToRawInt() becomes nullable
			}

			_loading = false;
		}

		private void SetFormToDefault()
		{
			_loading = true;
			SetSizeSelected(WatchSize.Byte);
			PopulateTypeDropdown();

			NameBox.Text = "";

			if (MemoryDomains != null)
			{
				AddressBox.SetHexProperties(MemoryDomains.SystemBus.Size);
			}

			ValueBox.ByteSize = 
				CompareBox.ByteSize =
				WatchSize.Byte;

			ValueBox.Type = 
				CompareBox.Type =
				DisplayType.Hex;

			ValueBox.ResetText();
			CompareBox.ResetText();

			ValueHexIndLabel.Text =
				CompareHexIndLabel.Text =
				HexInd;

			BigEndianCheckBox.Checked = false;

			SetTypeSelected(DisplayType.Hex);

			CheckFormState();
			CompareBox.Text = ""; // TODO: A needed hack until WatchValueBox.ToRawInt() becomes nullable
			_loading = false;
		}

		private void SetSizeSelected(WatchSize size)
		{
			switch (size)
			{
				default:
				case WatchSize.Byte:
					SizeDropDown.SelectedIndex = 0;
					break;
				case WatchSize.Word:
					SizeDropDown.SelectedIndex = 1;
					break;
				case WatchSize.DWord:
					SizeDropDown.SelectedIndex = 2;
					break;
			}
		}

		private void SetTypeSelected(Common.DisplayType type)
		{
			foreach (var item in DisplayTypeDropDown.Items)
			{
				if (item.ToString() == Watch.DisplayTypeToString(type))
				{
					DisplayTypeDropDown.SelectedItem = item;
					return;
				}
			}
		}

		private void SetDomainSelected(Emu.MemoryDomain domain)
		{
			foreach (var item in DomainDropDown.Items)
			{
				if (item.ToString() == domain.Name)
				{
					DomainDropDown.SelectedItem = item;
					return;
				}
			}
		}

		private void PopulateTypeDropdown()
		{
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

			DisplayTypeDropDown.SelectedItem = DisplayTypeDropDown.Items[0];
		}

		private void CheckFormState()
		{
			var valid = !string.IsNullOrWhiteSpace(AddressBox.Text) && !string.IsNullOrWhiteSpace(ValueBox.Text);
			AddButton.Enabled = valid;
			EditButton.Enabled = _editMode && valid;
		}

		private void SizeDropDown_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (!_loading)
			{
				PopulateTypeDropdown();

				ValueBox.ByteSize = 
					CompareBox.ByteSize =
					GetCurrentSize();
			}
		}

		private void DomainDropDown_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (!_loading)
			{
				var domain = MemoryDomains[DomainDropDown.SelectedItem.ToString()];
				AddressBox.SetHexProperties(domain.Size);
			}
		}

		private WatchSize GetCurrentSize()
		{
			switch (SizeDropDown.SelectedIndex)
			{
				default:
				case 0:
					return WatchSize.Byte;
				case 1:
					return WatchSize.Word;
				case 2:
					return WatchSize.DWord;
			}
		}

		private void DisplayTypeDropDown_SelectedIndexChanged(object sender, EventArgs e)
		{
			ValueBox.Type =
				CompareBox.Type =
				Watch.StringToDisplayType(DisplayTypeDropDown.SelectedItem.ToString());
		}

		private void AddButton_Click(object sender, EventArgs e)
		{
			_addCallback?.Invoke();
		}

		private void EditButton_Click(object sender, EventArgs e)
		{
			_editCallback?.Invoke();
		}

		#endregion

		#region API

		public void SetCheat(Cheat cheat)
		{
			_editMode = true;
			_cheat = cheat;
			if (cheat.IsSeparator)
			{
				SetFormToDefault();
			}
			else
			{
				SetFormToCheat();
			}
		}

		public void ClearForm()
		{
			_cheat = Cheat.Separator;
			_editMode = false;
			SetFormToDefault();
		}

		public Cheat OriginalCheat => _cheat;

		public Cheat GetCheat()
		{
			var domain = MemoryDomains[DomainDropDown.SelectedItem.ToString()];
			var address = AddressBox.ToRawInt().Value;
			if (address < domain.Size)
			{
				var watch = Watch.GenerateWatch(
					MemoryDomains[DomainDropDown.SelectedItem.ToString()],
					AddressBox.ToRawInt().Value,
					GetCurrentSize(),
					Watch.StringToDisplayType(DisplayTypeDropDown.SelectedItem.ToString()),
					BigEndianCheckBox.Checked,
					NameBox.Text);

				Cheat.CompareType comparisonType;
				switch (CompareTypeDropDown.SelectedItem.ToString())
				{
					case "":
						comparisonType = Cheat.CompareType.None;
						break;
					case "=":
						comparisonType = Cheat.CompareType.Equal;
						break;
					case ">":
						comparisonType = Cheat.CompareType.GreaterThan;
						break;
					case ">=":
						comparisonType = Cheat.CompareType.GreaterThanOrEqual;
						break;
					case "<":
						comparisonType = Cheat.CompareType.LessThan;
						break;
					case "<=":
						comparisonType = Cheat.CompareType.LessThanOrEqual;
						break;
					case "!=":
						comparisonType = Cheat.CompareType.NotEqual;
						break;
					default:
						comparisonType = Cheat.CompareType.None;
						break;
				}

				return new Cheat(
					watch,
					ValueBox.ToRawInt().Value,
					CompareBox.ToRawInt() == null ? null : (int?)CompareBox.ToRawInt().Value,
					true,
					comparisonType);
			}
			else
			{
				MessageBox.Show($"{address} is not a valid address for the domain {domain.Name}", "Index out of range", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return Cheat.Separator;
			}
		}

		public void SetAddEvent(Action addCallback)
		{
			_addCallback = addCallback;
		}

		public void SetEditEvent(Action editCallback)
		{
			_editCallback = editCallback;
		}

		#endregion

		private void CompareBox_TextChanged(object sender, EventArgs e)
		{
			WatchValueBox compareBox = (WatchValueBox)sender;

			PopulateComparisonTypeBox(string.IsNullOrWhiteSpace(compareBox.Text));
		}

		/// <summary>
		/// Populates the comparison type drop down
		/// </summary>
		/// <param name="empty">True if drop down should be left empty</param>
		private void PopulateComparisonTypeBox(bool empty = false)
		{
			// Don't need to do anything in this case
			if (empty && CompareTypeDropDown.Items.Count == 1)
			{
				return;
			}
			
			// Don't need to do anything in this case
			if (!empty && CompareTypeDropDown.Items.Count == 6)
			{
				return;
			}

			CompareTypeDropDown.Items.Clear();

			if (empty)
			{
				CompareTypeDropDown.Items.AddRange(new object[]
				{
					""
				});
			}
			else 
			{
				CompareTypeDropDown.Items.AddRange(new object[]
				{
					"=",
					">",
					">=",
					"<",
					"<=",
					"!="
				});
			}

			CompareTypeDropDown.SelectedIndex = 0;
		}
	}
}
