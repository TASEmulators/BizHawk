using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;

using BizHawk.Bizware.BizwareGL;
using BizHawk.Common;
using BizHawk.Common.StringExtensions;

using SharpDX;
using SharpDX.Direct3D9;
using SharpDX.Mathematics.Interop;

using static SDL2.SDL;

// todo - do a better job selecting shader model? base on caps somehow? try several and catch compilation exceptions (yuck, exceptions)
namespace BizHawk.Bizware.Graphics
{
	/// <summary>
	/// Direct3D9 implementation of the BizwareGL.IGL interface
	/// </summary>
	public sealed class IGL_D3D9 : IGL
	{
		public EDispMethod DispMethodEnum => EDispMethod.D3D9;

		private const int D3DERR_DEVICELOST = unchecked((int)0x88760868);
		private const int D3DERR_DEVICENOTRESET = unchecked((int)0x88760869);

		private Device _device;

		private IntPtr _offscreenSdl2Window;
		private IntPtr OffscreenNativeWindow;

		// rendering state
		private Pipeline _currPipeline;
		private D3D9Control _currentControl;

		// misc state
		private CacheBlendState _rsBlendNoneVerbatim, _rsBlendNoneOpaque, _rsBlendNormal;
		private readonly HashSet<RenderTarget> _renderTargets = new();

		public string API => "D3D9";

		static IGL_D3D9()
		{
			if (SDL_Init(SDL_INIT_VIDEO) != 0)
			{
				throw new($"Failed to init SDL video, SDL error: {SDL_GetError()}");
			}
		}

		public IGL_D3D9()
		{
			if (OSTailoredCode.IsUnixHost)
			{
				throw new NotSupportedException("D3D9 is Windows only");
			}

			// make an 'offscreen context' so we can at least do things without having to create a window
			_offscreenSdl2Window = SDL_CreateWindow(null, 0, 0, 1, 1, SDL_WindowFlags.SDL_WINDOW_HIDDEN);
			if (_offscreenSdl2Window == IntPtr.Zero)
			{
				throw new($"Failed to create offscreen SDL window, SDL error: {SDL_GetError()}");
			}

			// get the native window handle
			var wminfo = default(SDL_SysWMinfo);
			SDL_GetVersion(out wminfo.version);
			SDL_GetWindowWMInfo(_offscreenSdl2Window, ref wminfo);
			if (wminfo.subsystem != SDL_SYSWM_TYPE.SDL_SYSWM_WINDOWS)
			{
				throw new($"SDL_SysWMinfo did not report SDL_SYSWM_WINDOWS? Something went wrong... SDL error: {SDL_GetError()}");
			}

			OffscreenNativeWindow = wminfo.info.win.window;

			CreateDevice();
			CreateRenderStates();
		}

		public void AlternateVsyncPass(int pass)
		{
			while (true)
			{
				var status = _device.GetRasterStatus(0);
				if (status.InVBlank && pass == 0) return; // wait for vblank to begin
				if (!status.InVBlank && pass == 1) return; // wait for vblank to end
				// STOP! think you can use System.Threading.SpinWait? No, it's way too slow.
				// (on my system, the vblank is something like 24 of 1074 scanlines @ 60hz ~= 0.35ms which is an awfully small window to nail)
			}
		}

		private void CreateDevice()
		{
			// this object is only used for creating a device, it's not needed afterwards
			using var d3d9 = new Direct3D();

			var pp = MakePresentParameters();

			var flags = (d3d9.GetDeviceCaps(0, DeviceType.Hardware).DeviceCaps & DeviceCaps.HWTransformAndLight) != 0
				? CreateFlags.HardwareVertexProcessing
				: CreateFlags.SoftwareVertexProcessing;

			flags |= CreateFlags.FpuPreserve;
			_device = new(d3d9, 0, DeviceType.Hardware, pp.DeviceWindowHandle, flags, pp);
		}

		private void DestroyDevice()
		{
			if (_device != null)
			{
				_device.Dispose();
				_device = null;
			}
		}

		private PresentParameters MakePresentParameters()
		{
			return new()
			{
				BackBufferCount = 1,
				SwapEffect = SwapEffect.Discard,
				DeviceWindowHandle = OffscreenNativeWindow,
				Windowed = true,
				PresentationInterval = PresentInterval.Immediate,
				EnableAutoDepthStencil = false
			};
		}

