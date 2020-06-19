using System;
using System.IO;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using BizHawk.Bizware.BizwareGL;

using SlimDX.Direct3D9;
using OpenTK;
using gl = OpenTK.Graphics.OpenGL;
using sd = System.Drawing;
using sdi = System.Drawing.Imaging;
using swf = System.Windows.Forms;

// todo - do a better job selecting shader model? base on caps somehow? try several and catch compilation exceptions (yuck, exceptions)
namespace BizHawk.Client.EmuHawk
{
	public class IGL_SlimDX9 : IGL
	{
		const int D3DERR_DEVICELOST = -2005530520;
		const int D3DERR_DEVICENOTRESET = -2005530519;

		private static Direct3D _d3d;
		internal Device Dev;
		private readonly INativeWindow _offscreenNativeWindow;

		// rendering state
		private IntPtr _pVertexData;
		private Pipeline _currPipeline;
		private GLControlWrapperSlimDX9 _currentControl;

		public string API => "D3D9";

		public IGL_SlimDX9()
		{
			if (_d3d == null)
			{
				_d3d = new Direct3D();
			}

			// make an 'offscreen context' so we can at least do things without having to create a window
			_offscreenNativeWindow = new NativeWindow { ClientSize = new Size(8, 8) };

			CreateDevice();
			CreateRenderStates();
		}

		public void AlternateVsyncPass(int pass)
		{
			for (; ; )
			{
				var status = Dev.GetRasterStatus(0);
				if (status.InVBlank && pass == 0) return; // wait for vblank to begin
				if (!status.InVBlank && pass == 1) return; // wait for vblank to end
				// STOP! think you can use System.Threading.SpinWait? No, it's way too slow.
				// (on my system, the vblank is something like 24 of 1074 scanlines @ 60hz ~= 0.35ms which is an awfully small window to nail)
			}
		}

		private void DestroyDevice()
		{
			if (Dev != null)
			{
				Dev.Dispose();
				Dev = null;
			}
		}

		private PresentParameters MakePresentParameters()
		{
			return new PresentParameters
			{
				BackBufferWidth = 8,
				BackBufferHeight = 8,
				BackBufferCount = 2,
				DeviceWindowHandle = _offscreenNativeWindow.WindowInfo.Handle,
				PresentationInterval = PresentInterval.Immediate,
				EnableAutoDepthStencil = false
			};
		}

		private void ResetDevice(GLControlWrapperSlimDX9 control)
		{
			SuspendRenderTargets();
			FreeControlSwapChain(control);
			for (; ; )
			{
				var result = Dev.TestCooperativeLevel();
				if (result.IsSuccess)
					break;
				if (result.Code == D3DERR_DEVICENOTRESET)
				{
					try
					{
						var pp = MakePresentParameters();
						Dev.Reset(pp);
						break;
					}
					catch { }
				}

				Thread.Sleep(100);
			}

			RefreshControlSwapChain(control);
			ResumeRenderTargets();
		}

		public void CreateDevice()
		{
			DestroyDevice();

			var pp = MakePresentParameters();

			var flags = CreateFlags.SoftwareVertexProcessing;
			if ((_d3d.GetDeviceCaps(0, DeviceType.Hardware).DeviceCaps & DeviceCaps.HWTransformAndLight) != 0)
			{
				flags = CreateFlags.HardwareVertexProcessing;
			}
			
			flags |= CreateFlags.FpuPreserve;
			Dev = new Device(_d3d, 0, DeviceType.Hardware, pp.DeviceWindowHandle, flags, pp);
		}

		void IDisposable.Dispose()
		{
			DestroyDevice();
			_d3d.Dispose();
		}

		public void Clear(OpenTK.Graphics.OpenGL.ClearBufferMask mask)
		{
			ClearFlags flags = ClearFlags.None;
			if ((mask & gl.ClearBufferMask.ColorBufferBit) != 0) flags |= ClearFlags.Target;
			if ((mask & gl.ClearBufferMask.DepthBufferBit) != 0) flags |= ClearFlags.ZBuffer;
			if ((mask & gl.ClearBufferMask.StencilBufferBit) != 0) flags |= ClearFlags.Stencil;
			Dev.Clear(flags, _clearColor, 0.0f, 0);
		}

