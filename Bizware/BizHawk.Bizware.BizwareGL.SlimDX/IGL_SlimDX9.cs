using System;
using System.Linq;
using System.Text;
using System.IO;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;

using swf = System.Windows.Forms;
using sd = System.Drawing;
using sdi = System.Drawing.Imaging;

using BizHawk.Bizware.BizwareGL;

using SlimDX.Direct3D9;
using d3d9=SlimDX.Direct3D9;
using OpenTK;
using OpenTK.Graphics;
using gl=OpenTK.Graphics.OpenGL;

//todo - do a better job selecting shader model? base on caps somehow? try several and catch compilation exceptions (yuck, exceptions)

namespace BizHawk.Bizware.BizwareGL.Drivers.SlimDX
{

	public class IGL_SlimDX9 : IGL
	{
		static Direct3D d3d;
		internal Device dev;
		INativeWindow OffscreenNativeWindow;

		//rendering state
		IntPtr _pVertexData;
		RenderTarget _CurrRenderTarget;
		Pipeline _CurrPipeline;
		GLControlWrapper_SlimDX9 _CurrentControl;

		public string API { get { return "D3D9"; } }

		public IGL_SlimDX9()
		{
			if (d3d == null)
			{
				d3d = new Direct3D();
			}

			//make an 'offscreen context' so we can at least do things without having to create a window
			OffscreenNativeWindow = new OpenTK.NativeWindow();
			OffscreenNativeWindow.ClientSize = new sd.Size(8, 8);

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

		void ResetDevice()
		{
			devBB.Dispose();
			ResetHandlers.Reset();
			for (; ; )
			{
				var result = dev.TestCooperativeLevel();
				if (result.IsSuccess)
					break;
				if (result.Code == -2005530519) // D3DERR_DEVICENOTRESET
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
			ResetHandlers.Restore();
			devBB = dev.GetBackBuffer(0, 0);
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
			devBB = dev.GetBackBuffer(0, 0);
		}

		void IDisposable.Dispose()
		{
			ResetHandlers.Reset();
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

		class ShaderWrapper : IDisposable
		{
			public d3d9.ShaderBytecode bytecode;
			public d3d9.VertexShader vs;
			public d3d9.PixelShader ps;
			public Shader IGLShader;
			public Dictionary<string, string> MapCodeToNative;
			public Dictionary<string, string> MapNativeToCode;

			public void Dispose()
			{
				vs.Dispose();
				vs = null;
				ps.Dispose();
				bytecode.Dispose();
				bytecode = null;
			}
		}

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
					throw new InvalidOperationException("Error compiling shader: " + errors, ex);
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
				var s = new Shader(this, null, false);
				s.Errors = ex.ToString();
				return s;
			}
		}

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
					string profile = "vs_1_1";
					if (cg)
						profile = "vs_3_0"; //todo - smarter logic somehow

					bytecode = d3d9.ShaderBytecode.Compile(source, null, null, entry, profile, ShaderFlags.EnableBackwardsCompatibility, out errors);
				}
				catch (Exception ex)
				{
					throw new InvalidOperationException("Error compiling shader: " + errors, ex);
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
				var s = new Shader(this, null, false);
				s.Errors = ex.ToString();
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

		Blend ConvertBlendArg(gl.BlendingFactorSrc glmode)
		{
			if(glmode == gl.BlendingFactorSrc.Zero) return Blend.Zero;
			if(glmode == gl.BlendingFactorSrc.One) return Blend.One;
			if(glmode == gl.BlendingFactorSrc.SrcColor) return Blend.SourceColor;
			if(glmode == gl.BlendingFactorSrc.OneMinusSrcColor) return Blend.InverseSourceColor;
			if(glmode == gl.BlendingFactorSrc.SrcAlpha) return Blend.SourceAlpha;
			if(glmode == gl.BlendingFactorSrc.OneMinusSrcAlpha) return Blend.InverseSourceAlpha;
			if(glmode == gl.BlendingFactorSrc.DstAlpha) return Blend.DestinationAlpha;
			if(glmode == gl.BlendingFactorSrc.OneMinusDstAlpha) return Blend.InverseDestinationAlpha;
			if(glmode == gl.BlendingFactorSrc.DstColor) return Blend.DestinationColor;
			if(glmode == gl.BlendingFactorSrc.OneMinusDstColor) return Blend.InverseDestinationColor;
			if(glmode == gl.BlendingFactorSrc.SrcAlphaSaturate) return Blend.SourceAlphaSaturated;
			if(glmode == gl.BlendingFactorSrc.ConstantColorExt) throw new NotSupportedException();
			if(glmode == gl.BlendingFactorSrc.ConstantColor) return Blend.BlendFactor;
			if(glmode == gl.BlendingFactorSrc.OneMinusConstantColor) return Blend.InverseBlendFactor;
			if(glmode == gl.BlendingFactorSrc.OneMinusConstantColorExt) throw new NotSupportedException();
			if(glmode == gl.BlendingFactorSrc.ConstantAlpha) throw new NotSupportedException();
			if(glmode == gl.BlendingFactorSrc.ConstantAlphaExt) throw new NotSupportedException();
			if(glmode == gl.BlendingFactorSrc.OneMinusConstantAlphaExt) throw new NotSupportedException();
			if(glmode == gl.BlendingFactorSrc.OneMinusConstantAlpha) throw new NotSupportedException();
			if(glmode == gl.BlendingFactorSrc.Src1Alpha) throw new NotSupportedException();
			if(glmode == gl.BlendingFactorSrc.Src1Color) throw new NotSupportedException();
			if(glmode == gl.BlendingFactorSrc.OneMinusSrc1Color) throw new NotSupportedException();
			if (glmode == gl.BlendingFactorSrc.OneMinusSrc1Alpha) throw new NotSupportedException();
			throw new ArgumentOutOfRangeException();
		}

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

		public IBlendState BlendNoneCopy { get { return _rsBlendNoneVerbatim; } }
		public IBlendState BlendNoneOpaque { get { return _rsBlendNoneOpaque; } }
		public IBlendState BlendNormal { get { return _rsBlendNormal; } }

		public Pipeline CreatePipeline(VertexLayout vertexLayout, Shader vertexShader, Shader fragmentShader, bool required, string memo)
		{
			if (!vertexShader.Available || !fragmentShader.Available)
			{
				string errors = string.Format("Vertex Shader:\r\n {0} \r\n-------\r\nFragment Shader:\r\n{1}", vertexShader.Errors, fragmentShader.Errors);
				if (required)
					throw new InvalidOperationException("Couldn't build required GL pipeline:\r\n" + errors);
				var pipeline = new Pipeline(this, null, false, null, null, null);
				pipeline.Errors = errors;
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

					//Console.WriteLine("D3D UNIFORM: " + descr.Name);

					if (descr.StructMembers != 0)
					{
						string newprefix = prefix + descr.Name + ".";
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
							key = key + "[0]";
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

		class PipelineWrapper
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
			using (var bmp = new BitmapBuffer(bitmap, new BitmapLoadOptions()))
				return (this as IGL).LoadTexture(bmp);
		}

		public Texture2d LoadTexture(Stream stream)
		{
			using (var bmp = new BitmapBuffer(stream, new BitmapLoadOptions()))
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
		Surface devBB;
		public void EndControl(GLControlWrapper_SlimDX9 control)
		{
			if (control != _CurrentControl)
				throw new InvalidOperationException();

			dev.SetRenderTarget(0, devBB);

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
				if (ex.ResultCode.Name == "D3DERR_DEVICELOST")
					ResetDevice();
			}
		}

		
		public void FreeRenderTarget(RenderTarget rt)
		{
			//not needed 1st pass ??
			//int id = rt.Id.ToInt32();
			//var rtw = ResourceIDs.Lookup[id] as RenderTargetWrapper;
			//rtw.Target.Dispose();
			//ResourceIDs.Free(rt.Id);
		}

		public RenderTarget CreateRenderTarget(int w, int h)
		{
			var d3dtex = new d3d9.Texture(dev, w, h, 1, d3d9.Usage.RenderTarget, d3d9.Format.A8R8G8B8, d3d9.Pool.Default);
			var tw = new TextureWrapper() { Texture = d3dtex };
			var tex = new Texture2d(this, tw, w, h);
			RenderTarget rt = new RenderTarget(this, tw, tex);

			ResetHandlers.Add(rt, "RenderTarget", () => ResetRenderTarget(rt), () => RestoreRenderTarget(rt));
			return rt;
		}

		void ResetRenderTarget(RenderTarget rt)
		{
			var tw = rt.Texture2d.Opaque as TextureWrapper;
			tw.Texture.Dispose();
			tw.Texture = null;
		}

		void RestoreRenderTarget(RenderTarget rt)
		{
			var tw = rt.Texture2d.Opaque as TextureWrapper;
			int w = rt.Texture2d.IntWidth;
			int h = rt.Texture2d.IntHeight;
			var d3dtex = new d3d9.Texture(dev, w, h, 1, d3d9.Usage.RenderTarget, d3d9.Format.A8R8G8B8, d3d9.Pool.Default);
			tw.Texture = d3dtex;
			//i know it's weird, we have to re-add ourselves to the list
			//bad design..
			ResetHandlers.Add(rt, "RenderTarget", () => ResetRenderTarget(rt), () => RestoreRenderTarget(rt));
		}

		public void BindRenderTarget(RenderTarget rt)
		{
			_CurrRenderTarget = rt;

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

		public void RefreshControlSwapChain(GLControlWrapper_SlimDX9 control)
		{
			if (control.SwapChain != null)
			{
				control.SwapChain.Dispose();
				control.SwapChain = null;
			}
			ResetHandlers.Remove(control, "SwapChain");
			
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
			ResetHandlers.Add(control, "SwapChain", () =>
				{
					control.SwapChain.Dispose(); 
					control.SwapChain = null;
				},
				() => RefreshControlSwapChain(control));
		}

		DeviceLostHandler ResetHandlers = new DeviceLostHandler();

		class DeviceLostHandler
		{
			class ResetHandlerKey
			{
				public string Label;
				public object Object;
				public override int GetHashCode()
				{
					return Label.GetHashCode() ^ Object.GetHashCode();
				}
				public override bool Equals(object obj)
				{
					if (obj == null) return false;
					var key = obj as ResetHandlerKey;
					return key.Label == Label && key.Object == Object;
				}
			}

			class HandlerSet
			{
				public Action Reset, Restore;
			}

			Dictionary<ResetHandlerKey, HandlerSet> Handlers = new Dictionary<ResetHandlerKey, HandlerSet>();

			public void Add(object o, string label, Action reset, Action restore)
			{
				ResetHandlerKey hkey = new ResetHandlerKey() { Object = o, Label = label };
				Handlers[hkey] = new HandlerSet { Reset = reset, Restore = restore };
			}

			public void Remove(object o, string label)
			{
				ResetHandlerKey hkey = new ResetHandlerKey() { Object = o, Label = label };
				if(Handlers.ContainsKey(hkey))
					Handlers.Remove(hkey);
			}

			public void Reset()
			{
				foreach (var handler in Handlers)
					handler.Value.Reset();
			}

			public void Restore()
			{
				var todo = Handlers.ToArray();
				Handlers.Clear();
				foreach (var item in todo)
					item.Value.Restore();
			}
		}



		public IGraphicsControl Internal_CreateGraphicsControl()
		{
			var ret = new GLControlWrapper_SlimDX9(this);
			RefreshControlSwapChain(ret);
			return ret;
		}

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
