//	Asuka - VirtualDub Build/Post-Mortem Utility
//	Copyright (C) 2005-2006 Avery Lee
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
#include <stdio.h>
#include <exception>
#include <unordered_map>
#include <vd2/system/error.h>
#include <vd2/system/filesys.h>
#include <vd2/system/vdalloc.h>

#include "utils.h"
#include "glc.h"

using namespace GLCIL;

namespace {
	enum {
		kTokenIdent		= 256,
		kTokenInteger,
		kTokenDouble,
		kTokenForeignCode,
		kTokenCount
	};

}

IGLCFragmentShader *CompileFragmentShaderNVRegisterCombiners(GLCErrorSink& errout, const GLCFragmentShader& shader, bool NV_register_combiners_2);
IGLCFragmentShader *CompileFragmentShaderATIFragmentShader(GLCErrorSink& errout, const GLCFragmentShader& shader);

///////////////////////////////////////////////////////////////////////////////
struct GLCKeyword {
public:
	GLCKeyword(const char *s) : mpKeyword(s), mKeywordLen(strlen(s)) {}

	const char *mpKeyword;
	int mKeywordLen;
};

///////////////////////////////////////////////////////////////////////////////

class GLCTokenizer : public GLCErrorSink {
public:
	GLCTokenizer();
	~GLCTokenizer();

	void Init(const char *srcName, const char *src, uint32 len);
	int Token();
	void Push(int tok) { mPushedToken = tok; }
	void EnableNewlines(bool enable) { mbReturnNewlines = enable; }
	void EnableForeignCode(bool enable) { mbReturnForeignCode = enable; }

	GLCCodeLocation GetLocation() const {
		GLCCodeLocation loc = { mpSourceName, mLine, (int)(mpSrc - mpLineStart) };
		return loc;
	}

	int GetInteger() const { return mTokenInt; }
	double GetDouble() const { return mTokenDbl; }
	const char *GetToken() const { return mpTokenStart; }
	int GetTokenLen() const { return mTokenLen; }
	int GetLineNumber() const { return mLine; }

	bool IsKeyword(const GLCKeyword& keyword) const;

protected:
	bool mbReturnNewlines;
	bool mbReturnForeignCode;
	const char *mpSrc;
	const char *mpSrcEnd;
	const char *mpLineStart;
	const char *mpSourceName;
	const char *mpTokenStart;
	int mTokenLen;
	int mTokenInt;
	int mPushedToken;
	int mLine;
	double mTokenDbl;
};

GLCTokenizer::GLCTokenizer() {
}

GLCTokenizer::~GLCTokenizer() {
}

void GLCTokenizer::Init(const char *srcName, const char *src, uint32 len) {
	mpSrc = src;
	mpSrcEnd = src + len;
	mpLineStart = src;
	mpSourceName = srcName;
	mLine = 1;
	mPushedToken = 0;
	mbReturnNewlines = false;
	mbReturnForeignCode = false;
}

