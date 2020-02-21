//	VirtualDub - Video processing and capture application
//	A/V interface library
//	Copyright (C) 1998-2005 Avery Lee
//
//	This program is free software; you can redistribute it and/or modify
//	it under the terms of the GNU General Public License as published by
//	the Free Software Foundation; either version 2 of the License, or
//	(at your option) any later version.
//
//	This program is distributed in the hope that it will be useful,
//	but WITHOUT ANY WARRANTY; without even the implied warranty of
//	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//	GNU General Public License for more details.
//
//	You should have received a copy of the GNU General Public License
//	along with this program; if not, write to the Free Software
//	Foundation, Inc., 675 Mass Ave, Cambridge, MA 02139, USA.
//
//
//	This file is the DirectX 9 driver for the video display subsystem.
//	It does traditional point sampled and bilinearly filtered upsampling
//	as well as a special multipass algorithm for emulated bicubic
//	filtering.
//

#include <vd2/system/vdtypes.h>

#define DIRECTDRAW_VERSION 0x0900
#define INITGUID
#include <d3d9.h>
#include <vd2/system/vdtypes.h>
#include <vd2/system/vdalloc.h>
#include <vd2/system/error.h>
#include <vd2/system/binary.h>
#include <vd2/system/refcount.h>
#include <vd2/system/math.h>
#include <vd2/system/seh.h>
#include <vd2/system/time.h>
#include <vd2/system/vdstl.h>
#include <vd2/system/vdstl_vectorview.h>
#include <vd2/system/w32assist.h>
#include <vd2/Kasumi/blitter.h>
#include <vd2/Kasumi/pixmap.h>
#include <vd2/Kasumi/pixmapops.h>
#include <vd2/Kasumi/pixmaputils.h>
#include <vd2/Kasumi/text.h>
#include <vd2/Kasumi/region.h>

#include <displaydrvd3d9.h>
#include <vd2/VDDisplay/direct3d.h>
#include <vd2/VDDisplay/compositor.h>
#include <vd2/VDDisplay/display.h>
#include <vd2/VDDisplay/displaydrv.h>
#include <vd2/VDDisplay/logging.h>
#include <vd2/VDDisplay/renderer.h>
#include <vd2/VDDisplay/textrenderer.h>
#include <vd2/VDDisplay/internal/customshaderd3d9.h>
#include <vd2/VDDisplay/internal/screenfx.h>

namespace nsVDDisplay {
	#include "displayd3d9_shader.inl"
}

#ifdef _MSC_VER
#pragma warning(disable: 4351)		// warning C4351: new behavior: elements of array 'VDVideoUploadContextD3D9::mpD3DImageTextures' will be default initialized
#endif

#define VDDEBUG_DX9DISP(...) VDDispLogF(__VA_ARGS__)

#define D3D_DO(x) VDVERIFY(SUCCEEDED(mpD3DDevice->x))

using namespace nsVDD3D9;

class VDD3D9TextureGeneratorFullSizeRTT : public vdrefcounted<IVDD3D9TextureGenerator> {
public:
	bool GenerateTexture(VDD3D9Manager *pManager, IVDD3D9Texture *pTexture) {
		const D3DDISPLAYMODE& dmode = pManager->GetDisplayMode();

		int w = dmode.Width;
		int h = dmode.Height;

		pManager->AdjustTextureSize(w, h);

		IDirect3DDevice9 *dev = pManager->GetDevice();
		IDirect3DTexture9 *tex;
		HRESULT hr = dev->CreateTexture(w, h, 1, D3DUSAGE_RENDERTARGET, D3DFMT_A8R8G8B8, D3DPOOL_DEFAULT, &tex, NULL);
		if (FAILED(hr))
			return false;

		pTexture->SetD3DTexture(tex);
		tex->Release();
		return true;
	}
};

bool VDCreateD3D9TextureGeneratorFullSizeRTT(IVDD3D9TextureGenerator **ppGenerator) {
	*ppGenerator = new VDD3D9TextureGeneratorFullSizeRTT;
	if (!*ppGenerator)
		return false;
	(*ppGenerator)->AddRef();
	return true;
}

///////////////////////////////////////////////////////////////////////////

class VDD3D9TextureGeneratorFullSizeRTT16F : public vdrefcounted<IVDD3D9TextureGenerator> {
public:
	bool GenerateTexture(VDD3D9Manager *pManager, IVDD3D9Texture *pTexture) {
		const D3DDISPLAYMODE& dmode = pManager->GetDisplayMode();

		int w = dmode.Width;
		int h = dmode.Height;

		pManager->AdjustTextureSize(w, h);

		IDirect3DDevice9 *dev = pManager->GetDevice();
		IDirect3DTexture9 *tex;
		HRESULT hr = dev->CreateTexture(w, h, 1, D3DUSAGE_RENDERTARGET, D3DFMT_A16B16G16R16F, D3DPOOL_DEFAULT, &tex, NULL);
		if (FAILED(hr))
			return false;

		pTexture->SetD3DTexture(tex);
		tex->Release();
		return true;
	}
};

bool VDCreateD3D9TextureGeneratorFullSizeRTT16F(IVDD3D9TextureGenerator **ppGenerator) {
	*ppGenerator = new VDD3D9TextureGeneratorFullSizeRTT16F;
	if (!*ppGenerator)
		return false;
	(*ppGenerator)->AddRef();
	return true;
}

///////////////////////////////////////////////////////////////////////////

class VDD3D9TextureGeneratorDither final : public vdrefcounted<IVDD3D9TextureGenerator> {
public:
	bool GenerateTexture(VDD3D9Manager *pManager, IVDD3D9Texture *pTexture) {
		static const uint8 dither[16][16]={
			{  35, 80,227,165, 64,199, 42,189,138, 74,238,111, 43,153, 13,211 },
			{ 197,135, 20, 99,244,  4,162,105, 25,210, 38,134,225, 78,242, 87 },
			{  63,249,126,192, 50,174, 82,251,116,148, 97,176, 19,167, 52,163 },
			{ 187, 30, 85,142,219, 71,194, 45,169, 11,241, 58,216,106,204,  5 },
			{  94,151,235,  9,112,155, 17,224, 91,206, 84,188,120, 36,132,233 },
			{ 177, 48,124,201, 40,239,125, 66,180, 51,160,  7,152,255, 89, 56 },
			{  16,209, 72,161,121, 59,208,150, 28,248, 75,229,101, 26,140,220 },
			{ 170,110,226, 22,252,139,  1,109,195,115,172, 39,200,114,191, 68 },
			{ 136, 34, 96,183, 44,175, 95,234, 81, 15,143,217, 62,164,  2,237 },
			{  57,245,154, 61,203, 70,213, 37,137,243, 98, 23,179, 86,198,103 },
			{ 184, 12,123,221,  6,129,156, 88,185, 53,127,228, 49,250, 31,130 },
			{  77,205, 83,145,107,247, 29,223, 10,212,159, 79,168, 73,146,232 },
			{ 173, 41,240, 24,190, 54,178,102,149,118, 33,202,  8,215,119, 18 },
			{  92,158, 67,166, 76,207,133, 47,254, 65,230,100,131,157, 69,193 },
			{ 253,  3,214,117,231, 14, 93,171, 21,182,144, 55,246, 27,222, 46 },
			{ 141,181,104, 32,147,113,236, 60,218,122,  0,196, 90,186,128,108 },
		};

		IDirect3DDevice9 *dev = pManager->GetDevice();
		vdrefptr<IVDD3D9InitTexture> tex;
		if (!pManager->CreateInitTexture(16, 16, 1, D3DFMT_A8R8G8B8, ~tex))
			return false;

		VDD3D9LockInfo lockInfo;
		if (!tex->Lock(0, lockInfo)) {
			VDDEBUG_DX9DISP("VideoDisplay/DX9: Failed to load horizontal even/odd texture.");
			return false;
		}

		char *dst = (char *)lockInfo.mpData;
		for(int y=0; y<16; ++y) {
			const uint8 *srcrow = dither[y];
			uint32 *dstrow = (uint32 *)dst;

			for(int x=0; x<16; ++x)
				dstrow[x] = 0x01010101 * srcrow[x];

			dst += lockInfo.mPitch;
		}

		tex->Unlock(0);

		return pTexture->Init(tex);
	}
};

class VDD3D9TextureGeneratorHEvenOdd final : public vdrefcounted<IVDD3D9TextureGenerator> {
public:
	bool GenerateTexture(VDD3D9Manager *pManager, IVDD3D9Texture *pTexture) {
		IDirect3DDevice9 *dev = pManager->GetDevice();
		vdrefptr<IVDD3D9InitTexture> tex;
		if (!pManager->CreateInitTexture(16, 1, 1, D3DFMT_A8R8G8B8, ~tex))
			return false;

		VDD3D9LockInfo lockInfo;
		if (!tex->Lock(0, lockInfo)) {
			VDDEBUG_DX9DISP("VideoDisplay/DX9: Failed to load horizontal even/odd texture.");
			return false;
		}

		for(int i=0; i<16; ++i)
			((uint32 *)lockInfo.mpData)[i] = (uint32)-(sint32)(i&1);

		tex->Unlock(0);

		return pTexture->Init(tex);
	}
};

class VDD3D9TextureGeneratorCubicFilter final : public vdrefcounted<IVDD3D9TextureGenerator> {
public:
	bool GenerateTexture(VDD3D9Manager *pManager, IVDD3D9Texture *pTexture) {
		IDirect3DDevice9 *dev = pManager->GetDevice();
		vdrefptr<IVDD3D9InitTexture> tex;
		if (!pManager->CreateInitTexture(256, 4, 1, D3DFMT_A8R8G8B8, ~tex))
			return false;

		VDD3D9LockInfo lr;
		if (!tex->Lock(0, lr)) {
			VDDEBUG_DX9DISP("VideoDisplay/DX9: Failed to load cubic filter texture.");
			return false;
		}

		MakeCubic4Texture((uint32 *)lr.mpData, lr.mPitch, -0.75);

		tex->Unlock(0);

		return pTexture->Init(tex);
	}

protected:
	static void MakeCubic4Texture(uint32 *texture, ptrdiff_t pitch, double A) {
		int i;

		uint32 *p0 = texture;
		uint32 *p1 = vdptroffset(texture, pitch);
		uint32 *p2 = vdptroffset(texture, pitch*2);
		uint32 *p3 = vdptroffset(texture, pitch*3);

		for(i=0; i<256; i++) {
			double d = (double)(i&63) / 64.0;
			int y1, y2, y3, y4, ydiff;

			// Coefficients for all four pixels *must* add up to 1.0 for
			// consistent unity gain.
			//
			// Two good values for A are -1.0 (original VirtualDub bicubic filter)
			// and -0.75 (closely matches Photoshop).

			double c1 =         +     A*d -       2.0*A*d*d +       A*d*d*d;
			double c2 = + 1.0             -     (A+3.0)*d*d + (A+2.0)*d*d*d;
			double c3 =         -     A*d + (2.0*A+3.0)*d*d - (A+2.0)*d*d*d;
			double c4 =                   +           A*d*d -       A*d*d*d;

			const int maxval = 255;
			double scale = maxval / (c1 + c2 + c3 + c4);

			y1 = (int)floor(0.5 + c1 * scale);
			y2 = (int)floor(0.5 + c2 * scale);
			y3 = (int)floor(0.5 + c3 * scale);
			y4 = (int)floor(0.5 + c4 * scale);

			ydiff = maxval - y1 - y2 - y3 - y4;

			int ywhole = ydiff<0 ? (ydiff-2)/4 : (ydiff+2)/4;
			ydiff -= ywhole*4;

			y1 += ywhole;
			y2 += ywhole;
			y3 += ywhole;
			y4 += ywhole;

			if (ydiff < 0) {
				if (y1<y4)
					y1 += ydiff;
				else
					y4 += ydiff;
			} else if (ydiff > 0) {
				if (y2 > y3)
					y2 += ydiff;
				else
					y3 += ydiff;
			}

			p0[i] = (-y1 << 24) + (y2 << 16) + (y3 << 8) + (-y4);
		}
	}
};

///////////////////////////////////////////////////////////////////////////

#ifdef _MSC_VER
	#pragma warning(push)
	#pragma warning(disable: 4584)		// warning C4584: 'VDVideoDisplayDX9Manager' : base-class 'vdlist_node' is already a base-class of 'VDD3D9Client'
#endif

struct VDVideoDisplayDX9ManagerNode : public vdlist_node {};

class VDVideoDisplayDX9Manager final : public IVDVideoDisplayDX9Manager, public VDD3D9Client, public VDVideoDisplayDX9ManagerNode {
public:
	VDVideoDisplayDX9Manager(VDThreadID tid, HMONITOR hmonitor, bool use9ex);
	~VDVideoDisplayDX9Manager();

	int AddRef();
	int Release();

	bool Init();
	void Shutdown();

	CubicMode InitBicubic();
	void ShutdownBicubic();

	bool InitBicubicTempSurfaces(bool highPrecision);
	void ShutdownBicubicTempSurfaces(bool highPrecision);

	bool IsD3D9ExEnabled() const { return mbUseD3D9Ex; }
	bool Is16FEnabled() const { return mbIs16FEnabled; }
	bool IsPS11Enabled() const { return mbIsPS11Enabled; }
	bool IsPS20Enabled() const override { return mbIsPS20Enabled; }

	VDThreadID GetThreadId() const { return mThreadId; }
	HMONITOR GetMonitor() const { return mhMonitor; }

	IVDD3D9Texture	*GetTempRTT(int i) const { return mpRTTs[i]; }
	IVDD3D9Texture	*GetFilterTexture() const { return mpFilterTexture; }
	IVDD3D9Texture	*GetHEvenOddTexture() const { return mpHEvenOddTexture; }

	void		DetermineBestTextureFormat(int srcFormat, int& dstFormat, D3DFORMAT& dstD3DFormat);

	bool ValidateBicubicShader(CubicMode mode);

	bool BlitFixedFunction(const EffectContext& ctx, IDirect3DSurface9 *pRTOverride, bool bilinear);
	bool RunEffect(const EffectContext& ctx, const nsVDDisplay::TechniqueInfo& technique, IDirect3DSurface9 *pRTOverride) override;

public:
	void OnPreDeviceReset() {}
	void OnPostDeviceReset() {}

protected:
	bool BlitFixedFunction2(const EffectContext& ctx, IDirect3DSurface9 *pRTOverride, bool bilinear);
	bool RunEffect2(const EffectContext& ctx, const nsVDDisplay::TechniqueInfo& technique, IDirect3DSurface9 *pRTOverride);
	bool InitEffect();
	void ShutdownEffect();

	VDD3D9Manager		*mpManager;
	vdrefptr<IVDD3D9Texture>	mpFilterTexture;
	vdrefptr<IVDD3D9Texture>	mpHEvenOddTexture;
	vdrefptr<IVDD3D9Texture>	mpDitherTexture;
	vdrefptr<IVDD3D9Texture>	mpRTTs[3];

	vdfastvector<IDirect3DVertexShader9 *>	mVertexShaders;
	vdfastvector<IDirect3DPixelShader9 *>	mPixelShaders;

	CubicMode			mCubicMode;
	int					mCubicRefCount;
	int					mCubicTempSurfacesRefCount[2];
	bool				mbIs16FEnabled;
	bool				mbIsPS11Enabled;
	bool				mbIsPS20Enabled;
	bool				mbUseD3D9Ex;

	const VDThreadID	mThreadId;
	const HMONITOR		mhMonitor;
	int					mRefCount;
};

#ifdef _MSC_VER
	#pragma warning(pop)
#endif

///////////////////////////////////////////////////////////////////////////

class VDFontRendererD3D9 final : public vdrefcounted<IVDFontRendererD3D9> {
public:
	VDFontRendererD3D9();

	bool Init(VDD3D9Manager *d3dmgr);
	void Shutdown();

	bool Begin();
	void DrawTextLine(int x, int y, uint32 textColor, uint32 outlineColor, const char *s);
	void End();

protected:
	VDD3D9Manager *mpD3DManager;
	vdrefptr<IDirect3DTexture9> mpD3DFontTexture;

	struct GlyphLayoutInfo {
		int		mGlyph;
		float	mX;
	};

	typedef vdfastvector<GlyphLayoutInfo> GlyphLayoutInfos;
	GlyphLayoutInfos mGlyphLayoutInfos;

	struct GlyphInfo {
		vdrect32f	mPos;
		vdrect32f	mUV;
		float		mAdvance;
	};

	GlyphInfo mGlyphInfo[256];
};

bool VDCreateFontRendererD3D9(IVDFontRendererD3D9 **pp) {
	*pp = new_nothrow VDFontRendererD3D9();
	if (*pp)
		(*pp)->AddRef();
	return *pp != NULL;
}

VDFontRendererD3D9::VDFontRendererD3D9()
	: mpD3DManager(NULL)
{
}

bool VDFontRendererD3D9::Init(VDD3D9Manager *d3dmgr) {
	mpD3DManager = d3dmgr;

	vdfastvector<uint32> tempbits(256*256, 0);
	VDPixmap temppx={0};
	temppx.data = tempbits.data();
	temppx.w = 256;
	temppx.h = 256;
	temppx.format = nsVDPixmap::kPixFormat_XRGB8888;
	temppx.pitch = 256*sizeof(uint32);

	VDTextLayoutMetrics metrics;
	VDPixmapPathRasterizer rast;

	VDPixmapRegion outlineRegion;
	VDPixmapRegion charRegion;
	VDPixmapRegion charOutlineRegion;
	VDPixmapCreateRoundRegion(outlineRegion, 16.0f);

	static const float kFontSize = 16.0f;

	int x = 1;
	int y = 1;
	int lineHeight = 0;
	GlyphInfo *pgi = mGlyphInfo;
	for(int c=0; c<256; ++c) {
		char s[2]={(char)c, 0};

		VDPixmapGetTextExtents(NULL, kFontSize, s, metrics);

		if (metrics.mExtents.valid()) {
			int x1 = VDCeilToInt(metrics.mExtents.left * 8.0f - 0.5f);
			int y1 = VDCeilToInt(metrics.mExtents.top * 8.0f - 0.5f);
			int x2 = VDCeilToInt(metrics.mExtents.right * 8.0f - 0.5f);
			int y2 = VDCeilToInt(metrics.mExtents.bottom * 8.0f - 0.5f);
			int ix1 = x1 >> 3;
			int iy1 = y1 >> 3;
			int ix2 = (x2 + 7) >> 3;
			int iy2 = (y2 + 7) >> 3;
			int w = (ix2 - ix1) + 4;
			int h = (iy2 - iy1) + 4;

			if (x + w > 255) {
				x = 1;
				y += lineHeight + 1;
				lineHeight = 0;
			}

			if (lineHeight < h) {
				lineHeight = h;
				VDASSERT(lineHeight+y < 255);
			}

			rast.Clear();
			VDPixmapConvertTextToPath(rast, NULL, kFontSize * 64.0f, (float)(-ix1*64), (float)(-iy1*64), s);
			rast.ScanConvert(charRegion);
			VDPixmapConvolveRegion(charOutlineRegion, charRegion, outlineRegion);

			VDPixmapFillRegionAntialiased8x(temppx, charOutlineRegion, x*8+16, y*8+16, 0x000000FF);
			VDPixmapFillRegionAntialiased8x(temppx, charRegion, x*8+16, y*8+16, 0xFFFFFFFF);

			pgi->mAdvance = metrics.mAdvance;
			pgi->mPos.set((float)(ix1 - 2), (float)(iy1 - 2), (float)(ix2 + 2), (float)(iy2 + 2));
			pgi->mUV.set((float)x, (float)y, (float)(x+w), (float)(y+h));
			pgi->mUV.scale(1.0f / 256.0f, 1.0f / 256.0f);

			x += w+1;
		} else {
			pgi->mAdvance = metrics.mAdvance;
			pgi->mPos.clear();
			pgi->mUV.clear();
		}
		++pgi;
	}

	// create texture
	IDirect3DDevice9 *dev = mpD3DManager->GetDevice();
	const bool useDefault = mpD3DManager->GetDeviceEx() != NULL;

	HRESULT hr = dev->CreateTexture(256, 256, 1, 0, D3DFMT_A8R8G8B8, useDefault ? D3DPOOL_DEFAULT : D3DPOOL_MANAGED, ~mpD3DFontTexture, NULL);
	if (FAILED(hr)) {
		VDDEBUG_DX9DISP("VideoDisplay/DX9: Failed to create font cache texture.");
		Shutdown();
		return false;
	}

	vdrefptr<IDirect3DTexture9> uploadtex;
	if (useDefault) {
		hr = dev->CreateTexture(256, 256, 1, 0, D3DFMT_A8R8G8B8, D3DPOOL_SYSTEMMEM, ~uploadtex, NULL);
		if (FAILED(hr)) {
			VDDEBUG_DX9DISP("VideoDisplay/DX9: Failed to create font cache texture.");
			Shutdown();
			return false;
		}
	} else {
		uploadtex = mpD3DFontTexture;
	}

	// copy into texture
	D3DLOCKED_RECT lr;
	hr = uploadtex->LockRect(0, &lr, NULL, 0);
	VDASSERT(SUCCEEDED(hr));
	if (FAILED(hr)) {
		VDDEBUG_DX9DISP("VideoDisplay/DX9: Failed to load font cache texture.");
		Shutdown();
		return false;
	}
	
	uint32 *dst = (uint32 *)lr.pBits;
	const uint32 *src = tempbits.data();
	for(int y=0; y<256; ++y) {
		for(int x=0; x<256; ++x) {
			uint32 c = src[x];
			dst[x] = ((c >> 8) & 0xff) * 0x010101 + (c << 24);
		}

		src += 256;
		vdptrstep(dst, lr.Pitch);
	}

	VDVERIFY(SUCCEEDED(uploadtex->UnlockRect(0)));

	if (uploadtex != mpD3DFontTexture) {
		hr = dev->UpdateTexture(uploadtex, mpD3DFontTexture);
		if (FAILED(hr)) {
			Shutdown();
			return false;
		}
	}

	return true;
}

void VDFontRendererD3D9::Shutdown() {
	mpD3DFontTexture = NULL;
	mpD3DManager = NULL;
}

bool VDFontRendererD3D9::Begin() {
	if (!mpD3DManager)
		return false;

	IDirect3DDevice9 *dev = mpD3DManager->GetDevice();

	D3DVIEWPORT9 vp;
	HRESULT hr = dev->GetViewport(&vp);
	if (FAILED(hr))
		return false;

	const D3DMATRIX ident={
		{1,0,0,0,0,1,0,0,0,0,1,0,0,0,0,1}
	};

	dev->SetTransform(D3DTS_WORLD, &ident);
	dev->SetTransform(D3DTS_VIEW, &ident);

	const D3DMATRIX proj = {
		{
		2.0f / (float)vp.Width, 0.0f, 0.0f, 0.0f,
		0.0f, -2.0f / (float)vp.Height, 0.0f, 0.0f,
		0.0f, 0.0f, 0.0f, 0.0f,
		-1.0f - 1.0f / (float)vp.Width, 1.0f + 1.0f / (float)vp.Height, 0.0f, 1.0f
		}
	};

	dev->SetTransform(D3DTS_PROJECTION, &proj);

	dev->SetSamplerState(0, D3DSAMP_ADDRESSU, D3DTADDRESS_CLAMP);
	dev->SetSamplerState(0, D3DSAMP_ADDRESSV, D3DTADDRESS_CLAMP);
	dev->SetSamplerState(0, D3DSAMP_MINFILTER, D3DTEXF_LINEAR);
	dev->SetSamplerState(0, D3DSAMP_MAGFILTER, D3DTEXF_LINEAR);
	dev->SetSamplerState(0, D3DSAMP_MIPFILTER, D3DTEXF_NONE);

	dev->SetTextureStageState(1, D3DTSS_COLOROP, D3DTOP_DISABLE);
	dev->SetTextureStageState(1, D3DTSS_ALPHAOP, D3DTOP_DISABLE);

	// Rite of passage for any 3D programmer:
	// "Why the *&#$ didn't it draw anything!?"
	dev->SetRenderState(D3DRS_CULLMODE, D3DCULL_NONE);
	dev->SetRenderState(D3DRS_LIGHTING, FALSE);
	dev->SetRenderState(D3DRS_STENCILENABLE, FALSE);
	dev->SetRenderState(D3DRS_ZENABLE, FALSE);
	dev->SetRenderState(D3DRS_ALPHATESTENABLE, FALSE);
	dev->SetRenderState(D3DRS_ALPHABLENDENABLE, TRUE);
	dev->SetRenderState(D3DRS_BLENDOP, D3DBLENDOP_ADD);
	dev->SetRenderState(D3DRS_SRCBLEND, D3DBLEND_ONE);

	dev->SetVertexShader(NULL);
	dev->SetVertexDeclaration(mpD3DManager->GetVertexDeclaration());
	dev->SetPixelShader(NULL);
	dev->SetStreamSource(0, mpD3DManager->GetVertexBuffer(), 0, sizeof(nsVDD3D9::Vertex));
	dev->SetIndices(mpD3DManager->GetIndexBuffer());
	dev->SetTexture(0, mpD3DFontTexture);

	return mpD3DManager->BeginScene();
}

