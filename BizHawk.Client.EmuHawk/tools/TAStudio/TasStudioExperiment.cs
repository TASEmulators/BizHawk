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

			InputView.Invalidate();
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

		public TasStudioExperiment()
		{
			InitializeComponent();
		}

		private void TasStudioExperiment_Load(object sender, EventArgs e)
		{
			InputView.AddColumns(new []
			{
				new RollColumn
				{
					Group = "Core",
					Name = "MarkerColumn",
					Text = "",
					Width = 23,
				},
				new RollColumn
				{
					Group = "Core",
					Name = "FrameColumn",
					Text = "Frame",
					Width = 50,
				},
				new RollColumn
				{
					Group = "P1",
					Name = "P1 Up",
					Text = "U",
					Type = RollColumn.InputType.Boolean
				},
				new RollColumn
				{
					Group = "P1",
					Name = "P1 Down",
					Text = "D",
					Type = RollColumn.InputType.Boolean
				},
				new RollColumn
				{
					Group = "P1",
					Name = "P1 Left",
					Text = "L",
					Type = RollColumn.InputType.Boolean
				},
				new RollColumn
				{
					Group = "P1",
					Name = "P1 Right",
					Text = "R",
					Type = RollColumn.InputType.Boolean
				},
				new RollColumn
				{
					Group = "P1",
					Name = "P1 Select",
					Text = "s",
					Type = RollColumn.InputType.Boolean
				},
				new RollColumn
				{
					Group = "P1",
					Name = "P1 Start",
					Text = "S",
					Type = RollColumn.InputType.Boolean
				},
				new RollColumn
				{
					Group = "P1",
					Name = "P1 B",
					Text = "B",
					Type = RollColumn.InputType.Boolean
				},
				new RollColumn
				{
					Group = "P1",
					Name = "P1 A",
					Text = "A",
					Type = RollColumn.InputType.Boolean
				},
			});
		}

		private void settingsToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
		{

		}

		private void autoloadToolStripMenuItem_Click(object sender, EventArgs e)
		{

		}
	}
}
