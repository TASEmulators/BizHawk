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

namespace BizHawk.MultiClient
{
	public partial class LuaWriter : Form
	{
		public string CurrentFile = "";

		bool changes = false;
		bool hasChanged;
		public Regex keyWords = new Regex("and|break|do|else|if|end|false|for|function|in|local|nil|not|or|repeat|return|then|true|until|while|elseif");
		char[] Symbols = { '+','-','*','/','%','^','#','=','<','>','(',')','{','}','[',']',';',':',',','.' };


		public LuaWriter()
		{
			InitializeComponent();
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
			int selPos = LuaText.SelectionStart;
			int selChars = LuaText.SelectedText.Length;

			LuaText.SelectAll();
			LuaText.SelectionColor = Color.FromArgb(Global.Config.LuaDefaultTextColor);

			ColorReservedWords();
			ColorComments();
			ColorStrings();
            ColorSymbols();

			LuaText.Select(selPos, selChars);
		}

        private void ColorSymbols()
        {
            foreach (char mark in Symbols)
            {
                int currPos = 0;
                while (LuaText.Find(mark.ToString(), currPos, RichTextBoxFinds.None) >= 0)
                {
                    if (LuaText.SelectionColor.ToArgb() != Global.Config.LuaCommentColor && LuaText.SelectionColor.ToArgb() != Global.Config.LuaStringColor)
                        LuaText.SelectionColor = Color.FromArgb(Global.Config.LuaSymbolsColor);
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

				if (LuaText.Text.Substring(CommentMatch.Index, 4) == "--[[")
				{
					if (LuaText.Find("]]", RichTextBoxFinds.MatchCase) > 0)
						endComment = LuaText.SelectionStart - CommentMatch.Index + 2;
					else
						endComment = LuaText.Text.Length;

					LuaText.Select(CommentMatch.Index, endComment);
					LuaText.SelectionColor = Color.FromArgb(Global.Config.LuaCommentColor);
				}
				else
				{
					if (LuaText.GetLineFromCharIndex(CommentMatch.Index) + 1 == LuaText.Lines.Count())
						endComment = LuaText.Text.Length - CommentMatch.Index;
					else
						endComment = LuaText.GetFirstCharIndexFromLine(LuaText.GetLineFromCharIndex(CommentMatch.Index) + 1) - CommentMatch.Index;

					LuaText.Select(CommentMatch.Index, endComment);
					LuaText.SelectionColor = Color.FromArgb(Global.Config.LuaCommentColor);
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
				}
			}
		}

		private void LuaWriter_Load(object sender, EventArgs e)
        {
			if (!String.IsNullOrWhiteSpace(CurrentFile))
			{
				LoadCurrentFile();
				ProcessText();
				NoChanges();
			}
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

			using (StreamReader sr = file.OpenText())
			{
				StringBuilder luaText = new StringBuilder();
				string s = "";
				while ((s = sr.ReadLine()) != null)
				{
					luaText.Append(s);
					luaText.Append('\n');
				}

				if (luaText.Length > 0)
				{
					LuaText.Text = luaText.ToString();
				}
			}

			MessageLabel.Text = CurrentFile;
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
				MessageLabel.Text = Path.GetFileName(CurrentFile) + " saved.";
			}
		}

		private void SaveScript()
		{
			var file = new FileInfo(CurrentFile);

			using (StreamWriter sw = new StreamWriter(CurrentFile))
			{
				foreach (string s in LuaText.Lines)
				{
					sw.WriteLine(s + '\n');
				}
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
			hasChanged = true;
		}

		private void Changes()
		{
			changes = true;
			MessageLabel.Text = CurrentFile + " *";
		}

		private void LuaText_TextChanged(object sender, EventArgs e)
		{
			Changes();
		}

		private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			SaveScriptAs();
		}
	}
}