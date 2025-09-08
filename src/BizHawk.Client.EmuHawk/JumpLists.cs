using System.Reflection;
using System.Windows;
using System.Windows.Shell;

namespace BizHawk.Client.EmuHawk
{
	public static class JumpLists
	{
		static JumpLists()
		{
			var app = new Application();
			var jmp = new JumpList
			{
				ShowRecentCategory = true,
			};
			JumpList.SetJumpList(app, jmp);
		}

		/// <summary>
		/// add an item to the W7+ jumplist
		/// </summary>
		/// <param name="fullPath">fully qualified path, can include '|' character for archives</param>
		/// <param name="title">The text displayed in the jumplist entry</param>
		public static void AddRecentItem(string fullPath, string title)
		{
			string exepath = Assembly.GetEntryAssembly()!.Location;

			var ji = new JumpTask
			{
				ApplicationPath = exepath,
				Arguments = $"\"{fullPath}\"",
				Title = title,
			};
			JumpList.AddToRecentCategory(ji);
		}
	}
}