int GLCTokenizer::Token() {
	if (mPushedToken) {
		int tok = mPushedToken;
		mPushedToken = 0;
		return tok;
	}

	// skip whitespace and comments
	char c;
	for(;;) {
		if (mpSrc == mpSrcEnd)
			return 0;

		c = *mpSrc++;

		// skip whitespace
		if (c == ' ' || c == '\t')
			continue;

		// skip newlines
		if (c == '\n' || c == '\r') {
			++mLine;

			if (mpSrc != mpSrcEnd && *mpSrc == (c ^ ('\n' ^ '\r')))
				++mpSrc;

			mpTokenStart = mpSrc;
			mTokenLen = 0;
			mpLineStart = mpSrc;
			if (mbReturnNewlines)
				return '\n';
			else
				continue;
		}

		// skip C++ style comments
		if (c == '/' && mpSrc != mpSrcEnd && mpSrc[0] == '/') {
			++mpSrc;

			while(mpSrc != mpSrcEnd && *mpSrc != '\r' && *mpSrc != '\n')
				++mpSrc;

			continue;
		}

		break;
	}

	mpTokenStart = mpSrc - 1;

	if (mbReturnForeignCode) {
		int braceCount = 0;

		for(;;) {
			if (mpSrc == mpSrcEnd)
				break;

			c = *mpSrc++;

			// skip whitespace
			if (c == ' ' || c == '\t')
				continue;

			// skip newlines
			if (c == '\n' || c == '\r') {
				++mLine;

				if (mpSrc != mpSrcEnd && *mpSrc == (c ^ ('\n' ^ '\r')))
					++mpSrc;

				mpLineStart = mpSrc;
				continue;
			}

			// skip C++ style comments
			if (c == '/' && mpSrc != mpSrcEnd && mpSrc[0] == '/') {
				++mpSrc;

				while(mpSrc != mpSrcEnd && *mpSrc != '\r' && *mpSrc != '\n')
					++mpSrc;

				continue;
			}

			if (c == '{')
				++braceCount;
			else if (c == '}') {
				--braceCount;
				if (braceCount < 0) {
					--mpSrc;
					break;
				}
			}
		}

		mTokenLen = mpSrc - mpTokenStart;
		return kTokenForeignCode;
	}

	// check for integers
	if (isdigit((unsigned char)c)) {
		mTokenInt = c - '0';

		if (mpSrc != mpSrcEnd) {
			do {
				c = *mpSrc;
				if (!isdigit((unsigned char)c))
					break;

				mTokenInt = (mTokenInt * 10) + (c - '0');
			} while(++mpSrc != mpSrcEnd);

			if (c == '.') {
				++mpSrc;

				// hmm, it's a double.
				while(mpSrc != mpSrcEnd && isdigit((unsigned char)*mpSrc))
					++mpSrc;

				if (mpSrc != mpSrcEnd) {
					c = *mpSrc;

					if (c == 'e' || c == 'E') {
						++mpSrc;
						if (mpSrc == mpSrcEnd)
							ThrowError("Invalid floating-point constant");

						c = *mpSrc;
						if (c == '+' || c == '-')
							++mpSrc;

						if (mpSrc == mpSrcEnd || !isdigit((unsigned char)*mpSrc))
							ThrowError("Invalid floating-point constant");

						do {
							++mpSrc;
						} while(mpSrc != mpSrcEnd && isdigit((unsigned char)*mpSrc));
					}

					if (mpSrc != mpSrcEnd && (*mpSrc == 'f' || *mpSrc == 'F'))
						++mpSrc;
				}

				mTokenLen = mpSrc - mpTokenStart;

				std::string tmp(mpTokenStart, mpSrc);
				mTokenDbl = strtod(tmp.c_str(), NULL);

				return kTokenDouble;
			}
		}

		mTokenLen = mpSrc - mpTokenStart;
		return kTokenInteger;
	}

	// check for identifiers
	if (c == '_' || isalnum((unsigned char)c)) {
		while(mpSrc != mpSrcEnd) {
			c = *mpSrc;

			if (c != '_' && !isalnum((unsigned char)c))
				break;

			++mpSrc;
		}

		mTokenLen = mpSrc - mpTokenStart;
		return kTokenIdent;
	}

	// check for single char
	if (strchr("{}(),.=;+-*/", c)) {
		mTokenLen = 1;
		return (unsigned char)c;
	}

	if (isprint((unsigned char)c))
		ThrowError("Unrecognized character '%c'", c);
	else
		ThrowError("Unrecognized character '0x%02x'", (unsigned char)c);
}

bool GLCTokenizer::IsKeyword(const GLCKeyword& keyword) const {
	return (mTokenLen == keyword.mKeywordLen && !memcmp(mpTokenStart, keyword.mpKeyword, mTokenLen));
}

void GLCErrorSink::ThrowError(const char *format, ...) {
	char buf[4096];
	va_list val;

	va_start(val, format);
	int cnt = _vsnprintf(buf, 4096, format, val);
	va_end(val);

	if ((unsigned)cnt >= 4096)
		buf[4095] = 0;

	const GLCCodeLocation loc(GetLocation());
	throw MyError("Shader compilation failed.\n%s(%d,%d): Error! %s", loc.mpFileName, loc.mLine, loc.mColumn, buf);
}

void GLCErrorSink::ThrowError(const GLCCodeLocation& loc, const char *format, ...) {
	char buf[4096];
	va_list val;

	va_start(val, format);
	int cnt = _vsnprintf(buf, 4096, format, val);
	va_end(val);

	if ((unsigned)cnt >= 4096)
		buf[4095] = 0;

	throw MyError("Shader compilation failed.\n%s(%d,%d): Error! %s", loc.mpFileName, loc.mLine, loc.mColumn, buf);
}

///////////////////////////////////////////////////////////////////////////////
class GLCCompiler {
public:
	GLCCompiler();
	~GLCCompiler();