void VDFontRendererD3D9::DrawTextLine(int x, int y, uint32 textColor, uint32 outlineColor, const char *s) {
	const uint32 kMaxQuads = nsVDD3D9::kVertexBufferSize / 4;
	size_t len = strlen(s);

	mGlyphLayoutInfos.clear();
	mGlyphLayoutInfos.reserve(len);

	float xpos = (float)x;
	for(size_t i=0; i<len; ++i) {
		char c = *s++;
		const GlyphInfo& gi = mGlyphInfo[(int)c & 0xff];

		if (!gi.mPos.empty()) {
			mGlyphLayoutInfos.push_back();
			GlyphLayoutInfo& gli = mGlyphLayoutInfos.back();
			gli.mGlyph = (int)c & 0xff;
			gli.mX = xpos;
		}

		xpos += gi.mAdvance;
	}

	float ypos = (float)y;

	IDirect3DDevice9 *dev = mpD3DManager->GetDevice();
	for(int i=0; i<2; ++i) {
		uint32 vertexColor;

		switch(i) {
		case 0:
			vertexColor = outlineColor;
			dev->SetTextureStageState(0, D3DTSS_COLOROP, D3DTOP_MODULATE);
			dev->SetTextureStageState(0, D3DTSS_COLORARG1, D3DTA_DIFFUSE);
			dev->SetTextureStageState(0, D3DTSS_COLORARG2, D3DTA_TEXTURE | D3DTA_ALPHAREPLICATE);
			dev->SetTextureStageState(0, D3DTSS_ALPHAOP, D3DTOP_SELECTARG1);
			dev->SetTextureStageState(0, D3DTSS_ALPHAARG1, D3DTA_TEXTURE);
			dev->SetRenderState(D3DRS_DESTBLEND, D3DBLEND_INVSRCALPHA);
			break;
		case 1:
			vertexColor = textColor;
			dev->SetTextureStageState(0, D3DTSS_COLOROP, D3DTOP_MODULATE);
			dev->SetTextureStageState(0, D3DTSS_COLORARG1, D3DTA_DIFFUSE);
			dev->SetTextureStageState(0, D3DTSS_COLORARG2, D3DTA_TEXTURE);
			dev->SetTextureStageState(0, D3DTSS_ALPHAOP, D3DTOP_SELECTARG1);
			dev->SetTextureStageState(0, D3DTSS_ALPHAARG1, D3DTA_TEXTURE);
			dev->SetRenderState(D3DRS_DESTBLEND, D3DBLEND_ONE);
			break;
		}

		uint32 glyphCount = (uint32)mGlyphLayoutInfos.size();
		uint32 glyphStart = 0;
		const GlyphLayoutInfo *pgli = mGlyphLayoutInfos.data();
		while(glyphStart < glyphCount) {
			uint32 glyphsToRender = glyphCount - glyphStart;
			if (glyphsToRender > kMaxQuads)
				glyphsToRender = kMaxQuads;

			nsVDD3D9::Vertex *vx = mpD3DManager->LockVertices(glyphsToRender * 4);
			if (!vx)
				break;

			for(uint32 i=0; i<glyphsToRender; ++i) {
				const GlyphInfo& gi = mGlyphInfo[pgli->mGlyph];

				new(vx  ) nsVDD3D9::Vertex(pgli->mX + gi.mPos.left,  ypos + gi.mPos.top,    vertexColor, gi.mUV.left,  gi.mUV.top   );
				new(vx+1) nsVDD3D9::Vertex(pgli->mX + gi.mPos.left,  ypos + gi.mPos.bottom, vertexColor, gi.mUV.left,  gi.mUV.bottom);
				new(vx+2) nsVDD3D9::Vertex(pgli->mX + gi.mPos.right, ypos + gi.mPos.bottom, vertexColor, gi.mUV.right, gi.mUV.bottom);
				new(vx+3) nsVDD3D9::Vertex(pgli->mX + gi.mPos.right, ypos + gi.mPos.top,    vertexColor, gi.mUV.right, gi.mUV.top   );
				vx += 4;
				++pgli;
			}

			mpD3DManager->UnlockVertices();

			uint16 *idx = mpD3DManager->LockIndices(glyphsToRender * 6);
			if (!idx)
				break;

			uint32 vidx = 0;
			for(uint32 i=0; i<glyphsToRender; ++i) {
				idx[0] = vidx;
				idx[1] = vidx+1;
				idx[2] = vidx+2;
				idx[3] = vidx;
				idx[4] = vidx+2;
				idx[5] = vidx+3;
				vidx += 4;
				idx += 6;
			}

			mpD3DManager->UnlockIndices();

			mpD3DManager->DrawElements(D3DPT_TRIANGLELIST, 0, 4*glyphsToRender, 0, 2*glyphsToRender);

			glyphStart += glyphsToRender;
		}
	}
}

void VDFontRendererD3D9::End() {
	IDirect3DDevice9 *dev = mpD3DManager->GetDevice();

	dev->SetTexture(0, NULL);
}

///////////////////////////////////////////////////////////////////////////

VDDisplayCachedImageD3D9::VDDisplayCachedImageD3D9() {
	mListNodePrev = NULL;
	mListNodeNext = NULL;
}

VDDisplayCachedImageD3D9::~VDDisplayCachedImageD3D9() {
	if (mListNodePrev)
		vdlist_base::unlink(*this);
}

void *VDDisplayCachedImageD3D9::AsInterface(uint32 iid) {
	if (iid == kTypeID)
		return this;

	return NULL;
}

bool VDDisplayCachedImageD3D9::Init(VDD3D9Manager *mgr, void *owner, const VDDisplayImageView& imageView) {
	const VDPixmap& px = imageView.GetImage();
	int w = px.w;
	int h = px.h;

	if (!mgr->AdjustTextureSize(w, h, true))
		return false;

	IDirect3DDevice9 *dev = mgr->GetDevice();
	HRESULT hr = dev->CreateTexture(w, h, 1, 0, D3DFMT_X8R8G8B8, mgr->GetDeviceEx() ? D3DPOOL_DEFAULT : D3DPOOL_MANAGED, ~mpD3DTexture, NULL);
	if (FAILED(hr))
		return false;

	mWidth = px.w;
	mHeight = px.h;
	mTexWidth = w;
	mTexHeight = h;
	mpOwner = owner;
	mUniquenessCounter = imageView.GetUniquenessCounter() - 2;

	Update(imageView);
	return true;
}

void VDDisplayCachedImageD3D9::Shutdown() {
	mpD3DTexture.clear();
	mpOwner = NULL;
}

void VDDisplayCachedImageD3D9::Update(const VDDisplayImageView& imageView) {
	uint32 newCounter = imageView.GetUniquenessCounter();
	bool partialUpdateOK = ((mUniquenessCounter + 1) == newCounter);

	mUniquenessCounter = newCounter;

	if (mpD3DTexture) {
		const VDPixmap& px = imageView.GetImage();

		const uint32 numRects = imageView.GetDirtyListSize();

		D3DLOCKED_RECT lr;

		if (partialUpdateOK && numRects) {
			const vdrect32 *rects = imageView.GetDirtyList();

			if (SUCCEEDED(mpD3DTexture->LockRect(0, &lr, NULL, D3DLOCK_NO_DIRTY_UPDATE))) {
				VDPixmap dst = {};
				dst.format = nsVDPixmap::kPixFormat_XRGB8888;
				dst.w = mTexWidth;
				dst.h = mTexHeight;
				dst.pitch = lr.Pitch;
				dst.data = lr.pBits;

				for(uint32 i=0; i<numRects; ++i) {
					const vdrect32& r = rects[i];

					VDPixmapBlt(dst, r.left, r.top, px, r.left, r.top, r.width(), r.height());
				}

				mpD3DTexture->UnlockRect(0);

				for(uint32 i=0; i<numRects; ++i) {
					const vdrect32& r = rects[i];

					RECT r2 = {r.left, r.top, r.right, r.bottom};
					mpD3DTexture->AddDirtyRect(&r2);
				}
			}
		} else {
			if (SUCCEEDED(mpD3DTexture->LockRect(0, &lr, NULL, 0))) {
				VDPixmap dst = {};
				dst.format = nsVDPixmap::kPixFormat_XRGB8888;
				dst.w = mTexWidth;
				dst.h = mTexHeight;
				dst.pitch = lr.Pitch;
				dst.data = lr.pBits;

				VDPixmapBlt(dst, px);

				mpD3DTexture->UnlockRect(0);
			}
		}
	}
}

///////////////////////////////////////////////////////////////////////////

static VDCriticalSection g_csVDDisplayDX9Managers;
static vdlist<VDVideoDisplayDX9ManagerNode> g_VDDisplayDX9Managers;

bool VDInitDisplayDX9(HMONITOR hmonitor, bool use9ex, VDVideoDisplayDX9Manager **ppManager) {
	VDVideoDisplayDX9Manager *pMgr = NULL;
	bool firstClient = false;

	vdsynchronized(g_csVDDisplayDX9Managers) {
		vdlist<VDVideoDisplayDX9ManagerNode>::iterator it(g_VDDisplayDX9Managers.begin()), itEnd(g_VDDisplayDX9Managers.end());

		VDThreadID tid = VDGetCurrentThreadID();

		for(; it != itEnd; ++it) {
			VDVideoDisplayDX9Manager *mgr = static_cast<VDVideoDisplayDX9Manager *>(*it);

			if (mgr->GetThreadId() == tid && mgr->GetMonitor() == hmonitor && mgr->IsD3D9ExEnabled() == use9ex) {
				pMgr = mgr;
				break;
			}
		}

		if (!pMgr) {
			pMgr = new_nothrow VDVideoDisplayDX9Manager(tid, hmonitor, use9ex);
			if (!pMgr)
				return false;

			g_VDDisplayDX9Managers.push_back(pMgr);
			firstClient = true;
		}

		pMgr->AddRef();
	}

	if (firstClient) {
		if (!pMgr->Init()) {
			vdsynchronized(g_csVDDisplayDX9Managers) {
				g_VDDisplayDX9Managers.erase(pMgr);
			}
			pMgr->Release();
			return NULL;
		}
	}

	*ppManager = pMgr;
	return true;
}

VDVideoDisplayDX9Manager::VDVideoDisplayDX9Manager(VDThreadID tid, HMONITOR hmonitor, bool use9ex)
	: mpManager(NULL)
	, mCubicRefCount(0)
	, mThreadId(tid)
	, mhMonitor(hmonitor)
	, mRefCount(0)
	, mbIs16FEnabled(false)
	, mbIsPS20Enabled(false)
	, mbUseD3D9Ex(use9ex)
{
	mCubicTempSurfacesRefCount[0] = 0;
	mCubicTempSurfacesRefCount[1] = 0;
}

VDVideoDisplayDX9Manager::~VDVideoDisplayDX9Manager() {
	VDASSERT(!mRefCount);
	VDASSERT(!mCubicRefCount);
	VDASSERT(!mCubicTempSurfacesRefCount[0]);
	VDASSERT(!mCubicTempSurfacesRefCount[1]);

	vdsynchronized(g_csVDDisplayDX9Managers) {
		g_VDDisplayDX9Managers.erase(this);
	}
}

int VDVideoDisplayDX9Manager::AddRef() {
	return ++mRefCount;
}

int VDVideoDisplayDX9Manager::Release() {
	int rc = --mRefCount;
	if (!rc) {
		Shutdown();
		delete this;
	}
	return rc;
}

bool VDVideoDisplayDX9Manager::Init() {
	VDASSERT(!mpManager);
	mpManager = VDInitDirect3D9(this, mhMonitor, mbUseD3D9Ex);
	if (!mpManager)
		return false;

	// Check for 16F capability.
	//
	// We need:
	//	* Vertex and pixel shader 2.0.
	//	* 16F texture support.
	//	* 16F blending.

	const D3DCAPS9& caps = mpManager->GetCaps();

	mbIs16FEnabled = false;

	mbIsPS11Enabled = false;
	if (caps.VertexShaderVersion >= D3DVS_VERSION(1, 1) &&
		caps.PixelShaderVersion >= D3DPS_VERSION(1, 1))
	{
		mbIsPS11Enabled = true;
	}

	if (caps.VertexShaderVersion >= D3DVS_VERSION(2, 0) &&
		caps.PixelShaderVersion >= D3DPS_VERSION(2, 0))
	{
		mbIsPS20Enabled = true;

		if (mpManager->CheckResourceFormat(0, D3DRTYPE_TEXTURE, D3DFMT_A16B16G16R16F) &&
			mpManager->CheckResourceFormat(D3DUSAGE_QUERY_FILTER, D3DRTYPE_TEXTURE, D3DFMT_A16B16G16R16F))
		{
			mbIs16FEnabled = true;
		}
	}

	if (!mpManager->CreateSharedTexture<VDD3D9TextureGeneratorDither>("dither", ~mpDitherTexture)) {
		Shutdown();
		return false;
	}

	if (!mpManager->CreateSharedTexture<VDD3D9TextureGeneratorHEvenOdd>("hevenodd", ~mpHEvenOddTexture)) {
		Shutdown();
		return false;
	}

	if (!InitEffect()) {
		Shutdown();
		return false;
	}

	return true;
}

void VDVideoDisplayDX9Manager::Shutdown() {
	VDASSERT(!mCubicRefCount);
	VDASSERT(!mCubicTempSurfacesRefCount[0]);
	VDASSERT(!mCubicTempSurfacesRefCount[1]);

	mpDitherTexture = NULL;
	mpHEvenOddTexture = NULL;

	ShutdownEffect();

	if (mpManager) {
		VDDeinitDirect3D9(mpManager, this);
		mpManager = NULL;
	}
}

bool VDVideoDisplayDX9Manager::InitEffect() {
	using namespace nsVDDisplay;

	IDirect3DDevice9 *pD3DDevice = mpManager->GetDevice();
	const D3DCAPS9& caps = mpManager->GetCaps();

	// initialize vertex shaders
	if (g_effect.mVertexShaderOffsets.size() > 1 && mVertexShaders.empty()) {
		const size_t n = g_effect.mVertexShaderOffsets.size() - 1;
		mVertexShaders.resize(n, nullptr);

		for(uint32 i=0; i<n; ++i) {
			const uint32 *pVertexShaderData = &*g_effect.mShaderData.begin() + *(g_effect.mVertexShaderOffsets.begin() + i);

			if ((pVertexShaderData[0] & 0xffff) > (caps.VertexShaderVersion & 0xffff))
				continue;

			HRESULT hr = pD3DDevice->CreateVertexShader((const DWORD *)pVertexShaderData, &mVertexShaders[i]);
			if (FAILED(hr)) {
				VDDEBUG_DX9DISP("VideoDisplay/DX9: Unable to create vertex shader #%d.", i+1);
				VDDEBUG_DX9DISP("VideoDisplay/DX9: Vertex shader version is: %x.", pVertexShaderData[0]);
				VDDEBUG_DX9DISP("VideoDisplay/DX9: Supported vertex shader version is: %x.", caps.VertexShaderVersion);
				return false;
			}
		}
	}

	// initialize pixel shaders
	if (g_effect.mPixelShaderOffsets.size() > 1 && mPixelShaders.empty()) {
		const size_t n = g_effect.mPixelShaderOffsets.size() - 1;
		mPixelShaders.resize(n, nullptr);

		for(uint32 i=0; i<n; ++i) {
			const uint32 *pPixelShaderData = &*g_effect.mShaderData.begin() + *(g_effect.mPixelShaderOffsets.begin() + i);

			if ((pPixelShaderData[0] & 0xffff) > (caps.PixelShaderVersion & 0xffff))
				continue;

			HRESULT hr = pD3DDevice->CreatePixelShader((const DWORD *)pPixelShaderData, &mPixelShaders[i]);
			if (FAILED(hr)) {
				VDDEBUG_DX9DISP("VideoDisplay/DX9: Unable to create pixel shader #%d.", i+1);
				VDDEBUG_DX9DISP("VideoDisplay/DX9: Pixel shader version is: %x.", pPixelShaderData[0]);
				VDDEBUG_DX9DISP("VideoDisplay/DX9: Supported pixel shader version is: %x.", caps.PixelShaderVersion);
				return false;
			}
		}
	}

	return true;
}

void VDVideoDisplayDX9Manager::ShutdownEffect() {
	while(!mPixelShaders.empty()) {
		IDirect3DPixelShader9 *ps = mPixelShaders.back();
		mPixelShaders.pop_back();

		if (ps)
			ps->Release();
	}

	while(!mVertexShaders.empty()) {
		IDirect3DVertexShader9 *vs = mVertexShaders.back();
		mVertexShaders.pop_back();

		if (vs)
			vs->Release();
	}
}

VDVideoDisplayDX9Manager::CubicMode VDVideoDisplayDX9Manager::InitBicubic() {
	VDASSERT(mRefCount > 0);
	VDASSERT(mCubicRefCount >= 0);

	if (++mCubicRefCount > 1)
		return mCubicMode;

	mCubicMode = (CubicMode)kMaxCubicMode;
	while(mCubicMode > kCubicNotPossible) {
		if (ValidateBicubicShader(mCubicMode))
			break;
		mCubicMode = (CubicMode)(mCubicMode - 1);
	}

	if (mCubicMode == kCubicNotPossible)
		ShutdownBicubic();

	return mCubicMode;
}

void VDVideoDisplayDX9Manager::ShutdownBicubic() {
	VDASSERT(mCubicRefCount > 0);
	if (--mCubicRefCount)
		return;

	mpFilterTexture = NULL;
}

bool VDVideoDisplayDX9Manager::InitBicubicTempSurfaces(bool highPrecision) {
	VDASSERT(mRefCount > 0);
	VDASSERT(mCubicTempSurfacesRefCount[highPrecision] >= 0);

	if (++mCubicTempSurfacesRefCount[highPrecision] > 1)
		return true;

	if (highPrecision) {
		if (!mbIs16FEnabled) {
			ShutdownBicubicTempSurfaces(highPrecision);
			return false;
		}

		if (!mpManager->CreateSharedTexture("rtt3", VDCreateD3D9TextureGeneratorFullSizeRTT16F, ~mpRTTs[2])) {
			ShutdownBicubicTempSurfaces(highPrecision);
			return false;
		}
	} else {
		// create horizontal resampling texture
		if (!mpManager->CreateSharedTexture("rtt1", VDCreateD3D9TextureGeneratorFullSizeRTT, ~mpRTTs[0])) {
			ShutdownBicubicTempSurfaces(highPrecision);
			return false;
		}
	}

	return true;
}

void VDVideoDisplayDX9Manager::ShutdownBicubicTempSurfaces(bool highPrecision) {
	VDASSERT(mCubicTempSurfacesRefCount[highPrecision] > 0);
	if (--mCubicTempSurfacesRefCount[highPrecision])
		return;

	if (highPrecision) {
		mpRTTs[2] = NULL;
	} else {
		mpRTTs[1] = NULL;
		mpRTTs[0] = NULL;
	}
}

namespace {
	D3DFORMAT GetD3DTextureFormatForPixmapFormat(int format) {
		using namespace nsVDPixmap;

		switch(format) {
			case nsVDPixmap::kPixFormat_XRGB1555:
				return D3DFMT_X1R5G5B5;

			case nsVDPixmap::kPixFormat_RGB565:
				return D3DFMT_R5G6B5;

			case nsVDPixmap::kPixFormat_XRGB8888:
				return D3DFMT_X8R8G8B8;

			case nsVDPixmap::kPixFormat_Y8_FR:
				return D3DFMT_L8;

			default:
				return D3DFMT_UNKNOWN;
		}
	}
}

void VDVideoDisplayDX9Manager::DetermineBestTextureFormat(int srcFormat, int& dstFormat, D3DFORMAT& dstD3DFormat) {
	using namespace nsVDPixmap;

	// Try direct format first. If that doesn't work, try a fallback (in practice, we
	// only have one).

	dstFormat = srcFormat;
	for(int i=0; i<2; ++i) {
		dstD3DFormat = GetD3DTextureFormatForPixmapFormat(dstFormat);
		if (dstD3DFormat && mpManager->IsTextureFormatAvailable(dstD3DFormat)) {
			dstFormat = srcFormat;
			return;
		}

		// fallback
		switch(dstFormat) {
			case kPixFormat_XRGB1555:
				dstFormat = kPixFormat_RGB565;
				break;

			case kPixFormat_RGB565:
				dstFormat = kPixFormat_XRGB1555;
				break;

			default:
				goto fail;
		}
	}
fail:

	// Just use X8R8G8B8. We always know this works (we reject the device if it doesn't).
	dstFormat = kPixFormat_XRGB8888;
	dstD3DFormat = D3DFMT_X8R8G8B8;
}

bool VDVideoDisplayDX9Manager::ValidateBicubicShader(CubicMode mode) {
	using namespace nsVDDisplay;

	if (mode != kCubicUsePS2_0Path)
		return false;

	// Validate caps bits.
	const D3DCAPS9& caps = mpManager->GetCaps();
	if (caps.PixelShaderVersion < D3DPS_VERSION(2, 0))
		return false;

	return true;
}

bool VDVideoDisplayDX9Manager::BlitFixedFunction(const EffectContext& ctx, IDirect3DSurface9 *pRTOverride, bool bilinear) {
	mpManager->BeginScope(L"BlitFixedFunction");
	bool success = BlitFixedFunction2(ctx, pRTOverride, bilinear);
	mpManager->EndScope();

	return success;
}