		private void ResetDevice(D3D9Control control)
		{
			SuspendRenderTargets();
			FreeControlSwapChain(control);

			while (true)
			{
				var result = _device.TestCooperativeLevel();
				if (result.Success)
				{
					break;
				}

				if (result.Code == D3DERR_DEVICENOTRESET)
				{
					try
					{
						var pp = MakePresentParameters();
						_device.Reset(pp);
						break;
					}
					catch
					{
						// ignored
					}
				}

				Thread.Sleep(100);
			}

			RefreshControlSwapChain(control);
			ResumeRenderTargets();
		}

		public void Dispose()
		{
			DestroyDevice();

			if (_offscreenSdl2Window != IntPtr.Zero)
			{
				SDL_DestroyWindow(_offscreenSdl2Window);
				_offscreenSdl2Window = OffscreenNativeWindow = IntPtr.Zero;
			}
		}

		public void ClearColor(Color color)
			=> _device.Clear(ClearFlags.Target, color.ToSharpDXColor(), 0.0f, 0);

		public IBlendState CreateBlendState(
			BlendingFactorSrc colorSource,
			BlendEquationMode colorEquation,
			BlendingFactorDest colorDest,
			BlendingFactorSrc alphaSource,
			BlendEquationMode alphaEquation,
			BlendingFactorDest alphaDest)
			=> new CacheBlendState(true, colorSource, colorEquation, colorDest, alphaSource, alphaEquation, alphaDest);

		public void FreeTexture(Texture2d tex)
		{
			var tw = (TextureWrapper)tex.Opaque;
			tw.Texture.Dispose();
		}

		private class ShaderWrapper // Disposable fields cleaned up by Internal_FreeShader
		{
			public ShaderBytecode Bytecode;
			public VertexShader VS;
			public PixelShader PS;
			public Shader IGLShader;
		}

		/// <exception cref="InvalidOperationException"><paramref name="required"/> is <see langword="true"/> and compilation error occurred</exception>
		public Shader CreateFragmentShader(string source, string entry, bool required)
		{
			try
			{
				var sw = new ShaderWrapper();

				// ShaderFlags.EnableBackwardsCompatibility - used this once upon a time (please leave a note about why)
				var result = ShaderBytecode.Compile(
					shaderSource: source,
					entryPoint: entry,
					profile: "ps_3_0",
					shaderFlags: ShaderFlags.UseLegacyD3DX9_31Dll);

				sw.PS = new(_device, result);
				sw.Bytecode = result;

				var s = new Shader(this, sw, true);
				sw.IGLShader = s;

				return s;
			}
			catch (Exception ex)
			{
				if (required)
				{
					throw;
				}

				return new(this, null, false) { Errors = ex.ToString() };
			}
		}

		/// <exception cref="InvalidOperationException"><paramref name="required"/> is <see langword="true"/> and compilation error occurred</exception>
		public Shader CreateVertexShader(string source, string entry, bool required)
		{
			try
			{
				var sw = new ShaderWrapper();

				var result = ShaderBytecode.Compile(
					shaderSource: source,
					entryPoint: entry,
					profile: "vs_3_0",
					shaderFlags: ShaderFlags.UseLegacyD3DX9_31Dll);

				sw.VS = new(_device, result);
				sw.Bytecode = result;

				var s = new Shader(this, sw, true);
				sw.IGLShader = s;

				return s;
			}
			catch (Exception ex)
			{
				if (required)
				{
					throw;
				}

				return new(this, null, false) { Errors = ex.ToString() };
			}
		}

		private static BlendOperation ConvertBlendOp(BlendEquationMode glMode)
			=> glMode switch
			{
				BlendEquationMode.FuncAdd => BlendOperation.Add,
				BlendEquationMode.FuncSubtract => BlendOperation.Subtract,
				BlendEquationMode.Max => BlendOperation.Maximum,
				BlendEquationMode.Min => BlendOperation.Minimum,
				BlendEquationMode.FuncReverseSubtract => BlendOperation.ReverseSubtract,
				_ => throw new InvalidOperationException()
			};

