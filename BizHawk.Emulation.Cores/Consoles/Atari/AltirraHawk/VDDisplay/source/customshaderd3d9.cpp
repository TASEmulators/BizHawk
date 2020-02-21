//	VirtualDub - Video processing and capture application
//	Display library - custom D3D9 shader support
//	Copyright (C) 1998-2016 Avery Lee
//
//	This program is free software; you can redistribute it and/or
//	modify it under the terms of the GNU General Public License
//	as published by the Free Software Foundation; either version 2
//	of the License, or (at your option) any later version.
//
//	This program is distributed in the hope that it will be useful,
//	but WITHOUT ANY WARRANTY; without even the implied warranty of
//	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//	GNU General Public License for more details.
//
//	You should have received a copy of the GNU General Public License
//	along with this program; if not, write to the Free Software
//	Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.

#include <d3dcommon.h>
#include <d3d10.h>
#include <vd2/system/binary.h>
#include <vd2/system/error.h>
#include <vd2/system/file.h>
#include <vd2/system/filesys.h>
#include <vd2/system/seh.h>
#include <vd2/system/vdalloc.h>
#include <vd2/system/VDString.h>
#include <vd2/system/vectors.h>
#include <vd2/system/vdstl_vectorview.h>
#include <vd2/system/w32assist.h>
#include <vd2/Kasumi/pixmap.h>
#include <vd2/Kasumi/pixmapops.h>
#include <vd2/Kasumi/pixmaputils.h>
#include <vd2/VDDisplay/display.h>
#include <vd2/VDDisplay/direct3d.h>
#include <vd2/VDDisplay/minid3dx.h>
#include <vd2/VDDisplay/internal/customshaderd3d9.h>

namespace {
	bool IsTrueValue(const VDStringSpanA& s) {
		return s != "false" && s != "0";
	}

	IVDDisplayImageDecoder *g_pVDDisplayImageDecoder;
}

void VDDisplaySetImageDecoder(IVDDisplayImageDecoder *p) {
	g_pVDDisplayImageDecoder = p;
}

class VDFileParseException : public MyError {
public:
	VDFileParseException(uint32 line, const char *error)
		: MyError("Parse error at line %u: %s.", line, error) {}
};

class VDD3D9Exception : public MyError {
public:
	VDD3D9Exception(uint32 hr) : MyError("Direct3D error: %08X", hr) {}
};

class VDDisplayCustomShaderProps {
public:
	const char *GetString(const VDStringSpanA& key) const;
	bool GetBool(const VDStringSpanA& key, bool defaultValue) const;
	bool Add(const VDStringSpanA& key, const VDStringSpanA& value);

private:
	vdhashmap<VDStringA, VDStringA> mProps;
};

const char *VDDisplayCustomShaderProps::GetString(const VDStringSpanA& key) const {
	auto it = mProps.find(key);

	return it != mProps.end() ? it->second.c_str() : nullptr;
}

bool VDDisplayCustomShaderProps::GetBool(const VDStringSpanA& key, bool defaultValue) const {
	auto it = mProps.find(key);

	return it != mProps.end() ? IsTrueValue(it->second) : defaultValue;
}

bool VDDisplayCustomShaderProps::Add(const VDStringSpanA& key, const VDStringSpanA& value) {
	auto r = mProps.insert_as(key);
	if (!r.second)
		return false;

	r.first->second = value;
	return true;
}

class VDDisplayCustomShaderD3D9 final : public VDD3D9Client {
	VDDisplayCustomShaderD3D9(const VDDisplayCustomShaderD3D9&) = delete;
	VDDisplayCustomShaderD3D9& operator=(const VDDisplayCustomShaderD3D9&) = delete;
public:
	struct TextureSpec {
		IDirect3DTexture9 *mpTexture;
		uint32 mTexWidth;
		uint32 mTexHeight;
		uint32 mImageWidth;
		uint32 mImageHeight;
		bool mbLinear;
	};

	VDDisplayCustomShaderD3D9(VDD3D9Manager *d3d9mgr);
	~VDDisplayCustomShaderD3D9();

	bool HasScalingFactor() const { return mbHasScalingFactor; }
	bool IsOutputHalfFloat() const { return mbHalfFloatFramebuffer; }
	bool IsOutputFloat() const { return mbFloatFramebuffer; }

	void Init(const char *shaderPath, const VDDisplayCustomShaderProps& propLookup, const vdhashmap<VDStringA, TextureSpec>& customTextureLookup, const TextureSpec *passSpecs, uint32 pass, const wchar_t *basePath, bool& inputUseLinear, uint32& maxPrevFrames);
	TextureSpec Run(const vdrect32f *dstRect, const TextureSpec *srcTexSpecs, const TextureSpec *prevTexSpecs, const vdsize32& viewportSize, bool lastStage);

public:
	void OnPreDeviceReset() override;
	void OnPostDeviceReset() override;

private:
	enum VariableRef : uint32 {
		kVariableRef_VideoSize,			// float4(video_width, video_height, 0, 0)
		kVariableRef_TextureSize,			// float4(texture_width, texture_height, 0, 0)
		kVariableRef_OutputSize,			// float4(output_width, output_height, 0, 0)
		kVariableRef_FrameCount,			// float4(frame_count, 0, 0, 0)
		kVariableRef_FrameDirection,		// float4(frame_direction, 0, 0, 0)
		kVariableRef_ModelViewProj,		// float4x4(model_view_projection)
	};

	enum ScaleType : uint32 {
		kScaleType_Source,
		kScaleType_Viewport,
		kScaleType_Absolute,
	};

	struct VariableBinding {
		VariableRef mVariable;
		sint32 mVarIndex;
		VDD3DXRegisterSet mRegisterSet;
		uint32 mDstOffset;
		uint32 mLength;
	};

	enum TexRef : uint32 {
		kTexRef_None,
		kTexRef_PassInput = 0x20000,
		kTexRef_PrevInput = 0x30000,
		kTexRef_Custom = 0x40000,
		kTexRef_ClassMask = UINT32_C(0xFFFF0000),
		kTexRef_IndexMask = UINT32_C(0x0000FFFF)
	};

	struct RenderState {
		uint32 mState;
		uint32 mValue;
	};

	struct SamplerState {
		uint32 mStage;
		uint32 mState;
		uint32 mValue;
	};

	struct TextureBinding {
		uint32 mStage;
		TexRef mTexRef;
	};

	struct UploadSpan {
		uint32 mStart;
		uint32 mCount;
		uint32 mSrcOffset;
	};

	struct ShaderInfo {
		vdfastvector<UploadSpan> mUploadSpansB;
		vdfastvector<UploadSpan> mUploadSpansI;
		vdfastvector<UploadSpan> mUploadSpansF;
		vdfastvector<uint32> mUploadWindow;
		vdfastvector<VariableBinding> mVariableBindings;
	};

	void UpdateVariables(void *dst, vdvector_view<VariableBinding> bindings, const TextureSpec *srcTexSpecs, const TextureSpec *prevTexSpecs);

	template<class T, HRESULT (__stdcall IDirect3DDevice9::*T_UploadFn)(UINT, T *, UINT)>
	void UploadShaderData(IDirect3DDevice9 *dev, const vdfastvector<UploadSpan>& spans, const void *src);

	bool ProcessShader(const uint32 *shader, uint32 shaderSize, ShaderInfo& shaderInfo, const vdhashmap<VDStringA, TextureSpec>& customTextureLookup, uint32& maxPrevFrames);

	static void WriteTransposedMatrix(float *dst, const vdfloat4x4& src, uint32 n);

	VDD3D9Manager *const mpD3DMgr;
	vdrefptr<IDirect3DVertexShader9> mpVertexShader;
	vdrefptr<IDirect3DPixelShader9> mpPixelShader;
	vdrefptr<IDirect3DTexture9> mpOutputTexture;
	vdrefptr<IDirect3DSurface9> mpOutputSurface;
	uint32 mPrevSrcWidth = 0;
	uint32 mPrevSrcHeight = 0;

	bool mbVSConstantTableInited = false;
	bool mbPSConstantTableInited = false;

	bool mbHasScalingFactor = false;
	ScaleType mScaleTypeX = kScaleType_Source;
	ScaleType mScaleTypeY = kScaleType_Source;
	float mScaleFactorX = 0;
	float mScaleFactorY = 0;
	bool mbFloatFramebuffer = false;
	bool mbHalfFloatFramebuffer = false;

	uint32 mPassIndex = 0;
	uint32 mFrame = 0;
	uint32 mFrameCountLimit = UINT32_C(0xFFFFFFFF);

	VDD3DXConstantTable mVertexShaderConstantTable;
	VDD3DXConstantTable mPixelShaderConstantTable;

	vdfastvector<RenderState> mRenderStates;
	vdfastvector<SamplerState> mSamplerStates;

	ShaderInfo mVertexShaderInfo = {};
	ShaderInfo mPixelShaderInfo = {};

	vdfastvector<TextureBinding> mTextureBindings;
	TextureSpec mOutputTexSpec;

	vdfastvector<TextureSpec> mCustomTextures;
};

VDDisplayCustomShaderD3D9::VDDisplayCustomShaderD3D9(VDD3D9Manager *d3d9mgr)
	: mpD3DMgr(d3d9mgr)
{
	d3d9mgr->Attach(this);
}

VDDisplayCustomShaderD3D9::~VDDisplayCustomShaderD3D9() {
	for(auto& tex : mCustomTextures)
		vdsaferelease <<= tex.mpTexture;

	mCustomTextures.clear();

	mpD3DMgr->Detach(this);
}

namespace {
	class VDDisplayIncludeHandlerD3D9 final : public ID3D10Include {
	public:
		VDDisplayIncludeHandlerD3D9(const wchar_t *defaultBasePath)
			: mDefaultBasePath(defaultBasePath)
		{
		}