bool VDVideoDisplayDX9Manager::BlitFixedFunction2(const EffectContext& ctx, IDirect3DSurface9 *pRTOverride, bool bilinear) {
	using namespace nsVDDisplay;

	const D3DDISPLAYMODE& dmode = mpManager->GetDisplayMode();
	int clippedWidth = std::min<int>(ctx.mOutputX + ctx.mOutputW, dmode.Width);
	int clippedHeight = std::min<int>(ctx.mOutputY + ctx.mOutputH, dmode.Height);

	if (clippedWidth <= 0 || clippedHeight <= 0)
		return true;

	IDirect3DDevice9 *dev = mpManager->GetDevice();

	// bind vertex and pixel shaders
	HRESULT hr = dev->SetVertexShader(nullptr);
	if (FAILED(hr)) {
		VDDEBUG_DX9DISP("VideoDisplay/DX9: Couldn't clear vertex shader! hr=%08x %s", hr, VDDispDecodeD3D9Error(hr));
		return false;
	}

	hr = dev->SetPixelShader(nullptr);
	if (FAILED(hr)) {
		VDDEBUG_DX9DISP("VideoDisplay/DX9: Couldn't clear pixel shader! hr=%08x %s", hr, VDDispDecodeD3D9Error(hr));
		return false;
	}

	static const struct TextureStageState {
		DWORD mStage;
		D3DTEXTURESTAGESTATETYPE mState;
		DWORD mValue;
	} kTexStageStates[] = {
		{ 0, D3DTSS_COLOROP, D3DTOP_SELECTARG1 },
		{ 0, D3DTSS_COLORARG1, D3DTA_TEXTURE },
		{ 0, D3DTSS_COLORARG2, D3DTA_DIFFUSE },
		{ 0, D3DTSS_ALPHAOP, D3DTOP_SELECTARG1 },
		{ 0, D3DTSS_ALPHAARG1, D3DTA_TEXTURE },
		{ 0, D3DTSS_ALPHAARG2, D3DTA_DIFFUSE },
		{ 1, D3DTSS_COLOROP, D3DTOP_DISABLE },
		{ 1, D3DTSS_ALPHAOP, D3DTOP_DISABLE },
		{ 2, D3DTSS_COLOROP, D3DTOP_DISABLE },
		{ 2, D3DTSS_ALPHAOP, D3DTOP_DISABLE },
	};

	for(const auto& tss : kTexStageStates) {
		hr = dev->SetTextureStageState(tss.mStage, tss.mState, tss.mValue);

		if (FAILED(hr)) {
			VDDEBUG_DX9DISP("VideoDisplay/DX9: Failed to set texture stage state! hr=%08x %s", hr, VDDispDecodeD3D9Error(hr));
			return false;
		}
	}

	static const struct SamplerState {
		D3DSAMPLERSTATETYPE mState;
		DWORD mValue;
	} kSamplerStatesPoint[] = {
		{ D3DSAMP_MINFILTER, D3DTEXF_POINT },
		{ D3DSAMP_MAGFILTER, D3DTEXF_POINT },
		{ D3DSAMP_MIPFILTER, D3DTEXF_NONE },
		{ D3DSAMP_ADDRESSU, D3DTADDRESS_CLAMP },
		{ D3DSAMP_ADDRESSV, D3DTADDRESS_CLAMP },
	}, kSamplerStatesBilinear[] = {
		{ D3DSAMP_MINFILTER, D3DTEXF_LINEAR },
		{ D3DSAMP_MAGFILTER, D3DTEXF_LINEAR },
		{ D3DSAMP_MIPFILTER, D3DTEXF_NONE },
		{ D3DSAMP_ADDRESSU, D3DTADDRESS_CLAMP },
		{ D3DSAMP_ADDRESSV, D3DTADDRESS_CLAMP },
	};

	for(const auto& ss : bilinear ? kSamplerStatesBilinear : kSamplerStatesPoint) {
		hr = dev->SetSamplerState(0, ss.mState, ss.mValue);

		if (FAILED(hr)) {
			VDDEBUG_DX9DISP("VideoDisplay/DX9: Failed to set sampler state! hr=%08x %s", hr, VDDispDecodeD3D9Error(hr));
			return false;
		}
	}

	hr = dev->SetTexture(0, ctx.mpSourceTexture1);
	if (FAILED(hr)) {
		VDDEBUG_DX9DISP("VideoDisplay/DX9: Failed to set texture! hr=%08x %s", hr, VDDispDecodeD3D9Error(hr));
		return false;
	}

	// change viewport
	D3DVIEWPORT9 vp;
	vp.X = ctx.mViewportX;
	vp.Y = ctx.mViewportY;
	vp.Width = ctx.mViewportW;
	vp.Height = ctx.mViewportH;
	vp.MinZ = 0;
	vp.MaxZ = 1;

	hr = dev->SetViewport(&vp);
	if (FAILED(hr)) {
		VDDEBUG_DX9DISP("VideoDisplay/DX9: Failed to set viewport! hr=%08x %s", hr, VDDispDecodeD3D9Error(hr));
		return false;
	}

	// render!
	bool validDraw = true;

	const float ustep = 1.0f / (float)(int)ctx.mSourceTexW;
	const float vstep = 1.0f / (float)(int)ctx.mSourceTexH;
	const float u0 = ctx.mSourceArea.left   * ustep;
	const float v0 = ctx.mSourceArea.top    * vstep;
	const float u1 = ctx.mSourceArea.right  * ustep;
	const float v1 = ctx.mSourceArea.bottom * vstep;

	const float invVpW = 1.f / (float)vp.Width;
	const float invVpH = 1.f / (float)vp.Height;
	const float xstep =  2.0f * invVpW;
	const float ystep = -2.0f * invVpH;

	const float x0 = -1.0f - invVpW + ctx.mOutputX * xstep;
	const float y0 =  1.0f + invVpH + ctx.mOutputY * ystep;
	const float x1 = x0 + ctx.mOutputW * xstep;
	const float y1 = y0 + ctx.mOutputH * ystep;

	if (Vertex *pvx = mpManager->LockVertices(4)) {
		vd_seh_guard_try {
			pvx[0].SetFF2(x0, y0, 0xFFFFFFFF, u0, v0, 0, 0);
			pvx[1].SetFF2(x1, y0, 0xFFFFFFFF, u1, v0, 1, 0);
			pvx[2].SetFF2(x0, y1, 0xFFFFFFFF, u0, v1, 0, 1);
			pvx[3].SetFF2(x1, y1, 0xFFFFFFFF, u1, v1, 1, 1);
		} vd_seh_guard_except {
			validDraw = false;
		}

		mpManager->UnlockVertices();
	}

	if (!validDraw) {
		VDDEBUG_DX9DISP("VideoDisplay/DX9: Invalid vertex buffer lock detected -- bailing.");
		return false;
	}

	if (!mpManager->BeginScene())
		return false;

	hr = mpManager->DrawArrays(D3DPT_TRIANGLESTRIP, 0, 2);

	if (FAILED(hr)) {
		VDDEBUG_DX9DISP("VideoDisplay/DX9: Failed to draw primitive! hr=%08x %s", hr, VDDispDecodeD3D9Error(hr));
		return false;
	}

	return true;
}

bool VDVideoDisplayDX9Manager::RunEffect(const EffectContext& ctx, const nsVDDisplay::TechniqueInfo& technique, IDirect3DSurface9 *pRTOverride) {
	mpManager->BeginScope(L"RunEffect");
	bool success = RunEffect2(ctx, technique, pRTOverride);
	mpManager->EndScope();

	return success;
}

bool VDVideoDisplayDX9Manager::RunEffect2(const EffectContext& ctx, const nsVDDisplay::TechniqueInfo& technique, IDirect3DSurface9 *pRTOverride) {
	using namespace nsVDDisplay;

	const int firstRTTIndex = ctx.mbHighPrecision ? 2 : 0;

	IDirect3DTexture9 *const textures[15]={
		NULL,
		ctx.mpSourceTexture1,
		ctx.mpSourceTexture2,
		ctx.mpSourceTexture3,
		ctx.mpSourceTexture4,
		ctx.mpSourceTexture5,
		ctx.mpPaletteTexture,
		mpRTTs[firstRTTIndex] ? mpRTTs[firstRTTIndex]->GetD3DTexture() : NULL,
		mpRTTs[1] ? mpRTTs[1]->GetD3DTexture() : NULL,
		mpFilterTexture ? mpFilterTexture->GetD3DTexture() : NULL,
		mpHEvenOddTexture ? mpHEvenOddTexture->GetD3DTexture() : NULL,
		mpDitherTexture ? mpDitherTexture->GetD3DTexture() : NULL,
		ctx.mpInterpFilterH,
		ctx.mpInterpFilterV,
		ctx.mpInterpFilter
	};

	const D3DDISPLAYMODE& dmode = mpManager->GetDisplayMode();
	int clippedWidth = std::min<int>(ctx.mOutputX + ctx.mOutputW, dmode.Width);
	int clippedHeight = std::min<int>(ctx.mOutputY + ctx.mOutputH, dmode.Height);

	if (clippedWidth <= 0 || clippedHeight <= 0)
		return true;

	struct StdParamData {
		float vpsize[4];			// (viewport size)			vpwidth, vpheight, 1/vpheight, 1/vpwidth
		float srcsize[4];			// (source size)			srcwidth, srcheight, 1/srcheight, 1/srcwidth
		float texsize[4];			// (texture size)			texwidth, texheight, 1/texheight, 1/texwidth
		float tex2size[4];			// (texture2 size)			tex2width, tex2height, 1/tex2height, 1/tex2width
		float tempsize[4];			// (temp rtt size)			tempwidth, tempheight, 1/tempheight, 1/tempwidth
		float interphtexsize[4];	// (cubic htex interp info)
		float interpvtexsize[4];	// (cubic vtex interp info)
		float interptexsize[4];		// (interp tex info)
		float fieldinfo[4];			// (field information)		fieldoffset, -fieldoffset/4, und, und
		float chromauvscale[4];		// (chroma UV scale)		U scale, V scale, und, und
		float chromauvoffset[4];	// (chroma UV offset)		U offset, V offset, und, und
		float pixelsharpness[4];	// (pixel sharpness)		X factor, Y factor, ?, ?
	};

	VDASSERT(ctx.mOutputW);
	VDASSERT(ctx.mOutputH);
	VDASSERT(ctx.mSourceTexW);
	VDASSERT(ctx.mSourceTexH);
	VDASSERT(ctx.mSourceW);
	VDASSERT(ctx.mSourceH);

	StdParamData data;

	data.vpsize[0] = (float)ctx.mOutputW;
	data.vpsize[1] = (float)ctx.mOutputH;
	data.vpsize[2] = 1.0f / (float)ctx.mOutputH;
	data.vpsize[3] = 1.0f / (float)ctx.mOutputW;
	data.texsize[0] = (float)(int)ctx.mSourceTexW;
	data.texsize[1] = (float)(int)ctx.mSourceTexH;
	data.texsize[2] = 1.0f / (float)(int)ctx.mSourceTexH;
	data.texsize[3] = 1.0f / (float)(int)ctx.mSourceTexW;
	data.tex2size[0] = 1.f;
	data.tex2size[1] = 1.f;
	data.tex2size[2] = 1.f;
	data.tex2size[3] = 1.f;
	data.srcsize[0] = (float)(int)ctx.mSourceW;
	data.srcsize[1] = (float)(int)ctx.mSourceH;
	data.srcsize[2] = 1.0f / (float)(int)ctx.mSourceH;
	data.srcsize[3] = 1.0f / (float)(int)ctx.mSourceW;
	data.tempsize[0] = 1.f;
	data.tempsize[1] = 1.f;
	data.tempsize[2] = 1.f;
	data.tempsize[3] = 1.f;
	data.interphtexsize[0] = (float)ctx.mInterpHTexW;
	data.interphtexsize[1] = (float)ctx.mInterpHTexH;
	data.interphtexsize[2] = ctx.mInterpHTexH ? 1.0f / (float)ctx.mInterpHTexH : 0.0f;
	data.interphtexsize[3] = ctx.mInterpHTexW ? 1.0f / (float)ctx.mInterpHTexW : 0.0f;
	data.interpvtexsize[0] = (float)ctx.mInterpVTexW;
	data.interpvtexsize[1] = (float)ctx.mInterpVTexH;
	data.interpvtexsize[2] = ctx.mInterpVTexH ? 1.0f / (float)ctx.mInterpVTexH : 0.0f;
	data.interpvtexsize[3] = ctx.mInterpVTexW ? 1.0f / (float)ctx.mInterpVTexW : 0.0f;
	data.interptexsize[0] = (float)ctx.mInterpTexW;
	data.interptexsize[1] = (float)ctx.mInterpTexH;
	data.interptexsize[2] = ctx.mInterpTexH ? 1.0f / (float)ctx.mInterpTexH : 0.0f;
	data.interptexsize[3] = ctx.mInterpTexW ? 1.0f / (float)ctx.mInterpTexW : 0.0f;
	data.fieldinfo[0] = ctx.mFieldOffset;
	data.fieldinfo[1] = ctx.mFieldOffset * -0.25f;
	data.fieldinfo[2] = 0.0f;
	data.fieldinfo[3] = 0.0f;
	data.chromauvscale[0] = ctx.mChromaScaleU;
	data.chromauvscale[1] = ctx.mChromaScaleV;
	data.chromauvscale[2] = 0.0f;
	data.chromauvscale[3] = 0.0f;
	data.chromauvoffset[0] = ctx.mChromaOffsetU;
	data.chromauvoffset[1] = ctx.mChromaOffsetV;
	data.chromauvoffset[2] = 0.0f;
	data.chromauvoffset[3] = 0.0f;
	data.pixelsharpness[0] = ctx.mPixelSharpnessX;
	data.pixelsharpness[1] = ctx.mPixelSharpnessY;
	data.pixelsharpness[2] = 0.0f;
	data.pixelsharpness[3] = 0.0f;

	if (ctx.mpSourceTexture2) {
		D3DSURFACE_DESC desc;

		HRESULT hr = ctx.mpSourceTexture2->GetLevelDesc(0, &desc);
		if (FAILED(hr))
			return false;

		float w = (float)desc.Width;
		float h = (float)desc.Height;

		data.tex2size[0] = w;
		data.tex2size[1] = h;
		data.tex2size[2] = 1.0f / h;
		data.tex2size[3] = 1.0f / w;
	}

	if (mpRTTs[firstRTTIndex]) {
		data.tempsize[0] = (float)mpRTTs[firstRTTIndex]->GetWidth();
		data.tempsize[1] = (float)mpRTTs[firstRTTIndex]->GetHeight();
		data.tempsize[2] = 1.0f / data.tempsize[1];
		data.tempsize[3] = 1.0f / data.tempsize[0];
	}

	IDirect3DDevice9 *dev = mpManager->GetDevice();
	bool rtmain = true;

	for(const PassInfo& pi : technique.mPasses) {

		// bind vertex and pixel shaders
		HRESULT hr = dev->SetVertexShader(pi.mVertexShaderIndex >= 0 ? mVertexShaders[pi.mVertexShaderIndex] : NULL);
		if (FAILED(hr)) {
			VDDEBUG_DX9DISP("VideoDisplay/DX9: Couldn't set vertex shader! hr=%08x %s", hr, VDDispDecodeD3D9Error(hr));
			return false;
		}

		hr = dev->SetPixelShader(pi.mPixelShaderIndex >= 0 ? mPixelShaders[pi.mPixelShaderIndex] : NULL);
		if (FAILED(hr)) {
			VDDEBUG_DX9DISP("VideoDisplay/DX9: Couldn't set pixel shader! hr=%08x %s", hr, VDDispDecodeD3D9Error(hr));
			return false;
		}

		if (pi.mBumpEnvScale) {
			const float scaleData[4] = {
				data.texsize[3],
				0,
				0,
				data.texsize[2],
			};

			DWORD scaleData2[4];
			memcpy(scaleData2, scaleData, sizeof scaleData2);

			for(int i=0; i<4; ++i) {
				hr = dev->SetTextureStageState(1, (D3DTEXTURESTAGESTATETYPE)(D3DTSS_BUMPENVMAT00 + i), scaleData2[i]);

				if (FAILED(hr)) {
					VDDEBUG_DX9DISP("VideoDisplay/DX9: Failed to set state! hr=%08x %s", hr, VDDispDecodeD3D9Error(hr));
					return false;
				}
			}
		}

		// upload shader constants
		hr = dev->SetVertexShaderConstantF(0, (const float *)&data, sizeof data / sizeof(float[4]));
		if (FAILED(hr)) {
			VDDEBUG_DX9DISP("VideoDisplay/DX9: Failed to upload vertex shader constants! hr=%08x %s", hr, VDDispDecodeD3D9Error(hr));
			return false;
		}

		hr = dev->SetPixelShaderConstantF(0, (const float *)&data, sizeof data / sizeof(float[4]));
		if (FAILED(hr)) {
			VDDEBUG_DX9DISP("VideoDisplay/DX9: Failed to upload pixel shader constants! hr=%08x %s", hr, VDDispDecodeD3D9Error(hr));
			return false;
		}

		// set textures
		for(auto&& texb : pi.mTextureBindings) {
			VDASSERT(texb.mTexture < vdcountof(textures));

			hr = dev->SetTexture(texb.mStage, textures[texb.mTexture]);

			if (SUCCEEDED(hr))
				hr = dev->SetSamplerState(texb.mStage, D3DSAMP_ADDRESSU, texb.mbWrapU ? D3DTADDRESS_WRAP : D3DTADDRESS_CLAMP);
			
			if (SUCCEEDED(hr))
				hr = dev->SetSamplerState(texb.mStage, D3DSAMP_ADDRESSV, texb.mbWrapV ? D3DTADDRESS_WRAP : D3DTADDRESS_CLAMP);
			
			const bool bilinear = texb.mbBilinear || (texb.mbAutoBilinear && ctx.mbAutoBilinear);

			if (SUCCEEDED(hr))
				hr = dev->SetSamplerState(texb.mStage, D3DSAMP_MINFILTER, bilinear ? D3DTEXF_LINEAR : D3DTEXF_POINT);

			if (SUCCEEDED(hr))
				hr = dev->SetSamplerState(texb.mStage, D3DSAMP_MAGFILTER, bilinear ? D3DTEXF_LINEAR : D3DTEXF_POINT);

			if (SUCCEEDED(hr))
				hr = dev->SetSamplerState(texb.mStage, D3DSAMP_MIPFILTER, D3DTEXF_NONE);

			if (FAILED(hr)) {
				VDDEBUG_DX9DISP("VideoDisplay/DX9: Failed to set texture/sampler state! hr=%08x %s", hr, VDDispDecodeD3D9Error(hr));
				return false;
			}
		}

		// change render target
		if (pi.mRenderTarget >= 0) {
			if (!mpManager->EndScene())
				return false;

			HRESULT hr = E_FAIL;
			rtmain = false;

			switch(pi.mRenderTarget) {
				case 0:
					hr = dev->SetRenderTarget(0, pRTOverride ? pRTOverride : mpManager->GetRenderTarget());
					rtmain = true;
					break;
				case 1:
					if (mpRTTs[firstRTTIndex]) {
						IDirect3DSurface9 *pSurf;
						hr = mpRTTs[firstRTTIndex]->GetD3DTexture()->GetSurfaceLevel(0, &pSurf);
						if (SUCCEEDED(hr)) {
							hr = dev->SetRenderTarget(0, pSurf);
							pSurf->Release();
						}
					}
					break;
				case 2:
					if (mpRTTs[1]) {
						IDirect3DSurface9 *pSurf;
						hr = mpRTTs[1]->GetD3DTexture()->GetSurfaceLevel(0, &pSurf);
						if (SUCCEEDED(hr)) {
							hr = dev->SetRenderTarget(0, pSurf);
							pSurf->Release();
						}
					}
					break;
			}

			if (FAILED(hr)) {
				VDDEBUG_DX9DISP("VideoDisplay/DX9: Failed to set render target! hr=%08x %s", hr, VDDispDecodeD3D9Error(hr));
				return false;
			}
		}

		// change viewport
		D3DVIEWPORT9 vp;
		if (pi.mViewportW | pi.mViewportH) {
			HRESULT hr;

			IDirect3DSurface9 *rt;
			hr = dev->GetRenderTarget(0, &rt);
			if (SUCCEEDED(hr)) {
				D3DSURFACE_DESC desc;
				hr = rt->GetDesc(&desc);
				if (SUCCEEDED(hr)) {
					const DWORD hsizes[4]={ desc.Width, ctx.mSourceW, (DWORD)clippedWidth, (DWORD)ctx.mOutputW };
					const DWORD vsizes[4]={ desc.Height, ctx.mSourceH, (DWORD)clippedHeight, (DWORD)ctx.mOutputH };

					vp.X = rtmain ? ctx.mViewportX : 0;
					vp.Y = rtmain ? ctx.mViewportY : 0;
					vp.Width = hsizes[pi.mViewportW];
					vp.Height = vsizes[pi.mViewportH];
					vp.MinZ = 0;
					vp.MaxZ = 1;

					hr = dev->SetViewport(&vp);
				}
				rt->Release();
			}

			if (FAILED(hr)) {
				VDDEBUG_DX9DISP("VideoDisplay/DX9: Failed to set viewport! hr=%08x %s", hr, VDDispDecodeD3D9Error(hr));
				return false;
			}
		} else {
			vp.X = ctx.mViewportX;
			vp.Y = ctx.mViewportY;
			vp.Width = ctx.mViewportW;
			vp.Height = ctx.mViewportH;
			vp.MinZ = 0;
			vp.MaxZ = 1;

			HRESULT hr = dev->SetViewport(&vp);
			if (FAILED(hr)) {
				VDDEBUG_DX9DISP("VideoDisplay/DX9: Failed to retrieve viewport! hr=%08x %s", hr, VDDispDecodeD3D9Error(hr));
				return false;
			}
		}

		// render!
		bool validDraw = true;

		const float ustep = 1.0f / (float)(int)ctx.mSourceTexW * ctx.mDefaultUVScaleCorrectionX;
		const float vstep = 1.0f / (float)(int)ctx.mSourceTexH * ctx.mDefaultUVScaleCorrectionY;
		const float voffset = (pi.mbClipPosition ? ctx.mFieldOffset * vstep * -0.25f : 0.0f);
		float u0 = ctx.mSourceArea.left   * ustep;
		float v0 = ctx.mSourceArea.top    * vstep + voffset;
		float u1 = ctx.mSourceArea.right  * ustep;
		float v1 = ctx.mSourceArea.bottom * vstep + voffset;

		const float invVpW = 1.f / (float)vp.Width;
		const float invVpH = 1.f / (float)vp.Height;
		const float xstep =  2.0f * invVpW;
		const float ystep = -2.0f * invVpH;

		const float x0 = -1.0f - invVpW + ctx.mOutputX * xstep;
		const float y0 =  1.0f + invVpH + ctx.mOutputY * ystep;
		const float x1 = pi.mbClipPosition ? x0 + ctx.mOutputW * xstep : 1.f - invVpW;
		const float y1 = pi.mbClipPosition ? y0 + ctx.mOutputH * ystep : -1.f + invVpH;

		float u2 = 0;
		float u3 = 1;
		float v2 = 0;
		float v3 = 1;

		if (ctx.mbUseUV0Scale) {
			u0 *= ctx.mUV0Scale.x;
			v0 *= ctx.mUV0Scale.y;
			u1 *= ctx.mUV0Scale.x;
			v1 *= ctx.mUV0Scale.y;
		}

		if (ctx.mbUseUV1Area) {
			u2 = ctx.mUV1Area.left;
			v2 = ctx.mUV1Area.top;
			u3 = ctx.mUV1Area.right;
			v3 = ctx.mUV1Area.bottom;
		}

		if (Vertex *pvx = mpManager->LockVertices(4)) {
			vd_seh_guard_try {
				pvx[0].SetFF2(x0, y0, 0xFFFFFFFF, u0, v0, u2, v2);
				pvx[1].SetFF2(x1, y0, 0xFFFFFFFF, u1, v0, u3, v2);
				pvx[2].SetFF2(x0, y1, 0xFFFFFFFF, u0, v1, u2, v3);
				pvx[3].SetFF2(x1, y1, 0xFFFFFFFF, u1, v1, u3, v3);
			} vd_seh_guard_except {
				validDraw = false;
			}

			mpManager->UnlockVertices();
		}

		if (!validDraw) {
			VDDEBUG_DX9DISP("VideoDisplay/DX9: Invalid vertex buffer lock detected -- bailing.");
			return false;
		}

		if (!mpManager->BeginScene())
			return false;

		hr = mpManager->DrawArrays(D3DPT_TRIANGLESTRIP, 0, 2);

		if (FAILED(hr)) {
			VDDEBUG_DX9DISP("VideoDisplay/DX9: Failed to draw primitive! hr=%08x %s", hr, VDDispDecodeD3D9Error(hr));
			return false;
		}
	}

	// NVPerfHUD 3.1 draws a bit funny if we leave this set to REVSUBTRACT, even
	// with alpha blending off....
	dev->SetRenderState(D3DRS_BLENDOP, D3DBLENDOP_ADD);

	return true;
}

///////////////////////////////////////////////////////////////////////////

class VDVideoUploadContextD3D9 final : public vdrefcounted<IVDVideoUploadContextD3D9>, public VDD3D9Client {
public:
	VDVideoUploadContextD3D9();
	~VDVideoUploadContextD3D9();

