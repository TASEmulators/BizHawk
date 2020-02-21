//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2009-2011 Avery Lee
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
#include "simulator.h"
#include "uicaptionupdater.h"
#include "versioninfo.h"
#include "console.h"
#include "firmwaremanager.h"
#include "settings.h"

#if defined(WIN32) && defined(ATNRELEASE)
#include <intrin.h>
#endif

///////////////////////////////////////////////////////////////////////////

enum class ATUIWindowCaptionVariable : uint8 {
	None,
	IsTempProfile,
	IsDefaultProfile,
	MainTitle,
	IsDebugging,
	IsRunning,
	HardwareType,
	U1mb,
	KernelType,
	Is5200,
	VideoType,
	Vbxe,
	Rapidus,
	ExtCpu,
	MemoryType,
	Basic,
	Frame,
	Fps,
	HostCpu,
	ShowFps,
	MouseCapture,
	ProfileName,
};

enum class ATUIWindowCaptionInsn : uint8 {
	End,
	LiteralShort,		// stack[0] += litpool[*ip++]
	LiteralLong,		// stack[0] += litpool[*ip++]
	Variable,			// stack[0] += Variable(*ip++)
	AppendRope,			// stack[0] += stack[*ip++]
	PushRope,			//
	Skip,				// ip += *ip + 1;
	SkipZ,				// ip += (stack[0] ? 1 : 0) * (*ip) + 1
	CheckInterp,		// stack[0] = IsInterpolated(stack[0]) ? stack[0] : ""
	Invert,				//
};

struct ATUIWindowCaptionTemplate {
	vdfastvector<uint8> mInsns;
	VDStringW mStrHeap;
};

class ATUIWindowCaptionTemplateParser {
public:
	bool Parse(const char *s);

	ATUIWindowCaptionTemplate GetTemplate();
	sint32 GetErrorPos();

private:
	enum class Token : int {
		End = 0,
		EndOfLine = 256,
		Error,
		String,
		Variable,
	};

	bool ParseBlock();
	bool ParseExpression();
	bool ParseTerm(bool *readToken);
	Token NextToken();
	void EmitInsn(ATUIWindowCaptionInsn insn);
	void EmitInsn(ATUIWindowCaptionInsn insn, uint8 data);
	void EmitData(uint8 data);
	bool SetError();

	VDStringW mStrValue;
	VDStringA mTokBuffer;
	sint32 mIntValue = 0;
	Token mPushedToken {};

	const char *mpSrc0 = nullptr;
	const char *mpSrc = nullptr;
	const char *mpLiteralTokenPos = nullptr;
	const char *mpTokenPos = nullptr;
	sint32 mErrorPos = -1;

	ATUIWindowCaptionTemplate mTemplate;
};

bool ATUIWindowCaptionTemplateParser::Parse(const char *s) {
	// Grammar:
	//
	//	template := expression
	//	expression := unary-expression+
	//	unary-expression := "~" expression
	//	                  | value
	//	value := "(" expression ")"
	//	       | variable-name
	//	       | literal-string

	mpSrc0 = s;
	mpSrc = s;

	EmitInsn(ATUIWindowCaptionInsn::PushRope);
	if (!ParseBlock())
		return false;
	EmitInsn(ATUIWindowCaptionInsn::End);
	return true;
}

ATUIWindowCaptionTemplate ATUIWindowCaptionTemplateParser::GetTemplate() {
	return std::move(mTemplate);
}

sint32 ATUIWindowCaptionTemplateParser::GetErrorPos() {
	return mErrorPos;
}

bool ATUIWindowCaptionTemplateParser::ParseBlock() {
	for(;;) {
		Token tok = NextToken();

		if (tok == Token::End)
			break;

		if (tok != Token::EndOfLine) {
			mPushedToken = tok;

			if (tok == (Token)')')
				break;

			if (!ParseExpression())
				return false;
		}
	}

	return true;
}

