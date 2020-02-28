using System;
using System.IO;
using System.Collections.Generic;
using System.Drawing;
using BizHawk.Bizware.BizwareGL;

using SlimDX.Direct3D9;
using OpenTK;
using d3d9 = SlimDX.Direct3D9;
using gl = OpenTK.Graphics.OpenGL;
using sd = System.Drawing;
using sdi = System.Drawing.Imaging;
using swf = System.Windows.Forms;

//todo - do a better job selecting shader model? base on caps somehow? try several and catch compilation exceptions (yuck, exceptions)

namespace BizHawk.Client.EmuHawk
{

	public class IGL_SlimDX9 : IGL
	{
		const int D3DERR_DEVICELOST = -2005530520;
		const int D3DERR_DEVICENOTRESET = -2005530519;

		static Direct3D d3d;
		internal Device dev;
		INativeWindow OffscreenNativeWindow;

		//rendering state
		IntPtr _pVertexData;
		Pipeline _CurrPipeline;
		GLControlWrapper_SlimDX9 _CurrentControl;

		public string API => "D3D9";

		public IGL_SlimDX9()
		{
			if (d3d == null)
			{
				d3d = new Direct3D();
			}

			//make an 'offscreen context' so we can at least do things without having to create a window
			OffscreenNativeWindow = new NativeWindow { ClientSize = new Size(8, 8) };

			CreateDevice();
			CreateRenderStates();
		}

		public void AlternateVsyncPass(int pass)
		{
			for (; ; )
			{
				var status = dev.GetRasterStatus(0);
				if (status.InVBlank && pass == 0) return; //wait for vblank to begin
				if (!status.InVBlank && pass == 1) return; //wait for vblank to end
				//STOP! think you can use System.Threading.SpinWait? No, it's way too slow.
				//(on my system, the vblank is something like 24 of 1074 scanlines @ 60hz ~= 0.35ms which is an awfully small window to nail)
			}
		}

		private void DestroyDevice()
		{
			if (dev != null)
			{
				dev.Dispose();
				dev = null;
			}
		}

		PresentParameters MakePresentParameters()
		{
			return new PresentParameters
			{
				BackBufferWidth = 8,
				BackBufferHeight = 8,
				BackBufferCount = 2,
				DeviceWindowHandle = OffscreenNativeWindow.WindowInfo.Handle,
				PresentationInterval = PresentInterval.Immediate,
				EnableAutoDepthStencil = false
			};
		}

		void ResetDevice(GLControlWrapper_SlimDX9 control)
		{
			SuspendRenderTargets();
			FreeControlSwapChain(control);
			for (; ; )
			{
				var result = dev.TestCooperativeLevel();
				if (result.IsSuccess)
					break;
				if (result.Code == D3DERR_DEVICENOTRESET)
				{
					try
					{
						var pp = MakePresentParameters();
						dev.Reset(pp);
						break;
					}
					catch { }
				}
				System.Threading.Thread.Sleep(100);
			}
			RefreshControlSwapChain(control);
			ResumeRenderTargets();
		}

		public void CreateDevice()
		{
			DestroyDevice();

			var pp = MakePresentParameters();

			var flags = CreateFlags.SoftwareVertexProcessing;
			if ((d3d.GetDeviceCaps(0, DeviceType.Hardware).DeviceCaps & DeviceCaps.HWTransformAndLight) != 0)
			{
				flags = CreateFlags.HardwareVertexProcessing;
			}
			
			flags |= CreateFlags.FpuPreserve;
			dev = new Device(d3d, 0, DeviceType.Hardware, pp.DeviceWindowHandle, flags, pp);
		}

		void IDisposable.Dispose()
		{
			DestroyDevice();
			d3d.Dispose();
		}

		public void Clear(OpenTK.Graphics.OpenGL.ClearBufferMask mask)
		{
			ClearFlags flags = ClearFlags.None;
			if ((mask & gl.ClearBufferMask.ColorBufferBit) != 0) flags |= ClearFlags.Target;
			if ((mask & gl.ClearBufferMask.DepthBufferBit) != 0) flags |= ClearFlags.ZBuffer;
			if ((mask & gl.ClearBufferMask.StencilBufferBit) != 0) flags |= ClearFlags.Stencil;
			dev.Clear(flags, _clearColor, 0.0f, 0);
		}