	IDirect3DTexture9 *GetD3DTexture(int i = 0) {
		return !mpD3DConversionTextures.empty() ? mpD3DConversionTextures[i] : mpD3DImageTextures[i];
	}

	IDirect3DTexture9 *const *GetD3DTextures() {
		return !mpD3DConversionTextures.empty() ? mpD3DConversionTextures.data() : mpD3DImageTextures.data();
	}

	bool Init(void *hmonitor, bool use9ex, const VDPixmap& source, bool allowConversion, bool highPrecision, int buffers, bool use16bit);
	void Shutdown();

	void SetBufferCount(uint32 buffers);
	bool Update(const VDPixmap& source);

protected:
	bool Lock(IDirect3DTexture9 *tex, IDirect3DTexture9 *upload, D3DLOCKED_RECT *lr);
	bool Unlock(IDirect3DTexture9 *tex, IDirect3DTexture9 *upload);

	void OnPreDeviceReset() override;
	void OnPostDeviceReset() override;

	bool ReinitImageTextures();
	bool ReinitVRAMTextures();
	void ClearImageTexture(uint32 i);

	VDD3D9Manager	*mpManager = nullptr;
	vdrefptr<VDVideoDisplayDX9Manager> mpVideoManager;

	enum UploadMode {
		kUploadModeNormal,
		kUploadModeDirect8,
		kUploadModeDirect16
	} mUploadMode = {};

	int mBufferCount = 0;
	int	mConversionTexW = 0;
	int	mConversionTexH = 0;
	bool mbHighPrecision = false;
	bool mbPaletteTextureValid = false;
	bool mbPaletteTextureIdentity = false;

	int mSourceFmt = 0;
	D3DFORMAT mTexD3DFmt = {};
	VDPixmap			mTexFmt;
	VDPixmapCachedBlitter mCachedBlitter;

	vdfastvector<IDirect3DTexture9 *> mpD3DImageTextures;
	IDirect3DTexture9	*mpD3DImageTextureUpload = nullptr;
	IDirect3DTexture9	*mpD3DPaletteTexture = nullptr;
	IDirect3DTexture9	*mpD3DPaletteTextureUpload = nullptr;
	IDirect3DTexture9	*mpD3DImageTexture2a = nullptr;
	IDirect3DTexture9	*mpD3DImageTexture2aUpload = nullptr;
	IDirect3DTexture9	*mpD3DImageTexture2b = nullptr;
	IDirect3DTexture9	*mpD3DImageTexture2bUpload = nullptr;
	IDirect3DTexture9	*mpD3DImageTexture2c = nullptr;
	IDirect3DTexture9	*mpD3DImageTexture2cUpload = nullptr;
	IDirect3DTexture9	*mpD3DImageTexture2d = nullptr;
	IDirect3DTexture9	*mpD3DImageTexture2dUpload = nullptr;
	vdfastvector<IDirect3DTexture9 *> mpD3DConversionTextures;

	vdblock<uint32> mLastPalette;
};

bool VDCreateVideoUploadContextD3D9(IVDVideoUploadContextD3D9 **ppContext) {
	return VDRefCountObjectFactory<VDVideoUploadContextD3D9, IVDVideoUploadContextD3D9>(ppContext);
}

VDVideoUploadContextD3D9::VDVideoUploadContextD3D9() {
}

VDVideoUploadContextD3D9::~VDVideoUploadContextD3D9() {
	Shutdown();
}

bool VDVideoUploadContextD3D9::Init(void *hmonitor, bool use9ex, const VDPixmap& source, bool allowConversion, bool highPrecision, int buffers, bool use16bit) {
	mCachedBlitter.Invalidate();

	mBufferCount = buffers;
	mbHighPrecision = highPrecision;

	VDASSERT(!mpManager);
	mpManager = VDInitDirect3D9(this, (HMONITOR)hmonitor, use9ex);
	if (!mpManager)
		return false;

	if (!VDInitDisplayDX9((HMONITOR)hmonitor, use9ex, ~mpVideoManager)) {
		Shutdown();
		return false;
	}

	// check capabilities
	const D3DCAPS9& caps = mpManager->GetCaps();

	if (caps.MaxTextureWidth < (uint32)source.w || caps.MaxTextureHeight < (uint32)source.h) {
		VDDEBUG_DX9DISP("VideoDisplay/DX9: source image is larger than maximum texture size");
		Shutdown();
		return false;
	}

	// high precision requires VS/PS 2.0
	if (caps.VertexShaderVersion < D3DVS_VERSION(2, 0) || caps.PixelShaderVersion < D3DPS_VERSION(2, 0)) {
		mbHighPrecision = false;
	}

	// create source texture
	int texw = source.w;
	int texh = source.h;

	mpManager->AdjustTextureSize(texw, texh);

	memset(&mTexFmt, 0, sizeof mTexFmt);
	mTexFmt.format		= nsVDPixmap::kPixFormat_XRGB8888;

	if (use16bit) {
		if (mpManager->IsTextureFormatAvailable(D3DFMT_R5G6B5))
			mTexFmt.format		= nsVDPixmap::kPixFormat_RGB565;
		else if (mpManager->IsTextureFormatAvailable(D3DFMT_X1R5G5B5))
			mTexFmt.format		= nsVDPixmap::kPixFormat_XRGB1555;
	}

	mSourceFmt = source.format;

	HRESULT hr;
	D3DFORMAT d3dfmt;
	IDirect3DDevice9 *dev = mpManager->GetDevice();

	mUploadMode = kUploadModeNormal;

	const bool useDefault = (mpManager->GetDeviceEx() != NULL);
	const D3DPOOL texPool = useDefault ? D3DPOOL_DEFAULT : D3DPOOL_MANAGED;

	switch(source.format) {
		case nsVDPixmap::kPixFormat_YUV410_Planar_709:
		case nsVDPixmap::kPixFormat_YUV410_Planar_FR:
		case nsVDPixmap::kPixFormat_YUV410_Planar_709_FR:
		case nsVDPixmap::kPixFormat_YUV420_Planar_709:
		case nsVDPixmap::kPixFormat_YUV420_Planar_FR:
		case nsVDPixmap::kPixFormat_YUV420_Planar_709_FR:
		case nsVDPixmap::kPixFormat_YUV422_Planar_709:
		case nsVDPixmap::kPixFormat_YUV422_Planar_FR:
		case nsVDPixmap::kPixFormat_YUV422_Planar_709_FR:
		case nsVDPixmap::kPixFormat_YUV444_Planar_709:
		case nsVDPixmap::kPixFormat_YUV444_Planar_FR:
		case nsVDPixmap::kPixFormat_YUV444_Planar_709_FR:
			if (mpManager->IsTextureFormatAvailable(D3DFMT_L8) && caps.PixelShaderVersion >= D3DPS_VERSION(2, 0)) {
				mUploadMode = kUploadModeDirect8;
				d3dfmt = D3DFMT_L8;

				uint32 subw = texw;
				uint32 subh = texh;

				switch(source.format) {
					case nsVDPixmap::kPixFormat_YUV444_Planar:
					case nsVDPixmap::kPixFormat_YUV444_Planar_709:
					case nsVDPixmap::kPixFormat_YUV444_Planar_FR:
					case nsVDPixmap::kPixFormat_YUV444_Planar_709_FR:
						break;
					case nsVDPixmap::kPixFormat_YUV422_Planar:
					case nsVDPixmap::kPixFormat_YUV422_Planar_709:
					case nsVDPixmap::kPixFormat_YUV422_Planar_FR:
					case nsVDPixmap::kPixFormat_YUV422_Planar_709_FR:
						subw >>= 1;
						break;
					case nsVDPixmap::kPixFormat_YUV420_Planar:
					case nsVDPixmap::kPixFormat_YUV420_Planar_709:
					case nsVDPixmap::kPixFormat_YUV420_Planar_FR:
					case nsVDPixmap::kPixFormat_YUV420_Planar_709_FR:
					case nsVDPixmap::kPixFormat_YUV410_Planar:
					case nsVDPixmap::kPixFormat_YUV410_Planar_709:
					case nsVDPixmap::kPixFormat_YUV410_Planar_FR:
					case nsVDPixmap::kPixFormat_YUV410_Planar_709_FR:
						subw >>= 2;
						subh >>= 2;
						break;
				}

				if (subw < 1)
					subw = 1;
				if (subh < 1)
					subh = 1;

				hr = dev->CreateTexture(subw, subh, 1, 0, D3DFMT_L8, texPool, &mpD3DImageTexture2a, NULL);
				if (FAILED(hr)) {
					Shutdown();
					return false;
				}

				hr = dev->CreateTexture(subw, subh, 1, 0, D3DFMT_L8, texPool, &mpD3DImageTexture2b, NULL);
				if (FAILED(hr)) {
					Shutdown();
					return false;
				}

				if (useDefault) {
					hr = dev->CreateTexture(subw, subh, 1, 0, D3DFMT_L8, D3DPOOL_SYSTEMMEM, &mpD3DImageTexture2aUpload, NULL);
					if (FAILED(hr)) {
						Shutdown();
						return false;
					}

					hr = dev->CreateTexture(subw, subh, 1, 0, D3DFMT_L8, D3DPOOL_SYSTEMMEM, &mpD3DImageTexture2bUpload, NULL);
					if (FAILED(hr)) {
						Shutdown();
						return false;
					}
				}
			}
			break;

		case nsVDPixmap::kPixFormat_Pal8:
		case nsVDPixmap::kPixFormat_YUV410_Planar:
		case nsVDPixmap::kPixFormat_YUV420_Planar:
		case nsVDPixmap::kPixFormat_YUV422_Planar:
		case nsVDPixmap::kPixFormat_YUV444_Planar:
			if (mpManager->IsTextureFormatAvailable(D3DFMT_L8) && caps.PixelShaderVersion >= D3DPS_VERSION(1, 1)) {
				mUploadMode = kUploadModeDirect8;
				d3dfmt = D3DFMT_L8;

				uint32 subw = texw;
				uint32 subh = texh;

				switch(source.format) {
					case nsVDPixmap::kPixFormat_Pal8:
					case nsVDPixmap::kPixFormat_YUV444_Planar:
						break;
					case nsVDPixmap::kPixFormat_YUV422_Planar:
						subw >>= 1;
						break;
					case nsVDPixmap::kPixFormat_YUV420_Planar:
						subw >>= 1;
						subh >>= 1;
						break;
					case nsVDPixmap::kPixFormat_YUV410_Planar:
						subw >>= 2;
						subh >>= 2;
						break;
				}

				if (subw < 1)
					subw = 1;
				if (subh < 1)
					subh = 1;

				if (source.format == nsVDPixmap::kPixFormat_Pal8) {
					mbPaletteTextureValid = false;
					mLastPalette.resize(256);

					hr = dev->CreateTexture(256, 1, 1, 0, D3DFMT_X8R8G8B8, texPool, &mpD3DPaletteTexture, NULL);
					if (FAILED(hr)) {
						Shutdown();
						return false;
					}

					if (useDefault) {
						hr = dev->CreateTexture(256, 1, 1, 0, D3DFMT_X8R8G8B8, D3DPOOL_SYSTEMMEM, &mpD3DPaletteTextureUpload, NULL);
						if (FAILED(hr)) {
							Shutdown();
							return false;
						}
					}
				}

				hr = dev->CreateTexture(subw, subh, 1, 0, D3DFMT_L8, texPool, &mpD3DImageTexture2a, NULL);
				if (FAILED(hr)) {
					Shutdown();
					return false;
				}

				hr = dev->CreateTexture(subw, subh, 1, 0, D3DFMT_L8, texPool, &mpD3DImageTexture2b, NULL);
				if (FAILED(hr)) {
					Shutdown();
					return false;
				}

				if (useDefault) {
					hr = dev->CreateTexture(subw, subh, 1, 0, D3DFMT_L8, D3DPOOL_SYSTEMMEM, &mpD3DImageTexture2aUpload, NULL);
					if (FAILED(hr)) {
						Shutdown();
						return false;
					}

					hr = dev->CreateTexture(subw, subh, 1, 0, D3DFMT_L8, D3DPOOL_SYSTEMMEM, &mpD3DImageTexture2bUpload, NULL);
					if (FAILED(hr)) {
						Shutdown();
						return false;
					}
				}
			}
			break;
	}
	
	if (mUploadMode == kUploadModeNormal) {
		mpVideoManager->DetermineBestTextureFormat(source.format, mTexFmt.format, d3dfmt);

		if (source.format != mTexFmt.format) {
			if (!allowConversion) {
				Shutdown();
				return false;
			}
		}
	}

	if (mUploadMode != kUploadModeNormal) {
		mpD3DImageTextures.resize(1, nullptr);
		mpD3DConversionTextures.resize(buffers, nullptr);
	} else {
		mpD3DImageTextures.resize(buffers, nullptr);
	}

	mConversionTexW = texw;
	mConversionTexH = texh;
	if (!ReinitVRAMTextures()) {
		Shutdown();
		return false;
	}

	if (mUploadMode == kUploadModeDirect16) {
		texw = (source.w + 1) >> 1;
		texh = source.h;
		mpManager->AdjustTextureSize(texw, texh);
	}

	mTexFmt.w			= texw;
	mTexFmt.h			= texh;
	mTexD3DFmt = d3dfmt;

	if (!ReinitImageTextures()) {
		Shutdown();
		return false;
	}

	if (useDefault) {
		hr = dev->CreateTexture(texw, texh, 1, 0, d3dfmt, D3DPOOL_SYSTEMMEM, &mpD3DImageTextureUpload, nullptr);
		if (FAILED(hr)) {
			Shutdown();
			return false;
		}
	}

	// clear source textures
	for(uint32 i=0; i<mpD3DImageTextures.size(); ++i) {
		ClearImageTexture(i);
	}

	VDDEBUG_DX9DISP("VideoDisplay/DX9: Init successful for %dx%d source image (%s -> %s); monitor=%p", source.w, source.h, VDPixmapGetInfo(source.format).name, VDPixmapGetInfo(mTexFmt.format).name, hmonitor);
	return true;
}

void VDVideoUploadContextD3D9::Shutdown() {
	for(IDirect3DTexture9 *&tex : mpD3DConversionTextures)
		vdsaferelease <<= tex;

	for(IDirect3DTexture9 *&tex : mpD3DImageTextures)
		vdsaferelease <<= tex;

	vdsaferelease <<= mpD3DPaletteTexture;
	vdsaferelease <<= mpD3DPaletteTextureUpload;
	vdsaferelease <<= mpD3DImageTexture2d;
	vdsaferelease <<= mpD3DImageTexture2dUpload;
	vdsaferelease <<= mpD3DImageTexture2c;
	vdsaferelease <<= mpD3DImageTexture2cUpload;
	vdsaferelease <<= mpD3DImageTexture2b;
	vdsaferelease <<= mpD3DImageTexture2bUpload;
	vdsaferelease <<= mpD3DImageTexture2a;
	vdsaferelease <<= mpD3DImageTexture2aUpload;
	vdsaferelease <<= mpD3DImageTextureUpload;

	mpVideoManager = NULL;
	if (mpManager) {
		VDDeinitDirect3D9(mpManager, this);
		mpManager = NULL;
	}
}

void VDVideoUploadContextD3D9::SetBufferCount(uint32 buffers) {
	if (buffers == 0)
		buffers = 1;

	if (mpD3DConversionTextures.empty()) {
		while(mpD3DImageTextures.size() > buffers) {
			vdsaferelease <<= mpD3DImageTextures.back();
			mpD3DImageTextures.pop_back();
		}

		mpD3DImageTextures.resize(buffers, nullptr);

		ReinitImageTextures();
	} else {
		while(mpD3DConversionTextures.size() > buffers) {
			vdsaferelease <<= mpD3DConversionTextures.back();
			mpD3DConversionTextures.pop_back();
		}

		mpD3DConversionTextures.resize(buffers, nullptr);

		ReinitVRAMTextures();
	}
}

bool VDVideoUploadContextD3D9::Update(const VDPixmap& source) {
	using namespace nsVDDisplay;

	if (mpD3DConversionTextures.size() > 1)
		std::rotate(mpD3DConversionTextures.begin(), mpD3DConversionTextures.end() - 1, mpD3DConversionTextures.end());

	if (mpD3DImageTextures.size() > 1)
		std::rotate(mpD3DImageTextures.begin(), mpD3DImageTextures.end() - 1, mpD3DImageTextures.end());

	D3DLOCKED_RECT lr;
	HRESULT hr;

	if (mpD3DPaletteTexture) {
		bool paletteValid = mbPaletteTextureValid;
		if (paletteValid) {
			if (source.palette) {
				if (memcmp(mLastPalette.data(), source.palette, sizeof(uint32)*256))
					paletteValid = false;
			} else {
				if (!mbPaletteTextureIdentity)
					paletteValid = false;
			}
		}

		if (!paletteValid) {
			if (!Lock(mpD3DPaletteTexture, mpD3DPaletteTextureUpload, &lr))
				return false;

			if (source.palette) {
				memcpy(mLastPalette.data(), source.palette, 256*4);

				mbPaletteTextureIdentity = false;
			} else {
				uint32 *dst = mLastPalette.data();
				uint32 v = 0;
				for(uint32 i=0; i<256; ++i) {
					*dst++ = v;
					v += 0x010101;
				}

				mbPaletteTextureIdentity = true;
			}

			memcpy(lr.pBits, mLastPalette.data(), 256*4);

			VDVERIFY(Unlock(mpD3DPaletteTexture, mpD3DPaletteTextureUpload));

			mbPaletteTextureValid = true;
		}
	}
	
	if (!Lock(mpD3DImageTextures[0], mpD3DImageTextureUpload, &lr))
		return false;

	mTexFmt.data		= lr.pBits;
	mTexFmt.pitch		= lr.Pitch;

	VDPixmap dst(mTexFmt);
	VDPixmap src(source);

	if (mUploadMode == kUploadModeDirect16) {
		VDMemcpyRect(dst.data, dst.pitch, src.data, src.pitch, src.w * 2, src.h);
	} else if (mUploadMode == kUploadModeDirect8) {
		VDMemcpyRect(dst.data, dst.pitch, src.data, src.pitch, src.w, src.h);
	} else {
		if (dst.w > src.w)
			dst.w = src.w;
		
		if (dst.h > src.h)
			dst.h = src.h;

		mCachedBlitter.Blit(dst, src);
	}

	VDVERIFY(Unlock(mpD3DImageTextures[0], mpD3DImageTextureUpload));

	if (mUploadMode == kUploadModeDirect8) {
		uint32 subw = source.w;
		uint32 subh = source.h;

		switch(source.format) {
			case nsVDPixmap::kPixFormat_YUV410_Planar:
			case nsVDPixmap::kPixFormat_YUV410_Planar_FR:
			case nsVDPixmap::kPixFormat_YUV410_Planar_709:
			case nsVDPixmap::kPixFormat_YUV410_Planar_709_FR:
				subw >>= 2;
				subh >>= 2;
				break;
			case nsVDPixmap::kPixFormat_YUV420_Planar:
			case nsVDPixmap::kPixFormat_YUV420_Planar_FR:
			case nsVDPixmap::kPixFormat_YUV420_Planar_709:
			case nsVDPixmap::kPixFormat_YUV420_Planar_709_FR:
				subw >>= 1;
				subh >>= 1;
				break;
			case nsVDPixmap::kPixFormat_YUV422_Planar:
			case nsVDPixmap::kPixFormat_YUV422_Planar_FR:
			case nsVDPixmap::kPixFormat_YUV422_Planar_709:
			case nsVDPixmap::kPixFormat_YUV422_Planar_709_FR:
				subw >>= 1;
				break;
			case nsVDPixmap::kPixFormat_YUV444_Planar:
			case nsVDPixmap::kPixFormat_YUV444_Planar_FR:
			case nsVDPixmap::kPixFormat_YUV444_Planar_709:
			case nsVDPixmap::kPixFormat_YUV444_Planar_709_FR:
				break;
		}

		if (subw < 1)
			subw = 1;
		if (subh < 1)
			subh = 1;

		if (source.format != nsVDPixmap::kPixFormat_Pal8) {
			// upload Cb plane
			if (!Lock(mpD3DImageTexture2a, mpD3DImageTexture2aUpload, &lr))
				return false;

			VDMemcpyRect(lr.pBits, lr.Pitch, source.data2, source.pitch2, subw, subh);

			VDVERIFY(Unlock(mpD3DImageTexture2a, mpD3DImageTexture2aUpload));

			// upload Cr plane
			if (!Lock(mpD3DImageTexture2b, mpD3DImageTexture2bUpload, &lr))
				return false;

			VDMemcpyRect(lr.pBits, lr.Pitch, source.data3, source.pitch3, subw, subh);

			VDVERIFY(Unlock(mpD3DImageTexture2b, mpD3DImageTexture2bUpload));
		}
	}

	if (mUploadMode != kUploadModeNormal) {
		IDirect3DDevice9 *dev = mpManager->GetDevice();
		vdrefptr<IDirect3DSurface9> rtsurface;

		hr = mpD3DConversionTextures[0]->GetSurfaceLevel(0, ~rtsurface);
		if (FAILED(hr))
			return false;

		hr = dev->SetStreamSource(0, mpManager->GetVertexBuffer(), 0, sizeof(Vertex));
		if (FAILED(hr))
			return false;

		hr = dev->SetIndices(mpManager->GetIndexBuffer());
		if (FAILED(hr))
			return false;

		hr = dev->SetFVF(D3DFVF_XYZ | D3DFVF_DIFFUSE | D3DFVF_TEX2);
		if (FAILED(hr))
			return false;

		hr = dev->SetRenderTarget(0, rtsurface);
		if (FAILED(hr))
			return false;

		static const uint32 kRenderStates[][2]={
			{	D3DRS_LIGHTING,			FALSE				},
			{	D3DRS_CULLMODE,			D3DCULL_NONE		},
			{	D3DRS_ZENABLE,			FALSE				},
			{	D3DRS_ALPHATESTENABLE,	FALSE				},
			{	D3DRS_ALPHABLENDENABLE,	FALSE				},
			{	D3DRS_STENCILENABLE,	FALSE				},
		};

		for(int i=0; i<sizeof(kRenderStates)/sizeof(kRenderStates[0]); ++i) {
			const uint32 (&rs)[2] = kRenderStates[i];

			hr = dev->SetRenderState((D3DRENDERSTATETYPE)rs[0], rs[1]);
			if (FAILED(hr))
				return false;
		}

		bool success = false;
		if (mpManager->BeginScene()) {
			success = true;

			D3DVIEWPORT9 vp = { 0, 0, (DWORD)source.w, (DWORD)source.h, 0, 1 };
			hr = dev->SetViewport(&vp);
			if (FAILED(hr))
				success = false;

			if (success) {
				VDVideoDisplayDX9Manager::EffectContext ctx {};

				ctx.mpSourceTexture1 = mpD3DImageTextures[0];
				ctx.mpSourceTexture2 = mpD3DImageTexture2a;
				ctx.mpSourceTexture3 = mpD3DImageTexture2b;
				ctx.mpSourceTexture4 = mpD3DImageTexture2c;
				ctx.mpSourceTexture5 = mpD3DImageTexture2d;
				ctx.mpPaletteTexture = mpD3DPaletteTexture;
				ctx.mpInterpFilterH = NULL;
				ctx.mpInterpFilterV = NULL;
				ctx.mpInterpFilter = NULL;
				ctx.mSourceW = source.w;
				ctx.mSourceH = source.h;
				ctx.mSourceTexW = mTexFmt.w;
				ctx.mSourceTexH = mTexFmt.h;
				ctx.mSourceArea.set(0.0f, 0.0f, (float)source.w, (float)source.h);
				ctx.mInterpHTexW = 0;
				ctx.mInterpHTexH = 0;
				ctx.mInterpVTexW = 0;
				ctx.mInterpVTexH = 0;
				ctx.mInterpTexW = 0;
				ctx.mInterpTexH = 0;
				ctx.mViewportX = 0;
				ctx.mViewportY = 0;
				ctx.mViewportW = source.w;
				ctx.mViewportH = source.h;
				ctx.mOutputX = 0;
				ctx.mOutputY = 0;
				ctx.mOutputW = source.w;
				ctx.mOutputH = source.h;
				ctx.mDefaultUVScaleCorrectionX = 1.0f;
				ctx.mDefaultUVScaleCorrectionY = 1.0f;
				ctx.mChromaScaleU = 1.0f;
				ctx.mChromaScaleV = 1.0f;
				ctx.mChromaOffsetU = 0.0f;
				ctx.mChromaOffsetV = 0.0f;
				ctx.mbHighPrecision = mbHighPrecision;
				ctx.mFieldOffset = 0.0f;
				ctx.mPixelSharpnessX = 0.0f;
				ctx.mPixelSharpnessY = 0.0f;

				switch(source.format) {
					case nsVDPixmap::kPixFormat_YUV444_Planar_709:
						if (!mpVideoManager->RunEffect(ctx, g_technique_ycbcr_709_to_rgb_2_0, rtsurface))
							success = false;
						break;
					case nsVDPixmap::kPixFormat_YUV444_Planar_FR:
						if (!mpVideoManager->RunEffect(ctx, g_technique_ycbcr_601fr_to_rgb_2_0, rtsurface))
							success = false;
						break;
					case nsVDPixmap::kPixFormat_YUV444_Planar_709_FR:
						if (!mpVideoManager->RunEffect(ctx, g_technique_ycbcr_709fr_to_rgb_2_0, rtsurface))
							success = false;
						break;
					case nsVDPixmap::kPixFormat_YUV422_Planar_709:
						ctx.mChromaScaleU = 0.5f;
						ctx.mChromaOffsetU = -0.25f;
						if (!mpVideoManager->RunEffect(ctx, g_technique_ycbcr_709_to_rgb_2_0, rtsurface))
							success = false;
						break;
					case nsVDPixmap::kPixFormat_YUV422_Planar_FR:
						ctx.mChromaScaleU = 0.5f;
						ctx.mChromaOffsetU = -0.25f;
						if (!mpVideoManager->RunEffect(ctx, g_technique_ycbcr_601fr_to_rgb_2_0, rtsurface))
							success = false;
						break;
					case nsVDPixmap::kPixFormat_YUV422_Planar_709_FR:
						ctx.mChromaScaleU = 0.5f;
						ctx.mChromaOffsetU = -0.25f;
						if (!mpVideoManager->RunEffect(ctx, g_technique_ycbcr_709fr_to_rgb_2_0, rtsurface))
							success = false;
						break;
					case nsVDPixmap::kPixFormat_YUV420_Planar_709:
						ctx.mChromaScaleU = 0.5f;
						ctx.mChromaScaleV = 0.5f;
						ctx.mChromaOffsetU = -0.25f;
						if (!mpVideoManager->RunEffect(ctx, g_technique_ycbcr_709_to_rgb_2_0, rtsurface))
							success = false;
						break;
					case nsVDPixmap::kPixFormat_YUV420_Planar_FR:
						ctx.mChromaScaleU = 0.5f;
						ctx.mChromaScaleV = 0.5f;
						ctx.mChromaOffsetU = -0.25f;
						if (!mpVideoManager->RunEffect(ctx, g_technique_ycbcr_601fr_to_rgb_2_0, rtsurface))
							success = false;
						break;
					case nsVDPixmap::kPixFormat_YUV420_Planar_709_FR:
						ctx.mChromaScaleU = 0.5f;
						ctx.mChromaScaleV = 0.5f;
						ctx.mChromaOffsetU = -0.25f;
						if (!mpVideoManager->RunEffect(ctx, g_technique_ycbcr_709fr_to_rgb_2_0, rtsurface))
							success = false;
						break;

					// 4:1:0

					case nsVDPixmap::kPixFormat_YUV410_Planar_709:
						ctx.mChromaScaleU = 0.25f;
						ctx.mChromaScaleV = 0.25f;
						if (!mpVideoManager->RunEffect(ctx, g_technique_ycbcr_709_to_rgb_2_0, rtsurface))
							success = false;
						break;

					case nsVDPixmap::kPixFormat_YUV410_Planar_FR:
						ctx.mChromaScaleU = 0.25f;
						ctx.mChromaScaleV = 0.25f;
						if (!mpVideoManager->RunEffect(ctx, g_technique_ycbcr_601fr_to_rgb_2_0, rtsurface))
							success = false;
						break;

					case nsVDPixmap::kPixFormat_YUV410_Planar_709_FR:
						ctx.mChromaScaleU = 0.25f;
						ctx.mChromaScaleV = 0.25f;
						if (!mpVideoManager->RunEffect(ctx, g_technique_ycbcr_709fr_to_rgb_2_0, rtsurface))
							success = false;
						break;

					case nsVDPixmap::kPixFormat_Pal8:
						if (mpVideoManager->IsPS20Enabled()) {
							if (!mpVideoManager->RunEffect(ctx, g_technique_pal8_to_rgb_2_0, rtsurface))
								success = false;
						} else {
							if (!mpVideoManager->RunEffect(ctx, g_technique_pal8_to_rgb_1_1, rtsurface))
								success = false;
						}
						break;

					default:
						switch(source.format) {
							case nsVDPixmap::kPixFormat_YUV444_Planar:
								if (!mpVideoManager->RunEffect(ctx, g_technique_ycbcr_601_to_rgb_2_0, rtsurface))
									success = false;
								break;

							case nsVDPixmap::kPixFormat_YUV422_Planar:
								ctx.mChromaScaleU = 0.5f;
								ctx.mChromaOffsetU = -0.25f;
								if (!mpVideoManager->RunEffect(ctx, g_technique_ycbcr_601_to_rgb_2_0, rtsurface))
									success = false;
								break;

							case nsVDPixmap::kPixFormat_YUV420_Planar:
								ctx.mChromaScaleU = 0.5f;
								ctx.mChromaScaleV = 0.5f;
								ctx.mChromaOffsetU = -0.25f;
								if (!mpVideoManager->RunEffect(ctx, g_technique_ycbcr_601_to_rgb_2_0, rtsurface))
									success = false;
								break;

							case nsVDPixmap::kPixFormat_YUV410_Planar:
								ctx.mChromaScaleU = 0.25f;
								ctx.mChromaScaleV = 0.25f;
								if (!mpVideoManager->RunEffect(ctx, g_technique_ycbcr_601_to_rgb_2_0, rtsurface))
									success = false;
								break;
						}
						break;
				}
			}

			if (!mpManager->EndScene())
				success = false;
		}

		dev->SetRenderTarget(0, mpManager->GetRenderTarget());

		return success;
	}

	return true;
}