bool ATUIWindowCaptionTemplateParser::ParseExpression() {
	EmitInsn(ATUIWindowCaptionInsn::PushRope);

	if (!ParseTerm(nullptr))
		return false;

	for(;;) {
		Token tok = NextToken();

		if (tok == (Token)'?') {
			EmitInsn(ATUIWindowCaptionInsn::SkipZ, 0);
			const auto pos1 = mTemplate.mInsns.size();

			if (!ParseExpression())
				return false;

			tok = NextToken();
			if (tok == (Token)':') {
				EmitInsn(ATUIWindowCaptionInsn::Skip, 0);
				const auto pos2 = mTemplate.mInsns.size();

				uint32 delta1 = mTemplate.mInsns.size() - pos1;
				if (delta1 > 255)
					return SetError();
				mTemplate.mInsns[pos1 - 1] = (uint8)delta1;

				if (!ParseExpression())
					return false;

				uint32 delta2 = mTemplate.mInsns.size() - pos2;
				if (delta2 > 255)
					return SetError();
				mTemplate.mInsns[pos2 - 1] = (uint8)delta2;

				mPushedToken = NextToken();
			} else {
				uint32 delta1 = mTemplate.mInsns.size() - pos1;
				if (delta1 > 255)
					return SetError();
				mTemplate.mInsns[pos1 - 1] = (uint8)delta1;

				mPushedToken = tok;
			}

			return true;
		} else {
			mPushedToken = tok;
		}

		if (mPushedToken == Token::EndOfLine)
			break;

		bool readToken;
		if (!ParseTerm(&readToken))
			return false;

		if (!readToken)
			break;
	}

	EmitInsn(ATUIWindowCaptionInsn::AppendRope);
	return true;
}

bool ATUIWindowCaptionTemplateParser::ParseTerm(bool *readToken) {
	Token tok = NextToken();

	if (readToken)
		*readToken = true;

	if (tok == (Token)'~') {
		EmitInsn(ATUIWindowCaptionInsn::PushRope);

		if (!ParseTerm(nullptr))
			return false;

		EmitInsn(ATUIWindowCaptionInsn::CheckInterp);
		EmitInsn(ATUIWindowCaptionInsn::AppendRope);
		return true;
	} else if (tok == (Token)'!') {
		EmitInsn(ATUIWindowCaptionInsn::PushRope);

		if (!ParseTerm(nullptr))
			return false;

		EmitInsn(ATUIWindowCaptionInsn::Invert);
		EmitInsn(ATUIWindowCaptionInsn::AppendRope);
		return true;
	} else if (tok == Token::String) {
		VDStringW str = mStrValue;
		uint32 spanStart = 0;
		uint32 srcLen = (uint32)str.size();

		// we need to save this off as it will become invalidated after
		// first interpolation
		const char *tokenPos = mpTokenPos;

		while(spanStart < srcLen) {
			uint32 spanEnd = spanStart;
			bool interpStart = false;

			while(spanEnd < srcLen) {
				if (str[spanEnd] == L'$' && spanEnd + 1 < srcLen && str[spanEnd + 1] == L'{') {
					interpStart = true;
					break;
				}

				++spanEnd;
			}

			const uint32 heapStart = (uint16)mTemplate.mStrHeap.size();
			const uint32 spanLen = spanEnd - spanStart;

			if (spanLen) {
				if ((heapStart | spanLen) < 0x100) {
					EmitInsn(ATUIWindowCaptionInsn::LiteralShort, (uint8)heapStart);
					EmitData((uint8)spanLen);
				} else {
					EmitInsn(ATUIWindowCaptionInsn::LiteralLong, (uint8)heapStart);
					EmitData((uint8)(heapStart >> 8));
					EmitData((uint8)(spanLen));
					EmitData((uint8)(spanLen >> 8));
				}

				mTemplate.mStrHeap.append(str.begin() + spanStart, str.begin() + spanEnd);
			}

			spanStart = spanEnd;

			if (interpStart) {
				spanStart += 2;

				for(;;) {
					if (spanEnd >= srcLen)
						return SetError();

					if (str[spanEnd] == L'}')
						break;

					++spanEnd;
				}

				str[spanEnd++] = 0;

				VDStringW interpStr(str.begin() + spanStart, str.begin() + spanEnd);
				VDStringA interpStr2 = VDTextWToA(interpStr);
				const char *prevSrc = mpSrc;
				bool base = false;

				if (!mpLiteralTokenPos) {
					mpLiteralTokenPos = tokenPos;
					base = true;
				}

				mpSrc = interpStr2.c_str();

				if (!ParseBlock())
					break;

				mpSrc = prevSrc;

				if (base)
					mpLiteralTokenPos = nullptr;

				spanStart = spanEnd;
			}
		}

		return true;
	} else if (tok == Token::Variable) {
		EmitInsn(ATUIWindowCaptionInsn::Variable, (uint8)mIntValue);
		return true;
	} else if (tok == (Token)'(') {
		if (!ParseBlock())
			return false;

		tok = NextToken();
		if (tok != (Token)')')
			return SetError();

		return true;
	}

	if (!readToken)
		return SetError();

	mPushedToken = tok;
	*readToken = false;

	return true;
}

