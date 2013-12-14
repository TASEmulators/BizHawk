using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class TAStudio : Form, IToolForm
	{
		// TODO: UI flow that conveniently allows to start from savestate

		private const string MarkerColumnName = "MarkerColumn";
		private const string FrameColumnName = "FrameColumn";

		private int _defaultWidth;
		private int _defaultHeight;
		private TasMovie _tas;

		// Input Painting
		private string StartDrawColumn = String.Empty;
		private bool StartOn = false;

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
						GlobalWin.MainForm.StopMovie(saveChanges: false);
					}
				}
				else
				{
					e.Cancel = true;
				}
			};

			TopMost = Global.Config.TAStudioTopMost;
			TASView.InputPaintingMode = Global.Config.TAStudioDrawInput;
			TASView.PointedCellChanged += TASView_PointedCellChanged;
		}

		public bool AskSave()
		{
			if (_tas.Changes)
			{
				GlobalWin.Sound.StopSound();
				var result = MessageBox.Show("Save Changes?", "Tastudio", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button3);
				GlobalWin.Sound.StartSound();
				if (result == DialogResult.Yes)
				{
					SaveTASMenuItem_Click(null, null);
				}
				else if (result == DialogResult.No)
				{
					_tas.Changes = false;
					return true;
				}
				else if (result == DialogResult.Cancel)
				{
					return false;
				}

			}

			return true;
		}

		public bool UpdateBefore
		{
			get { return false; }
		}

		public void UpdateValues()
		{
			if (!IsHandleCreated || IsDisposed)
			{
				return;
			}

			TASView.ItemCount = _tas.InputLogLength;
		}

		public void Restart()
		{
			if (!IsHandleCreated || IsDisposed)
			{
				return;
			}
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
				color = record.Lagged ? Color.Pink : Color.LightGreen;
			}
		}

		private void TASView_QueryItemText(int index, int column, out string text)
		{
			try
			{
				var columnName = TASView.Columns[column].Name;
				var columnText = TASView.Columns[column].Text;

				if (columnName == MarkerColumnName)
				{
					text = String.Empty;
				}
				else if (columnName == FrameColumnName)
				{
					text = index.ToString().PadLeft(5, '0');
				}
				else
				{
					text = _tas[index].IsPressed(columnName) ? columnText : String.Empty;
				}
			}
			catch (Exception ex)
			{
				text = String.Empty;
				MessageBox.Show("oops\n" + ex);
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

			EngageTasStudio();
			LoadConfigSettings();
			_tas.ActivePlayers = new List<string> { "Player 1" };
			SetUpColumns();
		}

		private void EngageTasStudio()
		{
			GlobalWin.OSD.AddMessage("TAStudio engaged");
			Global.MovieSession.Movie = new TasMovie();
			
			_tas = Global.MovieSession.Movie as TasMovie;
			_tas.StartNewRecording();
			_tas.OnChanged += OnMovieChanged;
			GlobalWin.MainForm.StartNewMovie(_tas, true, true);
		}

		private void StartNewSession()
		{
			if (AskSave())
			{
				GlobalWin.OSD.AddMessage("new TAStudio session started");
				_tas.StartNewRecording();
				GlobalWin.MainForm.StartNewMovie(_tas, true, true);
				TASView.ItemCount = _tas.InputLogLength;
			}
		}

		private void SetUpColumns()
		{
			TASView.Columns.Clear();
			AddColumn(MarkerColumnName, String.Empty, 18);
			AddColumn(FrameColumnName, "Frame#", 68);

			foreach (var kvp in _tas.AvailableMnemonics)
			{
				AddColumn(kvp.Key, kvp.Value.ToString(), 20);
			}
		}

		public void AddColumn(string columnName, string columnText, int columnWidth)
		{
			if (TASView.Columns[columnName] == null)
			{
				var column = new ColumnHeader
				{
					Name = columnName,
					Text = columnText,
					Width = columnWidth,
				};

				TASView.Columns.Add(column);
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

		public void LoadFileFromRecent(string path)
		{
			var askResult = true;
			if (_tas.Changes)
			{
				askResult = AskSave();
			}

			if (askResult)
			{
				_tas.Filename = path;
				var loadResult = _tas.Load();
				if (!loadResult)
				{
					ToolHelpers.HandleLoadError(Global.Config.RecentTas, path);
				}
				else
				{
					Global.Config.RecentTas.Add(path);
					TASView.ItemCount = _tas.InputLogLength;
				}
			}
		}

		#region Events

		#region File Menu

		private void FileSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			SaveTASMenuItem.Enabled = !String.IsNullOrWhiteSpace(_tas.Filename);
		}

		private void RecentSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			RecentSubMenu.DropDownItems.Clear();
			RecentSubMenu.DropDownItems.AddRange(
				ToolHelpers.GenerateRecentMenu(Global.Config.RecentTas, LoadFileFromRecent)
			);
		}

		private void NewTASMenuItem_Click(object sender, EventArgs e)
		{
			StartNewSession();
		}


		private void OpenTASMenuItem_Click(object sender, EventArgs e)
		{
			var file = ToolHelpers.GetTasProjFileFromUser(_tas.Filename);
			if (file != null)
			{
				_tas.Filename = file.FullName;
				_tas.Load();
				Global.Config.RecentTas.Add(_tas.Filename);
				// TOOD: message to the user
			}
		}

		private void SaveTASMenuItem_Click(object sender, EventArgs e)
		{
			if (String.IsNullOrEmpty(_tas.Filename))
			{
				SaveAsTASMenuItem_Click(sender, e);
			}
			else
			{
				_tas.Save();
			}
			// TODO: inform the user it happened somehow
		}

		private void SaveAsTASMenuItem_Click(object sender, EventArgs e)
		{
			var file = ToolHelpers.GetTasProjSaveFileFromUser(_tas.Filename);
			if (file != null)
			{
				_tas.Filename = file.FullName;
				_tas.Save();
				Global.Config.RecentTas.Add(_tas.Filename);
				// TODO: inform the user it happened somehow
			}
		}

		private void ExitMenuItem_Click(object sender, EventArgs e)
		{
			Close();
		}

		#endregion

		#region Config

		private void ConfigSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			DrawInputByDraggingMenuItem.Checked = Global.Config.TAStudioDrawInput;
		}

		private void DrawInputByDraggingMenuItem_Click(object sender, EventArgs e)
		{
			TASView.InputPaintingMode = Global.Config.TAStudioDrawInput ^= true;
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

		#region TASView Events

		private void OnMovieChanged(object sender, MovieRecord.InputEventArgs e)
		{
			//TODO: move logic needs to go here
			TASView.ItemCount = _tas.InputLogLength;
		}

		private void TASView_MouseDown(object sender, MouseEventArgs e)
		{
			if (TASView.PointedCell.Row.HasValue && !String.IsNullOrEmpty(TASView.PointedCell.Column))
			{
				_tas.ToggleButton(TASView.PointedCell.Row.Value, TASView.PointedCell.Column);
				TASView.Refresh();

				StartDrawColumn = TASView.PointedCell.Column;
				StartOn = _tas.IsPressed(TASView.PointedCell.Row.Value, TASView.PointedCell.Column);
			}
		}

		private void TASView_PointedCellChanged(object sender, TasListView.CellEventArgs e)
		{
			if (TASView.IsPaintDown && e.NewCell.Row.HasValue && !String.IsNullOrEmpty(StartDrawColumn))
			{
				_tas.SetButton(e.NewCell.Row.Value, StartDrawColumn, StartOn); //Notice it uses new row, old column, you can only paint across a single column
				TASView.Refresh();
			}
		}

		#endregion

		#endregion
	}
}
