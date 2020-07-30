using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace BizHawk.Common
{
	/// <summary>Implementors are able to provide pointers to functions in dynamically-linked libraries, which are loaded through some undefined mechanism.</summary>
	/// <seealso cref="PatchImportResolver"/>
	public interface IImportResolver
	{
		IntPtr GetProcAddrOrZero(string entryPoint);

		/// <exception cref="InvalidOperationException">could not find symbol</exception>
		IntPtr GetProcAddrOrThrow(string entryPoint);
	}

	public class DynamicLibraryImportResolver : IDisposable, IImportResolver
	{
		private static readonly IReadOnlyCollection<string> UnixSearchPaths;

		static DynamicLibraryImportResolver()
		{
			var currDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().CodeBase)?.Replace("file:", "") ?? string.Empty;
			var sysLibDir = Environment.GetEnvironmentVariable("BIZHAWK_INT_SYSLIB_PATH") ?? "/usr/lib";
			UnixSearchPaths = new[]
			{
				$"{currDir}/", $"{currDir}/dll/",
				$"{sysLibDir}/bizhawk/", $"{sysLibDir}/", $"{sysLibDir}/mupen64plus/"
			};
		}

		private static string UnixResolveFilePath(string orig) => orig[0] == '/'
			? orig
			: UnixSearchPaths.Select(dir => dir + orig)
				.FirstOrDefault(s =>
				{
					var fi = new FileInfo(s);
					return fi.Exists && (fi.Attributes & FileAttributes.Directory) != FileAttributes.Directory;
				})
				?? orig;

		private IntPtr _p;

		public readonly bool HasLimitedLifetime;

		/// <param name="hasLimitedLifetime">will never be unloaded iff false (like <see cref="DllImportAttribute">[DllImport]</see>)</param>
		public DynamicLibraryImportResolver(string dllName, bool hasLimitedLifetime = true)
		{
			_p = OSTailoredCode.LinkedLibManager.LoadOrThrow(OSTailoredCode.IsUnixHost ? UnixResolveFilePath(dllName) : dllName); // on Windows, EmuHawk modifies its process' search path
			HasLimitedLifetime = hasLimitedLifetime;
			if (!hasLimitedLifetime) GC.SuppressFinalize(this);
		}

		public IntPtr GetProcAddrOrZero(string entryPoint) => OSTailoredCode.LinkedLibManager.GetProcAddrOrZero(_p, entryPoint);

		public IntPtr GetProcAddrOrThrow(string entryPoint) => OSTailoredCode.LinkedLibManager.GetProcAddrOrThrow(_p, entryPoint);

		private void DisposeHelper()
		{
			if (!HasLimitedLifetime || _p == IntPtr.Zero) return;
			OSTailoredCode.LinkedLibManager.FreeByPtr(_p);
			_p = IntPtr.Zero;
		}

		public void Dispose()
		{
			DisposeHelper();
			GC.SuppressFinalize(this);
		}

		~DynamicLibraryImportResolver()
		{
			DisposeHelper();
		}
	}

	public class InstanceDll : IDisposable, IImportResolver
	{
		public IntPtr HModule { get; private set; }

		public InstanceDll(string dllPath)
		{
			// copy the dll to a temp directory
			var path = TempFileManager.GetTempFilename(Path.GetFileNameWithoutExtension(dllPath), ".dll", false);
			File.Copy(dllPath, path, true);
			// try to locate dlls in the current directory (for libretro cores)
			// this isn't foolproof but it's a little better than nothing
			// setting PWD temporarily doesn't work. that'd be ideal since it supposedly gets searched early on,
			// but i guess not with SetDllDirectory in effect
			var envpath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Process);
			try
			{
				var envpath_new = $"{Path.GetDirectoryName(path)};{envpath}";
				Environment.SetEnvironmentVariable("PATH", envpath_new, EnvironmentVariableTarget.Process);
				HModule = OSTailoredCode.LinkedLibManager.LoadOrThrow(path); // consider using LoadLibraryEx instead of shenanigans?
				var newfname = TempFileManager.RenameTempFilenameForDelete(path);
				File.Move(path, newfname);
			}
			catch
			{
				// ignored
			}
			Environment.SetEnvironmentVariable("PATH", envpath, EnvironmentVariableTarget.Process);
		}

		public IntPtr GetProcAddrOrZero(string procName) => OSTailoredCode.LinkedLibManager.GetProcAddrOrZero(HModule, procName);

		public IntPtr GetProcAddrOrThrow(string procName) => OSTailoredCode.LinkedLibManager.GetProcAddrOrThrow(HModule, procName);

		public void Dispose()
		{
			if (HModule == IntPtr.Zero) return; // already freed
			OSTailoredCode.LinkedLibManager.FreeByPtr(HModule);
			HModule = IntPtr.Zero;
		}
	}

	/// <summary>Aggregates <see cref="IImportResolver">resolvers</see>, resolving addresses by searching through them, starting with the last.</summary>
	public class PatchImportResolver : IImportResolver
	{
		private readonly List<IImportResolver> _resolvers;

		public PatchImportResolver(params IImportResolver[] resolvers)
		{
			_resolvers = resolvers.ToList();
		}

		public IntPtr GetProcAddrOrZero(string entryPoint)
		{
			for (var i = _resolvers.Count - 1; i != 0; i--)
			{
				var ret = _resolvers[i].GetProcAddrOrZero(entryPoint);
				if (ret != IntPtr.Zero) return ret;
			}
			return _resolvers[0].GetProcAddrOrZero(entryPoint); // if it's Zero/NULL, return it anyway - the search failed
		}

		public IntPtr GetProcAddrOrThrow(string entryPoint)
		{
			var ret = GetProcAddrOrZero(entryPoint);
			return ret != IntPtr.Zero ? ret : throw new InvalidOperationException($"{entryPoint} was not found in any of the aggregated resolvers");
		}
	}
}
