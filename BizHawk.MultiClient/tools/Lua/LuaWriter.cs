using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using System.IO;
using BizHawk.MultiClient.tools;

namespace BizHawk.MultiClient
{
	public partial class LuaWriter : Form
	{
		//TODO:
		//ability to save new script (currently causes an exception)
		//New scripts should be added to lua console automatically
		//make functions is string part of string or comment since the actual way of validating it isn't correct
		//Save fontstyle to config
		//Line numbers
		//Option to toggle line numbers
		//Auto-complete drop down on functions in libraries
		//intellisense on library functions
		//Option to turn off basic lua script
		//Tool strip
		//function toolstrip button (inserts a function end block and puts cursor on blank line between them
		//error checking logic on library functions (check parameters, etc)
		//fix so drag & drop text file on edit box works (not just the edges around it
		//listview object with lua functions, double click inserts them into the script

		public string CurrentFile = "";
		public Regex keyWords = new Regex("and|break|do|else|if|end|false|for|function|in|local|nil|not|or|repeat|return|then|true|until|while|elseif");
		public Regex libraryWords;

		private bool changes;
		private bool redo;
		private bool hasChanged;
		private bool ProcessingText;
		private readonly char[] Symbols = { '+', '-', '*', '/', '%', '^', '#', '=', '<', '>', '(', ')', '{', '}', '[', ']', ';', ':', ',', '.' };
		private List<int[]> pos = new List<int[]>();

		public LuaWriter()
		{
			InitializeComponent();
			LuaText.MouseWheel += LuaText_MouseWheel;
		}

		void LuaText_MouseWheel(object sender, MouseEventArgs e)
		{
			if (KeyInput.IsPressed(SlimDX.DirectInput.Key.LeftControl))
			{
				Double Zoom;
				if ((LuaText.ZoomFactor == 0.1F && e.Delta < 0) || (LuaText.ZoomFactor == 5.0F && e.Delta > 0))
				{
					Zoom = (LuaText.ZoomFactor*100);
				}
				else
				{
					Zoom = (LuaText.ZoomFactor*100) + e.Delta/12;
				}

				ZoomLabel.Text = string.Format("Zoom: {0:0}%", Zoom);
			}
		}

		private void timer_Tick(object sender, EventArgs e)
		{
			if (!hasChanged)
			{
				return;
			}

			// ProcessText();   // Commenting out until it's fixed to not scroll everything all the time
			hasChanged = false;
		}

		private void ProcessText()
		{
			LuaText.InhibitPaint = true;
			ProcessingText = true;
			int selPos = LuaText.SelectionStart;
			int selChars = LuaText.SelectedText.Length;

			LuaText.SelectAll();
			LuaText.SelectionColor = Color.FromArgb(Global.Config.LuaDefaultTextColor);
			if (Global.Config.LuaDefaultTextBold)
				LuaText.SelectionFont = new Font(LuaText.SelectionFont, FontStyle.Bold);
			else
				LuaText.SelectionFont = new Font(LuaText.SelectionFont, FontStyle.Regular);

			AddCommentsAndStrings();
			AddNumbers();
			AddKeyWords();
			AddLibraries();
			AddSymbols();

			ColorText();

			LuaText.Select(selPos, selChars);
			ProcessingText = false;
			LuaText.InhibitPaint = false;
			LuaText.Refresh();
		}

		private void AddNumbers()
		{
			string temp = LuaText.Text;
			foreach (Match match in new Regex(@"(\d+\.?\d+|\.\d+|\d+)").Matches(temp))
			{
				if (!IsThisPartOfStringOrComment(match.Index))
				{
					char before = ' ', after = ' ';

					if (match.Index > 0)
						before = LuaText.Text[match.Index - 1];

					if (match.Index + match.Length != LuaText.Text.Length)
						after = LuaText.Text[match.Index + match.Length];

					if (!char.IsLetter(before) && !char.IsLetter(after))
					{
						AddPosition(match.Index, match.Length, Global.Config.LuaDecimalColor, Global.Config.LuaDecimalBold, 0);
					}
				}
			}
		}