		int _clearColor;
		public void SetClearColor(sd.Color color)
		{
			_clearColor = color.ToArgb();
		}

		public IBlendState CreateBlendState(gl.BlendingFactorSrc colorSource, gl.BlendEquationMode colorEquation, gl.BlendingFactorDest colorDest,
					gl.BlendingFactorSrc alphaSource, gl.BlendEquationMode alphaEquation, gl.BlendingFactorDest alphaDest)
		{
			return new CacheBlendState(true, colorSource, colorEquation, colorDest, alphaSource, alphaEquation, alphaDest);
		}


		public void FreeTexture(Texture2d tex) {
			var tw = tex.Opaque as TextureWrapper;
			tw.Texture.Dispose();
		}

		class ShaderWrapper // Disposable fields cleaned up by Internal_FreeShader
		{
			public d3d9.ShaderBytecode bytecode;
			public d3d9.VertexShader vs;
			public d3d9.PixelShader ps;
			public Shader IGLShader;
			public Dictionary<string, string> MapCodeToNative;
			public Dictionary<string, string> MapNativeToCode;
		}

		/// <exception cref="InvalidOperationException"><paramref name="required"/> is <see langword="true"/> and compilation error occurred</exception>
		public Shader CreateFragmentShader(bool cg, string source, string entry, bool required)
		{
			try
			{
				ShaderWrapper sw = new ShaderWrapper();
				if (cg)
				{
					var cgc = new CGC();
					var results = cgc.Run(source, entry, "hlslf", true);
					source = results.Code;
					entry = "main";
					if (!results.Succeeded)
					{
						if (required) throw new InvalidOperationException(results.Errors);
						else return new Shader(this, null, false);
					}

					sw.MapCodeToNative = results.MapCodeToNative;
					sw.MapNativeToCode = results.MapNativeToCode;
				}

				string errors = null;
				d3d9.ShaderBytecode bytecode = null;

				try
				{
					//cgc can create shaders that will need backwards compatibility...
					string profile = "ps_1_0";
					if (cg)
						profile = "ps_3_0"; //todo - smarter logic somehow

					//ShaderFlags.EnableBackwardsCompatibility - used this once upon a time (please leave a note about why)
					//
					bytecode = d3d9.ShaderBytecode.Compile(source, null, null, entry, profile, ShaderFlags.UseLegacyD3DX9_31Dll, out errors);
				}
				catch (Exception ex)
				{
					throw new InvalidOperationException($"Error compiling shader: {errors}", ex);
				}

				sw.ps = new PixelShader(dev, bytecode);
				sw.bytecode = bytecode;

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
		public Shader CreateVertexShader(bool cg, string source, string entry, bool required)
		{
			try
			{
				ShaderWrapper sw = new ShaderWrapper();
				if (cg)
				{
					var cgc = new CGC();
					var results = cgc.Run(source, entry, "hlslv", true);
					source = results.Code;
					entry = "main";
					if (!results.Succeeded)
					{
						if (required) throw new InvalidOperationException(results.Errors);
						return new Shader(this, null, false);
					}

					sw.MapCodeToNative = results.MapCodeToNative;
					sw.MapNativeToCode = results.MapNativeToCode;
				}

				string errors = null;
				d3d9.ShaderBytecode bytecode = null;

				try
				{
					//cgc can create shaders that will need backwards compatibility...
					string profile = "vs_1_1";
					if (cg)
						profile = "vs_3_0"; //todo - smarter logic somehow

					bytecode = d3d9.ShaderBytecode.Compile(source, null, null, entry, profile, ShaderFlags.EnableBackwardsCompatibility, out errors);
				}
				catch (Exception ex)
				{
					throw new InvalidOperationException($"Error compiling shader: {errors}", ex);
				}

				sw.vs = new VertexShader(dev, bytecode);
				sw.bytecode = bytecode;

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

		BlendOperation ConvertBlendOp(gl.BlendEquationMode glmode)
		{
			if (glmode == gl.BlendEquationMode.FuncAdd) return BlendOperation.Add;
			if (glmode == gl.BlendEquationMode.FuncSubtract) return BlendOperation.Subtract;
			if (glmode == gl.BlendEquationMode.Max) return BlendOperation.Maximum;
			if (glmode == gl.BlendEquationMode.Min) return BlendOperation.Minimum;
			if (glmode == gl.BlendEquationMode.FuncReverseSubtract) return BlendOperation.ReverseSubtract;
			throw new ArgumentOutOfRangeException();
		}

		Blend ConvertBlendArg(gl.BlendingFactorDest glmode) { return ConvertBlendArg((gl.BlendingFactorSrc)glmode); }

		Blend ConvertBlendArg(gl.BlendingFactorSrc glmode) => glmode switch
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
			
			var mybs = rsBlend as CacheBlendState;
			if (mybs.Enabled)
			{
				dev.SetRenderState(RenderState.AlphaBlendEnable, true);
				dev.SetRenderState(RenderState.SeparateAlphaBlendEnable, true);

				dev.SetRenderState(RenderState.BlendOperation, ConvertBlendOp(mybs.colorEquation));
				dev.SetRenderState(RenderState.SourceBlend, ConvertBlendArg(mybs.colorSource));
				dev.SetRenderState(RenderState.DestinationBlend, ConvertBlendArg(mybs.colorDest));

				dev.SetRenderState(RenderState.BlendOperationAlpha, ConvertBlendOp(mybs.alphaEquation));
				dev.SetRenderState(RenderState.SourceBlendAlpha, ConvertBlendArg(mybs.alphaSource));
				dev.SetRenderState(RenderState.DestinationBlendAlpha, ConvertBlendArg(mybs.alphaDest));
			}
			else dev.SetRenderState(RenderState.AlphaBlendEnable, false);
			if (rsBlend == _rsBlendNoneOpaque)
			{
				//make sure constant color is set correctly
				dev.SetRenderState(RenderState.BlendFactor, -1); //white
			}
		}

		void CreateRenderStates()
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

		CacheBlendState _rsBlendNoneVerbatim, _rsBlendNoneOpaque, _rsBlendNormal;

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
					throw new InvalidOperationException($"Couldn't build required GL pipeline:\r\n{errors}");
				var pipeline = new Pipeline(this, null, false, null, null, null) { Errors = errors };
				return pipeline;
			}

			VertexElement[] ves = new VertexElement[vertexLayout.Items.Count];
			int stride = 0;
			foreach (var kvp in vertexLayout.Items)
			{
				var item = kvp.Value;
				d3d9.DeclarationType decltype = DeclarationType.Float1;
				switch (item.AttribType)
				{
					case gl.VertexAttribPointerType.Float:
						if (item.Components == 1) decltype = DeclarationType.Float1;
						else if (item.Components == 2) decltype = DeclarationType.Float2;
						else if (item.Components == 3) decltype = DeclarationType.Float3;
						else if (item.Components == 4) decltype = DeclarationType.Float4;
						else throw new NotSupportedException();
						stride += 4 * item.Components;
						break;
					default:
						throw new NotSupportedException();
				}

				d3d9.DeclarationUsage usage = DeclarationUsage.Position;
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

				ves[kvp.Key] = new VertexElement(0, (short)item.Offset, decltype, DeclarationMethod.Default, usage, usageIndex);
			}


			var pw = new PipelineWrapper()
			{
				VertexDeclaration = new VertexDeclaration(dev, ves),
				VertexShader = vertexShader.Opaque as ShaderWrapper,
				FragmentShader = fragmentShader.Opaque as ShaderWrapper,
				VertexStride = stride,
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
				Queue<Tuple<string,EffectHandle>> todo = new Queue<Tuple<string,EffectHandle>>();
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
						string newprefix = $"{prefix}{descr.Name}.";
						for (int j = 0; j < descr.StructMembers; j++)
						{
							var subhandle = ct.GetConstant(handle, j);
							todo.Enqueue(Tuple.Create(newprefix, subhandle));
						}
						continue;
					}

					UniformInfo ui = new UniformInfo();
					UniformWrapper uw = new UniformWrapper();

					ui.Opaque = uw;
					string name = prefix + descr.Name;

					//mehhh not happy about this stuff
					if (fs.MapCodeToNative != null || vs.MapCodeToNative != null)
					{
						string key = name.TrimStart('$');
						if (descr.Rows != 1)
							key += "[0]";
						if (fs.MapCodeToNative != null && ct == fsct) if (fs.MapCodeToNative.ContainsKey(key)) name = fs.MapCodeToNative[key];
						if (vs.MapCodeToNative != null && ct == vsct) if (vs.MapCodeToNative.ContainsKey(key)) name = vs.MapCodeToNative[key];
					}
					
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

			//unavailable pipelines will have no opaque
			if (pw == null)
				return;

			pw.VertexDeclaration.Dispose();
			pw.FragmentShader.IGLShader.Release();
			pw.VertexShader.IGLShader.Release();
		}

		public void Internal_FreeShader(Shader shader)
		{
			var sw = shader.Opaque as ShaderWrapper;
			sw.bytecode.Dispose();
			if (sw.ps != null) sw.ps.Dispose();
			if (sw.vs != null) sw.vs.Dispose();
		}

		class UniformWrapper
		{
			public d3d9.EffectHandle EffectHandle;
			public d3d9.ConstantDescription Description;
			public bool FS;
			public d3d9.ConstantTable CT;
			public int SamplerIndex;
		}

		class PipelineWrapper // Disposable fields cleaned up by FreePipeline
		{
			public d3d9.VertexDeclaration VertexDeclaration;
			public ShaderWrapper VertexShader, FragmentShader;
			public int VertexStride;
			public d3d9.ConstantTable fsct, vsct;
		}

		class TextureWrapper
		{
			public d3d9.Texture Texture;
			public TextureAddress WrapClamp = TextureAddress.Clamp;
			public TextureFilter MinFilter = TextureFilter.Point, MagFilter = TextureFilter.Point;
		}

		public VertexLayout CreateVertexLayout() { return new VertexLayout(this, new IntPtr(0)); }

		public void BindPipeline(Pipeline pipeline)
		{
			_CurrPipeline = pipeline;

			if (pipeline == null)
			{
				//unbind? i dont know
				return;
			}

			var pw = pipeline.Opaque as PipelineWrapper;
			dev.PixelShader = pw.FragmentShader.ps;
			dev.VertexShader = pw.VertexShader.vs;
			dev.VertexDeclaration = pw.VertexDeclaration;
			
			//not helpful...
			//pw.vsct.SetDefaults(dev);
			//pw.fsct.SetDefaults(dev);
		}

		public void SetPipelineUniform(PipelineUniform uniform, bool value)
		{
			if (uniform.Owner == null) return; //uniform was optimized out

			foreach (var ui in uniform.UniformInfos)
			{
				var uw = ui.Opaque as UniformWrapper;
				uw.CT.SetValue(dev, uw.EffectHandle, value);
			}
		}

		public unsafe void SetPipelineUniformMatrix(PipelineUniform uniform, Matrix4 mat, bool transpose)
		{
			if (uniform.Owner == null) return; //uniform was optimized out

			foreach (var ui in uniform.UniformInfos)
			{
				var uw = ui.Opaque as UniformWrapper;
				uw.CT.SetValue(dev, uw.EffectHandle, mat.ToSlimDXMatrix(!transpose));
			}
		}

		public unsafe void SetPipelineUniformMatrix(PipelineUniform uniform, ref Matrix4 mat, bool transpose)
		{
			if (uniform.Owner == null) return; //uniform was optimized out

			foreach (var ui in uniform.UniformInfos)
			{
				var uw = ui.Opaque as UniformWrapper;
				uw.CT.SetValue(dev, uw.EffectHandle, mat.ToSlimDXMatrix(!transpose));
			}
		}

		public void SetPipelineUniform(PipelineUniform uniform, Vector4 value)
		{
			if (uniform.Owner == null) return; //uniform was optimized out

			foreach (var ui in uniform.UniformInfos)
			{
				var uw = ui.Opaque as UniformWrapper;
				uw.CT.SetValue(dev, uw.EffectHandle, value.ToSlimDXVector4());
			}
		}

		public void SetPipelineUniform(PipelineUniform uniform, Vector2 value)
		{
			if (uniform.Owner == null) return; //uniform was optimized out

			foreach (var ui in uniform.UniformInfos)
			{
				var uw = ui.Opaque as UniformWrapper;
				uw.CT.SetValue(dev, uw.EffectHandle, value.ToSlimDXVector2());
			}
		}

		public void SetPipelineUniform(PipelineUniform uniform, float value)
		{
			if (uniform.Owner == null) return; //uniform was optimized out
			foreach (var ui in uniform.UniformInfos)
			{
				var uw = ui.Opaque as UniformWrapper;
				uw.CT.SetValue(dev, uw.EffectHandle, value);
			}
		}

		public unsafe void SetPipelineUniform(PipelineUniform uniform, Vector4[] values)
		{
			if (uniform.Owner == null) return; //uniform was optimized out
			var v = new global::SlimDX.Vector4[values.Length];
			for (int i = 0; i < values.Length; i++)
				v[i] = values[i].ToSlimDXVector4();
			foreach (var ui in uniform.UniformInfos)
			{
				var uw = ui.Opaque as UniformWrapper;
				uw.CT.SetValue(dev, uw.EffectHandle, v);
			}
		}

		public void SetPipelineUniformSampler(PipelineUniform uniform, Texture2d tex)
		{
			if (uniform.Owner == null) return; //uniform was optimized out
			var tw = tex.Opaque as TextureWrapper;

			foreach (var ui in uniform.UniformInfos)
			{
				var uw = ui.Opaque as UniformWrapper;
				dev.SetTexture(uw.SamplerIndex, tw.Texture);

				dev.SetSamplerState(uw.SamplerIndex, SamplerState.AddressU, tw.WrapClamp);
				dev.SetSamplerState(uw.SamplerIndex, SamplerState.AddressV, tw.WrapClamp);
				dev.SetSamplerState(uw.SamplerIndex, SamplerState.MinFilter, tw.MinFilter);
				dev.SetSamplerState(uw.SamplerIndex, SamplerState.MagFilter, tw.MagFilter);
			}
		}

		public void SetTextureWrapMode(Texture2d tex, bool clamp)
		{
			var tw = tex.Opaque as TextureWrapper;
			tw.WrapClamp = clamp ? TextureAddress.Clamp : TextureAddress.Wrap;
		}

		public void TexParameter2d(Texture2d tex, gl.TextureParameterName pname, int param)
		{
			var tw = tex.Opaque as TextureWrapper;

			if(pname == gl.TextureParameterName.TextureMinFilter)
				tw.MinFilter = (param == (int)gl.TextureMinFilter.Linear) ? TextureFilter.Linear : TextureFilter.Point;
			if (pname == gl.TextureParameterName.TextureMagFilter)
				tw.MagFilter = (param == (int)gl.TextureMagFilter.Linear) ? TextureFilter.Linear : TextureFilter.Point;
		}

		public Texture2d LoadTexture(sd.Bitmap bitmap)
		{
			using var bmp = new BitmapBuffer(bitmap, new BitmapLoadOptions());
			return (this as IGL).LoadTexture(bmp);
		}

		public Texture2d LoadTexture(Stream stream)
		{
			using var bmp = new BitmapBuffer(stream, new BitmapLoadOptions());
			return (this as IGL).LoadTexture(bmp);
		}

		public Texture2d CreateTexture(int width, int height)
		{
			return null;
		}

		public Texture2d WrapGLTexture2d(IntPtr glTexId, int width, int height)
		{
			//not needed 1st pass (except for GL cores)
			//TODO - need to rip the texturedata. we had code for that somewhere...
			return null;
		}

		/// <exception cref="InvalidOperationException">GDI+ call returned unexpected data</exception>
		public unsafe void LoadTextureData(Texture2d tex, BitmapBuffer bmp)
		{
			sdi.BitmapData bmp_data = bmp.LockBits();
			var tw = tex.Opaque as TextureWrapper;
			var dr = tw.Texture.LockRectangle(0, LockFlags.None);

			//TODO - do we need to handle odd sizes, weird pitches here?
			if (bmp.Width * 4 != bmp_data.Stride)
				throw new InvalidOperationException();

			dr.Data.WriteRange(bmp_data.Scan0, bmp.Width * bmp.Height * 4);
			dr.Data.Close();

			tw.Texture.UnlockRectangle(0);
			bmp.UnlockBits(bmp_data);
		}


		public Texture2d LoadTexture(BitmapBuffer bmp)
		{
			var tex = new d3d9.Texture(dev, bmp.Width, bmp.Height, 1, d3d9.Usage.None, d3d9.Format.A8R8G8B8, d3d9.Pool.Managed);
			var tw = new TextureWrapper() { Texture = tex };
			var ret = new Texture2d(this, tw, bmp.Width, bmp.Height);
			LoadTextureData(ret, bmp);
			return ret;
		}

		/// <exception cref="InvalidOperationException">SlimDX call returned unexpected data</exception>
		public unsafe BitmapBuffer ResolveTexture2d(Texture2d tex)
		{
			//TODO - lazy create and cache resolving target in RT
			var target = new d3d9.Texture(dev, tex.IntWidth, tex.IntHeight, 1, d3d9.Usage.None, d3d9.Format.A8R8G8B8, d3d9.Pool.SystemMemory);
			var tw = tex.Opaque as TextureWrapper;
			dev.GetRenderTargetData(tw.Texture.GetSurfaceLevel(0), target.GetSurfaceLevel(0));
			var dr = target.LockRectangle(0, LockFlags.ReadOnly);
			if (dr.Pitch != tex.IntWidth * 4) throw new InvalidOperationException();
			int[] pixels = new int[tex.IntWidth * tex.IntHeight];
			dr.Data.ReadRange(pixels, 0, tex.IntWidth * tex.IntHeight);
			var bb = new BitmapBuffer(tex.IntWidth, tex.IntHeight, pixels);
			target.UnlockRectangle(0);
			target.Dispose(); //buffer churn warning
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
			return CreateGuiProjectionMatrix(new sd.Size(w, h));
		}

		public Matrix4 CreateGuiViewMatrix(int w, int h, bool autoflip)
		{
			return CreateGuiViewMatrix(new sd.Size(w, h), autoflip);
		}

		public Matrix4 CreateGuiProjectionMatrix(sd.Size dims)
		{
			Matrix4 ret = Matrix4.Identity;
			ret.M11 = 2.0f / (float)dims.Width;
			ret.M22 = 2.0f / (float)dims.Height;
			return ret;
		}

		public Matrix4 CreateGuiViewMatrix(sd.Size dims, bool autoflip)
		{
			Matrix4 ret = Matrix4.Identity;
			ret.M22 = -1.0f;
			ret.M41 = -(float)dims.Width * 0.5f - 0.5f;
			ret.M42 = (float)dims.Height * 0.5f + 0.5f;
			//autoflipping isnt needed on d3d
			return ret;
		}

		public void SetViewport(int x, int y, int width, int height)
		{
			dev.Viewport = new Viewport(x, y, width, height);
			dev.ScissorRect = new Rectangle(x, y, width, height);
		}

		public void SetViewport(int width, int height)
		{
			SetViewport(0, 0, width, height);
		}

		public void SetViewport(sd.Size size)
		{
			SetViewport(size.Width, size.Height);
		}

		public void SetViewport(swf.Control control)
		{
			var r = control.ClientRectangle;
			SetViewport(r.Left, r.Top, r.Width, r.Height);
		}

		public void BeginControl(GLControlWrapper_SlimDX9 control)
		{
			_CurrentControl = control;

			//this dispose isnt strictly needed but it seems benign
			var surface = _CurrentControl.SwapChain.GetBackBuffer(0);
			dev.SetRenderTarget(0, surface);
			surface.Dispose();
		}

		/// <exception cref="InvalidOperationException"><paramref name="control"/> does not match control passed to <see cref="BeginControl"/></exception>
		public void EndControl(GLControlWrapper_SlimDX9 control)
		{
			if (control != _CurrentControl)
				throw new InvalidOperationException();

			var surface = _CurrentControl.SwapChain.GetBackBuffer(0);
			dev.SetRenderTarget(0, surface);
			surface.Dispose();

			_CurrentControl = null;
		}

		public void SwapControl(GLControlWrapper_SlimDX9 control)
		{
			EndControl(control);

			try
			{
				var result = control.SwapChain.Present(Present.None);
				//var rs = dev.GetRasterStatus(0);
			}
			catch(d3d9.Direct3D9Exception ex)
			{
				if (ex.ResultCode.Code == D3DERR_DEVICELOST)
					ResetDevice(control);
			}
		}

		HashSet<RenderTarget> _renderTargets = new HashSet<RenderTarget>();

		public void FreeRenderTarget(RenderTarget rt)
		{
			var tw = rt.Texture2d.Opaque as TextureWrapper;
			tw.Texture.Dispose();
			tw.Texture = null;
			_renderTargets.Remove(rt);
		}

		public RenderTarget CreateRenderTarget(int w, int h)
		{
			var tw = new TextureWrapper() { Texture = CreateRenderTargetTexture(w, h) };
			var tex = new Texture2d(this, tw, w, h);
			var rt = new RenderTarget(this, tw, tex);
			_renderTargets.Add(rt);
			return rt;
		}

		d3d9.Texture CreateRenderTargetTexture(int w, int h)
		{
			return new d3d9.Texture(dev, w, h, 1, d3d9.Usage.RenderTarget, d3d9.Format.A8R8G8B8, d3d9.Pool.Default);
		}

		void SuspendRenderTargets()
		{
			foreach (var rt in _renderTargets)
			{
				var tw = rt.Opaque as TextureWrapper;
				tw.Texture.Dispose();
				tw.Texture = null;
			}
		}

		void ResumeRenderTargets()
		{
			foreach (var rt in _renderTargets)
			{
				var tw = rt.Opaque as TextureWrapper;
				tw.Texture = CreateRenderTargetTexture(rt.Texture2d.IntWidth, rt.Texture2d.IntHeight);
			}
		}

		public void BindRenderTarget(RenderTarget rt)
		{
			if (rt == null)
			{
				//this dispose is needed for correct device resets, I have no idea why
				//don't try caching it either
				var surface = _CurrentControl.SwapChain.GetBackBuffer(0);
				dev.SetRenderTarget(0, surface);
				surface.Dispose();

				dev.DepthStencilSurface = null;
				return;
			}

			//dispose doesn't seem necessary for reset here...
			var tw = rt.Opaque as TextureWrapper;
			dev.SetRenderTarget(0, tw.Texture.GetSurfaceLevel(0));
			dev.DepthStencilSurface = null;
		}

		public void FreeControlSwapChain(GLControlWrapper_SlimDX9 control)
		{
			if (control.SwapChain != null)
			{
				control.SwapChain.Dispose();
				control.SwapChain = null;
			}
		}

		public void RefreshControlSwapChain(GLControlWrapper_SlimDX9 control)
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

			control.SwapChain = new SwapChain(dev, pp);
		}

