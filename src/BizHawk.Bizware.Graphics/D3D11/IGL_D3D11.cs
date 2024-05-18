using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Numerics;

using BizHawk.Common;
using BizHawk.Common.StringExtensions;

using Vortice.D3DCompiler;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.Direct3D11.Shader;
using Vortice.DXGI;

namespace BizHawk.Bizware.Graphics
{
	/// <summary>
	/// Direct3D11 implementation of the BizwareGL.IGL interface
	/// </summary>
	public sealed class IGL_D3D11 : IGL
	{
		public EDispMethod DispMethodEnum => EDispMethod.D3D11;

		// D3D11 resources
		// these might need to be thrown out and recreated if the device is lost
		private readonly D3D11Resources _resources;

		private IDXGIFactory1 Factory1 => _resources.Factory1;
		private IDXGIFactory2 Factory2 => _resources.Factory2;
		private ID3D11Device Device => _resources.Device;
		private ID3D11DeviceContext Context => _resources.Context;
		private ID3D11BlendState BlendEnableState => _resources.BlendEnableState;
		private ID3D11BlendState BlendDisableState => _resources.BlendDisableState;
		private ID3D11SamplerState PointSamplerState => _resources.PointSamplerState;
		private ID3D11SamplerState LinearSamplerState => _resources.LinearSamplerState;
		private ID3D11RasterizerState RasterizerState => _resources.RasterizerState;

		private FeatureLevel DeviceFeatureLevel => _resources.DeviceFeatureLevel;
		private D3D11RenderTarget CurRenderTarget => _resources.CurRenderTarget;

		// rendering state
		private Pipeline _curPipeline;
		private D3D11SwapChain.SwapChainResources _controlSwapChain;

		public IGL_D3D11()
		{
			if (OSTailoredCode.IsUnixHost)
			{
				throw new NotSupportedException("D3D11 is Windows only");
			}

			_resources = new();
			_resources.CreateResources();
		}

		private IDXGISwapChain CreateDXGISwapChain(D3D11SwapChain.ControlParameters cp)
		{
			IDXGISwapChain ret;

			if (Factory2 is null)
			{
				// no Factory2, probably on Windows 7 without the Platform Update
				// we can assume a simple legacy format is needed here
				var sd = default(SwapChainDescription);
				sd.BufferDescription = new(
					width: cp.Width,
					height: cp.Height,
					refreshRate: new(0, 0),
					format: Format.B8G8R8A8_UNorm);
				sd.SampleDescription = SampleDescription.Default;
				sd.BufferUsage = Usage.RenderTargetOutput;
				sd.BufferCount = 2;
				sd.OutputWindow = cp.Handle;
				sd.Windowed = true;
				sd.SwapEffect = SwapEffect.Discard;
				sd.Flags = SwapChainFlags.None;

				ret = Factory1.CreateSwapChain(Device, sd);
			}
			else
			{
				// this is the optimal swapchain model
				// note however it requires windows 10+
				// a less optimal model will end up being used in case this fails
				var sd = new SwapChainDescription1(
					width: cp.Width,
					height: cp.Height,
					format: Format.B8G8R8A8_UNorm,
					stereo: false,
					swapEffect: SwapEffect.FlipDiscard,
					bufferUsage: Usage.RenderTargetOutput,
					bufferCount: 2,
					scaling: Scaling.Stretch,
					alphaMode: AlphaMode.Ignore,
					flags: SwapChainFlags.AllowTearing);

				try
				{
					ret = Factory2.CreateSwapChainForHwnd(Device, cp.Handle, sd);
				}
				catch
				{
					sd.SwapEffect = SwapEffect.Discard;
					sd.Flags = SwapChainFlags.None;
					ret = Factory2.CreateSwapChainForHwnd(Device, cp.Handle, sd);
				}
			}

			// don't allow DXGI to snoop alt+enter and such
			using var parentFactory = ret.GetParent<IDXGIFactory>();
			parentFactory.MakeWindowAssociation(cp.Handle, WindowAssociationFlags.IgnoreAll);
			return ret;
		}