		private static Blend ConvertBlendArg(BlendingFactorDest glMode)
			=> ConvertBlendArg((BlendingFactorSrc)glMode);

		private static Blend ConvertBlendArg(BlendingFactorSrc glMode)
			=> glMode switch
			{
				BlendingFactorSrc.Zero => Blend.Zero,
				BlendingFactorSrc.One => Blend.One,
				BlendingFactorSrc.SrcColor => Blend.SourceColor,
				BlendingFactorSrc.OneMinusSrcColor => Blend.InverseSourceColor,
				BlendingFactorSrc.SrcAlpha => Blend.SourceAlpha,
				BlendingFactorSrc.OneMinusSrcAlpha => Blend.InverseSourceAlpha,
				BlendingFactorSrc.DstAlpha => Blend.DestinationAlpha,
				BlendingFactorSrc.OneMinusDstAlpha => Blend.InverseDestinationAlpha,
				BlendingFactorSrc.DstColor => Blend.DestinationColor,
				BlendingFactorSrc.OneMinusDstColor => Blend.InverseDestinationColor,
				BlendingFactorSrc.SrcAlphaSaturate => Blend.SourceAlphaSaturated,
				BlendingFactorSrc.ConstantColor => Blend.BlendFactor,
				BlendingFactorSrc.OneMinusConstantColor => Blend.InverseBlendFactor,
				BlendingFactorSrc.ConstantAlpha => throw new NotSupportedException(),
				BlendingFactorSrc.OneMinusConstantAlpha => throw new NotSupportedException(),
				BlendingFactorSrc.Src1Alpha => throw new NotSupportedException(),
				BlendingFactorSrc.Src1Color => throw new NotSupportedException(),
				BlendingFactorSrc.OneMinusSrc1Color => throw new NotSupportedException(),
				BlendingFactorSrc.OneMinusSrc1Alpha => throw new NotSupportedException(),
				_ => throw new InvalidOperationException()
			};

		public void SetBlendState(IBlendState rsBlend)
		{
			var myBs = (CacheBlendState)rsBlend;
			if (myBs.Enabled)
			{
				_device.SetRenderState(RenderState.AlphaBlendEnable, true);
				_device.SetRenderState(RenderState.SeparateAlphaBlendEnable, true);

				_device.SetRenderState(RenderState.BlendOperation, ConvertBlendOp(myBs.colorEquation));
				_device.SetRenderState(RenderState.SourceBlend, ConvertBlendArg(myBs.colorSource));
				_device.SetRenderState(RenderState.DestinationBlend, ConvertBlendArg(myBs.colorDest));

				_device.SetRenderState(RenderState.BlendOperationAlpha, ConvertBlendOp(myBs.alphaEquation));
				_device.SetRenderState(RenderState.SourceBlendAlpha, ConvertBlendArg(myBs.alphaSource));
				_device.SetRenderState(RenderState.DestinationBlendAlpha, ConvertBlendArg(myBs.alphaDest));
			}
			else
			{
				_device.SetRenderState(RenderState.AlphaBlendEnable, false);
			}

			if (rsBlend == _rsBlendNoneOpaque)
			{
				// make sure constant color is set correctly
				_device.SetRenderState(RenderState.BlendFactor, -1); // white
			}
		}

		private void CreateRenderStates()
		{
			_rsBlendNoneVerbatim = new(
				false,
				BlendingFactorSrc.One, BlendEquationMode.FuncAdd, BlendingFactorDest.Zero,
				BlendingFactorSrc.One, BlendEquationMode.FuncAdd, BlendingFactorDest.Zero);

			_rsBlendNoneOpaque = new(
				false,
				BlendingFactorSrc.One, BlendEquationMode.FuncAdd, BlendingFactorDest.Zero,
				BlendingFactorSrc.ConstantAlpha, BlendEquationMode.FuncAdd, BlendingFactorDest.Zero);

			_rsBlendNormal = new(
				true,
				BlendingFactorSrc.SrcAlpha, BlendEquationMode.FuncAdd, BlendingFactorDest.OneMinusSrcAlpha,
				BlendingFactorSrc.One, BlendEquationMode.FuncAdd, BlendingFactorDest.Zero);
		}

