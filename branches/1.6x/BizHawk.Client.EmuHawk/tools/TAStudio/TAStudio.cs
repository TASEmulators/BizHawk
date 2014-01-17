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

		private MarkerList _markers = new MarkerList();
		private List<TasClipboardEntry> TasClipboard = new List<TasClipboardEntry>();

		// Input Painting
		private string StartDrawColumn = String.Empty;
		private bool StartOn = false;
		private bool StartMarkerDrag = false;
		private bool StartFrameDrag = false;

		#region API

		public TAStudio()
		{
			InitializeComponent();
			TasView.QueryItemText += TASView_QueryItemText;
			TasView.QueryItemBkColor += TASView_QueryItemBkColor;
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

			TopMost = Global.Config.TAStudioTopMost;
			TasView.InputPaintingMode = Global.Config.TAStudioDrawInput;
			TasView.PointedCellChanged += TASView_PointedCellChanged;
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

		private void TASView_QueryItemBkColor(int index, int column, ref Color color)
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

		private void TASView_QueryItemText(int index, int column, out string text)
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
			//If past greenzone, emulate and capture states
			//if past greenzone AND movie, record input and capture states
			//If in greenzone, loadstate
			//If near a greenzone item, load and emulate
				//Do capturing and recording as needed

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
				//Find the earliest frame before this state
			}
		}

		private void SetSplicer()
		{
			// TODO: columns selected
			// TODO: clipboard
			ListView.SelectedIndexCollection list = TasView.SelectedIndices;
			string message = String.Empty;

			if (list.Count > 0)
			{
				message = list.Count.ToString() + " rows, 0 col, clipboard: ";
			}
			else
			{
				message = list.Count.ToString() + " selected: none, clipboard: ";
			}

			message += TasClipboard.Any() ? TasClipboard.Count.ToString() : "empty";

			SplicerStatusLabel.Text = message;
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
			TasView.InputPaintingMode = Global.Config.TAStudioDrawInput ^= true;
		}

		private void CopyMenuItem_Click(object sender, EventArgs e)
		{
			TasClipboard.Clear();
			ListView.SelectedIndexCollection list = TasView.SelectedIndices;
			for (int i = 0; i < list.Count; i++)
			{
				TasClipboard.Add(new TasClipboardEntry(list[i], _tas[i].Buttons));
			}

			SetSplicer();
		}

		#endregion

		#region Settings Menu

		private void SettingsSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			SaveWindowPositionMenuItem.Checked = Global.Config.TAStudioSaveWindowPosition;
			AutoloadMenuItem.Checked = Global.Config.AutoloadTAStudio;
			AutoloadProjectMenuItem.Checked = Global.Config.AutoloadTAStudioProject;
			AlwaysOnTopMenuItem.Checked = Global.Config.TAStudioTopMost;
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
			Global.Config.TAStudioSaveWindowPosition ^= true;
		}

		private void AlwaysOnTopMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.TAStudioTopMost ^= true;
		}

		private void RestoreDefaultSettingsMenuItem_Click(object sender, EventArgs e)
		{
			Size = new Size(_defaultWidth, _defaultHeight);

			Global.Config.TAStudioSaveWindowPosition = true;
			Global.Config.TAStudioTopMost = true;
		}

		#endregion

		#region TASView Events

		private void OnMovieChanged(object sender, MovieRecord.InputEventArgs e)
		{
			//TODO: move logic needs to go here
			TasView.ItemCount = _tas.InputLogLength;
		}

		private void TASView_MouseDown(object sender, MouseEventArgs e)
		{
			if (TasView.PointedCell.Row.HasValue && !String.IsNullOrEmpty(TasView.PointedCell.Column))
			{
				if (TasView.PointedCell.Column == MarkerColumnName)
				{
					StartMarkerDrag = true;
				}
				else if (TasView.PointedCell.Column == FrameColumnName)
				{
					StartFrameDrag = true;
				}
				else
				{
					_tas.ToggleButton(TasView.PointedCell.Row.Value, TasView.PointedCell.Column);
					TasView.Refresh();

					StartDrawColumn = TasView.PointedCell.Column;
					StartOn = _tas.IsPressed(TasView.PointedCell.Row.Value, TasView.PointedCell.Column);
				}
			}
		}

		private void TASView_MouseUp(object sender, MouseEventArgs e)
		{
			StartMarkerDrag = false;
			StartFrameDrag = false;
			StartDrawColumn = String.Empty;
		}

		private void TASView_PointedCellChanged(object sender, TasListView.CellEventArgs e)
		{
			if (StartMarkerDrag)
			{
				if (e.NewCell.Row.HasValue)
				{
					GoToFrame(e.NewCell.Row.Value);
				}
			}
			else if (StartFrameDrag)
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
			else if (TasView.IsPaintDown && e.NewCell.Row.HasValue && !String.IsNullOrEmpty(StartDrawColumn))
			{
				_tas.SetButton(e.NewCell.Row.Value, StartDrawColumn, StartOn); //Notice it uses new row, old column, you can only paint across a single column
				TasView.Refresh();
			}
		}

		private void TASView_SelectedIndexChanged(object sender, EventArgs e)
		{
			SetSplicer();
		}

		#endregion

		#endregion

		#region Classes

		public class TasClipboardEntry
		{
			private readonly Dictionary<string, bool> _buttons;
			private int _frame;

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