		private int _clearColor;
		public void SetClearColor(Color color)
		{
			_clearColor = color.ToArgb();
		}

		public IBlendState CreateBlendState(gl.BlendingFactorSrc colorSource, gl.BlendEquationMode colorEquation, gl.BlendingFactorDest colorDest,
					gl.BlendingFactorSrc alphaSource, gl.BlendEquationMode alphaEquation, gl.BlendingFactorDest alphaDest)
		{
			return new CacheBlendState(true, colorSource, colorEquation, colorDest, alphaSource, alphaEquation, alphaDest);
		}

		public void FreeTexture(Texture2d tex) {
			var tw = (TextureWrapper)tex.Opaque;
			tw.Texture.Dispose();
		}

		private class ShaderWrapper // Disposable fields cleaned up by Internal_FreeShader
		{
			public ShaderBytecode bytecode;
			public VertexShader vs;
			public PixelShader ps;
			public Shader IGLShader;
		}

		/// <exception cref="InvalidOperationException"><paramref name="required"/> is <see langword="true"/> and compilation error occurred</exception>
		public Shader CreateFragmentShader(string source, string entry, bool required)
		{
			try
			{
				var sw = new ShaderWrapper();
			
				string errors = null;
				ShaderBytecode byteCode;

				try
				{
					string profile = "ps_3_0";

					// ShaderFlags.EnableBackwardsCompatibility - used this once upon a time (please leave a note about why)
					byteCode = ShaderBytecode.Compile(source, null, null, entry, profile, ShaderFlags.UseLegacyD3DX9_31Dll, out errors);
				}
				catch (Exception ex)
				{
					throw new InvalidOperationException($"Error compiling shader: {errors}", ex);
				}

				sw.ps = new PixelShader(Dev, byteCode);
				sw.bytecode = byteCode;

				Shader s = new Shader(this, sw, true);
				sw.IGLShader = s;

				return s;
			}
			catch (Exception ex)
			{
				if (required)
					throw;
				var s = new Shader(this, null, false) { Errors = ex.ToString() };
				return s;
			}
		}

		/// <exception cref="InvalidOperationException"><paramref name="required"/> is <see langword="true"/> and compilation error occurred</exception>
		public Shader CreateVertexShader(string source, string entry, bool required)
		{
			try
			{
				var sw = new ShaderWrapper();
				string errors = null;
				ShaderBytecode byteCode;

				try
				{
					string profile = "vs_3_0";
					byteCode = ShaderBytecode.Compile(source, null, null, entry, profile, ShaderFlags.EnableBackwardsCompatibility, out errors);
				}
				catch (Exception ex)
				{
					throw new InvalidOperationException($"Error compiling shader: {errors}", ex);
				}

				sw.vs = new VertexShader(Dev, byteCode);
				sw.bytecode = byteCode;

				Shader s = new Shader(this, sw, true);
				sw.IGLShader = s;
				return s;
			}
			catch(Exception ex)
			{
				if (required)
					throw;
				var s = new Shader(this, null, false) { Errors = ex.ToString() };
				return s;
			}
		}

		private BlendOperation ConvertBlendOp(gl.BlendEquationMode glMode)
		{
			return glMode switch
			{
				gl.BlendEquationMode.FuncAdd => BlendOperation.Add,
				gl.BlendEquationMode.FuncSubtract => BlendOperation.Subtract,
				gl.BlendEquationMode.Max => BlendOperation.Maximum,
				gl.BlendEquationMode.Min => BlendOperation.Minimum,
				gl.BlendEquationMode.FuncReverseSubtract => BlendOperation.ReverseSubtract,
				_ => throw new ArgumentOutOfRangeException()
			};
		}

		private Blend ConvertBlendArg(gl.BlendingFactorDest glMode) => ConvertBlendArg((gl.BlendingFactorSrc)glMode);