ATUIWindowCaptionTemplateParser::Token ATUIWindowCaptionTemplateParser::NextToken() {
	if (mPushedToken != Token()) {
		Token tok = mPushedToken;
		mPushedToken = {};

		return tok;
	}

	char c;

	for(;;) {
		c = *mpSrc;

		if (!c) {
			mpTokenPos = mpSrc;
			return Token::End;
		}

		++mpSrc;

		if (c == '/' && *mpSrc == '/') {
			++mpSrc;

			for(;;) {
				c = *mpSrc;

				if (!c) {
					mpTokenPos = mpSrc;
					return Token::End;
				}
			
				++mpSrc;
				
				if (c == '\n') {
					mpTokenPos = mpSrc - 1;
					return Token::EndOfLine;
				}
			}
		}

		if (c != ' ' && c != '\t' && c != '\r')
			break;
	}

	mpTokenPos = mpSrc - 1;

	if (c == '\n')
		return Token::EndOfLine;

	if (c == '~' || c == '?' || c == ':' || c == '!' || c == '(' || c == ')')
		return (Token)c;

	if (c == '"') {
		mTokBuffer.clear();

		for(;;) {
			c = *mpSrc;

			if (!c || c == '\r' || c == '\n') {
				SetError();
				return Token::Error;
			}

			++mpSrc;

			if (c == '"')
				break;

			if (c == '\\') {
				c = *mpSrc;

				if (c != '"') {
					SetError();
					return Token::Error;
				}

				++mpSrc;
			}

			mTokBuffer.push_back(c);
		}

		mStrValue = VDTextU8ToW(mTokBuffer);
		return Token::String;
	}

	if (isalpha((unsigned char)c)) {
		const char *start = mpSrc - 1;

		for(;;) {
			c = *mpSrc;

			if (!isalnum((unsigned char)c))
				break;

			++mpSrc;
		}

		mTokBuffer.assign(start, mpSrc);

		for(char& c : mTokBuffer)
			c = tolower((unsigned char)c);

		if (mTokBuffer == "istempprofile")		mIntValue = (sint32)ATUIWindowCaptionVariable::IsTempProfile;
		else if (mTokBuffer == "isdefaultprofile")	mIntValue = (sint32)ATUIWindowCaptionVariable::IsDefaultProfile;
		else if (mTokBuffer == "maintitle")		mIntValue = (sint32)ATUIWindowCaptionVariable::MainTitle;
		else if (mTokBuffer == "isdebugging")	mIntValue = (sint32)ATUIWindowCaptionVariable::IsDebugging;
		else if (mTokBuffer == "isrunning")		mIntValue = (sint32)ATUIWindowCaptionVariable::IsRunning;
		else if (mTokBuffer == "hardwaretype")	mIntValue = (sint32)ATUIWindowCaptionVariable::HardwareType;
		else if (mTokBuffer == "u1mb")			mIntValue = (sint32)ATUIWindowCaptionVariable::U1mb;
		else if (mTokBuffer == "kerneltype")	mIntValue = (sint32)ATUIWindowCaptionVariable::KernelType;
		else if (mTokBuffer == "is5200")		mIntValue = (sint32)ATUIWindowCaptionVariable::Is5200;
		else if (mTokBuffer == "videotype")		mIntValue = (sint32)ATUIWindowCaptionVariable::VideoType;
		else if (mTokBuffer == "vbxe")			mIntValue = (sint32)ATUIWindowCaptionVariable::Vbxe;
		else if (mTokBuffer == "rapidus")		mIntValue = (sint32)ATUIWindowCaptionVariable::Rapidus;
		else if (mTokBuffer == "extcpu")		mIntValue = (sint32)ATUIWindowCaptionVariable::ExtCpu;
		else if (mTokBuffer == "memorytype")	mIntValue = (sint32)ATUIWindowCaptionVariable::MemoryType;
		else if (mTokBuffer == "basic")			mIntValue = (sint32)ATUIWindowCaptionVariable::Basic;
		else if (mTokBuffer == "frame")			mIntValue = (sint32)ATUIWindowCaptionVariable::Frame;
		else if (mTokBuffer == "fps")			mIntValue = (sint32)ATUIWindowCaptionVariable::Fps;
		else if (mTokBuffer == "hostcpu")		mIntValue = (sint32)ATUIWindowCaptionVariable::HostCpu;
		else if (mTokBuffer == "showfps")		mIntValue = (sint32)ATUIWindowCaptionVariable::ShowFps;
		else if (mTokBuffer == "mousecapture")	mIntValue = (sint32)ATUIWindowCaptionVariable::MouseCapture;
		else if (mTokBuffer == "profilename")	mIntValue = (sint32)ATUIWindowCaptionVariable::ProfileName;
		else {
			SetError();
			return Token::Error;
		}

		return Token::Variable;
	}

	SetError();
	return Token::Error;
}

