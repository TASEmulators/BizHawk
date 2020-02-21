//	Asuka - VirtualDub Build/Post-Mortem Utility
//	Copyright (C) 2005-2012 Avery Lee
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

#include <stdafx.h>
#include <vector>
#include <list>
#include <unordered_map>
#include <string>
#include <windows.h>
#include <d3dcommon.h>
#include <d3d11.h>
#include <d3d10.h>
#include <d3d10shader.h>
#include <vd2/system/binary.h>
#include <vd2/system/refcount.h>
#include <vd2/system/error.h>
#include <vd2/system/file.h>
#include <vd2/system/filesys.h>
#include <vd2/system/hash.h>
#include <vd2/system/math.h>
#include <vd2/system/strutil.h>
#include <vd2/system/vdstl.h>

// TODO: Fix this
#include <vd2/VDDisplay/minid3dx.h>

namespace {
	HMODULE g_hmodD3DCompiler;

	typedef HRESULT (WINAPI *tpD3DCompile)(
		LPCVOID pSrcData,
		SIZE_T SrcDataSize,
		LPCSTR pSourceName,
		const D3D10_SHADER_MACRO *pDefines,
		ID3D10Include *pInclude,
		LPCSTR pEntrypoint,
		LPCSTR pTarget,
		UINT Flags1,
		UINT Flags2,
		ID3D10Blob **ppCode,
		ID3D10Blob **ppErrorMsgs
	);

	typedef HRESULT (WINAPI *tpD3DDisassemble)(
		LPCVOID pSrcData,
		SIZE_T SrcDataSize,
		UINT Flags,
		LPCSTR szComments,
		ID3D10Blob **ppDisassembly
	);

	tpD3DCompile g_pD3DCompile;
	tpD3DDisassemble g_pD3DDisassemble;

	void __cdecl ShutdownD3DCompiler() {
		if (g_hmodD3DCompiler) {
			FreeLibrary(g_hmodD3DCompiler);
			g_hmodD3DCompiler = nullptr;
		}
	}

	void InitD3DCompiler() {
		g_hmodD3DCompiler = LoadLibraryW(L"d3dcompiler_47");

		if (!g_hmodD3DCompiler)
			throw MyWin32Error("Unable to load D3DCompiler_47.dll: %%s", GetLastError());

		try {
			g_pD3DCompile = (tpD3DCompile)GetProcAddress(g_hmodD3DCompiler, "D3DCompile");
			if (!g_pD3DCompile)
				throw MyError("Unable to retrieve entry point: D3DCompiler_47:D3DCompile");

			g_pD3DDisassemble = (tpD3DDisassemble)GetProcAddress(g_hmodD3DCompiler, "D3DDisassemble");
			if (!g_pD3DDisassemble)
				throw MyError("Unable to retrieve entry point: D3DCompiler_47:D3DCompile");
		} catch(...) {
			ShutdownD3DCompiler();
			throw;
		}
	}
}

class FXC10IncludeHandler : public ID3D10Include {
public:
	FXC10IncludeHandler(const char *basePath) : mBasePath(VDTextAToW(basePath)) {}

    HRESULT STDMETHODCALLTYPE Open(D3D10_INCLUDE_TYPE IncludeType, LPCSTR pFileName, LPCVOID pParentData, LPCVOID *ppData, UINT *pBytes);
    HRESULT STDMETHODCALLTYPE Close(LPCVOID pData);

private:
	const VDStringW mBasePath;
};

HRESULT STDMETHODCALLTYPE FXC10IncludeHandler::Open(D3D10_INCLUDE_TYPE IncludeType, LPCSTR pFileName, LPCVOID pParentData, LPCVOID *ppData, UINT *pBytes) {
	try {
		vdblock<char> buf;

		const VDStringW& path = VDFileResolvePath(mBasePath.c_str(), VDTextAToW(pFileName).c_str());

		VDFile f(path.c_str());

		uint32 s = (uint32)f.size();
		buf.resize(s);
		f.read(buf.data(), s);

		void *p = malloc(s);

		memcpy(p, buf.data(), s);

		*ppData = p;
		*pBytes = s;
	} catch(const MyError&) {
		return E_FAIL;
	}

	return S_OK;
}

HRESULT STDMETHODCALLTYPE FXC10IncludeHandler::Close(LPCVOID pData) {
	free((void *)pData);
	return S_OK;
}