		private void AddCommentsAndStrings()
		{
			string temp = LuaText.Text;
			int position = 0;

			while (position <= temp.Length)
			{
				int comment = temp.IndexOf("--", position, StringComparison.Ordinal);
				int longcomment = temp.IndexOf("--[[", position);
				int quote = temp.IndexOf('"', position);
				int apos = temp.IndexOf('\'', position);
				int longstring = temp.IndexOf("[", position);

				int secondBracket = temp.IndexOf('[', longstring + 1);
				if (secondBracket >= 0)
					while (longstring >= 0 && secondBracket >= 0 && longstring != longcomment - 2 && !IsLongString(temp.Substring(longstring, secondBracket - longstring)))
					{
						longstring = temp.IndexOf('[', longstring + 1);
						if (longstring >= 0)
							secondBracket = temp.IndexOf('[', longstring + 1);
						if (secondBracket == -1)
							longstring = -1;
					}
				else
					longstring = -1;

				if (comment >= 0 && (comment < quote || quote == -1) && (comment < apos || apos == -1) && (comment < longstring || longstring == -1))
				{
					if (comment < longcomment || longcomment == -1)
					{
						position = AddComment(comment);
					}
					else if (comment >= longcomment && longcomment >= 0)
					{
						position = AddMultiLineComment(longcomment);
					}
				}
				else if (quote >= 0 && (quote < apos || apos == -1) && (quote < longstring || longstring == -1))
				{
					position = AddString(quote, '"');
				}
				else if (apos >= 0 && (apos < longstring || longstring == -1))
				{
					position = AddString(apos, '\'');
				}
				else if (longstring >= 0)
				{
					position = AddLongString(longstring, secondBracket);
				}

				position++;
			}
		}

		private int AddLongString(int startPos, int secondBracket)
		{
			int ending = 0;
			if (startPos != LuaText.Text.Length - 1)
			{
				string tempBracket = GetLongStringClosingBracket(LuaText.Text.Substring(startPos, secondBracket - startPos + 1));
				ending = LuaText.Text.IndexOf(tempBracket, secondBracket + 1);
				if (ending < 0)
					ending = LuaText.Text.Length;
				else
					ending += tempBracket.Length - 1;
			}

			AddPosition(startPos, ending - startPos + 1, Global.Config.LuaStringColor, Global.Config.LuaStringBold, 1);

			return ending;
		}

		private string GetLongStringClosingBracket(string openingBracket)
		{
			string closingBrackets = "]";
			int level = openingBracket.Count(c => c == '=');

			for (int x = 0; x < level; x++)
			{
				closingBrackets += '=';
			}

			return closingBrackets + ']';
		}

		private static bool IsLongString(string longstring)
		{
			return longstring.All(c => c == '[' || c == '=');
		}

		private void AddSymbols()
		{
			string temp = LuaText.Text;

			foreach (char mark in Symbols)
			{
				int currPos = 0;
				int selection = temp.IndexOf(mark, currPos);

				while (selection >= 0)
				{
					if(!IsThisPartOfStringOrComment(selection))
						AddPosition(selection, 1, Global.Config.LuaSymbolColor, Global.Config.LuaSymbolBold, 0);

					currPos = selection + 1;
					selection = temp.IndexOf(mark, currPos);

					if (currPos == temp.Length)
						break;
				}
			}
		}

		private int AddString(int startPos, char mark)
		{
			int endLine;

			if (LuaText.GetLineFromCharIndex(startPos) + 1 == LuaText.Lines.Count())
			{
				endLine = LuaText.Text.Length - 1;
			}
			else
			{
				endLine = LuaText.GetFirstCharIndexFromLine(LuaText.GetLineFromCharIndex(startPos) + 1) - 1;
			}

			int ending;

			if (startPos != LuaText.Text.Length - 1)
			{
				if (startPos + 1 != endLine)
				{
					ending = LuaText.Text.IndexOf(mark, startPos + 1, endLine - startPos + 1);
					if (ending > 0)
					{
						while (ending > 0)
						{
							if (!IsThisPartOfTheString(LuaText.Text.Substring(startPos, ending - startPos + 1)))
								break;
							else
								ending++;

							ending = LuaText.Text.IndexOf(mark, ending, endLine - startPos + 1);
						}
					}
					else
						ending = endLine;
				}
				else
					ending = endLine;
			}
			else
				ending = endLine;

			if (startPos != LuaText.Text.Length)
			{
				AddPosition(startPos, ending - startPos + 1, Global.Config.LuaStringColor, Global.Config.LuaStringBold, 1);
			}

			return ending;
		}