	void Compile(const char *sourceName, const char *src, uint32 len, FILE *f);

protected:
	void ParseTechnique();
	void ParseFragmentShader(GLCFragmentShader& shader);
	GLCDestArg ParseFragmentShaderDestination();
	GLCSourceArg ParseFragmentShaderArgument();

	double	ParseConstantDoubleExpression();

	void Expect(int c);
	VDNORETURN void Huh(const char *expected, int found);
	std::string TokenName(int tok);

	GLCTokenizer	mLexer;
	FILE *mpOutput;

	vdfastvector<IGLCFragmentShader *> mFragmentShaders;

	struct Technique {
		IGLCFragmentShader *mpFragmentShader;
	};

	typedef std::unordered_map<std::string, Technique> Techniques;
	Techniques mTechniques;
};

GLCCompiler::GLCCompiler() {
}

GLCCompiler::~GLCCompiler() {
	while(!mFragmentShaders.empty()) {
		delete mFragmentShaders.back();
		mFragmentShaders.pop_back();
	}
}

namespace {
	static const GLCKeyword sKeywordPass("pass");
	static const GLCKeyword sKeywordTechnique("technique");
	static const GLCKeyword sKeywordFragmentShader("fragmentshader");
	static const GLCKeyword sKeywordFragment_Shader("fragment_shader");
	static const GLCKeyword sKeywordNV_Register_Combiners("NV_register_combiners");
	static const GLCKeyword sKeywordNV_Register_Combiners2("NV_register_combiners2");
	static const GLCKeyword sKeywordATI_Fragment_Shader("ATI_fragment_shader");
	static const GLCKeyword sKeywordARB_Fragment_Shader("ARB_fragment_shader");
}

void GLCCompiler::Compile(const char *sourceName, const char *src, uint32 len, FILE *f) {
	mpOutput = f;
	mLexer.Init(sourceName, src, len);

	while(int tok = mLexer.Token()) {
		if (tok == kTokenIdent) {
			if (mLexer.IsKeyword(sKeywordTechnique)) {
				ParseTechnique();
			} else if (mLexer.IsKeyword(sKeywordFragment_Shader)) {
				GLCFragmentShader shader;
				ParseFragmentShader(shader);
				continue;
			}
		}
	}

	// write out fragment shaders
	fputs("/////////////////////////////////////////////////////////////////////////////\n", f);
	fputs("// fragment shaders\n", f);
	fputs("//\n", f);
	const int fragmentShaderCount = mFragmentShaders.size();

	for(int i=0; i<fragmentShaderCount; ++i) {
		char buf[64];
		sprintf(buf, "g_fragmentShader%d", i);

		mFragmentShaders[i]->Write(mpOutput, buf);
	}

	// write out techniques
	fputs("\n", f);
	fputs("/////////////////////////////////////////////////////////////////////////////\n", f);
	fputs("// techniques\n", f);
	fputs("//\n", f);
	fprintf(f, "static const struct VDOpenGLTechnique g_techniques[]={\n");
	Techniques::const_iterator it(mTechniques.begin()), itEnd(mTechniques.end());
	for(; it!=itEnd; ++it) {
		const Technique& tech = it->second;

		int index = std::find(mFragmentShaders.begin(), mFragmentShaders.end(), tech.mpFragmentShader) - mFragmentShaders.begin();

		fprintf(f, "\t{ &g_fragmentShader%d, %s },\n", index, tech.mpFragmentShader->GetTypeString());
	}
	fprintf(f, "};\n");
	
	it = mTechniques.begin();
	for(int techIndex = 0; it!=itEnd; ++it, ++techIndex) {
		const char *name = it->first.c_str();

		fprintf(f, "static const int kVDOpenGLTechIndex_%s = %d;\n", name, techIndex);
	}


	printf("Asuka: %d techniques, %d fragment shaders.\n", (int)mTechniques.size(), (int)mFragmentShaders.size());
}

namespace {
	class ARBFragmentShader : public vdrefcounted<IGLCFragmentShader> {
	public:
		ARBFragmentShader(const char *s, int len) {
			mText.assign(s, s+len);
			printf("%.*s\n", len, s);
		}

		const char *GetTypeString() {
			return "kVDOpenGLFragmentShaderModeARBFS";
		}

