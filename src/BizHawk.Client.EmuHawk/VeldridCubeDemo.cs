using System;
using System.Runtime.CompilerServices;
using System.Text;

using BizHawk.Bizware.BizwareGL;
using BizHawk.Common.IOExtensions;
using BizHawk.WinForms.Controls;

using Veldrid;
using Veldrid.SPIRV;

using Matrix4 = System.Numerics.Matrix4x4;
using Pipeline = Veldrid.Pipeline;
using Shader = Veldrid.Shader;

namespace BizHawk.Client.EmuHawk
{
	public sealed class VeldridCubeDemo : VeldridControl
	{
		private readonly struct VertexPositionTexture
		{
			public readonly float PosX;

			public readonly float PosY;

			public readonly float PosZ;

			public readonly float TexU;

			public readonly float TexV;

			public VertexPositionTexture(float x, float y, float z, float u, float v)
			{
				PosX = x;
				PosY = y;
				PosZ = z;
				TexU = u;
				TexV = v;
			}
		}

		private const string VertexCode = @"
#version 450

layout(set = 0, binding = 0) uniform ProjectionBuffer
{
	mat4 Projection;
};

layout(set = 0, binding = 1) uniform ViewBuffer
{
	mat4 View;
};

layout(set = 1, binding = 0) uniform WorldBuffer
{
	mat4 World;
};

layout(location = 0) in vec3 Position;
layout(location = 1) in vec2 TexCoords;
layout(location = 0) out vec2 fsin_texCoords;

void main()
{
	vec4 worldPosition = World * vec4(Position, 1);
	vec4 viewPosition = View * worldPosition;
	vec4 clipPosition = Projection * viewPosition;
	gl_Position = clipPosition;
	fsin_texCoords = TexCoords;
}";

		private const string FragmentCode = @"
#version 450

layout(location = 0) in vec2 fsin_texCoords;
layout(location = 0) out vec4 fsout_color;

layout(set = 1, binding = 1) uniform texture2D SurfaceTexture;
layout(set = 1, binding = 2) uniform sampler SurfaceSampler;

void main()
{
	fsout_color =  texture(sampler2D(SurfaceTexture, SurfaceSampler), fsin_texCoords);
}";

		private readonly ProcessedTexture _stoneTexData;
		private readonly VertexPositionTexture[] _vertices;
		private readonly ushort[] _indices;
		private DeviceBuffer _projectionBuffer;
		private DeviceBuffer _viewBuffer;
		private DeviceBuffer _worldBuffer;
		private DeviceBuffer _vertexBuffer;
		private DeviceBuffer _indexBuffer;
		private CommandList _cl;
		private Pipeline _pipeline;
		private ResourceSet _projViewSet;
		private ResourceSet _worldTextureSet;
		private float _ticks;
		private GraphicsDevice GraphicsDevice;
		private Swapchain MainSwapchain;

