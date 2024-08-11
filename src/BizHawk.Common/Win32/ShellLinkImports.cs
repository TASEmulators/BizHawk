#nullable disable

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// ReSharper disable FieldCanBeMadeReadOnly.Global
// ReSharper disable UnusedMember.Global

namespace BizHawk.Common
{
	public static class ShellLinkImports
	{
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

			public void GetPath(out string pszFile, int cch, uint fFlags)
			{
				var _pszFile = Marshal.AllocCoTaskMem(cch * sizeof(char));
				try
				{
					var hr = lpVtbl->GetPath((IShellLinkW*)Unsafe.AsPointer(ref this), _pszFile, cch, IntPtr.Zero, fFlags);
					Marshal.ThrowExceptionForHR(hr);
					pszFile = Marshal.PtrToStringUni(_pszFile);
				}
				finally
				{
					Marshal.FreeCoTaskMem(_pszFile);
				}
			}

#if false
			public void Resolve(IntPtr hwnd, int fFlags)
			{
				var hr = lpVtbl->Resolve((IShellLinkW*)Unsafe.AsPointer(ref this), hwnd, fFlags);
				Marshal.ThrowExceptionForHR(hr);
			}
#endif
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

			public void Load(string pszFileName, uint dwMode)
			{
				var _pszFileName = Marshal.StringToCoTaskMemUni(pszFileName);
				try
				{
					var hr = lpVtbl->Load((IPersistFile*)Unsafe.AsPointer(ref this), _pszFileName, dwMode);
					Marshal.ThrowExceptionForHR(hr);
				}
				finally
				{
					Marshal.FreeCoTaskMem(_pszFileName);
				}
			}
		}

		/// <remarks>CLSID_ShellLink from ShlGuid.h</remarks>
		public unsafe class ShellLink : IDisposable
		{
			public static readonly Guid Guid = new("00021401-0000-0000-C000-000000000046");
			public static explicit operator IShellLinkW*(ShellLink link) => link.SLI;
			public static explicit operator IPersistFile*(ShellLink link) => link.PFI;

			private IShellLinkW* SLI;
			private IPersistFile* PFI;

			public ShellLink()
			{
				var hr = Ole32Imports.CoCreateInstance(Guid, IntPtr.Zero, Ole32Imports.CLSCTX.INPROC_SERVER, IShellLinkW.Guid, out var psl);
				Marshal.ThrowExceptionForHR(hr);

				var sli = (IShellLinkW*)psl;
				hr = sli->lpVtbl->QueryInterface(sli, in IPersistFile.Guid, out var ppf);
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
		}
	}
}
