using System.Collections.Generic;

using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;

namespace BizHawk.Bizware.Graphics
{
	/// <summary>
	/// Encapsules all D3D11 resources
	/// This is mainly needed as we need to be able to recreate these resources
	/// Due to possibly getting a device reset/lost (e.g. from hibernation)
	/// </summary>
	internal sealed class D3D11Resources : IDisposable
	{
		public ID3D11Device Device;
		public ID3D11DeviceContext Context;
		public IDXGIFactory1 Factory1;
		public IDXGIFactory2 Factory2;
		public ID3D11BlendState BlendNormalState;
		public ID3D11BlendState BlendDisableState;
		public ID3D11SamplerState PointSamplerState;
		public ID3D11SamplerState LinearSamplerState;
		public ID3D11RasterizerState RasterizerState;

		public FeatureLevel DeviceFeatureLevel;
		public int MaxTextureDimension;
		public bool PresentAllowTearing;

		public D3D11RenderTarget CurRenderTarget;
		public D3D11Pipeline CurPipeline;

		public readonly HashSet<D3D11Texture2D> Textures = [ ];
		public readonly HashSet<D3D11Pipeline> Pipelines = [ ];

		public void CreateResources()
		{
			try
			{
#if false
				// use this to debug D3D11 calls
				// note the debug layer requires extra steps to use: https://learn.microsoft.com/en-us/windows/win32/direct3d11/overviews-direct3d-11-devices-layers#debug-layer
				// also debug output will only be present with a "native debugger" attached (pure managed debugger can't see this output)
				var creationFlags = DeviceCreationFlags.Singlethreaded | DeviceCreationFlags.BgraSupport | DeviceCreationFlags.Debug;
#else
				// IGL is not thread safe, so let's not bother making this implementation thread safe
				var creationFlags = DeviceCreationFlags.Singlethreaded | DeviceCreationFlags.BgraSupport;
#endif

				// GL interop doesn't support the single threaded flag
				if (D3D11GLInterop.IsAvailable)
				{
					creationFlags &= ~DeviceCreationFlags.Singlethreaded;
				}

				D3D11.D3D11CreateDevice(
					adapter: null,
					DriverType.Hardware,
					creationFlags,
					null!, // this is safe to be null
					out Device,
					out Context).CheckError();

				using var dxgiDevice = Device.QueryInterface<IDXGIDevice1>();
				dxgiDevice.MaximumFrameLatency = 1;

				using var adapter = dxgiDevice.GetAdapter();
				Factory1 = adapter.GetParent<IDXGIFactory1>();
				// we want IDXGIFactory2 for CreateSwapChainForHwnd
				// however, it's not guaranteed to be available (only available in Win8+ or Win7 with the Platform Update)
				Factory2 = Factory1.QueryInterfaceOrNull<IDXGIFactory2>();

				using var factory5 = Factory1.QueryInterfaceOrNull<IDXGIFactory5>();
				PresentAllowTearing = factory5?.PresentAllowTearing ?? false;

				var bd = default(BlendDescription);
				bd.AlphaToCoverageEnable = false;
				bd.IndependentBlendEnable = false;
				bd.RenderTarget[0].BlendEnable = true;
				bd.RenderTarget[0].SourceBlend = Blend.SourceAlpha;
				bd.RenderTarget[0].DestinationBlend = Blend.InverseSourceAlpha;
				bd.RenderTarget[0].BlendOperation = BlendOperation.Add;
				bd.RenderTarget[0].SourceBlendAlpha = Blend.One;
				bd.RenderTarget[0].DestinationBlendAlpha = Blend.InverseSourceAlpha;
				bd.RenderTarget[0].BlendOperationAlpha = BlendOperation.Add;
				bd.RenderTarget[0].RenderTargetWriteMask = ColorWriteEnable.All;
				BlendNormalState = Device.CreateBlendState(bd);

				bd.RenderTarget[0].SourceBlend = Blend.One;
				bd.RenderTarget[0].DestinationBlend = Blend.Zero;
				bd.RenderTarget[0].BlendEnable = false;
				BlendDisableState = Device.CreateBlendState(bd);

				PointSamplerState = Device.CreateSamplerState(SamplerDescription.PointClamp);
				LinearSamplerState = Device.CreateSamplerState(SamplerDescription.LinearClamp);

				DeviceFeatureLevel = Device.FeatureLevel;

				MaxTextureDimension = DeviceFeatureLevel switch
				{
					FeatureLevel.Level_9_1 or FeatureLevel.Level_9_2 => 2048,
					FeatureLevel.Level_9_3 => 4096,
					FeatureLevel.Level_10_0 or FeatureLevel.Level_10_1 => 8192,
					_ => ID3D11Resource.MaximumTexture2DSize,
				};

				var rd = new RasterizerDescription
				{
					CullMode = CullMode.None,
					FillMode = FillMode.Solid,
					ScissorEnable = true,
					DepthClipEnable = DeviceFeatureLevel is FeatureLevel.Level_9_1 or FeatureLevel.Level_9_2 or FeatureLevel.Level_9_3,
				};

				RasterizerState = Device.CreateRasterizerState(rd);

				foreach (var tex2d in Textures)
				{
					tex2d.CreateTexture();
				}
			}
			catch
			{
				Dispose();
				throw;
			}
		}

		public void DestroyResources()
		{
			foreach (var tex2d in Textures)
			{
				tex2d.DestroyTexture();
			}

			foreach (var pipeline in Pipelines)
			{
				pipeline.DestroyPipeline();
			}

			CurRenderTarget = null;
			CurPipeline = null;

			LinearSamplerState?.Dispose();
			LinearSamplerState = null;
			PointSamplerState?.Dispose();
			PointSamplerState = null;

			RasterizerState?.Dispose();
			RasterizerState = null;

			BlendNormalState?.Dispose();
			BlendNormalState = null;
			BlendDisableState?.Dispose();
			BlendDisableState = null;

			Context?.Dispose();
			Context = null;
			Device?.Dispose();
			Device = null;

			Factory2?.Dispose();
			Factory2 = null;

			Factory1?.Dispose();
			Factory1 = null;
		}

		public void Dispose()
		{
			DestroyResources();
			Textures.Clear();

			foreach (var pipeline in Pipelines)
			{
				pipeline.DestroyPendingBuffers();
			}

			Pipelines.Clear();
		}
	}
}