		private void ResetDevice(D3D11SwapChain.ControlParameters cp)
		{
			_controlSwapChain.Dispose();
			Context.Flush(); // important to properly dispose of the swapchain

			foreach (var pipeline in _resources.Pipelines)
			{
				var pw = (PipelineWrapper)pipeline.Opaque;

				for (var i = 0; i < ID3D11DeviceContext.CommonShaderConstantBufferSlotCount; i++)
				{
					pw.VSConstantBuffers[i]?.Dispose();
					pw.VSConstantBuffers[i] = null;
					pw.PSConstantBuffers[i]?.Dispose();
					pw.PSConstantBuffers[i] = null;
				}

				var vlw = (VertexLayoutWrapper)pipeline.VertexLayout.Opaque;
				vlw.VertexInputLayout.Dispose();
				vlw.VertexInputLayout = null;
				vlw.VertexBuffer.Dispose();
				vlw.VertexBuffer = null;
				vlw.VertexBufferCount = 0;
			}

			foreach (var sw in _resources.VertexShaders.Select(vertexShader => (ShaderWrapper)vertexShader.Opaque))
			{
				sw.VS.Dispose();
				sw.VS = null;
			}

			foreach (var sw in _resources.PixelShaders.Select(pixelShader => (ShaderWrapper)pixelShader.Opaque))
			{
				sw.PS.Dispose();
				sw.PS = null;
			}

			_resources.DestroyResources();
			_resources.CreateResources();

			foreach (var sw in _resources.VertexShaders.Select(vertexShader => (ShaderWrapper)vertexShader.Opaque))
			{
				sw.VS = Device.CreateVertexShader(sw.Bytecode.Span);
			}

			foreach (var sw in _resources.PixelShaders.Select(pixelShader => (ShaderWrapper)pixelShader.Opaque))
			{
				sw.PS = Device.CreatePixelShader(sw.Bytecode.Span);
			}

			foreach (var pipeline in _resources.Pipelines)
			{
				var pw = (PipelineWrapper)pipeline.Opaque;
				for (var i = 0; i < ID3D11DeviceContext.CommonShaderConstantBufferSlotCount; i++)
				{
					var cbw = pw.PendingBuffers[i];
					if (cbw == null)
					{
						break;
					}

					if (cbw.VSBufferSize > 0)
					{
						var bd = new BufferDescription(cbw.VSBufferSize, BindFlags.ConstantBuffer, ResourceUsage.Dynamic, CpuAccessFlags.Write);
						pw.VSConstantBuffers[i] = Device.CreateBuffer(bd);
						cbw.VSBufferDirty = true;
					}

					if (cbw.PSBufferSize > 0)
					{
						var bd = new BufferDescription(cbw.PSBufferSize, BindFlags.ConstantBuffer, ResourceUsage.Dynamic, CpuAccessFlags.Write);
						pw.PSConstantBuffers[i] = Device.CreateBuffer(bd);
						cbw.PSBufferDirty = true;
					}
				}

				CreateInputLayout(pipeline.VertexLayout, pw.VertexShader);
			}

			var swapChain = CreateDXGISwapChain(cp);
			var bbTex = swapChain.GetBuffer<ID3D11Texture2D>(0);
			var bbRtvd = new RenderTargetViewDescription(RenderTargetViewDimension.Texture2D, Format.B8G8R8A8_UNorm);
			var rtv = Device.CreateRenderTargetView(bbTex, bbRtvd);

			_controlSwapChain.Device = Device;
			_controlSwapChain.Context = Context;
			_controlSwapChain.Context1 = Context.QueryInterfaceOrNull<ID3D11DeviceContext1>();
			_controlSwapChain.BackBufferTexture = bbTex;
			_controlSwapChain.RTV = rtv;
			_controlSwapChain.SwapChain = swapChain;
			_controlSwapChain.AllowsTearing = (swapChain.Description.Flags & SwapChainFlags.AllowTearing) != 0;
		}

