using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;

using BizHawk.Common;
using BizHawk.Common.CollectionExtensions;
using BizHawk.Common.StringExtensions;

using Vortice.D3DCompiler;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.Direct3D11.Shader;
using Vortice.DXGI;

namespace BizHawk.Bizware.Graphics
{
	internal class D3D11Pipeline : IPipeline
	{
		private readonly D3D11Resources _resources;
		private ID3D11Device Device => _resources.Device;
		private ID3D11DeviceContext Context => _resources.Context;
		private ID3D11SamplerState PointSamplerState => _resources.PointSamplerState;
		private ID3D11SamplerState LinearSamplerState => _resources.LinearSamplerState;

		private readonly ReadOnlyMemory<byte> _vsBytecode, _psBytecode;
		private readonly InputElementDescription[] _inputElements;

		public ID3D11InputLayout VertexInputLayout;
		public readonly int VertexStride;

		public ID3D11Buffer VertexBuffer;
		public int VertexBufferCount;

		public ID3D11Buffer IndexBuffer;
		public int IndexBufferCount;

		private readonly record struct D3D11Uniform(IntPtr VariablePointer, int VariableSize, D3D11PendingBuffer PB);

		private readonly Dictionary<string, D3D11Uniform> _vsUniforms = new();
		private readonly Dictionary<string, D3D11Uniform> _psUniforms = new();

		private readonly Dictionary<string, int> _vsSamplers = new();
		private readonly Dictionary<string, int> _psSamplers = new();

		public class D3D11PendingBuffer
		{
			public IntPtr VSPendingBuffer, PSPendingBuffer;
			public int VSBufferSize, PSBufferSize;
			public bool VSBufferDirty, PSBufferDirty; 
		}

		public readonly D3D11PendingBuffer[] PendingBuffers = new D3D11PendingBuffer[ID3D11DeviceContext.CommonShaderConstantBufferSlotCount];
		public readonly ID3D11Buffer[] VSConstantBuffers = new ID3D11Buffer[ID3D11DeviceContext.CommonShaderConstantBufferSlotCount];
		public readonly ID3D11Buffer[] PSConstantBuffers = new ID3D11Buffer[ID3D11DeviceContext.CommonShaderConstantBufferSlotCount];

		public ID3D11VertexShader VS;
		public ID3D11PixelShader PS;

		private static ReadOnlyMemory<byte> CompileShader(PipelineCompileArgs.ShaderCompileArgs compileArgs, string profile, FeatureLevel featureLevel)
		{
			profile += featureLevel switch
			{
				FeatureLevel.Level_9_1 or FeatureLevel.Level_9_2 => "_4_0_level_9_1",
				FeatureLevel.Level_9_3 => "_4_0_level_9_3",
				_ => "_4_0",
			};

			// note: we use D3D9-like shaders for legacy reasons, so we need the backwards compat flag
			// TODO: remove D3D9 syntax from shaders
			return Compiler.Compile(
				shaderSource: compileArgs.Source,
				entryPoint: compileArgs.Entry,
				sourceName: null!, // this is safe to be null
				profile: profile,
				shaderFlags: ShaderFlags.OptimizationLevel3 | ShaderFlags.EnableBackwardsCompatibility);
		}

