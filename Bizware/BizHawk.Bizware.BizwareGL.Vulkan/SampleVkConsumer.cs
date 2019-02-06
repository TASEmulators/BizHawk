using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;

using BizHawk.Common.CollectionExtensions;

using Vulkan;

using Buffer = Vulkan.Buffer;
using VkPipeline = Vulkan.Pipeline;

namespace BizHawk.Bizware.BizwareGL.Drivers.Vulkan
{
	public class SampleVkConsumer
	{
		private Device device;
		private Queue queue;
		private SwapchainKhr swapchain;
		private Semaphore semaphore;
		private SurfaceCapabilitiesKhr surfaceCapabilities;
		private RenderPass renderPass;

		private Fence fence;
		private Image[] images;
		private Framebuffer[] framebuffers;

		private bool initialized;

		private static SurfaceFormatKhr SelectFormat(PhysicalDevice physicalDevice, SurfaceKhr surface)
		{
			var first = physicalDevice.GetSurfaceFormatsKHR(surface)
				.FirstOrNull(f => f.Format == Format.R8G8B8A8Unorm || f.Format == Format.B8G8R8A8Unorm);
			if (first == null) throw new Exception("didn't find the R8G8B8A8Unorm or B8G8R8A8Unorm format");
			return first.Value;
		}

		private SwapchainKhr CreateSwapchain(SurfaceKhr surface, SurfaceFormatKhr surfaceFormat) =>
			device.CreateSwapchainKHR(
				new SwapchainCreateInfoKhr
				{
					Surface = surface,
					MinImageCount = surfaceCapabilities.MinImageCount,
					ImageFormat = surfaceFormat.Format,
					ImageColorSpace = surfaceFormat.ColorSpace,
					ImageExtent = surfaceCapabilities.CurrentExtent,
					ImageUsage = ImageUsageFlags.ColorAttachment,
					PreTransform = SurfaceTransformFlagsKhr.Identity,
					ImageArrayLayers = 1,
					ImageSharingMode = SharingMode.Exclusive,
					QueueFamilyIndices = new[] { 0u },
					PresentMode = PresentModeKhr.Fifo,
					CompositeAlpha = surfaceCapabilities.SupportedCompositeAlpha.HasFlag(CompositeAlphaFlagsKhr.Inherit)
						? CompositeAlphaFlagsKhr.Inherit
						: CompositeAlphaFlagsKhr.Opaque
				});

		private Framebuffer[] CreateFramebuffers(Image[] img, SurfaceFormatKhr surfaceFormat) => Array.ConvertAll(
			img,
			i =>
				device.CreateFramebuffer(
					new FramebufferCreateInfo
					{
						Layers = 1,
						RenderPass = renderPass,
						Attachments = new[]
						{
							device.CreateImageView(
								new ImageViewCreateInfo
								{
									Image = i,
									ViewType = ImageViewType.View2D,
									Format = surfaceFormat.Format,
									Components = new ComponentMapping
									{
										R = ComponentSwizzle.R,
										G = ComponentSwizzle.G,
										B = ComponentSwizzle.B,
										A = ComponentSwizzle.A
									},
									SubresourceRange = new ImageSubresourceRange
									{
										AspectMask = ImageAspectFlags.Color,
										LevelCount = 1,
										LayerCount = 1
									}
								})
						},
						Width = surfaceCapabilities.CurrentExtent.Width,
						Height = surfaceCapabilities.CurrentExtent.Height
					}));

		private RenderPass CreateRenderPass(SurfaceFormatKhr surfaceFormat) =>
			device.CreateRenderPass(
				new RenderPassCreateInfo
				{
					Attachments = new[]
					{
						new AttachmentDescription
						{
							Format = surfaceFormat.Format,
							Samples = SampleCountFlags.Count1,
							LoadOp = AttachmentLoadOp.Clear,
							StoreOp = AttachmentStoreOp.Store,
							StencilLoadOp = AttachmentLoadOp.DontCare,
							StencilStoreOp = AttachmentStoreOp.DontCare,
							InitialLayout = ImageLayout.Undefined,
							FinalLayout = ImageLayout.PresentSrcKhr
						}
					},
					Subpasses = new[]
					{
						new SubpassDescription
						{
							PipelineBindPoint = PipelineBindPoint.Graphics,
							ColorAttachments = new[]
							{
								new AttachmentReference { Layout = ImageLayout.ColorAttachmentOptimal }
							}
						}
					}
				});

		public void Initialize(PhysicalDevice physicalDevice, SurfaceKhr surface)
		{
			var queueFamilyProperties = physicalDevice.GetQueueFamilyProperties();

			uint queueFamilyUsedIndex;
			for (queueFamilyUsedIndex = 0; queueFamilyUsedIndex < queueFamilyProperties.Length; ++queueFamilyUsedIndex)
			{
				if (!physicalDevice.GetSurfaceSupportKHR(queueFamilyUsedIndex, surface)) continue;

				if (queueFamilyProperties[queueFamilyUsedIndex].QueueFlags.HasFlag(QueueFlags.Graphics)) break;
			}

			var queueInfo = new DeviceQueueCreateInfo
				{ QueuePriorities = new[] { 1.0f }, QueueFamilyIndex = queueFamilyUsedIndex };

			var deviceInfo = new DeviceCreateInfo
			{
				EnabledExtensionNames = new[] { "VK_KHR_swapchain" },
				QueueCreateInfos = new[] { queueInfo }
			};

			device = physicalDevice.CreateDevice(deviceInfo);
			queue = device.GetQueue(0, 0);
			surfaceCapabilities = physicalDevice.GetSurfaceCapabilitiesKHR(surface);
			var surfaceFormat = SelectFormat(physicalDevice, surface);
			swapchain = CreateSwapchain(surface, surfaceFormat);
			images = device.GetSwapchainImagesKHR(swapchain);
			renderPass = CreateRenderPass(surfaceFormat);
			framebuffers = CreateFramebuffers(images, surfaceFormat);
			var fenceInfo = new FenceCreateInfo();
			fence = device.CreateFence(fenceInfo);
			var semaphoreInfo = new SemaphoreCreateInfo();
			semaphore = device.CreateSemaphore(semaphoreInfo);
			initialized = true;
			var vertexBuffer = CreateBuffer(
				physicalDevice,
				Logo.Vertices,
				BufferUsageFlags.VertexBuffer,
				typeof(float));
			var indexBuffer = CreateBuffer(physicalDevice, Logo.Indexes, BufferUsageFlags.IndexBuffer, typeof(short));
			uniformBuffer = CreateUniformBuffer(physicalDevice);
			descriptorSetLayout = CreateDescriptorSetLayout();
			var pipelines = CreatePipelines();
			descriptorSets = CreateDescriptorSets();
			UpdateDescriptorSets();

			commandBuffers = CreateCommandBuffers(
				images,
				framebuffers,
				pipelines[0],
				vertexBuffer,
				indexBuffer,
				(uint)Logo.Indexes.Length);
		}