///////////////////////////////////////////////////////////////////////////

class ArgumentParser {
public:
	ArgumentParser(const char *str) : mpStr(str) {}

	void SkipSpaces();
	bool TryToken(const char *token);
	bool TryParseUint32(uint32& result);
	VDStringSpanA GetIdentifier();

private:
	const char *mpStr;
};

void ArgumentParser::SkipSpaces() {
	mpStr = strskipspace(mpStr);
}

bool ArgumentParser::TryToken(const char *token) {
	SkipSpaces();

	const char *s = mpStr;

	while(const char c = *token++) {
		if (*s++ != c)
			return false;
	}

	mpStr = s;
	return true;
}

bool ArgumentParser::TryParseUint32(uint32& result) {
	const char *before = mpStr;
	VDStringSpanA id = GetIdentifier();
	const char *after = mpStr;
	mpStr = before;

	uint32 v = 0;
	for(const char c : id) {
		if (c < '0' || c > '9')
			return false;

		if (v > UINT32_MAX / 10)
			return false;

		v *= 10;

		const uint32 digit = (uint32)(c - '0');
		if (UINT32_MAX - v < digit)
			return false;

		v += digit;
	}

	mpStr = after;
	result = v;
	return true;
}

VDStringSpanA ArgumentParser::GetIdentifier() {
	SkipSpaces();

	const char *start = mpStr;

	while(*mpStr && !isspace((unsigned char)*mpStr))
		++mpStr;

	return VDStringSpanA(start, mpStr);
}

///////////////////////////////////////////////////////////////////////////

namespace {
	struct TextureBinding {
		uint8 mStage;
		uint8 mTexture;
		bool mbWrapU;
		bool mbWrapV;
		bool mbBilinear;
		bool mbAutoBilinear;

		static void EmitDefinition(FILE *fo) {
				fputs(R"_x_(
struct TextureBinding {
	uint8 mStage;
	uint8 mTexture;
	bool mbWrapU;
	bool mbWrapV;
	bool mbBilinear;
	bool mbAutoBilinear;
};
)_x_", fo);
		}
	};

	struct PassInfo {
		int mPSIndex = -1;
		int mVSIndex = -1;
		std::vector<TextureBinding> mTextureBindings;
		int mRenderTarget = -1;
		uint8 mViewportW = 0;
		uint8 mViewportH = 0;
		bool mbUseBumpEnv = false;
		bool mbClipPosition = false;

		static void EmitDefinition(FILE *fo) {
				fputs(R"_x_(
struct PassInfo {
	int mVertexShaderIndex;
	int mPixelShaderIndex;
	const vdvector_view<const TextureBinding> mTextureBindings;
	int mRenderTarget;
	uint8 mViewportW;
	uint8 mViewportH;
	uint8 mBumpEnvScale;
	bool mbClipPosition;
};
)_x_", fo);
		}

		void PreEmit(FILE *fo, const char *prefix) const {
			if (mTextureBindings.empty()) {
				fprintf(fo, "static const vdvector_view<TextureBinding> %s_texbindings {};\n", prefix);
			} else {
				fprintf(fo, "static const TextureBinding %s_texbindings[]={\n", prefix);
				for (auto&& texb : mTextureBindings) {
					fprintf(fo, "\t{ %u, %u, %s, %s, %s, %s },\n"
						, texb.mStage
						, texb.mTexture
						, texb.mbWrapU ? "true" : "false"
						, texb.mbWrapV ? "true" : "false"
						, texb.mbBilinear ? "true" : "false"
						, texb.mbAutoBilinear ? "true" : "false"
					);
				}
				fprintf(fo, "};\n");
			}
		}

		void Emit(FILE *fo, const char *prefix) const {
			fprintf(fo, "\t\t{\n");
			fprintf(fo, "\t\t\t%d, %d,\n", mVSIndex, mPSIndex);
			fprintf(fo, "\t\t\t%s_texbindings,\n", prefix);
			fprintf(fo, "\t\t\t%d,\n", mRenderTarget);
			fprintf(fo, "\t\t\t%u, %u,\n", mViewportW, mViewportH);
			fprintf(fo, "\t\t\t%s,\n", mbUseBumpEnv ? "true" : "false");
			fprintf(fo, "\t\t\t%s,\n", mbClipPosition ? "true" : "false");
			fprintf(fo, "\t\t}");
		}
	};