		public D3D11Pipeline(D3D11Resources resources, PipelineCompileArgs compileArgs)
		{
			_resources = resources;

			try
			{
				_vsBytecode = CompileShader(compileArgs.VertexShaderArgs, "vs", _resources.DeviceFeatureLevel);
				_psBytecode = CompileShader(compileArgs.FragmentShaderArgs, "ps", _resources.DeviceFeatureLevel);

				_inputElements = new InputElementDescription[compileArgs.VertexLayout.Count];
				for (var i = 0; i < compileArgs.VertexLayout.Count; i++)
				{
					var item = compileArgs.VertexLayout[i];

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
						4 => item.Integer ? Format.B8G8R8A8_UNorm : Format.R32G32B32A32_Float,
						_ => throw new InvalidOperationException()
					};

					_inputElements[i] = new(semanticName, item.Usage == AttribUsage.Texcoord1 ? 1 : 0, format, item.Offset, 0);
				}

				VertexStride = compileArgs.VertexLayoutStride;

				using var vsRefl = Compiler.Reflect<ID3D11ShaderReflection>(_vsBytecode.Span);
				using var psRefl = Compiler.Reflect<ID3D11ShaderReflection>(_psBytecode.Span);
				foreach (var refl in new[] { vsRefl, psRefl })
				{
					var isVs = refl == vsRefl;
					var reflCbs = refl.ConstantBuffers;
					var todo = new Queue<(string, int, string, int, ID3D11ShaderReflectionType)>();
					for (var i = 0; i < reflCbs.Length; i++)
					{
						var cbDesc = reflCbs[i].Description;
						var bd = new BufferDescription((cbDesc.Size + 15) & ~15, BindFlags.ConstantBuffer, ResourceUsage.Dynamic, CpuAccessFlags.Write);
						PendingBuffers[i] ??= new();
						if (isVs)
						{
							VSConstantBuffers[i] = Device.CreateBuffer(bd);
							PendingBuffers[i].VSPendingBuffer = Marshal.AllocCoTaskMem(cbDesc.Size);
							PendingBuffers[i].VSBufferSize = cbDesc.Size;
						}
						else
						{
							PSConstantBuffers[i] = Device.CreateBuffer(bd);
							PendingBuffers[i].PSPendingBuffer = Marshal.AllocCoTaskMem(cbDesc.Size);
							PendingBuffers[i].PSBufferSize = cbDesc.Size;
						}

						var prefix = cbDesc.Name.RemovePrefix('$');
						prefix = prefix is "Params" or "Globals" ? "" : $"{prefix}.";
						foreach (var reflVari in reflCbs[i].Variables)
						{
							var reflVariDesc = reflVari.Description;
							todo.Enqueue((prefix, i, reflVariDesc.Name, reflVariDesc.StartOffset, reflVari.VariableType));
						}
					}

					while (todo.Count != 0)
					{
						var (prefix, pbIndex, reflVariName, reflVariOffset, reflVariType) = todo.Dequeue();
						var reflVariTypeDesc = reflVariType.Description;
						if (reflVariTypeDesc.MemberCount > 0)
						{
							prefix = $"{prefix}{reflVariName}.";
							for (var i = 0; i < reflVariTypeDesc.MemberCount; i++)
							{
								var memberName = reflVariType.GetMemberTypeName(i);
								var memberType = reflVariType.GetMemberTypeByIndex(i);
								var memberOffset = memberType.Description.Offset;
								todo.Enqueue((prefix, pbIndex, memberName, reflVariOffset + memberOffset, memberType));
							}

							continue;
						}

						if (reflVariTypeDesc.Type is not (ShaderVariableType.Bool or ShaderVariableType.Float))
						{
							// unsupported type
							continue;
						}

						reflVariName = $"{prefix}{reflVariName}";
						var reflVariSize = 4 * reflVariTypeDesc.ColumnCount * reflVariTypeDesc.RowCount;
						if (isVs)
						{
							var reflVariPtr = PendingBuffers[pbIndex].VSPendingBuffer + reflVariOffset;
							_vsUniforms.Add(reflVariName, new(reflVariPtr, reflVariSize, PendingBuffers[pbIndex]));
						}
						else
						{
							var reflVariPtr = PendingBuffers[pbIndex].PSPendingBuffer + reflVariOffset;
							_psUniforms.Add(reflVariName, new(reflVariPtr, reflVariSize, PendingBuffers[pbIndex]));
						}
					}

					foreach (var resource in refl.BoundResources)
					{
						if (resource.Type == ShaderInputType.Sampler)
						{
							var samplerName = resource.Name.RemovePrefix('$');
							var samplerIndex = resource.BindPoint;
							if (isVs)
							{
								_vsSamplers.Add(samplerName, samplerIndex);
							}
							else
							{
								_psSamplers.Add(samplerName, samplerIndex);
							}
						}
					}
				}

				CreatePipeline();
				_resources.Pipelines.Add(this);
			}
			catch
			{
				DestroyPipeline();
				throw;
			}
		}

