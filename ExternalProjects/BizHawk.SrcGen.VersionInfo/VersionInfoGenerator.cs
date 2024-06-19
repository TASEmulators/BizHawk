namespace BizHawk.SrcGen.VersionInfo;

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;

[Generator]
public class VersionInfoGenerator : ISourceGenerator
{
	public void Initialize(GeneratorInitializationContext context)
	{
	}

	private static string? ExecuteGitWithArguments(string arguments)
	{
		var startInfo = new ProcessStartInfo("git", arguments)
		{
			RedirectStandardOutput = true,
			CreateNoWindow = true,
			UseShellExecute = false // this is just required for visual studio (:
		};
		try
		{
			using Process git = Process.Start(startInfo) ?? throw new Exception("Failed to start git process");
			git.WaitForExit();
			return git.StandardOutput.ReadLine();
		}
#if DEBUG
		catch (Exception e)
		{
			return $"{e.GetType()}: {e.Message}";
		}
#else
		catch (Exception)
		{
			return null;
		}
#endif
	}

	public void Execute(GeneratorExecutionContext context)
	{
		// Finds the current project directory in order to pass to git commands.
		// This is written in a way to (hopefully) work both for build and IDE analyzers
		// FIXME: This should probably be done in a better way, but I haven't found any
		string projectDir = Path.GetDirectoryName(context.Compilation.SyntaxTrees.First(x => x.HasCompilationUnitRoot && x.FilePath.Contains("BizHawk.Common")).FilePath)!;

		var rev = ExecuteGitWithArguments($"-C {projectDir} rev-list HEAD --count") ?? string.Empty;
		var branch = ExecuteGitWithArguments($"-C {projectDir} rev-parse --abbrev-ref HEAD") ?? "master";
		var hash = ExecuteGitWithArguments($"-C {projectDir} log -1 --format=\"%H\"") ?? "0000000000000000000000000000000000000000";

		// Generated source code
		string source = $@"namespace BizHawk.Common
{{
	public static partial class VersionInfo
	{{
		public const string SVN_REV = ""{rev}"";
		public const string GIT_BRANCH = ""{branch}"";
		public const string GIT_HASH = ""{hash}"";
		public const string GIT_SHORTHASH = ""{hash.Substring(startIndex: 0, length: 9)}"";
	}}
}}
";

		// Add the source code to the compilation
		context.AddSource("VersionInfo.g.cs", source);
	}
}