		public IBlendState BlendNoneCopy => _rsBlendNoneVerbatim;
		public IBlendState BlendNoneOpaque => _rsBlendNoneOpaque;
		public IBlendState BlendNormal => _rsBlendNormal;

		/// <exception cref="InvalidOperationException">
		/// <paramref name="required"/> is <see langword="true"/> and either <paramref name="vertexShader"/> or <paramref name="fragmentShader"/> is unavailable (their <see cref="Shader.Available"/> property is <see langword="false"/>), or
		/// one of <paramref name="vertexLayout"/>'s items has an unsupported value in <see cref="VertexLayout.LayoutItem.AttribType"/>, <see cref="VertexLayout.LayoutItem.Components"/>, or <see cref="VertexLayout.LayoutItem.Usage"/>
		/// </exception>
		public Pipeline CreatePipeline(VertexLayout vertexLayout, Shader vertexShader, Shader fragmentShader, bool required, string memo)
		{
			if (!vertexShader.Available || !fragmentShader.Available)
			{
				var errors = $"Vertex Shader:\r\n {vertexShader.Errors} \r\n-------\r\nFragment Shader:\r\n{fragmentShader.Errors}";
				if (required)
				{
					throw new InvalidOperationException($"Couldn't build required GL pipeline:\r\n{errors}");
				}

				return new(this, null, false, null, null, null) { Errors = errors };
			}

			var ves = new VertexElement[vertexLayout.Items.Count + 1];
			var stride = 0;
			foreach (var (i, item) in vertexLayout.Items)
			{
				DeclarationType declType;
				// ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
				switch (item.AttribType)
				{
					case VertexAttribPointerType.Float:
						declType = item.Components switch
						{
							1 => DeclarationType.Float1,
							2 => DeclarationType.Float2,
							3 => DeclarationType.Float3,
							4 => DeclarationType.Float4,
							_ => throw new InvalidOperationException()
						};
						stride += 4 * item.Components;
						break;
					default:
						throw new NotSupportedException();
				}

				DeclarationUsage usage;
				byte usageIndex = 0;
				// ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
				switch (item.Usage)
				{
					case AttribUsage.Position:
						usage = DeclarationUsage.Position;
						break;
					case AttribUsage.Texcoord0:
						usage = DeclarationUsage.TextureCoordinate;
						break;
					case AttribUsage.Texcoord1:
						usage = DeclarationUsage.TextureCoordinate;
						usageIndex = 1;
						break;
					case AttribUsage.Color0:
						usage = DeclarationUsage.Color;
						break;
					default:
						throw new NotSupportedException();
				}

				ves[i] = new(0, (short)item.Offset, declType, DeclarationMethod.Default, usage, usageIndex);
			}

			// must be placed at the end
			ves[vertexLayout.Items.Count] = VertexElement.VertexDeclarationEnd;

			var pw = new PipelineWrapper
			{
				VertexDeclaration = new(_device, ves),
				VertexShader = (ShaderWrapper)vertexShader.Opaque,
				FragmentShader = (ShaderWrapper)fragmentShader.Opaque,
				VertexStride = stride
			};

			// scan uniforms from reflection
			var uniforms = new List<UniformInfo>();
			var vsct = pw.VertexShader.Bytecode.ConstantTable;
			var psct = pw.FragmentShader.Bytecode.ConstantTable;
			foreach (var ct in new[] { vsct, psct })
			{
				var todo = new Queue<(string, EffectHandle)>();
				var n = ct.Description.Constants;
				for (var i = 0; i < n; i++)
				{
					var handle = ct.GetConstant(null, i);
					todo.Enqueue((string.Empty, handle));
				}

				while (todo.Count != 0)
				{
					var (prefix, handle) = todo.Dequeue();
					var descr = ct.GetConstantDescription(handle);

					// Console.WriteLine($"D3D UNIFORM: {descr.Name}");

					if (descr.StructMembers != 0)
					{
						var newPrefix = $"{prefix}{descr.Name}.";
						for (var j = 0; j < descr.StructMembers; j++)
						{
							var subHandle = ct.GetConstant(handle, j);
							todo.Enqueue((newPrefix, subHandle));
						}

						continue;
					}

					var ui = new UniformInfo();
					var uw = new UniformWrapper();

					ui.Opaque = uw;
					var name = prefix + descr.Name;

					// uniforms done through the entry point signature have $ in their names which isn't helpful, so get rid of that
					name = name.RemovePrefix('$');

					ui.Name = name;
					uw.EffectHandle = handle;
					uw.CT = ct;

					if (descr.Type == ParameterType.Sampler2D)
					{
						ui.IsSampler = true;
						ui.SamplerIndex = descr.RegisterIndex;
					}

					uniforms.Add(ui);
				}
			}

			return new(this, pw, true, vertexLayout, uniforms, memo);
		}