		private Blend ConvertBlendArg(gl.BlendingFactorSrc glMode) => glMode switch
		{
			gl.BlendingFactorSrc.Zero => Blend.Zero,
			gl.BlendingFactorSrc.One => Blend.One,
			gl.BlendingFactorSrc.SrcColor => Blend.SourceColor,
			gl.BlendingFactorSrc.OneMinusSrcColor => Blend.InverseSourceColor,
			gl.BlendingFactorSrc.SrcAlpha => Blend.SourceAlpha,
			gl.BlendingFactorSrc.OneMinusSrcAlpha => Blend.InverseSourceAlpha,
			gl.BlendingFactorSrc.DstAlpha => Blend.DestinationAlpha,
			gl.BlendingFactorSrc.OneMinusDstAlpha => Blend.InverseDestinationAlpha,
			gl.BlendingFactorSrc.DstColor => Blend.DestinationColor,
			gl.BlendingFactorSrc.OneMinusDstColor => Blend.InverseDestinationColor,
			gl.BlendingFactorSrc.SrcAlphaSaturate => Blend.SourceAlphaSaturated,
			gl.BlendingFactorSrc.ConstantColor => Blend.BlendFactor,
			gl.BlendingFactorSrc.OneMinusConstantColor => Blend.InverseBlendFactor,
			gl.BlendingFactorSrc.ConstantAlpha => throw new NotSupportedException(),
			gl.BlendingFactorSrc.OneMinusConstantAlpha => throw new NotSupportedException(),
			gl.BlendingFactorSrc.Src1Alpha => throw new NotSupportedException(),
			gl.BlendingFactorSrc.Src1Color => throw new NotSupportedException(),
			gl.BlendingFactorSrc.OneMinusSrc1Color => throw new NotSupportedException(),
			gl.BlendingFactorSrc.OneMinusSrc1Alpha => throw new NotSupportedException(),
			_ => throw new ArgumentOutOfRangeException()
		};

		public void SetBlendState(IBlendState rsBlend)
		{
			var myBs = (CacheBlendState)rsBlend;
			if (myBs.Enabled)
			{
				Dev.SetRenderState(RenderState.AlphaBlendEnable, true);
				Dev.SetRenderState(RenderState.SeparateAlphaBlendEnable, true);

				Dev.SetRenderState(RenderState.BlendOperation, ConvertBlendOp(myBs.colorEquation));
				Dev.SetRenderState(RenderState.SourceBlend, ConvertBlendArg(myBs.colorSource));
				Dev.SetRenderState(RenderState.DestinationBlend, ConvertBlendArg(myBs.colorDest));

				Dev.SetRenderState(RenderState.BlendOperationAlpha, ConvertBlendOp(myBs.alphaEquation));
				Dev.SetRenderState(RenderState.SourceBlendAlpha, ConvertBlendArg(myBs.alphaSource));
				Dev.SetRenderState(RenderState.DestinationBlendAlpha, ConvertBlendArg(myBs.alphaDest));
			}
			else Dev.SetRenderState(RenderState.AlphaBlendEnable, false);
			if (rsBlend == _rsBlendNoneOpaque)
			{
				// make sure constant color is set correctly
				Dev.SetRenderState(RenderState.BlendFactor, -1); // white
			}
		}

		private void CreateRenderStates()
		{
			_rsBlendNoneVerbatim = new CacheBlendState(
				false,
				gl.BlendingFactorSrc.One, gl.BlendEquationMode.FuncAdd, gl.BlendingFactorDest.Zero,
				gl.BlendingFactorSrc.One, gl.BlendEquationMode.FuncAdd, gl.BlendingFactorDest.Zero);

			_rsBlendNoneOpaque = new CacheBlendState(
				false,
				gl.BlendingFactorSrc.One, gl.BlendEquationMode.FuncAdd, gl.BlendingFactorDest.Zero,
				gl.BlendingFactorSrc.ConstantAlpha, gl.BlendEquationMode.FuncAdd, gl.BlendingFactorDest.Zero);

			_rsBlendNormal = new CacheBlendState(
				true,
				gl.BlendingFactorSrc.SrcAlpha, gl.BlendEquationMode.FuncAdd, gl.BlendingFactorDest.OneMinusSrcAlpha,
				gl.BlendingFactorSrc.One, gl.BlendEquationMode.FuncAdd, gl.BlendingFactorDest.Zero);
		}