void ATUIWindowCaptionTemplateParser::EmitInsn(ATUIWindowCaptionInsn insn) {
	mTemplate.mInsns.push_back((uint8)insn);
}

void ATUIWindowCaptionTemplateParser::EmitInsn(ATUIWindowCaptionInsn insn, uint8 data) {
	mTemplate.mInsns.push_back((uint8)insn);
	mTemplate.mInsns.push_back(data);
}

void ATUIWindowCaptionTemplateParser::EmitData(uint8 data) {
	mTemplate.mInsns.push_back(data);
}

bool ATUIWindowCaptionTemplateParser::SetError() {
	mErrorPos = (mpLiteralTokenPos ? mpLiteralTokenPos : mpTokenPos) - mpSrc0;
	return false;
}

///////////////////////////////////////////////////////////////////////////

sint32 ATUIParseWindowCaptionFormat(const char *str) {
	ATUIWindowCaptionTemplateParser parser;

	if (parser.Parse(str))
		return -1;
	else
		return parser.GetErrorPos();
}

const char *ATUIGetDefaultWindowCaptionTemplate() {
		return R"--(isTempProfile ? "*"
!isDefaultProfile ? "${profileName} - "
mainTitle
isDebugging ? isRunning ? " [running]" : " [stopped]"
": " hardwareType
~" ${!u1mb ? kernelType}"
~" ${!is5200 ? videoType}"
~"+${vbxe}"
~"+${rapidus}"
~"+${extcpu}"
!is5200 ? ~" / ${memorytype}"
~" / ${basic}"
showFps ? " - ${frame} (${fps} fps) (${hostCpu} CPU)"
~" (${mouseCapture})"
)--";
}

///////////////////////////////////////////////////////////////////////////

class ATUIWindowCaptionUpdater final : public vdrefcounted<IATUIWindowCaptionUpdater> {
public:
	ATUIWindowCaptionUpdater();
	~ATUIWindowCaptionUpdater();

	void Init(const vdfunction<void(const wchar_t *)>& fn) override;
	void InitMonitoring(ATSimulator *sim) override;

	bool SetTemplate(const char *s, uint32 *errorPos) override;

	void SetShowFps(bool showFps) override;
	void SetFullScreen(bool fs) override;
	void SetMouseCaptured(bool captured, bool mmbRelease) override;

	void Update(bool running, int ticks, float fps, float cpu) override;
	void CheckForStateChange(bool force) override;

protected:
	void ExecuteTemplateCode();
	void MarkDirty();
	template<class T> bool DetectChange(bool& changed, T& cachedValue, const T& currentValue);

	ATSimulator *mpSim = nullptr;
	bool mbLastRunning = false;
	bool mbLastCaptured = false;
	uint32 mLastProfileId = 0;

	bool mbShowFps = false;
	bool mbFullScreen = false;
	bool mbCaptured = false;
	bool mbCaptureMMBRelease = false;
	bool mbPeriodicUpdate = false;
	bool mbDirty = false;

	VDStringW	mMainTitle;
	VDStringW	mTemplate;
	VDStringW	mBuffer;

	struct Fragment {
		uint32 mBufferStart;
		ATUIWindowCaptionVariable mVariable;
	};

	vdfastvector<Fragment> mFragments;

	struct Rope {
		uint32 mFragmentStart;
		bool mbInterpolated;
	};

	uint32			mLastConfigChangeCounter = 0;
	ATHardwareMode	mLastHardwareMode = kATHardwareModeCount;
	uint64			mLastKernelId = 0;
	ATMemoryMode	mLastMemoryMode = kATMemoryModeCount;
	ATVideoStandard	mLastVideoStd = kATVideoStandardCount;
	bool			mbLastBASICState = false;
	bool			mbLastVBXEState = false;
	bool			mbLastSoundBoardState = false;
	bool			mbLastU1MBState = false;
	bool			mbForceUpdate = false;
	bool			mbLastDebugging = false;
	bool			mbLastShowFPS = false;;
	bool			mbTemporaryProfile = false;
	bool			mbDefaultProfile = false;
	ATCPUMode		mLastCPUMode = kATCPUModeCount;
	uint32			mLastCPUSubCycles = 0;

	ATUIWindowCaptionTemplate mTemplateCode;

