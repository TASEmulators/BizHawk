using System.Reflection;
using System.Text.RegularExpressions;
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
				Arguments = QuoteAndEscapeArgument(fullPath),
				Title = title,
			};
			JumpList.AddToRecentCategory(ji);
		}

		/// <summary>
		/// Wrap a single command line argument in double quotes and escape characters
		/// so that it correctly roundtrips back to <c>args[]</c> as a single argument.
		/// </summary>
		private static string QuoteAndEscapeArgument(string argument)
		{
			if (!string.IsNullOrEmpty(argument) && argument.IndexOfAny([ ' ', '"', '\t', '\n' ]) == -1)
				return argument;

			// Windows/.NET command line mangling
			// see https://learn.microsoft.com/en-us/dotnet/api/system.environment.getcommandlineargs#remarks

			// any double quote needs to be escaped with a backslash
			// any series of backslashes *directly preceding* a double quote also need to be escaped
			// so basically: insert \ before ", double any existing \ before a "
			// special case: backslashes at the end of the string also need to be escaped (so the wrapping double quote isn't)
			return '"' + Regex.Replace(argument ?? "",
				@"(\\*)(""|\\\z)", // group $1: 0 or more backslashes; group $2: double quote, or backslash at end of string
				"$1$1\\$2" // repeat group $1 twice, then backslash and group $2
			) + '"';
		}
	}
}