		public void FreePipeline(Pipeline pipeline)
		{
			// unavailable pipelines will have no opaque
			if (pipeline.Opaque is not PipelineWrapper pw)
			{
				return;
			}

			pw.VertexDeclaration.Dispose();
			pw.FragmentShader.IGLShader.Release();
			pw.VertexShader.IGLShader.Release();
		}

		public void Internal_FreeShader(Shader shader)
		{
			var sw = (ShaderWrapper)shader.Opaque;
			sw.Bytecode.Dispose();
			sw.PS?.Dispose();
			sw.VS?.Dispose();
		}

		private class UniformWrapper
		{
			public EffectHandle EffectHandle;
			public ConstantTable CT;
		}

		private class PipelineWrapper // Disposable fields cleaned up by FreePipeline
		{
			public VertexDeclaration VertexDeclaration;
			public ShaderWrapper VertexShader, FragmentShader;
			public int VertexStride;
		}

		private class TextureWrapper
		{
			public Texture Texture;
			public TextureAddress WrapClamp = TextureAddress.Clamp;
			public TextureFilter MinFilter = TextureFilter.Point, MagFilter = TextureFilter.Point;
		}

		public VertexLayout CreateVertexLayout()
			=> new(this, null);

		public void Internal_FreeVertexLayout(VertexLayout layout)
		{
		}

		public void BindPipeline(Pipeline pipeline)
		{
			_currPipeline = pipeline;

			if (pipeline == null)
			{
				// unbind? i don't know
				return;
			}

			var pw = (PipelineWrapper)pipeline.Opaque;
			_device.PixelShader = pw.FragmentShader.PS;
			_device.VertexShader = pw.VertexShader.VS;
			_device.VertexDeclaration = pw.VertexDeclaration;
		}

		public void SetPipelineUniform(PipelineUniform uniform, bool value)
		{
			foreach (var ui in uniform.UniformInfos)
			{
				var uw = (UniformWrapper)ui.Opaque;
				uw.CT.SetValue(_device, uw.EffectHandle, value);
			}
		}

		public void SetPipelineUniformMatrix(PipelineUniform uniform, Matrix4x4 mat, bool transpose)
			=> SetPipelineUniformMatrix(uniform, ref mat, transpose);

		public void SetPipelineUniformMatrix(PipelineUniform uniform, ref Matrix4x4 mat, bool transpose)
		{
			foreach (var ui in uniform.UniformInfos)
			{
				var uw = (UniformWrapper)ui.Opaque;
				uw.CT.SetValue(_device, uw.EffectHandle, mat.ToSharpDXMatrix(!transpose));
			}
		}

		public void SetPipelineUniform(PipelineUniform uniform, Vector4 value)
		{
			foreach (var ui in uniform.UniformInfos)
			{
				var uw = (UniformWrapper)ui.Opaque;
				uw.CT.SetValue(_device, uw.EffectHandle, value.ToSharpDXVector4());
			}
		}

		public void SetPipelineUniform(PipelineUniform uniform, Vector2 value)
		{
			foreach (var ui in uniform.UniformInfos)
			{
				var uw = (UniformWrapper)ui.Opaque;
				uw.CT.SetValue(_device, uw.EffectHandle, value.ToSharpDXVector2());
			}
		}

		public void SetPipelineUniform(PipelineUniform uniform, float value)
		{
			foreach (var ui in uniform.UniformInfos)
			{
				var uw = (UniformWrapper)ui.Opaque;
				uw.CT.SetValue(_device, uw.EffectHandle, value);
			}
		}