		private bool IsThisPartOfTheString(string wholestring)
		{
			int ammount = 0;
			for (int x = wholestring.Length - 2; x > -1; x--)
			{
				if (wholestring[x] == '\\')
				{
					ammount++;
				}
				else
				{
					break;
				}
			}

			return ammount % 2 != 0;
		}

		private int AddComment(int startPos)
		{
			int endComment;

			if (LuaText.GetLineFromCharIndex(startPos) + 1 == LuaText.Lines.Count())
				endComment = LuaText.Text.Length - startPos;
			else
				endComment = LuaText.GetFirstCharIndexFromLine(LuaText.GetLineFromCharIndex(startPos) + 1) - 1;

			AddPosition(startPos, endComment - startPos, Global.Config.LuaCommentColor, Global.Config.LuaCommentBold, 1);

			return endComment;
		}

		private int AddMultiLineComment(int startPos)
		{
			int endComment;

			int selection = LuaText.Text.IndexOf("]]", StringComparison.Ordinal);
			if (selection > 0)
			{
				endComment = selection - startPos + 2;
			}
			else
			{
				endComment = LuaText.Text.Length;
			}

			AddPosition(startPos, endComment, Global.Config.LuaCommentColor, Global.Config.LuaCommentBold, 1);

			return endComment;
		}

		private void AddKeyWords()
		{
			foreach (Match match in keyWords.Matches(LuaText.Text))
			{
				if (!IsThisPartOfStringOrComment(match.Index))
				{
					char before = ' ', after = ' ';

					if (match.Index > 0)
						if (match.Index > 5 && match.Value != "if" && LuaText.Text.Substring(match.Index - 4, 4) != "else")
							before = LuaText.Text[match.Index - 1];

					if (match.Index + match.Length != LuaText.Text.Length)
						if (match.Value != "else" && LuaText.Text.Substring(match.Index, 2) != "if")
							after = LuaText.Text[match.Index + match.Length];

					if (!char.IsLetterOrDigit(before) && !char.IsLetterOrDigit(after))
					{
						AddPosition(match.Index, match.Length, Global.Config.LuaKeyWordColor, Global.Config.LuaKeyWordBold, 1);
					}
				}
			}
		}

		private bool IsThisPartOfStringOrComment(int startPos)
		{
			return pos.Where(position => position[4] == 1).Any(position => position[0] <= startPos && position[0] + position[1] > startPos);
		}

		private void AddPosition(int start, int lenght, int color, bool bold, int iscommentorstring)
		{
			int IsBold = 0, IndexToAdd = 0;
			if (bold)
				IsBold = 1;

			for (int x = 0; x < pos.Count; x++)
			{
				if (start < pos[x][0])
				{
					IndexToAdd = x;
					break;
				}
				if (x == pos.Count - 1)
					IndexToAdd = x + 1;
			}

			pos.Insert(IndexToAdd, new[] { start, lenght, color, IsBold, iscommentorstring });
		}

		private void ColorText()
		{
			foreach (int[] positions in pos)
			{
				LuaText.Select(positions[0], positions[1]);
				LuaText.SelectionColor = Color.FromArgb(positions[2]);
				if (positions[3] == 1)
					LuaText.SelectionFont = new Font(LuaText.SelectionFont, FontStyle.Bold);
				else
					LuaText.SelectionFont = new Font(LuaText.SelectionFont, FontStyle.Regular);
			}

			pos = new List<int[]>();
		}

		private void AddLibraries()
		{
			foreach (Match match in libraryWords.Matches(LuaText.Text))
			{
				if (!IsThisPartOfStringOrComment(match.Index))
				{
					if (match.Index >= 0)
					{
						char before = ' ', after = ' ';

						if (match.Index > 0)
							before = LuaText.Text[match.Index - 1];

						if (match.Index + match.Length != LuaText.Text.Length)
							after = LuaText.Text[match.Index + match.Length];

						if (!char.IsLetterOrDigit(before))
						{
							if (after == '.')
							{
								AddPosition(match.Index, match.Length, Global.Config.LuaLibraryColor, Global.Config.LuaLibraryBold, 0);
							}
						}
					}
				}
			}
		}