		private CacheBlendState _rsBlendNoneVerbatim, _rsBlendNoneOpaque, _rsBlendNormal;

		public IBlendState BlendNoneCopy => _rsBlendNoneVerbatim;
		public IBlendState BlendNoneOpaque => _rsBlendNoneOpaque;
		public IBlendState BlendNormal => _rsBlendNormal;

		/// <exception cref="InvalidOperationException">
		/// <paramref name="required"/> is <see langword="true"/> and either <paramref name="vertexShader"/> or <paramref name="fragmentShader"/> is unavailable (their <see cref="Shader.Available"/> property is <see langword="false"/>), or
		/// one of <paramref name="vertexLayout"/>'s items has an unsupported value in <see cref="LayoutItem.AttribType"/>, <see cref="LayoutItem.Components"/>, or <see cref="LayoutItem.Usage"/>
		/// </exception>
		public Pipeline CreatePipeline(VertexLayout vertexLayout, Shader vertexShader, Shader fragmentShader, bool required, string memo)
		{
			if (!vertexShader.Available || !fragmentShader.Available)
			{
				string errors = $"Vertex Shader:\r\n {vertexShader.Errors} \r\n-------\r\nFragment Shader:\r\n{fragmentShader.Errors}";
				if (required)
				{
					throw new InvalidOperationException($"Couldn't build required GL pipeline:\r\n{errors}");
				}

				var pipeline = new Pipeline(this, null, false, null, null, null) { Errors = errors };
				return pipeline;
			}

			var ves = new VertexElement[vertexLayout.Items.Count];
			int stride = 0;
			foreach (var kvp in vertexLayout.Items)
			{
				var item = kvp.Value;
				DeclarationType declType;
				switch (item.AttribType)
				{
					case gl.VertexAttribPointerType.Float:
						if (item.Components == 1) declType = DeclarationType.Float1;
						else if (item.Components == 2) declType = DeclarationType.Float2;
						else if (item.Components == 3) declType = DeclarationType.Float3;
						else if (item.Components == 4) declType = DeclarationType.Float4;
						else throw new NotSupportedException();
						stride += 4 * item.Components;
						break;
					default:
						throw new NotSupportedException();
				}

				DeclarationUsage usage;
				byte usageIndex = 0;
				switch(item.Usage)
				{
					case AttributeUsage.Position: 
						usage = DeclarationUsage.Position; 
						break;
					case AttributeUsage.Texcoord0: 
						usage = DeclarationUsage.TextureCoordinate;
						break;
					case AttributeUsage.Texcoord1: 
						usage = DeclarationUsage.TextureCoordinate;
						usageIndex = 1;
						break;
					case AttributeUsage.Color0:
						usage = DeclarationUsage.Color;
						break;
					default:
						throw new NotSupportedException();
				}

				ves[kvp.Key] = new VertexElement(0, (short)item.Offset, declType, DeclarationMethod.Default, usage, usageIndex);
			}

			var pw = new PipelineWrapper
			{
				VertexDeclaration = new VertexDeclaration(Dev, ves),
				VertexShader = vertexShader.Opaque as ShaderWrapper,
				FragmentShader = fragmentShader.Opaque as ShaderWrapper,
				VertexStride = stride
			};

			//scan uniforms from constant tables
			//handles must be disposed later (with the pipeline probably)
			var uniforms = new List<UniformInfo>();
			var fs = pw.FragmentShader;
			var vs = pw.VertexShader;
			var fsct = fs.bytecode.ConstantTable;
			var vsct = vs.bytecode.ConstantTable;
			foreach(var ct in new[]{fsct,vsct})
			{
				var todo = new Queue<Tuple<string,EffectHandle>>();
				int n = ct.Description.Constants;
				for (int i = 0; i < n; i++)
				{
					var handle = ct.GetConstant(null, i);
					todo.Enqueue(Tuple.Create("", handle));
				}

				while(todo.Count != 0)
				{
					var tuple = todo.Dequeue();
					var prefix = tuple.Item1;
					var handle = tuple.Item2;
					var descr = ct.GetConstantDescription(handle);

					//Console.WriteLine($"D3D UNIFORM: {descr.Name}");

					if (descr.StructMembers != 0)
					{
						string newPrefix = $"{prefix}{descr.Name}.";
						for (int j = 0; j < descr.StructMembers; j++)
						{
							var subHandle = ct.GetConstant(handle, j);
							todo.Enqueue(Tuple.Create(newPrefix, subHandle));
						}
						continue;
					}

					var ui = new UniformInfo();
					var uw = new UniformWrapper();

					ui.Opaque = uw;
					string name = prefix + descr.Name;

					ui.Name = name;
					uw.Description = descr;
					uw.EffectHandle = handle;
					uw.FS = (ct == fsct);
					uw.CT = ct;
					if (descr.Type == ParameterType.Sampler2D)
					{
						ui.IsSampler = true;
						ui.SamplerIndex = descr.RegisterIndex;
						uw.SamplerIndex = descr.RegisterIndex;
					}

					uniforms.Add(ui);
				}
			}

			pw.fsct = fsct;
			pw.vsct = vsct;

			return new Pipeline(this, pw, true, vertexLayout, uniforms,memo);
		}