		HRESULT STDMETHODCALLTYPE Open(D3D10_INCLUDE_TYPE includeType, LPCSTR pFileName, LPCVOID pParentData, LPCVOID *ppData, UINT *pBytes);
		HRESULT STDMETHODCALLTYPE Close(LPCVOID pData);

	private:
		struct IncludeHeader {
			wchar_t *mpBasePath;
		};

		VDStringW mDefaultBasePath;
	};

	HRESULT STDMETHODCALLTYPE VDDisplayIncludeHandlerD3D9::Open(D3D10_INCLUDE_TYPE includeType, LPCSTR pFileName, LPCVOID pParentData, LPCVOID *ppData, UINT *pBytes) {
		const wchar_t *basePath = pParentData ? ((IncludeHeader *)((const char *)pParentData - sizeof(IncludeHeader)))->mpBasePath : mDefaultBasePath.c_str();
		const auto newPath = VDMakePath(VDStringSpanW(basePath), VDTextAToW(pFileName));

		*ppData = nullptr;
		*pBytes = 0;

		try {
			VDFile f(newPath.c_str());
			sint64 len = f.size();

			if (len < 0 || len > 0xFFFFFF)
				return E_FAIL;

			const VDStringSpanW newBasePath = VDFileSplitPathLeftSpan(newPath);

			uint32 len32 = (uint32)len;
			uint32 len32Aligned = (len32 + 3) & ~3;
			vdautoblockptr mem(malloc(len32Aligned + (newBasePath.size() + 1) * sizeof(wchar_t) + sizeof(IncludeHeader)));
			if (!mem)
				return E_OUTOFMEMORY;

			IncludeHeader *hdr = (IncludeHeader *)mem.get();
			char *buf = (char *)(hdr + 1);
			hdr->mpBasePath = (wchar_t *)(buf + len32Aligned);
			memcpy(hdr->mpBasePath, newBasePath.data(), newBasePath.size() * sizeof(wchar_t));
			hdr->mpBasePath[newBasePath.size()] = 0;

			f.read(buf, len32);

			mem.release();
			*ppData = buf;
			*pBytes = len32;
		} catch(const MyError&) {
			return E_FAIL;
		}

		return S_OK;
	}

	HRESULT STDMETHODCALLTYPE VDDisplayIncludeHandlerD3D9::Close(LPCVOID pData) {
		if (pData) {
			free(((IncludeHeader *)pData) - 1);
		}

		return S_OK;
	}
}

