using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class TasStudioExperiment : Form, IToolForm
	{
		#region IToolForm Implementation

		public bool UpdateBefore { get { return false; } }

		public void UpdateValues()
		{
			if (!IsHandleCreated || IsDisposed)
			{
				return;
			}

			Watches.UpdateValues();
			InputView.Refresh();
		}

		public void FastUpdate()
		{
			// TODO: think more about this
		}

		public void Restart()
		{
			if (!IsHandleCreated || IsDisposed)
			{
				return;
			}

		}

		public bool AskSave()
		{
			return true;
		}

		#endregion

		private readonly WatchList Watches = new WatchList(Global.Emulator.MemoryDomains.MainMemory);
		Random r;

		public TasStudioExperiment()
		{
			InitializeComponent();
			InputView.QueryItemText += TasView_QueryItemText;
			InputView.QueryItemBkColor += TasView_QueryItemBkColor;
			r = new Random((int)DateTime.Now.Ticks);
			InputView.FullRowSelect = true;
			InputView.MultiSelect = false;
		}

		private int? columnClicked = null;

		private void TasView_QueryItemText(int index, int column, out string text)
		{

			if (columnClicked.HasValue && column == columnClicked)
			{
				text = "!";
				return;
			}

			text = r.NextDouble() > .5 ? "_" : "";

			/*
			text = string.Empty;

			if (index >= Watches.ItemCount || Watches[index].IsSeparator)
			{
				return;
			}

			//var columnName = InputView.Columns[column].Name;

			switch (column)
			{
				case 0:
					text = Watches[index].AddressString;
					break;
				case 1:
					text = Watches[index].ValueString;
					break;
				case 2:
					text = Watches[index].PreviousStr;
					break;
				case 3:
					if (!Watches[index].IsSeparator)
					{
						text = Watches[index].ChangeCount.ToString();
					}

					break;
				case 4:
					text = Watches[index].Diff;
					break;
				case 5:
					text = Watches[index].Domain.Name;
					break;
				case 6:
					text = Watches[index].Notes;
					break;
			}
			*/
		}

		private void TasView_QueryItemBkColor(int index, int column, ref Color color)
		{
			var test = r.NextDouble() > .5;
			//var test = index == 2 && column == 2;

			color = test ? Color.LightCyan : Color.White;
		}

		private void TasStudioExperiment_Load(object sender, EventArgs e)
		{
			/*
			for (int i = 0; i < 20; i++)
			{
				Watches.Add(new ByteWatch(Watches.Domain, 0x0057, Watch.DisplayType.Signed, false, "Speed"));
			}

			InputView.AddColumns(new[]
			{
				new InputRoll.RollColumn
				{
					Group = "",
					Name = "Address",
					Text = "Address"
				},
				new InputRoll.RollColumn
				{
					Group = "",
					Name = "Value",
					Text = "Value"
				},
				new InputRoll.RollColumn
				{
					Group = "",
					Name = "Prev",
					Text = "Prev"
				},
				new InputRoll.RollColumn
				{
					Group = "",
					Name = "Changes",
					Text = "Changes"
				},
				new InputRoll.RollColumn
				{
					Group = "",
					Name = "Domain",
					Text = "Domain"
				},
				new InputRoll.RollColumn
				{
					Group = "",
					Name = "Diff",
					Text = "Diff"
				},
				new InputRoll.RollColumn
				{
					Group = "",
					Name = "Notes",
					Text = "Notes"
				},
			});
			*/

			InputView.AddColumns(new []
			{
				new InputRoll.RollColumn
				{
					Group = "Core",
					Name = "MarkerColumn",
					Text = "",
					Width = 23,
				},
				new InputRoll.RollColumn
				{
					Group = "Core",
					Name = "FrameColumn",
					Text = "Frame",
					Width = 50,
				},
				new InputRoll.RollColumn
				{
					Group = "P1",
					Name = "P1 Up",
					Text = "U",
					Type = InputRoll.RollColumn.InputType.Boolean
				},
				new InputRoll.RollColumn
				{
					Group = "P1",
					Name = "P1 Down",
					Text = "D",
					Type = InputRoll.RollColumn.InputType.Boolean
				},
				new InputRoll.RollColumn
				{
					Group = "P1",
					Name = "P1 Left",
					Text = "L",
					Type = InputRoll.RollColumn.InputType.Boolean
				},
				new InputRoll.RollColumn
				{
					Group = "P1",
					Name = "P1 Right",
					Text = "R",
					Type = InputRoll.RollColumn.InputType.Boolean
				},
				new InputRoll.RollColumn
				{
					Group = "P1",
					Name = "P1 Select",
					Text = "s",
					Type = InputRoll.RollColumn.InputType.Boolean
				},
				new InputRoll.RollColumn
				{
					Group = "P1",
					Name = "P1 Start",
					Text = "S",
					Type = InputRoll.RollColumn.InputType.Boolean
				},
				new InputRoll.RollColumn
				{
					Group = "P1",
					Name = "P1 B",
					Text = "B",
					Type = InputRoll.RollColumn.InputType.Boolean
				},
				new InputRoll.RollColumn
				{
					Group = "P1",
					Name = "P1 A",
					Text = "A",
					Type = InputRoll.RollColumn.InputType.Boolean
				},
			});
		}

		private void settingsToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
		{

		}

		private void autoloadToolStripMenuItem_Click(object sender, EventArgs e)
		{

		}

		private void InputView_PointedCellChanged(object sender, InputRoll.CellEventArgs e)
		{
			if (e.NewCell == null)
			{
				CurrentCellLabel.Text = "None";
			}
			else
			{
				string column = e.NewCell.Column != null ? e.NewCell.Column.Text : "None";
				string row = e.NewCell.RowIndex.HasValue ? e.NewCell.RowIndex.Value.ToString() : "None";

				CurrentCellLabel.Text = string.Format("Column: {0} RowIndex: {1}", column, row);
			}
		}

		private void InputView_ColumnClick(object sender, ColumnClickEventArgs e)
		{
			var column = InputView.GetColumn(e.Column);
			columnClicked = e.Column;
			InputView.Refresh();
		}

		private void InputView_SelectedIndexChanged(object sender, EventArgs e)
		{
			OutputLabel.Text = string.Join(",", InputView.SelectedIndices.ToArray());
		}
	}
}
