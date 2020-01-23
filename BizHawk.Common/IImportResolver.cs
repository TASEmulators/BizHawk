#nullable disable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.AccessControl;

namespace BizHawk.Common
{
	/// <summary>Implementors are able to provide pointers to functions in dynamically-linked libraries, which are loaded through some undefined mechanism.</summary>
	/// <seealso cref="PatchImportResolver"/>
	public interface IImportResolver
	{
		IntPtr? GetProcAddrOrNull(string entryPoint);

		/// <exception cref="InvalidOperationException">could not find symbol</exception>
		IntPtr GetProcAddrOrThrow(string entryPoint);
	}

	public class DynamicLibraryImportResolver : IDisposable, IImportResolver
	{
		private static readonly Lazy<IEnumerable<string>> asdf = new Lazy<IEnumerable<string>>(() =>
		{
			var currDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().CodeBase).Replace("file:", "");
			return new[] { "/usr/lib/", "/usr/lib/bizhawk/", "./", "./dll/" }.Select(dir => dir[0] == '.' ? currDir + dir.Substring(1) : dir);
		});

		private IntPtr _p;

		public DynamicLibraryImportResolver(string dllName)
		{
			static string ResolveFilePath(string orig) => orig[0] == '/' ? orig : asdf.Value.Select(dir => dir + orig).FirstOrDefault(File.Exists) ?? orig;
			_p = OSTailoredCode.LinkedLibManager.LoadOrThrow(OSTailoredCode.IsUnixHost ? ResolveFilePath(dllName) : dllName);
		}

		public IntPtr? GetProcAddrOrNull(string entryPoint) => OSTailoredCode.LinkedLibManager.GetProcAddrOrNull(_p, entryPoint);

		public IntPtr GetProcAddrOrThrow(string entryPoint) => OSTailoredCode.LinkedLibManager.GetProcAddrOrThrow(_p, entryPoint);

		private void DisposeHelper()
		{
			if (_p == IntPtr.Zero) return; // already freed
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

		public IntPtr? GetProcAddrOrNull(string procName) => OSTailoredCode.LinkedLibManager.GetProcAddrOrNull(HModule, procName);

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

		public IntPtr? GetProcAddrOrNull(string entryPoint)
		{
			for (var i = _resolvers.Count - 1; i != -1; i--)
			{
				var ret = _resolvers[i].GetProcAddrOrNull(entryPoint);
				if (ret != null) return ret.Value;
			}
			return null;
		}

		public IntPtr GetProcAddrOrThrow(string entryPoint) => GetProcAddrOrNull(entryPoint) ?? throw new InvalidOperationException($"{entryPoint} was not found in any of the aggregated resolvers");
	}
}