		public D3D11SwapChain CreateSwapChain(D3D11SwapChain.ControlParameters cp)
		{
			if (_controlSwapChain != null)
			{
				throw new InvalidOperationException($"{nameof(IGL_D3D11)} can only have 1 control swap chain");
			}

			var swapChain = CreateDXGISwapChain(cp);
			var bbTex = swapChain.GetBuffer<ID3D11Texture2D>(0);
			var rtvd = new RenderTargetViewDescription(RenderTargetViewDimension.Texture2D, Format.B8G8R8A8_UNorm);
			var rtv = Device.CreateRenderTargetView(bbTex, rtvd);

			_controlSwapChain = new()
			{
				Device = Device,
				Context = Context,
				Context1 = Context.QueryInterfaceOrNull<ID3D11DeviceContext1>(),
				BackBufferTexture = bbTex,
				RTV = rtv,
				SwapChain = swapChain,
				AllowsTearing = (swapChain.Description.Flags & SwapChainFlags.AllowTearing) != 0,
			};

			return new(_controlSwapChain, ResetDevice);
		}

		public void Dispose()
		{
			_controlSwapChain.Dispose();
			Context.Flush();
			_resources.Dispose();
		}

		public void ClearColor(Color color)
			=> Context.ClearRenderTargetView(CurRenderTarget?.RTV ?? _controlSwapChain.RTV, new(color.R, color.B, color.G, color.A));

		private class ShaderWrapper // Disposable fields cleaned up by Internal_FreeShader
		{
			public ReadOnlyMemory<byte> Bytecode;
			public ID3D11ShaderReflection Reflection;
			public ID3D11VertexShader VS;
			public ID3D11PixelShader PS;
			public Shader IGLShader;
		}