		private void GenerateLibraryRegex()
		{
			StringBuilder list = new StringBuilder();
			List<string> Libs = Global.MainForm.LuaConsole1.LuaImp.docs.GetLibraryList();
			for (int i = 0; i < Libs.Count; i++)
			{
				list.Append(Libs[i]);
				if (i < Libs.Count - 1)
				{
					list.Append('|');
				}
			}

			list.Append("|coroutine|package|debug|file|io|math|os|package|string|table");
			libraryWords = new Regex(list.ToString());
		}

		private void LoadFont()
		{
			LuaText.Font = new Font(Global.Config.LuaWriterFont, Global.Config.LuaWriterFontSize);
		}

		private void LuaWriter_Load(object sender, EventArgs e)
		{
			//LuaTextFont;
			ProcessingText = true;
			LuaText.SelectionTabs = new[] { 20, 40, 60, 80, 100, 120, 140, 160, 180, 200, 220, 240, 260, 280, 300, 320, 340, 360, 380, 400, 420, 480, 500, 520, 540, 560, 580, 600 }; //adelikat:  What a goofy way to have to do this
			LoadFont();
			LuaText.BackColor = Color.FromArgb(Global.Config.LuaWriterBackColor);
			LuaText.ZoomFactor = Global.Config.LuaWriterZoom;
			ZoomLabel.Text = string.Format("Zoom: {0}%", LuaText.ZoomFactor * 100);
			GenerateLibraryRegex();
			
			if (!String.IsNullOrWhiteSpace(CurrentFile))
			{
				LoadCurrentFile();
				NoChanges();
			}
			else if (!Global.Config.LuaWriterStartEmpty)
			{
				LuaText.Text = "while true do\n\t\n\temu.frameadvance()\nend";
				LuaText.SelectionStart = 15;
				Changes();
			}
			else
				startWithEmptyScriptToolStripMenuItem.Checked = true;

			UpdateLineNumber();
			ProcessText();
		}

		private void NoChanges()
		{
			changes = false;
			MessageLabel.Text = CurrentFile;
		}

		private void LoadCurrentFile()
		{
			var file = new FileInfo(CurrentFile);
			if (file.Exists == false)
			{
				return;
			}

			StreamReader sr = new StreamReader(file.FullName);
			LuaText.Text = sr.ReadToEnd();
			sr.Close();
		}

		private void LuaWriter_DragEnter(object sender, DragEventArgs e)
		{
			e.Effect = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
		}

		private void LuaWriter_DragDrop(object sender, DragEventArgs e)
		{
			string[] filePaths = (string[])e.Data.GetData(DataFormats.FileDrop);
			if (Path.GetExtension(filePaths[0]) == (".lua") || Path.GetExtension(filePaths[0]) == (".txt"))
			{
				//TODO: save changes
				CurrentFile = filePaths[0];
				LoadCurrentFile();
			}
		}

		private void saveToolStripMenuItem_Click(object sender, EventArgs e)
		{
			SaveScript();
			MessageLabel.Text = Path.GetFileName(CurrentFile) + " saved.";
		}

		private void SaveScript()
		{
			if (String.IsNullOrWhiteSpace(CurrentFile))
			{
				SaveScriptAs();
			}
			else
			{
				var file = new FileInfo(CurrentFile);
				/*if (!file.Exists)
				{
					SaveScriptAs();
				}*/
				using (StreamWriter sw = new StreamWriter(CurrentFile))
				{
					sw.Write(LuaText.Text);
				}
				MessageLabel.Text = Path.GetFileName(CurrentFile) + " saved.";
				NoChanges();
			}
		}

		private void SaveScriptAs()
		{
			var file = GetSaveFileFromUser(CurrentFile);
			if (file != null)
			{
				CurrentFile = file.FullName;
				SaveScript();
				MessageLabel.Text = Path.GetFileName(CurrentFile) + " saved.";
				Global.Config.RecentLua.Add(file.FullName);
			}
		}