		private CommandBuffer[] commandBuffers;
		private DescriptorSet[] descriptorSets;
		private DescriptorSetLayout descriptorSetLayout;
		private PipelineLayout pipelineLayout;
		private Buffer uniformBuffer;

		private CommandBuffer[] CreateCommandBuffers(
			IReadOnlyCollection<Image> imgs,
			IReadOnlyList<Framebuffer> bufs,
			VkPipeline pipeline,
			Buffer vertexBuffer,
			Buffer indexBuffer,
			uint indexLength)
		{
			var createPoolInfo = new CommandPoolCreateInfo { Flags = CommandPoolCreateFlags.ResetCommandBuffer };
			var commandPool = device.CreateCommandPool(createPoolInfo);
			var commandBufferAllocateInfo = new CommandBufferAllocateInfo
			{
				Level = CommandBufferLevel.Primary,
				CommandPool = commandPool,
				CommandBufferCount = (uint)imgs.Count
			};
			var buffers = device.AllocateCommandBuffers(commandBufferAllocateInfo);
			var commandBufferBeginInfo = new CommandBufferBeginInfo();

			for (var i = 0; i < imgs.Count; i++)
			{
				buffers[i].Begin(commandBufferBeginInfo);
				var renderPassBeginInfo = new RenderPassBeginInfo
				{
					Framebuffer = bufs[i],
					RenderPass = renderPass,
					ClearValues = new[]
					{
						new ClearValue { Color = new ClearColorValue(new[] { 0.9f, 0.87f, 0.75f, 1.0f }) }
					},
					RenderArea = new Rect2D { Extent = surfaceCapabilities.CurrentExtent }
				};
				buffers[i].CmdBeginRenderPass(renderPassBeginInfo, SubpassContents.Inline);
				buffers[i].CmdBindDescriptorSets(PipelineBindPoint.Graphics, pipelineLayout, 0, descriptorSets, null);
				buffers[i].CmdBindPipeline(PipelineBindPoint.Graphics, pipeline);
				buffers[i].CmdBindVertexBuffer(0, vertexBuffer, 0);
				buffers[i].CmdBindIndexBuffer(indexBuffer, 0, IndexType.Uint16);
				buffers[i].CmdDrawIndexed(indexLength, 1, 0, 0, 0);
				buffers[i].CmdEndRenderPass();
				buffers[i].End();
			}

			return buffers;
		}

		private static byte[] LoadResource(string name)
		{
			var stream = typeof(SampleVkConsumer).GetTypeInfo().Assembly.GetManifestResourceStream(name);
			if (stream == null) throw new InvalidOperationException("couldn't load resource");
			var bytes = new byte[stream.Length];
			stream.Read(bytes, 0, (int)stream.Length);
			return bytes;
		}

		private Buffer CreateBuffer(
			PhysicalDevice physicalDevice,
			object values,
			BufferUsageFlags usageFlags,
			Type type)
		{
			var array = values as Array;
			var length = array?.Length ?? 1;
			var size = Marshal.SizeOf(type) * length;
			var buffer = device.CreateBuffer(
				new BufferCreateInfo
				{
					Size = size,
					Usage = usageFlags,
					SharingMode = SharingMode.Exclusive,
					QueueFamilyIndices = new uint[] { 0 }
				});
			var memoryReq = device.GetBufferMemoryRequirements(buffer);
			var allocInfo = new MemoryAllocateInfo { AllocationSize = memoryReq.Size };
			var memoryProperties = physicalDevice.GetMemoryProperties();
			var heapIndexSet = false;
			for (uint i = 0; i < memoryProperties.MemoryTypeCount; i++)
			{
				if ((memoryProperties.MemoryTypes[i].PropertyFlags & MemoryPropertyFlags.HostVisible) !=
					MemoryPropertyFlags.HostVisible ||
					((memoryReq.MemoryTypeBits >> (int)i) & 1) != 1) continue; //TODO break?
				allocInfo.MemoryTypeIndex = i;
				heapIndexSet = true;
			}

			if (!heapIndexSet) allocInfo.MemoryTypeIndex = memoryProperties.MemoryTypes[0].HeapIndex;
			var deviceMemory = device.AllocateMemory(allocInfo);
			var memPtr = device.MapMemory(deviceMemory, 0, size, 0);
			if (type == typeof(float)) Marshal.Copy(values as float[], 0, memPtr, length);
			else if (type == typeof(short)) Marshal.Copy(values as short[], 0, memPtr, length);
			else if (type == typeof(AreaUniformBuffer)) Marshal.StructureToPtr(values, memPtr, false);
			device.UnmapMemory(deviceMemory);
			device.BindBufferMemory(buffer, deviceMemory, 0);
			return buffer;
		}

		private struct AreaUniformBuffer
		{
			public float width;
			public float height;
		}

		private DescriptorSetLayout CreateDescriptorSetLayout() =>
			device.CreateDescriptorSetLayout(
				new DescriptorSetLayoutCreateInfo
				{
					Bindings = new[]
					{
						new DescriptorSetLayoutBinding
						{
							DescriptorType = DescriptorType.UniformBuffer,
							DescriptorCount = 1,
							StageFlags = ShaderStageFlags.Vertex
						}
					}
				});