void VDDisplayCustomShaderD3D9::Init(const char *shaderPath8, const VDDisplayCustomShaderProps& propLookup, const vdhashmap<VDStringA, TextureSpec>& customTextureLookup, const TextureSpec *passSpecs, uint32 pass, const wchar_t *basePath, bool& inputUseLinear, uint32& maxPrevFrames) {
	const auto shaderPath = VDTextU8ToW(VDStringSpanA(shaderPath8));
	const auto shaderPrefix = VDFileSplitExtLeftSpan(shaderPath);

	mPassIndex = pass;

	// attempt to read in the vertex and pixel shaders
	HMODULE hmod = nullptr;
	typedef HRESULT (WINAPI *tpD3DCompileFromFile)(LPCWSTR, const D3D10_SHADER_MACRO *, ID3D10Include *, LPCSTR, LPCSTR, UINT, UINT, ID3D10Blob **, ID3D10Blob **);
	tpD3DCompileFromFile pD3DCompileFromFile = nullptr;

	vdfastvector<uint32> vsbytecode;
	vdfastvector<uint32> psbytecode;

	const bool shaderPrecompile = propLookup.GetBool(VDStringSpanA("shader_precompile"), false);

	try {
		for(int i=0; i<2; ++i) {
			const char *shaderType = i ? "vertex" : "pixel";
			const auto precompiledPath = VDMakePath(VDStringSpanW(basePath), shaderPrefix + (i ? L"-d3d9.psh" : L"-d3d9.vsh"));
			auto& bytecode = i ? psbytecode : vsbytecode;
			
			// try to load a precompiled shader first
			if (!shaderPrecompile) {
				VDFile pref;
				if (pref.tryOpen(precompiledPath.c_str())) {
					auto size = pref.size();

					if (size > 0x10000)
						throw MyError("Pass %u %s shader '%ls' is too large.", pass + 1, shaderType, precompiledPath.c_str());

					uint32 size4 = (uint32)size >> 2;
					bytecode.resize(size4);
					pref.read(bytecode.data(), size4 << 2);

					// check for malformed shaders
					const uint32 versionBase = i ? 0xFFFF0000 : 0xFFFE0000;
					const uint32 versionLo = versionBase + 0x0101;		// vs/ps_1_1
					const uint32 versionHi = versionBase + 0x0300;		// vs/ps_3_0
					if (!VDD3DXCheckShaderSize(bytecode.data(), size4 * 4) || bytecode[0] < versionLo || bytecode[0] > versionHi)
						throw MyError("Pass %u: invalid precompiled %s shader '%ls'", pass + 1, shaderType, precompiledPath.c_str());

					continue;
				}
			}

			// no dice... try fx specific next
			auto fxPath = VDMakePath(VDStringSpanW(basePath), shaderPrefix + L".fx");
			if (!VDDoesPathExist(fxPath.c_str())) {
				// nope... try original
				fxPath = VDMakePath(VDStringSpanW(basePath), shaderPath);
				if (!VDDoesPathExist(fxPath.c_str()))
					throw MyError("Pass %u %s shader: cannot find '%ls' or a precompiled/HLSL specific version", pass + 1, shaderType, shaderPath.c_str());
			}

			if (!hmod) {
				hmod = VDLoadSystemLibraryW32("d3dcompiler_47.dll");
				if (hmod) {
					pD3DCompileFromFile = (tpD3DCompileFromFile)GetProcAddress(hmod, "D3DCompileFromFile");
				}

				if (!pD3DCompileFromFile)
					throw MyError("Pass %u %s shader: cannot compile '%s' as d3dcompiler_47.dll is not available", pass + 1, shaderType, shaderPath.c_str());
			}

			VDDisplayIncludeHandlerD3D9 includeHandler(VDFileSplitPathLeft(fxPath).c_str());

			const char *profile = propLookup.GetString(VDStringA().sprintf("shader_profile_d3d9_%u", pass));

			if (!profile)
				profile = propLookup.GetString(VDStringSpanA("shader_profile_d3d9"));

			if (profile) {
				if (!i && (!strcmp(profile, "2_a") || !strcmp(profile, "2_b")))
					profile = "2_0";
			} else
				profile = "2_0";

			vdrefptr<ID3D10Blob> outputBlob;
			vdrefptr<ID3D10Blob> errorMessages;
			HRESULT hr = pD3DCompileFromFile(fxPath.c_str(),
				nullptr,
				&includeHandler,
				i ? "main_fragment" : "main_vertex",
				(VDStringA(i ? "ps_" : "vs_") + profile).c_str(),
				D3D10_SHADER_OPTIMIZATION_LEVEL3,		// equivalent to D3DCOMPILE_OPTIMIZATION_LEVEL3
				0,
				~outputBlob,
				~errorMessages);

			if (FAILED(hr) || !outputBlob) {
				if (errorMessages) {
					const char *str = (const char *)errorMessages->GetBufferPointer();
					const size_t len = errorMessages->GetBufferSize();

					VDStringSpanA errors(str, str + len);
					errors = errors.subspan(0, errors.find((char)0));

					VDCharMaskA linebreaks("\r\n");

					VDStringSpanA errorParser = errors;
					while(!errorParser.empty()) {
						auto eolPos = errorParser.find_first_of(linebreaks);

						VDStringSpanA line(errorParser.subspan(0, eolPos));

						if (line.find(" error X") != line.npos)
							throw MyError("Pass %u %s shader compilation failed: %s", pass + 1, shaderType, VDStringA(line).c_str());

						if (eolPos == errorParser.npos)
							break;

						errorParser = errorParser.subspan(eolPos + 1);
					}

					errors = errors.subspan(0, errors.find('\r'));
					errors = errors.subspan(0, errors.find('\n'));

					throw MyError("Pass %u %s shader compilation failed: %s", pass + 1, shaderType, VDStringA(errors).c_str());
				}

				throw MyError("Pass %u %s shader compilation failed: unknown error (%08X)", pass + 1, shaderType, hr);
			}

			const uint32 *blob = (const uint32 *)outputBlob->GetBufferPointer();
			bytecode.assign(blob, blob + (outputBlob->GetBufferSize() >> 2));

			if (shaderPrecompile) {
				VDFile fout(precompiledPath.c_str(), nsVDFile::kWrite | nsVDFile::kDenyAll | nsVDFile::kCreateAlways);

				fout.write(bytecode.data(), (long)(bytecode.size() * sizeof(bytecode[0])));
			}
		}

		if (hmod != nullptr) {
			FreeLibrary(hmod);
			hmod = nullptr;
		}
	} catch(...) {
		FreeLibrary(hmod);
		throw;
	}

	// check for mismatched shaders -- must be both <3.0 or both 3.0
	uint32 vsversion = vsbytecode[0];
	uint32 psversion = psbytecode[0];
	const bool vs3 = vsversion >= 0xFFFE0300;
	const bool ps3 = psversion >= 0xFFFF0300;

	if (vs3 != ps3)
		throw MyError("Pass %u has mismatched shaders -- cannot mix shader model 1/2 shaders with shader model 3 shaders", pass + 1);

	// check if we can actually run the shaders
	const auto& caps = mpD3DMgr->GetCaps();

	if (vsversion > caps.VertexShaderVersion)
		throw MyError("Pass %u requires a vertex shader version greater than supported by graphics device (%08X > %08X)", pass + 1, vsversion, caps.VertexShaderVersion);

	if (psversion > caps.PixelShaderVersion)
		throw MyError("Pass %u requires a pixel shader version greater than supported by graphics device (%08X > %08X)", pass + 1, psversion, caps.PixelShaderVersion);

	IDirect3DDevice9 *const dev = mpD3DMgr->GetDevice();
	HRESULT hr = dev->CreateVertexShader((const DWORD *)vsbytecode.data(), ~mpVertexShader);
	if (hr != D3D_OK)
		throw VDD3D9Exception(hr);

	hr = dev->CreatePixelShader((const DWORD *)psbytecode.data(), ~mpPixelShader);
	if (hr != D3D_OK)
		throw VDD3D9Exception(hr);

	mbVSConstantTableInited = ProcessShader(vsbytecode.data(), (uint32)(vsbytecode.size() * 4), mVertexShaderInfo, customTextureLookup, maxPrevFrames);
	mbPSConstantTableInited = ProcessShader(psbytecode.data(), (uint32)(psbytecode.size() * 4), mPixelShaderInfo, customTextureLookup, maxPrevFrames);

	// init filtering
	bool srcLinear = propLookup.GetBool(VDStringA().sprintf("filter_linear%u", pass), true);

	inputUseLinear = srcLinear;

	for(const auto& texBinding : mTextureBindings) {
		const uint32 refClass = texBinding.mTexRef & kTexRef_ClassMask;
		const uint32 refIndex = texBinding.mTexRef & kTexRef_IndexMask;

		const uint32 clampToBorder = (mpD3DMgr->GetCaps().TextureAddressCaps & D3DPTADDRESSCAPS_BORDER) ? D3DTADDRESS_BORDER : D3DTADDRESS_CLAMP;

		if (refClass == kTexRef_PassInput) {
			const bool refLinear = (refIndex == mPassIndex - 1) ? srcLinear : passSpecs[refIndex].mbLinear;

			const SamplerState newStates[] = {
				{ texBinding.mStage, D3DSAMP_ADDRESSU, clampToBorder },
				{ texBinding.mStage, D3DSAMP_ADDRESSV, clampToBorder },
				{ texBinding.mStage, D3DSAMP_MAGFILTER, refLinear ? (uint32)D3DTEXF_LINEAR : (uint32)D3DTEXF_POINT },
				{ texBinding.mStage, D3DSAMP_MINFILTER, refLinear ? (uint32)D3DTEXF_LINEAR : (uint32)D3DTEXF_POINT },
				{ texBinding.mStage, D3DSAMP_MIPFILTER, D3DTEXF_NONE },
				{ texBinding.mStage, D3DSAMP_MIPMAPLODBIAS, 0 },
				{ texBinding.mStage, D3DSAMP_SRGBTEXTURE, FALSE },
				{ texBinding.mStage, D3DSAMP_BORDERCOLOR, 0 },
			};

			mSamplerStates.insert(mSamplerStates.end(), std::begin(newStates), std::end(newStates));
		} else if (refClass == kTexRef_PrevInput) {
			const bool refLinear = mPassIndex == 0 ? srcLinear : passSpecs[0].mbLinear;

			const SamplerState newStates[] = {
				{ texBinding.mStage, D3DSAMP_ADDRESSU, clampToBorder },
				{ texBinding.mStage, D3DSAMP_ADDRESSV, clampToBorder },
				{ texBinding.mStage, D3DSAMP_MAGFILTER, refLinear ? (uint32)D3DTEXF_LINEAR : (uint32)D3DTEXF_POINT },
				{ texBinding.mStage, D3DSAMP_MINFILTER, refLinear ? (uint32)D3DTEXF_LINEAR : (uint32)D3DTEXF_POINT },
				{ texBinding.mStage, D3DSAMP_MIPFILTER, D3DTEXF_NONE },
				{ texBinding.mStage, D3DSAMP_MIPMAPLODBIAS, 0 },
				{ texBinding.mStage, D3DSAMP_SRGBTEXTURE, FALSE },
				{ texBinding.mStage, D3DSAMP_BORDERCOLOR, 0 },
			};

			mSamplerStates.insert(mSamplerStates.end(), std::begin(newStates), std::end(newStates));

			if (maxPrevFrames <= refIndex)
				maxPrevFrames = refIndex + 1;
		} else if (refClass == kTexRef_Custom) {
			const auto& texInfo = mCustomTextures[refIndex];
			const SamplerState newStates[] = {
				{ texBinding.mStage, D3DSAMP_ADDRESSU, clampToBorder },
				{ texBinding.mStage, D3DSAMP_ADDRESSV, clampToBorder },
				{ texBinding.mStage, D3DSAMP_MAGFILTER, texInfo.mbLinear ? (uint32)D3DTEXF_LINEAR : (uint32)D3DTEXF_POINT },
				{ texBinding.mStage, D3DSAMP_MINFILTER, texInfo.mbLinear ? (uint32)D3DTEXF_LINEAR : (uint32)D3DTEXF_POINT },
				{ texBinding.mStage, D3DSAMP_MIPFILTER, D3DTEXF_NONE },
				{ texBinding.mStage, D3DSAMP_MIPMAPLODBIAS, 0 },
				{ texBinding.mStage, D3DSAMP_SRGBTEXTURE, FALSE },
				{ texBinding.mStage, D3DSAMP_BORDERCOLOR, 0 },
			};

			mSamplerStates.insert(mSamplerStates.end(), std::begin(newStates), std::end(newStates));
		}
	}

	static const char *const kScaleTypePropNames[]={
		"scale_type%u",
		"scale_type_x%u",
		"scale_type_y%u",
	};

	ScaleType scaleTypes[3];
	uint32 scaleTypesFound = 0;

	for(uint32 i=0; i<3; ++i) {
		scaleTypes[i] = kScaleType_Source;

		const char *scaleTypeProp = propLookup.GetString(VDStringA().sprintf(kScaleTypePropNames[i], pass));
		if (scaleTypeProp) {
			VDStringSpanA scaleTypeStr(scaleTypeProp);

			if (scaleTypeStr == "source")
				scaleTypes[i] = kScaleType_Source;
			else if (scaleTypeStr == "viewport")
				scaleTypes[i] = kScaleType_Viewport;
			else if (scaleTypeStr == "absolute")
				scaleTypes[i] = kScaleType_Absolute;
			else
				throw MyError("Pass %u has invalid scale mode: \"%s\"", pass, scaleTypeProp);

			scaleTypesFound |= 1 << i;
		}
	}

	if (scaleTypesFound & 1) {
		mScaleTypeX = scaleTypes[0];
		mScaleTypeY = scaleTypes[0];
	} else {
		mScaleTypeX = scaleTypes[1];
		mScaleTypeY = scaleTypes[2];
	}

	mbHasScalingFactor = (scaleTypesFound != 0);
	mScaleFactorX = 1.0f;
	mScaleFactorY = 1.0f;

	if (mbHasScalingFactor) {
		float scaleFactors[3];
		uint32 scaleFactorsFound = 0;

		static const char *kScaleFactorPropNames[]={
			"scale%u",
			"scale_x%u",
			"scale_y%u",
		};

		for(uint32 i=0; i<3; ++i) {
			const char *scaleProp = propLookup.GetString(VDStringA().sprintf(kScaleFactorPropNames[i], pass));
			float factor = 1.0f;

			if (scaleProp) {
				if (i == 0 && mScaleTypeX != mScaleTypeY)
					throw MyError("Pass %u: can't use a single scale factor with mixed scale types", pass);

				char dummy;
				if (1 != sscanf(scaleProp, "%g%c", &factor, &dummy) || !(factor > 0) || !(factor < 16384.0f))
					throw MyError("Pass %u has invalid scale factor: %s", pass, scaleProp);

				scaleFactorsFound |= 1 << i;
			}

			scaleFactors[i] = factor;
		}

		if (scaleFactorsFound == 0) {
			mScaleTypeX = kScaleType_Source;
			mScaleTypeY = kScaleType_Source;
			mScaleFactorX = 1.0f;
			mScaleFactorY = 1.0f;
		} else if (scaleFactorsFound & 1) {
			mScaleFactorX = scaleFactors[0];
			mScaleFactorY = scaleFactors[0];
		} else {
			if (scaleFactorsFound & 2)
				mScaleFactorX = scaleFactors[1];
			else
				mScaleTypeX = kScaleType_Source;

			if (scaleFactorsFound & 4)
				mScaleFactorY = scaleFactors[2];
			else
				mScaleTypeY = kScaleType_Source;
		}
	}

	const char *frameCountModProp = propLookup.GetString(VDStringA().sprintf("frame_count_mod%u", pass));
	if (frameCountModProp) {
		unsigned mod;
		char dummy;
		if (1 != sscanf(frameCountModProp, "%u%c", &mod, &dummy) || mod == 0)
			throw MyError("Pass %u has invalid frame_count_mod value: %s", frameCountModProp);

		mFrameCountLimit = mod - 1;
	}

	mbFloatFramebuffer = propLookup.GetBool(VDStringA().sprintf("float_framebuffer%u", pass), false);

	if (mbFloatFramebuffer)
		mbHalfFloatFramebuffer = propLookup.GetBool(VDStringA().sprintf("halffloat_framebuffer%u", pass), false);
}

