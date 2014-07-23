#include "d3d8Wrapper.h"

D3D8Base::LPDIRECT3D8 g_D3D=NULL;

HMODULE hD3D;

ThreadSafePointerSet D3D8Wrapper::IDirect3DDevice8::m_List;
ThreadSafePointerSet D3D8Wrapper::IDirect3DBaseTexture8::m_List;
ThreadSafePointerSet D3D8Wrapper::IDirect3DVolumeTexture8::m_List;
ThreadSafePointerSet D3D8Wrapper::IDirect3DCubeTexture8::m_List;
ThreadSafePointerSet D3D8Wrapper::IDirect3DVertexBuffer8::m_List;
ThreadSafePointerSet D3D8Wrapper::IDirect3DIndexBuffer8::m_List;
ThreadSafePointerSet D3D8Wrapper::IDirect3DSurface8::m_List;
ThreadSafePointerSet D3D8Wrapper::IDirect3DVolume8::m_List;
ThreadSafePointerSet D3D8Wrapper::IDirect3DSwapChain8::m_List;

extern "C"
{
	namespace D3D8Wrapper
	{	
		IDirect3DDevice8 *last_device = NULL;
		IDirect3DSurface8 *render_surface = NULL;
		void (*rendering_callback)( int );

		D3D8Wrapper::IDirect3D8* WINAPI Direct3DCreate8(UINT Version)
		{
			// Get the real DLL path
			// Might be unsafe
			CHAR dll_path[1024];
			GetSystemDirectory(dll_path,1024);
			strcat(dll_path,"\\d3d8.dll");

			hD3D = LoadLibrary(dll_path);

			D3D8Wrapper::D3DCREATE pCreate = (D3D8Wrapper::D3DCREATE)GetProcAddress(hD3D, "Direct3DCreate8");

			// Use the real Direct3DCreate8 to make the base object
			D3D8Base::IDirect3D8* pD3D = pCreate(D3D_SDK_VERSION);

			// Wrap the object
			D3D8Wrapper::IDirect3D8* fD3D = D3D8Wrapper::IDirect3D8::GetDirect3D(pD3D);

			return fD3D;
		}
	}


	__declspec(dllexport) void __cdecl CloseDLL()
	{
		FreeLibrary(hD3D);
	}
	
	__declspec(dllexport) void __cdecl SetRenderingCallback(void (*callback)(int))
	{
		D3D8Wrapper::rendering_callback = callback;
	}

	__declspec(dllexport) void __cdecl ReadScreen(void *dest, int *width, int *height)
	{
		if (D3D8Wrapper::last_device == NULL)
		{
			*width = 0;
			*height = 0;
			return;
		}

		// get back buffer (surface)
		//D3D8Base::IDirect3DSurface8 *backbuffer;
		//D3D8Wrapper::last_device->GetD3D8Device()->GetBackBuffer(0,D3D8Base::D3DBACKBUFFER_TYPE_MONO,&backbuffer);

		// surface...
		// make a D3DSURFACE_DESC, pass to GetDesc
		D3D8Base::D3DSURFACE_DESC desc;
		D3D8Wrapper::render_surface->GetDesc(&desc);

		// get out height/width
		*width = desc.Width;
		*height = desc.Height;

		// if dest isn't null
		if (dest != NULL)
		{
			// make a RECT with size of buffer
			RECT entire_buffer;
			entire_buffer.left = 0;
			entire_buffer.top = 0;
			entire_buffer.right = desc.Width;
			entire_buffer.bottom = desc.Height;
		
			// make a D3DLOCKED_RECT, pass to LockRect
			D3D8Base::D3DLOCKED_RECT locked;
			HRESULT hr = D3D8Wrapper::render_surface->LockRect(&locked,&entire_buffer,D3DLOCK_READONLY);

			// read out pBits from the LOCKED_RECT
			int from_row = desc.Height - 1;
			for (int dest_row = 0; dest_row < desc.Height; dest_row++)
			{
				for (int col = 0; col < desc.Width*4; col++)
				{
					((char *)dest)[dest_row * desc.Width * 4 + col] = ((char *)locked.pBits)[from_row * desc.Width * 4 + col];
				}
				from_row--;
			}

			// unlock rect
			D3D8Wrapper::render_surface->UnlockRect();
		}

		// release the surface
		//backbuffer->Release();
		
		// we're done, maybe?
	}
}