	struct TechniqueInfo {
		VDStringA mName;
		std::vector<PassInfo> mPasses;

		static void EmitDefinition(FILE *fo) {
				fputs(R"_x_(
struct TechniqueInfo {
	const vdvector_view<const PassInfo> mPasses;
};
)_x_", fo);
		}

		void Emit(FILE *fo) const {
			uint32 index = 0;
			VDStringA passPrefix;
			for(auto&& pass : mPasses) {
				passPrefix.sprintf("g_technique_%s_pass_%u", mName.c_str(), index);
				pass.PreEmit(fo, passPrefix.c_str());

				++index;
			}

			fprintf(fo, "static const PassInfo g_technique_%s_passes[] = {\n", mName.c_str());
			index = 0;
			for (auto&& pass : mPasses) {
				passPrefix.sprintf("g_technique_%s_pass_%u", mName.c_str(), index++);
				pass.Emit(fo, passPrefix.c_str());
				fprintf(fo, ",\n");
			}
			fprintf(fo, "};\n");

			fprintf(fo, "extern const TechniqueInfo g_technique_%s= {\n", mName.c_str());
			fprintf(fo, "\tg_technique_%s_passes,\n", mName.c_str());
			fprintf(fo, "};\n");
		}
	};

	static const char *const kTextureNames[]={
		"vd_srctexture",
		"vd_src2atexture",
		"vd_src2btexture",
		"vd_src2ctexture",
		"vd_src2dtexture",
		"vd_srcpaltexture",
		"vd_temptexture",
		"vd_temp2texture",
		"vd_cubictexture",
		"vd_hevenoddtexture",
		"vd_dithertexture",
		"vd_interphtexture",
		"vd_interpvtexture",
		"vd_interptexture",
	};

	void DeleteShaderConstantTable(std::vector<uint32>& shader) {
		uint32 size;
		const void *data = VDD3DXFindShaderComment((const uint32 *)&shader[0], shader.size() * sizeof(uint32), VDMAKEFOURCC('C', 'T', 'A', 'B'), size);
		if (data) {
			ptrdiff_t offset = (char *)data - (char *)&shader[0];

			VDASSERT(!(offset & 3));
			VDASSERT(offset >= 8);

			// convert to dword offset
			offset >>= 2;
			size = (size + 3) >> 2;

			VDASSERT((size_t)offset + size <= shader.size());

			// erase comment token, fourcc, and comment data
			shader.erase(shader.begin() + (offset - 2), shader.begin() + offset + size);
		}
	}

	typedef std::vector<uint32> ShaderCode;

	struct ShaderCodeHash {
		size_t operator()(const ShaderCode& v) const {
			size_t h = 0;

			for(auto&& c : v)
				h = (h*3) + c;

			return h;
		}
	};

	struct ShaderCodeEq {
		bool operator()(const ShaderCode& a, const ShaderCode& b) const {
			return a == b;
		}
	};
}

///////////////////////////////////////////////////////////////////////////