		public void FreePipeline(Pipeline pipeline)
		{
			var pw = pipeline.Opaque as PipelineWrapper;

			// unavailable pipelines will have no opaque
			if (pw == null)
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
			sw.bytecode.Dispose();
			sw.ps?.Dispose();
			sw.vs?.Dispose();
		}

		private class UniformWrapper
		{
			public EffectHandle EffectHandle;
			public ConstantDescription Description;
			public bool FS;
			public ConstantTable CT;
			public int SamplerIndex;
		}

		private class PipelineWrapper // Disposable fields cleaned up by FreePipeline
		{
			public VertexDeclaration VertexDeclaration;
			public ShaderWrapper VertexShader, FragmentShader;
			public int VertexStride;
			public ConstantTable fsct, vsct;
		}

		private class TextureWrapper
		{
			public Texture Texture;
			public TextureAddress WrapClamp = TextureAddress.Clamp;
			public TextureFilter MinFilter = TextureFilter.Point, MagFilter = TextureFilter.Point;
		}

		public VertexLayout CreateVertexLayout() => new VertexLayout(this, new IntPtr(0));

		public void BindPipeline(Pipeline pipeline)
		{
			_currPipeline = pipeline;

			if (pipeline == null)
			{
				// unbind? i don't know
				return;
			}

			var pw = (PipelineWrapper)pipeline.Opaque;
			Dev.PixelShader = pw.FragmentShader.ps;
			Dev.VertexShader = pw.VertexShader.vs;
			Dev.VertexDeclaration = pw.VertexDeclaration;
			
			//not helpful...
			//pw.vsct.SetDefaults(dev);
			//pw.fsct.SetDefaults(dev);
		}

		public void SetPipelineUniform(PipelineUniform uniform, bool value)
		{
			if (uniform.Owner == null)
			{
				return; // uniform was optimized out
			}

			foreach (var ui in uniform.UniformInfos)
			{
				var uw = (UniformWrapper)ui.Opaque;
				uw.CT.SetValue(Dev, uw.EffectHandle, value);
			}
		}

		public unsafe void SetPipelineUniformMatrix(PipelineUniform uniform, Matrix4 mat, bool transpose)
		{
			if (uniform.Owner == null)
			{
				return; // uniform was optimized out
			}

			foreach (var ui in uniform.UniformInfos)
			{
				var uw = (UniformWrapper)ui.Opaque;
				uw.CT.SetValue(Dev, uw.EffectHandle, mat.ToSlimDXMatrix(!transpose));
			}
		}

		public unsafe void SetPipelineUniformMatrix(PipelineUniform uniform, ref Matrix4 mat, bool transpose)
		{
			if (uniform.Owner == null)
			{
				return; // uniform was optimized out
			}

			foreach (var ui in uniform.UniformInfos)
			{
				var uw = (UniformWrapper)ui.Opaque;
				uw.CT.SetValue(Dev, uw.EffectHandle, mat.ToSlimDXMatrix(!transpose));
			}
		}

