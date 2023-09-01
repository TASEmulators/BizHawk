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

		private const string HexInd = "0x";
		private bool _loading;
		private bool _editMode;

		private Action _addCallback;
		private Action _editCallback;

		private void CheatEdit_Load(object sender, EventArgs e) => Restart();

		public void Restart()
		{
			if (DesignMode)
			{
				return;
			}

			DomainDropDown.Items.Clear();
			DomainDropDown.Items.AddRange(MemoryDomains
				.Where(d => d.Writable)
				.Select(d => (object) d.ToString())
				.ToArray());

			DomainDropDown.SelectedItem = MemoryDomains.HasSystemBus
				? MemoryDomains.SystemBus.ToString()
				: MemoryDomains.MainMemory.ToString();

			SetFormToDefault();
		}

		private void SetFormToCheat()
		{
			_loading = true;
			SetSizeSelected(OriginalCheat.Size);
			PopulateTypeDropdown();
			SetTypeSelected(OriginalCheat.Type);
			SetDomainSelected(OriginalCheat.Domain);

			AddressBox.SetHexProperties(OriginalCheat.Domain.Size);

			ValueBox.ByteSize =
				CompareBox.ByteSize =
				OriginalCheat.Size;

			ValueBox.Type =
				CompareBox.Type =
				OriginalCheat.Type;

			ValueHexIndLabel.Text =
				CompareHexIndLabel.Text =
				OriginalCheat.Type == WatchDisplayType.Hex ? HexInd : "";

			BigEndianCheckBox.Checked = OriginalCheat.BigEndian ?? false;

			NameBox.Text = OriginalCheat.Name;
			AddressBox.Text = OriginalCheat.AddressStr;
			ValueBox.Text = OriginalCheat.ValueStr;
			CompareBox.Text = OriginalCheat.Compare.HasValue ? OriginalCheat.CompareStr : "";

			if (OriginalCheat.ComparisonType.Equals(Cheat.CompareType.None))
			{
				CompareTypeDropDown.SelectedIndex = 0;
			}
			else
			{
				CompareTypeDropDown.SelectedIndex = (int)OriginalCheat.ComparisonType - 1;
			}

			CheckFormState();
			if (!OriginalCheat.Compare.HasValue)
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
				WatchDisplayType.Hex;

			ValueBox.ResetText();
			CompareBox.ResetText();

			ValueHexIndLabel.Text =
				CompareHexIndLabel.Text =
				HexInd;

			BigEndianCheckBox.Checked = false;

			SetTypeSelected(WatchDisplayType.Hex);

			CheckFormState();
			CompareBox.Text = ""; // TODO: A needed hack until WatchValueBox.ToRawInt() becomes nullable
			_loading = false;
		}

		private void SetSizeSelected(WatchSize size)
		{
			SizeDropDown.SelectedIndex = size switch
			{
				WatchSize.Word => 1,
				WatchSize.DWord => 2,
				_ => 0,
			};
		}

		private void SetTypeSelected(WatchDisplayType type)
		{
			foreach (object item in DisplayTypeDropDown.Items)
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
			foreach (object item in DomainDropDown.Items)
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
					foreach (var t in ByteWatch.ValidTypes)
					{
						DisplayTypeDropDown.Items.Add(Watch.DisplayTypeToString(t));
					}

					break;
				case 1:
					foreach (var t in WordWatch.ValidTypes)
					{
						DisplayTypeDropDown.Items.Add(Watch.DisplayTypeToString(t));
					}

					break;
				case 2:
					foreach (var t in DWordWatch.ValidTypes)
					{
						DisplayTypeDropDown.Items.Add(Watch.DisplayTypeToString(t));
					}

					break;
			}

			DisplayTypeDropDown.SelectedItem = DisplayTypeDropDown.Items[0];
		}

		private void CheckFormState()
		{
			bool valid = !string.IsNullOrWhiteSpace(AddressBox.Text) && !string.IsNullOrWhiteSpace(ValueBox.Text);
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
				var domain = MemoryDomains[DomainDropDown.SelectedItem.ToString()]!;
				AddressBox.SetHexProperties(domain.Size);
			}
		}

		private WatchSize GetCurrentSize()
		{
			return SizeDropDown.SelectedIndex switch
			{
				1 => WatchSize.Word,
				2 => WatchSize.DWord,
				_ => WatchSize.Byte,
			};
		}

		private void DisplayTypeDropDown_SelectedIndexChanged(object sender, EventArgs e)
		{
			ValueBox.Type =
				CompareBox.Type =
				Watch.StringToDisplayType(DisplayTypeDropDown.SelectedItem.ToString());
		}

		private void AddButton_Click(object sender, EventArgs e) => _addCallback?.Invoke();

		private void EditButton_Click(object sender, EventArgs e) => _editCallback?.Invoke();

		public void SetCheat(Cheat cheat)
		{
			_editMode = true;
			OriginalCheat = cheat;
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
			OriginalCheat = Cheat.Separator;
			_editMode = false;
			SetFormToDefault();
		}

		public Cheat OriginalCheat { get; private set; }

		public Cheat GetCheat()
		{
			var domain = MemoryDomains[DomainDropDown.SelectedItem.ToString()]!;
			int address = AddressBox.ToRawInt().Value;
			if (address < domain.Size)
			{
				Watch watch = Watch.GenerateWatch(
					domain,
					address: address,
					GetCurrentSize(),
					Watch.StringToDisplayType(DisplayTypeDropDown.SelectedItem.ToString()),
					bigEndian: BigEndianCheckBox.Checked,
					NameBox.Text);

				var comparisonType = CompareTypeDropDown.SelectedItem.ToString() switch
				{
					"" => Cheat.CompareType.None,
					"=" => Cheat.CompareType.Equal,
					">" => Cheat.CompareType.GreaterThan,
					">=" => Cheat.CompareType.GreaterThanOrEqual,
					"<" => Cheat.CompareType.LessThan,
					"<=" => Cheat.CompareType.LessThanOrEqual,
					"!=" => Cheat.CompareType.NotEqual,
					_ => Cheat.CompareType.None
				};

				int? compare = CompareBox.ToRawInt();
				return new Cheat(
					watch,
					value: ValueBox.ToRawInt().Value,
					compare: compare,
					enabled: true,
					comparisonType);
			}

			MessageBox.Show($"{address} is not a valid address for the domain {domain.Name}", "Index out of range", MessageBoxButtons.OK, MessageBoxIcon.Warning);
			return Cheat.Separator;
		}

		public void SetAddEvent(Action addCallback) => _addCallback = addCallback;

		public void SetEditEvent(Action editCallback) => _editCallback = editCallback;

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