		public IGraphicsControl Internal_CreateGraphicsControl()
		{
			var ret = new GLControlWrapper_SlimDX9(this);
			RefreshControlSwapChain(ret);
			return ret;
		}

		/// <exception cref="NotSupportedException"><paramref name="mode"/> is <see cref="gl.PrimitiveType.TriangleStrip"/></exception>
		public unsafe void DrawArrays(gl.PrimitiveType mode, int first, int count)
		{
			PrimitiveType pt = PrimitiveType.TriangleStrip;
			
			if(mode != gl.PrimitiveType.TriangleStrip)
				throw new NotSupportedException();

			//for tristrip
			int primCount = (count - 2);

			var pw = _CurrPipeline.Opaque as PipelineWrapper;
			int stride = pw.VertexStride;
			byte* ptr = (byte*)_pVertexData.ToPointer() + first * stride;

			dev.DrawUserPrimitives(pt, primCount, (void*)ptr, (uint)stride);
		}

		
		public unsafe void BindArrayData(void* pData)
		{
			_pVertexData = new IntPtr(pData);
		}

		public void BeginScene()
		{
			dev.BeginScene();
			dev.SetRenderState(RenderState.CullMode, Cull.None);
			dev.SetRenderState(RenderState.ZEnable, false);
			dev.SetRenderState(RenderState.ZWriteEnable, false);
			dev.SetRenderState(RenderState.Lighting, false);
		}

		public void EndScene()
		{
			dev.EndScene();
		}
	} //class IGL_SlimDX
}