bool VDVideoUploadContextD3D9::Lock(IDirect3DTexture9 *tex, IDirect3DTexture9 *upload, D3DLOCKED_RECT *lr) {
	HRESULT hr = (upload ? upload : tex)->LockRect(0, lr, NULL, 0);

	return SUCCEEDED(hr);
}

bool VDVideoUploadContextD3D9::Unlock(IDirect3DTexture9 *tex, IDirect3DTexture9 *upload) {
	HRESULT hr;

	if (upload) {
		hr = upload->UnlockRect(0);
		if (FAILED(hr))
			return false;

		hr = mpManager->GetDevice()->UpdateTexture(upload, tex);
	} else {
		hr = tex->UnlockRect(0);
	}

	return SUCCEEDED(hr);
}

void VDVideoUploadContextD3D9::OnPreDeviceReset() {
	for(IDirect3DTexture9 *&tex : mpD3DConversionTextures)
		vdsaferelease <<= tex;
}

void VDVideoUploadContextD3D9::OnPostDeviceReset() {
	ReinitVRAMTextures();
}

bool VDVideoUploadContextD3D9::ReinitImageTextures() {
	for(IDirect3DTexture9 *& tex : mpD3DImageTextures) {
		if (!tex) {
			const bool useDefault = (mpManager->GetDeviceEx() != NULL);
			const D3DPOOL texPool = useDefault ? D3DPOOL_DEFAULT : D3DPOOL_MANAGED;

			HRESULT hr = mpManager->GetDevice()->CreateTexture(mTexFmt.w, mTexFmt.h, 1, 0, mTexD3DFmt, texPool, &tex, NULL);
			if (FAILED(hr))
				return false;
		}
	}

	return true;
}

bool VDVideoUploadContextD3D9::ReinitVRAMTextures() {
	if (mUploadMode != kUploadModeNormal) {
		IDirect3DDevice9 *dev = mpManager->GetDevice();

		for(IDirect3DTexture9 *&tex : mpD3DConversionTextures) {
			if (!tex) {
				HRESULT hr = dev->CreateTexture(mConversionTexW, mConversionTexH, 1, D3DUSAGE_RENDERTARGET, mbHighPrecision ? D3DFMT_A16B16G16R16F : D3DFMT_X8R8G8B8, D3DPOOL_DEFAULT, &tex, NULL);
				if (FAILED(hr))
					return false;

				mpManager->ClearRenderTarget(tex);
			}
		}
	}

	return true;
}

void VDVideoUploadContextD3D9::ClearImageTexture(uint32 i) {
	if (!mpD3DImageTextures[i])
		return;

	uint32 texw = mTexFmt.w;
	uint32 texh = mTexFmt.h;

	IDirect3DTexture9 *pDstTex = mpD3DImageTextures[i];
	IDirect3DTexture9 *pSrcTex = mpD3DImageTextureUpload;

	if (!pSrcTex)
		pSrcTex = pDstTex;
		
	D3DLOCKED_RECT lr;
	if (FAILED(pSrcTex->LockRect(0, &lr, NULL, 0)))
		return;

	switch(mSourceFmt) {
		case nsVDPixmap::kPixFormat_YUV444_Planar:
		case nsVDPixmap::kPixFormat_YUV444_Planar_709:
		case nsVDPixmap::kPixFormat_YUV422_Planar:
		case nsVDPixmap::kPixFormat_YUV422_Planar_709:
		case nsVDPixmap::kPixFormat_YUV420_Planar:
		case nsVDPixmap::kPixFormat_YUV420_Planar_709:
		case nsVDPixmap::kPixFormat_YUV411_Planar:
		case nsVDPixmap::kPixFormat_YUV411_Planar_709:
		case nsVDPixmap::kPixFormat_YUV410_Planar:
		case nsVDPixmap::kPixFormat_YUV410_Planar_709:
			VDMemset8Rect(lr.pBits, lr.Pitch, 0x10, texw, texh);
			break;
		case nsVDPixmap::kPixFormat_XRGB1555:
		case nsVDPixmap::kPixFormat_RGB565:
			VDMemset16Rect(lr.pBits, lr.Pitch, 0, texw, texh);
			break;
		case nsVDPixmap::kPixFormat_YUV444_Planar_FR:
		case nsVDPixmap::kPixFormat_YUV444_Planar_709_FR:
		case nsVDPixmap::kPixFormat_YUV422_Planar_FR:
		case nsVDPixmap::kPixFormat_YUV422_Planar_709_FR:
		case nsVDPixmap::kPixFormat_YUV420_Planar_FR:
		case nsVDPixmap::kPixFormat_YUV420_Planar_709_FR:
		case nsVDPixmap::kPixFormat_YUV411_Planar_FR:
		case nsVDPixmap::kPixFormat_YUV411_Planar_709_FR:
		case nsVDPixmap::kPixFormat_YUV410_Planar_FR:
		case nsVDPixmap::kPixFormat_YUV410_Planar_709_FR:
		case nsVDPixmap::kPixFormat_Pal8:
		case nsVDPixmap::kPixFormat_Y8:
		case nsVDPixmap::kPixFormat_Y8_FR:
			VDMemset8Rect(lr.pBits, lr.Pitch, 0, texw, texh);
			break;
		default:
			VDMemset32Rect(lr.pBits, lr.Pitch, 0, texw, texh);
			break;
	}

	pSrcTex->UnlockRect(0);

	if (pSrcTex != pDstTex)
		mpManager->GetDevice()->UpdateTexture(pSrcTex, pDstTex);
}

///////////////////////////////////////////////////////////////////////////

class VDVideoDisplayMinidriverDX9 final : public VDVideoDisplayMinidriver, public IVDDisplayCompositionEngine, protected VDD3D9Client {
public:
	VDVideoDisplayMinidriverDX9(bool clipToMonitor, bool use9ex);
	~VDVideoDisplayMinidriverDX9();

protected:
	bool PreInit(HWND hwnd, HMONITOR hmonitor) override;
	bool Init(HWND hwnd, HMONITOR hmonitor, const VDVideoDisplaySourceInfo& info) override;
	void Shutdown() override;

	bool ModifySource(const VDVideoDisplaySourceInfo& info) override;

	bool IsValid() override;
	bool IsScreenFXSupported() const override { return mbScreenFXSupported; }
	bool IsFramePending() override { return mbSwapChainPresentPending; }
	void SetFilterMode(FilterMode mode) override;
	void SetFullScreen(bool fs, uint32 w, uint32 h, uint32 refresh, bool use16bit) override;
	bool SetScreenFX(const VDVideoDisplayScreenFXInfo *screenFX) override;

	bool Tick(int id) override;
	void Poll() override;
	bool Resize(int w, int h) override;
	bool Invalidate() override;
	void PresentQueued() { Poll(); }
	bool Update(UpdateMode) override;
	void Refresh(UpdateMode) override;
	bool Paint(HDC hdc, const RECT& rClient, UpdateMode mode) override;

	void SetLogicalPalette(const uint8 *pLogicalPalette) override;
	bool AreVSyncTicksNeeded() const override { return (mbSwapChainVsync && !mbSwapChainVsyncEvent) || (mbFullScreen && mbSwapChainPresentPolling); }
	float GetSyncDelta() const override { return mSyncDelta; }

	IVDDisplayCompositionEngine *GetDisplayCompositionEngine() override { return this; }

public:
	void LoadCustomEffect(const wchar_t *path) override;

protected:
	void OnPreDeviceReset() override;
	void OnPostDeviceReset() override {}

	void OnVsyncEvent(IVDVideoDisplayMinidriverCallback *cb);

	void InitBicubic();
	void ShutdownBicubic();
	bool InitBicubicPS2Filters(int w, int h);
	void ShutdownBicubicPS2Filters();
	bool InitBoxlinearPS11Filters(int w, int h, float facx, float facy);
	void ShutdownBoxlinearPS11Filters();

	bool UpdateBackbuffer(const RECT& rClient, UpdateMode updateMode);
	bool UpdateScreen(const RECT& rClient, UpdateMode updateMode, bool polling);

	void DrawDebugInfo(FilterMode mode, const RECT& rClient);

	HWND				mhwnd = nullptr;
	VDD3D9Manager		*mpManager = nullptr;
	vdrefptr<VDVideoDisplayDX9Manager>	mpVideoManager;
	IDirect3DDevice9	*mpD3DDevice = nullptr;			// weak ref
	vdrefptr<IDirect3DTexture9>	mpD3DInterpFilterTextureH;
	vdrefptr<IDirect3DTexture9>	mpD3DInterpFilterTextureV;
	vdrefptr<IDirect3DTexture9>	mpD3DInterpFilterTexture;
	int					mInterpFilterHSizeDst = 0;
	int					mInterpFilterHSizeSrc = 0;
	int					mInterpFilterHTexSize = 0;
	int					mInterpFilterVSizeDst = 0;
	int					mInterpFilterVSizeSrc = 0;
	int					mInterpFilterVTexSize = 0;
	int					mInterpFilterTexSizeW = 0;
	int					mInterpFilterTexSizeH = 0;
	float				mInterpFilterFactorX = 0;
	float				mInterpFilterFactorY = 0;

	vdrefptr<IDirect3DTexture9> mpD3DScanlineMaskTexture;
	uint32 mCachedScanlineSrcH = 0;
	uint32 mCachedScanlineDstH = 0;
	float mCachedScanlineNormH = 0;
	float mCachedScanlineIntensity = 0;

	vdrefptr<IDirect3DTexture9> mpD3DGammaRampTexture;
	float mCachedGamma = 0;
	bool mbCachedGammaAdobeRgb = false;
	bool mbCachedGammaHasInputConversion = false;

	vdrefptr<IDirect3DTexture9> mpD3DBloomPrescale1RTT;
	vdrefptr<IDirect3DTexture9> mpD3DBloomPrescale2RTT;
	vdrefptr<IDirect3DTexture9> mpD3DBloomBlur1RTT;
	vdrefptr<IDirect3DTexture9> mpD3DBloomBlur2RTT;
	uint32 mCachedBloomInputTexW = 0;
	uint32 mCachedBloomInputTexH = 0;
	uint32 mCachedBloomPrescaleW = 0;
	uint32 mCachedBloomPrescaleH = 0;
	uint32 mCachedBloomPrescaleTexW = 0;
	uint32 mCachedBloomPrescaleTexH = 0;
	uint32 mCachedBloomBlurW = 0;
	uint32 mCachedBloomBlurH = 0;
	uint32 mCachedBloomBlurTexW = 0;
	uint32 mCachedBloomBlurTexH = 0;
	uint32 mCachedBloomOutputW = 0;
	uint32 mCachedBloomOutputH = 0;

	vdrefptr<IDirect3DTexture9> mpD3DArtifactingRTT;
	uint32 mCachedArtifactingW = 0;
	uint32 mCachedArtifactingH = 0;
	uint32 mCachedArtifactingTexW = 0;
	uint32 mCachedArtifactingTexH = 0;

	vdrefptr<VDVideoUploadContextD3D9>	mpUploadContext;
	vdrefptr<IVDFontRendererD3D9>	mpFontRenderer;
	vdrefptr<IVDDisplayRendererD3D9>	mpRenderer;

	vdrefptr<IVDD3D9SwapChain>	mpSwapChain;
	int					mSwapChainW = 0;
	int					mSwapChainH = 0;
	bool				mbSwapChainImageValid = false;
	bool				mbSwapChainPresentPending = false;
	bool				mbSwapChainPresentPolling = false;
	bool				mbSwapChainVsync = false;
	bool				mbSwapChainVsyncEvent = false;
	VDAtomicBool		mbSwapChainVsyncEventPending = false;
	bool				mbFirstPresent = true;
	bool				mbFullScreen = false;
	bool				mbFullScreenSet = false;
	uint32				mFullScreenWidth = 0;
	uint32				mFullScreenHeight = 0;
	uint32				mFullScreenRefreshRate = 0;
	bool				mbFullScreen16Bit = false;
	const bool			mbClipToMonitor;

	VDVideoDisplayDX9Manager::CubicMode	mCubicMode;
	bool				mbCubicInitialized = false;
	bool				mbCubicAttempted = false;
	bool				mbCubicUsingHighPrecision = false;
	bool				mbCubicTempSurfacesInitialized = false;
	bool				mbBoxlinearCapable11 = false;
	bool				mbScreenFXSupported = false;
	const bool			mbUseD3D9Ex;

	bool mbUseScreenFX = false;
	VDVideoDisplayScreenFXInfo mScreenFXInfo {};

	FilterMode			mPreferredFilter = kFilterAnySuitable;
	float				mSyncDelta = 0;
	VDD3DPresentHistory	mPresentHistory;

	VDPixmap					mTexFmt;

	VDVideoDisplaySourceInfo	mSource;

	VDStringA		mFormatString;
	VDStringA		mDebugString;
	VDStringA		mErrorString;

	vdautoptr<IVDDisplayCustomShaderPipelineD3D9> mpCustomPipeline;
};

IVDVideoDisplayMinidriver *VDCreateVideoDisplayMinidriverDX9(bool clipToMonitor, bool use9ex) {
	return new VDVideoDisplayMinidriverDX9(clipToMonitor, use9ex);
}

VDVideoDisplayMinidriverDX9::VDVideoDisplayMinidriverDX9(bool clipToMonitor, bool use9ex)
	: mbClipToMonitor(clipToMonitor)
	, mbUseD3D9Ex(use9ex)
{
}

VDVideoDisplayMinidriverDX9::~VDVideoDisplayMinidriverDX9() {
	Shutdown();
}

bool VDVideoDisplayMinidriverDX9::PreInit(HWND hwnd, HMONITOR hmonitor) {
	VDASSERT(!mpManager);

	mhwnd = hwnd;
	mbFullScreenSet = false;

	mpManager = VDInitDirect3D9(this, hmonitor, mbUseD3D9Ex);
	if (!mpManager) {
		Shutdown();
		return false;
	}

	if (!VDInitDisplayDX9(hmonitor, mbUseD3D9Ex, ~mpVideoManager)) {
		Shutdown();
		return false;
	}

	mbScreenFXSupported = false;

	if (mpVideoManager->IsPS20Enabled()) {
		mbScreenFXSupported = true;
	}

	return true;
}

bool VDVideoDisplayMinidriverDX9::Init(HWND hwnd, HMONITOR hmonitor, const VDVideoDisplaySourceInfo& info) {
	VDASSERT(mpVideoManager);

	mSource = info;

	// attempt to initialize D3D9
	if (mbFullScreen && !mbFullScreenSet) {
		mbFullScreenSet = true;
		mpManager->AdjustFullScreen(true, mFullScreenWidth, mFullScreenHeight, mFullScreenRefreshRate, mbFullScreen16Bit, mhwnd);
	}

	mpD3DDevice = mpManager->GetDevice();

	if (!VDCreateDisplayRendererD3D9(~mpRenderer)) {
		Shutdown();
		return false;
	}

	mpRenderer->Init(mpManager, mpVideoManager);

	mpUploadContext = new_nothrow VDVideoUploadContextD3D9;
	const uint32 bufferCount = mpCustomPipeline ? mpCustomPipeline->GetMaxPrevFrames() + 1 : 1;
	if (!mpUploadContext || !mpUploadContext->Init(hmonitor, mbUseD3D9Ex, info.pixmap, info.bAllowConversion, mbHighPrecision && mpVideoManager->Is16FEnabled(), bufferCount, info.use16bit)) {
		Shutdown();
		return false;
	}

	mSyncDelta = 0.0f;
	mbFirstPresent = true;

	mbBoxlinearCapable11 = mpVideoManager->IsPS11Enabled();

	mErrorString.clear();

	return true;
}

void VDVideoDisplayMinidriverDX9::LoadCustomEffect(const wchar_t *path) {
	mErrorString.clear();

	if (path && *path) {
		try {
			mpCustomPipeline = VDDisplayParseCustomShaderPipeline(mpManager, path);

			if (mpUploadContext)
				mpUploadContext->SetBufferCount(mpCustomPipeline->GetMaxPrevFrames() + 1);
		} catch(const MyError& e) {
			mErrorString = e.c_str();

			if (mpUploadContext)
				mpUploadContext->SetBufferCount(1);
		}
	} else {
		mpCustomPipeline.reset();

		if (mpUploadContext)
			mpUploadContext->SetBufferCount(1);
	}
}

void VDVideoDisplayMinidriverDX9::OnPreDeviceReset() {
	vdsaferelease <<= mpD3DBloomPrescale2RTT, mpD3DBloomBlur1RTT, mpD3DBloomBlur2RTT;

	ShutdownBicubic();
	ShutdownBicubicPS2Filters();
	ShutdownBoxlinearPS11Filters();
	mpSwapChain = NULL;
	mSwapChainW = 0;
	mSwapChainH = 0;
	mbSwapChainImageValid = false;
	mbSwapChainVsync = false;
	mbSwapChainVsyncEvent = false;
}

void VDVideoDisplayMinidriverDX9::OnVsyncEvent(IVDVideoDisplayMinidriverCallback *cb) {
	if (mbSwapChainVsyncEventPending.xchg(false))
		mSource.mpCB->QueuePresent();
}

