#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors.Transforms;

using Veldrid;

using Image = SixLabors.ImageSharp.Image;
using PixelFormat = Veldrid.PixelFormat;

namespace BizHawk.Bizware.BizwareGL
{
	public readonly struct ProcessedTexture
	{
		public readonly uint ArrayLayers;

		public readonly uint Depth;

		public readonly PixelFormat Format;

		public readonly uint Height;

		public readonly uint MipLevels;

		public readonly byte[] TextureData;

		public readonly TextureType Type;

		public readonly uint Width;

		public ProcessedTexture(
			PixelFormat format,
			TextureType type,
			uint width,
			uint height,
			uint depth,
			uint mipLevels,
			uint arrayLayers,
			IReadOnlyCollection<byte> textureData)
		{
			Format = format;
			Type = type;
			Width = width;
			Height = height;
			Depth = depth;
			MipLevels = mipLevels;
			ArrayLayers = arrayLayers;
			TextureData = textureData is byte[] a ? a : textureData.ToArray();
		}

		public readonly unsafe Texture CreateDeviceTexture(GraphicsDevice gd, ResourceFactory rf, TextureUsage usage)
		{
			static uint GetDimension(uint largestLevelDimension, uint mipLevel)
			{
				var ret = largestLevelDimension;
				for (uint i = 0; i < mipLevel; i++) ret >>= 1;
				return Math.Max(1, ret);
			}
			static uint GetFormatSize(PixelFormat format) => format switch
			{
				PixelFormat.R8_G8_B8_A8_UNorm => 4,
				PixelFormat.BC3_UNorm => 1,
				_ => throw new NotImplementedException()
			};
			var texture = rf.CreateTexture(new TextureDescription(Width, Height, Depth, MipLevels, ArrayLayers, Format, usage, Type));
			var staging = rf.CreateTexture(new TextureDescription(Width, Height, Depth, MipLevels, ArrayLayers, Format, TextureUsage.Staging, Type));
			ulong offset = 0;
			fixed (byte* texDataPtr = &TextureData[0])
			{
				for (uint level = 0; level < MipLevels; level++)
				{
					var mipWidth = GetDimension(Width, level);
					var mipHeight = GetDimension(Height, level);
					var mipDepth = GetDimension(Depth, level);
					var subresourceSize = mipWidth * mipHeight * mipDepth * GetFormatSize(Format);
					for (uint layer = 0; layer < ArrayLayers; layer++)
					{
						gd.UpdateTexture(staging, (IntPtr) (texDataPtr + offset), subresourceSize, 0, 0, 0, mipWidth, mipHeight, mipDepth, level, layer);
						offset += subresourceSize;
					}
				}
			}
			var cl = rf.CreateCommandList();
			cl.Begin();
			cl.CopyTexture(staging, texture);
			cl.End();
			gd.SubmitCommands(cl);
			return texture;
		}
	}

	public static class ProcessedTextureSerializer
	{
		public static ProcessedTexture DeserializeFrom(BinaryReader reader) => new(
			reader.ReadEnum<PixelFormat>(),
			reader.ReadEnum<TextureType>(),
			reader.ReadUInt32(),
			reader.ReadUInt32(),
			reader.ReadUInt32(),
			reader.ReadUInt32(),
			reader.ReadUInt32(),
			reader.ReadByteArray());

		public static ProcessedTexture DeserializeFrom(Stream inputStream)
			=> DeserializeFrom(new BinaryReader(inputStream));

		private static byte[] ReadByteArray(this BinaryReader reader)
			=> reader.ReadBytes(reader.ReadInt32()); // i.e. read 4 bytes into $length followed by $length bytes of data

		private static unsafe T ReadEnum<T>(this BinaryReader reader)
			where T : Enum
		{
			var i32 = reader.ReadInt32();
			return Unsafe.Read<T>(&i32);
		}

		public static Stream Serialize(this in ProcessedTexture tex)
		{
			MemoryStream outputStream = new();
			SerializeTo(in tex, outputStream);
			outputStream.Position = 0L;
			return outputStream;
		}