	vdfunction<void(const wchar_t *)> mpUpdateFn;
};

ATUIWindowCaptionUpdater::ATUIWindowCaptionUpdater() {
	mMainTitle = AT_FULL_VERSION_STR;

	// Check the Win32 page heap is enabled. There is no documented way to do this, so
	// we must crawl TEB -> PEB.NtGlobalFlags and then check for FLG_HEAP_PAGE_ALLOCS.
	// This isn't useful in Release and it might trip scanners, so it's only enabled
	// in Debug and Profile builds.
#if defined(WIN32) && defined(ATNRELEASE)
	bool pageHeap = false;

	#if defined(VD_CPU_AMD64)
		uint64 peb = __readgsqword(0x60);
		uint32 flags = *(const uint32 *)(peb + 0xBC);

		pageHeap = (flags & 0x02000000) != 0;
	#elif defined(VD_CPU_X86)
		uint32 peb = __readfsdword(0x30);
		uint32 flags = *(const uint32 *)(peb + 0x68);

		pageHeap = (flags & 0x02000000) != 0;
	#endif

		if (pageHeap)
			mMainTitle += L" [page heap enabled]";
#endif

	SetTemplate(nullptr, nullptr);
}

ATUIWindowCaptionUpdater::~ATUIWindowCaptionUpdater() {
}

void ATUIWindowCaptionUpdater::Init(const vdfunction<void(const wchar_t *)>& fn) {
	mpUpdateFn = fn;
	mpUpdateFn(mMainTitle.c_str());
}

void ATUIWindowCaptionUpdater::InitMonitoring(ATSimulator *sim) {
	mpSim = sim;

	CheckForStateChange(true);
}

bool ATUIWindowCaptionUpdater::SetTemplate(const char *s, uint32 *errorPos) {
	ATUIWindowCaptionTemplateParser parser;

	if (s && *s) {
		if (!parser.Parse(s)) {
			SetTemplate(nullptr, nullptr);

			if (errorPos)
				*errorPos = parser.GetErrorPos();
			return false;
		}
	} else {
		VDVERIFY(parser.Parse(ATUIGetDefaultWindowCaptionTemplate()));
	}

	mTemplateCode = std::move(parser.GetTemplate());

	MarkDirty();
	return true;
}

void ATUIWindowCaptionUpdater::SetShowFps(bool showFps) {
	if (mbShowFps != showFps) { 
		mbShowFps = showFps;

		MarkDirty();
	}
}

void ATUIWindowCaptionUpdater::SetFullScreen(bool fs) {
	if (mbFullScreen != fs) {
		mbFullScreen = fs;
		MarkDirty();
	}
}

void ATUIWindowCaptionUpdater::SetMouseCaptured(bool captured, bool mmbRelease) {
	if (mbCaptured != captured || mbCaptureMMBRelease != mmbRelease) {
		mbCaptured = captured;
		mbCaptureMMBRelease = mmbRelease;
		MarkDirty();
	}
}

void ATUIWindowCaptionUpdater::Update(bool running, int ticks, float fps, float cpu) {
	bool forceUpdate = false;

	DetectChange(forceUpdate, mbLastRunning, running);
	DetectChange(forceUpdate, mbLastCaptured, mbCaptured);
	DetectChange(forceUpdate, mLastProfileId, ATSettingsGetCurrentProfileId());

	CheckForStateChange(forceUpdate);

	if ((running && mbPeriodicUpdate && !mbFullScreen) || mbForceUpdate) {
		mbForceUpdate = false;

		uint32 lastIdx = 0;

		mBuffer.clear();

		for(const Fragment& fragment : mFragments) {
			mBuffer.append(mTemplate.data() + lastIdx, mTemplate.data() + fragment.mBufferStart);
			lastIdx = fragment.mBufferStart;

			switch(fragment.mVariable) {
				case ATUIWindowCaptionVariable::Frame:
					mBuffer.append_sprintf(L"%u", ticks);
					break;

				case ATUIWindowCaptionVariable::Fps:
					mBuffer.append_sprintf(L"%.3f", fps);
					break;

				case ATUIWindowCaptionVariable::HostCpu:
					mBuffer.append_sprintf(L"%.1f%%", cpu);
					break;
			}
		}

		mBuffer.append(mTemplate.data() + lastIdx, mTemplate.end());

		mpUpdateFn(mBuffer.c_str());
	}
}