VDDisplayCustomShaderD3D9::TextureSpec VDDisplayCustomShaderD3D9::Run(const vdrect32f *dstRect, const TextureSpec *srcTexSpecs, const TextureSpec *prevTexSpecs, const vdsize32& viewportSize, bool lastStage) {
	const TextureSpec& srcTexSpec = srcTexSpecs[mPassIndex];
	IDirect3DTexture9 *const srcTex = srcTexSpec.mpTexture;
	const vdsize32 srcSize = { (sint32)srcTexSpec.mTexWidth, (sint32)srcTexSpec.mTexHeight };
	const vdrect32f srcRect = { 0.0f, 0.0f, (float)srcTexSpec.mImageWidth / (float)srcTexSpec.mTexWidth, srcTexSpec.mImageHeight / (float)srcTexSpec.mTexHeight };

	IDirect3DDevice9 *const dev = mpD3DMgr->GetDevice();
	HRESULT hr;

	uint32 renderWidth = 1;
	uint32 renderHeight = 1;

	switch(mScaleTypeX) {
		case kScaleType_Source:
			renderWidth = VDRoundToInt((float)srcTexSpec.mImageWidth * mScaleFactorX);
			break;

		case kScaleType_Viewport:
			renderWidth = VDRoundToInt((float)viewportSize.w * mScaleFactorX);
			break;

		case kScaleType_Absolute:
			renderWidth = VDRoundToInt(mScaleFactorX);
			break;
	}

	switch(mScaleTypeY) {
		case kScaleType_Source:
			renderHeight = VDRoundToInt((float)srcTexSpec.mImageHeight * mScaleFactorY);
			break;

		case kScaleType_Viewport:
			renderHeight = VDRoundToInt((float)viewportSize.h * mScaleFactorY);
			break;

		case kScaleType_Absolute:
			renderHeight = VDRoundToInt(mScaleFactorY);
			break;
	}

	if (renderWidth < 1)
		renderWidth = 1;

	if (renderHeight < 1)
		renderHeight = 1;


	if (lastStage || (mPrevSrcWidth != srcSize.w || mPrevSrcHeight != srcSize.h)) {
		vdsaferelease <<= mpOutputSurface;
		vdsaferelease <<= mpOutputTexture;
	}

	bool clearRT = false;
	if (!lastStage && !mpOutputTexture) {
		int w = renderWidth;
		int h = renderHeight;

		if (!mpD3DMgr->AdjustTextureSize(w, h))
			throw VDD3D9Exception(E_FAIL);

		mOutputTexSpec.mTexWidth = w;
		mOutputTexSpec.mTexHeight = h;

		D3DFORMAT fmt = D3DFMT_A8R8G8B8;

		if (mbFloatFramebuffer) {
			if (mbHalfFloatFramebuffer) {
				if (mpD3DMgr->IsTextureFormatAvailable(D3DFMT_A16B16G16R16F))
					fmt = D3DFMT_A16B16G16R16F;
				else if (mpD3DMgr->IsTextureFormatAvailable(D3DFMT_A32B32G32R32F))
					fmt = D3DFMT_A32B32G32R32F;
			} else {
				if (mpD3DMgr->IsTextureFormatAvailable(D3DFMT_A32B32G32R32F))
					fmt = D3DFMT_A32B32G32R32F;
				else if (mpD3DMgr->IsTextureFormatAvailable(D3DFMT_A16B16G16R16F))
					fmt = D3DFMT_A16B16G16R16F;
			}
		}

		hr = dev->CreateTexture(w, h, 1, D3DUSAGE_RENDERTARGET, fmt, D3DPOOL_DEFAULT, ~mpOutputTexture, nullptr);
		if (hr != D3D_OK)
			throw VDD3D9Exception(hr);

		hr = mpOutputTexture->GetSurfaceLevel(0, ~mpOutputSurface);
		if (hr != D3D_OK) {
			vdsaferelease <<= mpOutputTexture;
			throw VDD3D9Exception(hr);
		}

		clearRT = true;
	}

	mPrevSrcWidth = srcSize.w;
	mPrevSrcHeight = srcSize.h;

	mOutputTexSpec.mImageWidth = renderWidth;
	mOutputTexSpec.mImageHeight = renderHeight;
	mOutputTexSpec.mpTexture = mpOutputTexture;
	
	hr = dev->SetVertexShader(mpVertexShader);
	if (hr != D3D_OK)
		throw VDD3D9Exception(hr);

	hr = dev->SetPixelShader(mpPixelShader);
	if (hr != D3D_OK)
		throw VDD3D9Exception(hr);

	UpdateVariables(mVertexShaderInfo.mUploadWindow.data(), vdvector_view<VariableBinding>(mVertexShaderInfo.mVariableBindings.data(), mVertexShaderInfo.mVariableBindings.size()), srcTexSpecs, prevTexSpecs);
	UploadShaderData<const BOOL, &IDirect3DDevice9::SetVertexShaderConstantB>(dev, mVertexShaderInfo.mUploadSpansB, mVertexShaderInfo.mUploadWindow.data());
	UploadShaderData<const int, &IDirect3DDevice9::SetVertexShaderConstantI>(dev, mVertexShaderInfo.mUploadSpansI, mVertexShaderInfo.mUploadWindow.data());
	UploadShaderData<const float, &IDirect3DDevice9::SetVertexShaderConstantF>(dev, mVertexShaderInfo.mUploadSpansF, mVertexShaderInfo.mUploadWindow.data());
	UpdateVariables(mPixelShaderInfo.mUploadWindow.data(), vdvector_view<VariableBinding>(mPixelShaderInfo.mVariableBindings.data(), mPixelShaderInfo.mVariableBindings.size()), srcTexSpecs, prevTexSpecs);
	UploadShaderData<const BOOL, &IDirect3DDevice9::SetPixelShaderConstantB>(dev, mPixelShaderInfo.mUploadSpansB, mPixelShaderInfo.mUploadWindow.data());
	UploadShaderData<const int, &IDirect3DDevice9::SetPixelShaderConstantI>(dev, mPixelShaderInfo.mUploadSpansI, mPixelShaderInfo.mUploadWindow.data());
	UploadShaderData<const float, &IDirect3DDevice9::SetPixelShaderConstantF>(dev, mPixelShaderInfo.mUploadSpansF, mPixelShaderInfo.mUploadWindow.data());

	for(const auto& rs : mRenderStates) {
		hr = dev->SetRenderState((D3DRENDERSTATETYPE)rs.mState, rs.mValue);
		if (hr != D3D_OK)
			throw VDD3D9Exception(hr);
	}

	for(const auto& ss : mSamplerStates) {
		hr = dev->SetSamplerState(ss.mStage, (D3DSAMPLERSTATETYPE)ss.mState, ss.mValue);
		if (hr != D3D_OK)
			throw VDD3D9Exception(hr);
	}

	for(const auto& binding : mTextureBindings) {
		IDirect3DTexture9 *tex = nullptr;

		switch(binding.mTexRef & kTexRef_ClassMask) {
			case kTexRef_PassInput:
				tex = srcTexSpecs[binding.mTexRef - kTexRef_PassInput].mpTexture;
				break;

			case kTexRef_PrevInput:
				tex = prevTexSpecs[binding.mTexRef - kTexRef_PrevInput].mpTexture;
				break;

			case kTexRef_Custom:
				tex = mCustomTextures[binding.mTexRef - kTexRef_Custom].mpTexture;
				break;
		}

		hr = dev->SetTexture(binding.mStage, tex);

		if (hr != D3D_OK)
			throw VDD3D9Exception(hr);
	}

	if (mpOutputSurface) {
		hr = dev->SetRenderTarget(0, mpOutputSurface);
		if (hr != D3D_OK)
			throw VDD3D9Exception(hr);

		if (clearRT) {
			hr = dev->Clear(0, nullptr, D3DCLEAR_TARGET, 0, 0, 0);
			if (hr != D3D_OK)
				throw VDD3D9Exception(hr);
		}
	}

	if (!mpD3DMgr->BeginScene())
		throw VDD3D9Exception(E_FAIL);

	D3DVIEWPORT9 vp;
	hr = dev->GetViewport(&vp);
	if (hr != D3D_OK)
		throw VDD3D9Exception(hr);

	sint32 xcen2 = !dstRect ? (sint32)vp.Width : (sint32)(dstRect->left + dstRect->right);
	sint32 ycen2 = !dstRect ? (sint32)vp.Height : (sint32)(dstRect->top + dstRect->bottom);
	sint32 xoffset = mpOutputTexture ? 0 : (xcen2 - (sint32)renderWidth) >> 1;
	sint32 yoffset = mpOutputTexture ? 0 : (ycen2 - (sint32)renderHeight) >> 1;
	const float xscale = 2.0f / (float)vp.Width;
	const float yscale = -2.0f / (float)vp.Height;

	float x0 = ((float)xoffset - 0.5f) * xscale - 1.0f;
	float y0 = ((float)yoffset - 0.5f) * yscale + 1.0f;
	float x1 = x0 + (float)renderWidth * xscale;
	float y1 = y0 + (float)renderHeight * yscale;

	const float srcu0 = srcRect.left;
	const float srcv0 = srcRect.top;
	const float srcu1 = srcRect.right;
	const float srcv1 = srcRect.bottom;
	const float auxu0 = 0;
	const float auxu1 = 1;
	const float auxv0 = 0;
	const float auxv1 = 1;

	auto *vx = mpD3DMgr->LockVertices(4);
	vd_seh_guard_try {
		vx[0].SetFF2(x0, y0, UINT32_C(0xFFFFFFFF), srcu0, srcv0, auxu0, auxv0);
		vx[1].SetFF2(x0, y1, UINT32_C(0xFFFFFFFF), srcu0, srcv1, auxu0, auxv1);
		vx[2].SetFF2(x1, y0, UINT32_C(0xFFFFFFFF), srcu1, srcv0, auxu1, auxv0);
		vx[3].SetFF2(x1, y1, UINT32_C(0xFFFFFFFF), srcu1, srcv1, auxu1, auxv1);
	} vd_seh_guard_except {
		mpD3DMgr->UnlockVertices();
		throw VDD3D9Exception(E_FAIL);
	}

	mpD3DMgr->UnlockVertices();

	hr = dev->SetStreamSource(0, mpD3DMgr->GetVertexBuffer(), 0, sizeof(nsVDD3D9::Vertex));
	if (hr != D3D_OK)
		throw VDD3D9Exception(hr);

	hr = dev->SetVertexDeclaration(mpD3DMgr->GetVertexDeclaration());
	if (hr != D3D_OK)
		throw VDD3D9Exception(hr);

	hr = mpD3DMgr->DrawArrays(D3DPT_TRIANGLESTRIP, 0, 2);
	if (hr != D3D_OK)
		throw VDD3D9Exception(hr);

	if (mpOutputSurface) {
		if (!mpD3DMgr->EndScene())
			throw VDD3D9Exception(E_FAIL);
	}

	// clear texture stages
	for (int i=0; i<16; ++i) {
		hr = dev->SetTexture(i, nullptr);
		if (hr != D3D_OK)
			throw VDD3D9Exception(hr);
	}

	// bump frame counter
	if (++mFrame > mFrameCountLimit)
		mFrame = 0;

	return mOutputTexSpec;
}