		public static void SerializeTo(this in ProcessedTexture tex, BinaryWriter writer)
		{
			writer.WriteEnum(tex.Format);
			writer.WriteEnum(tex.Type);
			writer.Write(tex.Width);
			writer.Write(tex.Height);
			writer.Write(tex.Depth);
			writer.Write(tex.MipLevels);
			writer.Write(tex.ArrayLayers);
			writer.WriteByteArray(tex.TextureData);
		}

		public static void SerializeTo(this in ProcessedTexture tex, Stream outputStream)
			=> SerializeTo(in tex, new BinaryWriter(outputStream));

		private static void WriteByteArray(this BinaryWriter writer, byte[] array)
		{
			writer.Write(array.Length);
			writer.Write(array);
		}

		private static void WriteEnum<T>(this BinaryWriter writer, T value)
			where T : Enum
			=> writer.Write(Convert.ToInt32(value));
	}

	public static class AssetProcessor
	{
		private static readonly IResampler LanczosResampler = KnownResamplers.Lanczos3;

		private static ProcessedTexture Run(Image<Rgba32> inputImage)
		{
			static int ComputeMipLevels(int width, int height) => 1 + (int) Math.Floor(Math.Log(Math.Max(width, height), 2));
			static Image<T>[] GenerateMipmaps<T>(Image<T> baseImage, out int totalSize)
				where T : unmanaged, IPixel<T>
			{
				var mipLevelCount = ComputeMipLevels(baseImage.Width, baseImage.Height);
				var mipLevels = new Image<T>[mipLevelCount];
				mipLevels[0] = baseImage;
				totalSize = baseImage.Width * baseImage.Height * Unsafe.SizeOf<T>();
				var i = 1;
				var currentWidth = baseImage.Width;
				var currentHeight = baseImage.Height;
				while (currentWidth != 1 || currentHeight != 1)
				{
					var newWidth = Math.Max(1, currentWidth / 2);
					var newHeight = Math.Max(1, currentHeight / 2);
					var newImage = baseImage.Clone(context => context.Resize(newWidth, newHeight, LanczosResampler));
					Debug.Assert(i < mipLevelCount);
					mipLevels[i] = newImage;
					totalSize += newWidth * newHeight * Unsafe.SizeOf<T>();
					i++;
					currentWidth = newWidth;
					currentHeight = newHeight;
				}
				Debug.Assert(i == mipLevelCount);
				return mipLevels;
			}

			var mipmaps = GenerateMipmaps(inputImage, out var totalSize);
			var allTexData = new byte[totalSize];
			var allTexDataSpan = allTexData.AsSpan();
			var offset = 0;
			foreach (var mipmap in mipmaps)
			{
				var mipSize = mipmap.Width * mipmap.Height * Unsafe.SizeOf<Rgba32>();
				if (!mipmap.TryGetSinglePixelSpan(out var span)) throw new Exception();
				var pixelPtr = MemoryMarshal.Cast<Rgba32, byte>(span);
				pixelPtr.CopyTo(allTexDataSpan.Slice(offset, mipSize));
				offset += mipSize;
			}
			return new(
				PixelFormat.R8_G8_B8_A8_UNorm,
				TextureType.Texture2D,
				(uint) inputImage.Width,
				(uint) inputImage.Height,
				1,
				(uint) mipmaps.Length,
				1,
				allTexData);
		}

		/// <param name="inputSpan">contains an image at <see cref="Stream.Position"/> in one of these formats: BMP, GIF, JPEG, PNG, TGA (other formats may be supported)</param>
		public static ProcessedTexture Run(ReadOnlySpan<byte> inputSpan) => Run(Image.Load<Rgba32>(inputSpan));

		/// <param name="inputStream">contains an image at <see cref="Stream.Position"/> in one of these formats: BMP, GIF, JPEG, PNG, TGA (other formats may be supported)</param>
		public static ProcessedTexture Run(Stream inputStream) => Run(Image.Load<Rgba32>(inputStream));

		public static ProcessedTexture Run(Bitmap inputImage)
		{
			MemoryStream ms = new();
			inputImage.Save(ms, ImageFormat.Png);
			ms.Position = 0;
			return Run(ms);
		}

		public static ProcessedTexture Run(Icon inputImage) => Run(inputImage.ToBitmap());
	}
}