void VDVideoDisplayMinidriverDX9::InitBicubic() {
	if (mbCubicInitialized || mbCubicAttempted)
		return;

	mbCubicAttempted = true;

	mCubicMode = mpVideoManager->InitBicubic();

	if (mCubicMode == VDVideoDisplayDX9Manager::kCubicNotPossible)
		return;

	VDASSERT(!mbCubicTempSurfacesInitialized);
	mbCubicUsingHighPrecision = mbHighPrecision;
	mbCubicTempSurfacesInitialized = mpVideoManager->InitBicubicTempSurfaces(mbCubicUsingHighPrecision);
	if (!mbCubicTempSurfacesInitialized) {
		mpVideoManager->ShutdownBicubic();
		mCubicMode = VDVideoDisplayDX9Manager::kCubicNotPossible;
		return;
	}

	VDDEBUG_DX9DISP("VideoDisplay/DX9: Bicubic initialization complete.");
	mbCubicInitialized = true;
}

void VDVideoDisplayMinidriverDX9::ShutdownBicubic() {
	if (mbCubicInitialized) {
		mbCubicInitialized = mbCubicAttempted = false;

		if (mbCubicTempSurfacesInitialized) {
			mbCubicTempSurfacesInitialized = false;

			mpVideoManager->ShutdownBicubicTempSurfaces(mbCubicUsingHighPrecision);
		}

		mpVideoManager->ShutdownBicubic();
	}
}

///////////////////////////////////////////////////////////////////////////

namespace {
	int GeneratePS2CubicTexture(VDD3D9Manager *pManager, int w, int srcw, vdrefptr<IDirect3DTexture9>& pTexture, int existingTexW) {
		IDirect3DDevice9 *dev = pManager->GetDevice();

		// Round up to next multiple of 128 pixels to reduce reallocation.
		int texw = (w + 127) & ~127;
		int texh = 1;
		pManager->AdjustTextureSize(texw, texh);

		// If we can't fit the texture, bail.
		if (texw < w)
			return -1;

		// Check if we need to reallocate the texture.
		HRESULT hr;
		D3DFORMAT format = D3DFMT_A8R8G8B8;
		const bool useDefault = (pManager->GetDeviceEx() != NULL);

		if (!pTexture || existingTexW != texw) {
			hr = dev->CreateTexture(texw, texh, 1, 0, format, useDefault ? D3DPOOL_DEFAULT : D3DPOOL_MANAGED, ~pTexture, NULL);
			if (FAILED(hr))
				return -1;
		}

		vdrefptr<IDirect3DTexture9> uploadtex;
		if (useDefault) {
			hr = dev->CreateTexture(texw, texh, 1, 0, format, D3DPOOL_SYSTEMMEM, ~uploadtex, NULL);
			if (FAILED(hr))
				return -1;
		} else {
			uploadtex = pTexture;
		}

		// Fill the texture.
		D3DLOCKED_RECT lr;
		hr = uploadtex->LockRect(0, &lr, NULL, 0);
		VDASSERT(SUCCEEDED(hr));
		if (FAILED(hr)) {
			VDDEBUG_DX9DISP("VideoDisplay/DX9: Failed to load bicubic texture.");
			return -1;
		}

		double dudx = (double)srcw / (double)w;
		double u = dudx * 0.5;
		double u0 = 0.5;
		double ud0 = 1.5;
		double ud1 = (double)srcw - 1.5;
		double u1 = (double)srcw - 0.5;
		uint32 *p0 = (uint32 *)lr.pBits;

		for(int x = 0; x < texw; ++x) {
			double ut = u;
			if (ut < u0)
				ut = u0;
			else if (ut > u1)
				ut = u1;
			int ix = VDFloorToInt(ut - 0.5);
			double d = ut - ((double)ix + 0.5);

			static const double m = -0.75;
			double c0 = (( (m    )*d - 2.0*m    )*d +   m)*d;
			double c1 = (( (m+2.0)*d -     m-3.0)*d      )*d + 1.0;
			double c2 = ((-(m+2.0)*d + 2.0*m+3.0)*d -   m)*d;
			double c3 = ((-(m    )*d +     m    )*d      )*d;

			double c03		= c0+c3;
			double k1 = d < 0.5 ? d < 1e-5 ? -m : c2 / d : d > 1-1e-5 ? -m : c1 / (1-d);
			double kx = d < 0.5 ? c1 - k1*(1-d) : c2 - k1*d;

			if (ut < ud0 || ut > ud1) {
				c0 = 0;
				k1 = 1.0;
				kx = 0.0;
				c3 = 0;
			}

			double blue		= -c0*4;
			double green	= k1 - 1.0 + 128.0f/255.0f;
			double red		= kx * 2;
			double alpha	= -c3*4;

			blue = fabs(c0 + c3) > 1e-9 ? c0 / (c0 + c3) : 0;
			green = fabs(green + red) > 1e-9 ? green / (green + red) : 0;
			red = fabs(c0 + c3);

			// The rounding here is a bit tricky. Here's how we use the values:
			//	r = p2 * (g + 0.5) + p3 * (r / 2) - p1 * (b / 4) - p4 * (a / 4)
			//
			// Which means we need:
			//	g + 0.5 + r/2 - b/4 - a/4 = 1
			//	g + r/2 - b/4 - a/4 = 0.5
			//	g*4 + r*2 - (b + a) = 2 (510 / 1020)

			uint8 ib = VDClampedRoundFixedToUint8Fast((float)blue);
			uint8 ig = VDClampedRoundFixedToUint8Fast((float)green);
			uint8 ir = VDClampedRoundFixedToUint8Fast((float)red);
			uint8 ia = VDClampedRoundFixedToUint8Fast((float)alpha);

			p0[x] = (uint32)ib + ((uint32)ig << 8) + ((uint32)ir << 16) + ((uint32)ia << 24);

			u += dudx;
		}

		VDVERIFY(SUCCEEDED(uploadtex->UnlockRect(0)));

		if (useDefault) {
			hr = pManager->GetDevice()->UpdateTexture(uploadtex, pTexture);
			if (FAILED(hr)) {
				pTexture.clear();
				return -1;
			}
		}

		return texw;
	}

	std::pair<int, int> GeneratePS11BoxlinearTexture(VDD3D9Manager *pManager, int w, int h, int srcw, int srch, float facx, float facy, vdrefptr<IDirect3DTexture9>& pTexture, int existingTexW, int existingTexH) {
		IDirect3DDevice9 *dev = pManager->GetDevice();

		// Round up to next multiple of 128 pixels to reduce reallocation.
		int texw = (w + 127) & ~127;
		int texh = (h + 127) & ~127;
		pManager->AdjustTextureSize(texw, texh);

		// If we can't fit the texture, bail.
		if (texw < w || texh < h)
			return std::pair<int, int>(-1, -1);

		// Check if we need to reallocate the texture.
		HRESULT hr;
		D3DFORMAT format = D3DFMT_V8U8;
		const bool useDefault = (pManager->GetDeviceEx() != NULL);

		if (!pTexture || existingTexW != texw || existingTexH != texh) {
			hr = dev->CreateTexture(texw, texh, 1, 0, D3DFMT_V8U8, useDefault ? D3DPOOL_DEFAULT : D3DPOOL_MANAGED, ~pTexture, NULL);
			if (FAILED(hr))
				return std::pair<int, int>(-1, -1);
		}

		vdrefptr<IDirect3DTexture9> uploadtex;
		if (useDefault) {
			hr = dev->CreateTexture(texw, texh, 1, 0, D3DFMT_V8U8, D3DPOOL_SYSTEMMEM, ~uploadtex, NULL);
			if (FAILED(hr))
				return std::pair<int, int>(-1, -1);
		} else {
			uploadtex = pTexture;
		}

		// Fill the texture.
		D3DLOCKED_RECT lr;
		hr = uploadtex->LockRect(0, &lr, NULL, 0);
		VDASSERT(SUCCEEDED(hr));
		if (FAILED(hr)) {
			VDDEBUG_DX9DISP("VideoDisplay/DX9: Failed to load boxlinear texture.");
			return std::pair<int, int>(-1, -1);
		}

		double dudx = (double)srcw / (double)w;
		double dvdy = (double)srch / (double)h;
		double u = dudx * 0.5;
		double v = dvdy * 0.5;

		vdfastvector<sint8> hfilter(texw);
		for(int x = 0; x < texw; ++x) {
			double edgePos = floor(u + 0.5);
			double snappedPos = edgePos - fmax(-0.5f, fmin((edgePos - u) * facx, 0.5));
			hfilter[x] = (sint8)floor(0.5 + 127.0 * (snappedPos - u));
			u += dudx;
		}

		vdfastvector<sint8> vfilter(texh);
		for(int y = 0; y < texh; ++y) {
			double edgePos = floor(v + 0.5);
			double snappedPos = edgePos - fmax(-0.5f, fmin((edgePos - v) * facy, 0.5));
			vfilter[y] = (sint8)floor(0.5 + 127.0 * (snappedPos - v));
			v += dvdy;
		}

		sint8 *p0 = (sint8 *)lr.pBits;

		for(int y = 0; y < texh; ++y) {
			sint8 *p = p0;
			sint8 vc = vfilter[y];

			for(int x = 0; x < texw; ++x) {
				p[0] = hfilter[x];
				p[1] = vc;
				p += 2;
			}

			p0 += lr.Pitch;
		}

		VDVERIFY(SUCCEEDED(uploadtex->UnlockRect(0)));

		if (useDefault) {
			hr = pManager->GetDevice()->UpdateTexture(uploadtex, pTexture);
			if (FAILED(hr)) {
				pTexture.clear();
				return std::pair<int,int>(texw, texh);
			}
		}

		return std::pair<int,int>(texw, texh);
	}
}

bool VDVideoDisplayMinidriverDX9::InitBicubicPS2Filters(int w, int h) {
	// requires PS2.0 path
	if (mCubicMode != VDVideoDisplayDX9Manager::kCubicUsePS2_0Path)
		return false;

	// update horiz filter
	if (!mpD3DInterpFilterTextureH || mInterpFilterHSizeDst != w || mInterpFilterHSizeSrc != mSource.pixmap.w) {
		int newtexw = GeneratePS2CubicTexture(mpManager, w, mSource.pixmap.w, mpD3DInterpFilterTextureH, mInterpFilterHSizeDst);
		if (newtexw < 0)
			return false;

		mInterpFilterHSizeDst = w;
		mInterpFilterHSizeSrc = mSource.pixmap.w;
		mInterpFilterHTexSize = newtexw;
	}

	// update vert filter
	if (!mpD3DInterpFilterTextureV || mInterpFilterVSizeDst != h || mInterpFilterVSizeSrc != mSource.pixmap.h) {
		int newtexw = GeneratePS2CubicTexture(mpManager, h, mSource.pixmap.h, mpD3DInterpFilterTextureV, mInterpFilterVSizeDst);
		if (newtexw < 0)
			return false;

		mInterpFilterVSizeDst = h;
		mInterpFilterVSizeSrc = mSource.pixmap.h;
		mInterpFilterVTexSize = newtexw;
	}
	return true;
}

void VDVideoDisplayMinidriverDX9::ShutdownBicubicPS2Filters() {
	mpD3DInterpFilterTextureH = NULL;
	mpD3DInterpFilterTextureV = NULL;
	mInterpFilterHSizeDst = 0;
	mInterpFilterHSizeSrc = 0;
	mInterpFilterHTexSize = 0;
	mInterpFilterVSizeDst = 0;
	mInterpFilterVSizeSrc = 0;
	mInterpFilterVTexSize = 0;
}

bool VDVideoDisplayMinidriverDX9::InitBoxlinearPS11Filters(int w, int h, float facx, float facy) {
	// update horiz filter
	if (!mpD3DInterpFilterTexture
		|| mInterpFilterHSizeDst != w
		|| mInterpFilterHSizeSrc != mSource.pixmap.w
		|| mInterpFilterVSizeDst != h
		|| mInterpFilterVSizeSrc != mSource.pixmap.h
		|| mInterpFilterFactorX != facx
		|| mInterpFilterFactorY != facy)
	{
		auto newtexsize = GeneratePS11BoxlinearTexture(mpManager, w, h, mSource.pixmap.w, mSource.pixmap.h, facx, facy, mpD3DInterpFilterTexture, mInterpFilterHSizeDst, mInterpFilterVSizeDst);
		if (newtexsize.first < 0)
			return false;

		mInterpFilterHSizeDst = w;
		mInterpFilterHSizeSrc = mSource.pixmap.w;
		mInterpFilterVSizeDst = h;
		mInterpFilterVSizeSrc = mSource.pixmap.h;
		mInterpFilterTexSizeW = newtexsize.first;
		mInterpFilterTexSizeH = newtexsize.second;
		mInterpFilterFactorX = facx;
		mInterpFilterFactorY = facy;
	}

	return true;
}

void VDVideoDisplayMinidriverDX9::ShutdownBoxlinearPS11Filters() {
	mpD3DInterpFilterTexture.clear();
	mInterpFilterTexSizeW = 0;
	mInterpFilterTexSizeH = 0;
}

void VDVideoDisplayMinidriverDX9::Shutdown() {
	vdsaferelease <<= mpD3DGammaRampTexture;
	vdsaferelease <<= mpD3DScanlineMaskTexture;
	vdsaferelease <<= mpD3DBloomPrescale1RTT, mpD3DBloomPrescale2RTT, mpD3DBloomBlur1RTT, mpD3DBloomBlur2RTT;
	vdsaferelease <<= mpD3DArtifactingRTT;

	mpCustomPipeline.reset();

	mpUploadContext = NULL;

	if (mpFontRenderer) {
		mpFontRenderer->Shutdown();
		mpFontRenderer.clear();
	}

	if (mpRenderer) {
		mpRenderer->Shutdown();
		mpRenderer.clear();
	}

	ShutdownBicubic();
	ShutdownBicubicPS2Filters();
	ShutdownBoxlinearPS11Filters();

	mpSwapChain = NULL;
	mbSwapChainVsync = false;
	mbSwapChainVsyncEvent = false;
	mSwapChainW = 0;
	mSwapChainH = 0;

	mpVideoManager = NULL;

	if (mpManager) {
		if (mbFullScreenSet) {
			mbFullScreenSet = false;
			mpManager->AdjustFullScreen(false, 0, 0, 0, false, nullptr);
		}

		VDDeinitDirect3D9(mpManager, this);
		mpManager = NULL;
	}

	mbCubicAttempted = false;
}

bool VDVideoDisplayMinidriverDX9::ModifySource(const VDVideoDisplaySourceInfo& info) {
	bool fastPath = false;

	if (mSource.pixmap.w == info.pixmap.w && mSource.pixmap.h == info.pixmap.h) {
		const int prevFormat = mSource.pixmap.format;
		const int nextFormat = info.pixmap.format;

		if (prevFormat == nextFormat)
			fastPath = true;
	}

	if (!fastPath) {
		ShutdownBoxlinearPS11Filters();

		mpUploadContext.clear();

		mpUploadContext = new_nothrow VDVideoUploadContextD3D9;
		const uint32 bufferCount = mpCustomPipeline ? mpCustomPipeline->GetMaxPrevFrames() + 1 : 1;
		if (!mpUploadContext || !mpUploadContext->Init(mpManager->GetMonitor(), mbUseD3D9Ex, info.pixmap, info.bAllowConversion, mbHighPrecision && mpVideoManager->Is16FEnabled(), bufferCount, mSource.use16bit)) {
			mpUploadContext.clear();
			return false;
		}
	}

	mSource = info;
	return true;
}

bool VDVideoDisplayMinidriverDX9::IsValid() {
	return mpD3DDevice != 0;
}

void VDVideoDisplayMinidriverDX9::SetFilterMode(FilterMode mode) {
	mPreferredFilter = mode;

	if (mode != kFilterBicubic && mode != kFilterAnySuitable) {
		ShutdownBicubicPS2Filters();

		if (mbCubicInitialized)
			ShutdownBicubic();
	}
}

void VDVideoDisplayMinidriverDX9::SetFullScreen(bool fs, uint32 w, uint32 h, uint32 refresh, bool use16bit) {
	if (mbFullScreen != fs) {
		mbFullScreen = fs;
		mFullScreenWidth = w;
		mFullScreenHeight = h;
		mFullScreenRefreshRate = refresh;
		mbFullScreen16Bit = use16bit;

		if (mpManager) {
			if (mbFullScreenSet != fs) {
				mbFullScreenSet = fs;
				mpManager->AdjustFullScreen(fs, w, h, refresh, use16bit, mhwnd);

				mbSwapChainPresentPending = false;
			}
		}
	}
}

bool VDVideoDisplayMinidriverDX9::SetScreenFX(const VDVideoDisplayScreenFXInfo *screenFX) {
	if (screenFX) {
		if (!mbScreenFXSupported)
			return false;

		mbUseScreenFX = true;
		mScreenFXInfo = *screenFX;

		const bool useInputConversion = (mScreenFXInfo.mColorCorrectionMatrix[0][0] != 0.0f);
		if (!mpD3DGammaRampTexture
			|| mCachedGamma != mScreenFXInfo.mGamma
			|| mbCachedGammaAdobeRgb != mScreenFXInfo.mbColorCorrectAdobeRGB
			|| mbCachedGammaHasInputConversion != useInputConversion)
		{
			mCachedGamma = mScreenFXInfo.mGamma;
			mbCachedGammaAdobeRgb = mScreenFXInfo.mbColorCorrectAdobeRGB;
			mbCachedGammaHasInputConversion = useInputConversion;

			vdrefptr<IVDD3D9InitTexture> initTex;
			if (!mpManager->CreateInitTexture(256, 1, 1, D3DFMT_A8R8G8B8, ~initTex))
				return false;

			VDD3D9LockInfo lockInfo;
			if (!initTex->Lock(0, lockInfo))
				return false;

			VDDisplayCreateGammaRamp((uint32 *)lockInfo.mpData, 256, useInputConversion, mScreenFXInfo.mbColorCorrectAdobeRGB, mScreenFXInfo.mGamma);

			initTex->Unlock(0);

			vdrefptr<IVDD3D9Texture> tex;
			if (!mpManager->CreateTexture(initTex, ~tex))
				return false;

			mpD3DGammaRampTexture = tex->GetD3DTexture();
		}
	} else {
		mbUseScreenFX = false;
		vdsaferelease <<= mpD3DGammaRampTexture, mpD3DScanlineMaskTexture;
	}

	if (!mbUseScreenFX || mScreenFXInfo.mBloomRadius == 0.0f) {
		vdsaferelease <<= mpD3DBloomPrescale2RTT, mpD3DBloomBlur1RTT, mpD3DBloomBlur2RTT;
	}

	if (!mbUseScreenFX || mScreenFXInfo.mPALBlendingOffset == 0.0f) {
		vdsaferelease <<= mpD3DArtifactingRTT;
	}

	return true;
}

bool VDVideoDisplayMinidriverDX9::Tick(int id) {
	return true;
}

void VDVideoDisplayMinidriverDX9::Poll() {
	if (mbSwapChainPresentPending) {
		RECT rClient = { mClientRect.left, mClientRect.top, mClientRect.right, mClientRect.bottom };
		if (!UpdateScreen(rClient, kModeVSync, true))
			mSource.mpCB->RequestNextFrame();
	}
}

bool VDVideoDisplayMinidriverDX9::Resize(int w, int h) {
	mbSwapChainImageValid = false;
	return VDVideoDisplayMinidriver::Resize(w, h);
}

bool VDVideoDisplayMinidriverDX9::Invalidate() {
	mbSwapChainImageValid = false;
	return false;
}

bool VDVideoDisplayMinidriverDX9::Update(UpdateMode mode) {
	if (!mpManager->CheckDevice())
		return false;

	if (!mpUploadContext->Update(mSource.pixmap))
		return false;

	mbSwapChainImageValid = false;

	return true;
}

void VDVideoDisplayMinidriverDX9::Refresh(UpdateMode mode) {
	if (mClientRect.right > 0 && mClientRect.bottom > 0) {
		RECT rClient = { mClientRect.left, mClientRect.top, mClientRect.right, mClientRect.bottom };

		if (!Paint(NULL, rClient, mode)) {
			VDDEBUG_DX9DISP("Refresh() failed in Paint()");
		}
	}
}

bool VDVideoDisplayMinidriverDX9::Paint(HDC, const RECT& rClient, UpdateMode updateMode) {
	return (mbSwapChainImageValid || UpdateBackbuffer(rClient, updateMode)) && UpdateScreen(rClient, updateMode, 0 != (updateMode & kModeVSync));
}

void VDVideoDisplayMinidriverDX9::SetLogicalPalette(const uint8 *pLogicalPalette) {
}

