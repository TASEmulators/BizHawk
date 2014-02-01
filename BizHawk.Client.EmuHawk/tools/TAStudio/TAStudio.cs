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

		private readonly MarkerList _markers = new MarkerList();
		private readonly List<TasClipboardEntry> _tasClipboard = new List<TasClipboardEntry>();

		private int _defaultWidth;
		private int _defaultHeight;
		private TasMovie _tas;

		// Input Painting
		private string _startDrawColumn = string.Empty;
		private bool _startOn;
		private bool _startMarkerDrag;
		private bool _startFrameDrag;

		#region API

		public TAStudio()
		{
			InitializeComponent();
			TasView.QueryItemText += TasView_QueryItemText;
			TasView.QueryItemBkColor += TasView_QueryItemBkColor;
			TasView.VirtualMode = true;
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

			TopMost = Global.Config.TAStudioSettings.TopMost;
			TasView.InputPaintingMode = Global.Config.TAStudioDrawInput;
			TasView.PointedCellChanged += TasView_PointedCellChanged;
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
					SaveTasMenuItem_Click(null, null);
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

			TasView.ItemCount = _tas.InputLogLength;
			if (_tas.IsRecording)
			{
				TasView.ensureVisible(_tas.InputLogLength - 1);
			}
			else
			{
				TasView.ensureVisible(Global.Emulator.Frame - 1);
			}
		}

		public void Restart()
		{
			if (!IsHandleCreated || IsDisposed)
			{
				return;
			}
		}

		#endregion

		private void TasView_QueryItemBkColor(int index, int column, ref Color color)
		{
			var record = _tas[index];
			if (_markers.CurrentFrame == index + 1)
			{
				color = Color.LightBlue;
			}
			else if (!record.HasState)
			{
				color = BackColor;
			}
			else
			{
				color = record.Lagged ? Color.Pink : Color.LightGreen;
			}
		}

		private void TasView_QueryItemText(int index, int column, out string text)
		{
			try
			{
				var columnName = TasView.Columns[column].Name;
				var columnText = TasView.Columns[column].Text;

				if (columnName == MarkerColumnName)
				{
					if (_markers.CurrentFrame == index + 1)
					{
						text = ">";
					}
					else
					{
						text = String.Empty;
					}
				}
				else if (columnName == FrameColumnName)
				{
					text = (index + 1).ToString().PadLeft(5, '0');
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

			if (Global.Config.AutoloadTAStudioProject)
			{
				Global.MovieSession.Movie = new TasMovie();
				_tas = Global.MovieSession.Movie as TasMovie;
				LoadFileFromRecent(Global.Config.RecentTas[0]);
			}
			else
			{
				EngageTasStudio();
			}

			_tas.ActivePlayers = new List<string> { "Player 1" }; // TODO


			SetUpColumns();
			LoadConfigSettings();
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
				TasView.ItemCount = _tas.InputLogLength;
			}
		}

		private void SetUpColumns()
		{
			TasView.Columns.Clear();
			AddColumn(MarkerColumnName, String.Empty, 18);
			AddColumn(FrameColumnName, "Frame#", 68);

			foreach (var kvp in _tas.AvailableMnemonics)
			{
				AddColumn(kvp.Key, kvp.Value.ToString(), 20);
			}
		}

		public void AddColumn(string columnName, string columnText, int columnWidth)
		{
			if (TasView.Columns[columnName] == null)
			{
				var column = new ColumnHeader
				{
					Name = columnName,
					Text = columnText,
					Width = columnWidth,
				};

				TasView.Columns.Add(column);
			}
		}

		private void LoadConfigSettings()
		{
			_defaultWidth = Size.Width;
			_defaultHeight = Size.Height;

			if (Global.Config.TAStudioSettings.UseWindowPosition)
			{
				Location = Global.Config.TAStudioSettings.WindowPosition;
			}

			if (Global.Config.TAStudioSettings.UseWindowSize)
			{
				Size = Global.Config.TAStudioSettings.WindowSize;
			}
		}

		private void SaveConfigSettings()
		{
			Global.Config.TAStudioSettings.Wndx = Location.X;
			Global.Config.TAStudioSettings.Wndy = Location.Y;
			Global.Config.TAStudioSettings.Width = Right - Left;
			Global.Config.TAStudioSettings.Height = Bottom - Top;
		}

		public void LoadFileFromRecent(string path)
		{
			if (AskSave())
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
					TasView.ItemCount = _tas.InputLogLength;
				}
			}
		}

		private void GoToFrame(int frame)
		{
			// If past greenzone, emulate and capture states
			// If past greenzone AND movie, record input and capture states
			// If in greenzone, loadstate
			// If near a greenzone item, load and emulate
			// Do capturing and recording as needed
			if (_tas[frame - 1].HasState) // Go back 1 frame and emulate
			{
				_tas.SwitchToPlay();
				Global.Emulator.LoadStateBinary(new BinaryReader(new MemoryStream(_tas[frame].State.ToArray())));
				Global.Emulator.FrameAdvance(true, true);
				GlobalWin.DisplayManager.NeedsToPaint = true;
				TasView.ensureVisible(frame);
				TasView.Refresh();
			}
			else
			{
				// Find the earliest frame before this state
			}
		}

		private void SetSplicer()
		{
			// TODO: columns selected
			// TODO: clipboard
			var list = TasView.SelectedIndices;
			var message = string.Empty;

			if (list.Count > 0)
			{
				message = list.Count + " rows, 0 col, clipboard: ";
			}
			else
			{
				message = list.Count + " selected: none, clipboard: ";
			}

			message += _tasClipboard.Any() ? _tasClipboard.Count.ToString() : "empty";

			SplicerStatusLabel.Text = message;
		}

		private void RefreshFloatingWindowControl()
		{
			Owner = Global.Config.TAStudioSettings.FloatingWindow ? null : GlobalWin.MainForm;
		}

		#region Events

		#region File Menu

		private void FileSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			SaveTASMenuItem.Enabled = !string.IsNullOrWhiteSpace(_tas.Filename);
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
			if (AskSave())
			{
				var file = ToolHelpers.GetTasProjFileFromUser(_tas.Filename);
				if (file != null)
				{
					_tas.Filename = file.FullName;
					_tas.Load();
					Global.Config.RecentTas.Add(_tas.Filename);
					TasView.ItemCount = _tas.InputLogLength;
					// TOOD: message to the user
				}
			}
		}

		private void SaveTasMenuItem_Click(object sender, EventArgs e)
		{
			if (string.IsNullOrEmpty(_tas.Filename))
			{
				SaveAsTasMenuItem_Click(sender, e);
			}
			else
			{
				_tas.Save();
			}
			// TODO: inform the user it happened somehow
		}

		private void SaveAsTasMenuItem_Click(object sender, EventArgs e)
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
			TasView.InputPaintingMode = Global.Config.TAStudioDrawInput ^= true;
		}

		private void CopyMenuItem_Click(object sender, EventArgs e)
		{
			_tasClipboard.Clear();
			var list = TasView.SelectedIndices;
			for (var i = 0; i < list.Count; i++)
			{
				_tasClipboard.Add(new TasClipboardEntry(list[i], _tas[i].Buttons));
			}

			SetSplicer();
		}

		#endregion

		#region Settings Menu

		private void SettingsSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			SaveWindowPositionMenuItem.Checked = Global.Config.TAStudioSettings.SaveWindowPosition;
			AutoloadMenuItem.Checked = Global.Config.AutoloadTAStudio;
			AutoloadProjectMenuItem.Checked = Global.Config.AutoloadTAStudioProject;
			AlwaysOnTopMenuItem.Checked = Global.Config.TAStudioSettings.TopMost;
			FloatingWindowMenuItem.Checked = Global.Config.TAStudioSettings.FloatingWindow;
		}

		private void AutoloadMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.AutoloadTAStudio ^= true;
		}

		private void AutoloadProjectMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.AutoloadTAStudioProject ^= true;
		}

		private void SaveWindowPositionMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.TAStudioSettings.SaveWindowPosition ^= true;
		}

		private void AlwaysOnTopMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.TAStudioSettings.TopMost ^= true;
		}

		private void FloatingWindowMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.TAStudioSettings.FloatingWindow ^= true;
			RefreshFloatingWindowControl();
		}

		private void RestoreDefaultSettingsMenuItem_Click(object sender, EventArgs e)
		{
			Size = new Size(_defaultWidth, _defaultHeight);

			Global.Config.TAStudioSettings.SaveWindowPosition = true;
			Global.Config.TAStudioSettings.TopMost = false;
			Global.Config.TAStudioSettings.FloatingWindow = false;

			RefreshFloatingWindowControl();
		}

		#endregion

		#region TASView Events

		private void OnMovieChanged(object sender, MovieRecord.InputEventArgs e)
		{
			// TODO: move logic needs to go here
			TasView.ItemCount = _tas.InputLogLength;
		}

		private void TasView_MouseDown(object sender, MouseEventArgs e)
		{
			if (TasView.PointedCell.Row.HasValue && !string.IsNullOrEmpty(TasView.PointedCell.Column))
			{
				if (TasView.PointedCell.Column == MarkerColumnName)
				{
					_startMarkerDrag = true;
				}
				else if (TasView.PointedCell.Column == FrameColumnName)
				{
					_startFrameDrag = true;
				}
				else
				{
					_tas.ToggleButton(TasView.PointedCell.Row.Value, TasView.PointedCell.Column);
					TasView.Refresh();

					_startDrawColumn = TasView.PointedCell.Column;
					_startOn = _tas.IsPressed(TasView.PointedCell.Row.Value, TasView.PointedCell.Column);
				}
			}
		}

		private void TasView_MouseUp(object sender, MouseEventArgs e)
		{
			_startMarkerDrag = false;
			_startFrameDrag = false;
			_startDrawColumn = string.Empty;
		}

		private void TasView_PointedCellChanged(object sender, TasListView.CellEventArgs e)
		{
			if (_startMarkerDrag)
			{
				if (e.NewCell.Row.HasValue)
				{
					GoToFrame(e.NewCell.Row.Value);
				}
			}
			else if (_startFrameDrag)
			{
				if (e.OldCell.Row.HasValue && e.NewCell.Row.HasValue)
				{
					int startVal, endVal;
					if (e.OldCell.Row.Value < e.NewCell.Row.Value)
					{
						startVal = e.OldCell.Row.Value;
						endVal = e.NewCell.Row.Value;
					}
					else
					{
						startVal = e.NewCell.Row.Value;
						endVal = e.OldCell.Row.Value;
					}

					for (int i = startVal + 1; i <= endVal; i++)
					{
						TasView.SelectItem(i, true);
					}
				}
			}
			else if (TasView.IsPaintDown && e.NewCell.Row.HasValue && !string.IsNullOrEmpty(_startDrawColumn))
			{
				_tas.SetButton(e.NewCell.Row.Value, _startDrawColumn, _startOn); // Notice it uses new row, old column, you can only paint across a single column
				TasView.Refresh();
			}
		}

		private void TasView_SelectedIndexChanged(object sender, EventArgs e)
		{
			SetSplicer();
		}

		#endregion

		#region Dialog Events

		protected override void OnShown(EventArgs e)
		{
			RefreshFloatingWindowControl();
			base.OnShown(e);
		}

		#endregion

		#endregion

		#region Classes

		public class TasClipboardEntry
		{
			private readonly Dictionary<string, bool> _buttons;
			private readonly int _frame;

			public TasClipboardEntry(int frame, Dictionary<string, bool> buttons)
			{
				_frame = frame;
				_buttons = buttons;
			}

			public int Frame
			{
				get { return _frame; }
			}

			public Dictionary<string, bool> Buttons
			{
				get { return _buttons; }
			}
		}

		#endregion
	}
}