		void Write(FILE *f, const char *sym) {
			fprintf(f, "static const char %s[]=\n", sym);

			bool open = false;
			bool sol = true;
			int len = mText.size();
			const char *s = mText.data();

			for(int i=0; i<len; ++i) {
				char c = s[i];

				if (!open) {
					putc('"', f);
					open = true;
				}

				if (c == '\r')
					continue;

				if (c == '\t')
					c = ' ';

				if (c == ' ' && sol)
					continue;

				if (c == '\n' || c == '\\' || c == '"')
					putc('\\', f);

				if (c == '\n') {
					fputs("n\"\n", f);
					open = false;
					sol = true;
				} else {
					sol = false;
					putc(c, f);
				}
			}

			if (open)
				fputs("\\n\"\n", f);

			fprintf(f, ";\n");
		}

	public:
		vdfastvector<char> mText;
	};
}

void GLCCompiler::ParseTechnique() {
	std::string namestr;

	Expect(kTokenIdent);
	const char *name = mLexer.GetToken();
	namestr.assign(name, name + mLexer.GetTokenLen());

	std::pair<Techniques::iterator, bool> result(mTechniques.insert(Techniques::value_type(namestr, Technique())));

	if (!result.second)
		mLexer.ThrowError("Technique '%s' already defined", namestr.c_str());

	Technique& tech = result.first->second;

	Expect('{');

	Expect(kTokenIdent);
	if (!mLexer.IsKeyword(sKeywordPass))
		Huh("pass declaration", kTokenIdent);

	Expect('{');

	for(;;) {
		int tok = mLexer.Token();

		if (tok == '\n')
			continue;
		else if (tok == '}')
			break;
		else if (tok == kTokenIdent) {
			if (mLexer.IsKeyword(sKeywordFragmentShader)) {
				Expect('=');
				Expect(kTokenIdent);
				if (mLexer.IsKeyword(sKeywordFragment_Shader)) {
					tok = mLexer.Token();

					if (tok != kTokenIdent)
						Huh("fragment shader profile", tok);

					enum {
						kProfileNVRC,
						kProfileNVRC2,
						kProfileATIFS,
						kProfileARBFS
					} profile;

					if (mLexer.IsKeyword(sKeywordNV_Register_Combiners))
						profile = kProfileNVRC;
					else if (mLexer.IsKeyword(sKeywordNV_Register_Combiners2))
						profile = kProfileNVRC2;
					else if (mLexer.IsKeyword(sKeywordATI_Fragment_Shader))
						profile = kProfileATIFS;
					else if (mLexer.IsKeyword(sKeywordARB_Fragment_Shader))
						profile = kProfileARBFS;
					else
						mLexer.ThrowError("Unknown shader profile '%.*s'", mLexer.GetTokenLen(), mLexer.GetToken());

					vdrefptr<IGLCFragmentShader> fshader;
					if (profile == kProfileARBFS) {
						Expect('{');
						mLexer.EnableForeignCode(true);
						Expect(kTokenForeignCode);
						mLexer.EnableForeignCode(false);
						fshader = new ARBFragmentShader(mLexer.GetToken(), mLexer.GetTokenLen());
						Expect('}');
					} else {
						GLCFragmentShader shader;
						ParseFragmentShader(shader);
						
						switch(profile) {
							case kProfileNVRC:
								fshader = CompileFragmentShaderNVRegisterCombiners(mLexer, shader, false);
								break;
							case kProfileNVRC2:
								fshader = CompileFragmentShaderNVRegisterCombiners(mLexer, shader, true);
								break;
							case kProfileATIFS:
								fshader = CompileFragmentShaderATIFragmentShader(mLexer, shader);
								break;
						}
					}

					tech.mpFragmentShader = fshader;

					mFragmentShaders.push_back(fshader.release());
				}
				Expect(';');
				continue;
			}
		}

		Huh("technique parameter", tok);
	}

	Expect('}');
}

