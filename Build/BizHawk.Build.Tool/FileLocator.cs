using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Win32;

static class FileLocator
{
	public static string LocateTool(string _toolName)
	{
		string t = ToolPathUtil.MakeToolName(_toolName);
		string dir = null;
		try
		{
			dir = FindToolPath(t);
		}
		catch { }
		if (dir == null)
			return "";
		else
			return System.IO.Path.Combine(dir, t);

	}

	//stolen from MSBuild.Community.Tasks
	static string FindToolPath(string toolName)
	{
		string toolPath =
				ToolPathUtil.FindInRegistry(toolName) ??
				ToolPathUtil.FindInPath(toolName) ??
				ToolPathUtil.FindInProgramFiles(toolName,
						@"Subversion\bin",
						@"CollabNet Subversion Server",
						@"CollabNet Subversion",
						@"CollabNet Subversion Client",
						@"VisualSVN\bin",
						@"VisualSVN Server\bin",
						@"TortoiseSVN\bin",
						@"SlikSvn\bin",
						@"Git\bin"
						);

		if (toolPath == null)
		{
			throw new Exception("Could not find svn.exe.  Looked in PATH locations and various common folders inside Program Files.");
		}

		return toolPath;
	}


}
