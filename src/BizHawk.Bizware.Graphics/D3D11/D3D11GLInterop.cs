using Silk.NET.Core.Contexts;
using Silk.NET.OpenGL;
using Silk.NET.WGL.Extensions.NV;

using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;

using BizHawk.Common.CollectionExtensions;
using BizHawk.Common.StringExtensions;

using static SDL2.SDL;

namespace BizHawk.Bizware.Graphics
{
	internal sealed class D3D11GLInterop : IDisposable
	{
		public static readonly bool IsAvailable;

		// we use glCopyImageSubData in order to copy an external gl texture to our wrapped gl texture
		// this is only guaranteed for GL 4.3 however, so we want to handle grabbing the ARB_copy_image or NV_copy_image extension variants
		private static IntPtr GetGLProcAddress(string proc)
		{
			var ret = SDL2OpenGLContext.GetGLProcAddress(proc);
			if (ret == IntPtr.Zero && proc == "glCopyImageSubData")
			{
				// note that both core and ARB_copy_image use the same proc name
				// however, NV_copy_image uses a different name
				ret = SDL2OpenGLContext.GetGLProcAddress("glCopyImageSubDataNV");
			}

			return ret;
		}

		private static readonly GL GL;
		private static readonly NVDXInterop NVDXInterop;

		private enum Vendor
		{
			Nvida,
			Amd,
			Intel,
			Unknown
		}

		private static readonly int[] _blacklistedIntelDeviceIds =
		[
			// Broadwell GPUs
			0x1602, 0x1606, 0x160A, 0x160B,
			0x160D, 0x160E, 0x1612, 0x1616,
			0x161A, 0x161B, 0x161D, 0x161E,
			0x1622, 0x1626, 0x162A, 0x162B,
			0x162D, 0x162E, 0x1632, 0x1636,
			0x163A, 0x163B, 0x163D, 0x163E,
			// Skylake GPUs
			0x1902, 0x1906, 0x190A, 0x190B,
			0x190E, 0x1912, 0x1913, 0x1915,
			0x1916, 0x1917, 0x191A, 0x191B,
			0x191D, 0x191E, 0x1921, 0x1923,
			0x1926, 0x1927, 0x192A, 0x192B,
			0x192D, 0x1932, 0x193A, 0x193B,
			0x193D
		];

		static D3D11GLInterop()
		{
			using (new SavedOpenGLContext())
			{
				try
				{
					// from Kronos:
					// "WGL function retrieval does require an active, current context. However, the use of WGL functions do not.
					// Therefore, you can destroy the context after querying all of the WGL extension functions."

					// from Microsoft:
					// "The extension function addresses are unique for each pixel format.
					// All rendering contexts of a given pixel format share the same extension function addresses."

					var (majorVersion, minorVersion) = OpenGLVersion.SupportsVersion(4, 3) ? (4, 3) : (2, 1);
					using var glContext = new SDL2OpenGLContext(majorVersion, minorVersion, true);

					GL = GL.GetApi(GetGLProcAddress);
					if (GL.CurrentVTable.Load("glCopyImageSubData") == IntPtr.Zero
						|| GL.CurrentVTable.Load("glGenTextures") == IntPtr.Zero
						|| GL.CurrentVTable.Load("glDeleteTextures") == IntPtr.Zero)
					{
						return;
					}

					// note: Silk.NET's WGL.IsExtensionPresent function seems to be bugged and just results in NREs...
					NVDXInterop = new(new LamdaNativeContext(SDL2OpenGLContext.GetGLProcAddress));
					if (NVDXInterop.CurrentVTable.Load("wglDXOpenDeviceNV") == IntPtr.Zero
						|| NVDXInterop.CurrentVTable.Load("wglDXCloseDeviceNV") == IntPtr.Zero
						|| NVDXInterop.CurrentVTable.Load("wglDXRegisterObjectNV") == IntPtr.Zero
						|| NVDXInterop.CurrentVTable.Load("wglDXUnregisterObjectNV") == IntPtr.Zero
						|| NVDXInterop.CurrentVTable.Load("wglDXLockObjectsNV") == IntPtr.Zero
						|| NVDXInterop.CurrentVTable.Load("wglDXUnlockObjectsNV") == IntPtr.Zero)
					{
						return;
					}

					var glVendor = GL.GetStringS(StringName.Vendor);
					var vendor = Vendor.Unknown;
					switch (glVendor)
					{
						case "NVIDIA Corporation":
							vendor = Vendor.Nvida;
							break;
						case "ATI Technologies Inc." or "Advanced Micro Devices, Inc.":
							vendor = Vendor.Amd;
							break;
						default:
						{
							if (glVendor.Contains("Intel", StringComparison.Ordinal))
							{
								vendor = Vendor.Intel;
							}

							break;
						}
					}

					// using these NVDXInterop functions shouldn't need a context active (see above Kronos comment)
					// however, some buggy drivers will end up failing if we don't have a context
					// explicitly make no context active to catch these buggy drivers
					SDL2OpenGLContext.MakeNoneCurrent();

					ID3D11Device device = null;
					var dxInteropDevice = IntPtr.Zero;
					try
					{
						D3D11.D3D11CreateDevice(
							adapter: null,
							DriverType.Hardware,
							DeviceCreationFlags.BgraSupport,
							null!,
							out device,
							out var context).CheckError();
						context.Dispose();

						// try to not use this extension if the D3D11 device is using different GPUs
						// some buggy drivers will end up crashing if these mismatch
						// presumingly hybrid GPU PCs just have an Intel card and an AMD or NVIDIA card
						// so just checking if vendors match should suffice
						using var dxgiDevice = device.QueryInterface<IDXGIDevice>();
						using var adapter = dxgiDevice.GetAdapter();
						var vendorId = adapter.Description.VendorId;
						switch (vendorId)
						{
							// match against vendor ids
							case 0x10DE when vendor == Vendor.Nvida:
							case 0x1002 when vendor == Vendor.Amd:
							case 0x8086 when vendor == Vendor.Intel:
								break;
							// for now, don't even try for unknown vendors
							default:
								return;
						}

						if (vendor == Vendor.Intel)
						{
							// avoid Broadwell and Skylake gpus, these have been reported crashing with gl interop
							// (specifically, Intel HD Graphics 5500 and Intel HD Graphics 530, presumingly all Broadwell and Skylake are affected, better safe than sorry)
							if (_blacklistedIntelDeviceIds.Contains(adapter.Description.DeviceId))
							{
								return;
							}
						}

						unsafe
						{
							dxInteropDevice = NVDXInterop.DxopenDevice((void*)device.NativePointer);
						}

						// TODO: test interop harder?
						IsAvailable = dxInteropDevice != IntPtr.Zero;
					}
					finally
					{
						if (dxInteropDevice != IntPtr.Zero)
						{
							NVDXInterop.DxcloseDevice(dxInteropDevice);
						}

						device?.Dispose();

						if (!IsAvailable)
						{
							GL?.Dispose();
							GL = null;
							NVDXInterop?.Dispose();
							NVDXInterop = null;
						}
					}
				}
				catch
				{
					// ignored
				}
			}
		}