void VDDisplayCustomShaderD3D9::OnPreDeviceReset() {
	vdsaferelease <<= mpOutputSurface;
	vdsaferelease <<= mpOutputTexture;
}

void VDDisplayCustomShaderD3D9::OnPostDeviceReset() {
}

void VDDisplayCustomShaderD3D9::UpdateVariables(void *dst, vdvector_view<VariableBinding> bindings, const TextureSpec *srcTexSpecs, const TextureSpec *prevTexSpecs) {
	float data[16];

	for(const auto& binding : bindings) {
		uint32 validLength = 0;

		const auto& srcTexSpec = binding.mVarIndex >= 0 ? srcTexSpecs[binding.mVarIndex] : prevTexSpecs[-binding.mVarIndex - 1];

		switch(binding.mVariable) {
			case kVariableRef_VideoSize:
				data[0] = (float)srcTexSpec.mImageWidth;
				data[1] = (float)srcTexSpec.mImageHeight;
				validLength = 2;
				break;

			case kVariableRef_TextureSize:
				data[0] = (float)srcTexSpec.mTexWidth;
				data[1] = (float)srcTexSpec.mTexHeight;
				validLength = 2;
				break;

			case kVariableRef_OutputSize:
				data[0] = (float)srcTexSpec.mImageWidth;
				data[1] = (float)srcTexSpec.mImageHeight;
				validLength = 2;
				break;

			case kVariableRef_FrameCount:
				if (binding.mVarIndex < 0) {
					uint32 frame = mFrame;
					uint32 offset = (uint32)-binding.mVarIndex;

					while(offset-- > 0) {
						if (frame == 0)
							frame = mFrameCountLimit;
						else
							--frame;
					}

					data[0] = (float)frame;
				} else
					data[0] = (float)mFrame;
				validLength = 1;
				break;

			case kVariableRef_FrameDirection:
				data[0] = 1;
				validLength = 1;
				break;

			case kVariableRef_ModelViewProj:
				data[ 0] = 1;
				data[ 1] = 0;
				data[ 2] = 0;
				data[ 3] = 0;
				data[ 4] = 0;
				data[ 5] = 1;
				data[ 6] = 0;
				data[ 7] = 0;
				data[ 8] = 0;
				data[ 9] = 0;
				data[10] = 1;
				data[11] = 0;
				data[12] = 0;
				data[13] = 0;
				data[14] = 0;
				data[15] = 1;
				validLength = 16;
				break;
		}

		if (validLength > binding.mLength)
			validLength = binding.mLength;

		switch(binding.mRegisterSet) {
			case kVDD3DXRegisterSet_Float4:
				{
					float *dstf = (float *)((char *)dst + binding.mDstOffset);

					memcpy(dstf, data, validLength * sizeof(float));

					if (validLength < binding.mLength)
						memset(dstf + validLength, 0, (binding.mLength - validLength) * sizeof(float));
				}
				break;

			case kVDD3DXRegisterSet_Int4:
				{
					int *dsti = (int *)((char *)dst + binding.mDstOffset);;

					for(uint32 i=0; i<validLength; ++i)
						dsti[i] = (int)data[i];

					if (validLength < binding.mLength)
						memset(dsti + validLength, 0, (binding.mLength - validLength) * sizeof(int));
				}
				break;

			case kVDD3DXRegisterSet_Bool:
				{
					BOOL *dstb = (BOOL *)((char *)dst + binding.mDstOffset);;

					for(uint32 i=0; i<validLength; ++i)
						dstb[i] = data[i] != 0 ? (BOOL)-1 : (BOOL)0;

					if (validLength < binding.mLength)
						memset(dstb + validLength, 0, (binding.mLength - validLength) * sizeof(BOOL));
				}
				break;
		}
	}
}

template<class T, HRESULT (__stdcall IDirect3DDevice9::*T_UploadFn)(UINT, T *, UINT)>
void VDDisplayCustomShaderD3D9::UploadShaderData(IDirect3DDevice9 *dev, const vdfastvector<UploadSpan>& spans, const void *src) {
	for(const auto& span : spans) {
		HRESULT hr = (dev->*T_UploadFn)(span.mStart, (T *)((const char *)src + span.mSrcOffset), span.mCount);

		if (hr != D3D_OK)
			throw VDD3D9Exception(hr);
	}
}

bool VDDisplayCustomShaderD3D9::ProcessShader(const uint32 *shader, uint32 shaderSize, ShaderInfo& shaderInfo, const vdhashmap<VDStringA, TextureSpec>& customTextureLookup, uint32& maxPrevFrames) {
	VDD3DXConstantTable ct;

	if (!VDD3DXGetShaderConstantTable(shader, shaderSize, ct))
		return false;

	const uint32 numParams = ct.GetParameterCount();
	uint32 totalUploadSize = 0;

	for(uint32 i=0; i<numParams; ++i) {
		const auto param = ct.GetParameter(i);
		uint32_t rc = param.GetRegisterCount();

		switch(param.GetRegisterSet()) {
			case kVDD3DXRegisterSet_Bool:
				shaderInfo.mUploadSpansB.push_back({ param.GetRegisterIndex(), rc, totalUploadSize * 4 });
				break;

			case kVDD3DXRegisterSet_Float4:
				shaderInfo.mUploadSpansF.push_back({ param.GetRegisterIndex(), rc, totalUploadSize * 4 });
				rc <<= 2;
				break;

			case kVDD3DXRegisterSet_Int4:
				shaderInfo.mUploadSpansI.push_back({ param.GetRegisterIndex(), rc, totalUploadSize * 4 });
				rc <<= 2;
				break;

			case kVDD3DXRegisterSet_Sampler:
				rc = 0;

				if (param.GetRegisterIndex() == 0)
					mTextureBindings.push_back({ param.GetRegisterIndex(), (TexRef)(kTexRef_PassInput + mPassIndex) });
				else {
					VDStringSpanA name(param.GetName());
					
					if (name == "$ORIG_texture")
						mTextureBindings.push_back({ param.GetRegisterIndex(), kTexRef_PassInput });
					else if (name.subspan(0,5) == "$PASS") {
						unsigned index;
						char dummy;

						if (1 != sscanf(VDStringA(name.subspan(5)).c_str(), "%u_texture%c", &index, &dummy) || index == 0 || mPassIndex < 1 || index > mPassIndex - 1)
							throw MyError("Invalid sampler reference from pass %u to %s", mPassIndex + 1, param.GetName());

						mTextureBindings.push_back({ param.GetRegisterIndex(), (TexRef)(kTexRef_PassInput + index)});
					} else if (name == "$PREV_texture") {
						mTextureBindings.push_back({ param.GetRegisterIndex(), kTexRef_PrevInput});
					} else if (name.subspan(0,5) == "$PREV") {
						unsigned index = 0;
						char dummy;

						if (1 != sscanf(VDStringA(name.subspan(5)).c_str(), "%u_texture%c", &index, &dummy) || index == 0 || index > 6)
							throw MyError("Invalid sampler reference from pass %u to %s", mPassIndex + 1, param.GetName());

						mTextureBindings.push_back({ param.GetRegisterIndex(), (TexRef)(kTexRef_PrevInput + index)});
					} else {
						auto itCustom = customTextureLookup.find_as(VDStringSpanA(param.GetName()));

						if (itCustom != customTextureLookup.end()) {
							IDirect3DTexture9 *tex = itCustom->second.mpTexture;

							auto itTexReg = std::find_if(mCustomTextures.begin(), mCustomTextures.end(), [tex](const TextureSpec& ts) { return ts.mpTexture == tex; });

							mTextureBindings.push_back({ param.GetRegisterIndex(), (TexRef)(kTexRef_Custom + (uint32)(itTexReg - mCustomTextures.end())) });

							if (itTexReg == mCustomTextures.end())
								mCustomTextures.push_back(itCustom->second);
						} else {
							mTextureBindings.push_back({ param.GetRegisterIndex(), kTexRef_None });
						}
					}
				}
				break;

			default:
				rc = 0;
				break;
		}

		totalUploadSize += rc;
	}

	shaderInfo.mUploadWindow.resize(totalUploadSize, 0);

	// loop over the params again and copy default parameter data
	uint32 uploadOffset = 0;
	for(uint32 i=0; i<numParams; ++i) {
		const auto param = ct.GetParameter(i);
		uint32_t rc = param.GetRegisterCount();

		switch(param.GetRegisterSet()) {
			case kVDD3DXRegisterSet_Bool:
				break;

			case kVDD3DXRegisterSet_Float4:
			case kVDD3DXRegisterSet_Int4:
				rc <<= 2;
				break;

			default:
				rc = 0;
				break;
		}

		// Scan parameters for variables we can bind to. Note that we explicitly ignore the
		// register set type as it's valid to bind to any type or even double-bind a variable,
		// i.e. IN.frame_count -> bool and float. One way this happens is due to a mixed type
		// struct.
		VDStringSpanA name(param.GetName());

		if ((param.GetParamClass() == kVDD3DXParameterClass_Matrix_ColumnMajor || param.GetParamClass() == kVDD3DXParameterClass_Matrix_RowMajor) && name == "$modelViewProj") {
			VariableBinding vbinding;
			vbinding.mRegisterSet = param.GetRegisterSet();
			vbinding.mDstOffset = uploadOffset * 4;
			vbinding.mLength = param.GetRegisterCount();

			if (vbinding.mRegisterSet != kVDD3DXRegisterSet_Bool)
				vbinding.mLength <<= 2;

			vbinding.mVariable = kVariableRef_ModelViewProj;
			shaderInfo.mVariableBindings.push_back(vbinding);
		} else if (param.GetParamClass() == kVDD3DXParameterClass_Struct && (name == "$IN" || name == "$ORIG" || name.subspan(0, 5) == "$PASS" || name.subspan(0, 5) == "$PREV")) {
			// iterate over sub-struct fields
			auto members = param.GetMembers();
			
			VariableBinding vbinding;
			if (name == "$IN") {
				vbinding.mVarIndex = mPassIndex;
			} else if (name == "$ORIG") {
				vbinding.mVarIndex = 0;
			} else if (name.subspan(0, 5) == "$PREV") {
				unsigned prevIndex = 0;

				if (name.size() > 5) {
					char dummy;
					if (1 != sscanf(VDStringA(name.subspan(5)).c_str(), "%u%c", &prevIndex, &dummy) || prevIndex < 1 || prevIndex > 6)
						throw MyError("Invalid reference from pass %u to parameter '%s'", mPassIndex + 1, VDStringA(name).c_str());
				}

				vbinding.mVarIndex = -((sint32)prevIndex + 1);

				if (maxPrevFrames <= prevIndex)
					maxPrevFrames = prevIndex+1;
			} else {
				unsigned passRefIndex;
				char dummy;
				if (1 != sscanf(VDStringA(name.subspan(5)).c_str(), "%u%c", &passRefIndex, &dummy) || passRefIndex == 0 || mPassIndex < 2 || passRefIndex > mPassIndex - 2)
					throw MyError("Invalid reference from pass %u to parameter '%s'", mPassIndex + 1, VDStringA(name).c_str());

				vbinding.mVarIndex = passRefIndex;
			}

			VDD3DXCTParameter member;
			while(members.Next(member)) {
				const auto memberClass = member.GetParamClass();

				if (memberClass != kVDD3DXParameterClass_Scalar
					&& memberClass != kVDD3DXParameterClass_Vector
					&& memberClass != kVDD3DXParameterClass_Matrix_ColumnMajor
					&& memberClass != kVDD3DXParameterClass_Matrix_RowMajor)
					continue;

				const auto memberType = member.GetParamType();
				if (memberType != kVDD3DXParameterType_Bool
					&& memberType != kVDD3DXParameterType_Float
					&& memberType != kVDD3DXParameterType_Int)
					continue;

				VDStringSpanA memberName(member.GetName());
				vbinding.mRegisterSet = member.GetRegisterSet();
				vbinding.mDstOffset = 4 * (member.GetRegisterIndex() - param.GetRegisterIndex());
				vbinding.mLength = member.GetRegisterCount();

				if (vbinding.mRegisterSet != kVDD3DXRegisterSet_Bool) {
					vbinding.mDstOffset <<= 2;
					vbinding.mLength <<= 2;
				}

				vbinding.mDstOffset += uploadOffset;

				if (memberName == "video_size") {
					vbinding.mVariable = kVariableRef_VideoSize;
					shaderInfo.mVariableBindings.push_back(vbinding);
				} else if (memberName == "texture_size") {
					vbinding.mVariable = kVariableRef_TextureSize;
					shaderInfo.mVariableBindings.push_back(vbinding);
				} else if (memberName == "output_size") {
					vbinding.mVariable = kVariableRef_OutputSize;
					shaderInfo.mVariableBindings.push_back(vbinding);
				} else if (memberName == "frame_count") {
					vbinding.mVariable = kVariableRef_FrameCount;
					shaderInfo.mVariableBindings.push_back(vbinding);
				} else if (memberName == "frame_direction") {
					vbinding.mVariable = kVariableRef_FrameDirection;
					shaderInfo.mVariableBindings.push_back(vbinding);
				}
			}
		}

		if (rc) {
			const void *src = param.GetDefaultValue();

			if (src)
				memcpy(&shaderInfo.mUploadWindow[uploadOffset], src, rc * 4);
		}

		uploadOffset += rc;
	}

	return true;
}

