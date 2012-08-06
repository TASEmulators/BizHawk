using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
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

		bool changes = false;
        bool hasChanged = false;
		bool ProcessingText;
		public Regex keyWords = new Regex("and|break|do|else|if|end|false|for|function|in|local|nil|not|or|repeat|return|then|true|until|while|elseif");
		char[] Symbols = { '+', '-', '*', '/', '%', '^', '#', '=', '<', '>', '(', ')', '{', '}', '[', ']', ';', ':', ',', '.' };
		public Regex libraryWords;

		List<int[]> pos = new List<int[]>();

		public LuaWriter()
		{
			InitializeComponent();
			LuaText.MouseWheel += new MouseEventHandler(LuaText_MouseWheel);
		}

		void LuaText_MouseWheel(object sender, MouseEventArgs e)
		{
			if (KeyInput.IsPressed(SlimDX.DirectInput.Key.LeftControl))
			{
				Double Zoom;
				if ((LuaText.ZoomFactor == 0.1F && e.Delta < 0) || (LuaText.ZoomFactor == 5.0F && e.Delta > 0))
					Zoom = (LuaText.ZoomFactor * 100);
				else
					Zoom = (LuaText.ZoomFactor * 100) + e.Delta / 12;

				ZoomLabel.Text = string.Format("Zoom: {0:0}%", Zoom);
			}
		}

		private void timer_Tick(object sender, EventArgs e)
		{
			if (!hasChanged)
			{
				return;
			}

			ProcessText();

			hasChanged = false;
		}

		private void ProcessText()
		{
            ProcessingText = true;
			int selPos = LuaText.SelectionStart;
			int selChars = LuaText.SelectedText.Length;

			LuaText.SelectAll();
			LuaText.SelectionColor = Color.FromArgb(Global.Config.LuaDefaultTextColor);
			if(Global.Config.LuaDefaultTextBold)
				LuaText.SelectionFont = new Font(LuaText.SelectionFont, FontStyle.Bold);
			else
				LuaText.SelectionFont = new Font(LuaText.SelectionFont, FontStyle.Regular);

			AddKeyWords();
			AddLibraries();
			AddSymbols();
			AddComments();
			AddStrings();
			AddLongStrings();

			ColorText();

			LuaText.Select(selPos, selChars);
            ProcessingText = false;
		}

		private void AddLongStrings()
		{
			string temp = LuaText.Text;
			int firstBracket = temp.IndexOf("["), secondBracket, ending;
			while (firstBracket >= 0)
			{
				ending = 0;
				if (firstBracket > 1 && temp.Substring(firstBracket - 2, 2) == "--")
				{
					firstBracket = temp.IndexOf("[", firstBracket + 1, temp.Length - firstBracket - 1);
					if (firstBracket == temp.Length - 1)
						break;
				}
				else if (firstBracket != temp.Length - 1)
				{
					secondBracket = temp.IndexOf("[", firstBracket + 1, temp.Length - firstBracket - 1);
					if (secondBracket >= 0 && IsLongString(temp.Substring(firstBracket, secondBracket - firstBracket + 1)))
					{
						if (secondBracket + 1 == temp.Length)
							ending = temp.Length - 1;
						else
						{
							string tempBracket = GetLongStringClosingBracket(temp.Substring(firstBracket, secondBracket - firstBracket + 1));
							ending = temp.IndexOf(tempBracket, secondBracket + 1);
							if (ending < 0)
								ending = temp.Length;
							else
								ending += tempBracket.Length - 1;
						}

						//Validate if such text is not part of a comment
						AddPosition(firstBracket, ending - firstBracket + 1, Global.Config.LuaStringColor, Global.Config.LuaStringBold);
						
						if (ending < temp.Length - 1)
							firstBracket = temp.IndexOf("[", ending + 1, temp.Length - ending - 1);
						else
							break;
					}
					else
					{
						if (secondBracket >= 0 && secondBracket != temp.Length - 1)
							firstBracket = temp.IndexOf("[", secondBracket, temp.Length - secondBracket - 1);
						else
							break;
					}
				}
				else
					break;
			}
		}

		private string GetLongStringClosingBracket(string openingBracket)
		{
			string closingBrackets = "]";
			int level = 0;

			foreach (char c in openingBracket)
				if (c == '=')
					level++;

			for (int x = 0; x < level; x++)
				closingBrackets += '=';

			return closingBrackets + ']';
		}

		private bool IsLongString(string longstring)
		{
			bool Validated = true;

			foreach (char c in longstring)
                if (c != '[' && c != '=')
                {
                    Validated = false;
                    break;
                }

			return Validated;
		}

		private void AddSymbols()
		{
			string temp = LuaText.Text;
			int selection;

			foreach (char mark in Symbols)
			{
				int currPos = 0;
				selection = temp.IndexOf(mark, currPos);

				while (selection >= 0)
				{
					//Validate if such text is not part of a string or comment
					AddPosition(selection, 1, Global.Config.LuaSymbolColor, Global.Config.LuaSymbolBold);

					currPos = selection + 1;
					selection = temp.IndexOf(mark, currPos);

					if (currPos == temp.Length)
						break;
				}
			}
		}

		private void AddStrings()
		{
			string temp = LuaText.Text;
			int firstMark, opening, ending, endLine;

			char[] chars = { '"', '\'' };
			foreach (char mark in chars)
			{
				firstMark = temp.IndexOf(mark.ToString());
				while (firstMark >= 0)
				{
					if (LuaText.SelectionColor.ToArgb() != Global.Config.LuaCommentColor)
					{
						opening = firstMark;
						if (LuaText.GetLineFromCharIndex(opening) + 1 == LuaText.Lines.Count())
							endLine = temp.Length - 1;
						else
							endLine = LuaText.GetFirstCharIndexFromLine(LuaText.GetLineFromCharIndex(opening) + 1) - 1;

						ending = 0;

						if (opening != temp.Length - 1)
						{
							if (opening + 1 != endLine)
							{
								ending = temp.IndexOf(mark, opening + 1, endLine - opening + 1);
								if (ending > 0)
								{
									while (ending > 0)
									{
										if (!IsThisPartOfTheString(temp.Substring(opening, ending - opening + 1)))
											break;
										else
											ending++;

										ending = temp.IndexOf(mark, ending, endLine - opening + 1);
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

						if (opening != temp.Length)
						{
							//Validate if such text is not part of a comment
							AddPosition(opening, ending - opening + 1, Global.Config.LuaStringColor, Global.Config.LuaStringBold);
							
							if (ending >= temp.Length)
								ending++;
							else
								break;

							firstMark = temp.IndexOf(mark, ending + 1);
						}
						else
							break;
					}
					else
						firstMark = temp.IndexOf(mark, firstMark + 1);
				}
			}
		}

		private bool IsThisPartOfTheString(string wholestring)
		{
			int ammount = 0;
			for (int x = wholestring.Length - 2; x > -1; x--)
			{
				if (wholestring[x] == '\\')
					ammount++;
				else
					break;
			}

			return !(ammount % 2 == 0);
		}

		private void AddComments()
		{
			string temp = LuaText.Text;
			foreach (Match match in new Regex("--").Matches(temp))
			{
				int selection, endComment;

				if (match.Index + 4 < temp.Length && temp.Substring(match.Index, 4) == "--[[")
				{
					selection = temp.IndexOf("]]");
					if (selection > 0)
						endComment = selection - match.Index + 2;
					else
						endComment = temp.Length;

					//Validate if such text is not part of a string
					AddPosition(match.Index, endComment, Global.Config.LuaCommentColor, Global.Config.LuaCommentBold);
				}
				else
				{
					if (LuaText.GetLineFromCharIndex(match.Index) + 1 == LuaText.Lines.Count())
						endComment = temp.Length - match.Index;
					else
						endComment = LuaText.GetFirstCharIndexFromLine(LuaText.GetLineFromCharIndex(match.Index) + 1) - match.Index;

					//Validate if such text is not part of a string
					AddPosition(match.Index, endComment, Global.Config.LuaCommentColor, Global.Config.LuaCommentBold);
				}
			}
		}

		private void AddKeyWords()
		{
			foreach (Match match in keyWords.Matches(LuaText.Text))
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
					//Validate if such text is not part of a string or comment
					AddPosition(match.Index, match.Length, Global.Config.LuaKeyWordColor, Global.Config.LuaKeyWordBold);
				}
			}
		}

		private void AddPosition(int start, int lenght, int color, bool bold)
		{
			int IsBold = 0, IndexToAdd = 0;
			if (bold)
				IsBold = 1;

			for(int x = 0; x < pos.Count; x++)
			{
				if (start < pos[x][0])
				{
					IndexToAdd = x;
					break;
				}
				if (x == pos.Count - 1)
					IndexToAdd = x + 1;
			}

			pos.Insert(IndexToAdd, new int[] { start, lenght, color, IsBold });
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
							//Validate if such text is not part of a string or comment
							AddPosition(match.Index, match.Length, Global.Config.LuaLibraryColor, Global.Config.LuaLibraryBold);
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
			LuaText.SelectionTabs = new int[] { 20, 40, 60, 80, 100, 120, 140, 160, 180, 200, 220, 240, 260, 280, 300, 320, 340, 360, 380, 400, 420, 480, 500, 520, 540, 560, 580, 600 }; //adelikat:  What a goofy way to have to do this
			LoadFont();
			LuaText.BackColor = Color.FromArgb(Global.Config.LuaWriterBackColor);
			LuaText.ZoomFactor = Global.Config.LuaWriterZoom;
			ZoomLabel.Text = string.Format("Zoom: {0}%", LuaText.ZoomFactor * 100);
			GenerateLibraryRegex();
			if (!String.IsNullOrWhiteSpace(CurrentFile))
			{
				LoadCurrentFile();
			}
			else if (!Global.Config.LuaWriterStartEmpty)
			{
				LuaText.Text = "while true do\n\t\n\temu.frameadvance()\nend";
				LuaText.SelectionStart = 15;
			}
			else
				startWithEmptyScriptToolStripMenuItem.Checked = true;

			UpdateLineNumber();
			ProcessText();
			NoChanges();
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
			e.Effect = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None; string[] filePaths = (string[])e.Data.GetData(DataFormats.FileDrop);
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
			if (!String.IsNullOrWhiteSpace(CurrentFile))
			{
				SaveScript();
			}
			else if (changes)
			{
                SaveScriptAs();
			}
            MessageLabel.Text = Path.GetFileName(CurrentFile) + " saved.";
		}

		private void SaveScript()
		{
			var file = new FileInfo(CurrentFile);

			using (StreamWriter sw = new StreamWriter(CurrentFile))
			{
                sw.Write(LuaText.Text);
			}

			NoChanges();
		}

		private void SaveScriptAs()
		{
			var file = GetSaveFileFromUser(CurrentFile);
			if (file != null)
			{
				CurrentFile = file.FullName;
				SaveScript();
				MessageLabel.Text = Path.GetFileName(CurrentFile) + " saved.";
				Global.Config.RecentWatches.Add(file.FullName);
			}
		}

		public static FileInfo GetSaveFileFromUser(string currentFile)
		{
			var sfd = new SaveFileDialog();
			if (currentFile.Length > 0)
			{
				sfd.FileName = Path.GetFileNameWithoutExtension(currentFile);
				sfd.InitialDirectory = Path.GetDirectoryName(currentFile);
			}
			else if (!(Global.Emulator is NullEmulator))
			{
				sfd.FileName = PathManager.FilesystemSafeName(Global.Game);
				sfd.InitialDirectory = PathManager.MakeAbsolutePath(Global.Config.LuaPath, "");
			}
			else
			{
				sfd.FileName = "NULL";
				sfd.InitialDirectory = PathManager.MakeAbsolutePath(Global.Config.LuaPath, "");
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
					
					int x = 0;
					int y = 0;

					int currentRow;
					int currentColumn;
					int fontHeight;
					int topRow;

					currentRow = LuaText.GetLineFromCharIndex(LuaText.SelectionStart);   //Currently selected row
                    topRow = 0;  //Need to figure this out, maybe LuaText.AutoScrollOffset?
                    currentColumn = LuaText.SelectionStart - LuaText.GetFirstCharIndexFromLine(currentRow);
					fontHeight = (int)LuaText.Font.GetHeight();   //Explicilty cast to int

					// Vertical position of auto complete box:
                    // This still needs to take into account the current scroll height of the box.  Currently it only will look correct when scrolled all the way to the top.
                    // ((Current row - Top row in view) + 1 to make it below the row) * (fontHeight + spaceBetweenLines)) + (Location of the top of the textbox in relation to overall control)
                    x = ((currentRow - topRow + 1) * (fontHeight + 1)) + LuaText.Location.Y;

                    // Horizontal position of auto complete box:
                    // (Width of each character in current font on the current line) + (Location of the left of the textbox in relation to overall control)
                    y = (currentColumn * (fontHeight / 2)) + LuaText.Location.X;  //    ¯\(°_o)/¯  Super-crude estimate for now, doesn't work great

					AutoCompleteView.Location = new Point(y, x);
					AutoCompleteView.Items.Clear();
					foreach(string function in libfunctions)
					{
						ListViewItem item = new ListViewItem(function);
						AutoCompleteView.Items.Add(item);
					}
                    AutoCompleteView.Visible = true;  //Show window after it has been positioned and set up
                }
			}

            if (e.KeyCode == Keys.Enter)
            {
                string[] Words = { "if", "for", "while", "function" };
                foreach (string Word in Words)
                {
                    try
                    {
                        int linenumber = LuaText.GetLineFromCharIndex(LuaText.GetFirstCharIndexOfCurrentLine());
                        int tabs = CountTabsAtBeginningOfLine(LuaText.Lines[linenumber]);
                        if (LuaText.Lines[linenumber].Substring(0 + tabs, Word.Length) == Word)
                        {
                            string str, tabsStr = "";

                            for (int a = 1; a <= tabs; a++)
                                tabsStr += "\t";

                            str = LuaText.Text.Insert(LuaText.SelectionStart, "\n" +  tabsStr + "\t\n" + tabsStr + "end");
                            LuaText.Text = str;
                            LuaText.Select(LuaText.GetFirstCharIndexFromLine(linenumber + 1) + 1 + tabs, 0);
                            e.SuppressKeyPress = true;
                            break;
                        }
                    }
                    catch { }
                }
            }
            
		}

		private string CurrentWord()
		{
			int last = LuaText.SelectionStart;

			int lastSpace = LuaText.Text.Substring(0, last).LastIndexOf(' ');
			int lastLine = LuaText.Text.Substring(0, last).LastIndexOf('\n');
			int start = 0;
			if (lastSpace > lastLine)
			{
				start = lastSpace;
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
			this.Close();
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
	}
}