void GLCCompiler::ParseFragmentShader(GLCFragmentShader& shader) {
	shader.mLocation = mLexer.GetLocation();

	Expect('{');

	mLexer.EnableNewlines(true);

	bool coissue = false;
	for(;;) {
		int tok = mLexer.Token();

		if (tok == '+') {
			coissue = true;
			continue;
		}

		if (tok == '}')
			break;
		else if (tok == '\n') {
			if (coissue)
				mLexer.ThrowError("Syntax error");
			continue;
		} else if (tok == kTokenIdent) {
			const GLCCodeLocation loc = mLexer.GetLocation();
			const char *s = mLexer.GetToken();
			int toklen = mLexer.GetTokenLen();
			int insnlen = 0;

			// parse instruction itself
			while(insnlen < toklen && s[insnlen] != '_')
				++insnlen;

			int insn = 0;
			int insn_dests = 1;
			int insn_sources = 0;
			switch(insnlen) {
			case 2:
				if (!memcmp(s, "dd", 2)) {
					insn = kFSOpDd;
					insn_dests = 2;
					insn_sources = 4;
				}
				if (!memcmp(s, "dm", 2)) {
					insn = kFSOpDm;
					insn_dests = 2;
					insn_sources = 4;
				}
				break;
			case 3:
				if (!memcmp(s, "mov", 3)) {
					insn = kFSOpMov;
					insn_sources = 1;
				} else if (!memcmp(s, "add", 3)) {
					insn = kFSOpAdd;
					insn_sources = 2;
				} else if (!memcmp(s, "sub", 3)) {
					insn = kFSOpSub;
					insn_sources = 2;
				} else if (!memcmp(s, "mul", 3)) {
					insn = kFSOpMul;
					insn_sources = 2;
				} else if (!memcmp(s, "mad", 3)) {
					insn = kFSOpMad;
					insn_sources = 3;
				} else if (!memcmp(s, "lrp", 3)) {
					insn = kFSOpLrp;
					insn_sources = 3;
				} else if (!memcmp(s, "dp3", 3)) {
					insn = kFSOpDp3;
					insn_sources = 2;
				} else if (!memcmp(s, "dp4", 3)) {
					insn = kFSOpDp4;
					insn_sources = 2;
				} else if (!memcmp(s, "dda", 3)) {
					insn = kFSOpDda;
					insn_dests = 3;
					insn_sources = 4;
				} else if (!memcmp(s, "mma", 3)) {
					insn = kFSOpMma;
					insn_dests = 3;
					insn_sources = 4;
				} else if (!memcmp(s, "def", 3)) {
					insn = kFSOpDef;
					insn_dests = 0;
					insn_sources = 4;
				}
				break;

			case 5:
				if (!memcmp(s, "final", 5)) {
					insn = kFSOpFinal;
					insn_dests = 0;
					insn_sources = 7;
				} else if (!memcmp(s, "texld", 5)) {
					insn = kFSOpTexld2Arg;
					insn_dests = 1;
					insn_sources = 1;
				} else if (!memcmp(s, "phase", 5)) {
					insn = kFSOpPhase;
					insn_dests = 0;
					insn_sources = 0;
				}
				break;

			case 6:
				if (!memcmp(s, "texcrd", 6)) {
					insn = kFSOpTexcrd;
					insn_dests = 1;
					insn_sources = 1;
				}
				break;
			}

			if (!insn)
				mLexer.ThrowError("Unknown fragment shader instruction '%.*s'", insnlen, s);

			// parse modifiers
			int modifiers = 0;
			while(insnlen < toklen) {
				if (s[insnlen] != '_')
					mLexer.ThrowError("Syntax error in fragment shader instruction");

				++insnlen;

				const char *modbase = s + insnlen;
				int modstart = insnlen;

				while(insnlen < toklen && s[insnlen] != '_')
					++insnlen;

				int modlen = insnlen - modstart;
				int modbit = 0;
				switch(modlen) {
				case 2:
					if (!memcmp(modbase, "x2", 2))
						modbit = kInsnModX2;
					else if (!memcmp(modbase, "x4", 2))
						modbit = kInsnModX4;
					else if (!memcmp(modbase, "x8", 2))
						modbit = kInsnModX8;
					else if (!memcmp(modbase, "d2", 2))
						modbit = kInsnModD2;
					else if (!memcmp(modbase, "d4", 2))
						modbit = kInsnModD4;
					else if (!memcmp(modbase, "d8", 2))
						modbit = kInsnModD8;
					break;

				case 3:
					if (!memcmp(modbase, "bx2", 3))
						modbit = kInsnModBX2;
					else if (!memcmp(modbase, "sat", 3))
						modbit = kInsnModSat;
					break;

				case 4:
					if (!memcmp(modbase, "bias", 4))
						modbit = kInsnModBias;
					break;
				}

				if (!modbit)
					mLexer.ThrowError("Invalid instruction modifier '%.*s'", modlen, modbase);

				modifiers |= modbit;
			}

			// def is special
			if (insn == kFSOpDef) {
				if (modifiers)
					mLexer.ThrowError("'def' instruction cannot have modifiers");

				if (coissue)
					mLexer.ThrowError("'def' instruction cannot be co-issued");

				GLCDestArg arg = ParseFragmentShaderDestination();

				if (arg.mWriteMask != 15 || (arg.mReg & kRegTypeMask) != kRegC0)
					mLexer.ThrowError("'def' must be used with a full constant register");

				int index = arg.mReg - kRegC0;

				if (shader.mUsedConstants & (1 << index))
					mLexer.ThrowError("Constant 'c%d' already defined", index);

				shader.mUsedConstants |= (1 << index);

				for(int i=0; i<4; ++i) {
					Expect(',');
					shader.mConstants[arg.mReg - kRegC0][i] = (float)ParseConstantDoubleExpression();
				}
			} else {
				GLCFragmentOp op={0};

				op.mInsn = insn;
				op.mModifiers = modifiers;
				op.mbCoIssue = coissue;
				coissue = false;

				// parse destinations
				for(int i=0; i<insn_dests; ++i) {
					if (i)
						Expect(',');

					op.mDstArgs[i] = ParseFragmentShaderDestination();
					if ((op.mDstArgs[i].mReg & kRegTypeMask) == kRegC0)
						mLexer.ThrowError("Constant registers cannot be used as destinations");
				}

				// parse sources
				for(int i=0; i<insn_sources; ++i) {
					if (i || insn_dests)
						Expect(',');
					op.mSrcArgs[i] = ParseFragmentShaderArgument();
				}

				op.mLocation = loc;

				Expect('\n');

				shader.mOps.push_back(op);
			}
			continue;
		}

		Huh("fragment shader instruction", tok);
	}

	mLexer.EnableNewlines(false);
}