void tool_fxc10(const vdfastvector<const char *>& args, const vdfastvector<const char *>& switches) {
	if (args.size() != 2) {
		puts("usage: asuka fxc10 source.fx target.cpp");
		exit(5);
	}

	InitD3DCompiler();

	atexit(ShutdownD3DCompiler);

	const char *filename = args[0];

	printf("Asuka: Compiling effect file: %s -> %s.\n", filename, args[1]);

	VDFile f(filename);
	FILE *fo = fopen(args[1], "w");
	if (!fo)
		printf("Asuka: Unable to open for write: %s\n", args[1]);

	try {
		fprintf(fo, "// Generated by Asuka from %s. DO NOT EDIT.\n", filename);

		uint32 len = VDClampToUint32(f.size());

		vdblock<char> buf(len);
		f.read(buf.data(), len);
		f.close();

		std::vector<TechniqueInfo> techniques;
		std::unordered_map<std::vector<uint32>, int, ShaderCodeHash, ShaderCodeEq> vshaders;
		std::unordered_map<std::vector<uint32>, int, ShaderCodeHash, ShaderCodeEq> pshaders;
		std::vector<const std::vector<uint32> *> vshaderTable;
		std::vector<const std::vector<uint32> *> pshaderTable;

		PassInfo *lastPass = nullptr;

		VDMemoryStream ms(buf.data(), len);
		VDTextStream ts(&ms);
		while(const char *line = ts.GetNextLine()) {
			ArgumentParser parser(line);

			parser.SkipSpaces();
			if (!parser.TryToken("//"))
				continue;

			const auto command = parser.GetIdentifier();
			if (command.size() < 2 || command[0] != '$' || command[1] != '$')
				continue;

			if (command == "$$emit_defs") {
				TextureBinding::EmitDefinition(fo);
				PassInfo::EmitDefinition(fo);
				TechniqueInfo::EmitDefinition(fo);

				for(auto&& tech : techniques) {
					tech.Emit(fo);
				}

				fputs(R"_x_(
struct EffectInfo {
	const vdvector_view<const uint32> mShaderData;
	const vdvector_view<const uint32> mVertexShaderOffsets;
	const vdvector_view<const uint32> mPixelShaderOffsets;
};
)_x_", fo);

				std::vector<uint32> shaderData;
				std::vector<int> vertexShaderOffsets;
				std::vector<int> pixelShaderOffsets;

				for(const auto *vs : vshaderTable) {
					vertexShaderOffsets.push_back(shaderData.size());
					shaderData.insert(shaderData.end(), vs->begin(), vs->end());
				}

				vertexShaderOffsets.push_back(shaderData.size());

				for(const auto *ps : pshaderTable) {
					pixelShaderOffsets.push_back(shaderData.size());
					shaderData.insert(shaderData.end(), ps->begin(), ps->end());
				}
				pixelShaderOffsets.push_back(shaderData.size());

				// output effect data
				fprintf(fo, "static const uint32 g_effect_shaderData[]={\n");
				for (int i = 0, count = shaderData.size(); i<count; i += 8) {
					fprintf(fo, "\t\t");
					for (int j = i; j<i + 8 && j<count; ++j)
						fprintf(fo, "0x%08x,", shaderData[j]);

					fprintf(fo, "\n");
				}
				fprintf(fo, "};\n");

				fprintf(fo, "static const uint32 g_effect_vsOffsets[]={\n");
				for (const int offset : vertexShaderOffsets)
					fprintf(fo, "%d,", offset);
				fprintf(fo, "};\n");

				fprintf(fo, "static const uint32 g_effect_psOffsets[]={\n");
				for (const int offset : pixelShaderOffsets)
					fprintf(fo, "%d,", offset);
				fprintf(fo, "};\n");

				fprintf(fo, "static const EffectInfo g_effect={\n");
				fprintf(fo, "\tg_effect_shaderData,\n");

				fprintf(fo, "\tg_effect_vsOffsets,\n");
				fprintf(fo, "\tg_effect_psOffsets,\n");

				fprintf(fo, "};\n");
			} else if (command == "$$technique") {
				const auto name = parser.GetIdentifier();

				techniques.emplace_back();
				TechniqueInfo& tech = techniques.back();
				tech.mName = name;

				lastPass = nullptr;
			} else if (command == "$$pass") {
				if (techniques.empty()) {
					printf("Effect compilation failed: pass definition with no technique\n");
					exit(20);
				}

				auto& passes = techniques.back().mPasses;
				passes.emplace_back();
				lastPass = &passes.back();

				if (passes.size() == 1) {
					lastPass->mRenderTarget = 0;
					lastPass->mViewportW = 2;
					lastPass->mViewportH = 2;
				}
			} else if (command == "$$target") {
				if (!lastPass)
					throw MyError("Effect compilation failed: target reference with no pass");

				const auto name = parser.GetIdentifier();

				if (name == "main") {
					lastPass->mRenderTarget = 0;

					// main RT defaults to out,out instead of full,full
					lastPass->mViewportW = 2;
					lastPass->mViewportH = 2;
				} else if (name == "temp")
					lastPass->mRenderTarget = 1;
				else if (name == "temp2")
					lastPass->mRenderTarget = 2;
				else
					throw MyError("Unknown target name: %.*s", (int)name.size(), name.data());
			} else if (command == "$$clip_pos") {
				if (!lastPass)
					throw MyError("Effect compilation failed: clip_pos command with no pass");

				lastPass->mbClipPosition = true;
			} else if (command == "$$viewport") {
				if (!lastPass)
					throw MyError("Effect compilation failed: viewport command with no pass");

				const auto hmode = parser.GetIdentifier();
				const auto vmode = parser.GetIdentifier();

				if (hmode == "src")
					lastPass->mViewportW = 1;
				else if (hmode == "out")
					lastPass->mViewportW = 2;
				else if (hmode == "unclipped")
					lastPass->mViewportW = 3;
				else if (hmode == "full")
					lastPass->mViewportW = 0;
				else
					throw MyError("Unknown viewport mode: %.*s", (int)hmode.size(), hmode.data());

				if (vmode == "src")
					lastPass->mViewportH = 1;
				else if (vmode == "out")
					lastPass->mViewportH = 2;
				else if (vmode == "unclipped")
					lastPass->mViewportH = 3;
				else if (vmode == "full")
					lastPass->mViewportH = 0;
				else
					throw MyError("Unknown viewport mode: %.*s", (int)vmode.size(), vmode.data());
			} else if (command == "$$bumpenv") {
				if (!lastPass)
					throw MyError("Effect compilation failed: bumpenv command with no pass");

				lastPass->mbUseBumpEnv = true;
			} else if (command == "$$vertex_shader_ext") {
				const auto shaderName = parser.GetIdentifier();

				if (!lastPass)
					throw MyError("Effect compilation failed: external vertex shader reference '%.*s' with no pass", (int)shaderName.size(), shaderName.data());

				VDFile f(VDMakePath(VDTextAToW(VDFileSplitPathLeftSpan(VDStringSpanA(filename))), VDTextAToW(shaderName)).c_str());
				const size_t len4 = (size_t)(f.size() >> 2);
				std::vector<uint32> vs(len4, 0);

				f.read(vs.data(), len4 * sizeof(uint32));

				DeleteShaderConstantTable(vs);

				auto r = vshaders.emplace(vs, (int)vshaders.size());
				if (r.second)
					vshaderTable.push_back(&r.first->first);

				lastPass->mVSIndex = (int)r.first->second;
			} else if (command == "$$pixel_shader_ext") {
				const auto shaderName = parser.GetIdentifier();

				if (!lastPass)
					throw MyError("Effect compilation failed: external pixel shader reference '%.*s' with no pass", (int)shaderName.size(), shaderName.data());

				VDFile f(VDMakePath(VDTextAToW(VDFileSplitPathLeftSpan(VDStringSpanA(filename))), VDTextAToW(shaderName)).c_str());
				const size_t len4 = (size_t)(f.size() >> 2);
				std::vector<uint32> ps(len4, 0);

				f.read(ps.data(), len4 * sizeof(uint32));

				DeleteShaderConstantTable(ps);

				auto r = pshaders.emplace(ps, (int)pshaders.size());
				if (r.second)
					pshaderTable.push_back(&r.first->first);

				lastPass->mPSIndex = (int)r.first->second;
			} else if (command == "$$vertex_shader") {
				const VDStringA compile_target = parser.GetIdentifier();
				const VDStringA function_name = parser.GetIdentifier();

				if (!lastPass) {
					printf("Effect compilation failed: vertex shader reference '%s' with no pass\n", function_name.c_str());
					exit(20);
				}

				FXC10IncludeHandler includeHandler(VDFileSplitPathLeft(VDStringA(filename)).c_str());

				vdrefptr<ID3D10Blob> shader;
				vdrefptr<ID3D10Blob> errors;
				HRESULT hr = g_pD3DCompile(buf.data(), len, filename, nullptr, &includeHandler, function_name.c_str(), compile_target.c_str(), 0, 0, ~shader, ~errors);

				if (FAILED(hr)) {
					printf("Effect compilation failed for \"%s\" with target %s (hr=%08x)\n", filename, compile_target.c_str(), (unsigned)hr);

					if (errors)
						puts((const char *)errors->GetBufferPointer());

					shader.clear();
					errors.clear();
					fclose(fo);
					remove(args[1]);
					exit(10);
				}

				const uint32 *src = (const uint32 *)shader->GetBufferPointer();
				const size_t len4 = shader->GetBufferSize() >> 2;
				std::vector<uint32> vs(src, src + len4);

				DeleteShaderConstantTable(vs);

				auto r = vshaders.emplace(vs, (int)vshaders.size());
				if (r.second)
					vshaderTable.push_back(&r.first->first);

				lastPass->mVSIndex = (int)r.first->second;
			} else if (command == "$$pixel_shader") {
				const VDStringA compile_target = parser.GetIdentifier();
				const VDStringA function_name = parser.GetIdentifier();

				if (!lastPass) {
					printf("Effect compilation failed: pixel shader reference '%s' with no pass\n", function_name.c_str());
					exit(20);
				}

				FXC10IncludeHandler includeHandler(VDFileSplitPathLeft(VDStringA(filename)).c_str());

				vdrefptr<ID3D10Blob> shader;
				vdrefptr<ID3D10Blob> errors;
				HRESULT hr = g_pD3DCompile(buf.data(), len, filename, nullptr, &includeHandler, function_name.c_str(), compile_target.c_str(), 0, 0, ~shader, ~errors);

				if (FAILED(hr)) {
					printf("Effect compilation failed for \"%s\" with target %s (hr=%08x)\n", filename, compile_target.c_str(), (unsigned)hr);

					if (errors)
						puts((const char *)errors->GetBufferPointer());

					shader.clear();
					errors.clear();
					fclose(fo);
					remove(args[1]);
					exit(10);
				}

				const uint32 *src = (const uint32 *)shader->GetBufferPointer();
				const size_t len4 = shader->GetBufferSize() >> 2;
				std::vector<uint32> ps(src, src + len4);

				DeleteShaderConstantTable(ps);

				auto r = pshaders.emplace(ps, (int)pshaders.size());
				if (r.second)
					pshaderTable.push_back(&r.first->first);

				lastPass->mPSIndex = (int)r.first->second;
			} else if (command == "$$texture") {
				if (!lastPass) {
					printf("Effect compilation failed: texture binding with no pass\n");
					exit(20);
				}

				uint32 stage;
				if (!parser.TryParseUint32(stage) || stage > 15) {
					printf("Effect compilation failed: invalid texture stage\n");
					exit(20);
				}

				const auto nameArg = parser.GetIdentifier();
				const auto addressUArg = parser.GetIdentifier();
				const auto addressVArg = parser.GetIdentifier();
				const auto filterArg = parser.GetIdentifier();

				int textureNameIndex = -1;

				for(size_t i=0; i<vdcountof(kTextureNames); ++i) {
					if (nameArg == kTextureNames[i]) {
						textureNameIndex = (int)i + 1;
						break;
					}
				}

				if (textureNameIndex < 0) {
					printf("Effect compilation failed: unknown texture '%s'\n", VDStringA(nameArg).c_str());
					exit(20);
				}

				bool wrapU = false;
				if (addressUArg == "wrap")
					wrapU = true;
				else if (addressUArg != "clamp") {
					printf("Effect compilation failed: unknown texture addressing mode '%s'\n", VDStringA(addressUArg).c_str());
					exit(20);
				}

				bool wrapV = false;
				if (addressVArg == "wrap")
					wrapV = true;
				else if (addressVArg != "clamp") {
					printf("Effect compilation failed: unknown texture addressing mode '%s'\n", VDStringA(addressVArg).c_str());
					exit(20);
				}

				bool bilinear = false;
				bool autobilinear = false;
				if (filterArg == "autobilinear") {
					autobilinear = true;
				} else if (filterArg == "bilinear") {
					bilinear = true;
				} else if (filterArg != "point") {
					printf("Effect compilation failed: unknown texture filtering mode '%s'\n", VDStringA(filterArg).c_str());
					exit(20);
				}

				lastPass->mTextureBindings.push_back( { (uint8)stage, (uint8)textureNameIndex, wrapU, wrapV, bilinear, autobilinear } );
			} else if (command == "$$export_shader") {
				const auto target = parser.GetIdentifier();
				const auto function = parser.GetIdentifier();
				const auto symbol = parser.GetIdentifier();

				if (symbol.empty())
					continue;
				bool multitarget = false;

				VDStringA target_name(target);
				VDStringA function_name(function);
				VDStringA symbol_name(symbol);

				printf("Asuka: compile %s %s() -> %s\n", target_name.c_str(), function_name.c_str(), symbol_name.c_str());

				vdvector<VDStringA> targets;
				if (!target_name.empty() && target_name[0] == '[') {
					VDStringRefA targets_parse(target_name.c_str() + 1);
					VDStringRefA target_token;

					for(;;) {
						if (!targets_parse.split(',', target_token)) {
							targets_parse.split(']', target_token);
							targets.push_back(target_token);
							break;
						}

						targets.push_back(target_token);
					}

					multitarget = true;
				} else {
					targets.push_back(target_name);
				}

				vdfastvector<uint8> shaderdata;
				vdfastvector<uint32> shadermetadata;

				fputs("\n\n", fo);

				uint32 target_count = 0;
				while(!targets.empty()) {
					VDStringA compile_target = targets.back();

					targets.pop_back();

					bool isd3d9 = false;
					bool isd3d10 = false;

					if (compile_target == "vs_1_1"
						|| compile_target == "vs_2_0"
						|| compile_target == "vs_3_0"
						|| compile_target == "ps_1_1"
						|| compile_target == "ps_1_2"
						|| compile_target == "ps_1_3"
						|| compile_target == "ps_2_0"
						|| compile_target == "ps_2_a"
						|| compile_target == "ps_2_b"
						|| compile_target == "ps_3_0")
					{
						isd3d9 = true;
					}
					else if (compile_target == "vs_4_0_level_9_1"
						|| compile_target == "vs_4_0_level_9_3"
						|| compile_target == "ps_4_0_level_9_1"
						|| compile_target == "ps_4_0_level_9_3")
					{
						isd3d10 = true;
					}

					const D3D10_SHADER_MACRO macros[]={
						{ "PROFILE_D3D9", isd3d9 ? "1" : "0" },
						{ "PROFILE_D3D10", isd3d10 ? "1" : "0" },
						{ NULL, NULL },
					};

					FXC10IncludeHandler includeHandler(VDFileSplitPathLeft(VDStringA(filename)).c_str());

					vdrefptr<ID3D10Blob> shader;
					vdrefptr<ID3D10Blob> errors;
					HRESULT hr = g_pD3DCompile(buf.data(), len, filename, macros, &includeHandler, function_name.c_str(), compile_target.c_str(), 0, 0, ~shader, ~errors);

					if (FAILED(hr)) {
						printf("Effect compilation failed for \"%s\" with target %s (hr=%08x)\n", filename, compile_target.c_str(), (unsigned)hr);

						if (errors)
							puts((const char *)errors->GetBufferPointer());

						shader.clear();
						errors.clear();
						fclose(fo);
						remove(args[1]);
						exit(10);
					}

					const uint8 *compile_data = (const uint8 *)shader->GetBufferPointer();
					const uint32 compile_len = shader->GetBufferSize();

					vdrefptr<ID3D10Blob> disasm;
					hr = g_pD3DDisassemble(compile_data, compile_len, 0, NULL, ~disasm);
					if (SUCCEEDED(hr)) {
						VDMemoryStream ms(disasm->GetBufferPointer(), disasm->GetBufferSize());
						VDTextStream ts(&ms);

						fprintf(fo, "/* -- %s --\n", compile_target.c_str());

						while(const char *line = ts.GetNextLine()) {
							fprintf(fo, "\t%s\n", line);
						}

						fputs("*/\n", fo);
					}

					disasm.clear();

					shadermetadata.push_back(VDHashString32I(compile_target.c_str()));
					shadermetadata.push_back(shaderdata.size());
					shadermetadata.push_back(compile_len);
					++target_count;

					shaderdata.insert(shaderdata.end(), compile_data, compile_data + compile_len);
				}

				if (multitarget) {
					shadermetadata.push_back(0);

					uint32 offset = shadermetadata.size() * 4;

					for(uint32 i=0; i<target_count; ++i)
						shadermetadata[i*3 + 1] += offset;

					const uint8 *metastart = (const uint8 *)shadermetadata.data();
					shaderdata.insert(shaderdata.begin(), metastart, metastart + shadermetadata.size() * 4);
				}

				const uint8 *data = shaderdata.data();
				uint32 data_len = shaderdata.size();

				fprintf(fo, "static const uint8 %s[] = {\n", symbol_name.c_str());

				while(data_len) {
					putc('\t', fo);

					uint32 tc = data_len > 16 ? 16 : data_len;

					for(uint32 i=0; i<tc; ++i)
						fprintf(fo, "0x%02x,", *data++);

					data_len -= tc;

					putc('\n', fo);
				}

				fprintf(fo, "};\n");
			} else {
				throw MyError("Unknown command: %.*s", (int)command.size(), command.data());
			}
		}
	} catch(const MyError&) {
		fclose(fo);
		remove(args[1]);
		throw;
	}

	fclose(fo);

	printf("Asuka: Compilation was successful.\n");
}