		public void SetPipelineUniform(PipelineUniform uniform, Vector4[] values)
		{
			var v = Array.ConvertAll(values, v => v.ToSharpDXVector4());
			foreach (var ui in uniform.UniformInfos)
			{
				var uw = (UniformWrapper)ui.Opaque;
				uw.CT.SetValue(_device, uw.EffectHandle, v);
			}
		}

		public void SetPipelineUniformSampler(PipelineUniform uniform, Texture2d tex)
		{
			if (uniform.Owner == null)
			{
				return; // uniform was optimized out
			}

			var tw = (TextureWrapper)tex.Opaque;
			foreach (var ui in uniform.UniformInfos)
			{
				if (!ui.IsSampler)
				{
					throw new InvalidOperationException("Uniform was not a texture/sampler");
				}

				_device.SetTexture(ui.SamplerIndex, tw.Texture);

				_device.SetSamplerState(ui.SamplerIndex, SamplerState.AddressU, (int)tw.WrapClamp);
				_device.SetSamplerState(ui.SamplerIndex, SamplerState.AddressV, (int)tw.WrapClamp);
				_device.SetSamplerState(ui.SamplerIndex, SamplerState.MinFilter, (int)tw.MinFilter);
				_device.SetSamplerState(ui.SamplerIndex, SamplerState.MagFilter, (int)tw.MagFilter);
			}
		}

		public void SetTextureWrapMode(Texture2d tex, bool clamp)
		{
			var tw = (TextureWrapper)tex.Opaque;
			tw.WrapClamp = clamp ? TextureAddress.Clamp : TextureAddress.Wrap;
		}

		public void SetMinFilter(Texture2d texture, TextureMinFilter minFilter)
			=> ((TextureWrapper)texture.Opaque).MinFilter = minFilter == TextureMinFilter.Linear
				? TextureFilter.Linear
				: TextureFilter.Point;

		public void SetMagFilter(Texture2d texture, TextureMagFilter magFilter)
			=> ((TextureWrapper)texture.Opaque).MagFilter = magFilter == TextureMagFilter.Linear
				? TextureFilter.Linear
				: TextureFilter.Point;

		public Texture2d LoadTexture(Bitmap bitmap)
		{
			using var bmp = new BitmapBuffer(bitmap, new());
			return LoadTexture(bmp);
		}

		public Texture2d LoadTexture(Stream stream)
		{
			using var bmp = new BitmapBuffer(stream, new());
			return LoadTexture(bmp);
		}

		public Texture2d CreateTexture(int width, int height)
		{
			var tex = new Texture(_device, width, height, 1, Usage.None, Format.A8R8G8B8, Pool.Managed);
			var tw = new TextureWrapper { Texture = tex };
			return new(this, tw, width, height);	
		}

		public Texture2d WrapGLTexture2d(IntPtr glTexId, int width, int height)
		{
			// only used for OpenGL
			return null;
		}

		/// <exception cref="InvalidOperationException">GDI+ call returned unexpected data</exception>
		public unsafe void LoadTextureData(Texture2d tex, BitmapBuffer bmp)
		{
			var tw = (TextureWrapper)tex.Opaque;
			var bmpData = bmp.LockBits();

			try
			{
				var dr = tw.Texture.LockRectangle(0, LockFlags.None);

				// TODO - do we need to handle odd sizes, weird pitches here?
				if (bmp.Width * 4 != bmpData.Stride || bmpData.Stride != dr.Pitch)
				{
					throw new InvalidOperationException();
				}

				var srcSpan = new ReadOnlySpan<byte>(bmpData.Scan0.ToPointer(), bmpData.Stride * bmp.Height);
				var dstSpan = new Span<byte>(dr.DataPointer.ToPointer(), dr.Pitch * bmp.Height);
				srcSpan.CopyTo(dstSpan);
			}
			finally
			{
				tw.Texture.UnlockRectangle(0);
				bmp.UnlockBits(bmpData);
			}
		}

		public Texture2d LoadTexture(BitmapBuffer bmp)
		{
			var ret = CreateTexture(bmp.Width, bmp.Height);
			LoadTextureData(ret, bmp);
			return ret;
		}