		public void SetPipelineUniform(PipelineUniform uniform, Vector4 value)
		{
			if (uniform.Owner == null)
			{
				return; // uniform was optimized out
			}

			foreach (var ui in uniform.UniformInfos)
			{
				var uw = (UniformWrapper)ui.Opaque;
				uw.CT.SetValue(Dev, uw.EffectHandle, value.ToSlimDXVector4());
			}
		}

		public void SetPipelineUniform(PipelineUniform uniform, Vector2 value)
		{
			if (uniform.Owner == null) return; // uniform was optimized out

			foreach (var ui in uniform.UniformInfos)
			{
				var uw = (UniformWrapper)ui.Opaque;
				uw.CT.SetValue(Dev, uw.EffectHandle, value.ToSlimDXVector2());
			}
		}

		public void SetPipelineUniform(PipelineUniform uniform, float value)
		{
			if (uniform.Owner == null) return; // uniform was optimized out
			foreach (var ui in uniform.UniformInfos)
			{
				var uw = (UniformWrapper)ui.Opaque;
				uw.CT.SetValue(Dev, uw.EffectHandle, value);
			}
		}

		public unsafe void SetPipelineUniform(PipelineUniform uniform, Vector4[] values)
		{
			if (uniform.Owner == null) return; // uniform was optimized out
			var v = new SlimDX.Vector4[values.Length];
			for (int i = 0; i < values.Length; i++)
			{
				v[i] = values[i].ToSlimDXVector4();
			}

			foreach (var ui in uniform.UniformInfos)
			{
				var uw = (UniformWrapper)ui.Opaque;
				uw.CT.SetValue(Dev, uw.EffectHandle, v);
			}
		}

		public void SetPipelineUniformSampler(PipelineUniform uniform, Texture2d tex)
		{
			if (uniform.Owner == null) return; // uniform was optimized out
			var tw = tex.Opaque as TextureWrapper;

			foreach (var ui in uniform.UniformInfos)
			{
				var uw = (UniformWrapper)ui.Opaque;
				Dev.SetTexture(uw.SamplerIndex, tw.Texture);

				Dev.SetSamplerState(uw.SamplerIndex, SamplerState.AddressU, tw.WrapClamp);
				Dev.SetSamplerState(uw.SamplerIndex, SamplerState.AddressV, tw.WrapClamp);
				Dev.SetSamplerState(uw.SamplerIndex, SamplerState.MinFilter, tw.MinFilter);
				Dev.SetSamplerState(uw.SamplerIndex, SamplerState.MagFilter, tw.MagFilter);
			}
		}

		public void SetTextureWrapMode(Texture2d tex, bool clamp)
		{
			var tw = (TextureWrapper)tex.Opaque;
			tw.WrapClamp = clamp ? TextureAddress.Clamp : TextureAddress.Wrap;
		}

		public void TexParameter2d(Texture2d tex, gl.TextureParameterName pName, int param)
		{
			var tw = (TextureWrapper)tex.Opaque;

			if (pName == gl.TextureParameterName.TextureMinFilter)
			{
				tw.MinFilter = param == (int)gl.TextureMinFilter.Linear
					? TextureFilter.Linear
					: TextureFilter.Point;
			}

			if (pName == gl.TextureParameterName.TextureMagFilter)
			{
				tw.MagFilter = param == (int)gl.TextureMagFilter.Linear
					? TextureFilter.Linear 
					: TextureFilter.Point;
			}
		}

		public Texture2d LoadTexture(Bitmap bitmap)
		{
			using var bmp = new BitmapBuffer(bitmap, new BitmapLoadOptions());
			return (this as IGL).LoadTexture(bmp);
		}

		public Texture2d LoadTexture(Stream stream)
		{
			using var bmp = new BitmapBuffer(stream, new BitmapLoadOptions());
			return (this as IGL).LoadTexture(bmp);
		}

		public Texture2d CreateTexture(int width, int height) => null;

		public Texture2d WrapGLTexture2d(IntPtr glTexId, int width, int height)
		{
			// not needed 1st pass (except for GL cores)
			// TODO - need to rip the texture data. we had code for that somewhere...
			return null;
		}