		/// <exception cref="InvalidOperationException"><paramref name="required"/> is <see langword="true"/> and compilation error occurred</exception>
		public Shader CreateVertexShader(string source, string entry, bool required)
		{
			try
			{
				var sw = new ShaderWrapper();

				var profile = DeviceFeatureLevel switch
				{
					FeatureLevel.Level_9_1 or FeatureLevel.Level_9_2 => "vs_4_0_level_9_1",
					FeatureLevel.Level_9_3 => "vs_4_0_level_9_3",
					_ => "vs_4_0",
				};

				// note: we use D3D9-like shaders for legacy reasons, so we need the backwards compat flag
				// TODO: remove D3D9 syntax from shaders
				var result = Compiler.Compile(
					shaderSource: source,
					entryPoint: entry,
					sourceName: null!, // this is safe to be null
					profile: profile,
					shaderFlags: ShaderFlags.OptimizationLevel3 | ShaderFlags.EnableBackwardsCompatibility);

				sw.VS = Device.CreateVertexShader(result.Span);
				sw.Reflection = Compiler.Reflect<ID3D11ShaderReflection>(result.Span);
				sw.Bytecode = result;

				var s = new Shader(this, sw, true);
				sw.IGLShader = s;
				_resources.VertexShaders.Add(s);

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
		public Shader CreateFragmentShader(string source, string entry, bool required)
		{
			try
			{
				var sw = new ShaderWrapper();

				var profile = DeviceFeatureLevel switch
				{
					FeatureLevel.Level_9_1 or FeatureLevel.Level_9_2 => "ps_4_0_level_9_1",
					FeatureLevel.Level_9_3 => "ps_4_0_level_9_3",
					_ => "ps_4_0",
				};

				// note: we use D3D9-like shaders for legacy reasons, so we need the backwards compat flag
				// TODO: remove D3D9 syntax from shaders
				var result = Compiler.Compile(
					shaderSource: source,
					entryPoint: entry,
					sourceName: null!, // this is safe to be null
					profile: profile,
					shaderFlags: ShaderFlags.OptimizationLevel3 | ShaderFlags.EnableBackwardsCompatibility);

				sw.PS = Device.CreatePixelShader(result.Span);
				sw.Reflection = Compiler.Reflect<ID3D11ShaderReflection>(result.Span);
				sw.Bytecode = result;

				var s = new Shader(this, sw, true);
				sw.IGLShader = s;
				_resources.PixelShaders.Add(s);

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

		public void EnableBlending()
			=> Context.OMSetBlendState(BlendEnableState);

		public void DisableBlending()
			=> Context.OMSetBlendState(BlendDisableState);

		private void CreateInputLayout(VertexLayout vertexLayout, ShaderWrapper vertexShader)
		{
			var ves = new InputElementDescription[vertexLayout.Items.Count];
			var stride = 0;
			foreach (var (i, item) in vertexLayout.Items)
			{
				if (item.AttribType != VertexAttribPointerType.Float)
				{
					throw new NotSupportedException();
				}

				var semanticName = item.Usage switch
				{
					AttribUsage.Position => "POSITION",
					AttribUsage.Color0 => "COLOR",
					AttribUsage.Texcoord0 or AttribUsage.Texcoord1 => "TEXCOORD",
					_ => throw new InvalidOperationException()
				};

				var format = item.Components switch
				{
					1 => Format.R32_Float,
					2 => Format.R32G32_Float,
					3 => Format.R32G32B32_Float,
					4 => Format.R32G32B32A32_Float,
					_ => throw new InvalidOperationException()
				};

				ves[i] = new(semanticName, item.Usage == AttribUsage.Texcoord1 ? 1 : 0, format, item.Offset, 0);
				stride += 4 * item.Components;
			}

			var vlw = (VertexLayoutWrapper)vertexLayout.Opaque;
			var bc = vertexShader.Bytecode.Span;
			vlw.VertexInputLayout = Device.CreateInputLayout(ves, bc);
			vlw.VertexStride = stride;
		}

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
					throw new InvalidOperationException($"Couldn't build required D3D11 pipeline:\r\n{errors}");
				}

				return new(this, null, false, null, null, null) { Errors = errors };
			}

			var pw = new PipelineWrapper
			{
				VertexShader = (ShaderWrapper)vertexShader.Opaque,
				FragmentShader = (ShaderWrapper)fragmentShader.Opaque,
			};

			CreateInputLayout(vertexLayout, pw.VertexShader);

			// scan uniforms from reflection
			var uniforms = new List<UniformInfo>();
			var vsrefl = pw.VertexShader.Reflection;
			var psrefl = pw.FragmentShader.Reflection;
			foreach (var refl in new[] { vsrefl, psrefl })
			{
				var isVs = refl == vsrefl;
				var reflCbs = refl.ConstantBuffers;
				var todo = new Queue<(string, PendingBufferWrapper, string, int, ID3D11ShaderReflectionType)>();
				for (var i = 0; i < reflCbs.Length; i++)
				{
					var cbDesc = reflCbs[i].Description;
					var bd = new BufferDescription((cbDesc.Size + 15) & ~15, BindFlags.ConstantBuffer, ResourceUsage.Dynamic, CpuAccessFlags.Write);
					var constantBuffer = Device.CreateBuffer(bd);
					var pendingBuffer = Marshal.AllocCoTaskMem(cbDesc.Size);
					pw.PendingBuffers[i] ??= new();
					if (isVs)
					{
						pw.VSConstantBuffers[i] = constantBuffer;
						pw.PendingBuffers[i].VSPendingBuffer = pendingBuffer;
						pw.PendingBuffers[i].VSBufferSize = cbDesc.Size;
					}
					else
					{
						pw.PSConstantBuffers[i] = constantBuffer;
						pw.PendingBuffers[i].PSPendingBuffer = pendingBuffer;
						pw.PendingBuffers[i].PSBufferSize = cbDesc.Size;
					}

					var prefix = cbDesc.Name.RemovePrefix('$');
					prefix = prefix is "Params" or "Globals" ? "" : $"{prefix}.";
					foreach (var reflVari in reflCbs[i].Variables)
					{
						var reflVariDesc = reflVari.Description;
						todo.Enqueue((prefix, pw.PendingBuffers[i], reflVariDesc.Name, reflVariDesc.StartOffset, reflVari.VariableType));
					}
				}

				while (todo.Count != 0)
				{
					var (prefix, pbw, reflVariName, reflVariOffset, reflVariType) = todo.Dequeue();
					var reflVariTypeDesc = reflVariType.Description;
					if (reflVariTypeDesc.MemberCount > 0)
					{
						prefix = $"{prefix}{reflVariName}.";
						for (var i = 0; i < reflVariTypeDesc.MemberCount; i++)
						{
							var memberName = reflVariType.GetMemberTypeName(i);
							var memberType = reflVariType.GetMemberTypeByIndex(i);
							var memberOffset = memberType.Description.Offset;
							todo.Enqueue((prefix, pbw, memberName, reflVariOffset + memberOffset, memberType));
						}

						continue;
					}

					if (reflVariTypeDesc.Type is not (ShaderVariableType.Bool or ShaderVariableType.Float))
					{
						// unsupported type
						continue;
					}

					var reflVariSize = 4 * reflVariTypeDesc.ColumnCount * reflVariTypeDesc.RowCount;
					uniforms.Add(new()
					{
						Name = $"{prefix}{reflVariName}",
						Opaque = new UniformWrapper
						{
							PBW = pbw,
							VariableStartOffset = reflVariOffset,
							VariableSize = reflVariSize,
							VS = isVs
						}
					});
				}

				uniforms.AddRange(refl.BoundResources
					.Where(resource => resource.Type == ShaderInputType.Sampler)
					.Select(resource => new UniformInfo
					{
						IsSampler = true,
						Name = resource.Name.RemovePrefix('$'),
						Opaque = new UniformWrapper { VS = isVs },
						SamplerIndex = resource.BindPoint
					}));
			}

			var ret = new Pipeline(this, pw, true, vertexLayout, uniforms, memo);
			_resources.Pipelines.Add(ret);
			return ret;
		}

		public void FreePipeline(Pipeline pipeline)
		{
			// unavailable pipelines will have no opaque
			if (pipeline.Opaque is not PipelineWrapper pw)
			{
				return;
			}

			for (var i = 0; i < ID3D11DeviceContext.CommonShaderConstantBufferSlotCount; i++)
			{
				if (pw.PendingBuffers[i] != null)
				{
					Marshal.FreeCoTaskMem(pw.PendingBuffers[i].VSPendingBuffer);
					Marshal.FreeCoTaskMem(pw.PendingBuffers[i].PSPendingBuffer);
					pw.PendingBuffers[i] = null;
				}

				pw.VSConstantBuffers[i]?.Dispose();
				pw.VSConstantBuffers[i] = null;
				pw.PSConstantBuffers[i]?.Dispose();
				pw.PSConstantBuffers[i] = null;
			}

			pw.VertexShader.IGLShader.Release();
			pw.FragmentShader.IGLShader.Release();

			_resources.Pipelines.Remove(pipeline);
		}

		public void Internal_FreeShader(Shader shader)
		{
			var sw = (ShaderWrapper)shader.Opaque;
			sw.Reflection?.Dispose();
			sw.Reflection = null;

			if (sw.VS != null)
			{
				sw.VS.Dispose();
				sw.VS = null;
				_resources.VertexShaders.Remove(shader);
			}

			if (sw.PS != null)
			{
				sw.PS.Dispose();
				sw.PS = null;
				_resources.PixelShaders.Remove(shader);
			}
		}

		private class UniformWrapper
		{
			public PendingBufferWrapper PBW;
			public int VariableStartOffset;
			public int VariableSize;
			public bool VS;
		}

		private class PendingBufferWrapper
		{
			public IntPtr VSPendingBuffer, PSPendingBuffer;
			public int VSBufferSize, PSBufferSize;
			public bool VSBufferDirty, PSBufferDirty; 
		}

		private class PipelineWrapper // Disposable fields cleaned up by FreePipeline
		{
			public readonly PendingBufferWrapper[] PendingBuffers = new PendingBufferWrapper[ID3D11DeviceContext.CommonShaderConstantBufferSlotCount];
			public readonly ID3D11Buffer[] VSConstantBuffers = new ID3D11Buffer[ID3D11DeviceContext.CommonShaderConstantBufferSlotCount];
			public readonly ID3D11Buffer[] PSConstantBuffers = new ID3D11Buffer[ID3D11DeviceContext.CommonShaderConstantBufferSlotCount];
			public ShaderWrapper VertexShader, FragmentShader;
		}

		private class VertexLayoutWrapper
		{
			public ID3D11InputLayout VertexInputLayout;
			public int VertexStride;

			public ID3D11Buffer VertexBuffer;
			public int VertexBufferCount;
		}

		public VertexLayout CreateVertexLayout()
			=> new(this, new VertexLayoutWrapper());

		public void Internal_FreeVertexLayout(VertexLayout layout)
		{
			var vlw = (VertexLayoutWrapper)layout.Opaque;
			vlw.VertexInputLayout?.Dispose();
			vlw.VertexInputLayout = null;
			vlw.VertexBuffer?.Dispose();
			vlw.VertexBuffer = null;
		}

		public void BindPipeline(Pipeline pipeline)
		{
			_curPipeline = pipeline;

			if (pipeline == null)
			{
				// unbind? i don't know
				return;
			}

			var pw = (PipelineWrapper)pipeline.Opaque;
			Context.VSSetShader(pw.VertexShader.VS);
			Context.PSSetShader(pw.FragmentShader.PS);

			Context.VSSetConstantBuffers(0, pw.VSConstantBuffers);
			Context.PSSetConstantBuffers(0, pw.PSConstantBuffers);

			var vlw = (VertexLayoutWrapper)pipeline.VertexLayout.Opaque;
			Context.IASetInputLayout(vlw.VertexInputLayout);
			Context.IASetVertexBuffer(0, vlw.VertexBuffer, vlw.VertexStride);

			// not sure if this applies to the current pipeline or all pipelines
			// just set it every time to be safe
			Context.RSSetState(RasterizerState);
		}

		public void SetPipelineUniform(PipelineUniform uniform, bool value)
		{
			foreach (var ui in uniform.UniformInfos)
			{
				var uw = (UniformWrapper)ui.Opaque;
				var pendingBuffer = uw.VS ? uw.PBW.VSPendingBuffer : uw.PBW.PSPendingBuffer;
				unsafe
				{
					// note: HLSL bool is 4 bytes large
					var b = value ? 1 : 0;
					var variablePtr = (void*)(pendingBuffer + uw.VariableStartOffset);
					Buffer.MemoryCopy(&b, variablePtr, uw.VariableSize, sizeof(int));
				}

				if (uw.VS)
				{
					uw.PBW.VSBufferDirty = true;
				}
				else
				{
					uw.PBW.PSBufferDirty = true;
				}
			}
		}

		public void SetPipelineUniformMatrix(PipelineUniform uniform, Matrix4x4 mat, bool transpose)
			=> SetPipelineUniformMatrix(uniform, ref mat, transpose);

		public void SetPipelineUniformMatrix(PipelineUniform uniform, ref Matrix4x4 mat, bool transpose)
		{
			foreach (var ui in uniform.UniformInfos)
			{
				var uw = (UniformWrapper)ui.Opaque;
				var pendingBuffer = uw.VS ? uw.PBW.VSPendingBuffer : uw.PBW.PSPendingBuffer;
				unsafe
				{
					// transpose logic is inversed
					var m = transpose ? Matrix4x4.Transpose(mat) : mat;
					var variablePtr = (void*)(pendingBuffer + uw.VariableStartOffset);
					Buffer.MemoryCopy(&m, variablePtr, uw.VariableSize, sizeof(Matrix4x4));
				}

				if (uw.VS)
				{
					uw.PBW.VSBufferDirty = true;
				}
				else
				{
					uw.PBW.PSBufferDirty = true;
				}
			}
		}

		public void SetPipelineUniform(PipelineUniform uniform, Vector4 value)
		{
			foreach (var ui in uniform.UniformInfos)
			{
				var uw = (UniformWrapper)ui.Opaque;
				var pendingBuffer = uw.VS ? uw.PBW.VSPendingBuffer : uw.PBW.PSPendingBuffer;
				unsafe
				{
					var variablePtr = (void*)(pendingBuffer + uw.VariableStartOffset);
					Buffer.MemoryCopy(&value, variablePtr, uw.VariableSize, sizeof(Vector4));
				}

				if (uw.VS)
				{
					uw.PBW.VSBufferDirty = true;
				}
				else
				{
					uw.PBW.PSBufferDirty = true;
				}
			}
		}

		public void SetPipelineUniform(PipelineUniform uniform, Vector2 value)
		{
			foreach (var ui in uniform.UniformInfos)
			{
				var uw = (UniformWrapper)ui.Opaque;
				var pendingBuffer = uw.VS ? uw.PBW.VSPendingBuffer : uw.PBW.PSPendingBuffer;
				unsafe
				{
					var variablePtr = (void*)(pendingBuffer + uw.VariableStartOffset);
					Buffer.MemoryCopy(&value, variablePtr, uw.VariableSize, sizeof(Vector2));
				}

				if (uw.VS)
				{
					uw.PBW.VSBufferDirty = true;
				}
				else
				{
					uw.PBW.PSBufferDirty = true;
				}
			}
		}

		public void SetPipelineUniform(PipelineUniform uniform, float value)
		{
			foreach (var ui in uniform.UniformInfos)
			{
				var uw = (UniformWrapper)ui.Opaque;
				var pendingBuffer = uw.VS ? uw.PBW.VSPendingBuffer : uw.PBW.PSPendingBuffer;
				unsafe
				{
					var variablePtr = (void*)(pendingBuffer + uw.VariableStartOffset);
					Buffer.MemoryCopy(&value, variablePtr, uw.VariableSize, sizeof(float));
				}

				if (uw.VS)
				{
					uw.PBW.VSBufferDirty = true;
				}
				else
				{
					uw.PBW.PSBufferDirty = true;
				}
			}
		}

		public void SetPipelineUniform(PipelineUniform uniform, Vector4[] values)
		{
			foreach (var ui in uniform.UniformInfos)
			{
				var uw = (UniformWrapper)ui.Opaque;
				var pendingBuffer = uw.VS ? uw.PBW.VSPendingBuffer : uw.PBW.PSPendingBuffer;
				unsafe
				{
					fixed (Vector4* v = values)
					{
						var variablePtr = (void*)(pendingBuffer + uw.VariableStartOffset);
						Buffer.MemoryCopy(v, variablePtr, uw.VariableSize, values.Length * sizeof(Vector4));
					}
				}

				if (uw.VS)
				{
					uw.PBW.VSBufferDirty = true;
				}
				else
				{
					uw.PBW.PSBufferDirty = true;
				}
			}
		}

		public void SetPipelineUniformSampler(PipelineUniform uniform, ITexture2D tex)
		{
			if (uniform.Owner == null)
			{
				return; // uniform was optimized out
			}

			var d3d11Tex = (D3D11Texture2D)tex;
			var sampler = d3d11Tex.LinearFiltering ? LinearSamplerState : PointSamplerState;

			foreach (var ui in uniform.UniformInfos)
			{
				if (!ui.IsSampler)
				{
					throw new InvalidOperationException("Uniform was not a texture/sampler");
				}

				var uw = (UniformWrapper)ui.Opaque;
				if (uw.VS)
				{
					if (DeviceFeatureLevel == FeatureLevel.Level_9_1)
					{
						throw new InvalidOperationException("Feature level 9.1 does not support setting a shader resource in a vertex shader");
					}

					Context.VSSetShaderResource(ui.SamplerIndex, d3d11Tex.SRV);
					Context.VSSetSampler(ui.SamplerIndex, sampler);
				}
				else
				{
					Context.PSSetShaderResource(ui.SamplerIndex, d3d11Tex.SRV);
					Context.PSSetSampler(ui.SamplerIndex, sampler);
				}
			}
		}

		public ITexture2D CreateTexture(int width, int height)
			=> new D3D11Texture2D(_resources, BindFlags.ShaderResource, ResourceUsage.Dynamic, CpuAccessFlags.Write, width, height);

		// not used for non-GL backends
		public ITexture2D WrapGLTexture2D(int glTexId, int width, int height)
			=> null;

		public Matrix4x4 CreateGuiProjectionMatrix(int width, int height)
		{
			var ret = Matrix4x4.Identity;
			ret.M11 = 2.0f / width;
			ret.M22 = 2.0f / height;
			return ret;
		}

		public Matrix4x4 CreateGuiViewMatrix(int width, int height, bool autoFlip)
		{
			var ret = Matrix4x4.Identity;
			ret.M22 = -1.0f;
			ret.M41 = width * -0.5f;
			ret.M42 = height * 0.5f;

			// auto-flipping isn't needed on D3D
			return ret;
		}

		public void SetViewport(int x, int y, int width, int height)
		{
			Context.RSSetViewport(x, y, width, height);
			Context.RSSetScissorRect(x, y, width, height);
		}

		public IRenderTarget CreateRenderTarget(int width, int height)
			=> new D3D11RenderTarget(_resources, width, height);

		public void BindDefaultRenderTarget()
		{
			_resources.CurRenderTarget = null;
			Context.OMSetRenderTargets(_controlSwapChain.RTV);
		}

		public void Draw(IntPtr data, int count)
		{
			var vlw = (VertexLayoutWrapper)_curPipeline.VertexLayout.Opaque;
			var stride = vlw.VertexStride;

			if (vlw.VertexBufferCount < count)
			{
				vlw.VertexBuffer?.Dispose();
				var bd = new BufferDescription(stride * count, BindFlags.VertexBuffer, ResourceUsage.Dynamic, CpuAccessFlags.Write);
				vlw.VertexBuffer = Device.CreateBuffer(in bd, data);
				vlw.VertexBufferCount = count;
			}
			else
			{
				var mappedVb = Context.Map(vlw.VertexBuffer, MapMode.WriteDiscard);
				try
				{
					unsafe
					{
						Buffer.MemoryCopy((void*)data, (void*)mappedVb.DataPointer, stride * vlw.VertexBufferCount, stride * count);
					}
				}
				finally
				{
					Context.Unmap(vlw.VertexBuffer);
				}
			}

			unsafe
			{
				var pw = (PipelineWrapper)_curPipeline.Opaque;
				for (var i = 0; i < ID3D11DeviceContext.CommonShaderConstantBufferSlotCount; i++)
				{
					var pbw = pw.PendingBuffers[i];
					if (pbw == null)
					{
						break;
					}

					if (pbw.VSBufferDirty)
					{
						var vsCb = Context.Map(pw.VSConstantBuffers[i], MapMode.WriteDiscard);
						Buffer.MemoryCopy((void*)pbw.VSPendingBuffer, (void*)vsCb.DataPointer, pbw.VSBufferSize, pbw.VSBufferSize);
						Context.Unmap(pw.VSConstantBuffers[i]);
						pbw.VSBufferDirty = false;
					}

					if (pbw.PSBufferDirty)
					{
						var psCb = Context.Map(pw.PSConstantBuffers[i], MapMode.WriteDiscard);
						Buffer.MemoryCopy((void*)pbw.PSPendingBuffer, (void*)psCb.DataPointer, pbw.PSBufferSize, pbw.PSBufferSize);
						Context.Unmap(pw.PSConstantBuffers[i]);
						pbw.PSBufferDirty = false;
					}
				}
			}

			Context.Draw(count, 0);
		}
	}
}