void ATUIWindowCaptionUpdater::CheckForStateChange(bool force) {
	if (!mpSim)
		return;

	bool change = force;

	if (mbDirty) {
		mbDirty = false;

		change = true;
	}

	if (DetectChange(change, mLastConfigChangeCounter, mpSim->GetConfigChangeCounter()) || force) {
		DetectChange(change, mLastHardwareMode, mpSim->GetHardwareMode());
		DetectChange(change, mLastKernelId, mpSim->GetActualKernelId());
		DetectChange(change, mLastMemoryMode, mpSim->GetMemoryMode());

		bool basic = mpSim->IsBASICEnabled();

		if (mLastHardwareMode != kATHardwareMode_800XL && mLastHardwareMode != kATHardwareMode_XEGS && mLastHardwareMode != kATHardwareMode_130XE)
			basic = false;

		DetectChange(change, mbLastBASICState, basic);
		DetectChange(change, mLastVideoStd, mpSim->GetVideoStandard());
		DetectChange(change, mbLastVBXEState, mpSim->GetVBXE() != NULL);
		DetectChange(change, mbLastU1MBState, mpSim->IsUltimate1MBEnabled());

		const ATCPUEmulator& cpu = mpSim->GetCPU();
		ATCPUMode cpuMode = cpu.GetCPUMode();
		uint32 cpuSubCycles = cpu.GetSubCycles();

		DetectChange(change, mLastCPUMode, cpuMode);
		DetectChange(change, mLastCPUSubCycles, cpuSubCycles);
	}

	DetectChange(change, mbTemporaryProfile, ATSettingsGetTemporaryProfileMode());
	DetectChange(change, mbDefaultProfile, ATSettingsIsCurrentProfileADefault());
	DetectChange(change, mbLastDebugging, ATIsDebugConsoleActive());

	if (!force && !change)
		return;

	ExecuteTemplateCode();
	//RebuildTemplate();

	mbForceUpdate = true;
}

