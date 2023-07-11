// this is a mess, get back to it later
#if false
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;

using BizHawk.Bizware.BizwareGL;
using BizHawk.Common;
using BizHawk.Common.StringExtensions;
using Vortice.D3DCompiler;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.Direct3D11.Shader;
using Vortice.DXGI;
using static SDL2.SDL;

using BizPrimitiveType = BizHawk.Bizware.BizwareGL.PrimitiveType;

using Color4 = Vortice.Mathematics.Color4;

// todo - do a better job selecting shader model? base on caps somehow? try several and catch compilation exceptions (yuck, exceptions)
namespace BizHawk.Bizware.Graphics
{
	/// <summary>
	/// Direct3D11 implementation of the BizwareGL.IGL interface
	/// </summary>
	public sealed class IGL_D3D11 : IGL
	{
		public EDispMethod DispMethodEnum => EDispMethod.D3D9;

		private const int D3DERR_DEVICELOST = unchecked((int)0x88760868);
		private const int D3DERR_DEVICENOTRESET = unchecked((int)0x88760869);

		private IDXGIFactory2 _factory;
		private ID3D11Device _device;
		private ID3D11DeviceContext _context;

		private IntPtr _offscreenSdl2Window;
		private IntPtr OffscreenNativeWindow;

		// rendering state
		private IntPtr _pVertexData;
		private Pipeline _currPipeline;
		private RenderTarget _currentRenderTarget;
		private D3D11Control _currentControl;

		// misc state
		private Color4 _clearColor;
		private CacheBlendState _rsBlendNoneVerbatim, _rsBlendNoneOpaque, _rsBlendNormal;
		private readonly HashSet<RenderTarget> _renderTargets = new();

		public string API => "D3D11";

		static IGL_D3D11()
		{
			if (SDL_Init(SDL_INIT_VIDEO) != 0)
			{
				throw new($"Failed to init SDL video, SDL error: {SDL_GetError()}");
			}
		}

		public IGL_D3D11()
		{
			if (OSTailoredCode.IsUnixHost)
			{
				throw new NotSupportedException("D3D11 is Windows only");
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

			_factory = DXGI.CreateDXGIFactory1<IDXGIFactory2>();

			CreateDevice();
			CreateRenderStates();
		}

		private void CreateDevice()
		{
			// IGL is not thread safe, so let's not bother making this implementation thread safe
			D3D11.D3D11CreateDevice(null, DriverType.Hardware, DeviceCreationFlags.Singlethreaded, Array.Empty<FeatureLevel>(), out _device, out _context).CheckError();
		}

		private void DestroyDevice()
		{
			if (_device != null)
			{
				_device.Dispose();
				_device = null;
			}

			if (_context != null)
			{
				_context.Dispose();
				_device = null;
			}
		}

		/*private PresentParameters MakePresentParameters()
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
		}*/

		public void Dispose()
		{
			DestroyDevice();

			if (_offscreenSdl2Window != IntPtr.Zero)
			{
				SDL_DestroyWindow(_offscreenSdl2Window);
				_offscreenSdl2Window = OffscreenNativeWindow = IntPtr.Zero;
			}
		}

		public void Clear(ClearBufferMask mask)
		{
			if ((mask & ClearBufferMask.ColorBufferBit) != 0)
			{
				var tw = (TextureWrapper)_currentRenderTarget.Texture2d
				_context.ClearRenderTargetView((ID3D11RenderTargetView), _clearColor);
			}

			/*var flags = ClearFlags.None;
			if ((mask & ClearBufferMask.ColorBufferBit) != 0) flags |= ClearFlags.Target;
			if ((mask & ClearBufferMask.DepthBufferBit) != 0) flags |= ClearFlags.ZBuffer;
			if ((mask & ClearBufferMask.StencilBufferBit) != 0) flags |= ClearFlags.Stencil;

			_device.Clear(flags, _clearColor, 0.0f, 0);*/
		}

		public void SetClearColor(Color color)
			=> _clearColor = new(color.R, color.G, color.B, color.A);

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
			public ReadOnlyMemory<byte> Bytecode;
			public ID3D11ShaderReflection Reflection;
			public ID3D11VertexShader VS;
			public ID3D11PixelShader PS;
			public Shader IGLShader;
		}

