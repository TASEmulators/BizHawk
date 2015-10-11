#include "d3d8Wrapper.h"

D3D8Base::LPDIRECT3D8 g_D3D=NULL;

HMODULE realDLL;

extern "C"
{
	namespace D3D8Wrapper
	{	
		IDirect3DDevice8 *last_device = NULL;
		IDirect3DSurface8 *render_surface = NULL;
		void (*rendering_callback)( int );

		D3D8Wrapper::IDirect3D8* WINAPI Direct3DCreate8(UINT Version)
		{
			//sometimes, Intel drivers will clear the dll path. So let's save and restore it (do their job for them)
			char oldDllDirectory[MAX_PATH];
			GetDllDirectory(MAX_PATH, oldDllDirectory);

			// Get the real DLL path from the system directory, needs to be specific to avoid binding to the d3d8.dll we're in now!
			// Might be unsafe
			CHAR dll_path[1024];
			GetSystemDirectory(dll_path,1024);
			strcat(dll_path,"\\d3d8.dll");

			realDLL = LoadLibrary(dll_path);

			D3D8Wrapper::D3DCREATE realDirect3DCreate8 = (D3D8Wrapper::D3DCREATE)GetProcAddress(realDLL, "Direct3DCreate8");

			// Use the real Direct3DCreate8 to make the base object
			D3D8Base::IDirect3D8* realD3D = realDirect3DCreate8(D3D_SDK_VERSION);

			// Wrap the object
			D3D8Wrapper::IDirect3D8* wrappedD3D = D3D8Wrapper::IDirect3D8::GetDirect3D(realD3D);

			//restore old DLL directory
			SetDllDirectory(oldDllDirectory);

			return wrappedD3D;
		}
	}


	__declspec(dllexport) void __cdecl CloseDLL()
	{
		FreeLibrary(realDLL);
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

			//resolve rendertarget to a system memory texture, for locking
			//TODO! UNACCEPTABLE CODE! THIS IS HORRIBLE!
			//X8R8G8B8 or fail -- A8R8G8B8 will malfunction (is it because the source surface format isnt matching? I think so)
			static D3D8Wrapper::IDirect3DTexture8* tex = NULL;
			static RECT texRect;
			if(!tex || (texRect.right != entire_buffer.right) || (texRect.bottom != entire_buffer.bottom))
			{
				if(tex)
					tex->Release();
				texRect = entire_buffer;
				D3D8Wrapper::last_device->CreateTexture(desc.Width, desc.Height, 1, 0, D3D8Base::D3DFMT_X8R8G8B8, D3D8Base::D3DPOOL_SYSTEMMEM, &tex);
			}

			D3D8Wrapper::IDirect3DSurface8* surf;
			tex->GetSurfaceLevel(0,&surf);
			HRESULT hr = D3D8Wrapper::last_device->CopyRects(D3D8Wrapper::render_surface,NULL,0,surf,NULL);

			// make a D3DLOCKED_RECT, pass to LockRect
			D3D8Base::D3DLOCKED_RECT locked;
			hr = surf->LockRect(&locked,&entire_buffer,D3DLOCK_READONLY);

			//this loop was reversed from the original.
			//it should be faster anyway if anything since the reading can be prefetched forwardly.
			//TODO - allow bizhawk to handle flipped images.... nonetheless, it might not handle images with unusual pitches, although maybe we should consider that.
			//so this code will probably remain for quite some time
			int dest_row = desc.Height - 1;
			for (UINT from_row = 0; from_row < desc.Height; from_row++)
			{
				memcpy((char*)dest + (dest_row * desc.Width*4),(char*)locked.pBits + from_row * locked.Pitch, desc.Width*4);
				dest_row--;
			}

			// unlock rect
			surf->UnlockRect();
			surf->Release();
		}

		// release the surface
		//backbuffer->Release();
		
		// we're done, maybe?
	}
}