GLCDestArg GLCCompiler::ParseFragmentShaderDestination() {
	GLCDestArg arg;

	int tok = mLexer.Token();

	if (tok != kTokenIdent)
		Huh("fragment shader register", tok);

	// parse out base register
	const char *s = mLexer.GetToken();
	int namelen = mLexer.GetTokenLen();

	if (memchr(s, '_', namelen))
		mLexer.ThrowError("Destination register cannot have modifier");

	arg.mReg = 0;
	if (namelen == 7 && !memcmp(s, "discard", 7)) {
		arg.mReg = kRegDiscard;
	} else {
		unsigned index = 0;

		for(int i=1; i<namelen; ++i) {
			if (!isdigit((unsigned char)s[i]))
				goto invalid_register;

			index = (index * 10) + (s[i] - '0');
		}

		if (s[0] == 't') {
			if (index >= 6)
				mLexer.ThrowError("Invalid texture register '%.*s'", namelen, s);
			arg.mReg = kRegT0 + index;
		} else if (s[0] == 'r') {
			if (index >= 6)
				mLexer.ThrowError("Invalid spare register '%.*s'", namelen, s);
			arg.mReg = kRegR0 + index;
		} else if (s[0] == 'c') {
			if (index >= 16)
				mLexer.ThrowError("Invalid constant register '%.*s'", namelen, s);
			arg.mReg = kRegC0 + index;
		} else if (s[0] == 'v') {
			if (index >= 2)
				mLexer.ThrowError("Invalid vertex interpolator register '%.*s'", namelen, s);
			arg.mReg = kRegV0 + index;
		}
	}

	if (!arg.mReg) {
invalid_register:
		mLexer.ThrowError("Unknown fragment shader destination register '%.*s'", namelen, s);
	}

	// parse out write mask
	tok = mLexer.Token();

	arg.mWriteMask = 15;
	if (tok == '.') {
		tok = mLexer.Token();

		if (tok != kTokenIdent)
			Huh("register write mask", tok);

		const char *s = mLexer.GetToken();
		int len = mLexer.GetTokenLen();

		arg.mWriteMask = 0;
		for(int i=0; i<len; ++i) {
			int bit;

			switch(s[i]) {
				case 'r':
					bit = 1;
					break;
				case 'g':
					bit = 2;
					break;
				case 'b':
					bit = 4;
					break;
				case 'a':
					bit = 8;
					break;

				default:
invalid_write_mask:
					mLexer.ThrowError("Invalid destination write mask");
			}

			if ((arg.mWriteMask & bit) || bit < arg.mWriteMask)
				goto invalid_write_mask;

			arg.mWriteMask |= bit;
		}
	} else {
		mLexer.Push(tok);
	}

	return arg;
}

