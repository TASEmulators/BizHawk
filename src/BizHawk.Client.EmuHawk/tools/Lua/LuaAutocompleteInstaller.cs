using System.IO;
using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public class LuaAutocompleteInstaller
	{
		public enum TextEditors { Sublime2, NotePad }

		public bool IsInstalled(TextEditors editor)
		{
			return editor switch
			{
				TextEditors.Sublime2 => IsSublimeInstalled(),
				TextEditors.NotePad => IsNotepadInstalled(),
				_ => false
			};
		}

		public bool IsBizLuaRegistered(TextEditors editor)
		{
			return editor switch
			{
				TextEditors.Sublime2 => IsBizLuaSublimeInstalled(),
				TextEditors.NotePad => IsBizLuaNotepadInstalled(),
				_ => false
			};
		}

		public void InstallBizLua(TextEditors editor, LuaDocumentation docs)
		{
			switch (editor)
			{
				case TextEditors.Sublime2:
					InstallBizLuaToSublime2(docs);
					break;
				case TextEditors.NotePad:
					InstallBizLuaToNotepad(docs);
					break;
			}
		}

		private string AppDataFolder => Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

		private bool IsSublimeInstalled()
		{
			// The most likely location of the app, eventually we should consider looking through the registry or installed apps as a more robust way to detect it;
			string exePath = @"C:\Program Files\Sublime Text 2\sublime_text.exe";
			return File.Exists(exePath);
		}

		private bool IsNotepadInstalled()
		{
			// The most likely location of the app, eventually we should consider looking through the registry or installed apps as a more robust way to detect it;
			string exePath = @"C:\Program Files (x86)\Notepad++\notepad++.exe";
			return File.Exists(exePath);
		}

		private readonly string SublimeLuaPath = @"Sublime Text 2\Packages\Lua";
		private readonly string SublimeCompletionsFilename = "bizhawk.lua.sublime-completions";

		private bool IsBizLuaSublimeInstalled()
		{
			var bizCompletions = Path.Combine(AppDataFolder, SublimeLuaPath, SublimeCompletionsFilename);
			return File.Exists(bizCompletions);
		}

		private readonly string NotepadPath = "TODO";
		private readonly string NotepadAutoCompleteFileName = "TODO";

		private bool IsBizLuaNotepadInstalled()
		{
			var bizCompletions = Path.Combine(AppDataFolder, NotepadPath, NotepadAutoCompleteFileName);
			return File.Exists(bizCompletions);
		}

		private void InstallBizLuaToSublime2(LuaDocumentation docs)
		{
			var bizCompletions = Path.Combine(AppDataFolder, SublimeLuaPath, SublimeCompletionsFilename);

			var text = docs.ToSublime2CompletionList();
			File.WriteAllText(bizCompletions, text);
		}

		private void InstallBizLuaToNotepad(LuaDocumentation docs)
		{
			var bizAutocomplete = Path.Combine(AppDataFolder, NotepadPath, NotepadAutoCompleteFileName);

			var text = docs.ToNotepadPlusPlusAutoComplete();

			// TODO
			//File.WriteAllText(bizCompletions, text);
		}
	}
}