		/// <exception cref="InvalidOperationException">GDI+ call returned unexpected data</exception>
		public unsafe void LoadTextureData(Texture2d tex, BitmapBuffer bmp)
		{
			sdi.BitmapData bmpData = bmp.LockBits();
			var tw = tex.Opaque as TextureWrapper;
			var dr = tw.Texture.LockRectangle(0, LockFlags.None);

			// TODO - do we need to handle odd sizes, weird pitches here?
			if (bmp.Width * 4 != bmpData.Stride)
			{
				throw new InvalidOperationException();
			}

			dr.Data.WriteRange(bmpData.Scan0, bmp.Width * bmp.Height * 4);
			dr.Data.Close();

			tw.Texture.UnlockRectangle(0);
			bmp.UnlockBits(bmpData);
		}

		public Texture2d LoadTexture(BitmapBuffer bmp)
		{
			var tex = new Texture(Dev, bmp.Width, bmp.Height, 1, Usage.None, Format.A8R8G8B8, Pool.Managed);
			var tw = new TextureWrapper { Texture = tex };
			var ret = new Texture2d(this, tw, bmp.Width, bmp.Height);
			LoadTextureData(ret, bmp);
			return ret;
		}

		/// <exception cref="InvalidOperationException">SlimDX call returned unexpected data</exception>
		public unsafe BitmapBuffer ResolveTexture2d(Texture2d tex)
		{
			//TODO - lazy create and cache resolving target in RT
			var target = new Texture(Dev, tex.IntWidth, tex.IntHeight, 1, Usage.None, Format.A8R8G8B8, Pool.SystemMemory);
			var tw = tex.Opaque as TextureWrapper;
			Dev.GetRenderTargetData(tw.Texture.GetSurfaceLevel(0), target.GetSurfaceLevel(0));
			var dr = target.LockRectangle(0, LockFlags.ReadOnly);
			if (dr.Pitch != tex.IntWidth * 4) throw new InvalidOperationException();
			int[] pixels = new int[tex.IntWidth * tex.IntHeight];
			dr.Data.ReadRange(pixels, 0, tex.IntWidth * tex.IntHeight);
			var bb = new BitmapBuffer(tex.IntWidth, tex.IntHeight, pixels);
			target.UnlockRectangle(0);
			target.Dispose(); // buffer churn warning
			return bb;
		}

		public Texture2d LoadTexture(string path)
		{
			//not needed 1st pass ??
			//todo
			//using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
			//  return (this as IGL).LoadTexture(fs);
			return null;
		}

		public Matrix4 CreateGuiProjectionMatrix(int w, int h)
		{
			return CreateGuiProjectionMatrix(new Size(w, h));
		}

		public Matrix4 CreateGuiViewMatrix(int w, int h, bool autoFlip)
		{
			return CreateGuiViewMatrix(new Size(w, h), autoFlip);
		}

		public Matrix4 CreateGuiProjectionMatrix(Size dims)
		{
			Matrix4 ret = Matrix4.Identity;
			ret.M11 = 2.0f / (float)dims.Width;
			ret.M22 = 2.0f / (float)dims.Height;
			return ret;
		}

		public Matrix4 CreateGuiViewMatrix(Size dims, bool autoFlip)
		{
			Matrix4 ret = Matrix4.Identity;
			ret.M22 = -1.0f;
			ret.M41 = -(float)dims.Width * 0.5f - 0.5f;
			ret.M42 = (float)dims.Height * 0.5f + 0.5f;

			// auto-flipping isn't needed on d3d
			return ret;
		}

		public void SetViewport(int x, int y, int width, int height)
		{
			Dev.Viewport = new Viewport(x, y, width, height);
			Dev.ScissorRect = new Rectangle(x, y, width, height);
		}

		public void SetViewport(int width, int height)
		{
			SetViewport(0, 0, width, height);
		}

		public void SetViewport(Size size)
		{
			SetViewport(size.Width, size.Height);
		}

		public void SetViewport(swf.Control control)
		{
			var r = control.ClientRectangle;
			SetViewport(r.Left, r.Top, r.Width, r.Height);
		}

		public void BeginControl(GLControlWrapperSlimDX9 control)
		{
			_currentControl = control;

			// this dispose isn't strictly needed but it seems benign
			var surface = _currentControl.SwapChain.GetBackBuffer(0);
			Dev.SetRenderTarget(0, surface);
			surface.Dispose();
		}