void VDDisplayCustomShaderD3D9::WriteTransposedMatrix(float *dst, const vdfloat4x4& src, uint32 n) {
	if (n >= 4) {
		dst[ 0] = src.x.x;
		dst[ 1] = src.y.x;
		dst[ 2] = src.z.x;
		dst[ 3] = src.w.x;
		dst[ 4] = src.x.y;
		dst[ 5] = src.y.y;
		dst[ 6] = src.z.y;
		dst[ 7] = src.w.y;
		dst[ 8] = src.x.z;
		dst[ 9] = src.y.z;
		dst[10] = src.z.z;
		dst[11] = src.w.z;
		dst[12] = src.x.w;
		dst[13] = src.y.w;
		dst[14] = src.z.w;
		dst[15] = src.w.w;
	} else {
		switch(n) {
			case 3:
				dst[ 8] = src.x.z;
				dst[ 9] = src.y.z;
				dst[10] = src.z.z;
				dst[11] = src.w.z;
			case 2:
				dst[ 4] = src.x.y;
				dst[ 5] = src.y.y;
				dst[ 6] = src.z.y;
				dst[ 7] = src.w.y;
			case 1:
				dst[ 0] = src.x.x;
				dst[ 1] = src.y.x;
				dst[ 2] = src.z.x;
				dst[ 3] = src.w.x;
				break;
		}
	}
}

///////////////////////////////////////////////////////////////////////////

class VDDisplayCustomShaderPipelineD3D9 final : public IVDDisplayCustomShaderPipelineD3D9, public VDD3D9Client {
	VDDisplayCustomShaderPipelineD3D9(const VDDisplayCustomShaderPipelineD3D9&) = delete;
	VDDisplayCustomShaderPipelineD3D9& operator=(const VDDisplayCustomShaderPipelineD3D9&) = delete;
public:
	VDDisplayCustomShaderPipelineD3D9(VDD3D9Manager *d3d9mgr);
	~VDDisplayCustomShaderPipelineD3D9();

	void Parse(const wchar_t *path);

	bool ContainsFinalBlit() const override;
	uint32 GetMaxPrevFrames() const override { return mMaxPrevFrames; }
	bool HasTimingInfo() const override { return !mPassInfos.empty(); }
	const VDDisplayCustomShaderPassInfo *GetPassTimings(uint32& numPasses) override;

	void Run(IDirect3DTexture9 *const *srcTextures, const vdsize32& texSize, const vdsize32& imageSize, const vdsize32& viewportSize) override;
	void RunFinal(const vdrect32f& dstRect, const vdsize32& viewportSize) override;

	IDirect3DTexture9 *GetFinalOutput(uint32& imageWidth, uint32& imageHeight) override;

public:
	void OnPreDeviceReset() override;
	void OnPostDeviceReset() override;

private:
	void CreateQueries();
	void DestroyQueries();
	void ParsePropertyListFile(VDDisplayCustomShaderProps& props, const wchar_t *path);
	void ParseTextures(const VDDisplayCustomShaderProps& props, const wchar_t *path);
	void LoadTexture(const char *name, const wchar_t *path, bool linear);

	VDD3D9Manager *const mpManager;
	uint32 mMaxPrevFrames = 0;

	vdfastvector<VDDisplayCustomShaderD3D9 *> mPasses;
	vdfastvector<VDDisplayCustomShaderD3D9::TextureSpec> mInputTexSpecs;
	vdfastvector<VDDisplayCustomShaderD3D9::TextureSpec> mPrevInputTexSpecs;

	vdhashmap<VDStringA, VDDisplayCustomShaderD3D9::TextureSpec> mCustomTextures;

	vdfastvector<VDDisplayCustomShaderPassInfo> mPassInfos;
	vdfastvector<IDirect3DQuery9 *> mpTimingQueries;
	vdfastvector<UINT64> mTimingValues;
	uint32 mNextTimingValueIssue = 0;
	uint32 mNextTimingValueQuery = 0;
	bool mbIssueQueries = false;
	bool mbQueryQueries = false;
	float mTicksToSeconds = 0;
};

VDDisplayCustomShaderPipelineD3D9::VDDisplayCustomShaderPipelineD3D9(VDD3D9Manager *d3d9mgr)
	: mpManager(d3d9mgr)
{
	mpManager->Attach(this);
}

VDDisplayCustomShaderPipelineD3D9::~VDDisplayCustomShaderPipelineD3D9() {
	DestroyQueries();

	for(auto& tex : mCustomTextures) {
		vdsaferelease <<= tex.second.mpTexture;
	}

	mCustomTextures.clear();

	while(!mPasses.empty()) {
		auto *p = mPasses.back();
		mPasses.pop_back();

		delete p;
	}

	mpManager->Detach(this);
}