		/// <exception cref="InvalidOperationException"><paramref name="required"/> is <see langword="true"/> and compilation error occurred</exception>
		public Shader CreateFragmentShader(string source, string entry, bool required)
		{
			try
			{
				var sw = new ShaderWrapper();

				// ShaderFlags.EnableBackwardsCompatibility - used this once upon a time (please leave a note about why)
				var result = Compiler.Compile(
					shaderSource: source,
					entryPoint: entry,
					sourceName: null!, // this is safe to be null
					profile: "ps_4_0_level_9_1",
					shaderFlags: ShaderFlags.OptimizationLevel3);

				sw.PS = _device.CreatePixelShader(result.Span);
				sw.Reflection = Compiler.Reflect<ID3D11ShaderReflection>(result.Span);

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

				var result = Compiler.Compile(
					shaderSource: source,
					entryPoint: entry,
					sourceName: null!, // this is safe to be null
					profile: "vs_4_0_level_9_1",
					shaderFlags: ShaderFlags.OptimizationLevel3);

				sw.VS = _device.CreateVertexShader(result.Span);
				sw.Reflection = Compiler.Reflect<ID3D11ShaderReflection>(result.Span);

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
				BlendEquationMode.Max => BlendOperation.Max,
				BlendEquationMode.Min => BlendOperation.Min,
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
				BlendingFactorSrc.SrcAlphaSaturate => Blend.SourceAlphaSaturate,
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

			var bd = default(BlendDescription);
			bd.AlphaToCoverageEnable = false;
			bd.IndependentBlendEnable = false;
			bd.RenderTarget[0].BlendEnable = myBs.Enabled;
			bd.RenderTarget[0].SourceBlend = ConvertBlendArg(myBs.colorSource);
			bd.RenderTarget[0].DestinationBlend = ConvertBlendArg(myBs.colorDest);
			bd.RenderTarget[0].BlendOperation = ConvertBlendOp(myBs.colorEquation);
			bd.RenderTarget[0].SourceBlendAlpha = ConvertBlendArg(myBs.alphaSource);
			bd.RenderTarget[0].DestinationBlendAlpha = ConvertBlendArg(myBs.alphaDest);
			bd.RenderTarget[0].BlendOperationAlpha = ConvertBlendOp(myBs.alphaEquation);
			bd.RenderTarget[0].RenderTargetWriteMask = ColorWriteEnable.All;

			using var bs = _device.CreateBlendState(bd);
			_context.OMSetBlendState(bs, new Color4(1.0f));
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

			var ves = new InputElementDescription[vertexLayout.Items.Count];
			var stride = 0;
			foreach (var (i, item) in vertexLayout.Items)
			{
				if (item.AttribType != VertexAttribPointerType.Float)
				{
					throw new NotSupportedException();
				}

				ves[i] = new(item.Name, item.Usage == AttribUsage.Texcoord1 ? 1 : 0, Format.R32_Float, item.Offset, i);
				stride += 4 * item.Components;
			}

			var bc = ((ShaderWrapper)vertexShader.Opaque).Bytecode.Span;
			var pw = new PipelineWrapper
			{
				VertexDeclaration = _device.CreateInputLayout(ves, bc),
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
			public ID3D11InputLayout VertexDeclaration;
			public ShaderWrapper VertexShader, FragmentShader;
			public int VertexStride;
		}

		private class TextureWrapper
		{
			public ID3D11Texture2D Texture;
			public TextureAddress WrapClamp = TextureAddress.Clamp;
			public TextureFilter MinFilter = TextureFilter.Point, MagFilter = TextureFilter.Point;
		}

		public VertexLayout CreateVertexLayout() => new(this, new IntPtr(0));

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

			var rd = new RasterizerDescription()
			{
				CullMode = CullMode.None,
				FillMode = FillMode.Solid,
				ScissorEnable = true,
			};

			using var rs = _device.CreateRasterizerState(rd);
			_context.RSSetState(rs);
		}

		public void SetPipelineUniform(PipelineUniform uniform, bool value)
		{
			foreach (var ui in uniform.UniformInfos)
			{
				var uw = (UniformWrapper)ui.Opaque;
				uw.CT.SetValue(_device, uw.EffectHandle, value);
			}
		}

		public void SetPipelineUniformMatrix(PipelineUniform uniform, Matrix4 mat, bool transpose)
			=> SetPipelineUniformMatrix(uniform, ref mat, transpose);

		public void SetPipelineUniformMatrix(PipelineUniform uniform, ref Matrix4 mat, bool transpose)
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
			// not needed 1st pass (except for GL cores)
			// TODO - need to rip the texture data. we had code for that somewhere...
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

		public Matrix4 CreateGuiProjectionMatrix(int w, int h)
		{
			return CreateGuiProjectionMatrix(new(w, h));
		}

		public Matrix4 CreateGuiViewMatrix(int w, int h, bool autoFlip)
		{
			return CreateGuiViewMatrix(new(w, h), autoFlip);
		}

		public Matrix4 CreateGuiProjectionMatrix(Size dims)
		{
			var ret = Matrix4.Identity;
			ret.Row0.X = 2.0f / dims.Width;
			ret.Row1.Y = 2.0f / dims.Height;
			return ret;
		}

		public Matrix4 CreateGuiViewMatrix(Size dims, bool autoFlip)
		{
			var ret = Matrix4.Identity;
			ret.Row1.Y = -1.0f;
			ret.Row3.X = -dims.Width * 0.5f - 0.5f;
			ret.Row3.Y = dims.Height * 0.5f + 0.5f;

			// auto-flipping isn't needed on d3d
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

		internal void BeginControl(D3D11Control control)
		{
			_currentControl = control;

			using var renderTargetView = control.SwapChain.GetBuffer<ID3D11RenderTargetView>(0);
			_context.OMSetRenderTargets(renderTargetView);
		}

		/// <exception cref="InvalidOperationException"><paramref name="control"/> does not match control passed to <see cref="BeginControl"/></exception>
		internal void EndControl(D3D11Control control)
		{
			if (control != _currentControl)
			{
				throw new InvalidOperationException($"{nameof(control)} does not match control passed to {nameof(BeginControl)}");
			}

			using var renderTargetView = control.SwapChain.GetBuffer<ID3D11RenderTargetView>(0);
			_context.OMSetRenderTargets(renderTargetView);

			_currentControl = null;
		}

		internal void SwapControl(D3D11Control control)
		{
			EndControl(control);
			control.SwapChain.Present(control.Vsync ? 1 : 0);
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
				_currentRenderTarget = null;
				return;
			}

			// dispose doesn't seem necessary for reset here...
			var tw = (TextureWrapper)rt.Opaque;
			_device.SetRenderTarget(0, tw.Texture.GetSurfaceLevel(0));
			_device.DepthStencilSurface = null;
			_currentRenderTarget = rt;
		}

		internal static void FreeControlSwapChain(D3D11Control control)
		{
			control.SwapChain?.Dispose();
			control.SwapChain = null;
		}

		internal void RefreshControlSwapChain(D3D11Control control)
		{
			FreeControlSwapChain(control);

			var sd = new SwapChainDescription1(
				width: Math.Max(1, control.ClientSize.Width),
				height: Math.Max(1, control.ClientSize.Height),
				format: Format.B8G8R8X8_UNorm,
				stereo: false,
				swapEffect: SwapEffect.Discard,
				bufferUsage: Usage.RenderTargetOutput,
				bufferCount: 1,
				scaling: Scaling.None,
				alphaMode: AlphaMode.Ignore,
				flags: control.Vsync ? SwapChainFlags.None : SwapChainFlags.AllowTearing);

			control.SwapChain = _factory.CreateSwapChainForHwnd(_device, control.Handle, sd);
		}

		public IGraphicsControl Internal_CreateGraphicsControl()
		{
			var ret = new D3D11Control(this);
			ret.CreateControl();
			return ret;
		}

		/// <exception cref="NotSupportedException"><paramref name="mode"/> is not <see cref="BizwareGL.PrimitiveType.TriangleStrip"/></exception>
		public void DrawArrays(BizPrimitiveType mode, int first, int count)
		{
			if (mode != BizPrimitiveType.TriangleStrip)
			{
				throw new NotSupportedException();
			}

			_context.IASetPrimitiveTopology(PrimitiveTopology.TriangleStrip);

			var pw = (PipelineWrapper)_currPipeline.Opaque;
			var stride = pw.VertexStride;
			var ptr = _pVertexData;

			var bd = new BufferDescription(stride * count, BindFlags.VertexBuffer);
			using var vb = _device.CreateBuffer(in bd, ptr);
			_context.IASetVertexBuffer(0, vb, stride);
			_context.Draw(count, first * stride);
		}

		public void BindArrayData(IntPtr pData)
			=> _pVertexData = pData;

		public void BeginScene()
			=> _context.Begin(null);

		public void EndScene()
			=> _context.End(null);
	}
}
#endif