		private VkPipeline[] CreatePipelines()
		{
			pipelineLayout = device.CreatePipelineLayout(
				new PipelineLayoutCreateInfo
				{
					SetLayouts = new[] { descriptorSetLayout }
				});
			return device.CreateGraphicsPipelines(
				device.CreatePipelineCache(new PipelineCacheCreateInfo()),
				new[]
				{
					new GraphicsPipelineCreateInfo
					{
						Layout = pipelineLayout,
						ViewportState = new PipelineViewportStateCreateInfo
						{
							Viewports = new[]
							{
								new Viewport
								{
									MinDepth = 0,
									MaxDepth = 1.0f,
									Width = surfaceCapabilities.CurrentExtent.Width,
									Height = surfaceCapabilities.CurrentExtent.Height
								}
							},
							Scissors = new[]
							{
								new Rect2D { Extent = surfaceCapabilities.CurrentExtent }
							}
						},
						Stages = new[]
						{
							new PipelineShaderStageCreateInfo
							{
								Stage = ShaderStageFlags.Vertex,
								Module =
									device.CreateShaderModule(LoadResource("XLogo.Common.Shaders.shader.vert.spv")),
								Name = "main"
							},
							new PipelineShaderStageCreateInfo
							{
								Stage = ShaderStageFlags.Fragment,
								Module =
									device.CreateShaderModule(LoadResource("XLogo.Common.Shaders.shader.frag.spv")),
								Name = "main"
							}
						},
						MultisampleState = new PipelineMultisampleStateCreateInfo
						{
							RasterizationSamples = SampleCountFlags.Count1
						},
						ColorBlendState = new PipelineColorBlendStateCreateInfo
						{
							LogicOp = LogicOp.Copy,
							Attachments = new[]
							{
								new PipelineColorBlendAttachmentState
								{
									ColorWriteMask = ColorComponentFlags.R | ColorComponentFlags.G |
										ColorComponentFlags.B |
										ColorComponentFlags.A
								}
							}
						},
						RasterizationState = new PipelineRasterizationStateCreateInfo
						{
							PolygonMode = PolygonMode.Fill,
							CullMode = (uint)CullModeFlags.None,
							FrontFace = FrontFace.Clockwise,
							LineWidth = 1.0f
						},
						InputAssemblyState = new PipelineInputAssemblyStateCreateInfo
						{
							Topology = PrimitiveTopology.TriangleList
						},
						VertexInputState = new PipelineVertexInputStateCreateInfo
						{
							VertexBindingDescriptions = new[]
							{
								new VertexInputBindingDescription
								{
									Stride = 3 * sizeof(float),
									InputRate = VertexInputRate.Vertex
								}
							},
							VertexAttributeDescriptions = new[]
							{
								new VertexInputAttributeDescription
								{
									Format = Format.R32G32B32Sfloat
								}
							}
						},
						RenderPass = renderPass
					}
				});
		}

		private Buffer CreateUniformBuffer(PhysicalDevice physicalDevice) => CreateBuffer(
			physicalDevice,
			new AreaUniformBuffer
			{
				width = surfaceCapabilities.CurrentExtent.Width,
				height = surfaceCapabilities.CurrentExtent.Height
			},
			BufferUsageFlags.UniformBuffer,
			typeof(AreaUniformBuffer));

		private DescriptorSet[] CreateDescriptorSets() => device.AllocateDescriptorSets(
			new DescriptorSetAllocateInfo
			{
				SetLayouts = new[] { descriptorSetLayout },
				DescriptorPool = device.CreateDescriptorPool(
					new DescriptorPoolCreateInfo
					{
						PoolSizes = new[]
						{
							new DescriptorPoolSize
							{
								Type = DescriptorType.UniformBuffer,
								DescriptorCount = 1
							}
						},
						MaxSets = 1
					})
			});

		private void UpdateDescriptorSets()
		{
			device.UpdateDescriptorSets(
				new[]
				{
					new WriteDescriptorSet
					{
						DstSet = descriptorSets[0],
						DescriptorType = DescriptorType.UniformBuffer,
						BufferInfo = new[]
						{
							new DescriptorBufferInfo
							{
								Buffer = uniformBuffer,
								Offset = 0,
								Range = 2 * sizeof(float)
							}
						}
					}
				},
				null);
		}

		public void DrawFrame()
		{
			if (!initialized) return;
			var nextIndex = device.AcquireNextImageKHR(swapchain, ulong.MaxValue, semaphore);
			device.ResetFence(fence);
			queue.Submit(
				new SubmitInfo
				{
					WaitSemaphores = new[] { semaphore },
					WaitDstStageMask = new[] { PipelineStageFlags.AllGraphics },
					CommandBuffers = new[] { commandBuffers[nextIndex] }
				},
				fence);
			device.WaitForFence(fence, true, 100000000);
			queue.PresentKHR(
				new PresentInfoKhr
				{
					Swapchains = new[] { swapchain },
					ImageIndices = new[] { nextIndex }
				});
		}