bool VDVideoDisplayMinidriverDX9::UpdateBackbuffer(const RECT& rClient0, UpdateMode updateMode) {
	using namespace nsVDDisplay;

	const D3DDISPLAYMODE& displayMode = mpManager->GetDisplayMode();
	int rtw = displayMode.Width;
	int rth = displayMode.Height;
	RECT rClient = rClient0;
	if (mbFullScreen) {
		rClient.right = rtw;
		rClient.bottom = rth;
	}

	RECT rClippedClient={0,0,std::min<int>(rClient.right, rtw), std::min<int>(rClient.bottom, rth)};

	// Make sure the device is sane.
	if (!mpManager->CheckDevice())
		return false;

	// Check if we need to create or resize the swap chain.
	if (!mbFullScreen) {
		if (mpManager->GetDeviceEx()) {
			if (mSwapChainW != rClippedClient.right || mSwapChainH != rClippedClient.bottom) {
				mpSwapChain = NULL;
				mbSwapChainVsync = false;
				mbSwapChainVsyncEvent = false;
				mSwapChainW = 0;
				mSwapChainH = 0;
				mbSwapChainImageValid = false;
			}

			if (!mpSwapChain || mSwapChainW != rClippedClient.right || mSwapChainH != rClippedClient.bottom) {
				int scw = std::min<int>(rClippedClient.right, rtw);
				int sch = std::min<int>(rClippedClient.bottom, rth);

				if (!mpManager->CreateSwapChain(mhwnd, scw, sch, mbClipToMonitor, mSource.use16bit, ~mpSwapChain))
					return false;

				mSwapChainW = scw;
				mSwapChainH = sch;
			}
		} else {
			if (mSwapChainW >= rClippedClient.right + 128 || mSwapChainH >= rClippedClient.bottom + 128) {
				mpSwapChain = NULL;
				mbSwapChainVsync = false;
				mbSwapChainVsyncEvent = false;
				mSwapChainW = 0;
				mSwapChainH = 0;
				mbSwapChainImageValid = false;
			}

			if (!mpSwapChain || mSwapChainW < rClippedClient.right || mSwapChainH < rClippedClient.bottom) {
				int scw = std::min<int>((rClippedClient.right + 127) & ~127, rtw);
				int sch = std::min<int>((rClippedClient.bottom + 127) & ~127, rth);

				if (!mpManager->CreateSwapChain(mhwnd, scw, sch, mbClipToMonitor, mSource.use16bit, ~mpSwapChain))
					return false;

				mSwapChainW = scw;
				mSwapChainH = sch;
			}
		}
	}

	VDDisplayCompositeInfo compInfo = {};

	if (mpCompositor) {
		compInfo.mWidth = rClient.right;
		compInfo.mHeight = rClient.bottom;

		mpCompositor->PreComposite(compInfo);
	}

	// Do we need to switch bicubic modes?
	FilterMode mode = mPreferredFilter;

	if (mode == kFilterAnySuitable)
		mode = kFilterBicubic;

	// bicubic modes cannot clip
	if (rClient.right != rClippedClient.right || rClient.bottom != rClippedClient.bottom)
		mode = kFilterBilinear;

	// must force bilinear if screen effects are enabled
	if (mode == kFilterBicubic && mbUseScreenFX)
		mode = kFilterBilinear;

	if (mode != kFilterBicubic && mbCubicInitialized)
		ShutdownBicubic();
	else if (mode == kFilterBicubic && !mbCubicInitialized && !mbCubicAttempted)
		InitBicubic();

	if (mpD3DInterpFilterTexture && mPixelSharpnessX <= 1 && mPixelSharpnessY <= 1)
		ShutdownBoxlinearPS11Filters();

	static const D3DMATRIX ident={
		{1,0,0,0,0,1,0,0,0,0,1,0,0,0,0,1}
	};

	D3D_DO(SetTransform(D3DTS_WORLD, &ident));
	D3D_DO(SetTransform(D3DTS_VIEW, &ident));
	D3D_DO(SetTransform(D3DTS_PROJECTION, &ident));

	D3D_DO(SetStreamSource(0, mpManager->GetVertexBuffer(), 0, sizeof(Vertex)));
	D3D_DO(SetIndices(mpManager->GetIndexBuffer()));
	D3D_DO(SetFVF(D3DFVF_XYZ | D3DFVF_DIFFUSE | D3DFVF_TEX2));
	D3D_DO(SetRenderState(D3DRS_LIGHTING, FALSE));
	D3D_DO(SetRenderState(D3DRS_CULLMODE, D3DCULL_NONE));
	D3D_DO(SetRenderState(D3DRS_ZENABLE, FALSE));
	D3D_DO(SetRenderState(D3DRS_ALPHATESTENABLE, FALSE));
	D3D_DO(SetRenderState(D3DRS_ALPHABLENDENABLE, FALSE));
	D3D_DO(SetRenderState(D3DRS_STENCILENABLE, FALSE));
	D3D_DO(SetTextureStageState(0, D3DTSS_TEXCOORDINDEX, 0));
	D3D_DO(SetTextureStageState(1, D3DTSS_TEXCOORDINDEX, 1));
	D3D_DO(SetTextureStageState(2, D3DTSS_TEXCOORDINDEX, 2));

	vdrefptr<IDirect3DSurface9> pRTMain;

	mpManager->SetSwapChainActive(NULL);

	if (mpSwapChain) {
		IDirect3DSwapChain9 *sc = mpSwapChain->GetD3DSwapChain();
		HRESULT hr = sc->GetBackBuffer(0, D3DBACKBUFFER_TYPE_MONO, ~pRTMain);
		if (FAILED(hr))
			return false;
	} else {
		mpManager->SetSwapChainActive(NULL);
		mpD3DDevice->GetRenderTarget(0, ~pRTMain);
	}

	mbSwapChainImageValid = false;

	bool bSuccess = false;

	const D3DDISPLAYMODE& dispMode = mpManager->GetDisplayMode();
	bool needBlit = true;
	if (mColorOverride) {
		mpManager->SetSwapChainActive(mpSwapChain);

		D3DRECT rClear;
		rClear.x1 = rClient.left;
		rClear.y1 = rClient.top;
		rClear.x2 = rClient.right;
		rClear.y2 = rClient.bottom;
		HRESULT hr = mpD3DDevice->Clear(1, &rClear, D3DCLEAR_TARGET, mColorOverride, 0.0f, 0);

		bSuccess = SUCCEEDED(hr);
		needBlit = false;
	} 

	IDirect3DTexture9 *srcTex = mpUploadContext->GetD3DTexture();
	uint32 srcWidth = mSource.pixmap.w;
	uint32 srcHeight = mSource.pixmap.h;

	if (needBlit && mpCustomPipeline) {
		needBlit = false;

		try {
			vdsize32 viewportSize(rClient.right, rClient.bottom);

			if (mbDestRectEnabled)
				viewportSize = mDestRect.size();

			mpCustomPipeline->Run(mpUploadContext->GetD3DTextures(), vdsize32(srcWidth, srcHeight), vdsize32(mSource.pixmap.w, mSource.pixmap.h), viewportSize);

			if (mpCustomPipeline->ContainsFinalBlit()) {
				mpManager->SetSwapChainActive(mpSwapChain);

				vdrefptr<IDirect3DSurface9> rt;
				HRESULT hr = mpD3DDevice->GetRenderTarget(0, ~rt);
				if (SUCCEEDED(hr)) {
					D3DSURFACE_DESC desc;
					hr = rt->GetDesc(&desc);
					if (SUCCEEDED(hr)) {
						uint32 clippedWidth = std::min<uint32>(srcWidth, rClient0.right);
						uint32 clippedHeight = std::min<uint32>(srcHeight, rClient0.bottom);

						D3DVIEWPORT9 vp = {};
						vp.X = 0;
						vp.Y = 0;
						vp.Width = (DWORD)rClippedClient.right;
						vp.Height = (DWORD)rClippedClient.bottom;
						vp.MinZ = 0;
						vp.MaxZ = 1;

						hr = mpD3DDevice->SetViewport(&vp);
						if (SUCCEEDED(hr)) {

							D3DRECT r;
							r.x1 = 0;
							r.y1 = 0;
							r.x2 = rClippedClient.right;
							r.y2 = rClippedClient.bottom;

							hr = mpD3DDevice->Clear(1, &r, D3DCLEAR_TARGET, mBackgroundColor, 0.0f, 0);
							if (FAILED(hr))
								return false;

							const float invw = 1.0f / (float)rClippedClient.right;
							const float invh = 1.0f / (float)rClippedClient.bottom;

							vdrect32f dstRect(0.0f, 0.0f, (float)rClient0.right, (float)rClient0.bottom);

							if (mbDestRectEnabled)
								dstRect = vdrect32f((float)mDestRect.left, (float)mDestRect.top, (float)mDestRect.right, (float)mDestRect.bottom);

							mpCustomPipeline->RunFinal(
								dstRect, viewportSize
							);

							bSuccess = true;
						}
					}
				}
			} else {
				needBlit = true;

				srcTex = mpCustomPipeline->GetFinalOutput(srcWidth, srcHeight);
			}
		} catch(const MyError&) {
		}
	}
	
	if (needBlit) {
		bSuccess = false;
		needBlit = false;

		D3DRECT rects[4];
		D3DRECT *nextRect = rects;
		RECT rDest = rClippedClient;

		if (mbDestRectEnabled) {
			// clip client rect to dest rect
			if (rDest.left < mDestRect.left)
				rDest.left = mDestRect.left;

			if (rDest.top < mDestRect.top)
				rDest.top = mDestRect.top;

			if (rDest.right > mDestRect.right)
				rDest.right = mDestRect.right;

			if (rDest.bottom > mDestRect.bottom)
				rDest.bottom = mDestRect.bottom;

			// fix rect in case dest rect lies entirely outside of client rect
			if (rDest.left > rClippedClient.right)
				rDest.left = rClippedClient.right;

			if (rDest.top > rClippedClient.bottom)
				rDest.top = rClippedClient.bottom;

			if (rDest.right < rDest.left)
				rDest.right = rDest.left;

			if (rDest.bottom < rDest.top)
				rDest.bottom = rDest.top;
		}

		if (rDest.right <= rDest.left || rDest.bottom <= rDest.top) {
			mpManager->SetSwapChainActive(mpSwapChain);

			D3DRECT r;
			r.x1 = rClippedClient.left;
			r.y1 = rClippedClient.top;
			r.x2 = rClippedClient.right;
			r.y2 = rClippedClient.bottom;

			HRESULT hr = mpD3DDevice->Clear(1, &r, D3DCLEAR_TARGET, mBackgroundColor, 0.0f, 0);
			if (FAILED(hr))
				return false;
		} else {
			if (rDest.top > rClippedClient.top) {
				nextRect->x1 = rClippedClient.left;
				nextRect->y1 = rClippedClient.top;
				nextRect->x2 = rClippedClient.right;
				nextRect->y2 = rDest.top;
				++nextRect;
			}

			if (rDest.left > rClippedClient.left) {
				nextRect->x1 = rClippedClient.left;
				nextRect->y1 = rDest.top;
				nextRect->x2 = rDest.left;
				nextRect->y2 = rDest.bottom;
				++nextRect;
			}

			if (rDest.right < rClippedClient.right) {
				nextRect->x1 = rDest.right;
				nextRect->y1 = rDest.top;
				nextRect->x2 = rClippedClient.right;
				nextRect->y2 = rDest.bottom;
				++nextRect;
			}

			if (rDest.bottom < rClippedClient.bottom) {
				nextRect->x1 = rClippedClient.left;
				nextRect->y1 = rDest.bottom;
				nextRect->x2 = rClippedClient.right;
				nextRect->y2 = rClippedClient.bottom;
				++nextRect;
			}

			HRESULT hr;
			if (nextRect > rects) {
				mpManager->SetSwapChainActive(mpSwapChain);

				hr = mpD3DDevice->Clear(nextRect - rects, rects, D3DCLEAR_TARGET, mBackgroundColor, 0.0f, 0);
				if (FAILED(hr))
					return false;
			}

			VDVideoDisplayDX9Manager::EffectContext ctx {};

			ctx.mpSourceTexture1 = srcTex;
			ctx.mpSourceTexture2 = NULL;
			ctx.mpSourceTexture3 = NULL;
			ctx.mpSourceTexture4 = NULL;
			ctx.mpSourceTexture5 = NULL;
			ctx.mpInterpFilterH = NULL;
			ctx.mpInterpFilterV = NULL;
			ctx.mpInterpFilter = NULL;
			ctx.mSourceW = srcWidth;
			ctx.mSourceH = srcHeight;
			ctx.mSourceArea.set(0, 0, (float)ctx.mSourceW, (float)ctx.mSourceH);

			D3DSURFACE_DESC desc;

			hr = ctx.mpSourceTexture1->GetLevelDesc(0, &desc);
			if (FAILED(hr))
				return false;

			ctx.mSourceTexW = desc.Width;
			ctx.mSourceTexH = desc.Height;
			ctx.mInterpHTexW = 1;
			ctx.mInterpHTexH = 1;
			ctx.mInterpVTexW = 1;
			ctx.mInterpVTexH = 1;
			ctx.mInterpTexW = 1;
			ctx.mInterpTexH = 1;
			ctx.mViewportX = rDest.left;
			ctx.mViewportY = rDest.top;
			ctx.mViewportW = rDest.right - rDest.left;
			ctx.mViewportH = rDest.bottom - rDest.top;
			ctx.mOutputX = 0;
			ctx.mOutputY = 0;
			ctx.mOutputW = rDest.right - rDest.left;
			ctx.mOutputH = rDest.bottom - rDest.top;
			ctx.mFieldOffset = 0.0f;
			ctx.mDefaultUVScaleCorrectionX = 1.0f;
			ctx.mDefaultUVScaleCorrectionY = 1.0f;
			ctx.mChromaScaleU = 1.0f;
			ctx.mChromaScaleV = 1.0f;
			ctx.mChromaOffsetU = 0.0f;
			ctx.mChromaOffsetV = 0.0f;
			ctx.mPixelSharpnessX = mPixelSharpnessX;
			ctx.mPixelSharpnessY = mPixelSharpnessY;
			ctx.mbHighPrecision = mbHighPrecision;

			if (mbCubicInitialized &&
				(uint32)rClient.right <= dispMode.Width &&
				(uint32)rClient.bottom <= dispMode.Height &&
				(uint32)mSource.pixmap.w <= dispMode.Width &&
				(uint32)mSource.pixmap.h <= dispMode.Height
				)
			{
				int cubicMode = mCubicMode;

				if (!InitBicubicPS2Filters(ctx.mViewportW, ctx.mViewportH))
					cubicMode = VDVideoDisplayDX9Manager::kCubicNotPossible;
				else {
					ctx.mpInterpFilterH = mpD3DInterpFilterTextureH;
					ctx.mpInterpFilterV = mpD3DInterpFilterTextureV;
					ctx.mInterpHTexW = mInterpFilterHTexSize;
					ctx.mInterpHTexH = 1;
					ctx.mInterpVTexW = mInterpFilterVTexSize;
					ctx.mInterpVTexH = 1;
				}

				if (mbHighPrecision && mpVideoManager->Is16FEnabled() && cubicMode == mCubicMode) {
					bSuccess = mpVideoManager->RunEffect(ctx, g_technique_bicubic_2_0_dither, pRTMain);
				} else {
					switch(cubicMode) {
					case VDVideoDisplayDX9Manager::kCubicUsePS2_0Path:
						bSuccess = mpVideoManager->RunEffect(ctx, g_technique_bicubic_2_0, pRTMain);
						break;
					default:
						mpManager->SetSwapChainActive(mpSwapChain);
						bSuccess = mpVideoManager->BlitFixedFunction(ctx, pRTMain, true);
						break;
					}
				}
			} else if (mbUseScreenFX) {
				bSuccess = true;

				if (mScreenFXInfo.mPALBlendingOffset != 0) {
					// rebuild PAL artifacting texture if necessary
					uint32 srcw = ctx.mSourceW;
					uint32 srch = ctx.mSourceH;
					uint32 texw = ctx.mSourceW;
					uint32 texh = ctx.mSourceH;

					// can't take nonpow2conditional due to dependent read in subsequent passes
					if (!mpManager->AdjustTextureSize(texw, texh)) {
						bSuccess = false;
					}

					if (bSuccess && mpD3DArtifactingRTT && (mCachedArtifactingTexW != texw || mCachedArtifactingTexH != texh)) {
						vdsaferelease <<= mpD3DArtifactingRTT;
					}

					if (bSuccess && !mpD3DArtifactingRTT) {
						hr = mpD3DDevice->CreateTexture(texw, texh, 1, D3DUSAGE_RENDERTARGET, D3DFMT_X8R8G8B8, D3DPOOL_DEFAULT, ~mpD3DArtifactingRTT, nullptr);
						bSuccess = SUCCEEDED(hr);
						if (bSuccess) {
							mCachedArtifactingTexW = texw;
							mCachedArtifactingTexH = texh;
						}
					}

					if (mpD3DArtifactingRTT) {
						if (mCachedArtifactingW != srcw || mCachedArtifactingH != srch) {
							mCachedArtifactingW = srcw;
							mCachedArtifactingH = srch;

							mpManager->ClearRenderTarget(mpD3DArtifactingRTT);
						}
					}

					if (bSuccess) {
						vdrefptr<IDirect3DSurface9> palRTSurface;
						hr = mpD3DArtifactingRTT->GetSurfaceLevel(0, ~palRTSurface);
						bSuccess = SUCCEEDED(hr);

						if (bSuccess) {
							auto ctx2 = ctx;
							ctx2.mOutputX = 0;
							ctx2.mOutputY = 0;
							ctx2.mOutputW = ctx.mSourceW;
							ctx2.mOutputH = ctx.mSourceH;
							ctx2.mViewportX = 0;
							ctx2.mViewportY = 0;
							ctx2.mViewportW = ctx2.mSourceW;
							ctx2.mViewportH = ctx2.mSourceH;
							ctx2.mbUseUV1Area = true;
							ctx2.mUV1Area.set(
								0.0f,
								0.0f,
								(float)ctx2.mSourceW / (float)ctx2.mSourceTexW,
								(float)ctx2.mSourceH / (float)ctx2.mSourceTexH
							);

							ctx2.mUV1Area.translate(0.0f, (float)mScreenFXInfo.mPALBlendingOffset / (float)ctx2.mSourceTexH);

							bSuccess = mpVideoManager->RunEffect(ctx2, g_technique_screenfx_palartifacting, palRTSurface);

							ctx.mpSourceTexture1 = mpD3DArtifactingRTT;
							ctx.mSourceTexW = mCachedArtifactingTexW;
							ctx.mSourceTexH = mCachedArtifactingTexH;
						}
					}
				}

				// rebuild the scanline texture if necessary
				if (mScreenFXInfo.mScanlineIntensity > 0 && (!mpD3DScanlineMaskTexture
					|| mCachedScanlineSrcH != ctx.mSourceH
					|| mCachedScanlineDstH != ctx.mOutputH
					|| mCachedScanlineIntensity != mScreenFXInfo.mScanlineIntensity))
				{
					vdsaferelease <<= mpD3DScanlineMaskTexture;

					int texw = 1;
					int texh = ctx.mOutputH;

					mpManager->AdjustTextureSize(texw, texh, true);

					vdrefptr<IVDD3D9InitTexture> initTex;
					if (mpManager->CreateInitTexture(1, texh, 1, D3DFMT_A8R8G8B8, ~initTex)) {
						VDD3D9LockInfo lockInfo;
						if (initTex->Lock(0, lockInfo)) {
							VDDisplayCreateScanlineMaskTexture((uint32 *)lockInfo.mpData, lockInfo.mPitch, ctx.mSourceH, ctx.mOutputH, texh, mScreenFXInfo.mScanlineIntensity);
							initTex->Unlock(0);

							vdrefptr<IVDD3D9Texture> tex;
							if (mpManager->CreateTexture(initTex, ~tex)) {
								mpD3DScanlineMaskTexture = tex->GetD3DTexture();

								mCachedScanlineSrcH = ctx.mSourceH;
								mCachedScanlineDstH = ctx.mOutputH;
								mCachedScanlineNormH = (float)ctx.mOutputH / (float)texh;
								mCachedScanlineIntensity = mScreenFXInfo.mScanlineIntensity;
							} else {
								bSuccess = false;
							}
						} else {
							bSuccess = false;
						}
					} else {
						bSuccess = false;
					}
				}

				const TechniqueInfo *kTechniques[2][2][2][3] = {
					{
						{
							{
								&g_technique_screenfx_ptlinear_noscanlines_linear,
								&g_technique_screenfx_ptlinear_noscanlines_gamma,
								&g_technique_screenfx_ptlinear_noscanlines_cc
							},
							{
								&g_technique_screenfx_ptlinear_scanlines_linear,
								&g_technique_screenfx_ptlinear_scanlines_gamma,
								&g_technique_screenfx_ptlinear_scanlines_cc
							},
						},
						{
							{
								&g_technique_screenfx_sharp_noscanlines_linear,
								&g_technique_screenfx_sharp_noscanlines_gamma,
								&g_technique_screenfx_sharp_noscanlines_cc
							},
							{
								&g_technique_screenfx_sharp_scanlines_linear,
								&g_technique_screenfx_sharp_scanlines_gamma,
								&g_technique_screenfx_sharp_scanlines_cc
							},
						},
					},
					{
						{
							{
								&g_technique_screenfx_distort_ptlinear_noscanlines_linear,
								&g_technique_screenfx_distort_ptlinear_noscanlines_gamma,
								&g_technique_screenfx_distort_ptlinear_noscanlines_cc
							},
							{
								&g_technique_screenfx_distort_ptlinear_scanlines_linear,
								&g_technique_screenfx_distort_ptlinear_scanlines_gamma,
								&g_technique_screenfx_distort_ptlinear_scanlines_cc
							},
						},
						{
							{
								&g_technique_screenfx_distort_sharp_noscanlines_linear,
								&g_technique_screenfx_distort_sharp_noscanlines_gamma,
								&g_technique_screenfx_distort_sharp_noscanlines_cc
							},
							{
								&g_technique_screenfx_distort_sharp_scanlines_linear,
								&g_technique_screenfx_distort_sharp_scanlines_gamma,
								&g_technique_screenfx_distort_sharp_scanlines_cc
							},
						},
					},
				};

				const bool useDistortion = mScreenFXInfo.mDistortionX > 0;
				const bool useSharpBilinear = mPixelSharpnessX > 1 || mPixelSharpnessY > 1;
				const bool useScanlines = mScreenFXInfo.mScanlineIntensity > 0.0f;
				const bool useGammaCorrection = mScreenFXInfo.mGamma != 1.0f;
				const bool useColorCorrection = mScreenFXInfo.mColorCorrectionMatrix[0][0] != 0.0f;

				struct VSConstants {
					float mScanlineInfo[4];
					float mDistortionInfo[4];
				} vsConstants {};

				vsConstants.mScanlineInfo[0] = mCachedScanlineNormH;
				vsConstants.mDistortionInfo[0] = 1.0f;
				vsConstants.mDistortionInfo[1] = useDistortion ? -0.5f : 0.0f;

				struct PSConstants {
					float mSharpnessInfo[4];
					float mDistortionScales[4];
					float mImageUVSize[4];
					float mColorCorrectMatrix[3][4];
				} psConstants {};

				psConstants.mSharpnessInfo[0] = mPixelSharpnessX;
				psConstants.mSharpnessInfo[1] = mPixelSharpnessY;
				psConstants.mSharpnessInfo[2] = 1.0f / desc.Height;
				psConstants.mSharpnessInfo[3] = 1.0f / desc.Width;
	
				VDDisplayDistortionMapping distortionMapping;
				distortionMapping.Init(mScreenFXInfo.mDistortionX, mScreenFXInfo.mDistortionYRatio, (float)ctx.mOutputW / (float)ctx.mOutputH);

				psConstants.mDistortionScales[0] = distortionMapping.mScaleX;
				psConstants.mDistortionScales[1] = distortionMapping.mScaleY;
				psConstants.mDistortionScales[2] = distortionMapping.mSqRadius;

				psConstants.mImageUVSize[0] = useSharpBilinear ? (float)ctx.mSourceW : (float)ctx.mSourceW / (float)ctx.mSourceTexW;
				psConstants.mImageUVSize[1] = useSharpBilinear ? (float)ctx.mSourceH : (float)ctx.mSourceH / (float)ctx.mSourceTexH;
				psConstants.mImageUVSize[2] = useScanlines ? useSharpBilinear ? 0.25f : 0.25f / (float)ctx.mSourceTexH : 0.0f;
				psConstants.mImageUVSize[3] = mCachedScanlineNormH;
				
				// need to transpose matrix to column major storage
				psConstants.mColorCorrectMatrix[0][0] = mScreenFXInfo.mColorCorrectionMatrix[0][0];
				psConstants.mColorCorrectMatrix[0][1] = mScreenFXInfo.mColorCorrectionMatrix[1][0];
				psConstants.mColorCorrectMatrix[0][2] = mScreenFXInfo.mColorCorrectionMatrix[2][0];
				psConstants.mColorCorrectMatrix[1][0] = mScreenFXInfo.mColorCorrectionMatrix[0][1];
				psConstants.mColorCorrectMatrix[1][1] = mScreenFXInfo.mColorCorrectionMatrix[1][1];
				psConstants.mColorCorrectMatrix[1][2] = mScreenFXInfo.mColorCorrectionMatrix[2][1];
				psConstants.mColorCorrectMatrix[2][0] = mScreenFXInfo.mColorCorrectionMatrix[0][2];
				psConstants.mColorCorrectMatrix[2][1] = mScreenFXInfo.mColorCorrectionMatrix[1][2];
				psConstants.mColorCorrectMatrix[2][2] = mScreenFXInfo.mColorCorrectionMatrix[2][2];

				mpD3DDevice->SetVertexShaderConstantF(16, (const float *)&vsConstants, sizeof(vsConstants)/16);
				mpD3DDevice->SetPixelShaderConstantF(16, (const float *)&psConstants, sizeof(psConstants)/16);

				ctx.mbAutoBilinear = (mPreferredFilter != kFilterPoint);
				ctx.mpSourceTexture2 = mpD3DGammaRampTexture;
				ctx.mpSourceTexture3 = mpD3DScanlineMaskTexture;

				if (useScanlines) 
					ctx.mSourceArea.translate(0.0f, 0.25f);

				if (useSharpBilinear) {
					ctx.mbUseUV0Scale = true;
					ctx.mUV0Scale = vdfloat2{(float)ctx.mSourceTexW, (float)ctx.mSourceTexH};
				}

				const TechniqueInfo& technique = *kTechniques[useDistortion][useSharpBilinear][useScanlines][useColorCorrection ? 2 : useGammaCorrection ? 1 : 0];

				if (mScreenFXInfo.mBloomRadius <= 0) {
					bSuccess = mpVideoManager->RunEffect(ctx, technique, pRTMain);
				} else {
					float blurRadius = mScreenFXInfo.mBloomRadius * (float)ctx.mOutputW / (float)ctx.mSourceW;
					uint32 factor1 = 1;
					uint32 factor2 = 1;
					float prescaleOffset = 0.0f;
					bool prescale2x = false;

					if (blurRadius > 56) {
						prescaleOffset = 1.0f;
						factor1 = 16;
						factor2 = 4;
						prescale2x = true;
					} else if (blurRadius > 28) {
						prescaleOffset = 1.0f;
						factor1 = 8;
						factor2 = 4;
						prescale2x = true;
					} else if (blurRadius > 14) {
						factor1 = 4;
						factor2 = 4;
						prescaleOffset = 1.0f;
					} else if (blurRadius > 7) {
						factor1 = 2;
						factor2 = 2;
						prescaleOffset = 0.5f;
					} else {
						factor1 = 1;
						factor2 = 1;
						prescaleOffset = 0.0f;
					}

					uint32 blurW = std::max<uint32>((ctx.mOutputW + factor1 - 1)/factor1, 1);
					uint32 blurH = std::max<uint32>((ctx.mOutputH + factor1 - 1)/factor1, 1);
					uint32 prescaleW = blurW * (factor1 / factor2);
					uint32 prescaleH = blurH * (factor1 / factor2);

					uint32 inputTexW = ctx.mOutputW;
					uint32 inputTexH = ctx.mOutputH;
					uint32 prescaleTexW = prescale2x ? prescaleW : 0;
					uint32 prescaleTexH = prescale2x ? prescaleH : 0;
					uint32 blurTexW = blurW;
					uint32 blurTexH = blurH;

					bSuccess = true;

					if (!mpManager->AdjustTextureSize(inputTexW, inputTexH, true)
						|| (prescaleTexW && !mpManager->AdjustTextureSize(prescaleTexW, prescaleTexH, true))
						|| !mpManager->AdjustTextureSize(blurTexW, blurTexH, true))
					{
						bSuccess = false;
					}

					bool needClear = false;
					if (bSuccess) {
						if (mCachedBloomOutputW != ctx.mOutputW
							|| mCachedBloomOutputH != ctx.mOutputH
							|| mCachedBloomPrescaleW != prescaleW
							|| mCachedBloomPrescaleH != prescaleH
							|| mCachedBloomBlurW != blurW
							|| mCachedBloomBlurH != blurH)
						{
							mCachedBloomOutputW = ctx.mOutputW;
							mCachedBloomOutputH = ctx.mOutputH;
							mCachedBloomPrescaleW = prescaleW;
							mCachedBloomPrescaleH = prescaleH;
							mCachedBloomBlurW = blurW;
							mCachedBloomBlurH = blurH;

							needClear = true;
						}
					}

					if (mCachedBloomInputTexW != inputTexW || mCachedBloomInputTexH != inputTexH) {
						vdsaferelease <<= mpD3DBloomPrescale1RTT;
						mCachedBloomInputTexW = inputTexW;
						mCachedBloomInputTexH = inputTexH;
					}

					if (mCachedBloomPrescaleTexW != prescaleTexW || mCachedBloomPrescaleTexH != prescaleTexH) {
						vdsaferelease <<= mpD3DBloomPrescale2RTT;

						mCachedBloomPrescaleTexW = prescaleTexW;
						mCachedBloomPrescaleTexH = prescaleTexH;
					}

					if (mCachedBloomBlurTexW != blurTexW || mCachedBloomBlurTexH != blurTexH) {
						vdsaferelease <<= mpD3DBloomBlur1RTT;
						vdsaferelease <<= mpD3DBloomBlur2RTT;

						mCachedBloomBlurTexW = blurTexW;
						mCachedBloomBlurTexH = blurTexH;
					}

					if (bSuccess && !mpD3DBloomPrescale1RTT) {
						hr = mpD3DDevice->CreateTexture(inputTexW, inputTexH, 1, D3DUSAGE_RENDERTARGET, D3DFMT_X8R8G8B8, D3DPOOL_DEFAULT, ~mpD3DBloomPrescale1RTT, nullptr);
						if (FAILED(hr))
							bSuccess = false;

						needClear = true;
					}

					if (bSuccess && prescaleTexW && !mpD3DBloomPrescale2RTT) {
						hr = mpD3DDevice->CreateTexture(prescaleTexW, prescaleTexH, 1, D3DUSAGE_RENDERTARGET, D3DFMT_X8R8G8B8, D3DPOOL_DEFAULT, ~mpD3DBloomPrescale2RTT, nullptr);
						if (FAILED(hr))
							bSuccess = false;

						needClear = true;
					}

					if (bSuccess && !mpD3DBloomBlur1RTT) {
						hr = mpD3DDevice->CreateTexture(blurTexW, blurTexH, 1, D3DUSAGE_RENDERTARGET, D3DFMT_X8R8G8B8, D3DPOOL_DEFAULT, ~mpD3DBloomBlur1RTT, nullptr);
						if (FAILED(hr))
							bSuccess = false;

						needClear = true;
					}

					if (bSuccess && !mpD3DBloomBlur2RTT) {
						hr = mpD3DDevice->CreateTexture(blurTexW, blurTexH, 1, D3DUSAGE_RENDERTARGET, D3DFMT_X8R8G8B8, D3DPOOL_DEFAULT, ~mpD3DBloomBlur2RTT, nullptr);
						if (FAILED(hr))
							bSuccess = false;

						needClear = true;
					}

					VDASSERT(bSuccess);

					if (needClear) {
						if (mpD3DBloomPrescale1RTT)
							mpManager->ClearRenderTarget(mpD3DBloomPrescale1RTT);

						if (mpD3DBloomPrescale2RTT)
							mpManager->ClearRenderTarget(mpD3DBloomPrescale2RTT);

						if (mpD3DBloomBlur1RTT)
							mpManager->ClearRenderTarget(mpD3DBloomBlur1RTT);

						if (mpD3DBloomBlur2RTT)
							mpManager->ClearRenderTarget(mpD3DBloomBlur2RTT);
					}

					struct VSConstants {
						float mViewport[4];
						float mUVOffset[4];
						float mBlurOffsets1[4];
						float mBlurOffsets2[4];
					} vsConstants1 {}, vsConstants2 {}, vsConstants3 {};

					vsConstants1.mUVOffset[0] = prescaleOffset / (float)mCachedBloomInputTexW;
					vsConstants1.mUVOffset[1] = prescaleOffset / (float)mCachedBloomInputTexH;

					if (mCachedBloomPrescaleTexW) {
						vsConstants2.mUVOffset[0] = prescaleOffset / (float)mCachedBloomPrescaleTexW;
						vsConstants2.mUVOffset[1] = prescaleOffset / (float)mCachedBloomPrescaleTexH;
					} else {
						vsConstants2.mUVOffset[0] = vsConstants1.mUVOffset[0];
						vsConstants2.mUVOffset[1] = vsConstants1.mUVOffset[1];
					}

					const float mainBlurRadius = blurRadius / (float)factor1;
					const float blurHeightScale = 1.5f / std::min<float>(7.0f, mainBlurRadius);

					const float wf = expf(-7.5f * (1.5f / 7.0f));
					const float w0 = std::max<float>(0, expf(-7.0f * blurHeightScale) - wf);
					const float w1 = std::max<float>(0, expf(-6.0f * blurHeightScale) - wf);
					const float w2 = std::max<float>(0, expf(-5.0f * blurHeightScale) - wf);
					const float w3 = std::max<float>(0, expf(-4.0f * blurHeightScale) - wf);
					const float w4 = std::max<float>(0, expf(-3.0f * blurHeightScale) - wf);
					const float w5 = std::max<float>(0, expf(-2.0f * blurHeightScale) - wf);
					const float w6 = std::max<float>(0, expf(-1.0f * blurHeightScale) - wf);

					const float wscale = 1.0f / (w6 + 2*(w5+w4+w3+w2+w1));

					const float blurUStep = 1.0f / (float)blurTexW;
					const float blurVStep = 1.0f / (float)blurTexH;
					const float filterOffset0 = 1.0f + w4 / std::max<float>(w4 + w5, 1e-10f);
					const float filterOffset1 = 3.0f + w2 / std::max<float>(w2 + w3, 1e-10f);
					const float filterOffset2 = 5.0f + w0 / std::max<float>(w0 + w1, 1e-10f);

					vsConstants2.mBlurOffsets1[0] = filterOffset0 * blurUStep;
					vsConstants2.mBlurOffsets2[0] = filterOffset1 * blurUStep;
					vsConstants2.mBlurOffsets2[2] = filterOffset2 * blurUStep;

					vsConstants3.mBlurOffsets1[1] = filterOffset0 * blurVStep;
					vsConstants3.mBlurOffsets2[1] = filterOffset1 * blurVStep;
					vsConstants3.mBlurOffsets2[3] = filterOffset2 * blurVStep;

					struct PSConstants {
						float mWeights[4];
						float mThresholds[4];
						float mScales[4];
					} psConstants {};

					psConstants.mWeights[0] = w6 * wscale;
					psConstants.mWeights[1] = (w5+w4) * wscale;
					psConstants.mWeights[2] = (w3+w2) * wscale;
					psConstants.mWeights[3] = (w1+w0) * wscale;

					psConstants.mThresholds[0] = 1.0f + mScreenFXInfo.mBloomThreshold;
					psConstants.mThresholds[1] = -mScreenFXInfo.mBloomThreshold;
					psConstants.mScales[0] = mScreenFXInfo.mBloomDirectIntensity;
					psConstants.mScales[1] = mScreenFXInfo.mBloomIndirectIntensity;

					// input render
					auto ctx2 = ctx;
					ctx2.mViewportX = 0;
					ctx2.mViewportY = 0;
					ctx2.mOutputX = 0;
					ctx2.mOutputY = 0;

					if (bSuccess) {
						vdrefptr<IDirect3DSurface9> prescaleSurface;
						hr = mpD3DBloomPrescale1RTT->GetSurfaceLevel(0, ~prescaleSurface);
						bSuccess = SUCCEEDED(hr) && mpVideoManager->RunEffect(ctx2, technique, prescaleSurface);
					}

					ctx2.mbUseUV0Scale = false;

					// prescale passes
					if (bSuccess) {
						mpD3DDevice->SetPixelShaderConstantF(16, (const float *)&psConstants, sizeof(psConstants)/16);

						ctx2.mpSourceTexture1 = mpD3DBloomPrescale1RTT;
						ctx2.mSourceTexW = mCachedBloomInputTexW;
						ctx2.mSourceTexH = mCachedBloomInputTexH;
						ctx2.mSourceArea = vdrect32f(0, 0, mCachedBloomPrescaleW * factor2, mCachedBloomPrescaleH * factor2);

						if (prescale2x) {
							mpD3DDevice->SetVertexShaderConstantF(16, (const float *)&vsConstants1, sizeof(vsConstants1)/16);

							ctx2.mOutputW = ctx2.mViewportW = prescaleW;
							ctx2.mOutputH = ctx2.mViewportH = prescaleH;

							vdrefptr<IDirect3DSurface9> prescale2Surface;
							hr = mpD3DBloomPrescale2RTT->GetSurfaceLevel(0, ~prescale2Surface);
							bSuccess = SUCCEEDED(hr) && mpVideoManager->RunEffect(ctx2, g_technique_screenfx_bloom_prescale, prescale2Surface);

							ctx2.mpSourceTexture1 = mpD3DBloomPrescale2RTT;
							ctx2.mSourceTexW = mCachedBloomPrescaleTexW;
							ctx2.mSourceTexH = mCachedBloomPrescaleTexH;
							ctx2.mSourceArea = vdrect32f(0, 0, mCachedBloomPrescaleW, mCachedBloomPrescaleH);
						}

						if (bSuccess) {
							mpD3DDevice->SetVertexShaderConstantF(16, (const float *)&vsConstants2, sizeof(vsConstants2)/16);

							ctx2.mOutputW = ctx2.mViewportW = blurW;
							ctx2.mOutputH = ctx2.mViewportH = blurH;
							vdrefptr<IDirect3DSurface9> blurSurface;
							hr = mpD3DBloomBlur1RTT->GetSurfaceLevel(0, ~blurSurface);
							bSuccess = SUCCEEDED(hr) && mpVideoManager->RunEffect(ctx2, prescale2x ? g_technique_screenfx_bloom_prescale2 : g_technique_screenfx_bloom_prescale, blurSurface);
						}
					}

					// horizontal blur pass
					if (bSuccess) {
						ctx2.mpSourceTexture1 = mpD3DBloomBlur1RTT;
						ctx2.mSourceTexW = mCachedBloomBlurTexW;
						ctx2.mSourceTexH = mCachedBloomBlurTexH;
						ctx2.mSourceArea = vdrect32f(0, 0, mCachedBloomBlurW, mCachedBloomBlurH);

						vdrefptr<IDirect3DSurface9> blur2Surface;
						hr = mpD3DBloomBlur2RTT->GetSurfaceLevel(0, ~blur2Surface);
						bSuccess = SUCCEEDED(hr) && mpVideoManager->RunEffect(ctx2, g_technique_screenfx_bloom_blur, blur2Surface);
					}

					// vertical blur pass
					if (bSuccess) {
						ctx2.mpSourceTexture1 = mpD3DBloomBlur2RTT;
						
						mpD3DDevice->SetVertexShaderConstantF(16, (const float *)&vsConstants3, sizeof(vsConstants3)/16);

						vdrefptr<IDirect3DSurface9> blurSurface;
						hr = mpD3DBloomBlur1RTT->GetSurfaceLevel(0, ~blurSurface);
						bSuccess = SUCCEEDED(hr) && mpVideoManager->RunEffect(ctx2, g_technique_screenfx_bloom_blur, blurSurface);
					}

					// final pass
					if (bSuccess) {
						ctx.mpSourceTexture1 = mpD3DBloomBlur1RTT;
						ctx.mpSourceTexture2 = mpD3DBloomPrescale1RTT;
						ctx.mSourceTexW = mCachedBloomInputTexW;
						ctx.mSourceTexH = mCachedBloomInputTexH;
						ctx.mSourceArea = vdrect32f(0, 0, mCachedBloomOutputW, mCachedBloomOutputH);
						ctx.mbUseUV0Scale = false;
						ctx.mbUseUV1Area = true;
						ctx.mUV1Area = vdrect32f(0.0f, 0.0f,
							(float)ctx.mOutputW / ((float)factor1 * (float)blurTexW),
							(float)ctx.mOutputH / ((float)factor1 * (float)blurTexH));

						bSuccess = mpVideoManager->RunEffect(ctx, g_technique_screenfx_bloom_final, pRTMain);
					}

					VDASSERT(bSuccess);

				}
			} else if (mbHighPrecision && mpVideoManager->Is16FEnabled()) {
				if (mPreferredFilter == kFilterPoint)
					bSuccess = mpVideoManager->RunEffect(ctx, g_technique_point_2_0, pRTMain);
				else
					bSuccess = mpVideoManager->RunEffect(ctx, g_technique_bilinear_2_0, pRTMain);
			} else {
				if (mPreferredFilter == kFilterPoint) {
					mpManager->SetSwapChainActive(mpSwapChain);
					bSuccess = mpVideoManager->BlitFixedFunction(ctx, pRTMain, false);
				} else if ((mPixelSharpnessX > 1 || mPixelSharpnessY > 1) && mpVideoManager->IsPS20Enabled())
					bSuccess = mpVideoManager->RunEffect(ctx, g_technique_boxlinear_2_0, pRTMain);
				else if ((mPixelSharpnessX > 1 || mPixelSharpnessY > 1) && mbBoxlinearCapable11) {
					if (!InitBoxlinearPS11Filters(ctx.mViewportW, ctx.mViewportH, mPixelSharpnessX, mPixelSharpnessY))
						mbBoxlinearCapable11 = false;
					else {
						ctx.mpInterpFilter = mpD3DInterpFilterTexture;
						ctx.mInterpTexW = mInterpFilterTexSizeW;
						ctx.mInterpTexH = mInterpFilterTexSizeH;
					}

					if (ctx.mpInterpFilter)
						bSuccess = mpVideoManager->RunEffect(ctx, g_technique_boxlinear_1_1, pRTMain);
					else {
						mpManager->SetSwapChainActive(mpSwapChain);
						bSuccess = mpVideoManager->BlitFixedFunction(ctx, pRTMain, true);
					}
				} else {
					mpManager->SetSwapChainActive(mpSwapChain);

					bSuccess = mpVideoManager->BlitFixedFunction(ctx, pRTMain, true);
				}
			}
		}
	}

	pRTMain = NULL;

	if (mpCompositor) {
		D3DVIEWPORT9 vp;

		vp.X = 0;
		vp.Y = 0;
		vp.Width = rClippedClient.right;
		vp.Height = rClippedClient.bottom;
		vp.MinZ = 0;
		vp.MaxZ = 1;
		mpD3DDevice->SetViewport(&vp);

		if (mpRenderer->Begin()) {
			mpCompositor->Composite(*mpRenderer, compInfo);
			mpRenderer->End();
		}
	}

	if (mbDisplayDebugInfo || !mErrorString.empty() || (mpCustomPipeline && mpCustomPipeline->HasTimingInfo())) {
		D3DVIEWPORT9 vp;

		vp.X = 0;
		vp.Y = 0;
		vp.Width = rClippedClient.right;
		vp.Height = rClippedClient.bottom;
		vp.MinZ = 0;
		vp.MaxZ = 1;
		mpD3DDevice->SetViewport(&vp);
		DrawDebugInfo(mode, rClient);
	}

	if (bSuccess && !mpManager->EndScene())
		bSuccess = false;

	if (updateMode & kModeVSync)
		mpManager->Flush();

	mpManager->SetSwapChainActive(NULL);

	if (!bSuccess) {
		VDDEBUG_DX9DISP("VideoDisplay/DX9: Render failed -- applying boot to the head.");

		if (!mpManager->Reset())
			return false;

	} else {
		mbSwapChainImageValid = true;
		mbSwapChainPresentPending = true;
		mbSwapChainPresentPolling = false;
	}

	return bSuccess;
}