		/// <exception cref="InvalidOperationException">Vortice call returned unexpected data</exception>
		public BitmapBuffer ResolveTexture2d(Texture2d tex)
		{
			// TODO - lazy create and cache resolving target in RT
			using var target = new Texture(_device, tex.IntWidth, tex.IntHeight, 1, Usage.None, Format.A8R8G8B8, Pool.SystemMemory);
			var tw = (TextureWrapper)tex.Opaque;

			_device.GetRenderTargetData(tw.Texture.GetSurfaceLevel(0), target.GetSurfaceLevel(0));

			try
			{
				var dr = target.LockRectangle(0, LockFlags.ReadOnly);

				if (dr.Pitch != tex.IntWidth * 4)
				{
					throw new InvalidOperationException();
				}
				
				var pixels = new int[tex.IntWidth * tex.IntHeight];
				Marshal.Copy(dr.DataPointer, pixels, 0, tex.IntWidth * tex.IntHeight);
				return new(tex.IntWidth, tex.IntHeight, pixels);
			}
			finally
			{
				target.UnlockRectangle(0);
			}
		}

		public Texture2d LoadTexture(string path)
		{
			using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
			return LoadTexture(fs);
		}

		public Matrix4x4 CreateGuiProjectionMatrix(int w, int h)
		{
			return CreateGuiProjectionMatrix(new(w, h));
		}

		public Matrix4x4 CreateGuiViewMatrix(int w, int h, bool autoFlip)
		{
			return CreateGuiViewMatrix(new(w, h), autoFlip);
		}

		public Matrix4x4 CreateGuiProjectionMatrix(Size dims)
		{
			var ret = Matrix4x4.Identity;
			ret.M11 = 2.0f / dims.Width;
			ret.M22 = 2.0f / dims.Height;
			return ret;
		}

		public Matrix4x4 CreateGuiViewMatrix(Size dims, bool autoFlip)
		{
			var ret = Matrix4x4.Identity;
			ret.M22 = -1.0f;
			ret.M41 = -dims.Width * 0.5f - 0.5f;
			ret.M42 = dims.Height * 0.5f + 0.5f;

			// auto-flipping isn't needed on D3D
			return ret;
		}

		public void SetViewport(int x, int y, int width, int height)
		{
			_device.Viewport = new() { X = x, Y = y, Width = width, Height = height, MinDepth = 0, MaxDepth = 1 };
			_device.ScissorRect = new(x, y, x + width, y + height);
		}

		public void SetViewport(int width, int height)
		{
			SetViewport(0, 0, width, height);
		}

		public void SetViewport(Size size)
		{
			SetViewport(size.Width, size.Height);
		}

		internal void BeginControl(D3D9Control control)
		{
			_currentControl = control;

			// this dispose isn't strictly needed but it seems benign
			using var surface = control.SwapChain.GetBackBuffer(0);
			_device.SetRenderTarget(0, surface);
		}

		/// <exception cref="InvalidOperationException"><paramref name="control"/> does not match control passed to <see cref="BeginControl"/></exception>
		internal void EndControl(D3D9Control control)
		{
			if (control != _currentControl)
			{
				throw new InvalidOperationException($"{nameof(control)} does not match control passed to {nameof(BeginControl)}");
			}

			using var surface = control.SwapChain.GetBackBuffer(0);
			_device.SetRenderTarget(0, surface);

			_currentControl = null;
		}

		internal void SwapControl(D3D9Control control)
		{
			EndControl(control);

			try
			{
				control.SwapChain.Present(Present.None);
			}
			catch (SharpDXException ex)
			{
				if (ex.ResultCode.Code == D3DERR_DEVICELOST)
				{
					ResetDevice(control);
				}
			}
		}

		public void FreeRenderTarget(RenderTarget rt)
		{
			var tw = (TextureWrapper)rt.Texture2d.Opaque;
			tw.Texture.Dispose();
			tw.Texture = null;
			_renderTargets.Remove(rt);
		}

		public RenderTarget CreateRenderTarget(int w, int h)
		{
			var tw = new TextureWrapper { Texture = CreateRenderTargetTexture(w, h) };
			var tex = new Texture2d(this, tw, w, h);
			var rt = new RenderTarget(this, tw, tex);
			_renderTargets.Add(rt);
			return rt;
		}

