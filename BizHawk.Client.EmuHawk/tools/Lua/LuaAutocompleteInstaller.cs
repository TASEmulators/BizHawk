using System;
using System.IO;

namespace BizHawk.Client.EmuHawk
{
	public class LuaAutocompleteInstaller
	{
		#region API

		public enum TextEditors { Sublime2, NotePad }

		public bool IsInstalled(TextEditors editor)
		{
			switch (editor)
			{
				case TextEditors.Sublime2:
					return IsSublimeInstalled();
				case TextEditors.NotePad:
					return IsNotepadInstalled();
			}

			return false;
		}

		public bool IsBizLuaRegistered(TextEditors editor)
		{
			switch (editor)
			{
				case TextEditors.Sublime2:
					return IsBizLuaSublimeInstalled();
				case TextEditors.NotePad:
					return IsBizLuaNotepadInstalled();
			}

			return false;
		}

		public void InstallBizLua(TextEditors editor)
		{
			switch (editor)
			{
				case TextEditors.Sublime2:
					InstallBizLuaToSublime2();
					break;
				case TextEditors.NotePad:
					InstallBizLuaToNotepad();
					break;
			}
		}

		#endregion

		private string AppDataFolder => Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

		private bool IsSublimeInstalled()
		{
			// The most likely location of the app, eventually we should consider looking through the registry or installed apps as a more robust way to detect it;
			string exePath = @"C:\Program Files\Sublime Text 2\sublime_text.exe";

			if (File.Exists(exePath))
			{
				return true;
			}

			return false;
		}

		private bool IsNotepadInstalled()
		{
			// The most likely location of the app, eventually we should consider looking through the registry or installed apps as a more robust way to detect it;
			string exePath = @"C:\Program Files (x86)\Notepad++\notepad++.exe";

			if (File.Exists(exePath))
			{
				return true;
			}

			return false;
		}

		private string SublimeLuaPath = @"Sublime Text 2\Packages\Lua";
		private string SublimeCompletionsFilename = "bizhawk.lua.sublime-completions";

		private bool IsBizLuaSublimeInstalled()
		{
			var bizCompletions = Path.Combine(AppDataFolder, SublimeLuaPath, SublimeCompletionsFilename);

			if (File.Exists(bizCompletions))
			{
				return true;
			}

			return false;
		}

		private string NotepadPath = "TODO";
		private string NotepadAutoCompleteFileName = "TODO";

		private bool IsBizLuaNotepadInstalled()
		{
			var bizCompletions = Path.Combine(AppDataFolder, NotepadPath, NotepadAutoCompleteFileName);

			if (File.Exists(bizCompletions))
			{
				return true;
			}

			return false;
		}

		private void InstallBizLuaToSublime2()
		{
			var bizCompletions = Path.Combine(AppDataFolder, SublimeLuaPath, SublimeCompletionsFilename);

			var text = GlobalWin.Tools.LuaConsole.LuaImp.Docs.ToSublime2CompletionList();
			File.WriteAllText(bizCompletions, text);
		}

		private void InstallBizLuaToNotepad()
		{
			var bizAutocomplete = Path.Combine(AppDataFolder, NotepadPath, NotepadAutoCompleteFileName);

			var text = GlobalWin.Tools.LuaConsole.LuaImp.Docs.ToNotepadPlusPlusAutoComplete();

			// TODO
			//File.WriteAllText(bizCompletions, text);
		}
	}
}