		public VeldridCubeDemo()
		{
			IsAnimated = true;

			GraphicsDeviceCreated += (gd, factory, sc) =>
			{
				GraphicsDevice = gd;
				MainSwapchain = sc;

				_projectionBuffer = factory.CreateBuffer(new(64, BufferUsage.UniformBuffer));
				_viewBuffer = factory.CreateBuffer(new(64, BufferUsage.UniformBuffer));
				_worldBuffer = factory.CreateBuffer(new(64, BufferUsage.UniformBuffer));

				_vertexBuffer = factory.CreateBuffer(new((uint) (Unsafe.SizeOf<VertexPositionTexture>() * _vertices.Length), BufferUsage.VertexBuffer));
				GraphicsDevice.UpdateBuffer(_vertexBuffer, 0, _vertices);

				_indexBuffer = factory.CreateBuffer(new((uint) (sizeof(ushort) * _indices.Length), BufferUsage.IndexBuffer));
				GraphicsDevice.UpdateBuffer(_indexBuffer, 0, _indices);

				var surfaceTextureView = factory.CreateTextureView(_stoneTexData.CreateDeviceTexture(GraphicsDevice, factory, TextureUsage.Sampled));

				ShaderSetDescription shaderSet = new(
					new VertexLayoutDescription[]
					{
						new(
							new VertexElementDescription("Position", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3),
							new VertexElementDescription("TexCoords", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2))
					},
					factory.CreateFromSpirv(
						new ShaderDescription(ShaderStages.Vertex, Encoding.UTF8.GetBytes(VertexCode), "main"),
						new ShaderDescription(ShaderStages.Fragment, Encoding.UTF8.GetBytes(FragmentCode), "main")));

				var projViewLayout = factory.CreateResourceLayout(new(
					new("ProjectionBuffer", ResourceKind.UniformBuffer, ShaderStages.Vertex),
					new("ViewBuffer", ResourceKind.UniformBuffer, ShaderStages.Vertex)));

				var worldTextureLayout = factory.CreateResourceLayout(new(
					new("WorldBuffer", ResourceKind.UniformBuffer, ShaderStages.Vertex),
					new("SurfaceTexture", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
					new("SurfaceSampler", ResourceKind.Sampler, ShaderStages.Fragment)));

				_pipeline = factory.CreateGraphicsPipeline(new(
					BlendStateDescription.SingleOverrideBlend,
					DepthStencilStateDescription.DepthOnlyLessEqual,
					RasterizerStateDescription.Default,
					PrimitiveTopology.TriangleList,
					shaderSet,
					new[] { projViewLayout, worldTextureLayout },
					MainSwapchain.Framebuffer.OutputDescription));

				_projViewSet = factory.CreateResourceSet(new(projViewLayout, _projectionBuffer, _viewBuffer));

				_worldTextureSet = factory.CreateResourceSet(new(
					worldTextureLayout,
					_worldBuffer,
					surfaceTextureView,
					GraphicsDevice.Aniso4xSampler));

				_cl = factory.CreateCommandList();
			};
			GraphicsDeviceDestroyed += () =>
			{
				GraphicsDevice = null;
				MainSwapchain = null;
			};
			Rendering += deltaSeconds =>
			{
				_ticks += deltaSeconds;
				_cl.Begin();
				_cl.UpdateBuffer(
					_projectionBuffer,
					0,
					Matrix4.CreatePerspectiveFieldOfView(
						1.0f,
						(float) Width / Height,
						0.5f,
						100.0f));
				_cl.UpdateBuffer(
					_viewBuffer,
					0,
					Matrix4.CreateLookAt(new(0.0f, 0.0f, 2.5f), new(0.0f, 0.0f, 0.0f), new(0.0f, 1.0f, 0.0f)));
				_cl.UpdateBuffer(
					_worldBuffer,
					0,
					Matrix4.CreateFromAxisAngle(new(0.0f, 1.0f, 0.0f), _ticks)
						* Matrix4.CreateFromAxisAngle(new(1.0f, 0.0f, 0.0f), _ticks / 3.0f));
				_cl.SetFramebuffer(MainSwapchain.Framebuffer);
				_cl.ClearColorTarget(0, RgbaFloat.Black);
				_cl.ClearDepthStencil(1.0f);
				_cl.SetPipeline(_pipeline);
				_cl.SetVertexBuffer(0, _vertexBuffer);
				_cl.SetIndexBuffer(_indexBuffer, IndexFormat.UInt16);
				_cl.SetGraphicsResourceSet(0, _projViewSet);
				_cl.SetGraphicsResourceSet(1, _worldTextureSet);
				_cl.DrawIndexed(unchecked((uint) _indices.Length), 1, 0, 0, 0);
				_cl.End();
				GraphicsDevice.SubmitCommands(_cl);
				GraphicsDevice.SwapBuffers(MainSwapchain);
				GraphicsDevice.WaitForIdle();
			};

			_stoneTexData = AssetProcessor.Run(Properties.Resources.CorpHawk);
			_vertices = new VertexPositionTexture[]
			{
				// Top
				new(-0.5f, +0.5f, -0.5f, 0.0f, 0.0f),
				new(+0.5f, +0.5f, -0.5f, 1.0f, 0.0f),
				new(+0.5f, +0.5f, +0.5f, 1.0f, 1.0f),
				new(-0.5f, +0.5f, +0.5f, 0.0f, 1.0f),
				// Bottom
				new(-0.5f, -0.5f, +0.5f, 0.0f, 0.0f),
				new(+0.5f, -0.5f, +0.5f, 1.0f, 0.0f),
				new(+0.5f, -0.5f, -0.5f, 1.0f, 1.0f),
				new(-0.5f, -0.5f, -0.5f, 0.0f, 1.0f),
				// Left
				new(-0.5f, +0.5f, -0.5f, 0.0f, 0.0f),
				new(-0.5f, +0.5f, +0.5f, 1.0f, 0.0f),
				new(-0.5f, -0.5f, +0.5f, 1.0f, 1.0f),
				new(-0.5f, -0.5f, -0.5f, 0.0f, 1.0f),
				// Right
				new(+0.5f, +0.5f, +0.5f, 0.0f, 0.0f),
				new(+0.5f, +0.5f, -0.5f, 1.0f, 0.0f),
				new(+0.5f, -0.5f, -0.5f, 1.0f, 1.0f),
				new(+0.5f, -0.5f, +0.5f, 0.0f, 1.0f),
				// Back
				new(+0.5f, +0.5f, -0.5f, 0.0f, 0.0f),
				new(-0.5f, +0.5f, -0.5f, 1.0f, 0.0f),
				new(-0.5f, -0.5f, -0.5f, 1.0f, 1.0f),
				new(+0.5f, -0.5f, -0.5f, 0.0f, 1.0f),
				// Front
				new(-0.5f, +0.5f, +0.5f, 0.0f, 0.0f),
				new(+0.5f, +0.5f, +0.5f, 1.0f, 0.0f),
				new(+0.5f, -0.5f, +0.5f, 1.0f, 1.0f),
				new(-0.5f, -0.5f, +0.5f, 0.0f, 1.0f),
			};
			_indices = new ushort[] {
				0, 1, 2,
				0, 2, 3,
				4, 5, 6,
				4, 6, 7,
				8, 9, 10,
				8, 10, 11,
				12, 13, 14,
				12, 14, 15,
				16, 17, 18,
				16, 18, 19,
				20, 21, 22,
				20, 22, 23,
			};
		}

		public Shader LoadShader(ResourceFactory factory, string set, ShaderStages stage, string entryPoint)
		{
			static string GetExtension(GraphicsBackend backendType) => backendType switch
			{
				GraphicsBackend.Direct3D11 => "hlsl.bytes",
				GraphicsBackend.Vulkan => "450.glsl.spv",
				GraphicsBackend.Metal => "metallib",
				GraphicsBackend.OpenGL => "330.glsl",
				GraphicsBackend.OpenGLES => "300.glsles",
				_ => throw new Exception()
			};
			var shaderBytes = EmuHawk.ReflectionCache.EmbeddedResourceStream($"{set}-{stage.ToString().ToLowerInvariant()}.{GetExtension(factory.BackendType)}").ReadAllBytes();
			return factory.CreateShader(new(stage, shaderBytes, entryPoint));
		}
	}
}