		public void Dispose()
		{
			DestroyPipeline();
			DestroyPendingBuffers();
			_resources.Pipelines.Remove(this);
		}

		public void CreatePipeline()
		{
			VertexInputLayout = Device.CreateInputLayout(_inputElements, _vsBytecode.Span);

			for (var i = 0; i < ID3D11DeviceContext.CommonShaderConstantBufferSlotCount; i++)
			{
				var pb = PendingBuffers[i];
				if (pb == null)
				{
					break;
				}

				if (pb.VSBufferSize > 0)
				{
					var bd = new BufferDescription(pb.VSBufferSize, BindFlags.ConstantBuffer, ResourceUsage.Dynamic, CpuAccessFlags.Write);
					VSConstantBuffers[i] = Device.CreateBuffer(bd);
					pb.VSBufferDirty = true;
				}

				if (pb.PSBufferSize > 0)
				{
					var bd = new BufferDescription(pb.PSBufferSize, BindFlags.ConstantBuffer, ResourceUsage.Dynamic, CpuAccessFlags.Write);
					PSConstantBuffers[i] = Device.CreateBuffer(bd);
					pb.PSBufferDirty = true;
				}
			}

			VS = Device.CreateVertexShader(_vsBytecode.Span);
			PS = Device.CreatePixelShader(_psBytecode.Span);
		}

		public void DestroyPipeline()
		{
			VertexInputLayout?.Dispose();
			VertexInputLayout = null;
			VertexBuffer?.Dispose();
			VertexBuffer = null;
			VertexBufferCount = 0;
			IndexBuffer?.Dispose();
			IndexBuffer = null;
			IndexBufferCount = 0;

			for (var i = 0; i < ID3D11DeviceContext.CommonShaderConstantBufferSlotCount; i++)
			{
				VSConstantBuffers[i]?.Dispose();
				VSConstantBuffers[i] = null;
				PSConstantBuffers[i]?.Dispose();
				PSConstantBuffers[i] = null;
			}

			VS?.Dispose();
			VS = null;
			PS?.Dispose();
			PS = null;
		}

		public void DestroyPendingBuffers()
		{
			for (var i = 0; i < ID3D11DeviceContext.CommonShaderConstantBufferSlotCount; i++)
			{
				if (PendingBuffers[i] == null)
				{
					break;
				}

				Marshal.FreeCoTaskMem(PendingBuffers[i].VSPendingBuffer);
				Marshal.FreeCoTaskMem(PendingBuffers[i].PSPendingBuffer);
				PendingBuffers[i] = null;
			}
		}

		public void SetVertexData(IntPtr data, int count)
		{
			if (VertexBufferCount < count)
			{
				VertexBuffer?.Dispose();
				var bd = new BufferDescription( count * VertexStride, BindFlags.VertexBuffer, ResourceUsage.Dynamic, CpuAccessFlags.Write);
				VertexBuffer = Device.CreateBuffer(in bd, data);
				VertexBufferCount = count;
				Context.IASetVertexBuffer(0, VertexBuffer, VertexStride);
			}
			else
			{
				var mappedVb = Context.Map(VertexBuffer, MapMode.WriteDiscard);
				try
				{
					Util.UnsafeSpanFromPointer(ptr: data, length: count * VertexStride)
						.CopyTo(Util.UnsafeSpanFromPointer(ptr: mappedVb.DataPointer, length: VertexBufferCount * VertexStride));
				}
				finally
				{
					Context.Unmap(VertexBuffer);
				}
			}
		}

