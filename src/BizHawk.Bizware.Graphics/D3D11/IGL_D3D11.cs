using System.Drawing;
using System.Numerics;

using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;

using BizHawk.Common;

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

		private ID3D11Device Device => _resources.Device;
		private ID3D11DeviceContext Context => _resources.Context;
		private IDXGIFactory1 Factory1 => _resources.Factory1;
		private IDXGIFactory2 Factory2 => _resources.Factory2;
		private ID3D11BlendState BlendNormalState => _resources.BlendNormalState;
		private ID3D11BlendState BlendDisableState => _resources.BlendDisableState;
		private ID3D11RasterizerState RasterizerState => _resources.RasterizerState;

		private bool PresentAllowTearing => _resources.PresentAllowTearing;

		private D3D11RenderTarget CurRenderTarget => _resources.CurRenderTarget;
		private D3D11Pipeline CurPipeline => _resources.CurPipeline;

		private D3D11SwapChain.SwapChainResources _controlSwapChain;
		private readonly D3D11GLInterop _glInterop;

		public IGL_D3D11()
		{
			if (OSTailoredCode.IsUnixHost)
			{
				throw new NotSupportedException("D3D11 is Windows only");
			}

			_resources = new();
			_resources.CreateResources();

			if (D3D11GLInterop.IsAvailable)
			{
				_glInterop = new(_resources);
			}
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
					flags: PresentAllowTearing ? SwapChainFlags.AllowTearing : SwapChainFlags.None);

				try
				{
					ret = Factory2.CreateSwapChainForHwnd(Device, cp.Handle, sd);
				}
				catch
				{
					try
					{
						// FlipSequential is supported on Win8+
						sd.SwapEffect = SwapEffect.FlipSequential;
						ret = Factory2.CreateSwapChainForHwnd(Device, cp.Handle, sd);
					}
					catch
					{
						sd.SwapEffect = SwapEffect.Discard;
						sd.Flags = SwapChainFlags.None;
						ret = Factory2.CreateSwapChainForHwnd(Device, cp.Handle, sd);
					}
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
			Context.Flush(); // important to immediately dispose of the swapchain (if it's still around, we can't recreate it)

			_glInterop?.Dispose();
			_resources.DestroyResources();
			_resources.CreateResources();
			_glInterop?.OpenInteropDevice();

			var swapChain = CreateDXGISwapChain(cp);
			var bbTex = swapChain.GetBuffer<ID3D11Texture2D>(0);
			var bbRtvd = new RenderTargetViewDescription(RenderTargetViewDimension.Texture2D, Format.B8G8R8A8_UNorm);
			var rtv = Device.CreateRenderTargetView(bbTex, bbRtvd);
			var swapChainDesc = swapChain.Description;

			_controlSwapChain.Device = Device;
			_controlSwapChain.Context = Context;
			_controlSwapChain.Context1 = Context.QueryInterfaceOrNull<ID3D11DeviceContext1>();
			_controlSwapChain.BackBufferTexture = bbTex;
			_controlSwapChain.RTV = rtv;
			_controlSwapChain.SwapChain = swapChain;
			_controlSwapChain.HasFlipModel = swapChainDesc.SwapEffect is SwapEffect.FlipSequential or SwapEffect.FlipDiscard;
			_controlSwapChain.AllowsTearing = (swapChainDesc.Flags & SwapChainFlags.AllowTearing) != 0;
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
			var swapChainDesc = swapChain.Description;

			_controlSwapChain = new()
			{
				Device = Device,
				Context = Context,
				Context1 = Context.QueryInterfaceOrNull<ID3D11DeviceContext1>(),
				BackBufferTexture = bbTex,
				RTV = rtv,
				SwapChain = swapChain,
				HasFlipModel = swapChainDesc.SwapEffect is SwapEffect.FlipSequential or SwapEffect.FlipDiscard,
				AllowsTearing = (swapChainDesc.Flags & SwapChainFlags.AllowTearing) != 0,
			};

			return new(_controlSwapChain, ResetDevice);
		}

		public void Dispose()
		{
			_controlSwapChain.Dispose();
			_resources.Dispose();
		}

		public int MaxTextureDimension => _resources.MaxTextureDimension;

		public void ClearColor(Color color)
			=> Context.ClearRenderTargetView(CurRenderTarget?.RTV ?? _controlSwapChain.RTV, new(color.R, color.B, color.G, color.A));

		public void EnableBlending()
			=> Context.OMSetBlendState(BlendNormalState);

		public void DisableBlending()
			=> Context.OMSetBlendState(BlendDisableState);

		public IPipeline CreatePipeline(PipelineCompileArgs compileArgs)
			=> new D3D11Pipeline(_resources, compileArgs);

		public void BindPipeline(IPipeline pipeline)
		{
			var d3d11Pipeline = (D3D11Pipeline)pipeline;
			_resources.CurPipeline = d3d11Pipeline;

			if (d3d11Pipeline == null)
			{
				// unbind? i don't know
				return;
			}

			Context.VSSetShader(d3d11Pipeline.VS);
			Context.PSSetShader(d3d11Pipeline.PS);

			Context.VSSetConstantBuffers(0, d3d11Pipeline.VSConstantBuffers);
			Context.PSSetConstantBuffers(0, d3d11Pipeline.PSConstantBuffers);

			Context.VSUnsetSamplers(0, ID3D11DeviceContext.CommonShaderSamplerSlotCount);
			Context.VSUnsetShaderResources(0, ID3D11DeviceContext.CommonShaderSamplerSlotCount);
			Context.PSUnsetSamplers(0, ID3D11DeviceContext.CommonShaderSamplerSlotCount);
			Context.PSUnsetShaderResources(0, ID3D11DeviceContext.CommonShaderSamplerSlotCount);

			Context.IASetInputLayout(d3d11Pipeline.VertexInputLayout);
			Context.IASetVertexBuffer(0, d3d11Pipeline.VertexBuffer, d3d11Pipeline.VertexStride);
			Context.IASetIndexBuffer(d3d11Pipeline.IndexBuffer, Format.R16_UInt, 0);

			// not sure if this applies to the current pipeline or all pipelines
			// just set it every time to be safe
			Context.RSSetState(RasterizerState);
		}

		public ITexture2D CreateTexture(int width, int height)
			=> new D3D11Texture2D(_resources, BindFlags.ShaderResource, ResourceUsage.Dynamic, CpuAccessFlags.Write, width, height);

		public ITexture2D WrapGLTexture2D(int glTexId, int width, int height)
			=> _glInterop?.WrapGLTexture(glTexId, width, height);

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

		private void UpdateConstantBuffers()
		{
			for (var i = 0; i < ID3D11DeviceContext.CommonShaderConstantBufferSlotCount; i++)
			{
				var pb = CurPipeline.PendingBuffers[i];
				if (pb == null)
				{
					break;
				}

				if (pb.VSBufferDirty)
				{
					var vsCb = Context.Map(CurPipeline.VSConstantBuffers[i], MapMode.WriteDiscard);
					Util.UnsafeSpanFromPointer(ptr: pb.VSPendingBuffer, length: pb.VSBufferSize)
						.CopyTo(Util.UnsafeSpanFromPointer(ptr: vsCb.DataPointer, length: pb.VSBufferSize));
					Context.Unmap(CurPipeline.VSConstantBuffers[i]);
					pb.VSBufferDirty = false;
				}

				if (pb.PSBufferDirty)
				{
					var psCb = Context.Map(CurPipeline.PSConstantBuffers[i], MapMode.WriteDiscard);
					Util.UnsafeSpanFromPointer(ptr: pb.PSPendingBuffer, length: pb.PSBufferSize)
						.CopyTo(Util.UnsafeSpanFromPointer(ptr: psCb.DataPointer, length: pb.PSBufferSize));
					Context.Unmap(CurPipeline.PSConstantBuffers[i]);
					pb.PSBufferDirty = false;
				}
			}
		}

		public void Draw(int vertexCount)
		{
			UpdateConstantBuffers();
			Context.IASetPrimitiveTopology(PrimitiveTopology.TriangleStrip);
			Context.Draw(vertexCount, 0);
		}

		public void DrawIndexed(int indexCount, int indexStart, int vertexStart)
		{
			UpdateConstantBuffers();
			Context.IASetPrimitiveTopology(PrimitiveTopology.TriangleList);
			Context.DrawIndexed(indexCount, indexStart, vertexStart);
		}
	}
}
