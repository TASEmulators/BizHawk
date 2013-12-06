using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class TAStudio : Form, IToolForm
	{
		private const string MarkerColumn = "";
		private const string FrameColumn = "Frame#";

		private int _defaultWidth;
		private int _defaultHeight;
		private TasMovie _tas;

		#region API

		public TAStudio()
		{
			InitializeComponent();
			TASView.QueryItemText += TASView_QueryItemText;
			TASView.QueryItemBkColor += TASView_QueryItemBkColor;
			TASView.VirtualMode = true;
			Closing += (o, e) =>
			{
				if (AskSave())
				{
					SaveConfigSettings();
					GlobalWin.OSD.AddMessage("TAStudio Disengaged");
					if (Global.MovieSession.Movie is TasMovie)
					{
						Global.MovieSession.Movie = new Movie();
					}
				}
				else
				{
					e.Cancel = true;
				}
			};

			TopMost = Global.Config.TAStudioTopMost;
		}

		public bool AskSave()
		{
			// TODO: eventually we want to do this
			return true;
		}

		public bool UpdateBefore
		{
			get { return false; }
		}

		public void UpdateValues()
		{
			if (!IsHandleCreated || IsDisposed) return;

			TASView.ItemCount = _tas.InputLogLength;
		}

		public void Restart()
		{
			if (!IsHandleCreated || IsDisposed) return;
		}

		#endregion

		private void TASView_QueryItemBkColor(int index, int column, ref Color color)
		{
			var record = _tas[index];
			if (!record.HasState)
			{
				color = BackColor;
			}
			else
			{
				if (record.Lagged)
				{
					color = Color.Pink;
				}
				else
				{
					color = Color.LightGreen;
				}
			}
		}

		private void TASView_QueryItemText(int index, int column, out string text)
		{
			text = String.Empty;
			var columnName = TASView.Columns[column].Name;

			if (columnName == MarkerColumn)
			{
				text = "X";
			}
			else if (columnName == FrameColumn)
			{
				text = index.ToString().PadLeft(5, '0');
			}
			else
			{
				text = _tas[index].IsPressed(1, columnName) ? columnName : String.Empty;
			}
		}

		private void TAStudio_Load(object sender, EventArgs e)
		{
			if (Global.MovieSession.Movie.IsActive)
			{
				var result = MessageBox.Show("Warning, Tastudio doesn't support .bkm movie files at this time, opening this will cause you to lose your work, proceed? If you have unsaved changes you should cancel this, and savebefore opening TAStudio", "Unsupported movie", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning);
				if (result != DialogResult.Yes)
				{
					Close();
					return;
				}
			}

			GlobalWin.OSD.AddMessage("TAStudio engaged");
			Global.MovieSession.Movie = new TasMovie();
			_tas = Global.MovieSession.Movie as TasMovie;
			_tas.StartNewRecording();

			LoadConfigSettings();

			SetUpColumns();
		}

		private void SetUpColumns()
		{
			TASView.Columns.Clear();
			ToolHelpers.AddColumn(TASView, "", true, 18);
			ToolHelpers.AddColumn(TASView, "Frame#", true, 68);

			var mnemonics = MnemonicConstants.BUTTONS[Global.Emulator.Controller.Type.Name].Select(x => x.Value);

			foreach (var mnemonic in mnemonics)
			{
				ToolHelpers.AddColumn(TASView, mnemonic, true, 20);
			}
		}

		private void LoadConfigSettings()
		{
			_defaultWidth = Size.Width;
			_defaultHeight = Size.Height;

			if (Global.Config.TAStudioSaveWindowPosition && Global.Config.TASWndx >= 0 && Global.Config.TASWndy >= 0)
			{
				Location = new Point(Global.Config.TASWndx, Global.Config.TASWndy);
			}

			if (Global.Config.TASWidth >= 0 && Global.Config.TASHeight >= 0)
			{
				Size = new Size(Global.Config.TASWidth, Global.Config.TASHeight);
			}
		}

		private void SaveConfigSettings()
		{
			Global.Config.TASWndx = Location.X;
			Global.Config.TASWndy = Location.Y;
			Global.Config.TASWidth = Right - Left;
			Global.Config.TASHeight = Bottom - Top;
		}

		#region Events

		#region File Menu

		private void ExitMenuItem_Click(object sender, EventArgs e)
		{
			Close();
		}

		#endregion

		#region Settings Menu
		
		private void SettingsSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			SaveWindowPositionMenuItem.Checked = Global.Config.TAStudioSaveWindowPosition;
			AutoloadMenuItem.Checked = Global.Config.AutoloadTAStudio;
			AlwaysOnTopMenuItem.Checked = Global.Config.TAStudioTopMost;
		}

		private void AutoloadMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.AutoloadTAStudio ^= true;
		}
		private void SaveWindowPositionMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.TAStudioSaveWindowPosition ^= true;
		}

		private void AlwaysOnTopMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.TAStudioTopMost ^= true;
		}

		#endregion

		#endregion
	}
}