		public void SetIndexData(IntPtr data, int count)
		{
			if (IndexBufferCount < count)
			{
				IndexBuffer?.Dispose();
				var bd = new BufferDescription(count * 2, BindFlags.IndexBuffer, ResourceUsage.Dynamic, CpuAccessFlags.Write);
				IndexBuffer = Device.CreateBuffer(in bd, data);
				IndexBufferCount = count;
				Context.IASetIndexBuffer(IndexBuffer, Format.R16_UInt, 0);
			}
			else
			{
				var mappedIb = Context.Map(IndexBuffer, MapMode.WriteDiscard);
				try
				{
					Util.UnsafeSpanFromPointer(ptr: data, length: count * 2)
						.CopyTo(Util.UnsafeSpanFromPointer(ptr: mappedIb.DataPointer, length: IndexBufferCount * 2));
				}
				finally
				{
					Context.Unmap(IndexBuffer);
				}
			}
		}

		public bool HasUniformSampler(string name)
		{
			return _vsSamplers.ContainsKey(name)
				|| _psSamplers.ContainsKey(name);
		}

		public string GetUniformSamplerName(int index)
		{
			var sampler = _psSamplers.AsEnumerable().FirstOrNull(s => s.Value == index)
				?? _vsSamplers.AsEnumerable().FirstOrNull(s => s.Value == index);

			return sampler?.Key;
		}

		public void SetUniformSampler(string name, ITexture2D tex)
		{
			if (tex == null)
			{
				return;
			}

			var d3d11Tex = (D3D11Texture2D)tex;
			var sampler = d3d11Tex.LinearFiltering ? LinearSamplerState : PointSamplerState;

			if (_vsSamplers.TryGetValue(name, out var vsSamplerIndex))
			{
				Context.VSSetSampler(vsSamplerIndex, sampler);
				Context.VSSetShaderResource(vsSamplerIndex, d3d11Tex.SRV);
			}

			if (_psSamplers.TryGetValue(name, out var psSamplerIndex))
			{
				Context.PSSetSampler(psSamplerIndex, sampler);
				Context.PSSetShaderResource(psSamplerIndex, d3d11Tex.SRV);
			}
		}

		private unsafe void SetUniform(string name, void* valPtr, int valSize)
		{
			if (_vsUniforms.TryGetValue(name, out var vsUniform))
			{
				Buffer.MemoryCopy(valPtr, (void*)vsUniform.VariablePointer, vsUniform.VariableSize, valSize);
				vsUniform.PB.VSBufferDirty = true;
			}

			if (_psUniforms.TryGetValue(name, out var psUniform))
			{
				Buffer.MemoryCopy(valPtr, (void*)psUniform.VariablePointer, psUniform.VariableSize, valSize);
				psUniform.PB.PSBufferDirty = true;
			}
		}

		public void SetUniformMatrix(string name, Matrix4x4 mat, bool transpose)
			=> SetUniformMatrix(name, ref mat, transpose);

		public unsafe void SetUniformMatrix(string name, ref Matrix4x4 mat, bool transpose)
		{
			var m = transpose ? Matrix4x4.Transpose(mat) : mat;
			SetUniform(name, &m, sizeof(Matrix4x4));
		}

		public unsafe void SetUniform(string name, Vector4 value)
			=> SetUniform(name, &value, sizeof(Vector4));

		public unsafe void SetUniform(string name, Vector2 value)
			=> SetUniform(name, &value, sizeof(Vector2));

		public unsafe void SetUniform(string name, float value)
			=> SetUniform(name, &value, sizeof(float));

		public unsafe void SetUniform(string name, bool value)
		{
			// note: HLSL bool is 4 bytes large
			var b = value ? 1 : 0;
			SetUniform(name, &b, sizeof(int));
		}
	}
}
