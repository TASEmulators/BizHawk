using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Common.CollectionExtensions;
using BizHawk.WinForms.Controls;

using Emu = BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public sealed class WatchEditor : Form
	{
		public enum Mode { New, Duplicate, Edit }

		public Emu.IMemoryDomains MemoryDomains { get; set; }

		private Mode _mode = Mode.New;
		private bool _loading = true;

		private bool _changedSize;
		private bool _changedDisplayType;

		public List<Watch> Watches { get; } = new List<Watch>();

		public Point InitialLocation { get; set;  } = new Point(0, 0);

		private readonly HexTextBox AddressBox;

		private readonly CheckBox BigEndianCheckBox;

		private readonly ComboBox DisplayTypeDropDown;

		private readonly ComboBox DomainDropDown;

		private readonly TextBox NotesBox;

		private readonly ComboBox SizeDropDown;

		public WatchEditor()
		{
			_changedDisplayType = false;

			SuspendLayout();

			TableLayoutPanel tlpMain = new()
			{
				ColumnStyles = { new(), new() },
				Location = new(4, 4),
				RowStyles = { new(), new(), new(), new(), new(), new() },
				Size = new(201, 160),
			};
			var row = 0;

			LocLabelEx label6 = new() { Anchor = AnchorStyles.Right, Text = "Mem Domain:" };
			DomainDropDown = new()
			{
				DropDownStyle = ComboBoxStyle.DropDownList,
				FormattingEnabled = true,
				Size = new(120, 21),
			};
			DomainDropDown.SelectedIndexChanged += DomainComboBox_SelectedIndexChanged;
			tlpMain.Controls.Add(label6, row: row, column: 0);
			tlpMain.Controls.Add(DomainDropDown, row: row, column: 1);
			row++;

			LocLabelEx label1 = new() { Anchor = AnchorStyles.Right, Text = "Address:" };
			AddressBox = new()
			{
				CharacterCasing = CharacterCasing.Upper,
				MaxLength = 8,
				Nullable = false,
				Size = new(100, 20),
				Text = "00000000",
			};
			SingleRowFLP flpAddr = new()
			{
				Controls = { new LabelEx { Text = "0x" }, AddressBox },
			};
			tlpMain.Controls.Add(label1, row: row, column: 0);
			tlpMain.Controls.Add(flpAddr, row: row, column: 1);
			row++;

			LocLabelEx label3 = new() { Anchor = AnchorStyles.Right, Text = "Size:" };
			SizeDropDown = new()
			{
				DropDownStyle = ComboBoxStyle.DropDownList,
				FormattingEnabled = true,
				Items = { "1 Byte", "2 Byte", "4 Byte" },
				Size = new(120, 21),
			};
			SizeDropDown.SelectedIndexChanged += SizeDropDown_SelectedIndexChanged;
			tlpMain.Controls.Add(label3, row: row, column: 0);
			tlpMain.Controls.Add(SizeDropDown, row: row, column: 1);
			row++;

			//TODO merge into size dropdown (event handlers will need rewriting)
			BigEndianCheckBox = new()
			{
				AutoSize = true,
				Size = new(77, 17),
				Text = "Big Endian",
				UseVisualStyleBackColor = true,
			};
			tlpMain.Controls.Add(BigEndianCheckBox, row: row, column: 1);
			row++;

			LocLabelEx DisplayTypeLabel = new() { Anchor = AnchorStyles.Right, Text = "Display Type:" };
			DisplayTypeDropDown = new()
			{
				DropDownStyle = ComboBoxStyle.DropDownList,
				FormattingEnabled = true,
				Size = new(120, 21),
			};
			DisplayTypeDropDown.SelectedIndexChanged += DisplayTypeDropDown_SelectedIndexChanged;
			tlpMain.Controls.Add(DisplayTypeLabel, row: row, column: 0);
			tlpMain.Controls.Add(DisplayTypeDropDown, row: row, column: 1);
			row++;

			LocLabelEx label2 = new() { Anchor = AnchorStyles.Right, Text = "Notes:" };
			NotesBox = new() { MaxLength = 256, Size = new(120, 20) };
			tlpMain.Controls.Add(label2, row: row, column: 0);
			tlpMain.Controls.Add(NotesBox, row: row, column: 1);
			row++;

			Button OK = new()
			{
				Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
				Location = new(12, 260),
				Size = new(75, 23),
				Text = "OK",
				UseVisualStyleBackColor = true,
			};
			OK.Click += Ok_Click;
			Button Cancel = new()
			{
				Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
				DialogResult = DialogResult.Cancel,
				Location = new(123, 260),
				Size = new(75, 23),
				Text = "Cancel",
				UseVisualStyleBackColor = true,
			};
			Cancel.Click += Cancel_Click;
			AcceptButton = OK;
			AutoScaleDimensions = new(6F, 13F);
			AutoScaleMode = AutoScaleMode.Font;
			CancelButton = Cancel;
			ClientSize = new(215, 296);
			Controls.Add(tlpMain);
			Controls.Add(OK);
			Controls.Add(Cancel);
			FormBorderStyle = FormBorderStyle.FixedDialog;
			MaximizeBox = false;
			MinimizeBox = false;
			Name = nameof(WatchEditor);
			StartPosition = FormStartPosition.CenterParent;
			Text = "New Watch";
			Load += RamWatchNewWatch_Load;

			ResumeLayout(performLayout: false);
			PerformLayout();
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
					SizeDropDown.SelectedItem = MemoryDomains.First().WordSize switch
					{
						1 => SizeDropDown.Items[0],
						2 => SizeDropDown.Items[1],
						4 => SizeDropDown.Items[2],
						_ => SizeDropDown.Items[0]
					};
					break;
				case Mode.Duplicate:
				case Mode.Edit:
					SizeDropDown.SelectedItem = Watches[0].Size switch
					{
						WatchSize.Byte => SizeDropDown.Items[0],
						WatchSize.Word => SizeDropDown.Items[1],
						WatchSize.DWord => SizeDropDown.Items[2],
						_ => SizeDropDown.SelectedItem
					};

					var index = DisplayTypeDropDown.Items.IndexOf(Watch.DisplayTypeToString(Watches[0].Type));
					DisplayTypeDropDown.SelectedItem = DisplayTypeDropDown.Items[index];

					if (Watches.Count > 1)
					{
						NotesBox.Enabled = false;
						NotesBox.Text = "";

						AddressBox.Enabled = false;
						AddressBox.Text = Watches.Select(a => a.AddressString).Aggregate((addrStr, nextStr) => $"{addrStr},{nextStr}");

						BigEndianCheckBox.ThreeState = true;

						if (Watches.Select(static s => s.Size).Distinct().CountIsAtLeast(2))
						{
							DisplayTypeDropDown.Enabled = false;
						}
					}
					else
					{
						NotesBox.Text = Watches[0].Notes;
						NotesBox.Select();
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
			Text = _mode switch
			{
				Mode.New => "New Watch",
				Mode.Edit => $"Edit {(Watches.Count == 1 ? "Watch" : "Watches")}",
				Mode.Duplicate => "Duplicate Watch",
				_ => "New Watch"
			};
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
					foreach (WatchDisplayType t in ByteWatch.ValidTypes)
					{
						DisplayTypeDropDown.Items.Add(Watch.DisplayTypeToString(t));
					}
					break;
				case 1:
					foreach (WatchDisplayType t in WordWatch.ValidTypes)
					{
						DisplayTypeDropDown.Items.Add(Watch.DisplayTypeToString(t));
					}
					break;
				case 2:
					foreach (WatchDisplayType t in DWordWatch.ValidTypes)
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
				var firstWasBE = Watches[0].BigEndian;
				if (Watches.TrueForAll(w => w.BigEndian == firstWasBE))
				{
					BigEndianCheckBox.Checked = firstWasBE;
				}
				else
				{
					BigEndianCheckBox.Checked = true;
					BigEndianCheckBox.CheckState = CheckState.Indeterminate;
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
					var size = SizeDropDown.SelectedIndex switch
					{
						1 => WatchSize.Word,
						2 => WatchSize.DWord,
						_ => WatchSize.Byte
					};

					var displayType = Watch.StringToDisplayType(DisplayTypeDropDown.SelectedItem.ToString());

					Watches[i] = Watch.GenerateWatch(
						Watches[i].Domain,
						Watches.Count == 1 ? AddressBox.ToRawInt() ?? 0 : Watches[i].Address,
						size,
						_changedDisplayType ? displayType : Watches[i].Type,
						Watches[i].BigEndian,
						Watches[i].Notes);
				}
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
	}
}