		private static class Logo
		{
			public static readonly float[] Vertices =
			{
				0.008181f, -0.000009f, 0.000000f, 0.008618f, 0.000000f, 0.000000f, 0.008376f, 0.000000f, 0.000000f,
				0.009286f, 0.000000f, 0.000000f, 0.010295f, 0.000000f, 0.000000f, 0.011559f, 0.000000f, 0.000000f,
				0.012995f, 0.000000f, 0.000000f, 0.014515f, 0.000000f, 0.000000f, 0.016036f, 0.000000f, 0.000000f,
				0.017471f, 0.000000f, 0.000000f, 0.018735f, 0.000000f, 0.000000f, 0.019744f, 0.000000f, 0.000000f,
				0.020412f, 0.000000f, 0.000000f, 0.020654f, 0.000000f, 0.000000f, 0.020849f, -0.000009f, 0.000000f,
				0.021043f, -0.000037f, 0.000000f, 0.007987f, -0.000037f, 0.000000f, 0.021233f, -0.000081f, 0.000000f,
				0.007797f, -0.000081f, 0.000000f, 0.021419f, -0.000141f, 0.000000f, 0.007611f, -0.000141f, 0.000000f,
				0.021599f, -0.000217f, 0.000000f, 0.007431f, -0.000217f, 0.000000f, 0.021773f, -0.000308f, 0.000000f,
				0.007257f, -0.000308f, 0.000000f, 0.021938f, -0.000412f, 0.000000f, 0.007092f, -0.000412f, 0.000000f,
				0.022094f, -0.000530f, 0.000000f, 0.006936f, -0.000531f, 0.000000f, 0.022240f, -0.000661f, 0.000000f,
				0.006790f, -0.000661f, 0.000000f, 0.022373f, -0.000804f, 0.000000f, 0.006657f, -0.000804f, 0.000000f,
				0.022494f, -0.000957f, 0.000000f, 0.006536f, -0.000957f, 0.000000f, 0.022600f, -0.001122f, 0.000000f,
				0.006430f, -0.001122f, 0.000000f, 0.006348f, -0.001263f, 0.000000f, 0.023151f, -0.002075f, 0.000000f,
				0.006118f, -0.001663f, 0.000000f, 0.005759f, -0.002284f, 0.000000f, 0.023765f, -0.003138f, 0.000000f,
				0.005293f, -0.003089f, 0.000000f, 0.004743f, -0.004042f, 0.000000f, 0.024422f, -0.004276f, 0.000000f,
				0.004128f, -0.005106f, 0.000000f, 0.025101f, -0.005450f, 0.000000f, 0.003471f, -0.006243f, 0.000000f,
				0.008513f, -0.006215f, 0.000000f, 0.020491f, -0.006216f, 0.000000f, 0.025779f, -0.006625f, 0.000000f,
				0.008509f, -0.006215f, 0.000000f, 0.008518f, -0.006215f, 0.000000f, 0.008505f, -0.006215f, 0.000000f,
				0.008522f, -0.006215f, 0.000000f, 0.008501f, -0.006215f, 0.000000f, 0.008526f, -0.006215f, 0.000000f,
				0.008496f, -0.006215f, 0.000000f, 0.008531f, -0.006215f, 0.000000f, 0.008492f, -0.006215f, 0.000000f,
				0.008535f, -0.006215f, 0.000000f, 0.008488f, -0.006216f, 0.000000f, 0.008539f, -0.006216f, 0.000000f,
				0.008447f, -0.006228f, 0.000000f, 0.008567f, -0.006216f, 0.000000f, 0.020491f, -0.006216f, 0.000000f,
				0.008539f, -0.006216f, 0.000000f, 0.008647f, -0.006216f, 0.000000f, 0.008771f, -0.006216f, 0.000000f,
				0.008931f, -0.006216f, 0.000000f, 0.009121f, -0.006216f, 0.000000f, 0.009333f, -0.006216f, 0.000000f,
				0.009560f, -0.006216f, 0.000000f, 0.009794f, -0.006216f, 0.000000f, 0.010028f, -0.006216f, 0.000000f,
				0.010255f, -0.006216f, 0.000000f, 0.010467f, -0.006216f, 0.000000f, 0.010657f, -0.006216f, 0.000000f,
				0.010680f, -0.006217f, 0.000000f, 0.018373f, -0.006216f, 0.000000f, 0.010657f, -0.006216f, 0.000000f,
				0.018373f, -0.006216f, 0.000000f, 0.018349f, -0.006217f, 0.000000f, 0.018401f, -0.006216f, 0.000000f,
				0.018481f, -0.006216f, 0.000000f, 0.018604f, -0.006216f, 0.000000f, 0.018765f, -0.006216f, 0.000000f,
				0.018955f, -0.006216f, 0.000000f, 0.019167f, -0.006216f, 0.000000f, 0.019394f, -0.006216f, 0.000000f,
				0.019628f, -0.006216f, 0.000000f, 0.019862f, -0.006216f, 0.000000f, 0.020089f, -0.006216f, 0.000000f,
				0.020301f, -0.006216f, 0.000000f, 0.020536f, -0.006221f, 0.000000f, 0.018325f, -0.006220f, 0.000000f,
				0.010703f, -0.006221f, 0.000000f, 0.018301f, -0.006225f, 0.000000f, 0.020580f, -0.006235f, 0.000000f,
				0.010726f, -0.006227f, 0.000000f, 0.018278f, -0.006232f, 0.000000f, 0.010748f, -0.006234f, 0.000000f,
				0.008409f, -0.006246f, 0.000000f, 0.018256f, -0.006242f, 0.000000f, 0.010770f, -0.006243f, 0.000000f,
				0.020619f, -0.006256f, 0.000000f, 0.018235f, -0.006253f, 0.000000f, 0.002792f, -0.007418f, 0.000000f,
				0.010790f, -0.006255f, 0.000000f, 0.008375f, -0.006271f, 0.000000f, 0.018214f, -0.006265f, 0.000000f,
				0.010810f, -0.006267f, 0.000000f, 0.020656f, -0.006284f, 0.000000f, 0.018195f, -0.006280f, 0.000000f,
				0.010828f, -0.006282f, 0.000000f, 0.008345f, -0.006301f, 0.000000f, 0.018177f, -0.006296f, 0.000000f,
				0.010846f, -0.006297f, 0.000000f, 0.020687f, -0.006318f, 0.000000f, 0.018161f, -0.006314f, 0.000000f,
				0.010861f, -0.006315f, 0.000000f, 0.008319f, -0.006335f, 0.000000f, 0.018146f, -0.006332f, 0.000000f,
				0.010876f, -0.006333f, 0.000000f, 0.020714f, -0.006356f, 0.000000f, 0.018133f, -0.006353f, 0.000000f,
				0.010888f, -0.006353f, 0.000000f, 0.008298f, -0.006372f, 0.000000f, 0.010936f, -0.006438f, 0.000000f,
				0.017811f, -0.006925f, 0.000000f, 0.020735f, -0.006397f, 0.000000f, 0.008283f, -0.006412f, 0.000000f,
				0.020749f, -0.006441f, 0.000000f, 0.008273f, -0.006454f, 0.000000f, 0.011071f, -0.006678f, 0.000000f,
				0.020757f, -0.006486f, 0.000000f, 0.008269f, -0.006496f, 0.000000f, 0.020756f, -0.006531f, 0.000000f,
				0.008272f, -0.006538f, 0.000000f, 0.020748f, -0.006576f, 0.000000f, 0.008282f, -0.006579f, 0.000000f,
				0.020731f, -0.006618f, 0.000000f, 0.008299f, -0.006618f, 0.000000f, 0.011806f, -0.012868f, 0.000000f,
				0.020684f, -0.006701f, 0.000000f, 0.026437f, -0.007762f, 0.000000f, 0.011281f, -0.007050f, 0.000000f,
				0.020552f, -0.006936f, 0.000000f, 0.017453f, -0.007563f, 0.000000f, 0.020347f, -0.007302f, 0.000000f,
				0.011554f, -0.007534f, 0.000000f, 0.020081f, -0.007776f, 0.000000f, 0.002114f, -0.008592f, 0.000000f,
				0.011876f, -0.008106f, 0.000000f, 0.017069f, -0.008246f, 0.000000f, 0.027051f, -0.008826f, 0.000000f,
				0.019767f, -0.008336f, 0.000000f, 0.012236f, -0.008745f, 0.000000f, 0.016673f, -0.008951f, 0.000000f,
				0.019416f, -0.008962f, 0.000000f, 0.001457f, -0.009730f, 0.000000f, 0.012620f, -0.009428f, 0.000000f,
				0.027602f, -0.009779f, 0.000000f, 0.016277f, -0.009656f, 0.000000f, 0.019040f, -0.009631f, 0.000000f,
				0.013017f, -0.010133f, 0.000000f, 0.018653f, -0.010322f, 0.000000f, 0.015893f, -0.010339f, 0.000000f,
				0.000842f, -0.010794f, 0.000000f, 0.028067f, -0.010584f, 0.000000f, 0.013414f, -0.010838f, 0.000000f,
				0.018265f, -0.011013f, 0.000000f, 0.015534f, -0.010978f, 0.000000f, 0.028426f, -0.011206f, 0.000000f,
				0.000291f, -0.011747f, 0.000000f, 0.013799f, -0.011520f, 0.000000f, 0.015213f, -0.011550f, 0.000000f,
				0.017890f, -0.011682f, 0.000000f, 0.028657f, -0.011605f, 0.000000f, 0.014159f, -0.012159f, 0.000000f,
				0.014941f, -0.012034f, 0.000000f, 0.028739f, -0.011747f, 0.000000f, 0.017539f, -0.012308f, 0.000000f,
				0.000202f, -0.011920f, 0.000000f, 0.028828f, -0.011920f, 0.000000f, 0.000130f, -0.012101f, 0.000000f,
				0.028901f, -0.012101f, 0.000000f, 0.014732f, -0.012406f, 0.000000f, 0.000073f, -0.012288f, 0.000000f,
				0.028957f, -0.012288f, 0.000000f, 0.014481f, -0.012731f, 0.000000f, 0.000032f, -0.012479f, 0.000000f,
				0.028998f, -0.012479f, 0.000000f, 0.017224f, -0.012868f, 0.000000f, 0.014597f, -0.012646f, 0.000000f,
				0.000008f, -0.012673f, 0.000000f, 0.029022f, -0.012673f, 0.000000f, 0.014549f, -0.012731f, 0.000000f,
				-0.000000f, -0.012868f, 0.000000f, 0.029030f, -0.012868f, 0.000000f, 0.014485f, -0.012739f, 0.000000f,
				0.014545f, -0.012739f, 0.000000f, 0.014541f, -0.012747f, 0.000000f, 0.014489f, -0.012747f, 0.000000f,
				0.014537f, -0.012755f, 0.000000f, 0.014493f, -0.012755f, 0.000000f, 0.014533f, -0.012764f, 0.000000f,
				0.014497f, -0.012764f, 0.000000f, 0.014530f, -0.012772f, 0.000000f, 0.014500f, -0.012772f, 0.000000f,
				0.014527f, -0.012781f, 0.000000f, 0.014503f, -0.012781f, 0.000000f, 0.014524f, -0.012790f, 0.000000f,
				0.014506f, -0.012790f, 0.000000f, 0.014522f, -0.012798f, 0.000000f, 0.014508f, -0.012798f, 0.000000f,
				0.014520f, -0.012807f, 0.000000f, 0.014510f, -0.012807f, 0.000000f, 0.014518f, -0.012816f, 0.000000f,
				0.014512f, -0.012816f, 0.000000f, 0.014516f, -0.012825f, 0.000000f, 0.014514f, -0.012825f, 0.000000f,
				0.014515f, -0.012834f, 0.000000f, 0.000008f, -0.013064f, 0.000000f, 0.029022f, -0.013064f, 0.000000f,
				0.011491f, -0.013428f, 0.000000f, 0.020731f, -0.019110f, 0.000000f, 0.014514f, -0.012903f, 0.000000f,
				0.014516f, -0.012903f, 0.000000f, 0.014515f, -0.012894f, 0.000000f, 0.014518f, -0.012912f, 0.000000f,
				0.014512f, -0.012912f, 0.000000f, 0.014520f, -0.012921f, 0.000000f, 0.014510f, -0.012921f, 0.000000f,
				0.014522f, -0.012930f, 0.000000f, 0.014508f, -0.012930f, 0.000000f, 0.014524f, -0.012938f, 0.000000f,
				0.014506f, -0.012938f, 0.000000f, 0.014527f, -0.012947f, 0.000000f, 0.014503f, -0.012947f, 0.000000f,
				0.014530f, -0.012956f, 0.000000f, 0.014500f, -0.012956f, 0.000000f, 0.014533f, -0.012964f, 0.000000f,
				0.014497f, -0.012964f, 0.000000f, 0.014537f, -0.012972f, 0.000000f, 0.014493f, -0.012972f, 0.000000f,
				0.014541f, -0.012981f, 0.000000f, 0.014489f, -0.012981f, 0.000000f, 0.014545f, -0.012989f, 0.000000f,
				0.014485f, -0.012989f, 0.000000f, 0.014549f, -0.012997f, 0.000000f, 0.014481f, -0.012997f, 0.000000f,
				0.014433f, -0.013082f, 0.000000f, 0.014871f, -0.013569f, 0.000000f, 0.000032f, -0.013257f, 0.000000f,
				0.028998f, -0.013257f, 0.000000f, 0.014298f, -0.013321f, 0.000000f, 0.000073f, -0.013449f, 0.000000f,
				0.028957f, -0.013449f, 0.000000f, 0.014088f, -0.013694f, 0.000000f, 0.011140f, -0.014053f, 0.000000f,
				0.000130f, -0.013635f, 0.000000f, 0.028901f, -0.013635f, 0.000000f, 0.015229f, -0.014207f, 0.000000f,
				0.000202f, -0.013816f, 0.000000f, 0.028828f, -0.013816f, 0.000000f, 0.013815f, -0.014178f, 0.000000f,
				0.000291f, -0.013990f, 0.000000f, 0.028739f, -0.013990f, 0.000000f, 0.000373f, -0.014131f, 0.000000f,
				0.028188f, -0.014943f, 0.000000f, 0.010765f, -0.014721f, 0.000000f, 0.000604f, -0.014531f, 0.000000f,
				0.013493f, -0.014750f, 0.000000f, 0.015613f, -0.014890f, 0.000000f, 0.000963f, -0.015152f, 0.000000f,
				0.010377f, -0.015411f, 0.000000f, 0.013134f, -0.015389f, 0.000000f, 0.016009f, -0.015595f, 0.000000f,
				0.027573f, -0.016007f, 0.000000f, 0.001428f, -0.015957f, 0.000000f, 0.012749f, -0.016071f, 0.000000f,
				0.009990f, -0.016101f, 0.000000f, 0.016405f, -0.016300f, 0.000000f, 0.001979f, -0.016910f, 0.000000f,
				0.026916f, -0.017144f, 0.000000f, 0.012352f, -0.016776f, 0.000000f, 0.009614f, -0.016769f, 0.000000f,
				0.016789f, -0.016983f, 0.000000f, 0.009263f, -0.017394f, 0.000000f, 0.011955f, -0.017481f, 0.000000f,
				0.002593f, -0.017974f, 0.000000f, 0.017148f, -0.017622f, 0.000000f, 0.026238f, -0.018318f, 0.000000f,
				0.008949f, -0.017954f, 0.000000f, 0.011570f, -0.018164f, 0.000000f, 0.017469f, -0.018194f, 0.000000f,
				0.008683f, -0.018427f, 0.000000f, 0.003251f, -0.019112f, 0.000000f, 0.011211f, -0.018803f, 0.000000f,
				0.017741f, -0.018677f, 0.000000f, 0.025559f, -0.019493f, 0.000000f, 0.008478f, -0.018792f, 0.000000f,
				0.017950f, -0.019050f, 0.000000f, 0.008346f, -0.019027f, 0.000000f, 0.010888f, -0.019375f, 0.000000f,
				0.008299f, -0.019110f, 0.000000f, 0.018085f, -0.019290f, 0.000000f, 0.008280f, -0.019153f, 0.000000f,
				0.020750f, -0.019153f, 0.000000f, 0.003929f, -0.020286f, 0.000000f, 0.020760f, -0.019198f, 0.000000f,
				0.008270f, -0.019198f, 0.000000f, 0.020761f, -0.019244f, 0.000000f, 0.008269f, -0.019244f, 0.000000f,
				0.020754f, -0.019290f, 0.000000f, 0.008276f, -0.019291f, 0.000000f, 0.018133f, -0.019375f, 0.000000f,
				0.020740f, -0.019335f, 0.000000f, 0.008290f, -0.019336f, 0.000000f, 0.020719f, -0.019378f, 0.000000f,
				0.008311f, -0.019378f, 0.000000f, 0.010876f, -0.019396f, 0.000000f, 0.018145f, -0.019396f, 0.000000f,
				0.020692f, -0.019417f, 0.000000f, 0.008338f, -0.019417f, 0.000000f, 0.010862f, -0.019415f, 0.000000f,
				0.018160f, -0.019416f, 0.000000f, 0.010847f, -0.019433f, 0.000000f, 0.018176f, -0.019434f, 0.000000f,
				0.020660f, -0.019452f, 0.000000f, 0.008370f, -0.019452f, 0.000000f, 0.010830f, -0.019449f, 0.000000f,
				0.018194f, -0.019451f, 0.000000f, 0.010811f, -0.019464f, 0.000000f, 0.018213f, -0.019466f, 0.000000f,
				0.020623f, -0.019480f, 0.000000f, 0.008407f, -0.019480f, 0.000000f, 0.010792f, -0.019478f, 0.000000f,
				0.018233f, -0.019480f, 0.000000f, 0.010771f, -0.019490f, 0.000000f, 0.018255f, -0.019492f, 0.000000f,
				0.020582f, -0.019502f, 0.000000f, 0.008448f, -0.019502f, 0.000000f, 0.010749f, -0.019500f, 0.000000f,
				0.018277f, -0.019502f, 0.000000f, 0.024902f, -0.020630f, 0.000000f, 0.010727f, -0.019508f, 0.000000f,
				0.018300f, -0.019510f, 0.000000f, 0.020537f, -0.019516f, 0.000000f, 0.008492f, -0.019516f, 0.000000f,
				0.010704f, -0.019514f, 0.000000f, 0.018324f, -0.019516f, 0.000000f, 0.010681f, -0.019519f, 0.000000f,
				0.018348f, -0.019519f, 0.000000f, 0.020491f, -0.019521f, 0.000000f, 0.008539f, -0.019521f, 0.000000f,
				0.010657f, -0.019521f, 0.000000f, 0.018373f, -0.019521f, 0.000000f, 0.008729f, -0.019521f, 0.000000f,
				0.008941f, -0.019521f, 0.000000f, 0.009168f, -0.019521f, 0.000000f, 0.009402f, -0.019521f, 0.000000f,
				0.009636f, -0.019521f, 0.000000f, 0.009863f, -0.019521f, 0.000000f, 0.010075f, -0.019521f, 0.000000f,
				0.010265f, -0.019521f, 0.000000f, 0.010425f, -0.019521f, 0.000000f, 0.010549f, -0.019521f, 0.000000f,
				0.010629f, -0.019521f, 0.000000f, 0.018563f, -0.019521f, 0.000000f, 0.018775f, -0.019521f, 0.000000f,
				0.019002f, -0.019521f, 0.000000f, 0.019236f, -0.019521f, 0.000000f, 0.019470f, -0.019521f, 0.000000f,
				0.019696f, -0.019521f, 0.000000f, 0.019908f, -0.019521f, 0.000000f, 0.020098f, -0.019521f, 0.000000f,
				0.020259f, -0.019521f, 0.000000f, 0.020383f, -0.019521f, 0.000000f, 0.020462f, -0.019521f, 0.000000f,
				0.004608f, -0.021460f, 0.000000f, 0.024287f, -0.021694f, 0.000000f, 0.005265f, -0.022598f, 0.000000f,
				0.023737f, -0.022647f, 0.000000f, 0.005879f, -0.023662f, 0.000000f, 0.023271f, -0.023453f, 0.000000f,
				0.022913f, -0.024074f, 0.000000f, 0.006430f, -0.024615f, 0.000000f, 0.022682f, -0.024473f, 0.000000f,
				0.022600f, -0.024615f, 0.000000f, 0.006536f, -0.024779f, 0.000000f, 0.022494f, -0.024779f, 0.000000f,
				0.006657f, -0.024933f, 0.000000f, 0.022373f, -0.024933f, 0.000000f, 0.022240f, -0.025075f, 0.000000f,
				0.006790f, -0.025075f, 0.000000f, 0.022094f, -0.025206f, 0.000000f, 0.006936f, -0.025206f, 0.000000f,
				0.021938f, -0.025324f, 0.000000f, 0.007092f, -0.025324f, 0.000000f, 0.021773f, -0.025429f, 0.000000f,
				0.007257f, -0.025429f, 0.000000f, 0.021599f, -0.025519f, 0.000000f, 0.007431f, -0.025519f, 0.000000f,
				0.021419f, -0.025595f, 0.000000f, 0.007611f, -0.025595f, 0.000000f, 0.021233f, -0.025656f, 0.000000f,
				0.007797f, -0.025656f, 0.000000f, 0.021043f, -0.025700f, 0.000000f, 0.007987f, -0.025700f, 0.000000f,
				0.020849f, -0.025727f, 0.000000f, 0.008181f, -0.025727f, 0.000000f, 0.020654f, -0.025736f, 0.000000f,
				0.008376f, -0.025736f, 0.000000f, 0.008540f, -0.025736f, 0.000000f, 0.020654f, -0.025736f, 0.000000f,
				0.008376f, -0.025736f, 0.000000f, 0.009002f, -0.025736f, 0.000000f, 0.009719f, -0.025736f, 0.000000f,
				0.010650f, -0.025736f, 0.000000f, 0.011751f, -0.025736f, 0.000000f, 0.012980f, -0.025736f, 0.000000f,
				0.014295f, -0.025736f, 0.000000f, 0.015652f, -0.025736f, 0.000000f, 0.017009f, -0.025736f, 0.000000f,
				0.018323f, -0.025736f, 0.000000f, 0.019552f, -0.025736f, 0.000000f
			};

