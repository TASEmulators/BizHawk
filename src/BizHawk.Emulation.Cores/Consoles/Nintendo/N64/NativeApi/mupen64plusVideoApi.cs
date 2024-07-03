using System.Collections.Generic;
using System.Runtime.InteropServices;
using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.N64.NativeApi
{
	internal class mupen64plusVideoApi
	{
		private IntPtr GfxDll;

		/// <summary>
		/// Fills a provided buffer with the mupen64plus framebuffer
		/// </summary>
		/// <param name="framebuffer">The buffer to fill</param>
		/// <param name="width">A pointer to a variable to fill with the width of the framebuffer</param>
		/// <param name="height">A pointer to a variable to fill with the height of the framebuffer</param>
		/// <param name="buffer">Which buffer to read: 0 = front, 1 = back</param>
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void ReadScreen2(int[] framebuffer, ref int width, ref int height, int buffer);

		private readonly ReadScreen2 GFXReadScreen2;

		/// <summary>
		/// Gets the width and height of the mupen64plus framebuffer
		/// </summary>
		/// <param name="dummy">Use IntPtr.Zero</param>
		/// <param name="width">A pointer to a variable to fill with the width of the framebuffer</param>
		/// <param name="height">A pointer to a variable to fill with the height of the framebuffer</param>
		/// <param name="buffer">Which buffer to read: 0 = front, 1 = back</param>
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void ReadScreen2Res(IntPtr dummy, ref int width, ref int height, int buffer);

		private readonly ReadScreen2Res GFXReadScreen2Res;

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate int GetScreenTextureID();

		private GetScreenTextureID GFXGetScreenTextureID;

		public mupen64plusVideoApi(mupen64plusApi core, VideoPluginSettings settings)
		{
			string videoplugin;
			switch (settings.Plugin)
			{
				default:
				case PluginType.Rice:
					videoplugin = "mupen64plus-video-rice.dll";
					break;
				case PluginType.Glide:
					videoplugin = "mupen64plus-video-glide64.dll";
					break;
				case PluginType.GlideMk2:
					videoplugin = "mupen64plus-video-glide64mk2.dll";
					break;
				case PluginType.GLideN64:
					videoplugin = "mupen64plus-video-GLideN64.dll";
					break;
				case PluginType.Angrylion:
					videoplugin = "mupen64plus-video-angrylion-rdp.dll";
					break;
			}

			GfxDll = core.AttachPlugin(mupen64plusApi.m64p_plugin_type.M64PLUGIN_GFX,
				videoplugin);
			GFXReadScreen2 = mupen64plusApi.GetTypedDelegate<ReadScreen2>(GfxDll, "ReadScreen2");
			GFXReadScreen2Res = mupen64plusApi.GetTypedDelegate<ReadScreen2Res>(GfxDll, "ReadScreen2");
			var funcPtr = OSTailoredCode.LinkedLibManager.GetProcAddrOrZero(GfxDll, "GetScreenTextureID");
			if (funcPtr != IntPtr.Zero) GFXGetScreenTextureID = (GetScreenTextureID) Marshal.GetDelegateForFunctionPointer(funcPtr, typeof(GetScreenTextureID));
		}

		public void GetScreenDimensions(ref int width, ref int height)
		{
			GFXReadScreen2Res(IntPtr.Zero, ref width, ref height, 0);
		}

		private int[] m64pBuffer = new int[0];
		/// <summary>
		/// This function copies the frame buffer from mupen64plus
		/// </summary>
		public void Getm64pFrameBuffer(int[] buffer, ref int width, ref int height)
		{
			if (m64pBuffer.Length != width * height)
				m64pBuffer = new int[width * height];
			// Actually get the frame buffer
			GFXReadScreen2(m64pBuffer, ref width, ref height, 0);

			// vflip
			int fromindex = width * (height - 1) * 4;
			int toindex = 0;

			for (int j = 0; j < height; j++)
			{
				Buffer.BlockCopy(m64pBuffer, fromindex, buffer, toindex, width * 4);
				fromindex -= width * 4;
				toindex += width * 4;
			}

			// opaque
			unsafe
			{
				fixed (int* ptr = &buffer[0])
				{
					int l = buffer.Length;
					for (int i = 0; i < l; i++)
					{
						ptr[i] |= unchecked((int)0xff000000);
					}
				}
			}
		}
	}


	public class VideoPluginSettings
	{
		public PluginType Plugin;
		//public Dictionary<string, int> IntParameters = new Dictionary<string,int>();
		//public Dictionary<string, string> StringParameters = new Dictionary<string,string>();

		public Dictionary<string, object> Parameters = new Dictionary<string, object>();
		public int Height;
		public int Width;

		public VideoPluginSettings(PluginType Plugin, int Width, int Height)
		{
			this.Plugin = Plugin;
			this.Width = Width;
			this.Height = Height;
		}
	}
}