		/// <exception cref="InvalidOperationException"><paramref name="control"/> does not match control passed to <see cref="BeginControl"/></exception>
		public void EndControl(GLControlWrapperSlimDX9 control)
		{
			if (control != _currentControl)
			{
				throw new InvalidOperationException();
			}

			var surface = _currentControl.SwapChain.GetBackBuffer(0);
			Dev.SetRenderTarget(0, surface);
			surface.Dispose();

			_currentControl = null;
		}

		public void SwapControl(GLControlWrapperSlimDX9 control)
		{
			EndControl(control);

			try
			{
				var result = control.SwapChain.Present(Present.None);
				//var rs = dev.GetRasterStatus(0);
			}
			catch(Direct3D9Exception ex)
			{
				if (ex.ResultCode.Code == D3DERR_DEVICELOST)
					ResetDevice(control);
			}
		}

		private readonly HashSet<RenderTarget> _renderTargets = new HashSet<RenderTarget>();

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
			return new Texture(Dev, w, h, 1, Usage.RenderTarget, Format.A8R8G8B8, Pool.Default);
		}

		private void SuspendRenderTargets()
		{
			foreach (var rt in _renderTargets)
			{
				var tw = rt.Opaque as TextureWrapper;
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
				var surface = _currentControl.SwapChain.GetBackBuffer(0);
				Dev.SetRenderTarget(0, surface);
				surface.Dispose();

				Dev.DepthStencilSurface = null;
				return;
			}

			// dispose doesn't seem necessary for reset here...
			var tw = rt.Opaque as TextureWrapper;
			Dev.SetRenderTarget(0, tw.Texture.GetSurfaceLevel(0));
			Dev.DepthStencilSurface = null;
		}

		public void FreeControlSwapChain(GLControlWrapperSlimDX9 control)
		{
			if (control.SwapChain != null)
			{
				control.SwapChain.Dispose();
				control.SwapChain = null;
			}
		}

		public void RefreshControlSwapChain(GLControlWrapperSlimDX9 control)
		{
			FreeControlSwapChain(control);

			var pp = new PresentParameters
			{
				BackBufferWidth = Math.Max(8,control.ClientSize.Width),
				BackBufferHeight = Math.Max(8, control.ClientSize.Height),
				BackBufferCount = 1,
				BackBufferFormat = Format.X8R8G8B8,
				DeviceWindowHandle = control.Handle,
				Windowed = true,
				PresentationInterval = control.Vsync ? PresentInterval.One : PresentInterval.Immediate
			};

			control.SwapChain = new SwapChain(Dev, pp);
		}

		public IGraphicsControl Internal_CreateGraphicsControl()
		{
			var ret = new GLControlWrapperSlimDX9(this);
			RefreshControlSwapChain(ret);
			return ret;
		}

		/// <exception cref="NotSupportedException"><paramref name="mode"/> is <see cref="gl.PrimitiveType.TriangleStrip"/></exception>
		public unsafe void DrawArrays(gl.PrimitiveType mode, int first, int count)
		{
			var pt = PrimitiveType.TriangleStrip;

			if (mode != gl.PrimitiveType.TriangleStrip)
			{
				throw new NotSupportedException();
			}

			//for tristrip
			int primCount = count - 2;

			var pw = (PipelineWrapper)_currPipeline.Opaque;
			int stride = pw.VertexStride;
			byte* ptr = (byte*)_pVertexData.ToPointer() + first * stride;

			Dev.DrawUserPrimitives(pt, primCount, (void*)ptr, (uint)stride);
		}

		public unsafe void BindArrayData(void* pData)
		{
			_pVertexData = new IntPtr(pData);
		}

		public void BeginScene()
		{
			Dev.BeginScene();
			Dev.SetRenderState(RenderState.CullMode, Cull.None);
			Dev.SetRenderState(RenderState.ZEnable, false);
			Dev.SetRenderState(RenderState.ZWriteEnable, false);
			Dev.SetRenderState(RenderState.Lighting, false);
		}

		public void EndScene()
		{
			Dev.EndScene();
		}
	}
}
