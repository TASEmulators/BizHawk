using System.IO;
using System.Reflection;

namespace BizHawk.Client.EmuHawk
{
	public static class JumpLists
	{
		private static readonly Type JumpList;
		private static readonly Type JumpTask;

		static JumpLists()
		{
			try
			{
				var presentationFramework =
					Assembly.Load(
						"PresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35");
				var application = presentationFramework.GetType("System.Windows.Application");
				JumpList = presentationFramework.GetType("System.Windows.Shell.JumpList");
				JumpTask = presentationFramework.GetType("System.Windows.Shell.JumpTask");
				var app = Activator.CreateInstance(application);
				dynamic jmp = Activator.CreateInstance(JumpList);
				jmp.ShowRecentCategory = true;
				JumpList
					.GetMethod("SetJumpList")
					?.Invoke(null, new[] {app, jmp});
			}
			catch
			{
				// Do nothing
			}
		}

		/// <summary>
		/// add an item to the W7+ jumplist
		/// </summary>
		/// <param name="fullPath">fully qualified path, can include '|' character for archives</param>
		/// <param name="title">The text displayed in the jumplist entry</param>
		public static void AddRecentItem(string fullPath, string title)
		{
			try
			{
				var execPath = AppContext.BaseDirectory;
				dynamic ji = Activator.CreateInstance(JumpTask);

				ji.ApplicationPath = execPath;
				ji.Arguments = $"\"{fullPath}\"";
				ji.Title = title;
				// for some reason, this doesn't work
				ji.WorkingDirectory = Path.GetDirectoryName(execPath);

				JumpList
					.GetMethod("AddToRecentCategory", new[] {JumpTask})
					?.Invoke(null, new[] {ji});
			}
			catch
			{
				// Do nothing
			}
		}
	}
}