			public static readonly short[] Indexes =
			{
				0, 1, 2, 0, 3, 1, 0, 4, 3, 0, 5, 4, 0, 6, 5, 0, 7, 6, 0, 8, 7, 0, 9, 8, 0, 10, 9, 0, 11, 10, 0, 12, 11,
				0, 13, 12, 0, 14, 13, 0, 15, 14, 16, 15, 0, 16, 17, 15, 18, 17, 16, 18, 19, 17, 20, 19, 18, 20, 21, 19,
				22, 21, 20, 22, 23, 21, 24, 23, 22, 24, 25, 23, 26, 25, 24, 26, 27, 25, 28, 27, 26, 28, 29, 27, 30, 29,
				28, 30, 31, 29, 32, 31, 30, 32, 33, 31, 34, 33, 32, 34, 35, 33, 36, 35, 34, 37, 35, 36, 37, 38, 35, 39,
				38, 37, 40, 38, 39, 40, 41, 38, 42, 41, 40, 43, 41, 42, 43, 44, 41, 45, 44, 43, 45, 46, 44, 47, 46, 45,
				47, 48, 46, 48, 49, 46, 49, 50, 46, 47, 51, 48, 52, 49, 48, 47, 53, 51, 54, 49, 52, 47, 55, 53, 56, 49,
				54, 47, 57, 55, 58, 49, 56, 47, 59, 57, 60, 49, 58, 47, 61, 59, 62, 49, 60, 47, 63, 61, 64, 65, 66, 67,
				65, 64, 68, 65, 67, 69, 65, 68, 70, 65, 69, 71, 65, 70, 72, 65, 71, 73, 65, 72, 74, 65, 73, 75, 65, 74,
				76, 65, 75, 77, 65, 76, 78, 79, 80, 81, 65, 77, 78, 82, 79, 83, 65, 81, 84, 65, 83, 85, 65, 84, 86, 65,
				85, 87, 65, 86, 88, 65, 87, 89, 65, 88, 90, 65, 89, 91, 65, 90, 92, 65, 91, 93, 65, 92, 94, 50, 49, 78,
				95, 82, 96, 95, 78, 96, 97, 95, 98, 50, 94, 99, 97, 96, 99, 100, 97, 101, 100, 99, 47, 102, 63, 101,
				103, 100, 104, 103, 101, 105, 50, 98, 104, 106, 103, 107, 102, 47, 108, 106, 104, 107, 109, 102, 108,
				110, 106, 111, 110, 108, 112, 50, 105, 111, 113, 110, 114, 113, 111, 107, 115, 109, 114, 116, 113, 117,
				116, 114, 118, 50, 112, 117, 119, 116, 120, 119, 117, 107, 121, 115, 120, 122, 119, 123, 122, 120, 124,
				50, 118, 123, 125, 122, 126, 125, 123, 107, 127, 121, 128, 125, 126, 128, 129, 125, 130, 50, 124, 107,
				131, 127, 132, 50, 130, 107, 133, 131, 134, 129, 128, 135, 50, 132, 107, 136, 133, 137, 50, 135, 107,
				138, 136, 139, 50, 137, 107, 140, 138, 141, 50, 139, 107, 142, 140, 107, 143, 142, 144, 50, 141, 144,
				145, 50, 146, 129, 134, 147, 145, 144, 146, 148, 129, 149, 145, 147, 150, 148, 146, 151, 145, 149, 152,
				143, 107, 153, 148, 150, 153, 154, 148, 151, 155, 145, 156, 155, 151, 157, 154, 153, 157, 158, 154, 159,
				155, 156, 160, 143, 152, 161, 158, 157, 159, 162, 155, 161, 163, 158, 164, 162, 159, 165, 163, 161, 166,
				162, 164, 165, 167, 163, 168, 143, 160, 166, 169, 162, 170, 167, 165, 171, 169, 166, 170, 172, 167, 171,
				173, 169, 174, 143, 168, 175, 172, 170, 175, 176, 172, 177, 173, 171, 177, 178, 173, 179, 176, 175, 179,
				180, 176, 177, 181, 178, 182, 181, 177, 183, 143, 174, 182, 184, 181, 185, 143, 183, 182, 186, 184, 179,
				187, 180, 188, 143, 185, 182, 189, 186, 190, 187, 179, 191, 143, 188, 182, 192, 189, 193, 192, 182, 190,
				194, 187, 195, 143, 191, 193, 196, 192, 190, 197, 194, 198, 143, 195, 193, 199, 196, 200, 197, 190, 200,
				201, 197, 200, 202, 201, 203, 202, 200, 203, 204, 202, 205, 204, 203, 205, 206, 204, 207, 206, 205, 207,
				208, 206, 209, 208, 207, 209, 210, 208, 211, 210, 209, 211, 212, 210, 213, 212, 211, 213, 214, 212, 215,
				214, 213, 215, 216, 214, 217, 216, 215, 217, 218, 216, 219, 218, 217, 219, 220, 218, 221, 220, 219, 221,
				222, 220, 223, 143, 198, 193, 224, 199, 223, 225, 143, 226, 224, 193, 227, 228, 229, 227, 230, 228, 231,
				230, 227, 231, 232, 230, 233, 232, 231, 233, 234, 232, 235, 234, 233, 235, 236, 234, 237, 236, 235, 237,
				238, 236, 239, 238, 237, 239, 240, 238, 241, 240, 239, 241, 242, 240, 243, 242, 241, 243, 244, 242, 245,
				244, 243, 245, 246, 244, 247, 246, 245, 247, 248, 246, 249, 248, 247, 249, 250, 248, 251, 250, 249, 252,
				250, 251, 252, 253, 250, 254, 225, 223, 226, 255, 224, 256, 253, 252, 257, 225, 254, 226, 258, 255, 259,
				253, 256, 257, 260, 225, 261, 260, 257, 226, 262, 258, 259, 263, 253, 264, 260, 261, 226, 265, 262, 266,
				263, 259, 267, 260, 264, 226, 268, 265, 269, 260, 267, 226, 270, 268, 269, 271, 260, 272, 271, 269, 273,
				263, 266, 273, 274, 263, 275, 271, 272, 275, 276, 271, 277, 274, 273, 277, 278, 274, 226, 279, 270, 280,
				276, 275, 281, 278, 277, 280, 282, 276, 281, 283, 278, 284, 282, 280, 226, 285, 279, 286, 283, 281, 284,
				287, 282, 286, 288, 283, 284, 289, 287, 290, 288, 286, 291, 289, 284, 290, 292, 288, 226, 293, 285, 291,
				294, 289, 295, 292, 290, 295, 296, 292, 291, 297, 294, 298, 297, 291, 299, 296, 295, 299, 300, 296, 226,
				301, 293, 298, 302, 297, 299, 303, 300, 298, 304, 302, 305, 303, 299, 298, 306, 304, 305, 307, 303, 298,
				308, 306, 309, 301, 226, 310, 308, 298, 311, 301, 309, 310, 312, 308, 313, 301, 311, 310, 314, 312, 315,
				301, 313, 310, 316, 314, 305, 317, 307, 318, 301, 315, 310, 319, 316, 320, 301, 318, 310, 321, 319, 322,
				317, 305, 322, 323, 317, 324, 301, 320, 310, 325, 321, 326, 323, 322, 326, 327, 323, 328, 327, 326, 328,
				329, 327, 330, 301, 324, 310, 331, 325, 332, 329, 328, 332, 333, 329, 334, 333, 332, 334, 335, 333, 336,
				301, 330, 310, 337, 331, 338, 335, 334, 338, 339, 335, 340, 339, 338, 340, 341, 339, 342, 301, 336, 310,
				343, 337, 344, 341, 340, 344, 345, 341, 342, 346, 301, 347, 345, 344, 347, 348, 345, 349, 346, 342, 310,
				350, 343, 351, 348, 347, 351, 352, 348, 353, 352, 351, 353, 354, 352, 355, 346, 349, 310, 356, 350, 357,
				354, 353, 357, 358, 354, 310, 359, 356, 310, 360, 359, 310, 361, 360, 310, 362, 361, 310, 363, 362, 310,
				364, 363, 310, 365, 364, 310, 366, 365, 310, 367, 366, 310, 368, 367, 310, 369, 368, 310, 357, 369, 310,
				358, 357, 310, 370, 358, 310, 371, 370, 310, 372, 371, 310, 373, 372, 310, 374, 373, 310, 375, 374, 310,
				376, 375, 310, 377, 376, 310, 378, 377, 310, 379, 378, 310, 380, 379, 310, 355, 380, 310, 346, 355, 381,
				346, 310, 381, 382, 346, 383, 382, 381, 383, 384, 382, 385, 384, 383, 385, 386, 384, 385, 387, 386, 388,
				387, 385, 388, 389, 387, 388, 390, 389, 391, 390, 388, 391, 392, 390, 393, 392, 391, 393, 394, 392, 393,
				395, 394, 396, 395, 393, 396, 397, 395, 398, 397, 396, 398, 399, 397, 400, 399, 398, 400, 401, 399, 402,
				401, 400, 402, 403, 401, 404, 403, 402, 404, 405, 403, 406, 405, 404, 406, 407, 405, 408, 407, 406, 408,
				409, 407, 410, 409, 408, 410, 411, 409, 412, 411, 410, 412, 413, 411, 414, 413, 412, 415, 416, 417, 418,
				416, 415, 419, 416, 418, 420, 416, 419, 421, 416, 420, 422, 416, 421, 423, 416, 422, 424, 416, 423, 425,
				416, 424, 426, 416, 425, 427, 416, 426
			};
		}
	}
}