		private readonly D3D11Resources _resources;
		private ID3D11Device Device => _resources.Device;

		private IntPtr _dxInteropDevice;
		private IntPtr _lastGLContext;
		private D3D11Texture2D _wrappedGLTexture;
		private IntPtr _wrappedGLTexInteropHandle;
		private uint _wrappedGLTexID;

		public D3D11GLInterop(D3D11Resources resources)
		{
			_resources = resources;
			try
			{
				OpenInteropDevice();
			}
			catch
			{
				Dispose();
				throw;
			}
		}

		public void OpenInteropDevice()
		{
			unsafe
			{
				_dxInteropDevice = NVDXInterop.DxopenDevice((void*)Device.NativePointer);
			}

			if (_dxInteropDevice == IntPtr.Zero)
			{
				throw new InvalidOperationException("Failed to open DX interop device");
			}
		}

		public D3D11Texture2D WrapGLTexture(int glTexId, int width, int height)
		{
			var glContext = SDL_GL_GetCurrentContext();
			if (glContext == IntPtr.Zero)
			{
				// can't do much without a context...
				return null;
			}

			if (_lastGLContext != glContext)
			{
				DestroyWrappedTexture(hasActiveContext: false);
				_lastGLContext = glContext;
			}

			if (_wrappedGLTexture == null || _wrappedGLTexture.Width != width || _wrappedGLTexture.Height != height)
			{
				DestroyWrappedTexture(hasActiveContext: true);
				CreateWrappedTexture(width, height);
			}

			unsafe
			{
				var wrappedGLTexInteropHandle = _wrappedGLTexInteropHandle;
				NVDXInterop.DxlockObjects(hDevice: _dxInteropDevice, 1, &wrappedGLTexInteropHandle);

				GL.CopyImageSubData((uint)glTexId, CopyImageSubDataTarget.Texture2D, 0, 0, 0, 0,
					_wrappedGLTexID, CopyImageSubDataTarget.Texture2D, 0, 0, 0, 0, (uint)width, (uint)height, 1);

				NVDXInterop.DxunlockObjects(hDevice: _dxInteropDevice, 1, &wrappedGLTexInteropHandle);
			}

			return _wrappedGLTexture;
		}

		private void CreateWrappedTexture(int width, int height)
		{
			_wrappedGLTexID = GL.GenTexture();
			_wrappedGLTexture = new(_resources, BindFlags.ShaderResource, ResourceUsage.Default, CpuAccessFlags.None, width, height, wrapped: true);
			unsafe
			{
				_wrappedGLTexInteropHandle = NVDXInterop.DxregisterObject(
					hDevice: _dxInteropDevice,
					dxObject: (void*)_wrappedGLTexture.Texture.NativePointer,
					name: _wrappedGLTexID,
					type: (NV)GLEnum.Texture2D,
					access: NV.AccessWriteDiscardNV);
			}
		}

		private void DestroyWrappedTexture(bool hasActiveContext = false)
		{
			if (_wrappedGLTexInteropHandle != IntPtr.Zero)
			{
				NVDXInterop.DxunregisterObject(_dxInteropDevice, _wrappedGLTexInteropHandle);
				_wrappedGLTexInteropHandle = IntPtr.Zero;
			}

			// this gl tex id is owned by the external context, which might be unavailable
			// if it's unavailable, we assume that context was just destroyed
			// therefore, assume the texture was already destroy in that case
			if (hasActiveContext && _wrappedGLTexID != 0)
			{
				GL.DeleteTexture(_wrappedGLTexID);
			}

			_wrappedGLTexID = 0;

			_wrappedGLTexture?.Dispose();
			_wrappedGLTexture = null;
		}

		public void Dispose()
		{
			DestroyWrappedTexture(hasActiveContext: false);

			if (_dxInteropDevice != IntPtr.Zero)
			{
				NVDXInterop.DxcloseDevice(_dxInteropDevice);
				_dxInteropDevice = IntPtr.Zero;
			}

			_lastGLContext = IntPtr.Zero;
		}
	}
}