GLCSourceArg GLCCompiler::ParseFragmentShaderArgument() {
	GLCSourceArg arg;
	int tok = mLexer.Token();

	// parse out complement and negation modifiers
	arg.mMods = 0;
	for(;;) {
		if (tok == '-')
			arg.mMods ^= kRegModNegate;
		else if (tok == kTokenInteger) {
			if (mLexer.GetInteger() != 1)
				mLexer.ThrowError("Invalid register modifier");

			Expect('-');

			arg.mMods |= kRegModComplement;
		} else
			break;

		tok = mLexer.Token();
	}

	if (tok != kTokenIdent)
		Huh("fragment shader register", tok);

	// parse out base register
	const char *s = mLexer.GetToken();
	int toklen = mLexer.GetTokenLen();
	int namelen = 0;

	// parse instruction itself
	while(namelen < toklen && s[namelen] != '_')
		++namelen;

	arg.mReg = 0;
	if (namelen == 4 && !memcmp(s, "zero", 4)) {
		arg.mReg = kRegZero;
	} else if (namelen == 7 && !memcmp(s, "discard", 7)) {
		mLexer.ThrowError("'discard' cannot be used in a source argument");
	} else {
		unsigned index = 0;

		for(int i=1; i<namelen; ++i) {
			if (!isdigit((unsigned char)s[i]))
				goto invalid_register;

			index = (index * 10) + (s[i] - '0');
		}

		if (s[0] == 't') {
			if (index >= 6)
				mLexer.ThrowError("Invalid texture register '%.*s'", namelen, s);
			arg.mReg = kRegT0 + index;
		} else if (s[0] == 'r') {
			if (index >= 6)
				mLexer.ThrowError("Invalid spare register '%.*s'", namelen, s);
			arg.mReg = kRegR0 + index;
		} else if (s[0] == 'c') {
			if (index >= 16)
				mLexer.ThrowError("Invalid constant register '%.*s'", namelen, s);
			arg.mReg = kRegC0 + index;
		} else if (s[0] == 'v') {
			if (index >= 2)
				mLexer.ThrowError("Invalid vertex interpolator register '%.*s'", namelen, s);
			arg.mReg = kRegV0 + index;
		}
	}

	if (!arg.mReg) {
invalid_register:
		mLexer.ThrowError("Unknown fragment shader register '%.*s'", namelen, s);
	}

	// parse modifiers
	while(namelen < toklen) {
		if (s[namelen] != '_')
			mLexer.ThrowError("Syntax error in fragment shader register");

		++namelen;

		const char *modbase = s + namelen;
		int modstart = namelen;

		while(namelen < toklen && s[namelen] != '_')
			++namelen;

		int modlen = namelen - modstart;
		int modbit = 0;
		switch(modlen) {
		case 2:
			if (!memcmp(modbase, "x2", 2))
				modbit = kRegModX2;
			break;
		case 3:
			if (!memcmp(modbase, "bx2", 3))
				modbit = kRegModBias | kRegModX2;
			else if (!memcmp(modbase, "sat", 3))
				modbit = kRegModSaturate;
			break;

		case 4:
			if (!memcmp(modbase, "bias", 4))
				modbit = kRegModBias;
			break;
		}

		if (!modbit)
			mLexer.ThrowError("Invalid register modifier '%.*s'", modlen, modbase);

		arg.mMods |= modbit;
	}

	tok = mLexer.Token();

	arg.mSwizzle = kSwizzleNone;
	arg.mSize = 4;
	if (tok == '.') {
		tok = mLexer.Token();

		if (tok != kTokenIdent)
			Huh("register swizzle", tok);

		const char *s = mLexer.GetToken();
		int len = mLexer.GetTokenLen();

		arg.mSwizzle = 0;
		arg.mSize = 0;
		for(int i=0; i<len; ++i) {
			int component;

			switch(s[i]) {
				case 'r':
					component = 0;
					break;
				case 'g':
					component = 1;
					break;
				case 'b':
					component = 2;
					break;
				case 'a':
					component = 3;
					break;

				default:
invalid_swizzle:
					mLexer.ThrowError("Invalid register swizzle");
			}

			arg.mSwizzle += component << (2*i);
			++arg.mSize;
			if (arg.mSize >= 4)
				goto invalid_swizzle;
		}

		// .a/r/g/b -> .aaaa/rrrr/gggg/bbbb
		if (arg.mSize == 1) {
			arg.mSize = 4;
			arg.mSwizzle *= 0x55;
		}
	} else {
		mLexer.Push(tok);
	}

	return arg;
}

