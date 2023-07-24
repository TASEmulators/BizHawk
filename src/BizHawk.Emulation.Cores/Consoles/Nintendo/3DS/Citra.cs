using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

using BizHawk.BizInvoke;
using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo._3DS
{
	[PortedCore(CoreNames.Citra, "Citra Emulator Project", "nightly-1953", "https://citra-emu.org", singleInstance: true, isReleased: false)]
	[ServiceNotApplicable(new[] { typeof(IDriveLight), typeof(IRegionable) })]
	public partial class Citra
	{
		private static readonly LibCitra _core;

		static Citra()
		{
			var resolver = new DynamicLibraryImportResolver(
				OSTailoredCode.IsUnixHost ? "libcitra-headless.so" : "citra-headless.dll", hasLimitedLifetime: false);
			_core = BizInvoker.GetInvoker<LibCitra>(resolver, CallingConventionAdapters.Native);
		}

		private static Citra CurrentCore;

		private readonly IOpenGLProvider _openGLProvider;
		private readonly bool _supportsOpenGL43;
		private readonly List<object> _glContexts = new();
		private readonly LibCitra.ConfigCallbackInterface _configCallbackInterface;
		private readonly LibCitra.GLCallbackInterface _glCallbackInterface;
		private readonly LibCitra.InputCallbackInterface _inputCallbackInterface;
		private readonly IntPtr _context;
		private readonly CitraVideoProvider _citraVideoProvider;

		[CoreConstructor(VSystemID.Raw._3DS)]
		public Citra(CoreLoadParameters<CitraSettings, CitraSyncSettings> lp)
		{
			if (lp.Roms.Exists(r => r.RomPath.Contains("|")))
			{
				throw new InvalidOperationException("3DS does not support compressed ROMs");
			}

			CurrentCore?.Dispose();
			CurrentCore = this;

			_serviceProvider = new(this);
			_settings = lp.Settings ?? new();
			_syncSettings = lp.SyncSettings ?? new();

			DeterministicEmulation = lp.DeterministicEmulationRequested;
			_userPath = lp.Comm.CoreFileProvider.GetUserPath(SystemId, temp: DeterministicEmulation) + Path.DirectorySeparatorChar;
			_userPath = _userPath.Replace('\\', '/'); // Citra doesn't like backslashes in the user folder, for whatever reason

			_configCallbackInterface.GetBoolean = GetBooleanSettingCallback;
			_configCallbackInterface.GetInteger = GetIntegerSettingCallback;
			_configCallbackInterface.GetFloat = GetFloatSettingCallback;
			_configCallbackInterface.GetString = GetStringSettingCallback;

			_openGLProvider = lp.Comm.OpenGLProvider;
			_supportsOpenGL43 = _openGLProvider.GLVersion >= 430;
			if (!_supportsOpenGL43 && _syncSettings.GraphicsApi == CitraSyncSettings.EGraphicsApi.OpenGL)
			{
				lp.Comm.Notify("OpenGL 4.3 is not supported on this machine, falling back to software renderer", null);
			}

			_glCallbackInterface.RequestGLContext = RequestGLContextCallback;
			_glCallbackInterface.ReleaseGLContext = ReleaseGLContextCallback;
			_glCallbackInterface.ActivateGLContext = ActivateGLContextCallback;
			_glCallbackInterface.GetGLProcAddress = GetGLProcAddressCallback;

			_inputCallbackInterface.GetButton = GetButtonCallback;
			_inputCallbackInterface.GetAxis = GetAxisCallback;
			_inputCallbackInterface.GetTouch = GetTouchCallback;
			_inputCallbackInterface.GetMotion = GetMotionCallback;

			_context = _core.Citra_CreateContext(ref _configCallbackInterface, ref _glCallbackInterface, ref _inputCallbackInterface);

			if (_supportsOpenGL43 && _syncSettings.GraphicsApi == CitraSyncSettings.EGraphicsApi.OpenGL)
			{
				_citraVideoProvider = new CitraGLTextureProvider(_core, _context);
			}
			else
			{
				_citraVideoProvider = new(_core, _context);
			}

			_serviceProvider.Register<IVideoProvider>(_citraVideoProvider);

			var boot9 = lp.Comm.CoreFileProvider.GetFirmware(new("3DS", "boot9"));
			if (boot9 is not null)
			{
				File.WriteAllBytes(Path.Combine(_userPath, "sysdata", "boot9.bin"), boot9);
			}

			var sector0x96 = lp.Comm.CoreFileProvider.GetFirmware(new("3DS", "sector0x96"));
			if (sector0x96 is not null)
			{
				File.WriteAllBytes(Path.Combine(_userPath, "sysdata", "sector0x96.bin"), sector0x96);
			}

			var seeddb = lp.Comm.CoreFileProvider.GetFirmware(new("3DS", "seeddb"));
			if (seeddb is not null)
			{
				File.WriteAllBytes(Path.Combine(_userPath, "sysdata", "seeddb.bin"), seeddb);
			}

			void InstallFirmCia(string firmName)
			{
				var firm = lp.Comm.CoreFileProvider.GetFirmware(new("3DS", firmName));
				if (firm is not null)
				{
					var firmCia = TempFileManager.GetTempFilename(firmName, ".cia", false);
					try
					{
						File.WriteAllBytes(firmCia, firm);
						var message = new byte[1024];
						_core.Citra_InstallCIA(_context, firmCia, true, message, message.Length);
					}
					finally
					{
						TempFileManager.RenameTempFilenameForDelete(firmCia);
					}
				}
			}

			InstallFirmCia("NATIVE_FIRM");
			InstallFirmCia("SAFE_MODE_FIRM");
			InstallFirmCia("N3DS_SAFE_MODE_FIRM");

			var romPath = lp.Roms[0].RomPath;
			if (lp.Roms[0].Extension.ToLowerInvariant() == ".cia")
			{
				var message = new byte[1024];
				var res = _core.Citra_InstallCIA(_context, romPath, true, message, message.Length);
				var outMsg = Encoding.UTF8.GetString(message).TrimEnd();
				if (res)
				{
					romPath = outMsg;
				}
				else
				{
					Dispose();
					throw new(outMsg);
				}
			}

			var errorMessage = new byte[1024];
			if (!_core.Citra_LoadROM(_context, romPath, errorMessage, errorMessage.Length))
			{
				Dispose();
				throw new($"{Encoding.UTF8.GetString(errorMessage).TrimEnd()}");
			}
		}

		private IntPtr RequestGLContextCallback()
		{
			var context = _openGLProvider.RequestGLContext(4, 3, false);
			_glContexts.Add(context);
			var handle = GCHandle.Alloc(context, GCHandleType.Weak);
			return GCHandle.ToIntPtr(handle);
		}

		private void ReleaseGLContextCallback(IntPtr context)
		{
			var handle = GCHandle.FromIntPtr(context);
			_openGLProvider.ReleaseGLContext(handle.Target);
			_glContexts.Remove(handle.Target);
			handle.Free();
		}

		private void ActivateGLContextCallback(IntPtr context)
		{
			var handle = GCHandle.FromIntPtr(context);
			_openGLProvider.ActivateGLContext(handle.Target);
		}

		private IntPtr GetGLProcAddressCallback(string proc)
			=> _openGLProvider.GetGLProcAddress(proc);
	}
}