void VDDisplayCustomShaderPipelineD3D9::Parse(const wchar_t *path) {
	VDDisplayCustomShaderProps props;

	ParsePropertyListFile(props, path);

	const VDStringW basePath = VDFileSplitPathLeftSpan(VDStringSpanW(path));
	ParseTextures(props, basePath.c_str());

	mMaxPrevFrames = 0;
	for(uint32 passIndex = 0; ; ++passIndex) {
		const char *shaderPath = props.GetString(VDStringA().sprintf("shader%u", (unsigned)passIndex));
		if (!shaderPath) {
			if (passIndex == 0)
				throw MyError("Custom shader pipeline contains no passes.");

			break;
		}

		mPasses.push_back(nullptr);
		auto *pass = new VDDisplayCustomShaderD3D9(mpManager);
		mPasses.back() = pass;
		mInputTexSpecs.push_back();

		pass->Init(shaderPath, props, mCustomTextures, mInputTexSpecs.data(), passIndex, basePath.c_str(), mInputTexSpecs.back().mbLinear, mMaxPrevFrames);
	}

	const uint32 passCount = (uint32)mPasses.size();
	const bool containsFinalBlit = !mPasses.empty() && mPasses.back()->HasScalingFactor();

	mInputTexSpecs.resize(mPasses.size() + (containsFinalBlit ? 0 : 1));

	mPrevInputTexSpecs.resize(mMaxPrevFrames, { nullptr, 1, 1, 1, 1, true });

	bool profile = props.GetBool(VDStringSpanA("shader_show_stats"), false);

	if (profile) {
		IDirect3DDevice9 *dev = mpManager->GetDevice();
		HRESULT hr = dev->CreateQuery(D3DQUERYTYPE_TIMESTAMP, NULL);

		if (hr == D3D_OK) {
			vdrefptr<IDirect3DQuery9> q;
			hr = dev->CreateQuery(D3DQUERYTYPE_TIMESTAMPFREQ, ~q);
			if (hr == D3D_OK) {
				hr = q->Issue(D3DISSUE_END);

				if (hr == D3D_OK) {
					UINT64 freq = 0;

					for(;;) {
						hr = q->GetData(&freq, sizeof freq, D3DGETDATA_FLUSH);
						if (hr != S_FALSE)
							break;

						Sleep(1);
					}

					if (hr == S_OK && freq > 0) {
						mPassInfos.resize(passCount + 1, {});
						mpTimingQueries.resize(passCount + 1, nullptr);
						mTimingValues.resize(passCount + 1, 0);

						mTicksToSeconds = 1.0f / (float)freq;

						CreateQueries();

						mbIssueQueries = true;
					}
				}
			}
		}
	}
}

const VDDisplayCustomShaderPassInfo *VDDisplayCustomShaderPipelineD3D9::GetPassTimings(uint32& numPasses) {
	const uint32 n = (uint32)mPassInfos.size();
	numPasses = n;

	if (!numPasses)
		return nullptr;

	if (mbQueryQueries) {
		while(mNextTimingValueQuery < n) {
			IDirect3DQuery9 *q = mpTimingQueries[mNextTimingValueQuery];

			if (!q)
				break;

			UINT64 t = 0;
			HRESULT hr = q->GetData(&t, sizeof t, 0);
			if (hr != S_OK)
				break;

			mTimingValues[mNextTimingValueQuery++] = t;
		}

		if (mNextTimingValueQuery >= n) {
			for(uint32 i=1; i<n; ++i)
				mPassInfos[i - 1].mTiming = (float)(mTimingValues[i] - mTimingValues[i - 1]) * mTicksToSeconds;

			mPassInfos[n - 1].mTiming = (float)(mTimingValues[n - 1] - mTimingValues[0]) * mTicksToSeconds;

			mNextTimingValueIssue = 0;
			mNextTimingValueQuery = 0;
			mbQueryQueries = false;
			mbIssueQueries = true;
		}
	}

	for(uint32 i=0; i<n - 1; ++i) {
		auto& info = mPassInfos[i];
		const auto& texInfo = i + 1 >= mInputTexSpecs.size() ? mInputTexSpecs.back() : mInputTexSpecs[i + 1];

		info.mOutputWidth = texInfo.mImageWidth;
		info.mOutputHeight = texInfo.mImageHeight;
		info.mbOutputFloat = mPasses[i]->IsOutputFloat();
		info.mbOutputHalfFloat = mPasses[i]->IsOutputHalfFloat();
	}

	return mPassInfos.data();
}

bool VDDisplayCustomShaderPipelineD3D9::ContainsFinalBlit() const {
	return !mPasses.empty() && mPasses.back()->HasScalingFactor();
}

void VDDisplayCustomShaderPipelineD3D9::Run(IDirect3DTexture9 *const *srcTextures, const vdsize32& texSize, const vdsize32& imageSize, const vdsize32& viewportSize) {
	if (!mPasses.empty()) {
		VDDisplayCustomShaderD3D9::TextureSpec srcTexSpec = { *srcTextures++, (uint32)texSize.w, (uint32)texSize.h, (uint32)imageSize.w, (uint32)imageSize.h };
		mInputTexSpecs.front() = srcTexSpec;

		for(auto& prevTexSpec : mPrevInputTexSpecs) {
			srcTexSpec.mpTexture = *srcTextures++;
			prevTexSpec = srcTexSpec;
		}

		auto it = mPasses.begin();
		auto itEnd = mPasses.end();

		if (mPasses.back()->HasScalingFactor())
			--itEnd;

		auto itSpec = mInputTexSpecs.begin();

		uint32 passIndex = 0;
		for(; it != itEnd; ++it, ++itSpec, ++passIndex) {
			VDDisplayCustomShaderD3D9 *pass = *it;

			if (mbIssueQueries && mNextTimingValueIssue == passIndex) {
				HRESULT hr = mpTimingQueries[passIndex]->Issue(D3DISSUE_END);
				if (hr != D3D_OK)
					throw VDD3D9Exception(hr);

				++mNextTimingValueIssue;
			}

			const bool useLinearSave = itSpec[1].mbLinear;
			itSpec[1] = pass->Run(nullptr, mInputTexSpecs.data(), mPrevInputTexSpecs.data(), viewportSize, false);
			itSpec[1].mbLinear = useLinearSave;
		}

		if (mbIssueQueries && !ContainsFinalBlit() && mNextTimingValueIssue == passIndex) {
			HRESULT hr = mpTimingQueries[passIndex]->Issue(D3DISSUE_END);
			if (hr != D3D_OK)
				throw VDD3D9Exception(hr);

			++mNextTimingValueIssue;
			mbIssueQueries = false;
			mbQueryQueries = true;
		}
	}
}

void VDDisplayCustomShaderPipelineD3D9::RunFinal(const vdrect32f& dstRect, const vdsize32& viewportSize) {
	if (!mPasses.empty() && ContainsFinalBlit()) {
		uint32 passIndex = (uint32)mPasses.size() - 1;
		if (mbIssueQueries && mNextTimingValueIssue == passIndex) {
			HRESULT hr = mpTimingQueries[passIndex]->Issue(D3DISSUE_END);
			if (hr != D3D_OK)
				throw VDD3D9Exception(hr);

			++mNextTimingValueIssue;
		}

		mPasses.back()->Run(&dstRect, mInputTexSpecs.data(), mPrevInputTexSpecs.data(), viewportSize, true);

		++passIndex;
		if (mbIssueQueries && mNextTimingValueIssue == passIndex) {
			HRESULT hr = mpTimingQueries[passIndex]->Issue(D3DISSUE_END);
			if (hr != D3D_OK)
				throw VDD3D9Exception(hr);

			++mNextTimingValueIssue;
			mbIssueQueries = false;
			mbQueryQueries = true;
		}
	}
}

IDirect3DTexture9 *VDDisplayCustomShaderPipelineD3D9::GetFinalOutput(uint32& imageWidth, uint32& imageHeight) {
	const auto& result = mInputTexSpecs.back();

	imageWidth = result.mImageWidth;
	imageHeight = result.mImageHeight;

	return result.mpTexture;
}

void VDDisplayCustomShaderPipelineD3D9::OnPreDeviceReset() {
	DestroyQueries();

	for(auto& spec : mInputTexSpecs)
		spec.mpTexture = nullptr;

	for(auto& spec : mPrevInputTexSpecs)
		spec.mpTexture = nullptr;
}

void VDDisplayCustomShaderPipelineD3D9::OnPostDeviceReset() {
	CreateQueries();
}

void VDDisplayCustomShaderPipelineD3D9::CreateQueries() {
	IDirect3DDevice9 *dev = mpManager->GetDevice();

	for(IDirect3DQuery9 *& p : mpTimingQueries) {
		if (!p) {
			HRESULT hr = dev->CreateQuery(D3DQUERYTYPE_TIMESTAMP, &p);

			if (hr != D3D_OK)
				throw VDD3D9Exception(hr);
		}
	}
}

void VDDisplayCustomShaderPipelineD3D9::DestroyQueries() {
	uint32 index = 0;

	for(auto *&ptr : mpTimingQueries) {
		if (ptr && index < mNextTimingValueIssue) {
			for(;;) {
				HRESULT hr = ptr->GetData(nullptr, 0, D3DGETDATA_FLUSH);

				if (hr != S_FALSE)
					break;
			}
		}

		vdsaferelease <<= ptr;

		++index;
	}
}

void VDDisplayCustomShaderPipelineD3D9::ParsePropertyListFile(VDDisplayCustomShaderProps& props, const wchar_t *path) {
	VDTextInputFile tf(path);
	uint32 lineno = 0;

	const VDCharMaskA whitespace(" \r\n\t\v");
	const VDCharMaskA hashOrEqual("#=");
	const VDCharMaskA whitespaceOrHash(" \r\n\t\v#");

	for(;;) {
		++lineno;

		const char *s = tf.GetNextLine();
		if (!s)
			break;

		VDStringRefA line(s);

		line = line.trim(whitespace);
		if (line.empty() || line[0] == '#')
			continue;

		auto eqpos = line.find_first_of(hashOrEqual);

		if (eqpos == line.npos || line[eqpos] == '#')
			throw VDFileParseException(lineno, "expected '=' after key");

		VDStringSpanA key(line.subspan(0, eqpos).trim(whitespace));
		if (key.empty())
			throw VDFileParseException(lineno, "expected key");

		line = line.subspan(eqpos + 1).trim_start(whitespace);

		if (line.empty() || line[0] == '#')
			throw VDFileParseException(lineno, "expected value");

		VDStringSpanA value;
		if (line[0] == '"') {
			auto quotePos = line.find('"', 1);

			if (quotePos == line.npos)
				throw VDFileParseException(lineno, "missing '\"' at end of value string");

			value = line.subspan(1, quotePos - 1);

			// check for garbage afterward
			if (line.find_first_not_of(whitespaceOrHash, quotePos + 1) != line.npos)
				throw VDFileParseException(lineno, "expected end of line");
		} else {
			value = line.subspan(0, line.find('#')).trim(whitespace);
		}

		if (!props.Add(key, value))
			throw VDFileParseException(lineno, "duplicate key");
	}
}