bool VDVideoDisplayMinidriverDX9::UpdateScreen(const RECT& rClient, UpdateMode updateMode, bool polling) {
	if (!mbSwapChainImageValid)
		return false;

	HRESULT hr;
	if (mbFullScreen) {
		hr = mpManager->PresentFullScreen(!polling && !(updateMode & kModeDoNotWait));

		if (!polling || !mbSwapChainPresentPolling) {
			mPresentHistory.mPresentStartTime = VDGetPreciseTick();
		}

		if (hr == S_OK) {
			mPresentHistory.mAveragePresentTime += ((VDGetPreciseTick() - mPresentHistory.mPresentStartTime)*VDGetPreciseSecondsPerTick() - mPresentHistory.mAveragePresentTime) * 0.01f;
		}

		if (hr == S_FALSE && polling) {
			++mPresentHistory.mPollCount;
			mPresentHistory.mbPresentPending = true;
		} else {
			mPresentHistory.mbPresentPending = false;
		}

	} else {
		bool vsync = (updateMode & kModeVSync) != 0;

		if (mbSwapChainVsync != vsync) {
			mbSwapChainVsync = vsync;

			if (vsync) {
				const auto refreshRate = mpManager->GetDisplayMode().RefreshRate;
				uint32 delay = refreshRate == 0 ? 5 : refreshRate > 50 ? 500 / refreshRate : 10;

				auto *cb = mSource.mpCB;
				mbSwapChainVsyncEvent = mpSwapChain->SetVsyncCallback(mpManager->GetMonitor(), [this, cb]{ OnVsyncEvent(cb); }, delay);
			} else {
				mpSwapChain->SetVsyncCallback(nullptr, {}, 0);
				mbSwapChainVsyncEvent = false;
			}
		}

		if (mbSwapChainVsyncEvent) {
			if (!mbSwapChainPresentPolling) {
				mpSwapChain->RequestVsyncCallback();
				mbSwapChainVsyncEventPending = true;
				mbSwapChainPresentPolling = true;
				mpManager->Flush();
				return true;
			} else if (mbSwapChainVsyncEventPending) {
				return true;
			} else {
				hr = mpManager->PresentSwapChain(mpSwapChain, &rClient, mhwnd, false, true, false, mSyncDelta, mPresentHistory);
			}
		} else {
			hr = mpManager->PresentSwapChain(mpSwapChain, &rClient, mhwnd, vsync, !polling || !mbSwapChainPresentPolling, polling || (updateMode & kModeDoNotWait) != 0, mSyncDelta, mPresentHistory);
		}
	}

	if (hr == S_FALSE && polling) {
		mbSwapChainPresentPolling = true;
		return true;
	}

	// Workaround for Windows Vista DWM composition chain not updating.
	if (!mbFullScreen && mbFirstPresent) {
		SetWindowPos(mhwnd, NULL, 0, 0, 0, 0, SWP_NOMOVE|SWP_NOSIZE|SWP_NOACTIVATE|SWP_NOZORDER|SWP_FRAMECHANGED);
		mbFirstPresent = false;
	}

	mbSwapChainPresentPending = false;
	mbSwapChainPresentPolling = false;
	VDASSERT(!mPresentHistory.mbPresentPending);

	if (FAILED(hr)) {
		VDDEBUG_DX9DISP("VideoDisplay/DX9: Render failed in UpdateScreen() with hr=%08X (%s) -- applying boot to the head.", hr, VDDispDecodeD3D9Error(hr));

		// TODO: Need to free all DEFAULT textures before proceeding

		if (!mpManager->Reset())
			return false;
	}

	mSource.mpCB->RequestNextFrame();
	return true;
}

void VDVideoDisplayMinidriverDX9::DrawDebugInfo(FilterMode mode, const RECT& rClient) {
	if (!mpFontRenderer) {
		// init font renderer
		if (!VDCreateFontRendererD3D9(~mpFontRenderer))
			return;

		mpFontRenderer->Init(mpManager);		// we explicitly allow this to fail
	}

	if (!mpManager->BeginScene())
		return;
	
	if (!mpFontRenderer->Begin())
		return;

	if (mbDisplayDebugInfo) {
		const char *modestr = "point";

		switch(mode) {
			case kFilterBilinear:
				modestr = "bilinear";
				break;
			case kFilterBicubic:
				modestr = "bicubic";
				break;
		}

		GetFormatString(mSource, mFormatString);
		mDebugString.sprintf("Direct3D9%s minidriver - %s (%s%s)  Average present time: %6.2fms"
			, mpManager->GetDeviceEx() ? "Ex" : ""
			, mFormatString.c_str()
			, modestr
			, mbHighPrecision && mpVideoManager->Is16FEnabled() ? "-16F" : ""
			, mPresentHistory.mAveragePresentTime * 1000.0);

		mpFontRenderer->DrawTextLine(10, rClient.bottom - 40, 0xFFFFFF00, 0, mDebugString.c_str());

		mDebugString.sprintf("Target scanline: %7.2f  Average bracket [%7.2f,%7.2f]  Last bracket [%4d,%4d]  Poll count %5d"
				, mPresentHistory.mScanlineTarget
				, mPresentHistory.mAverageStartScanline
				, mPresentHistory.mAverageEndScanline
				, mPresentHistory.mLastBracketY1
				, mPresentHistory.mLastBracketY2
				, mPresentHistory.mPollCount);
		mPresentHistory.mPollCount = 0;
		mpFontRenderer->DrawTextLine(10, rClient.bottom - 20, 0xFFFFFF00, 0, mDebugString.c_str());
	}

	if (!mErrorString.empty())
		mpFontRenderer->DrawTextLine(10, rClient.bottom - 60, 0xFFFF4040, 0, mErrorString.c_str());

	if (mpCustomPipeline && mpCustomPipeline->HasTimingInfo()) {
		uint32 numTimings = 0;
		const auto *passInfos = mpCustomPipeline->GetPassTimings(numTimings);

		if (passInfos) {
			for(uint32 i=0; i<numTimings; ++i) {
				if (i + 1 == numTimings)
					mDebugString.sprintf("Total: %7.2fms %ux%u", passInfos[i].mTiming * 1000.0f, mbDestRectEnabled ? mDestRect.width() : rClient.right, mbDestRectEnabled ? mDestRect.height() : rClient.bottom);
				else
					mDebugString.sprintf("Pass #%-2u: %7.2fms %ux%u%s"
						, i + 1
						, passInfos[i].mTiming * 1000.0f
						, passInfos[i].mOutputWidth
						, passInfos[i].mOutputHeight
						, passInfos[i].mbOutputFloat ? passInfos[i].mbOutputHalfFloat ? " half" : " float" : ""
					);
				mpFontRenderer->DrawTextLine(10, 10 + 14*i, 0xFFFFFF00, 0, mDebugString.c_str());
			}
		}
	}

	mpFontRenderer->End();
}

#undef VDDEBUG_DX9DISP
#undef D3D_DO
