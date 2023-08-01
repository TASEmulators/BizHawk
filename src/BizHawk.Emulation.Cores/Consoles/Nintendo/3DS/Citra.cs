using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

using BizHawk.BizInvoke;
using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.N3DS
{
	[PortedCore(CoreNames.Citra, "Citra Emulator Project", "nightly-1957", "https://citra-emu.org", singleInstance: true, isReleased: false)]
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

		public Rectangle TouchScreenRectangle { get; private set; }
		public bool TouchScreenRotated { get; private set; }
		public bool TouchScreenEnabled { get; private set; }

		[CoreConstructor(VSystemID.Raw.N3DS)]
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
			_supportsOpenGL43 = _openGLProvider.SupportsGLVersion(4, 3);
			if (!_supportsOpenGL43/* && _syncSettings.GraphicsApi == CitraSyncSettings.EGraphicsApi.OpenGL*/)
			{
				throw new("OpenGL 4.3 is required, but it is not supported on this machine");
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

			if (_supportsOpenGL43/* && _syncSettings.GraphicsApi == CitraSyncSettings.EGraphicsApi.OpenGL*/)
			{
				_citraVideoProvider = new CitraGLTextureProvider(_core, _context);
			}
			else
			{
				_citraVideoProvider = new(_core, _context);
			}

			_serviceProvider.Register<IVideoProvider>(_citraVideoProvider);

			var sysDataDir = Path.Combine(_userPath, "sysdata");
			if (!Directory.Exists(sysDataDir))
			{
				Directory.CreateDirectory(sysDataDir);
			}

			var aesKeys = lp.Comm.CoreFileProvider.GetFirmware(new("3DS", "aes_keys"));
			if (aesKeys is not null)
			{
				File.WriteAllBytes(Path.Combine(sysDataDir, "aes_keys.txt"), aesKeys);
			}

			var seeddb = lp.Comm.CoreFileProvider.GetFirmware(new("3DS", "seeddb"));
			if (seeddb is not null)
			{
				File.WriteAllBytes(Path.Combine(sysDataDir, "seeddb.bin"), seeddb);
			}

			var romPath = lp.Roms[0].RomPath;
			if (lp.Roms[0].Extension.ToLowerInvariant() == ".cia")
			{
				var message = new byte[1024];
				var res = _core.Citra_InstallCIA(_context, romPath, message, message.Length);
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

			// user could have other CIAs after the first ROM (e.g. DLCs, updates)
			// they need to installed at once in the case of recording
			// as the temp folder is cleaned for each session
			var dummyBuffer = new byte[1];
			for (var i = 1; i < lp.Roms.Count; i++)
			{
				// doesn't make sense if not a CIA
				if (lp.Roms[i].Extension.ToLowerInvariant() != ".cia")
				{
					Dispose();
					throw new("ROMs after the index 0 should be CIAs");
				}

				_core.Citra_InstallCIA(_context, lp.Roms[i].RomPath, dummyBuffer, dummyBuffer.Length);
			}

			var errorMessage = new byte[1024];
			if (!_core.Citra_LoadROM(_context, romPath, errorMessage, errorMessage.Length))
			{
				Dispose();
				throw new($"{Encoding.UTF8.GetString(errorMessage).TrimEnd()}");
			}

			InitMemoryDomains();
			// for some reason, if a savestate is created on frame 0, Citra will crash if another savestate is made after loading that state
			// advance one frame to avoid that issue
			_core.Citra_RunFrame(_context);
			OnVideoRefresh();
		}

		private IntPtr RequestGLContextCallback()
		{
			var context = _openGLProvider.RequestGLContext(4, 3, true, false);
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