void VDDisplayCustomShaderPipelineD3D9::ParseTextures(const VDDisplayCustomShaderProps& props, const wchar_t *basePath) {
	const char *texturesProp = props.GetString(VDStringSpanA("textures"));
	if (!texturesProp)
		return;

	VDStringRefA texturesList(texturesProp);
	const VDCharMaskA whitespace(" \r\n\t\v");

	while(!texturesList.empty()) {
		VDStringRefA textureName;
		if (!texturesList.split(';', textureName)) {
			textureName = texturesList;
			texturesList.clear();
		}

		textureName = textureName.trim(whitespace);
		if (textureName.empty())
			continue;

		const char *texPath = props.GetString(textureName);
		if (!texPath)
			throw MyError("No path specified for texture: %s", VDStringA(textureName).c_str());

		bool linear = props.GetBool(VDStringA().sprintf("%s_linear", VDStringA(textureName).c_str()), true);

		LoadTexture(VDStringA(textureName).c_str(), VDMakePath(VDStringSpanW(basePath), VDTextU8ToW(VDStringSpanA(texPath))).c_str(), linear);
	}
}

void VDDisplayCustomShaderPipelineD3D9::LoadTexture(const char *name, const wchar_t *path, bool linear) {
	auto insertResult = mCustomTextures.insert_as(VDStringSpanA(name));
	if (!insertResult.second)
		return;

	VDFileStream fs(path);
	VDBufferedStream bs(&fs, 4096);

	// read TARGA header
	uint8 header[18];
	bs.Read(header, 18);

	// check if it might actually be a PNG
	static constexpr uint8 kPNGHeader[8]={
		137, 80, 78, 71, 13, 10, 26, 10
	};

	IDirect3DDevice9 *dev = mpManager->GetDevice();
	vdrefptr<IDirect3DTexture9> tex;
	uint32 width = 0;
	uint32 height = 0;
	HRESULT hr;
	if (g_pVDDisplayImageDecoder && !memcmp(header, kPNGHeader, 8)) {
		VDPixmapBuffer buf;
		bs.Seek(0);
		if (!g_pVDDisplayImageDecoder->DecodeImage(buf, bs))
			throw MyError("Unsupported image format: %s", name);

		width = buf.w;
		height = buf.h;

		hr = dev->CreateTexture(width, height, 1, 0, D3DFMT_A8R8G8B8, mpManager->IsD3D9ExEnabled() ? D3DPOOL_SYSTEMMEM : D3DPOOL_MANAGED, ~tex, nullptr);
		if (hr != D3D_OK)
			throw VDD3D9Exception(hr);

		D3DLOCKED_RECT lr;
		hr = tex->LockRect(0, &lr, nullptr, 0);
		if (hr != D3D_OK)
			throw VDD3D9Exception(hr);

		VDPixmap pxdst {};
		pxdst.format = nsVDPixmap::kPixFormat_XRGB8888;
		pxdst.w = width;
		pxdst.h = height;
		pxdst.pitch = lr.Pitch;
		pxdst.data = lr.pBits;
		VDPixmapBlt(pxdst, buf);

		tex->UnlockRect(0);
	} else {
		// load as TARGA
		const uint8 idSize = header[0];
		const uint8 dataType = header[2];
		const uint32 width = VDReadUnalignedLEU16(&header[12]);
		const uint32 height = VDReadUnalignedLEU16(&header[14]);
		const uint8 imagePixelSize = header[16];
		const uint8 imageAlphaBits = header[17] & 15;

		if (!width || !height || width > 16384 || height > 16384 || (width & (width - 1)) || (height & (height - 1)))
			throw MyError("Unsupported TARGA image size: %ux%u", width, height);

		uint32 bpp;
		if ((dataType == 2 || dataType == 10) && imagePixelSize == 24 && imageAlphaBits == 0) {
			bpp = 3;
		} else if ((dataType == 2 || dataType == 10) && imagePixelSize == 32 && imageAlphaBits == 0) {
			bpp = 4;
		} else if ((dataType == 2 || dataType == 10) && imagePixelSize == 32 && imageAlphaBits == 8) {
			bpp = 4;
		} else
			throw MyError("TARGA image must be 24-bit or 32-bit.");

		// read image raw buffer
		vdblock<uint8> rawBuffer;
	
		if (dataType == 10)
			rawBuffer.resize(std::min<uint32>(bpp * width * height * 2, (uint32)(fs.size() - (18 + idSize))));
		else
			rawBuffer.resize(bpp * width * height);

		bs.Seek(18 + idSize);
		bs.Read(rawBuffer.data(), (sint32)rawBuffer.size());

		// read/decompress image
		class DecompressionException : public MyError {
		public:
			DecompressionException() : MyError("TARGA decompression error.") {}
		};

		if (dataType == 10) {
			vdblock<uint8> decompBuffer(bpp * width * height);

			const uint8 *src = rawBuffer.data();
			const uint8 *srcEnd = src + rawBuffer.size();
			uint8 *dst = decompBuffer.data();
			uint8 *dstEnd = dst + decompBuffer.size();

			while(dstEnd != dst) {
				if ((size_t)(srcEnd - src) < bpp + 1)
					throw DecompressionException();

				const uint8 code = *src++;
				const uint32 rawcnt = (uint32)(code & 0x7F) + 1;
				const uint32 rawlen = rawcnt * bpp;
				if ((size_t)(dstEnd - dst) < rawlen)
					throw DecompressionException();

				if (code & 0x80) {
					uint8 c0 = *src++;
					uint8 c1 = *src++;
					uint8 c2 = *src++;
					if (bpp == 3) {
						for(uint32 i=0; i<rawcnt; ++i) {
							*dst++ = c0;
							*dst++ = c1;
							*dst++ = c2;
						}
					} else {
						uint8 c3 = *src++;

						for(uint32 i=0; i<rawcnt; ++i) {
							*dst++ = c0;
							*dst++ = c1;
							*dst++ = c2;
							*dst++ = c3;
						}
					}
				} else {
					if ((size_t)(srcEnd - src) < rawlen)
						throw DecompressionException();

					memcpy(dst, src, rawlen);
					dst += rawlen;
					src += rawlen;
				}
			}

			rawBuffer.swap(decompBuffer);
		}

		// allocate texture
		hr = dev->CreateTexture(width, height, 1, 0, D3DFMT_A8R8G8B8, mpManager->IsD3D9ExEnabled() ? D3DPOOL_SYSTEMMEM : D3DPOOL_MANAGED, ~tex, nullptr);
		if (hr != D3D_OK)
			throw VDD3D9Exception(hr);

		D3DLOCKED_RECT lr;
		hr = tex->LockRect(0, &lr, nullptr, 0);
		if (hr != D3D_OK)
			throw VDD3D9Exception(hr);

		const void *src0 = rawBuffer.data();
		ptrdiff_t srcModulo = 0;

		if (!(header[17] & 0x20)) {
			src0 = (const char *)src0 + width * bpp * (height - 1);
			srcModulo = 0 - 2*width*bpp;
		}

		try {
			if (bpp == 3) {
				const uint8 *src = (const uint8 *)src0;
				uint8 *dstRow = (uint8 *)lr.pBits;

				for(uint32 y=0; y<height; ++y) {
					uint8 *dst = dstRow;
					dstRow += lr.Pitch;

					for(uint32 x=0; x<width; ++x) {
						dst[0] = src[0];
						dst[1] = src[1];
						dst[2] = src[2];
						dst[3] = (uint8)0xFF;
						dst += 4;
						src += 3;
					}

					src += srcModulo;
				}
			} else if (bpp == 4) {
				if (imageAlphaBits == 0) {
					const uint32 *src = (const uint32 *)src0;
					uint32 *dstRow = (uint32 *)lr.pBits;

					for(uint32 y=0; y<height; ++y) {
						uint32 *dst = dstRow;
						dstRow = (uint32 *)((char *)dstRow + lr.Pitch);

						for(uint32 x=0; x<width; ++x) {
							dst[x] = src[x] | UINT32_C(0xFF000000);
						}

						src = (const uint32 *)((const char *)(src + width) + srcModulo);
					}				
				} else
					VDMemcpyRect(lr.pBits, lr.Pitch, rawBuffer.data(), width*4, width*4, height);
			}
		} catch(...) {
			tex->UnlockRect(0);
			throw;
		}

		tex->UnlockRect(0);
	}

	if (mpManager->IsD3D9ExEnabled()) {
		vdrefptr<IDirect3DTexture9> tex2;
		hr = dev->CreateTexture(width, height, 1, 0, D3DFMT_A8R8G8B8, D3DPOOL_DEFAULT, ~tex2, nullptr);
		if (hr != D3D_OK)
			throw VDD3D9Exception(hr);

		hr = dev->UpdateTexture(tex, tex2);
		if (hr != D3D_OK)
			throw VDD3D9Exception(hr);

		tex = std::move(tex2);
	}

	auto& newTex = insertResult.first->second;
	newTex.mpTexture = tex.release();
	newTex.mImageWidth = width;
	newTex.mImageHeight = height;
	newTex.mTexWidth = width;
	newTex.mTexHeight = height;
	newTex.mbLinear = linear;
}

IVDDisplayCustomShaderPipelineD3D9 *VDDisplayParseCustomShaderPipeline(VDD3D9Manager *d3d9mgr, const wchar_t *path) {
	auto p = vdmakeautoptr(new VDDisplayCustomShaderPipelineD3D9(d3d9mgr));

	p->Parse(path);

	return p.release();
}