		public static FileInfo GetSaveFileFromUser(string currentFile)
		{
			var sfd = new SaveFileDialog();
			if (!String.IsNullOrWhiteSpace(currentFile))
			{
				sfd.FileName = Path.GetFileNameWithoutExtension(currentFile);
				sfd.InitialDirectory = Path.GetDirectoryName(currentFile);
			}
			else if (!(Global.Emulator is NullEmulator))
			{
				sfd.FileName = PathManager.FilesystemSafeName(Global.Game);
				sfd.InitialDirectory = PathManager.GetLuaPath();
			}
			else
			{
				sfd.FileName = "NULL";
				sfd.InitialDirectory = PathManager.GetLuaPath();
			}
			sfd.Filter = "Watch Files (*.lua)|*.lua|All Files|*.*";
			sfd.RestoreDirectory = true;
			Global.Sound.StopSound();
			var result = sfd.ShowDialog();
			Global.Sound.StartSound();
			if (result != DialogResult.OK)
				return null;
			var file = new FileInfo(sfd.FileName);
			return file;
		}

		private void LuaText_KeyUp(object sender, KeyEventArgs e)
		{

		}

		private int CountTabsAtBeginningOfLine(string line)
		{
			int tabs = 0;
			foreach (Char c in line)
			{
				if (c == '\t')
					tabs++;
				else
					break;
			}
			return tabs;
		}

		private void Changes()
		{
			changes = true;
			MessageLabel.Text = CurrentFile + " *";
		}

		private void LuaText_TextChanged(object sender, EventArgs e)
		{
			if (!ProcessingText)
			{
				hasChanged = true;
				Changes();
			}
		}

		private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			SaveScriptAs();
		}

		private void syntaxHighlightingToolStripMenuItem_Click(object sender, EventArgs e)
		{
			LuaWriterColorConfig l = new LuaWriterColorConfig();
			if (l.ShowDialog() == DialogResult.OK)
			{
				ProcessText();  //Update display with new settings
			}

		}

		private void fontToolStripMenuItem_Click(object sender, EventArgs e)
		{
			FontDialog f = new FontDialog();
			DialogResult result = f.ShowDialog();
			if (result == DialogResult.OK)
			{
				LuaText.Font = f.Font;
				Global.Config.LuaWriterFont = f.Font.Name;
				Global.Config.LuaWriterFontSize = f.Font.Size;
				ProcessText();   //Re-update coloring and such when font changes
			}
		}

