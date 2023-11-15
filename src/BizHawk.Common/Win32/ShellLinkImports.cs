#nullable disable

using System;
using System.Runtime.InteropServices;

// ReSharper disable FieldCanBeMadeReadOnly.Global
// ReSharper disable UnusedMember.Global

namespace BizHawk.Common
{
	public static class ShellLinkImports
	{
		[StructLayout(LayoutKind.Sequential)]
		public struct WIN32_FIND_DATAW
		{
			public uint dwFileAttributes;
			public FILETIME ftCreationTime;
			public FILETIME ftLastAccessTime;
			public FILETIME ftLastWriteTime;
			public uint nFileSizeHigh;
			public uint nFileSizeLow;
			public uint dwReserved0;
			public uint dwReserved1;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = Win32Imports.MAX_PATH)]
			public string cFileName;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)]
			public string cAlternateFileName;
			// Obsolete fields
			public uint dwFileType;
			public uint dwCreatorType;
			public int wFinderFlags;

			public struct FILETIME
			{
				public uint dwLowDateTime;
				public uint dwHighDateTime;
			}
		}

		/// <summary>The IShellLink interface allows Shell links to be created, modified, and resolved</summary>
		[StructLayout(LayoutKind.Sequential)]
		public unsafe struct IShellLinkW
		{
			public static readonly Guid Guid = new("000214F9-0000-0000-C000-000000000046");

			[StructLayout(LayoutKind.Sequential)]
			public struct IShellLinkWVtbl
			{
				// IUnknown functions
				public delegate* unmanaged[Stdcall]<IShellLinkW*, in Guid, out IntPtr, int> QueryInterface;
				public delegate* unmanaged[Stdcall]<IShellLinkW*, uint> AddRef;
				public delegate* unmanaged[Stdcall]<IShellLinkW*, uint> Release;
				// IShellLinkW functions
				public delegate* unmanaged[Stdcall]<IShellLinkW*, IntPtr, int, IntPtr, uint, int> GetPath;
				public delegate* unmanaged[Stdcall]<IShellLinkW*, out IntPtr, int> GetIDList;
				public delegate* unmanaged[Stdcall]<IShellLinkW*, IntPtr, int> SetIDList;
				public delegate* unmanaged[Stdcall]<IShellLinkW*, IntPtr, int, int> GetDescription;
				public delegate* unmanaged[Stdcall]<IShellLinkW*, IntPtr, int> SetDescription;
				public delegate* unmanaged[Stdcall]<IShellLinkW*, IntPtr, int, int> GetWorkingDirectory;
				public delegate* unmanaged[Stdcall]<IShellLinkW*, IntPtr, int> SetWorkingDirectory;
				public delegate* unmanaged[Stdcall]<IShellLinkW*, IntPtr, int, int> GetArguments;
				public delegate* unmanaged[Stdcall]<IShellLinkW*, IntPtr, int> SetArguments;
				public delegate* unmanaged[Stdcall]<IShellLinkW*, out short, int> GetHotkey;
				public delegate* unmanaged[Stdcall]<IShellLinkW*, short, int> SetHotkey;
				public delegate* unmanaged[Stdcall]<IShellLinkW*, out int, int> GetShowCmd;
				public delegate* unmanaged[Stdcall]<IShellLinkW*, int, int> SetShowCmd;
				public delegate* unmanaged[Stdcall]<IShellLinkW*, IntPtr, int, out int, int> GetIconLocation;
				public delegate* unmanaged[Stdcall]<IShellLinkW*, IntPtr, int, int> SetIconLocation;
				public delegate* unmanaged[Stdcall]<IShellLinkW*, IntPtr, int, int> SetRelativePath;
				public delegate* unmanaged[Stdcall]<IShellLinkW*, IntPtr, int, int> Resolve;
				public delegate* unmanaged[Stdcall]<IShellLinkW*, IntPtr, int> SetPath;
			}

			public IShellLinkWVtbl* lpVtbl;
		}

		[StructLayout(LayoutKind.Sequential)]
		public unsafe struct IPersist
		{
			public static readonly Guid Guid = new("0000010c-0000-0000-C000-000000000046");

			[StructLayout(LayoutKind.Sequential)]
			public struct IPersistVtbl
			{
				// IUnknown functions
				public delegate* unmanaged[Stdcall]<IPersist*, in Guid, out IntPtr, int> QueryInterface;
				public delegate* unmanaged[Stdcall]<IPersist*, uint> AddRef;
				public delegate* unmanaged[Stdcall]<IPersist*, uint> Release;
				// IPersist functions
				public delegate* unmanaged[Stdcall]<IPersist*, out Guid, int> GetClassID;
			}

			public IPersistVtbl* lpVtbl;
		}

		[StructLayout(LayoutKind.Sequential)]
		public unsafe struct IPersistFile
		{
			public static readonly Guid Guid = new("0000010b-0000-0000-C000-000000000046");

			[StructLayout(LayoutKind.Sequential)]
			public struct IPersistFileVtbl
			{
				// IUnknown functions
				public delegate* unmanaged[Stdcall]<IPersistFile*, in Guid, out IntPtr, int> QueryInterface;
				public delegate* unmanaged[Stdcall]<IPersistFile*, uint> AddRef;
				public delegate* unmanaged[Stdcall]<IPersistFile*, uint> Release;
				// IPersist functions
				public delegate* unmanaged[Stdcall]<IPersistFile*, out Guid, int> GetClassID;
				// IPersistFile functions
				public delegate* unmanaged[Stdcall]<IPersistFile*, int> IsDirty;
				public delegate* unmanaged[Stdcall]<IPersistFile*, IntPtr, uint, int> Load;
				public delegate* unmanaged[Stdcall]<IPersistFile*, IntPtr, bool, int> Save;
				public delegate* unmanaged[Stdcall]<IPersistFile*, IntPtr, int> SaveCompleted;
				public delegate* unmanaged[Stdcall]<IPersistFile*, out IntPtr, int> GetCurFile;
			}

			public IPersistFileVtbl* lpVtbl;
		}

		/// <remarks>CLSID_ShellLink from ShlGuid.h</remarks>
		public unsafe class ShellLink : IDisposable
		{
			public static readonly Guid Guid = new("00021401-0000-0000-C000-000000000046");

			private IShellLinkW* SLI;
			private IPersistFile* PFI;

			public ShellLink()
			{
				var hr = Ole32Imports.CoCreateInstance(Guid, IntPtr.Zero, Ole32Imports.CLSCTX.INPROC_SERVER, IShellLinkW.Guid, out var psl);
				Marshal.ThrowExceptionForHR(hr);

				var sli = (IShellLinkW*)psl;
				hr = sli->lpVtbl->QueryInterface(sli, in IPersist.Guid, out var ppf);
				var hrEx = Marshal.GetExceptionForHR(hr);
				if (hrEx != null)
				{
					sli->lpVtbl->Release(sli);
					throw hrEx;
				}

				SLI = sli;
				PFI = (IPersistFile*)ppf;
			}

			public void Dispose()
			{
				if (PFI != null)
				{
					PFI->lpVtbl->Release(PFI);
					PFI = null;
				}

				if (SLI != null)
				{
					SLI->lpVtbl->Release(SLI);
					SLI = null;
				}
			}

			public void GetPath(out string pszFile, int cch, out WIN32_FIND_DATAW pfd, uint fFlags)
			{
				var pszFile_ = Marshal.AllocCoTaskMem(cch * sizeof(char));
#if false // should we do this? we don't need pfd (NULL is valid), and we could delete the WIN32_FIND_DATAW definition by doing this
				var hr = SLI->lpVtbl->GetPath(SLI, pszFile_, cch, IntPtr.Zero, fFlags);
#else
				var pfd_ = Marshal.AllocCoTaskMem(Marshal.SizeOf<WIN32_FIND_DATAW>());
				var hr = SLI->lpVtbl->GetPath(SLI, pszFile_, cch, pfd_, fFlags);
#endif
				try
				{
					Marshal.ThrowExceptionForHR(hr);
					pszFile = Marshal.PtrToStringUni(pszFile_);
					pfd = Marshal.PtrToStructure<WIN32_FIND_DATAW>(pfd_);
				}
				finally
				{
					Marshal.FreeCoTaskMem(pszFile_);
					Marshal.FreeCoTaskMem(pfd_);
				}
			}

#if false
			public void Resolve(IntPtr hwnd, int fFlags)
			{
				var hr = SLI->lpVtbl->Resolve(SLI, hwnd, fFlags);
				Marshal.ThrowExceptionForHR(hr);
			}
#endif

			public void Load(string pszFileName, uint dwMode)
			{
				var pszFileName_ = Marshal.StringToCoTaskMemUni(pszFileName);
				var hr = PFI->lpVtbl->Load(PFI, pszFileName_, dwMode);
				Marshal.FreeCoTaskMem(pszFileName_);
				Marshal.ThrowExceptionForHR(hr);
			}
		}
	}
}
