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

			ColorReservedWords();
			ColorLibraries();
			ColorSymbols();
			ColorComments();
			ColorStrings();
			ColorLongStrings();
			LuaText.Select(selPos, selChars);
            ProcessingText = false;
		}

		private void ColorLongStrings()
		{
			int firstBracket = LuaText.Find("["), secondBracket, ending;
			while (firstBracket >= 0)
			{
				ending = 0;
				if (firstBracket > 1 && LuaText.Text.Substring(firstBracket - 2, 2) == "--")
				{
					firstBracket = LuaText.Find("[", firstBracket + 1, LuaText.Text.Length, RichTextBoxFinds.MatchCase);
					if (firstBracket == LuaText.Text.Length - 1)
						break;
				}
				else if (firstBracket != LuaText.Text.Length - 1)
				{
					secondBracket = LuaText.Find("[", firstBracket + 1, LuaText.Text.Length, RichTextBoxFinds.MatchCase);
                    if (secondBracket >= 0 && IsLongString(LuaText.Text.Substring(firstBracket, secondBracket - firstBracket + 1)))
                    {
                        if (secondBracket + 1 == LuaText.Text.Length)
                            ending = LuaText.Text.Length - 1;
                        else
                        {
                            string temp = GetLongStringClosingBracket(LuaText.Text.Substring(firstBracket, secondBracket - firstBracket + 1));
                            ending = LuaText.Find(temp, secondBracket + 1, LuaText.Text.Length, RichTextBoxFinds.MatchCase);
                            if (ending < 0)
                                ending = LuaText.Text.Length;
                            else
                                ending += temp.Length - 1;
                        }

                        LuaText.Select(firstBracket, ending - firstBracket + 1);
                        LuaText.SelectionColor = Color.FromArgb(Global.Config.LuaStringColor);
						if(Global.Config.LuaStringBold)
							LuaText.SelectionFont = new Font(LuaText.SelectionFont, FontStyle.Bold);
						else
							LuaText.SelectionFont = new Font(LuaText.SelectionFont, FontStyle.Regular);

                        if (ending < LuaText.Text.Length - 1)
                            firstBracket = LuaText.Find("[", ending + 1, LuaText.Text.Length, RichTextBoxFinds.MatchCase);
                        else
                            break;
                    }
                    else
                    {
                        if(secondBracket >= 0 && secondBracket != LuaText.Text.Length - 1)
                            firstBracket = LuaText.Find("[", secondBracket, LuaText.Text.Length, RichTextBoxFinds.MatchCase);
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

		private void ColorSymbols()
		{
			foreach (char mark in Symbols)
			{
				int currPos = 0;
				while (LuaText.Find(mark.ToString(), currPos, RichTextBoxFinds.None) >= 0)
				{
					LuaText.SelectionColor = Color.FromArgb(Global.Config.LuaSymbolColor);
                    if (Global.Config.LuaSymbolBold)
                        LuaText.SelectionFont = new Font(LuaText.SelectionFont, FontStyle.Bold);
					else
						LuaText.SelectionFont = new Font(LuaText.SelectionFont, FontStyle.Regular);

					currPos = LuaText.SelectionStart + 1;

					if (currPos == LuaText.Text.Length)
						break;
				}
			}
		}

		private void ColorStrings()
		{
			int firstMark, opening, ending, endLine;

			char[] chars = { '"', '\'' };
			foreach (char mark in chars)
			{
				firstMark = LuaText.Find(mark.ToString());
				while (firstMark >= 0)
				{
					if (LuaText.SelectionColor.ToArgb() != Global.Config.LuaCommentColor)
					{
						opening = firstMark;
						if (LuaText.GetLineFromCharIndex(opening) + 1 == LuaText.Lines.Count())
							endLine = LuaText.Text.Length - 1;
						else
							endLine = LuaText.GetFirstCharIndexFromLine(LuaText.GetLineFromCharIndex(opening) + 1) - 1;

						ending = 0;

						if (opening != LuaText.Text.Length - 1)
						{
							if (opening + 1 != endLine)
							{
								ending = LuaText.Find(mark.ToString(), opening + 1, endLine, RichTextBoxFinds.MatchCase);
								if (ending > 0)
								{
									while (ending > 0)
									{
										if (!IsThisPartOfTheString(LuaText.Text.Substring(opening, ending - opening + 1)))
											break;
										else
											ending++;

										ending = LuaText.Find(mark.ToString(), ending, endLine, RichTextBoxFinds.MatchCase);
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

						if (opening != LuaText.Text.Length)
						{
							LuaText.Select(opening, ending - opening + 1);
							LuaText.SelectionColor = Color.FromArgb(Global.Config.LuaStringColor);
                            if (Global.Config.LuaStringBold)
                                LuaText.SelectionFont = new Font(LuaText.SelectionFont, FontStyle.Bold);
							else
								LuaText.SelectionFont = new Font(LuaText.SelectionFont, FontStyle.Regular);

							if (ending >= LuaText.Text.Length)
								ending++;
							else
								break;

							firstMark = LuaText.Find(mark.ToString(), ending + 1, LuaText.Text.Length, RichTextBoxFinds.MatchCase);
						}
						else
							break;
					}
					else
						firstMark = LuaText.Find(mark.ToString(), firstMark + 1, LuaText.Text.Length, RichTextBoxFinds.MatchCase);
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

		private void ColorComments()
		{
			foreach (Match CommentMatch in new Regex("--").Matches(LuaText.Text))
			{
				int endComment;

				if (CommentMatch.Index + 4 < LuaText.Text.Length && LuaText.Text.Substring(CommentMatch.Index, 4) == "--[[")
				{
					if (LuaText.Find("]]", RichTextBoxFinds.MatchCase) > 0)
						endComment = LuaText.SelectionStart - CommentMatch.Index + 2;
					else
						endComment = LuaText.Text.Length;

					LuaText.Select(CommentMatch.Index, endComment);
					LuaText.SelectionColor = Color.FromArgb(Global.Config.LuaCommentColor);
                    if (Global.Config.LuaCommentBold)
                        LuaText.SelectionFont = new Font(LuaText.SelectionFont, FontStyle.Bold);
					else
						LuaText.SelectionFont = new Font(LuaText.SelectionFont, FontStyle.Regular);
				}
				else
				{
					if (LuaText.GetLineFromCharIndex(CommentMatch.Index) + 1 == LuaText.Lines.Count())
						endComment = LuaText.Text.Length - CommentMatch.Index;
					else
						endComment = LuaText.GetFirstCharIndexFromLine(LuaText.GetLineFromCharIndex(CommentMatch.Index) + 1) - CommentMatch.Index;

					LuaText.Select(CommentMatch.Index, endComment);
					LuaText.SelectionColor = Color.FromArgb(Global.Config.LuaCommentColor);
                    if (Global.Config.LuaCommentBold)
                        LuaText.SelectionFont = new Font(LuaText.SelectionFont, FontStyle.Bold);
					else
						LuaText.SelectionFont = new Font(LuaText.SelectionFont, FontStyle.Regular);
				}
			}
		}

		private void ColorReservedWords()
		{
			foreach (Match keyWordMatch in keyWords.Matches(LuaText.Text))
			{
				char before = ' ', after = ' ';

				if (keyWordMatch.Index > 0)
					if (keyWordMatch.Index > 5 && keyWordMatch.Value != "if" && LuaText.Text.Substring(keyWordMatch.Index - 4, 4) != "else")
						before = LuaText.Text[keyWordMatch.Index - 1];

				if (keyWordMatch.Index + keyWordMatch.Length != LuaText.Text.Length)
					if (keyWordMatch.Value != "else" && LuaText.Text.Substring(keyWordMatch.Index, 2) != "if")
						after = LuaText.Text[keyWordMatch.Index + keyWordMatch.Length];

				if (!char.IsLetterOrDigit(before) && !char.IsLetterOrDigit(after))
				{
					LuaText.Select(keyWordMatch.Index, keyWordMatch.Length);
					LuaText.SelectionColor = Color.FromArgb(Global.Config.LuaKeyWordColor);
                    if (Global.Config.LuaKeyWordBold)
                        LuaText.SelectionFont = new Font(LuaText.SelectionFont, FontStyle.Bold);
					else
						LuaText.SelectionFont = new Font(LuaText.SelectionFont, FontStyle.Regular);
				}
			}
		}

		private void ColorLibraries()
		{
			foreach (Match libraryWordMatch in libraryWords.Matches(LuaText.Text))
			{
				if (libraryWordMatch.Index >= 0)
				{
                    char before = ' ', after = ' ';

                    if (libraryWordMatch.Index > 0)
                        before = LuaText.Text[libraryWordMatch.Index - 1];

                    if (libraryWordMatch.Index + libraryWordMatch.Length != LuaText.Text.Length)
                        after = LuaText.Text[libraryWordMatch.Index + libraryWordMatch.Length];

					if (!char.IsLetterOrDigit(before))
					{
						if (after == '.')
						{
							LuaText.Select(libraryWordMatch.Index, libraryWordMatch.Length);
							LuaText.SelectionColor = Color.FromArgb(Global.Config.LuaLibraryColor);
                            if (Global.Config.LuaLibraryBold)
                                LuaText.SelectionFont = new Font(LuaText.SelectionFont, FontStyle.Bold);
							else
								LuaText.SelectionFont = new Font(LuaText.SelectionFont, FontStyle.Regular);
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
					
					/*  This part doesn't work yet.
					int x = 0;
					int y = 0;

					int currentRow;
					int currentColumn;
					int fontHeight;
					int topRow;

					currentRow = LuaText.GetLineFromCharIndex(LuaText.SelectionStart);   //Currently selected row
					currentColumn = LuaText.SelectionStart - LuaText.GetFirstCharIndexFromLine(currentRow);
					fontHeight = (int)LuaText.Font.GetHeight();   //Explicilty cast to int (may be a problem later)

					x = ((currentRow + 1) * fontHeight) + LuaText.Location.Y;
					y = 50;

					AutoCompleteView.Location = new Point(y, x);
					*/
	
					AutoCompleteView.Location = new Point(0, 0);
					AutoCompleteView.Visible = true;
					AutoCompleteView.Items.Clear();
					foreach(string function in libfunctions)
					{
						ListViewItem item = new ListViewItem(function);
						AutoCompleteView.Items.Add(item);
					}
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