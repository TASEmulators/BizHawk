using System.Collections.Generic;
using System.Runtime.InteropServices;

using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;

namespace BizHawk.Bizware.Graphics
{
	internal class D3D11Texture2D : ITexture2D
	{
		protected readonly D3D11Resources _resources;
		private readonly BindFlags _bindFlags;
		private readonly ResourceUsage _usage;
		private readonly CpuAccessFlags _cpuAccessFlags;

		protected ID3D11Device Device => _resources.Device;
		protected ID3D11DeviceContext Context => _resources.Context;
		private HashSet<D3D11Texture2D> Textures => _resources.Textures;

		private ID3D11Texture2D StagingTexture;

		public ID3D11Texture2D Texture;
		public ID3D11ShaderResourceView SRV;
		public bool LinearFiltering;

		public int Width { get; }
		public int Height { get; }
		public bool IsUpsideDown { get; }

		public D3D11Texture2D(D3D11Resources resources, BindFlags bindFlags, ResourceUsage usage, CpuAccessFlags cpuAccessFlags, int width, int height, bool wrapped = false)
		{
			_resources = resources;
			_bindFlags = bindFlags;
			_usage = usage;
			_cpuAccessFlags = cpuAccessFlags;
			Width = width;
			Height = height;
			IsUpsideDown = wrapped;
			// ReSharper disable once VirtualMemberCallInConstructor
			CreateTexture();
			Textures.Add(this);
		}

		public void Dispose()
		{
			DestroyTexture();
			Textures.Remove(this);
		}

		public virtual void CreateTexture()
		{
			// wrapped textures are R8G8B8A8 rather than B8G8R8A8...
			var format = IsUpsideDown ? Format.R8G8B8A8_UNorm : Format.B8G8R8A8_UNorm;
			Texture = Device.CreateTexture2D(
				format,
				Width,
				Height,
				mipLevels: 1,
				bindFlags: _bindFlags,
				usage: _usage,
				cpuAccessFlags: _cpuAccessFlags);

			var srvd = new ShaderResourceViewDescription(ShaderResourceViewDimension.Texture2D, format, mostDetailedMip: 0, mipLevels: 1);
			SRV = Device.CreateShaderResourceView(Texture, srvd);
		}

		public virtual void DestroyTexture()
		{
			SRV?.Dispose();
			SRV = null;
			Texture?.Dispose();
			Texture = null;
			StagingTexture?.Dispose();
			StagingTexture = null;
		}

		public BitmapBuffer Resolve()
		{
			StagingTexture ??= Device.CreateTexture2D(
				Format.B8G8R8A8_UNorm,
				Width,
				Height,
				mipLevels: 1,
				bindFlags: BindFlags.None,
				usage: ResourceUsage.Staging,
				cpuAccessFlags: CpuAccessFlags.Read);

			Context.CopyResource(StagingTexture, Texture);

			try
			{
				var srcSpan = Context.MapReadOnly<byte>(StagingTexture);
				var pixels = new int[Width * Height];
				var dstSpan = MemoryMarshal.AsBytes(pixels.AsSpan());

				if (srcSpan.Length == dstSpan.Length)
				{
					srcSpan.CopyTo(dstSpan);
				}
				else
				{
					int srcStart = 0, dstStart = 0;
					int srcStride = srcSpan.Length / Height, dstStride = Width * sizeof(int);
					for (var i = 0; i < Height; i++)
					{
						srcSpan.Slice(srcStart, dstStride)
							.CopyTo(dstSpan.Slice(dstStart, dstStride));
						srcStart += srcStride;
						dstStart += dstStride;
					}
				}

				return new(Width, Height, pixels);
			}
			finally
			{
				Context.Unmap(StagingTexture, 0);
			}
		}

		public unsafe void LoadFrom(BitmapBuffer buffer)
		{
			if (buffer.Width != Width || buffer.Height != Height)
			{
				throw new InvalidOperationException("BitmapBuffer dimensions do not match texture dimensions");
			}

			if ((_cpuAccessFlags & CpuAccessFlags.Write) == 0)
			{
				throw new InvalidOperationException("This texture cannot be written by the CPU");
			}

			var bmpData = buffer.LockBits();
			try
			{
				var srcSpan = new ReadOnlySpan<byte>(bmpData.Scan0.ToPointer(), bmpData.Stride * buffer.Height);
				var mappedTex = Context.Map<byte>(Texture, 0, 0, MapMode.WriteDiscard);

				if (srcSpan.Length == mappedTex.Length)
				{
					srcSpan.CopyTo(mappedTex);
				}
				else
				{
					// D3D11 sometimes has weird pitches (seen with 3DS)
					int srcStart = 0, dstStart = 0;
					int srcStride = bmpData.Stride, dstStride = mappedTex.Length / buffer.Height;
					var height = buffer.Height;
					for (var i = 0; i < height; i++)
					{
						srcSpan.Slice(srcStart, srcStride)
							.CopyTo(mappedTex.Slice(dstStart, dstStride));
						srcStart += srcStride;
						dstStart += dstStride;
					}
				}
			}
			finally
			{
				Context.Unmap(Texture, 0);
				buffer.UnlockBits(bmpData);
			}
		}

		public void SetFilterLinear()
			=> LinearFiltering = true;

		public void SetFilterNearest()
			=> LinearFiltering = false;

		public override string ToString()
			=> $"D3D11 Texture2D: {Width}x{Height}";
	}
}