double GLCCompiler::ParseConstantDoubleExpression() {
	vdfastvector<double> mValueStack;
	vdfastvector<int> mOpStack;
	int parensValue = 0;
	bool needValue = true;

	for(;;) {
		int tok = mLexer.Token();

		if (needValue) {
			if (tok == '(')
				parensValue += 0x1000000;
			else if (tok == '-')
				mOpStack.push_back(parensValue + 'N' + 0x60000);
			else if (tok == kTokenInteger) {
				mValueStack.push_back(mLexer.GetInteger());
				needValue = false;
			} else if (tok == kTokenDouble) {
				mValueStack.push_back(mLexer.GetDouble());
				needValue = false;
			} else if (tok != '+')
				Huh("numeric value", tok);
		} else {
			int opValue;

			if (tok == ')' && parensValue) {
				parensValue -= 0x1000000;
				continue;
			}

			if (tok == '+')
				opValue = parensValue + tok + 0x10000;
			else if (tok == '-')
				opValue = parensValue + tok + 0x10000;
			else if (tok == '*')
				opValue = parensValue + tok + 0x20000;
			else if (tok == '/')
				opValue = parensValue + tok + 0x20000;
			else if (tok == ';' || tok == ',' || tok == ')' || tok == '\n') {
				if (parensValue)
					mLexer.ThrowError("Unmatched ')'");

				mLexer.Push(tok);
				opValue = 0;
			} else
				Huh("expression operator", tok);

			while(!mOpStack.empty() && (mOpStack.back() & 0xffff0000) >= (opValue & 0xffff0000)) {
				int op = mOpStack.back() & 0xffff;
				mOpStack.pop_back();

				double y;
				switch(op) {
					case '+':
						y = mValueStack.back();
						mValueStack.pop_back();
						mValueStack.back() += y;
						break;
					case '-':
						y = mValueStack.back();
						mValueStack.pop_back();
						mValueStack.back() -= y;
						break;
					case '*':
						y = mValueStack.back();
						mValueStack.pop_back();
						mValueStack.back() *= y;
						break;
					case '/':
						y = mValueStack.back();
						if (y == 0.0f)
							mLexer.ThrowError("Division by zero");
						mValueStack.pop_back();
						mValueStack.back() /= y;
						break;
					case 'N':
						mValueStack.back() = -mValueStack.back();
						break;
				}
			}

			mOpStack.push_back(opValue);

			if (!opValue)
				break;

			needValue = true;
		}
	}

	VDASSERT(mValueStack.size() == 1);
	return mValueStack.back();
}

void GLCCompiler::Expect(int c) {
	int tok = mLexer.Token();

	if (c != tok)
		mLexer.ThrowError("Expected '%s', found '%s'", TokenName(c).c_str(), TokenName(tok).c_str());
}

void GLCCompiler::Huh(const char *expected, int found) {
	mLexer.ThrowError("Expected %s, found '%s'", expected, TokenName(found).c_str());
}

std::string GLCCompiler::TokenName(int tok) {
	switch(tok) {
	case kTokenIdent:
		return "identifier";

	case kTokenInteger:
		return "integer";

	case kTokenDouble:
		return "real";

	case kTokenForeignCode:
		return "foreign code";

	case '\n':
		return "end-of-line";

	default:
		{
			char buf[2]= { (char)tok, 0 };
			return buf;
		}
		break;
	}
}

///////////////////////////////////////////////////////////////////////////////
void tool_glc(const vdfastvector<const char *>& args, const vdfastvector<const char *>& switches) {
	if (args.size() < 2) {
		printf("usage: glc <binary file> <.cpp output file>\n");
		exit(5);
	}

	printf("Asuka: Compiling effect file (OpenGL): %s -> %s.\n", args[0], args[1]);

	FILE *f = fopen(args[0], "rb");
	if (!f)
		fail("    couldn't open: %s\n", args[0]);
	fseek(f, 0, SEEK_END);
	size_t l = ftell(f);
	vdfastvector<char> buf(l);
	fseek(f, 0, SEEK_SET);
	if (!buf.empty())
		fread(&buf[0], l, 1, f);
	fclose(f);

	f = fopen(args[1], "w");
	if (!f)
		fail("    couldn't open: %s\n", args[1]);

	fprintf(f, "// Automatically generated by Asuka from \"%s.\" DO NOT EDIT!\n\n", VDFileSplitPath(args[0]));

	GLCCompiler glc;

	glc.Compile(args[0], buf.data(), buf.size(), f);

	fclose(f);

	printf("Asuka: Compilation was successful.\n");
}