		private void LuaText_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Escape)
			{
				AutoCompleteView.Visible = false;
			}

			if (e.KeyCode == Keys.OemPeriod)
			{
				string currentword = CurrentWord();
				if (IsLibraryWord(currentword))
				{
					List<string> libfunctions = Global.MainForm.LuaConsole1.LuaImp.docs.GetFunctionsByLibrary(currentword);

					// Position autocomplete box near the cursor's current position
                    int x = LuaText.GetPositionFromCharIndex(LuaText.SelectionStart).X + LuaText.Location.X + 5;
                    int y = LuaText.GetPositionFromCharIndex(LuaText.SelectionStart).Y + LuaText.Location.Y + (int)LuaText.Font.GetHeight() + 5;  // One row down
					AutoCompleteView.Location = new Point(x, y);

                    // Populate list with available options
                    AutoCompleteView.Items.Clear();
					foreach (string function in libfunctions)
					{
						ListViewItem item = new ListViewItem(function);
						AutoCompleteView.Items.Add(item);
					}

                    // Show window after it has been positioned and set up
                    AutoCompleteView.Visible = true;  
				}
			}

			if (e.KeyCode == Keys.Enter)
			{
				string[] Words = { "if", "for", "while", "function" };
				string tabsStr = "";
				int linenumber = LuaText.GetLineFromCharIndex(LuaText.GetFirstCharIndexOfCurrentLine());
				int tabs = CountTabsAtBeginningOfLine(LuaText.Lines[linenumber]);

				for (int a = 1; a <= tabs; a++)
					tabsStr += "\t";

				foreach (string Word in Words)
				{
					try
					{
						if (LuaText.Lines[linenumber].Substring(0 + tabs, Word.Length) == Word)
						{
							string str = LuaText.Text.Insert(LuaText.SelectionStart, "\n" + tabsStr + "\t\n" + tabsStr + "end");
							LuaText.Text = str;
							LuaText.Select(LuaText.GetFirstCharIndexFromLine(linenumber + 1) + 1 + tabs, 0);
							e.SuppressKeyPress = true;
							return;
						}
					}
					catch { }
				}

				string tempStr = LuaText.Text.Insert(LuaText.SelectionStart, "\n" + tabsStr);
				LuaText.Text = tempStr;
				LuaText.Select(LuaText.GetFirstCharIndexFromLine(linenumber + 1) + tabs, 0);
				e.SuppressKeyPress = true;
			}

			if (e.KeyCode == Keys.Up || e.KeyCode == Keys.Down)
			{
				if (AutoCompleteView.Visible)
				{
					e.SuppressKeyPress = true;
					SelectNextItem(e.KeyCode == Keys.Down);
				}
			}



		}

		private void SelectNextItem(bool Next)
		{
			if (AutoCompleteView.SelectedItems.Count > 0)
			{
				if (Next)
				{
					if (AutoCompleteView.FocusedItem == AutoCompleteView.Items[AutoCompleteView.Items.Count - 1])
						return;

					AutoCompleteView.FocusedItem = AutoCompleteView.Items[AutoCompleteView.Items.IndexOf(AutoCompleteView.SelectedItems[0]) + 1];
				}
				else
				{
					if (AutoCompleteView.FocusedItem == AutoCompleteView.Items[0])
						return;

					AutoCompleteView.FocusedItem = AutoCompleteView.Items[AutoCompleteView.Items.IndexOf(AutoCompleteView.SelectedItems[0]) - 1];
				}
			}
			else
			{
				if (Next)
					AutoCompleteView.FocusedItem = AutoCompleteView.Items[0];
			}
		}

		private string CurrentWord()
		{
			int last = LuaText.SelectionStart;

			int lastSpace = LuaText.Text.Substring(0, last).LastIndexOf(' ');
			int lastTab = LuaText.Text.Substring(0, last).LastIndexOf('\t');
			int lastLine = LuaText.Text.Substring(0, last).LastIndexOf('\n');
			int start;
			if (lastSpace > lastLine || lastTab > lastLine)
			{
				if (lastSpace > lastTab)
					start = lastSpace;
				else
					start = lastTab;
			}
			else
			{
				start = lastLine;
			}

			if (start == -1)
			{
				start = 0;
			}

			int length = last - start - 1;
			string word = LuaText.Text.Substring(start + 1, length);

			return word;
		}

		private bool IsLibraryWord(string word)
		{
			List<string> Libs = Global.MainForm.LuaConsole1.LuaImp.docs.GetLibraryList();
			if (Libs.Contains(word))
			{
				return true;
			}
			return false;
		}

		private void AutoCompleteView_MouseDoubleClick(object sender, MouseEventArgs e)
		{
			ListView.SelectedIndexCollection indexes = AutoCompleteView.SelectedIndices;
			if (indexes.Count > 0)
			{
				string str = AutoCompleteView.Items[indexes[0]].Text;
				int start = LuaText.SelectionStart;
				LuaText.Text = LuaText.Text.Insert(start, str);
				AutoCompleteView.Visible = false;
				LuaText.Select(start + str.Length, 0);
			}
		}

		private void LuaText_SelectionChanged(object sender, EventArgs e)
		{
			UpdateLineNumber();
		}

		private void UpdateLineNumber()
		{
			if (!hasChanged)
			{
				int currentLineIndex = LuaText.GetLineFromCharIndex(LuaText.SelectionStart);
				int lastLineIndex = LuaText.GetLineFromCharIndex(LuaText.TextLength);
				int currentColumnIndex = LuaText.SelectionStart - LuaText.GetFirstCharIndexFromLine(currentLineIndex);

				PositionLabel.Text = string.Format("Line {0}/{1}, Column {2}", currentLineIndex + 1, lastLineIndex + 1, currentColumnIndex + 1);
			}
		}

		private void LuaText_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
		{

		}

		private void exitToolStripMenuItem_Click(object sender, EventArgs e)
		{
			//TODO: check for changes and ask save
			Close();
		}

		private void LuaWriter_FormClosing(object sender, FormClosingEventArgs e)
		{
			Global.Config.LuaWriterZoom = LuaText.ZoomFactor;
			Global.Config.LuaWriterStartEmpty = startWithEmptyScriptToolStripMenuItem.Checked;
			Global.Config.LuaWriterBackColor = LuaText.BackColor.ToArgb();
		}

		private void restoreSettingsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.LuaDefaultTextColor = -16777216;
			Global.Config.LuaKeyWordColor = -16776961;
			Global.Config.LuaCommentColor = -16744448;
			Global.Config.LuaStringColor = -8355712;
			Global.Config.LuaSymbolColor = -16777216;
			Global.Config.LuaLibraryColor = -16711681;

			Global.Config.LuaDefaultTextBold = false;
			Global.Config.LuaKeyWordBold = false;
			Global.Config.LuaCommentBold = false;
			Global.Config.LuaStringBold = false;
			Global.Config.LuaSymbolBold = false;
			Global.Config.LuaLibraryBold = false;

			Global.Config.LuaWriterBackColor = -1;
			LuaText.BackColor = Color.FromArgb(-1);

			Global.Config.LuaWriterStartEmpty = false;
			startWithEmptyScriptToolStripMenuItem.Checked = false;

			ProcessText();

			Global.Config.LuaWriterFontSize = 11;
			Global.Config.LuaWriterFont = "Courier New";
			LuaText.Font = new Font("Courier New", 11);

			Global.Config.LuaWriterZoom = 1;
			LuaText.ZoomFactor = 1;
			ZoomLabel.Text = "Zoom: 100%";
		}

		private void goToToolStripMenuItem_Click(object sender, EventArgs e)
		{
			InputPrompt gotodialog = new InputPrompt();
			gotodialog.FormClosing += (s, a) =>
				{
					if (gotodialog.UserOK)
					{
						int x;

						if (!int.TryParse(gotodialog.UserText, out x))
						{
							a.Cancel = true;
							MessageBox.Show("You must enter only numbers.", "Invalid text", MessageBoxButtons.OK, MessageBoxIcon.Error);
						}
						else if (Convert.ToInt32(gotodialog.UserText) > LuaText.Lines.Length)
						{
							a.Cancel = true;
							MessageBox.Show("You must enter a number between 1 and " + LuaText.Lines.Length.ToString() + '.', "Invalid Line number", MessageBoxButtons.OK, MessageBoxIcon.Error);
						}
					}
				};
			gotodialog.Text = "Go To Line";
			gotodialog.SetMessage("Line Number (1 - " + LuaText.Lines.Length.ToString() + ')');
			gotodialog.ShowDialog();
			int linepos = Convert.ToInt32(gotodialog.UserText) - 1;
			LuaText.Select(LuaText.GetFirstCharIndexFromLine(linepos) + CountTabsAtBeginningOfLine(LuaText.Lines[linepos]), 0);
		}

		private void selectAllToolStripMenuItem_Click(object sender, EventArgs e)
		{
			LuaText.SelectAll();
		}

		private void cutToolStripMenuItem_Click(object sender, EventArgs e)
		{
			LuaText.Cut();
		}

		private void copyToolStripMenuItem_Click(object sender, EventArgs e)
		{
			LuaText.Copy();
		}

		private void pasteToolStripMenuItem_Click(object sender, EventArgs e)
		{
			LuaText.Paste();
		}

		private void undoToolStripMenuItem_Click(object sender, EventArgs e)
		{
			LuaText.Undo();
			redo = true;
		}

		private void redoToolStripMenuItem_Click(object sender, EventArgs e)
		{
			LuaText.Redo();
		}

		private void startWithEmptyScriptToolStripMenuItem_Click(object sender, EventArgs e)
		{
			startWithEmptyScriptToolStripMenuItem.Checked ^= true;
		}

		private void backgroundColorToolStripMenuItem_Click(object sender, EventArgs e)
		{
			ColorDialog col = new ColorDialog();
			if (col.ShowDialog() == DialogResult.OK)
			{
				LuaText.BackColor = col.Color;
			}
		}

		private void editToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			if (changes)
			{
				undoToolStripMenuItem.Enabled = true;
			}
			else
			{
				undoToolStripMenuItem.Enabled = false;
			}

			if (redo)
			{
				redoToolStripMenuItem.Enabled = true;
			}
			else
			{
				redoToolStripMenuItem.Enabled = false;
			}
		}
	}
}