		private Texture CreateRenderTargetTexture(int w, int h)
		{
			return new(_device, w, h, 1, Usage.RenderTarget, Format.A8R8G8B8, Pool.Default);
		}

		private void SuspendRenderTargets()
		{
			foreach (var tw in _renderTargets.Select(rt => (TextureWrapper)rt.Opaque))
			{
				tw.Texture.Dispose();
				tw.Texture = null;
			}
		}

		private void ResumeRenderTargets()
		{
			foreach (var rt in _renderTargets)
			{
				var tw = (TextureWrapper)rt.Opaque;
				tw.Texture = CreateRenderTargetTexture(rt.Texture2d.IntWidth, rt.Texture2d.IntHeight);
			}
		}

		public void BindRenderTarget(RenderTarget rt)
		{
			if (rt == null)
			{
				// this dispose is needed for correct device resets, I have no idea why
				// don't try caching it either
				using var surface = _currentControl.SwapChain.GetBackBuffer(0);
				_device.SetRenderTarget(0, surface);
				_device.DepthStencilSurface = null;
				return;
			}

			// dispose doesn't seem necessary for reset here...
			var tw = (TextureWrapper)rt.Opaque;
			_device.SetRenderTarget(0, tw.Texture.GetSurfaceLevel(0));
			_device.DepthStencilSurface = null;
		}

		internal static void FreeControlSwapChain(D3D9Control control)
		{
			control.SwapChain?.Dispose();
			control.SwapChain = null;
		}

		internal void RefreshControlSwapChain(D3D9Control control)
		{
			FreeControlSwapChain(control);

			var pp = new PresentParameters
			{
				BackBufferWidth = Math.Max(1, control.ClientSize.Width),
				BackBufferHeight = Math.Max(1, control.ClientSize.Height),
				BackBufferCount = 1,
				BackBufferFormat = Format.X8R8G8B8,
				SwapEffect = SwapEffect.Discard,
				DeviceWindowHandle = control.Handle,
				Windowed = true,
				PresentationInterval = control.Vsync ? PresentInterval.One : PresentInterval.Immediate
			};

			control.SwapChain = new(_device, pp);
		}

		public IGraphicsControl Internal_CreateGraphicsControl()
		{
			var ret = new D3D9Control(this);
			ret.CreateControl();
			return ret;
		}

		private delegate void DrawPrimitiveUPDelegate(Device device, PrimitiveType primitiveType, int primitiveCount, IntPtr vertexStreamZeroDataRef, int vertexStreamZeroStride);

		private static readonly Lazy<DrawPrimitiveUPDelegate> _drawPrimitiveUP = new(() =>
		{
			var mi = typeof(Device).GetMethod("DrawPrimitiveUP", BindingFlags.Instance | BindingFlags.NonPublic);
			return (DrawPrimitiveUPDelegate)Delegate.CreateDelegate(typeof(DrawPrimitiveUPDelegate), mi!);
		});

		private void DrawPrimitiveUP(PrimitiveType primitiveType, int primitiveCount, IntPtr vertexStreamZeroDataRef, int vertexStreamZeroStride)
			=> _drawPrimitiveUP.Value(_device, primitiveType, primitiveCount, vertexStreamZeroDataRef, vertexStreamZeroStride);

		public void Draw(IntPtr data, int count)
		{
			var pw = (PipelineWrapper)_currPipeline.Opaque;

			// this is stupid, sharpdx only public exposes DrawUserPrimatives
			// why is this bad? it takes in an array of T
			// and uses the size of T to determine stride
			// since stride for us is just completely variable, this is no good
			// DrawPrimitiveUP is internal so we have to use this hack to use it directly

			DrawPrimitiveUP(PrimitiveType.TriangleStrip, count - 2, data, pw.VertexStride);
		}

		public void BeginScene()
		{
			_device.BeginScene();
			_device.SetRenderState(RenderState.CullMode, Cull.None);
			_device.SetRenderState(RenderState.ZEnable, false);
			_device.SetRenderState(RenderState.ZWriteEnable, false);
			_device.SetRenderState(RenderState.Lighting, false);
		}

		public void EndScene()
			=> _device.EndScene();
	}
}
