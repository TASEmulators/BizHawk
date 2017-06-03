using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;

namespace BizHawk.Client.EmuHawk
{
	public class JumpLists
	{
		static readonly Assembly PresentationFramework;
		static Type Application;
		static Type JumpList;
		static Type JumpTask;

		static object _app;
		static JumpLists()
		{
			try
			{
				PresentationFramework = Assembly.Load("PresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35");
				Application = PresentationFramework.GetType("System.Windows.Application");
				JumpList = PresentationFramework.GetType("System.Windows.Shell.JumpList");
				JumpTask = PresentationFramework.GetType("System.Windows.Shell.JumpTask");
				_app = Activator.CreateInstance(Application);
				dynamic jmp = Activator.CreateInstance(JumpList);
				jmp.ShowRecentCategory = true;
				JumpList.GetMethod("SetJumpList").Invoke(null, new[] { _app, jmp });
			}
			catch { }
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
			try
			{
				string exepath = Assembly.GetEntryAssembly().Location;

				dynamic ji = Activator.CreateInstance(JumpTask);

				ji.ApplicationPath = exepath;
				ji.Arguments = '"' + fullpath + '"';
				ji.Title = title;
				// for some reason, this doesn't work
				ji.WorkingDirectory = Path.GetDirectoryName(exepath);

				JumpList.GetMethod("AddToRecentCategory", new[] { JumpTask }).Invoke(null, new[] { ji });
			}
			catch { }
		}
	}
}