void ATUIWindowCaptionUpdater::ExecuteTemplateCode() {
	using Insn = ATUIWindowCaptionInsn;

	vdfastvector<Rope> ropeStack;

	const auto popRope = [&] {
		uint32 fragLevel = (uint32)ropeStack.back().mFragmentStart;

		if (fragLevel < mFragments.size())
			mTemplate.resize(mFragments[fragLevel].mBufferStart);

		mFragments.resize(fragLevel);
		ropeStack.pop_back();
	};

	mTemplate.clear();
	mFragments.clear();

	const uint8 *ip = mTemplateCode.mInsns.data();

	for(;;) {
		Insn insn = (Insn)*ip++;

		if (insn == Insn::End)
			break;

		switch(insn) {
			case Insn::LiteralShort:
				mFragments.push_back({(uint32)mTemplate.size(), ATUIWindowCaptionVariable::None});
				{
					const wchar_t *s = mTemplateCode.mStrHeap.data() + ip[0];
					mTemplate.append(s, s + ip[1]);
					ip += 2;
				}
				break;

			case Insn::LiteralLong:
				mFragments.push_back({(uint32)mTemplate.size(), ATUIWindowCaptionVariable::None});
				{
					const wchar_t *s = mTemplateCode.mStrHeap.data() + ip[0] + ((uint32)ip[1] << 8);
					const uint32 len = ip[2] + ((uint32)ip[3] << 8);
					mTemplate.append(s, s + len);
					ip += 4;
				}
				break;

			case Insn::Variable:
				mFragments.push_back(Fragment { (uint32)mTemplate.size(), ATUIWindowCaptionVariable::None});

				switch((ATUIWindowCaptionVariable)*ip++) {
					case ATUIWindowCaptionVariable::IsTempProfile:
						if (mbTemporaryProfile)
							mTemplate.push_back('*');
						break;

					case ATUIWindowCaptionVariable::IsDefaultProfile:
						if (mbDefaultProfile)
							mTemplate.push_back(L'1');
						break;

					case ATUIWindowCaptionVariable::IsDebugging:
						if (ATIsDebugConsoleActive())
							mTemplate.push_back(L'1');
						break;

					case ATUIWindowCaptionVariable::ShowFps:
						if (mbShowFps)
							mTemplate.push_back(L'1');
						break;

					case ATUIWindowCaptionVariable::MainTitle:
						mTemplate += mMainTitle;
						break;

					case ATUIWindowCaptionVariable::IsRunning:
						if (mbLastRunning)
							mTemplate.push_back(L'1');
						break;

					case ATUIWindowCaptionVariable::HardwareType:
						switch(mLastHardwareMode) {
							case kATHardwareMode_800:
								mTemplate += L"800";
								break;

							case kATHardwareMode_800XL:
								mTemplate += L"XL";
								break;

							case kATHardwareMode_130XE:
								mTemplate += L"XE";
								break;

							case kATHardwareMode_1200XL:
								mTemplate += L"1200XL";
								break;

							case kATHardwareMode_XEGS:
								mTemplate += L"XEGS";
								break;

							case kATHardwareMode_5200:
								mTemplate += L"5200";
								break;
						}
						break;

					case ATUIWindowCaptionVariable::U1mb:
						if (mbLastU1MBState)
							mTemplate += L"U1MB";
						break;

					case ATUIWindowCaptionVariable::KernelType:
						{
							ATFirmwareInfo fwInfo;
							if (mpSim->GetFirmwareManager()->GetFirmwareInfo(mLastKernelId, fwInfo)) {
								switch(fwInfo.mId) {
									case kATFirmwareId_Kernel_HLE:
										mTemplate += L"ATOS/HLE";
										break;

									case kATFirmwareId_Kernel_LLE:
										if (mLastHardwareMode == kATHardwareMode_800)
											mTemplate += L"ATOS";
										else
											mTemplate += L"ATOS/800";
										break;

									case kATFirmwareId_Kernel_LLEXL:
										switch(mLastHardwareMode) {
											case kATHardwareMode_800XL:
											case kATHardwareMode_1200XL:
											case kATHardwareMode_XEGS:
											case kATHardwareMode_130XE:
												mTemplate += L"ATOS";
												break;

											default:
												mTemplate += L"ATOS/XL";
												break;
										}
										break;

									case kATFirmwareId_Kernel_816:
										mTemplate += L"ATOS/816";
										break;

									case kATFirmwareId_5200_LLE:
										mTemplate += L"ATOS";
										break;

									default:
										switch(fwInfo.mType) {
											case kATFirmwareType_Kernel800_OSA:
												mTemplate += L"OS-A";
												break;

											case kATFirmwareType_Kernel800_OSB:
												mTemplate += L"OS-B";
												break;

											case kATFirmwareType_KernelXL:
												if (mLastHardwareMode != kATHardwareMode_800XL && mLastHardwareMode != kATHardwareMode_130XE)
													mTemplate += L"XL/XE";
												break;

											case kATFirmwareType_Kernel1200XL:
												if (mLastHardwareMode != kATHardwareMode_1200XL)
													mTemplate += L"1200XL";
												break;

											case kATFirmwareType_KernelXEGS:
												if (mLastHardwareMode != kATHardwareMode_XEGS)
													mTemplate += L"XEGS";
												break;

											case kATFirmwareType_Kernel5200:
												if (mLastHardwareMode != kATHardwareMode_5200)
													mTemplate += L"5200";
												break;
										}
										break;
								}
							}
						}
						break;

					case ATUIWindowCaptionVariable::Is5200:
						if (mLastHardwareMode == kATHardwareMode_5200)
							mTemplate += L"5200";
						break;

					case ATUIWindowCaptionVariable::VideoType:
						switch(mLastVideoStd) {
							case kATVideoStandard_NTSC:
							default:
								mTemplate += L"NTSC";
								break;

							case kATVideoStandard_PAL:
								mTemplate += L"PAL";
								break;

							case kATVideoStandard_SECAM:
								mTemplate += L"SECAM";
								break;

							case kATVideoStandard_NTSC50:
								mTemplate += L"NTSC-50";
								break;

							case kATVideoStandard_PAL60:
								mTemplate += L"PAL-60";
								break;
						}
						break;

					case ATUIWindowCaptionVariable::Vbxe:
						if (mbLastVBXEState)
							mTemplate += L"VBXE";
						break;

					case ATUIWindowCaptionVariable::Rapidus:
						if (mpSim->IsRapidusEnabled())
							mTemplate += L"Rapidus";
						break;

					case ATUIWindowCaptionVariable::ExtCpu:
						switch(mLastCPUMode) {
							case kATCPUMode_65C02:
								mTemplate += L"C02";
								break;

							case kATCPUMode_65C816:
								mTemplate += L"816";
								if (mLastCPUSubCycles > 1)
									mTemplate.append_sprintf(L" @ %.3gMHz", (double)mLastCPUSubCycles * 1.79);
								break;

							default:
								break;
						}
						break;

					case ATUIWindowCaptionVariable::MemoryType:
						switch(mLastMemoryMode) {
							case kATMemoryMode_8K:			mTemplate += L"8K";			break;
							case kATMemoryMode_16K:			mTemplate += L"16K";			break;
							case kATMemoryMode_24K:			mTemplate += L"24K";			break;
							case kATMemoryMode_32K:			mTemplate += L"32K";			break;
							case kATMemoryMode_40K:			mTemplate += L"40K";			break;
							case kATMemoryMode_48K:			mTemplate += L"48K";			break;
							case kATMemoryMode_52K:			mTemplate += L"52K";			break;
							case kATMemoryMode_64K:			mTemplate += L"64K";			break;
							case kATMemoryMode_128K:		mTemplate += L"128K";			break;
							case kATMemoryMode_256K:		mTemplate += L"256K Rambo";	break;
							case kATMemoryMode_320K:		mTemplate += L"320K Rambo";	break;
							case kATMemoryMode_320K_Compy:	mTemplate += L"320K Compy";	break;
							case kATMemoryMode_576K:		mTemplate += L"576K";			break;
							case kATMemoryMode_576K_Compy:	mTemplate += L"576K Compy";	break;
							case kATMemoryMode_1088K:		mTemplate += L"1088K";		break;
						}
						break;

					case ATUIWindowCaptionVariable::Basic:
						if (mbLastBASICState)
							mTemplate += L"BASIC";
						break;

					case ATUIWindowCaptionVariable::MouseCapture:
						if (mbCaptured) {
							if (mbCaptureMMBRelease)
								mTemplate += L"mouse captured - right Alt or MMB to release";
							else
								mTemplate += L"mouse captured - right Alt to release";
						}
						break;

					case ATUIWindowCaptionVariable::ProfileName:
						mTemplate += ATSettingsProfileGetName(ATSettingsGetCurrentProfileId());
						break;

					case ATUIWindowCaptionVariable::Frame:
					case ATUIWindowCaptionVariable::Fps:
					case ATUIWindowCaptionVariable::HostCpu:
						mFragments.back().mVariable = (ATUIWindowCaptionVariable)ip[-1];
						break;
				}

				if (mFragments.back().mVariable == ATUIWindowCaptionVariable::None && mFragments.back().mBufferStart == mTemplate.size())
					mFragments.pop_back();
				else
					ropeStack.back().mbInterpolated = true;
				break;

			case Insn::AppendRope:
				(ropeStack.end() - 2)->mbInterpolated |= ropeStack.back().mbInterpolated;
				ropeStack.pop_back();
				break;

			case Insn::PushRope:
				ropeStack.push_back({(uint32)mFragments.size(), false});
				break;

			case Insn::Skip:
				++ip;
				ip += ip[-1];
				break;

			case Insn::SkipZ:
				++ip;
				if (ropeStack.back().mFragmentStart == mFragments.size())
					ip += ip[-1];
				
				popRope();
				break;

			case Insn::CheckInterp:
				if (!ropeStack.back().mbInterpolated) {
					popRope();

					ropeStack.push_back({(uint32)mFragments.size(), false});
				}
				break;

			case Insn::Invert:
				if (ropeStack.back().mFragmentStart != mFragments.size()) {
					popRope();

					ropeStack.push_back({(uint32)mFragments.size(), false});
				} else {
					mFragments.push_back({(uint32)mTemplate.size(), ATUIWindowCaptionVariable::None});
					mTemplate.push_back('1');
				}

				ropeStack.back().mbInterpolated = false;
				break;
		}
	}

	// optimize the fragment list
	bool lastWasLiteral = false;

	mbPeriodicUpdate = false;

	mFragments.erase(std::remove_if(mFragments.begin(), mFragments.end(),
		[&](const Fragment& f) -> bool {
			switch(f.mVariable) {
				case ATUIWindowCaptionVariable::None:
					if (lastWasLiteral)
						return true;

					lastWasLiteral = false;
					break;

				case ATUIWindowCaptionVariable::Frame:
				case ATUIWindowCaptionVariable::Fps:
				case ATUIWindowCaptionVariable::HostCpu:
					mbPeriodicUpdate = true;
					break;
			}

			return false;
		}
	), mFragments.end());
}

void ATUIWindowCaptionUpdater::MarkDirty() {
	mbDirty = true;
}

template<class T>
bool ATUIWindowCaptionUpdater::DetectChange(bool& changed, T& cachedValue, const T& currentValue) {
	bool localChanged = false;

	if (cachedValue != currentValue) {
		cachedValue = currentValue;
		localChanged = true;
		changed = true;
	}

	return localChanged;
}

///////////////////////////////////////////////////////////////////////////

void ATUICreateWindowCaptionUpdater(IATUIWindowCaptionUpdater **ptr) {
	*ptr = new ATUIWindowCaptionUpdater;
	(*ptr)->AddRef();
}
