using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Shell;
using System.IO;
using System.Reflection;

namespace BizHawk.Client.EmuHawk
{
	public class JumpLists
	{
		static Application _app;
		static JumpLists()
		{
			_app = new Application();
			var jmp = new JumpList();
			jmp.ShowRecentCategory = true;
			JumpList.SetJumpList(_app, jmp);
		}

		/// <summary>
		/// add an item to the W7+ jumplist
		/// </summary>
		/// <param name="fullpath">fully qualified path, can include '|' character for archives</param>
		public static void AddRecentItem(string fullpath, string title)
		{
			//string title;
			//if (fullpath.Contains('|'))
			//  title = fullpath.Split('|')[1];
			//else
			//  title = Path.GetFileName(fullpath);

			string exepath = Assembly.GetEntryAssembly().Location;

			var ji = new JumpTask
			{
				ApplicationPath = exepath,
				Arguments = '"' + fullpath + '"',
				Title = title,
				// for some reason, this doesn't work
				WorkingDirectory = Path.GetDirectoryName(exepath)
			};
			JumpList.AddToRecentCategory(ji);
		}
	}
}
