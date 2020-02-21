//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2008-2010 Avery Lee
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
#include <list>
#include <vd2/system/binary.h>
#include <vd2/system/file.h>
#include <vd2/system/filesys.h>
#include <vd2/system/error.h>
#include <vd2/system/math.h>
#include <vd2/system/strutil.h>
#include <vd2/system/unknown.h>
#include <vd2/system/vdstl_hashset.h>
#include <vd2/system/vdstl_hashmap.h>
#include <at/atcore/address.h>
#include <at/atcore/consoleoutput.h>
#include <at/atcore/device.h>
#include <at/atcore/devicemanager.h>
#include <at/atcore/sioutils.h>
#include <at/atcpu/execstate.h>
#include <at/atdebugger/argparse.h>
#include <at/atdebugger/target.h>
#include <at/atio/cassetteimage.h>
#include <at/atcore/enumparseimpl.h>
#include <at/atcore/vfs.h>
#include <at/atnativeui/uiframe.h>
#include "console.h"
#include "cpu.h"
#include "cpuheatmap.h"
#include "simulator.h"
#include "disasm.h"
#include "disk.h"
#include "debugger.h"
#include "debuggerexp.h"
#include "debuggerlog.h"
#include "decmath.h"
#include "symbols.h"
#include "ksyms.h"
#include "kerneldb.h"
#include "cassette.h"
#include "vbxe.h"
#include "uirender.h"
#include "resource.h"
#include "oshelper.h"
#include "bkptmanager.h"
#include "mmu.h"
#include "verifier.h"
#include "pclink.h"
#include "ide.h"
#include "cassette.h"
#include "slightsid.h"
#include "covox.h"
#include "ultimate1mb.h"
#include "pbi.h"
#include "dragoncart.h"
#include "decmath.h"
#include "profiler.h"
#include "fdc.h"
#include "versioninfo.h"
#include <at/atemulation/riot.h>
#include <at/atemulation/ctc.h>

extern ATSimulator g_sim;

void ATSetFullscreen(bool enabled);
bool ATConsoleCheckBreak();
void ATCreateDebuggerCmdAssemble(uint32 address, IATDebuggerActiveCommand **);
void ATConsoleExecuteCommand(const char *s, bool echo = true);
void ATDebuggerInitCommands();

///////////////////////////////////////////////////////////////////////////////

AT_DEFINE_ENUM_TABLE_BEGIN(ATDebuggerSymbolLoadMode)
	{ ATDebuggerSymbolLoadMode::Default, "default" },	
	{ ATDebuggerSymbolLoadMode::Disabled, "disabled" },
	{ ATDebuggerSymbolLoadMode::Deferred, "deferred" },
	{ ATDebuggerSymbolLoadMode::Enabled, "enabled" },
AT_DEFINE_ENUM_TABLE_END(ATDebuggerSymbolLoadMode, ATDebuggerSymbolLoadMode::Default)

AT_DEFINE_ENUM_TABLE_BEGIN(ATDebuggerScriptAutoLoadMode)
	{ ATDebuggerScriptAutoLoadMode::Default, "default" },	
	{ ATDebuggerScriptAutoLoadMode::Disabled, "disabled" },
	{ ATDebuggerScriptAutoLoadMode::AskToLoad, "asktoload" },
	{ ATDebuggerScriptAutoLoadMode::Enabled, "enabled" },
AT_DEFINE_ENUM_TABLE_END(ATDebuggerScriptAutoLoadMode, ATDebuggerScriptAutoLoadMode::Default)

///////////////////////////////////////////////////////////////////////////////

int ATDebuggerParseArgv(const char *s, vdfastvector<char>& tempstr, vdfastvector<const char *>& argptrs) {
	vdfastvector<size_t> argoffsets;
	const char *t = s;
	for(;;) {
		while(*t && *t == ' ')
			++t;

		if (!*t)
			break;

		argoffsets.push_back(tempstr.size());

		bool allowEscaping = false;
		if (*t == '\\' && t[1] == '"') {
			++t;
			allowEscaping = true;
		}

		if (*t == '"') {
			++t;

			tempstr.push_back('"');
			for(;;) {
				char c = *t;
				if (!c)
					break;
				++t;

				if (c == '"')
					break;

				if (c == '\\' && allowEscaping) {
					c = *t;
					if (!c)
						break;
					++t;

					if (c == 'n')
						c = '\n';
				}

				tempstr.push_back(c);
			}

			tempstr.push_back('"');
		} else {
			const char *start = t;
			while(*t && *t != ' ')
				++t;

			tempstr.insert(tempstr.end(), start, t);
		}

		tempstr.push_back(0);

		if (!*t)
			break;
	}

	const int argc = (int)argoffsets.size();
	argptrs.clear();
	argptrs.resize(argc + 1, NULL);
	for(int i=0; i<argc; ++i)
		argptrs[i] = tempstr.data() + argoffsets[i];

	return (int)argc;
}

void ATDebuggerSerializeArgv(VDStringA& dst, int argc, const char *const *argv) {
	for(int i=0; i<argc; ++i) {
		if (i)
			dst += ' ';

		const char *s = argv[i];

		if (*s == '"') {
			++s;
			bool requiresEscaping = false;

			const char *end;
			for(end = s; *end; ++end) {
				unsigned char c = *end;

				if (c == '"' && !end[1])
					break;

				if (c == '"' || c == '\\')
					requiresEscaping = true;
			}

			if (requiresEscaping)
				dst += '\\';

			dst += '"';
			for(const char *t = s; t != end; ++t) {
				unsigned char c = *t;

				if (c == '"' || c == '\\')
					dst += '\\';

				dst += c;
			}
			dst += '"';
		} else {
			dst += s;
		}
	}
}

class ATDebuggerConsoleOutput : public ATConsoleOutput {
public:
	virtual void WriteLine(const char *s) override {
		mTempLine = s;
		mTempLine += '\n';

		ATConsoleWrite(mTempLine.c_str());
	}

protected:
	VDStringA mTempLine;
};

///////////////////////////////////////////////////////////////////////////////

class ATDebugger final : public IATSimulatorCallback, public IATDebugger, public IATDebuggerSymbolLookup, public IATDeviceChangeCallback {
public:
	ATDebugger();
	~ATDebugger();

	ATBreakpointManager *GetBreakpointManager() { return mpBkptManager; }

	bool IsRunning() const;
	bool AreCommandsQueued() const;
	bool IsSourceModeEnabled() const { return mbSourceMode; }

	const ATDebuggerExprParseOpts& GetExprOpts() const { return mExprOpts; }
	void SetExprOpts(const ATDebuggerExprParseOpts& opts) { mExprOpts = opts; }
	void SetExprTemp(int index, sint32 val) { VDASSERT(index >= 0 && index < 10); mExprTemporaries[index] = val; }

	const char *GetTempString() const { return mTempString.c_str(); }
	void SetTempString(const char *s) { mTempString = s; }

	bool Init();
	void Shutdown();

	bool IsEnabled() const override { return mbEnabled; }
	void SetEnabled(bool enabled) override;

	ATDebuggerScriptAutoLoadMode GetScriptAutoLoadMode() const override;
	void SetScriptAutoLoadMode(ATDebuggerScriptAutoLoadMode mode) override;
	void SetScriptAutoLoadConfirmFn(vdfunction<bool()> fn) override { mpScriptAutoLoadConfirmFn = std::move(fn); }

	void ShowBannerOnce() override;

	bool Tick();

	void SetSourceMode(ATDebugSrcMode sourceMode);
	void Break();
	void Stop();
	void Run(ATDebugSrcMode sourceMode);
	void RunTraced();
	void RunToScanline(int scan);
	void RunToVBI();
	void RunToEndOfFrame();
	void StepInto(ATDebugSrcMode sourceMode, const ATDebuggerStepRange *stepRanges = NULL, uint32 stepRangeCount = 0);
	void StepOver(ATDebugSrcMode sourceMode, const ATDebuggerStepRange *stepRanges = NULL, uint32 stepRangeCount = 0);
	void StepOut(ATDebugSrcMode sourceMode);
	uint16 GetPC() const;
	void SetPC(uint16 pc);
	uint32 GetExtPC() const;
	uint16 GetFramePC() const;
	void SetFramePC(uint16 pc);
	uint32 GetCallStack(ATCallStackFrame *dst, uint32 maxCount);
	void DumpCallStack();
	void DumpState(bool verbose = false, const ATCPUExecState *state = nullptr);

	// breakpoints
	bool ArePCBreakpointsSupported() const;
	bool AreAccessBreakpointsSupported() const;
	bool IsDeferredBreakpointSet(const char *fn, uint32 line);
	bool ClearUserBreakpoint(uint32 useridx);
	void ClearOnResetBreakpoints();
	void ClearAllBreakpoints();
	bool IsBreakpointAtPC(uint32 pc) const;
	void ToggleBreakpoint(uint32 addr);
	void ToggleAccessBreakpoint(uint32 addr, bool write);
	void ToggleSourceBreakpoint(const char *fn, uint32 line);
	sint32 LookupUserBreakpoint(uint32 useridx) const;
	sint32 LookupUserBreakpointByNum(uint32 number, const char *groupName) const;
	sint32 LookupUserBreakpointByAddr(uint32 address) const;
	uint32 SetSourceBreakpoint(const char *fn, uint32 line, ATDebugExpNode *condexp, const char *command, bool continueExecution = false);
	uint32 SetConditionalBreakpoint(ATDebugExpNode *exp, const char *command = NULL, bool continueExecution = false);
	void SetBreakpointClearOnReset(uint32 useridx, bool clearOnReset);
	void SetBreakpointOneShot(uint32 useridx, bool oneShot);
	sint32 RegisterUserBreakpoint(uint32 useridx, const char *group);
	uint32 RegisterSystemBreakpoint(uint32 sysidx, ATDebugExpNode *condexp = NULL, const char *command = NULL, bool continueExecution = false);
	vdvector<VDStringA> GetBreakpointGroups() const;
	VDStringA GetBreakpointName(uint32 useridx) const;
	bool GetBreakpointInfo(uint32 useridx, ATDebuggerBreakpointInfo& info) const;
	void GetBreakpointList(vdfastvector<uint32>& bps, const char *group = nullptr) const;
	ATDebugExpNode *GetBreakpointCondition(uint32 useridx) const;
	void SetBreakpointCondition(uint32 useridx, vdautoptr<ATDebugExpNode>& expr);
	const char *GetBreakpointCommand(uint32 useridx) const;
	bool GetBreakpointSourceLocation(uint32 useridx, VDStringA& file, uint32& line) const;

	bool IsBreakOnEXERunAddrEnabled() const { return mbBreakOnEXERunAddr; }
	void SetBreakOnEXERunAddrEnabled(bool en) { mbBreakOnEXERunAddr = en; }

	int AddWatch(uint32 address, int length);
	int AddWatchExpr(ATDebugExpNode *expr);
	bool ClearWatch(int idx);
	void ClearAllWatches();
	bool GetWatchInfo(int idx, ATDebuggerWatchInfo& info);

	void ListModules();
	void ReloadModules();

	void DumpCIOParameters();
	void DumpSIOParameters();

	bool IsCIOTracingEnabled() const { return mSysBPTraceCIO > 0; }
	bool IsSIOTracingEnabled() const { return mSysBPTraceSIO > 0; }

	void SetCIOTracingEnabled(bool enabled);
	void SetSIOTracingEnabled(bool enabled);

	// symbol handling
	void GetModuleIds(vdfastvector<uint32>& ids) const;

	uint32 AddModule(uint32 targetId, uint32 base, uint32 size, IATSymbolStore *symbolStore, const char *name, const wchar_t *path);
	void RemoveModule(uint32 base, uint32 size, IATSymbolStore *symbolStore);
	const char *GetModuleShortName(uint32 moduleId) const;
	uint32 GetModuleTargetId(uint32 moduleId) const;

	void AddClient(IATDebuggerClient *client, bool requestUpdate);
	void RemoveClient(IATDebuggerClient *client);
	void RequestClientUpdate(IATDebuggerClient *client);

	ATDebuggerSymbolLoadMode GetSymbolLoadMode(bool whenEnabled) const override;
	void SetSymbolLoadMode(bool whenEnabled, ATDebuggerSymbolLoadMode mode) override;

	bool IsSymbolLoadingEnabled() const override;
	uint32 LoadSymbols(const wchar_t *fileName, bool processDirectives, const uint32 *targetIdOverride = nullptr, bool loadImmediately = false);
	void UnloadSymbols(uint32 moduleId);
	void LoadDeferredSymbols(uint32 moduleId);
	void LoadAllDeferredSymbols();
	void ClearSymbolDirectives(uint32 moduleId);
	void ProcessSymbolDirectives(uint32 id);

	sint32 ResolveSourceLocation(const char *fn, uint32 line);
	sint32 ResolveSymbol(const char *s, bool allowGlobal = false, bool allowShortBase = true, bool allowNakedHex = true);
	uint32 ResolveSymbolThrow(const char *s, bool allowGlobal = false, bool allowShortBase = true);
	void EnumModuleSymbols(uint32 moduleId, ATCallbackHandler1<void, const ATSymbolInfo&> callback) const;
	void EnumSourceFiles(const vdfunction<void(const wchar_t *, uint32)>& fn) const;

	uint32 AddCustomModule(uint32 targetId, const char *name, const char *shortname);
	uint32 GetCustomModuleIdByShortName(const char *name);
	void AddCustomSymbol(uint32 address, uint32 len, const char *name, uint32 rwxmode, uint32 moduleId = 0);
	void RemoveCustomSymbol(uint32 address);
	void LoadCustomSymbols(const wchar_t *filename);
	void SaveCustomSymbols(const wchar_t *filename);

	VDStringA GetAddressText(uint32 globalAddr, bool useHexSpecifier, bool addSymbolInfo = false);

	sint32 EvaluateThrow(const char *s);
	std::pair<bool, sint32> Evaluate(ATDebugExpNode *expr);

	ATDebugExpEvalContext GetEvalContext() const;
	ATDebugExpEvalContext GetEvalContextForTarget(uint32 targetIndex) const;

	void GetDirtyStorage(vdfastvector<ATDebuggerStorageId>& ids) const;

	void QueueBatchFile(const wchar_t *s);
	void QueueAutoLoadBatchFile(const wchar_t *s);

	bool InvokeCommand(const char *name, ATDebuggerCmdParser& parser) const;
	void AddCommand(const char *name, void (*pfn)(ATDebuggerCmdParser&));

	void QueueCommand(const char *s, bool echo) {
		mCommandQueue.push_back(VDStringA());
		VDStringA& t = mCommandQueue.back();
		
		t.push_back(echo ? 'e' : ' ');
		t += s;
	}

	void QueueCommandFront(const char *s, bool echo) {
		mCommandQueue.push_front(VDStringA());
		VDStringA& t = mCommandQueue.front();
		
		t.push_back(echo ? 'e' : ' ');
		t += s;
	}

	void ExecuteCommand(const char *s) {
		if (mActiveCommands.empty())
			return;

		IATDebuggerActiveCommand *cmd = mActiveCommands.back();

		if (!cmd->ProcessSubCommand(s)) {
			cmd->EndCommand();
			cmd->Release();
			mActiveCommands.pop_back();
		}

		UpdatePrompt();
	}

	IATDebuggerActiveCommand *GetActiveCommand() {
		return mActiveCommands.empty() ? NULL : mActiveCommands.back();
	}

	void StartActiveCommand(IATDebuggerActiveCommand *cmd) {
		cmd->AddRef();
		mActiveCommands.push_back(cmd);

		cmd->BeginCommand(this);

		UpdatePrompt();
	}

	void TerminateActiveCommands() {
		while(!mActiveCommands.empty()) {
			IATDebuggerActiveCommand *cmd = mActiveCommands.back();
			mActiveCommands.pop_back();
			cmd->EndCommand();
			cmd->Release();
		}
	}

	const char *GetRepeatCommand() const { return mRepeatCommand.c_str(); }
	void SetRepeatCommand(const char *s) { mRepeatCommand = s; }

	uint32 GetContinuationAddress() const { return mContinuationAddress; }
	void SetContinuationAddress(uint32 addr) { mContinuationAddress = addr; }

	void DefineCommands(const ATDebuggerCmdDef *defs, size_t numDefs) override;

	bool IsCommandAliasPresent(const char *alias) const;
	bool MatchCommandAlias(const char *alias, const char *const *argv, int argc, vdfastvector<char>& tempstr, vdfastvector<const char *>& argptrs) const;
	const char *GetCommandAlias(const char *alias, const char *args) const;
	void SetCommandAlias(const char *alias, const char *args, const char *command);
	void ListCommandAliases();
	void ClearCommandAliases();

	void OnExeQueueCmd(bool onrun, const char *s);
	void OnExeClear();
	bool OnExeGetCmd(bool onrun, int index, VDStringA& s);

	void WriteMemoryCPU(uint16 address, const void *data, uint32 len) {
		const uint8 *data8 = (const uint8 *)data;

		while(len--)
			mpCurrentTarget->WriteByte((uint16)(address++), *data8++);
	}

	void WriteGlobalMemory(uint32 address, const void *data, uint32 len) {
		const uint8 *data8 = (const uint8 *)data;
		uint32 aspace = address & kATAddressSpaceMask;

		while(len--)
			mpCurrentTarget->WriteByte((address++ & kATAddressOffsetMask) + aspace, *data8++);
	}

	VDEvent<IATDebugger, const char *>& OnPromptChanged() { return mEventPromptChanged; }

	const char *GetPrompt() const {
		return mPrompt.c_str();
	}

	void UpdatePrompt() {
		if (mActiveCommands.empty())
			SetPromptDefault();
		else {
			IATDebuggerActiveCommand *cmd = mActiveCommands.back();

			if (cmd->IsBusy())
				SetPrompt("BUSY");
			else
				SetPrompt(cmd->GetPrompt());
		}
	}

	void SetPromptDefault() {
		if (mCurrentTargetIndex || mDebugTargets.size() > 1) {
			VDStringA s;

			s.sprintf("Altirra:%u", mCurrentTargetIndex);
			SetPrompt(s.c_str());
		} else
			SetPrompt("Altirra");
	}

	void SetPrompt(const char *prompt) {
		if (mPrompt != prompt) {
			mPrompt = prompt;
			mEventPromptChanged.Raise(this, prompt);
		}
	}

	bool SetTarget(uint32 index);
	uint32 GetTargetIndex() const { return mCurrentTargetIndex; }
	IATDebugTarget *GetTarget() const { return mpCurrentTarget; }
	void GetTargetList(vdfastvector<IATDebugTarget *>& targets);

	VDEvent<IATDebugger, bool>& OnRunStateChanged() { return mEventRunStateChanged; }
	VDEvent<IATDebugger, ATDebuggerOpenEvent *>& OnDebuggerOpen() { return mEventOpen; }

	void SendRegisterUpdate() {
		mbClientUpdatePending = true;
	}

	enum {
		kModuleId_KernelDB = 1,
		kModuleId_KernelROM,
		kModuleId_Hardware,
		kModuleId_Manual,
		kModuleId_Custom
	};

public:
	bool GetSourceFilePath(uint32 moduleId, uint16 fileId, VDStringW& path);
	bool LookupSymbol(uint32 moduleOffset, uint32 flags, ATSymbol& symbol);
	bool LookupSymbol(uint32 moduleOffset, uint32 flags, ATDebuggerSymbol& symbol);
	bool LookupLine(uint32 addr, bool searchUp, uint32& moduleId, ATSourceLineInfo& lineInfo);
	bool LookupFile(const wchar_t *fileName, uint32& moduleId, uint16& fileId);
	void GetLinesForFile(uint32 moduleId, uint16 fileId, vdfastvector<ATSourceLineInfo>& lines);

public:
	void OnSimulatorEvent(ATSimulatorEvent ev) override;

public:
	void OnDeviceAdded(uint32 iid, IATDevice *dev, void *iface) override;
	void OnDeviceRemoving(uint32 iid, IATDevice *dev, void *iface) override;
	void OnDeviceRemoved(uint32 iid, IATDevice *dev, void *iface) override;

protected:
	struct Module {
		uint32	mId = 0;
		uint32	mTargetId = 0;
		uint32	mBase = 0;
		uint32	mSize = 0;
		bool	mbDirty = false;
		bool	mbDeferredLoad = false;
		bool	mbDirectivesProcessed = false;
		vdrefptr<IATSymbolStore> mpSymbols;
		VDStringA	mShortName;
		VDStringA	mName;
		VDStringW	mPath;
		vdfastvector<uint16> mSilentlyIgnoredFiles;
	};
	
	struct UserBP;

	void UpdateSymbolLoadMode();
	void LoadDeferredSymbols(Module& module);
	void ResolveDeferredBreakpoints();
	void ClearAllBreakpoints(bool notify);
	void UpdateClientSystemState(IATDebuggerClient *client = NULL);
	void ActivateSourceWindow();
	Module *GetModuleById(uint32 id);
	const Module *GetModuleById(uint32 id) const;
	void NotifyEvent(ATDebugEvent eventId);
	void OnBreakpointHit(ATBreakpointManager *sender, ATBreakpointEvent *event);

	void OnTargetStepComplete(uint32 targetIndex, bool successful);

	void SetupRangeStep(bool stepInto, const ATDebuggerStepRange *stepRanges, uint32 stepRangeCount);
	static ATCPUStepResult CPUSourceStepCallback(ATCPUEmulator *cpu, uint32 pc, bool call, void *data);

	void InterruptRun(bool leaveRunning = false);

	uint32 GetCallStack6502(ATCallStackFrame *dst, uint32 maxCount);
	uint32 GetCallStackZ80(ATCallStackFrame *dst, uint32 maxCount);

	enum RunState {
		kRunState_Stopped,
		kRunState_StepInto,
		kRunState_StepOver,
		kRunState_StepOut,
		kRunState_Run,
		kRunState_RunToScanline,
		kRunState_RunToVBI1,
		kRunState_RunToVBI2,
		kRunState_RunToEndOfFrame,
		kRunState_TargetStepInto,
		kRunState_TargetStepComplete
	} mRunState;

	uint32	mNextModuleId;
	uint16	mFramePC;
	uint32	mSysBPTraceCIO = 0;
	uint32	mSysBPTraceSIO = 0;
	uint32	mSysBPEEXRun;
	bool	mbSourceMode;
	bool	mbBreakOnEXERunAddr;
	bool	mbClientUpdatePending;
	bool	mbClientLastRunState;
	bool	mbSymbolUpdatePending;
	bool	mbEnabled;
	bool	mbBannerDisplayed = false;
	bool	mbSymbolLoadingEnabled = true;
	bool	mbDeferredSymbolLoadingEnabled = false;

	ATDebuggerSymbolLoadMode mSymbolLoadModes[2] {};
	ATDebuggerScriptAutoLoadMode mScriptAutoLoadMode = ATDebuggerScriptAutoLoadMode::AskToLoad;

	VDStringA	mRepeatCommand;
	uint32	mContinuationAddress;

	uint32	mExprAddress;
	uint8	mExprValue;

	sint32	mExprTemporaries[10];

	VDStringA mTempString;

	typedef std::list<Module> Modules; 
	Modules		mModules;

	typedef std::vector<IATDebuggerClient *> Clients;
	Clients mClients;
	int mClientsBusy;
	bool mbClientsChanged;

	ATDebuggerExprParseOpts mExprOpts;

	struct WatchInfo {
		uint32 mAddress;
		sint32 mLength;
		uint32 mTargetIndex;
		vdautoptr<ATDebugExpNode> mpExpr;
	};

	WatchInfo mWatches[8];

	VDStringA	mPrompt;
	VDEvent<IATDebugger, const char *> mEventPromptChanged;
	VDEvent<IATDebugger, bool> mEventRunStateChanged;
	VDEvent<IATDebugger, ATDebuggerOpenEvent *> mEventOpen;

	typedef vdfastvector<IATDebuggerActiveCommand *> ActiveCommands;
	ActiveCommands mActiveCommands;

	struct UserBP {
		uint32	mSysBP;
		uint32	mTargetIndex;
		uint32	mModuleId;
		ATDebugExpNode	*mpCondition;
		VDStringA mCommand;
		VDStringA mSource;
		uint32 mSourceLine;
		bool mbContinueExecution;
		bool mbClearOnReset;
		bool mbOneShot;
		sint32 mTagNumber;
		const char *mpTagName;
	};

	struct UserBPFreePred {
		bool operator()(const UserBP& x) {
			return x.mSysBP == (uint32)-1;
		}
	};

	typedef vdvector<sint32> UserBPGroup;
	UserBPGroup mUserNumberedBPs;

	typedef vdhashmap<VDStringA, UserBPGroup, vdhash<VDStringA>, vdstringpred> UserBPGroups;
	UserBPGroups mUserBPGroups;

	typedef vdvector<UserBP> UserBPs;
	UserBPs mUserBPs;

	typedef vdhashmap<uint32, uint32> SysBPToUserBPMap;
	SysBPToUserBPMap mSysBPToUserBPMap;

	ATBreakpointManager *mpBkptManager;
	VDDelegate	mDelBreakpointHit;

	std::deque<VDStringA> mCommandQueue;

	struct AliasSorter;
	typedef vdvector<std::pair<VDStringA, VDStringA> > AliasList;

	typedef vdhashmap<VDStringA, AliasList, vdhash<VDStringA>, vdstringpred> Aliases;
	Aliases mAliases;

	typedef vdhashmap<VDStringA, void (*)(ATDebuggerCmdParser&)> Commands;
	Commands mCommands;

	typedef std::deque<VDStringA> OnExeCmds;
	OnExeCmds mOnExeCmds[2];

	typedef vdfastvector<ATDebuggerStepRange> StepRanges;
	StepRanges mStepRanges;
	uint8 mStepS;
	bool mbStepInto;

	IATDebugTarget *mpCurrentTarget;
	uint32 mCurrentTargetIndex;
	vdfastvector<IATDebugTarget *> mDebugTargets;
	vdfunction<bool()> mpScriptAutoLoadConfirmFn;
};

ATDebugger g_debugger;

IATDebugger *ATGetDebugger() { return &g_debugger; }
IATDebuggerSymbolLookup *ATGetDebuggerSymbolLookup() { return &g_debugger; }

void ATInitDebugger() {
	g_debugger.Init();
}

void ATShutdownDebugger() {
	g_debugger.Shutdown();
}

ATDebugger::ATDebugger()
	: mRunState(kRunState_Run)
	, mNextModuleId(kModuleId_Custom)
	, mFramePC(0)
	, mSysBPEEXRun(0)
	, mbSourceMode(false)
	, mExprAddress(0)
	, mExprValue(0)
	, mbClientUpdatePending(false)
	, mbClientLastRunState(false)
	, mbSymbolUpdatePending(false)
	, mContinuationAddress(0)
	, mClientsBusy(0)
	, mbClientsChanged(false)
	, mpBkptManager(NULL)
{
	SetSymbolLoadMode(false, ATDebuggerSymbolLoadMode::Default);
	SetSymbolLoadMode(true, ATDebuggerSymbolLoadMode::Default);
	SetPromptDefault();

	for(auto& watch : mWatches)
		watch.mLength = -1;

	mExprOpts.mbDefaultHex = false;
	mExprOpts.mbAllowUntaggedHex = true;

	memset(mExprTemporaries, 0, sizeof mExprTemporaries);

	ATDebuggerInitCommands();
}

ATDebugger::~ATDebugger() {
	TerminateActiveCommands();
}

bool ATDebugger::IsRunning() const {
	return g_sim.IsRunning();
}

bool ATDebugger::AreCommandsQueued() const {
	return !mActiveCommands.empty() || !mCommandQueue.empty();
}

bool ATDebugger::Init() {
	g_sim.GetEventManager()->AddCallback(this);

	if (!mpBkptManager) {
		mpBkptManager = new ATBreakpointManager;
		mpBkptManager->Init(&g_sim.GetCPU(), g_sim.GetMemoryManager(), &g_sim);
		mpBkptManager->OnBreakpointHit() += mDelBreakpointHit.Bind(this, &ATDebugger::OnBreakpointHit);
	}

	mDebugTargets = { g_sim.GetDebugTarget() };
	mCurrentTargetIndex = 0;
	mpCurrentTarget = mDebugTargets[0];

	mpBkptManager->AttachTarget(0, mpCurrentTarget);

	auto *pDM = g_sim.GetDeviceManager();
	pDM->AddDeviceChangeCallback(IATDeviceDebugTarget::kTypeID, this);

	for(IATDeviceDebugTarget *dev : pDM->GetInterfaces<IATDeviceDebugTarget>(false, false)) {
		uint32 i=0;
			
		while(IATDebugTarget *target = dev->GetDebugTarget(i++)) {
			if (std::find(mDebugTargets.begin(), mDebugTargets.end(), target) == mDebugTargets.end())
				mDebugTargets.push_back(target);
		}
	}

	UpdatePrompt();
	return true;
}

void ATDebugger::Shutdown() {
	mDebugTargets.clear();
	mpCurrentTarget = nullptr;

	g_sim.GetDeviceManager()->RemoveDeviceChangeCallback(IATDeviceDebugTarget::kTypeID, this);

	ClearAllBreakpoints(false);

	if (mpBkptManager) {
		mpBkptManager->OnBreakpointHit() -= mDelBreakpointHit;
		mpBkptManager->Shutdown();
		delete mpBkptManager;
		mpBkptManager = NULL;
	}
}

void ATDebugger::SetEnabled(bool enabled) {
	mbEnabled = enabled;
	UpdateSymbolLoadMode();

	if (!enabled) {
		TerminateActiveCommands();

		InterruptRun(true);

		ClearAllBreakpoints();
		UpdateClientSystemState();
	}
}

ATDebuggerScriptAutoLoadMode ATDebugger::GetScriptAutoLoadMode() const {
	return mScriptAutoLoadMode;
}

void ATDebugger::SetScriptAutoLoadMode(ATDebuggerScriptAutoLoadMode mode) {
	if (mode == ATDebuggerScriptAutoLoadMode::Default)
		mode = ATDebuggerScriptAutoLoadMode::AskToLoad;

	mScriptAutoLoadMode = mode;
}

void ATDebugger::ShowBannerOnce() {
	if (mbBannerDisplayed)
		return;

	mbBannerDisplayed = true;

	ATConsoleWrite("Altirra Debugger " AT_VERSION "\n");
	ATConsoleWrite("\n");

	if (!mbSymbolLoadingEnabled)
		ATConsoleWrite("Automatic symbol and debug script loading is disabled.\n");
	else if (mbDeferredSymbolLoadingEnabled) {
		ATConsoleWrite(
			"Automatic symbol loading is deferred. Symbol files will be detected when images are\n"
			"loaded but not loaded until requested.\n");
	} else
		ATConsoleWrite("Automatic symbol loading is enabled.\n");

	for(const Module& module : mModules) {
		if (module.mbDeferredLoad) {
			ATConsoleWrite(
				"\n"
				"Some symbol files are in deferred load status. Use lm to query module symbol status\n"
				"and .loadsym or .reload to load deferred symbol files. Symbol load modes can be\n"
				"changed in Configure System.\n"
			);
			break;
		}
	}

	ATConsoleWrite(
		"\n"
		"Use .help for a list of commands and .help <cmdname> for help on a specific command.\n"
		"_______\n"
		"\n"
	);
}

void ATDebugger::SetSourceMode(ATDebugSrcMode sourceMode) {
	switch(sourceMode) {
		case kATDebugSrcMode_Disasm:
			mbSourceMode = false;
			break;

		case kATDebugSrcMode_Source:
			mbSourceMode = true;
			break;

		case kATDebugSrcMode_Same:
			break;
	}
}

bool ATDebugger::Tick() {
	if (g_sim.IsRunning())
		return false;

	if (!mActiveCommands.empty()) {
		IATDebuggerActiveCommand *acmd = mActiveCommands.back();

		if (acmd->IsBusy()) {
			if (!acmd->ProcessSubCommand(NULL)) {
				acmd->EndCommand();
				acmd->Release();
				mActiveCommands.pop_back();

				UpdatePrompt();
			}

			return true;
		}

		if (mCommandQueue.empty())
			return false;
	} else {
		if (mRunState == kRunState_TargetStepInto) {
			// Uh oh... the target still hasn't completed the step operation. Ask it to run a sync and
			// see if that does the trick (we should be stepping the main simulator by single cycles).
			auto *ec = vdpoly_cast<IATDebugTargetExecutionControl *>(mpCurrentTarget);
			if (ec)
				ec->StepUpdate();

			if (mRunState == kRunState_TargetStepInto) {
				g_sim.ResumeSingleCycle();
				return true;
			}
		}

		if (mRunState == kRunState_TargetStepComplete) {
			DumpState();
			mbClientUpdatePending = true;
			mRunState = kRunState_Stopped;
		}

		if (mCommandQueue.empty()) {
			if (!mRunState) {
				if (mbSymbolUpdatePending) {
					mbSymbolUpdatePending = false;

					NotifyEvent(kATDebugEvent_SymbolsChanged);
				}

				if (mbClientUpdatePending)
					UpdateClientSystemState();
			}

			return false;
		}
	}

	VDStringA s;
	s.swap(mCommandQueue.front());
	mCommandQueue.pop_front();

	try {
		const char *t = s.c_str();
		VDASSERT(*t);
		ATConsoleExecuteCommand(t + 1, t[0] == 'e');
	} catch(const MyError& e) {
		ATConsolePrintf("%s\n", e.gets());
	}

	if (mCommandQueue.empty()) {
		if (mRunState) {
			g_sim.Resume();
			return true;
		}
	}

	return true;
}

void ATDebugger::Break() {
	if (g_sim.IsRunning()) {
		g_sim.Suspend();
		mRunState = kRunState_Stopped;

		ATCPUExecState execState;
		mpCurrentTarget->GetExecState(execState);

		if (mpCurrentTarget->GetDisasmMode() == kATDebugDisasmMode_Z80)
			mFramePC = execState.mZ80.mPC;
		else
			mFramePC = execState.m6502.mPC;

		DumpState(false, &execState);

		mbClientUpdatePending = true;
	}

	TerminateActiveCommands();
	mCommandQueue.clear();

	UpdatePrompt();
}

void ATDebugger::Stop() {
	if (!g_sim.IsRunning())
		return;

	g_sim.Suspend();

	mRunState = kRunState_Stopped;

	ATCPUExecState execState;
	mpCurrentTarget->GetExecState(execState);

	if (mpCurrentTarget->GetDisasmMode() == kATDebugDisasmMode_Z80)
		mFramePC = execState.mZ80.mPC;
	else
		mFramePC = execState.m6502.mPC;

	mbClientUpdatePending = true;
}

void ATDebugger::Run(ATDebugSrcMode sourceMode) {
	if (g_sim.IsRunning())
		return;

	SetSourceMode(sourceMode);

	ATCPUEmulator& cpu = g_sim.GetCPU();
	cpu.SetStep(false);
	cpu.SetTrace(false);
	g_sim.Resume();
	mbClientUpdatePending = true;
	mRunState = kRunState_Run;

	if (!mbClientLastRunState)
		UpdateClientSystemState();
}

void ATDebugger::RunTraced() {
	if (g_sim.IsRunning())
		return;

	if (mCurrentTargetIndex != 0)
		throw MyError("Step execution is not available on the current target.");

	ATCPUEmulator& cpu = g_sim.GetCPU();
	cpu.SetStep(false);
	cpu.SetTrace(true);
	g_sim.Resume();
	mbClientUpdatePending = true;
	mRunState = kRunState_Run;

	if (!mbClientLastRunState)
		UpdateClientSystemState();
}

void ATDebugger::RunToScanline(int scan) {
	mRunState = kRunState_RunToScanline;
	mbClientUpdatePending = true;
	g_sim.SetBreakOnScanline(scan);
	g_sim.Resume();

	if (!mbClientLastRunState)
		UpdateClientSystemState();
}

void ATDebugger::RunToVBI() {
	ATCPUEmulator& cpu = g_sim.GetCPU();
	cpu.SetStep(false);
	cpu.SetTrace(false);
	g_sim.SetBreakOnScanline(248);
	g_sim.Resume();
	mbClientUpdatePending = true;
	mRunState = kRunState_RunToVBI1;

	if (!mbClientLastRunState)
		UpdateClientSystemState();
}

void ATDebugger::RunToEndOfFrame() {
	mRunState = kRunState_RunToEndOfFrame;

	g_sim.SetBreakOnFrameEnd(true);
	g_sim.Resume();
}

bool ATDebugger::ArePCBreakpointsSupported() const {
	return mCurrentTargetIndex == 0 || mpBkptManager->AreBreakpointsSupported(mCurrentTargetIndex);
}

bool ATDebugger::AreAccessBreakpointsSupported() const {
	return mCurrentTargetIndex == 0;
}

bool ATDebugger::IsDeferredBreakpointSet(const char *fn, uint32 line) {
	if (!*fn)
		return false;

	UserBPs::iterator it(mUserBPs.begin()), itEnd(mUserBPs.end());
	for(; it != itEnd; ++it) {
		UserBP& ubp = *it;

		if (ubp.mSourceLine == line && ubp.mSource == fn)
			return true;
	}

	return false;
}

bool ATDebugger::ClearUserBreakpoint(uint32 useridx) {
	if (useridx >= mUserBPs.size())
		return false;

	UserBP& bp = mUserBPs[useridx];
	if (bp.mSysBP == (uint32)-1)
		return false;

	bp.mModuleId = 0;

	vdsafedelete <<= bp.mpCondition;

	bp.mSource.clear();

	if ((sint32)bp.mSysBP > 0) {
		mpBkptManager->Clear(bp.mSysBP);

		SysBPToUserBPMap::iterator it(mSysBPToUserBPMap.find(bp.mSysBP));
		if (it != mSysBPToUserBPMap.end())
			mSysBPToUserBPMap.erase(it);
	}

	bp.mSysBP = (uint32)-1;

	UserBPGroup *group = bp.mpTagName ? &mUserBPGroups.find_as(bp.mpTagName)->second : &mUserNumberedBPs;

	auto it2 = std::find(group->begin(), group->end(), (sint32)useridx);
	VDASSERT(it2 != group->end());

	*it2 = -1;

	while(!group->empty() && group->back() == -1)
		group->pop_back();

	return true;
}

void ATDebugger::ClearOnResetBreakpoints() {
	size_t n = mUserBPs.size();
	bool notify = false;

	for(uint32 i=0; i<n; ++i) {
		UserBP& bp = mUserBPs[i];

		if (bp.mbClearOnReset && bp.mSysBP != (uint32)-1) {
			ClearUserBreakpoint(i);
			notify = true;
		}
	}

	if (notify)
		g_sim.NotifyEvent(kATSimEvent_CPUPCBreakpointsUpdated);
}

void ATDebugger::ClearAllBreakpoints() {
	ClearAllBreakpoints(false);
}

void ATDebugger::ClearAllBreakpoints(bool notify) {
	size_t n = mUserBPs.size();

	for(uint32 i=0; i<n; ++i) {
		UserBP& bp = mUserBPs[i];

		if (bp.mSysBP != (uint32)-1)
			ClearUserBreakpoint(i);
	}

	if (notify)
		g_sim.NotifyEvent(kATSimEvent_CPUPCBreakpointsUpdated);
}

bool ATDebugger::IsBreakpointAtPC(uint32 pc) const {
	ATBreakpointIndices indices;

	mpBkptManager->GetAtPC(mCurrentTargetIndex, pc, indices);

	for(const auto& sysidx : indices) {
		auto it = mSysBPToUserBPMap.find(sysidx);

		if (it != mSysBPToUserBPMap.end())
			return true;
	}

	return false;
}

void ATDebugger::ToggleBreakpoint(uint32 addr) {
	ATBreakpointIndices indices;

	mpBkptManager->GetAtPC(mCurrentTargetIndex, addr, indices);

	// try to find an index we know about
	sint32 useridx = -1;
	for(const uint32 sysidx : indices) {
		SysBPToUserBPMap::const_iterator it(mSysBPToUserBPMap.find(sysidx));
		if (it != mSysBPToUserBPMap.end()) {
			if (!mUserBPs[it->second].mpTagName) {
				useridx = it->second;
				break;
			}
		}
	}

	if (useridx >= 0) {
		ClearUserBreakpoint(useridx);
	} else {
		const uint32 sysidx = mpBkptManager->SetAtPC(mCurrentTargetIndex, addr);
		useridx = RegisterSystemBreakpoint(sysidx);
		RegisterUserBreakpoint(useridx, nullptr);
	}

	g_sim.NotifyEvent(kATSimEvent_CPUPCBreakpointsUpdated);
}

void ATDebugger::ToggleAccessBreakpoint(uint32 addr, bool write) {
	ATBreakpointIndices indices;

	mpBkptManager->GetAtAccessAddress(addr, indices);

	// try to find an index we know about and is the right type
	sint32 useridx = -1;
	uint32 sysidx;
	while(!indices.empty()) {
		sysidx = indices.back();
		indices.pop_back();

		ATBreakpointInfo info;
		VDVERIFY(mpBkptManager->GetInfo(sysidx, info));

		if (write) {
			if (!info.mbBreakOnWrite)
				continue;
		} else {
			if (!info.mbBreakOnRead)
				continue;
		}

		SysBPToUserBPMap::const_iterator it(mSysBPToUserBPMap.find(sysidx));
		if (it != mSysBPToUserBPMap.end()) {
			if (!mUserBPs[it->second].mpTagName) {
				useridx = it->second;
				break;
			}
		}
	}

	if (useridx >= 0) {
		ClearUserBreakpoint(useridx);
	} else {
		sysidx = mpBkptManager->SetAccessBP(addr, !write, write);
		const uint32 useridx = RegisterSystemBreakpoint(sysidx);
		RegisterUserBreakpoint(useridx, nullptr);
	}

	g_sim.NotifyEvent(kATSimEvent_CPUPCBreakpointsUpdated);
}

void ATDebugger::ToggleSourceBreakpoint(const char *fn, uint32 line) {
	UserBPs::iterator it(mUserBPs.begin()), itEnd(mUserBPs.end());
	for(; it != itEnd; ++it) {
		UserBP& ubp = *it;

		if (!ubp.mpTagName && ubp.mSourceLine == line && ubp.mSource == fn) {
			ClearUserBreakpoint((uint32)(it - mUserBPs.begin()));
			g_sim.NotifyEvent(kATSimEvent_CPUPCBreakpointsUpdated);
			return;
		}
	}

	sint32 addr = ResolveSourceLocation(fn, line);
	if (addr >= 0) {
		sint32 useridx = LookupUserBreakpointByAddr(addr);
		if (useridx >= 0) {
			ClearUserBreakpoint(useridx);
			g_sim.NotifyEvent(kATSimEvent_CPUPCBreakpointsUpdated);
			return;
		}
	}

	const uint32 useridx = SetSourceBreakpoint(fn, line, NULL, NULL);
	RegisterUserBreakpoint(useridx, nullptr);
	g_sim.NotifyEvent(kATSimEvent_CPUPCBreakpointsUpdated);
}

sint32 ATDebugger::LookupUserBreakpoint(uint32 useridx) const {
	if (useridx >= mUserBPs.size())
		return -1;

	return mUserBPs[useridx].mSysBP;
}

sint32 ATDebugger::LookupUserBreakpointByNum(uint32 number, const char *groupName) const {
	const UserBPGroup *group = &mUserNumberedBPs;

	if (groupName && *groupName) {
		auto it = mUserBPGroups.find_as(groupName);

		if (it == mUserBPGroups.end())
			return -1;

		group = &it->second;
	}

	if (number >= group->size())
		return -1;

	return (*group)[number];
}

sint32 ATDebugger::LookupUserBreakpointByAddr(uint32 address) const {
	ATBreakpointIndices indices;
	mpBkptManager->GetAtPC(mCurrentTargetIndex, address, indices);

	while(!indices.empty()) {
		SysBPToUserBPMap::const_iterator it(mSysBPToUserBPMap.find(indices.back()));

		if (it != mSysBPToUserBPMap.end())
			return it->second;

		indices.pop_back();
	}

	return -1;
}

uint32 ATDebugger::SetSourceBreakpoint(const char *fn, uint32 line, ATDebugExpNode *condexp, const char *command, bool continueExecution) {
	sint32 address = ResolveSourceLocation(fn, line);
	uint32 sysidx = 0;

	if (address >= 0)
		sysidx = mpBkptManager->SetAtPC(mCurrentTargetIndex, address);

	UserBPs::iterator it(std::find_if(mUserBPs.begin(), mUserBPs.end(), UserBPFreePred()));
	uint32 useridx = (uint32)(it - mUserBPs.begin());

	if (it == mUserBPs.end())
		mUserBPs.push_back();

	UserBP& ubp = mUserBPs[useridx];
	ubp.mSysBP = sysidx;
	ubp.mTargetIndex = mCurrentTargetIndex;
	ubp.mpCondition = condexp;
	ubp.mCommand = command ? command : "";
	ubp.mModuleId = 0;
	ubp.mSource = fn;
	ubp.mSourceLine = line;
	ubp.mbContinueExecution = continueExecution;
	ubp.mbClearOnReset = false;
	ubp.mbOneShot = false;
	ubp.mpTagName = nullptr;
	ubp.mTagNumber = 0;

	if (sysidx)
		mSysBPToUserBPMap[sysidx] = useridx;

	return useridx;
}

uint32 ATDebugger::SetConditionalBreakpoint(ATDebugExpNode *exp0, const char *command, bool continueExecution) {
	vdautoptr<ATDebugExpNode> exp(exp0);

	if (exp->mType == kATDebugExpNodeType_Const) {
		sint32 v;

		if (exp->Evaluate(v, ATDebugExpEvalContext())) {
			VDString s;

			exp->ToString(s);

			if (v)
				throw MyError("Error: Condition '%s' is always true.", s.c_str());
			else
				throw MyError("Error: Condition '%s' is always false.", s.c_str());
		}
	}

	vdautoptr<ATDebugExpNode> extpc;
	vdautoptr<ATDebugExpNode> extread;
	vdautoptr<ATDebugExpNode> extwrite;
	vdautoptr<ATDebugExpNode> rem;
	vdautoptr<ATDebugExpNode> rangelo;
	vdautoptr<ATDebugExpNode> rangehi;
	bool isrange = false;
	bool israngewrite;
	bool isinsn = false;
	sint32 rangeloaddr;
	sint32 rangehiaddr;
	sint32 addr;

	if (!exp->ExtractEqConst(kATDebugExpNodeType_Read, ~extread, ~rem) &&
		!exp->ExtractEqConst(kATDebugExpNodeType_Write, ~extwrite, ~rem) &&
		!exp->ExtractEqConst(kATDebugExpNodeType_PC, ~extpc, ~rem))
	{
		// Hmm. Okay, let's see if we can extract a range breakpoint.
		vdautoptr<ATDebugExpNode> temprem;

		ATDebugExpNodeType oplo;
		ATDebugExpNodeType ophi;

		vdautoptr<ATDebugExpNode> expt(exp->Clone());

		if (exp->ExtractRelConst(kATDebugExpNodeType_Read, ~rangelo, ~temprem, &oplo) &&
			temprem &&
			temprem->ExtractRelConst(kATDebugExpNodeType_Read, ~rangehi, ~rem, &ophi))
		{
			isrange = true;
			israngewrite = false;
		}
		else if (expt->ExtractRelConst(kATDebugExpNodeType_Write, ~rangelo, ~temprem, &oplo) &&
			temprem &&
			temprem->ExtractRelConst(kATDebugExpNodeType_Write, ~rangehi, ~rem, &ophi))
		{
			isrange = true;
			israngewrite = true;
		}

		if (isrange) {
			// One of the ranges should be LT/LE and the other one GT/GE; validate this and swap around
			// the terms if needed.
			bool validRange = true;

			if (oplo == kATDebugExpNodeType_LT || oplo == kATDebugExpNodeType_LE) {
				rangelo.swap(rangehi);
				std::swap(oplo, ophi);
			}

			VDVERIFY(rangelo->Evaluate(rangeloaddr, ATDebugExpEvalContext()));
			VDVERIFY(rangehi->Evaluate(rangehiaddr, ATDebugExpEvalContext()));

			if (oplo == kATDebugExpNodeType_GT)
				++rangeloaddr;
			else if (oplo != kATDebugExpNodeType_GE)
				validRange = false;

			if (ophi == kATDebugExpNodeType_LT)
				--rangehiaddr;
			else if (ophi != kATDebugExpNodeType_LE)
				validRange = false;

			if (!validRange) {
				throw MyError("Unable to parse access range: relative checks for read or write accesses were found, but a range could not be determined. "
					"An access range must be specified with the READ or WRITE operators using a </<= and >/>= pair.");
			}

			if (rangeloaddr < 0 || rangehiaddr > 0xFFFF || rangeloaddr > rangehiaddr)
				throw MyError("Invalid access range: $%04X-%04X.\n", rangeloaddr, rangehiaddr);

			// Check if we're only doing exactly one byte and demote to a single address breakpoint
			// if so.
			if (rangeloaddr == rangehiaddr) {
				if (israngewrite)
					rangelo.swap(extwrite);
				else
					rangelo.swap(extread);

				addr = rangeloaddr;
				isrange = false;
			}
		} else {
			isinsn = true;
			rem = std::move(exp);
		}
	} else {
		VDVERIFY((extpc ? extpc : extread ? extread : extwrite)->Evaluate(addr, ATDebugExpEvalContext()));

		if (extpc) {
			if (addr < 0 || addr > 0xFFFFFF)
				throw MyError("Invalid PC breakpoint address: $%x. Addresses must be in the 24-bit address space.", addr);
		} else {
			if (addr < 0 || addr > 0xFFFFFF)
				throw MyError("Invalid access breakpoint address: $%x. Addresses must be in the 24-bit address space.", addr);
		}
	}

	if (rem) {
		// check if the remainder is always true, and if so, drop it
		if (rem->mType == kATDebugExpNodeType_Const) {
			sint32 v;

			if (rem->Evaluate(v, ATDebugExpEvalContext()) && v)
				rem.reset();
		}
	}

	if (isrange || extread || extwrite) {
		if (!g_debugger.AreAccessBreakpointsSupported())
			throw MyError("Memory access breakpoints are not supported on the current target.");
	} else {
		if (!g_debugger.ArePCBreakpointsSupported())
			throw MyError("PC breakpoints are not supported on the current target.");
	}

	ATBreakpointManager *bpm = GetBreakpointManager();
	const uint32 sysidx = isrange ? bpm->SetAccessRangeBP(rangeloaddr, rangehiaddr - rangeloaddr + 1, !israngewrite, israngewrite)
						: extpc ? bpm->SetAtPC(g_debugger.GetTargetIndex(), (uint32)addr)
						: extread ? bpm->SetAccessBP(addr, true, false)
						: extwrite ? bpm->SetAccessBP(addr, false, true)
						: isinsn ? bpm->SetInsnBP(g_debugger.GetTargetIndex())
						: bpm->SetAccessBP(addr, false, true);

	uint32 useridx = RegisterSystemBreakpoint(sysidx, rem, command, continueExecution);
	rem.release();

	return useridx;
}

void ATDebugger::SetBreakpointClearOnReset(uint32 useridx, bool clearOnReset) {
	mUserBPs[useridx].mbClearOnReset = clearOnReset;
}

void ATDebugger::SetBreakpointOneShot(uint32 useridx, bool clearOnReset) {
	mUserBPs[useridx].mbOneShot = clearOnReset;
}

sint32 ATDebugger::RegisterUserBreakpoint(uint32 useridx, const char *groupName) {
	UserBPGroup *group = &mUserNumberedBPs;

	if (groupName) {
		if (!*groupName)
			groupName = nullptr;
		else {
			auto r = mUserBPGroups.insert_as(groupName);

			group = &r.first->second;
			groupName = r.first->first.c_str();
		}
	}

	UserBP& ubp = mUserBPs[useridx];

	auto it = std::find(group->begin(), group->end(), -1);
	sint32 index = (sint32)(it - group->begin());

	if (it == group->end())
		group->push_back((sint32)useridx);
	else
		*it = (sint32)useridx;

	ubp.mpTagName = groupName;
	ubp.mTagNumber = index;
	return index;
}

uint32 ATDebugger::RegisterSystemBreakpoint(uint32 sysidx, ATDebugExpNode *condexp, const char *command, bool continueExecution) {
	UserBPs::iterator it(std::find_if(mUserBPs.begin(), mUserBPs.end(), UserBPFreePred()));
	uint32 useridx = (uint32)(it - mUserBPs.begin());

	if (it == mUserBPs.end())
		mUserBPs.push_back();

	UserBP& ubp = mUserBPs[useridx];
	ubp.mSysBP = sysidx;
	ubp.mTargetIndex = mCurrentTargetIndex;
	ubp.mpCondition = condexp;
	ubp.mCommand = command ? command : "";
	ubp.mModuleId = 0;
	ubp.mbContinueExecution = continueExecution;
	ubp.mbClearOnReset = false;
	ubp.mbOneShot = false;
	ubp.mpTagName = nullptr;
	ubp.mTagNumber = 0;

	mSysBPToUserBPMap[sysidx] = useridx;

	return useridx;
}

vdvector<VDStringA> ATDebugger::GetBreakpointGroups() const {
	vdvector<VDStringA> groups;

	groups.push_back();

	for(const auto& groupEntry : mUserBPGroups)
		groups.push_back(groupEntry.first);

	return groups;
}

VDStringA ATDebugger::GetBreakpointName(uint32 useridx) const {
	const UserBP& ubp = mUserBPs[useridx];
	VDStringA s;

	if (ubp.mpTagName) {
		s = ubp.mpTagName;
		s += '.';
	}

	s.append_sprintf("%u", ubp.mTagNumber);
	return s;
}

bool ATDebugger::GetBreakpointInfo(uint32 useridx, ATDebuggerBreakpointInfo& info) const {
	if (useridx >= mUserBPs.size())
		return false;

	const UserBP& bp = mUserBPs[useridx];
	info.mTargetIndex = bp.mTargetIndex;
	info.mNumber = bp.mTagNumber;
	info.mbClearOnReset = bp.mbClearOnReset;
	info.mbOneShot = bp.mbOneShot;

	if (bp.mSysBP == (uint32)-1) {
		info.mbDeferred = true;
		return false;
	}

	ATBreakpointInfo bpinfo;
	mpBkptManager->GetInfo(bp.mSysBP, bpinfo);

	info.mAddress = bpinfo.mAddress;
	info.mLength = bpinfo.mLength;
	info.mbBreakOnPC = bpinfo.mbBreakOnPC;
	info.mbBreakOnInsn = bpinfo.mbBreakOnInsn;
	info.mbBreakOnRead = bpinfo.mbBreakOnRead;
	info.mbBreakOnWrite = bpinfo.mbBreakOnWrite;
	info.mbDeferred = false;

	return true;
}

void ATDebugger::GetBreakpointList(vdfastvector<uint32>& bps, const char *groupName) const {
	const UserBPGroup *group = &mUserNumberedBPs;

	if (groupName && *groupName) {
		auto it = mUserBPGroups.find_as(groupName);

		if (it == mUserBPGroups.end())
			return;

		group = &it->second;
	}

	size_t n = group->size();
	bps.reserve(n);

	for (sint32 idx : *group) {
		if (idx >= 0)
			bps.push_back((uint32)idx);
	}
}

ATDebugExpNode *ATDebugger::GetBreakpointCondition(uint32 useridx) const {
	if (useridx >= mUserBPs.size())
		return NULL;

	return mUserBPs[useridx].mpCondition;
}

void ATDebugger::SetBreakpointCondition(uint32 useridx, vdautoptr<ATDebugExpNode>& expr) {
	if (useridx >= mUserBPs.size())
		return;

	auto& ubp = mUserBPs[useridx];

	if (ubp.mSysBP == (uint32)-1)
		return;

	delete ubp.mpCondition;
	ubp.mpCondition = expr.release();
}

const char *ATDebugger::GetBreakpointCommand(uint32 useridx) const {
	if (useridx >= mUserBPs.size())
		return NULL;

	const char *s = mUserBPs[useridx].mCommand.c_str();

	return *s ? s : NULL;
}

bool ATDebugger::GetBreakpointSourceLocation(uint32 useridx, VDStringA& file, uint32& line) const {
	if (useridx >= mUserBPs.size())
		return NULL;

	const UserBP& ubp = mUserBPs[useridx];
	if (ubp.mSource.empty())
		return false;

	file = ubp.mSource;
	line = ubp.mSourceLine;
	return true;
}

void ATDebugger::StepInto(ATDebugSrcMode sourceMode, const ATDebuggerStepRange *stepRanges, uint32 stepRangeCount) {
	if (g_sim.IsRunning())
		return;

	IATDebugTargetExecutionControl *ec = nullptr;
	if (mCurrentTargetIndex != 0) {
		ec = vdpoly_cast<IATDebugTargetExecutionControl *>(mpCurrentTarget);

		if (!ec)
			throw MyError("Step execution is not available on the current target.");

		if (stepRangeCount > 0)
			throw MyError("Range step execution is not available on the current target.");
	}

	SetSourceMode(sourceMode);

	if (ec) {
		mbClientUpdatePending = true;
		mRunState = kRunState_TargetStepInto;

		if (!mbClientLastRunState)
			UpdateClientSystemState();

		uint32 targetIndex = mCurrentTargetIndex;
		ec->StepInto([this, targetIndex](bool successful) { OnTargetStepComplete(targetIndex, successful); });

		if (mRunState == kRunState_TargetStepInto)
			g_sim.ResumeSingleCycle();
	} else {
		ATCPUEmulator& cpu = g_sim.GetCPU();

		cpu.SetTrace(false);

		if (mbSourceMode && stepRangeCount > 0)
			SetupRangeStep(true, stepRanges, stepRangeCount);
		else
			cpu.SetStep(true);

		g_sim.Resume();
		mbClientUpdatePending = true;
		mRunState = kRunState_StepInto;

		if (!mbClientLastRunState)
			UpdateClientSystemState();
	}
}

void ATDebugger::StepOver(ATDebugSrcMode sourceMode, const ATDebuggerStepRange *stepRanges, uint32 stepRangeCount) {
	if (g_sim.IsRunning())
		return;

	IATDebugTargetExecutionControl *ec = nullptr;
	if (mCurrentTargetIndex != 0) {
		ec = vdpoly_cast<IATDebugTargetExecutionControl *>(mpCurrentTarget);

		if (!ec)
			throw MyError("Step Over is not available on the current target.");

		if (stepRangeCount > 0)
			throw MyError("Range step execution is not available on the current target.");
	}

	SetSourceMode(sourceMode);

	if (ec) {
		mbClientUpdatePending = true;
		mRunState = kRunState_TargetStepInto;

		if (!mbClientLastRunState)
			UpdateClientSystemState();

		uint32 targetIndex = mCurrentTargetIndex;
		ec->StepOver([this, targetIndex](bool successful) { OnTargetStepComplete(targetIndex, successful); });

		if (mRunState == kRunState_TargetStepInto)
			g_sim.ResumeSingleCycle();
	} else {
		ATCPUEmulator& cpu = g_sim.GetCPU();

		uint8 opcode = g_sim.DebugReadByte(cpu.GetInsnPC());

		cpu.SetTrace(false);
		if (opcode == 0x20) {
			cpu.SetRTSBreak(cpu.GetS());
			cpu.SetStep(false);
		} else {
			SetupRangeStep(false, stepRanges, stepRangeCount);
		}

		g_sim.Resume();
		mbClientUpdatePending = true;
		mRunState = kRunState_StepOver;

		if (!mbClientLastRunState)
			UpdateClientSystemState();
	}
}

void ATDebugger::StepOut(ATDebugSrcMode sourceMode) {
	if (g_sim.IsRunning())
		return;

	IATDebugTargetExecutionControl *ec = nullptr;
	if (mCurrentTargetIndex != 0) {
		ec = vdpoly_cast<IATDebugTargetExecutionControl *>(mpCurrentTarget);

		if (!ec)
			throw MyError("Step Out is not available on the current target.");
	}

	SetSourceMode(sourceMode);

	if (ec) {
		mbClientUpdatePending = true;
		mRunState = kRunState_TargetStepInto;

		if (!mbClientLastRunState)
			UpdateClientSystemState();

		uint32 targetIndex = mCurrentTargetIndex;
		ec->StepOut([this, targetIndex](bool successful) { OnTargetStepComplete(targetIndex, successful); });

		if (mRunState == kRunState_TargetStepInto)
			g_sim.ResumeSingleCycle();
	} else {
		ATCPUEmulator& cpu = g_sim.GetCPU();
		uint8 s = cpu.GetS();
		if (s == 0xFF)
			return StepInto(sourceMode);

		++s;

		ATCallStackFrame frames[2];
		uint32 framecount = GetCallStack(frames, 2);

		if (framecount >= 2)
			s = (uint8)frames[1].mSP;

		cpu.SetStep(false);
		cpu.SetTrace(false);
		cpu.SetRTSBreak(s);
		g_sim.Resume();
		mbClientUpdatePending = true;
		mRunState = kRunState_StepOut;

		if (!mbClientLastRunState)
			UpdateClientSystemState();
	}
}

uint16 ATDebugger::GetPC() const {
	ATCPUExecState state;
	mpCurrentTarget->GetExecState(state);

	return mpCurrentTarget->GetDisasmMode() == kATDebugDisasmMode_Z80 ? state.mZ80.mPC : state.m6502.mPC;
}

void ATDebugger::SetPC(uint16 pc) {
	if (g_sim.IsRunning())
		return;

	ATCPUEmulator& cpu = g_sim.GetCPU();
	cpu.SetPC(pc);
	mFramePC = pc;
	mbClientUpdatePending = true;
}

uint32 ATDebugger::GetExtPC() const {
	ATCPUExecState state;
	mpCurrentTarget->GetExecState(state);

	if (mpCurrentTarget->GetDisasmMode() == kATDebugDisasmMode_Z80)
		return (uint32)state.mZ80.mPC;
	else
		return (uint32)state.m6502.mPC + ((uint32)state.m6502.mK << 16);
}

uint16 ATDebugger::GetFramePC() const {
	return mFramePC;
}

void ATDebugger::SetFramePC(uint16 pc) {
	if (mFramePC != pc) {
		mFramePC = pc;

		mbClientUpdatePending = true;
	}
}

uint32 ATDebugger::GetCallStack(ATCallStackFrame *dst, uint32 maxCount) {
	switch(mpCurrentTarget->GetDisasmMode()) {
		case kATDebugDisasmMode_Z80:
			return GetCallStackZ80(dst, maxCount);

		default:
			return GetCallStack6502(dst, maxCount);
	}
};

uint32 ATDebugger::GetCallStackZ80(ATCallStackFrame *dst, uint32 maxCount) {
	struct StackStateZ80 {
		uint16 mPC;
		uint16 mSP;
		bool mbDDFD;
		bool mbCB;
		bool mbED;
	};

	IATDebugTarget *target = GetTarget();
	ATCPUExecState state;

	target->GetExecState(state);

	uint16 vSP = state.mZ80.mSP;
	uint16 vPC = state.mZ80.mPC;
	bool vDDFD = false;
	bool vCB = false;
	bool vED = false;

	const auto readWord = [=](uint32 addr) {
		uint8 buf[2];
		target->DebugReadMemory(addr, buf, 2);

		return VDReadUnalignedLEU16(buf);
	};

	std::deque<StackStateZ80> q;
	for(uint32 i=0; i<maxCount; ++i) {
		dst->mPC = vPC;
		dst->mSP = vSP;
		dst->mP = 0;
		++dst;

		uint32 seenFlags[2048] = {0};
		q.clear();

		StackStateZ80 ss = { vPC, vSP, vDDFD, vCB, vED };
		q.push_back(ss);

		bool found = false;
		int insnLimit = 1000;
		while(!q.empty() && insnLimit--) {
			ss = q.front();
			q.pop_front();

			vPC = ss.mPC;
			vSP = ss.mSP;
			vDDFD = ss.mbDDFD;
			vED = ss.mbED;
			vCB = ss.mbCB;

			uint32& seenFlagWord = seenFlags[vPC >> 5];
			uint32 seenBit = (1 << (vPC & 31));
			if (seenFlagWord & seenBit)
				continue;

			seenFlagWord |= seenBit;

			uint8 opcode = target->DebugReadByte(vPC);
			uint16 nextPC = vPC;
			
			if (vED) {
				const uint8 len = ATGetOpcodeLengthZ80ED(opcode);
				if (!len)
					continue;

				nextPC += len;

				if ((opcode & 0xC7) == 0x45) {
					// RETN
					nextPC = readWord(vSP);
					vSP += 2;
				}
			} else if (vCB) {
				const uint8 len = vDDFD ? ATGetOpcodeLengthZ80DDFDCB(opcode) : ATGetOpcodeLengthZ80CB(opcode);

				if (!len)
					continue;

				// CB xx instructions are all bit instructions, so we can ignore them.

				nextPC += len;
			} else {
				uint8 len = ATGetOpcodeLengthZ80(opcode);

				if (len) {
					vDDFD = false;
					vCB = false;
					vED = false;

					switch(opcode) {
						case 0x10:	// DJNZ
						case 0x20:	// JR NZ
						case 0x28:	// JR Z
						case 0x30:	// JR NC
						case 0x38:	// JR C
							q.push_front(StackStateZ80 { (uint16)(vPC + (uint16)(sint8)target->DebugReadByte(vPC + 1)), vSP });
							break;

						case 0x18:	// JR
							nextPC = (uint16)(vPC + (uint16)(sint8)target->DebugReadByte(vPC + 1));
							break;

						case 0x33:	// INC SP
							++vSP;
							break;

						case 0x3B:	// DEC SP
							--vSP;
							break;

						case 0xC0:	// RET NZ
						case 0xC8:	// RET Z
						case 0xC9:	// RET
						case 0xD0:	// RET NC
						case 0xD8:	// RET C
						case 0xE0:	// RET PO
						case 0xE8:	// RET PE
						case 0xF0:	// RET P
						case 0xF8:	// RET M
							vPC = readWord(vSP);
							vSP += 2;
							goto found;

						case 0xC1:	// POP AF
						case 0xD1:	// POP BC
						case 0xE1:	// POP DE
						case 0xF1:	// POP HL
							vSP += 2;
							break;

						case 0xC2:	// JP NZ
						case 0xCA:	// JP Z
						case 0xD2:	// JP NC
						case 0xDA:	// JP C
						case 0xE2:	// JP PO
						case 0xEA:	// JP PE
						case 0xF2:	// JP P
						case 0xFA:	// JP M
							q.push_front(StackStateZ80 { readWord(vPC + 1), vSP });
							break;

						case 0xC3:	// JP
							nextPC = readWord(vPC + 1);
							break;

						case 0xC5:	// PUSH AF
						case 0xD5:	// PUSH BC
						case 0xE5:	// PUSH DE
						case 0xF5:	// PUSH HL
							vSP -= 2;
							break;
					}
				} else {
					switch(opcode) {
						case 0xDD:
						case 0xFD:
							vDDFD = true;
							len = 1;
							break;

						case 0xED:
							vED = true;
							len = 1;
							break;

						case 0xCB:
							vCB = true;
							len = 1;
							break;

						default:
							continue;
					}
				}

				nextPC += len;
			}

			ss.mPC		= nextPC;
			ss.mSP		= vSP;
			ss.mbDDFD	= vDDFD;
			ss.mbCB		= vCB;
			ss.mbED		= vED;
			q.push_back(ss);
		}

		return i + 1;

found:
		;
	}

	return maxCount;
}

uint32 ATDebugger::GetCallStack6502(ATCallStackFrame *dst, uint32 maxCount) {
	struct StackState {
		uint16 mPC;
		uint8 mS;
		uint8 mP;
		uint8 mK;
	};

	IATDebugTarget *target = GetTarget();
	ATCPUExecState state;

	target->GetExecState(state);

	uint8 vS = state.m6502.mS;
	uint8 vP = state.m6502.mP;
	uint16 vPC = state.m6502.mPC;
	uint8 vK = state.m6502.mK;

	const auto readWord = [=](uint32 addr) {
		uint8 buf[2];
		target->DebugReadMemory(addr, buf, 2);

		return VDReadUnalignedLEU16(buf);
	};

	std::deque<StackState> q;

	const ATDebugDisasmMode disasmMode = target->GetDisasmMode();
	bool isC02 = disasmMode != kATDebugDisasmMode_6502;
	bool is816 = disasmMode == kATDebugDisasmMode_65C816;
	for(uint32 i=0; i<maxCount; ++i) {
		dst->mPC = vPC + ((uint32)vK << 16);
		dst->mSP = vS + 0x0100;
		dst->mP = vP;
		++dst;

		uint32 seenFlags[2048] = {0};
		q.clear();

		StackState ss = { vPC, vS, vP, vK };
		q.push_back(ss);

		// we keep track of this stack level so we don't push false frames from PHA+PHA+RTS
		int thresholdS = vS;

		bool found = false;
		int insnLimit = 1000;
		while(!q.empty() && insnLimit--) {
			ss = q.front();
			q.pop_front();

			vPC = ss.mPC;
			vS = ss.mS;
			vP = ss.mP;

			uint32& seenFlagWord = seenFlags[vPC >> 5];
			uint32 seenBit = (1 << (vPC & 31));
			if (seenFlagWord & seenBit)
				continue;

			seenFlagWord |= seenBit;

			uint8 opcode = target->DebugReadByte(vPC + ((uint32)vK << 16));
			uint16 nextPC = vPC + ATGetOpcodeLength(opcode, vP, state.m6502.mbEmulationFlag, disasmMode);

			if (opcode == 0x00)				// BRK
				continue;
			else if (opcode == 0x58)		// CLI
				vP &= ~0x04;
			else if (opcode == 0x78)		// SEI
				vP |= 0x04;
			else if (opcode == 0x4C)		// JMP abs
				nextPC = readWord(vPC + 1);
			else if (opcode == 0x6C) {		// JMP (ind)
				nextPC = readWord(readWord(vPC + 1));
			} else if (opcode == 0x40) {	// RTI
				if (vS > 0xFC)
					continue;
				vP = target->DebugReadByte(vS + 0x0101);
				vPC = readWord(vS + 0x0102);
				vS += 3;

				if (vS <= thresholdS)
					continue;

				found = true;
				break;
			} else if (opcode == 0x60) {	// RTS
				if (vS > 0xFD)
					continue;
				vPC = readWord(vS + 0x0101) + 1;
				vS += 2;

				if (vS <= thresholdS)
					continue;

				found = true;
				break;
			} else if (opcode == 0x08) {	// PHP
				if (!vS)
					continue;
				--vS;
			} else if (opcode == 0x28) {	// PLP
				if (vS == 0xFF)
					continue;
				++vS;
				vP = target->DebugReadByte(0x100 + vS);
			} else if (opcode == 0x48) {	// PHA
				if (!vS)
					continue;
				--vS;
			} else if (opcode == 0x68) {	// PLA
				if (vS == 0xFF)
					continue;
				++vS;
			} else if ((opcode & 0x1f) == 0x10) {	// Bcc
				ss.mS	= vS;
				ss.mP	= vP;
				ss.mK	= vK;

				const uint8 delta = target->DebugReadByte((uint16)(vPC + 1) + ((uint32)vK << 16));
				ss.mPC = nextPC + (sint16)(sint8)delta;

				// take branch first for a forward branch, else fall through first
				if (delta >= 0x80)
					q.push_back(ss);
				else
					q.push_front(ss);
			} else if (isC02) {
				if (opcode == 0x80) {	// BRA
					nextPC += (sint16)(sint8)target->DebugReadByte((uint16)(vPC + 1) + ((uint32)vK << 16));
				} else if (opcode == 0xDA || opcode == 0x5A) {	// PHX, PHY
					if (!vS)
						continue;
					--vS;
				} else if (opcode == 0xFA || opcode == 0x7A) {	// PLX, PLY
					if (vS == 0xFF)
						continue;
					++vS;
				} else if (is816) {
					if (opcode == 0x82) {	// BRL
						nextPC += (sint16)(target->DebugReadByte((uint16)(vPC + 1) + ((uint32)vK << 16)) + 256*(int)target->DebugReadByte((uint16)(vPC + 2) + ((uint32)vK << 16)));
					} else if (opcode == 0x0B) {	// PHD
						if (vS < 2)
							continue;
						vS -= 2;
					} else if (opcode == 0x2B) {	// PLD
						if (vS >= 0xFE)
							continue;

						vS += 2;
					} else if (opcode == 0x4B) {	// PHK
						if (!vS)
							continue;
						--vS;						
					} else if (opcode == 0x8B) {	// PHB
						if (!vS)
							continue;
						--vS;						
					} else if (opcode == 0xAB) {	// PLB
						if (vS == 0xFF)
							continue;

						++vS;
					}
				}
			}

			ss.mS	= vS;
			ss.mP	= vP;
			ss.mK	= vK;
			ss.mPC	= nextPC;
			q.push_back(ss);
		}

		if (!found)
			return i + 1;
	}

	return maxCount;
}

void ATDebugger::DumpCallStack() {
	ATCallStackFrame frames[16];

	uint32 frameCount = GetCallStack(frames, 16);

	ATConsolePrintf("I SP    PC\n");
	ATConsolePrintf("----------------------\n");
	for(uint32 i=0; i<frameCount; ++i) {
		const ATCallStackFrame& fr = frames[i];
		ATSymbol sym;
		const char *symname = "";
		if (LookupSymbol(fr.mPC, kATSymbol_Execute, sym))
			symname = sym.mpName;

		ATConsolePrintf("%c %04X  %04X (%s)\n", fr.mP & 0x04 ? '*' : ' ', fr.mSP, fr.mPC, symname);
	}

	ATConsolePrintf("End of stack trace.\n");
}

void ATDebugger::DumpState(bool verbose, const ATCPUExecState *state) {
	if (mCurrentTargetIndex) {
		IATDebugTarget *target = mpCurrentTarget;

		ATCPUExecState tstate;
		if (!state) {
			target->GetExecState(tstate);
			state = &tstate;
		}

		VDString s;

		// The target may be time skewed, so we have to emit the timestamp ourselves.
		ATCPUTimestampDecoder timestamp = g_sim.GetTimestampDecoder();

		const auto beamPos = timestamp.GetBeamPosition(g_sim.GetTimestamp() + (uint32)target->GetTimeSkew());

		s.sprintf("(%3d:%3d,%3d) ", beamPos.mFrame, beamPos.mY, beamPos.mX);

		const auto disasmMode = target->GetDisasmMode();
		uint16 pc;
		uint8 k = 0;

		if (disasmMode == kATDebugDisasmMode_8048) {
			const ATCPUExecState8048& state8048 = state->m8048;
			const uint8 *r = state8048.mReg[state8048.mPSW & 0x10 ? 1 : 0];

			s.append_sprintf("A=%02X R0=%02X R1=%02X R2=%02X R3=%02X PSW=%02X (%c%c%c/RB%c/SP%u)  "
				, state8048.mA
				, r[0]
				, r[1]
				, r[2]
				, r[3]
				, state8048.mPSW
				, state8048.mPSW & 0x80 ? 'C' : '-'
				, state8048.mPSW & 0x40 ? 'A' : '-'
				, state8048.mPSW & 0x20 ? 'F' : '-'
				, state8048.mPSW & 0x10 ? '1' : '0'
				, state8048.mPSW & 7
				);

			pc = state8048.mPC;
			k = 0;
		} else if (disasmMode == kATDebugDisasmMode_Z80) {
			const ATCPUExecStateZ80& stateZ80 = state->mZ80;
			s.append_sprintf("BC=%02X%02X DE=%02X%02X HL=%02X%02X AF=%02X%02X"
				, stateZ80.mB
				, stateZ80.mC
				, stateZ80.mD
				, stateZ80.mE
				, stateZ80.mH
				, stateZ80.mL
				, stateZ80.mA
				, stateZ80.mF
			);

			if (verbose)
				s.append_sprintf(" SP=%04X", stateZ80.mSP);

			s.append_sprintf(
				" (%c%c-%c-%c%c%c)  "
				, stateZ80.mF & 0x80 ? 'S' : '-'
				, stateZ80.mF & 0x40 ? 'Z' : '-'
				, stateZ80.mF & 0x10 ? 'H' : '-'
				, stateZ80.mF & 0x04 ? 'P' : '-'
				, stateZ80.mF & 0x02 ? 'N' : '-'
				, stateZ80.mF & 0x01 ? 'C' : '-'
				);

			pc = stateZ80.mPC;
			k = 0;
		} else if (disasmMode == kATDebugDisasmMode_6809) {
			const ATCPUExecState6809& state6809 = state->m6809;
			s.append_sprintf("A=%02X B=%02X X=%04X Y=%04X"
				, state6809.mA
				, state6809.mB
				, state6809.mX
				, state6809.mY
			);

			if (verbose)
				s.append_sprintf(" S=%04X U=%04X CC=%02X", state6809.mS, state6809.mU, state6809.mCC);

			s.append_sprintf(
				" (%c%c%c%c%c%c%c%c)  "
				, state6809.mCC & 0x80 ? 'E' : '-'
				, state6809.mCC & 0x40 ? 'F' : '-'
				, state6809.mCC & 0x20 ? 'H' : '-'
				, state6809.mCC & 0x10 ? 'I' : '-'
				, state6809.mCC & 0x07 ? 'N' : '-'
				, state6809.mCC & 0x04 ? 'Z' : '-'
				, state6809.mCC & 0x02 ? 'V' : '-'
				, state6809.mCC & 0x01 ? 'C' : '-'
				);

			pc = state6809.mPC;
			k = 0;
		} else {
			const ATCPUExecState6502& state6502 = state->m6502;

			pc = state6502.mPC;
			k = state6502.mK;

			if (disasmMode != kATDebugDisasmMode_65C816) {
				s.append_sprintf("A=%02X X=%02X Y=%02X S=%02X P=%02X (%c%c%c%c%c%c)  "
					, state6502.mA
					, state6502.mX
					, state6502.mY
					, state6502.mS
					, state6502.mP
					, state6502.mP & 0x80 ? 'N' : ' '
					, state6502.mP & 0x40 ? 'V' : ' '
					, state6502.mP & 0x08 ? 'D' : ' '
					, state6502.mP & 0x04 ? 'I' : ' '
					, state6502.mP & 0x02 ? 'Z' : ' '
					, state6502.mP & 0x01 ? 'C' : ' '
					);
			} else if (state->m6502.mbEmulationFlag) {
				s.append_sprintf("C=%02X%02X X=%02X Y=%02X S=%02X P=%02X (%c%c%c%c%c%c)  "
					, state6502.mAH
					, state6502.mA
					, state6502.mX
					, state6502.mY
					, state6502.mS
					, state6502.mP
					, state6502.mP & 0x80 ? 'N' : ' '
					, state6502.mP & 0x40 ? 'V' : ' '
					, state6502.mP & 0x08 ? 'D' : ' '
					, state6502.mP & 0x04 ? 'I' : ' '
					, state6502.mP & 0x02 ? 'Z' : ' '
					, state6502.mP & 0x01 ? 'C' : ' '
					);
			} else {
				if (!(state6502.mP & AT6502::kFlagX)) {
					s.append_sprintf("%c=%02X%02X X=%02X%02X Y=%02X%02X"
						, (state6502.mP & AT6502::kFlagM) ? 'C' : 'A'
						, state6502.mAH
						, state6502.mA
						, state6502.mXH
						, state6502.mX
						, state6502.mYH
						, state6502.mY
						);
				} else {
					s.append_sprintf("%c=%02X%02X X=--%02X Y=--%02X"
						, (state6502.mP & AT6502::kFlagM) ? 'C' : 'A'
						, state6502.mAH
						, state6502.mA
						, state6502.mX
						, state6502.mY
						);
				}

				s.append_sprintf(" S=%02X%02X P=%02X (%c%c%c%c%c%c%c%c)  "
						, state6502.mSH
						, state6502.mS
						, state6502.mP
						, state6502.mP & 0x80 ? 'N' : ' '
						, state6502.mP & 0x40 ? 'V' : ' '
						, state6502.mP & 0x20 ? 'M' : ' '
						, state6502.mP & 0x10 ? 'X' : ' '
						, state6502.mP & 0x08 ? 'D' : ' '
						, state6502.mP & 0x04 ? 'I' : ' '
						, state6502.mP & 0x02 ? 'Z' : ' '
						, state6502.mP & 0x01 ? 'C' : ' '
						);
			}
		}

		ATCPUHistoryEntry hent = {};
		ATDisassembleCaptureRegisterContext(hent, *state, disasmMode);
		ATDisassembleCaptureInsnContext(target, pc, k, hent);

		ATDisassembleInsn(s, target, disasmMode, hent, false, false, true, true, false, false, false, false, false);
		s += '\n';
		ATConsoleWrite(s.c_str());

		if (verbose) {
			if (disasmMode == kATDebugDisasmMode_8048) {
				const ATCPUExecState8048& state8048 = state->m8048;
				const uint8 *r = state8048.mReg[state8048.mPSW & 0x10 ? 1 : 0];

				ATConsolePrintf("                   R4=%02X R5=%02X R6=%02X R7=%02X P1=%02X P2=%02X\n"
					, r[4]
					, r[5]
					, r[6]
					, r[7]
					, state8048.mP1
					, state8048.mP2
				);
			} else if (disasmMode == kATDebugDisasmMode_65C816) {
				ATConsolePrintf("              B=%02X D=%04X\n", state->m6502.mB, state->m6502.mDP);
			} else if (disasmMode == kATDebugDisasmMode_Z80) {
				const ATCPUExecStateZ80& stateZ80 = state->mZ80;

				ATConsolePrintf("              AF'=%02X%02X BC'=%02X%02X DE'=%02X%02X HL'=%02X%02X I=%02X IFF1=%c IFF2=%c\n"
					, state->mZ80.mAltA
					, state->mZ80.mAltF
					, state->mZ80.mAltB
					, state->mZ80.mAltC
					, state->mZ80.mAltD
					, state->mZ80.mAltE
					, state->mZ80.mAltH
					, state->mZ80.mAltL
					, state->mZ80.mI
					, state->mZ80.mbIFF1 ? '1' : '0'
					, state->mZ80.mbIFF2 ? '1' : '0'
				);
			}
		}
	} else {
		ATCPUEmulator& cpu = g_sim.GetCPU();
		cpu.DumpStatus(true);
	}
}

int ATDebugger::AddWatch(uint32 address, int length) {
	for(int i=0; i<8; ++i) {
		auto& watch = mWatches[i];

		if (watch.mLength < 0) {
			watch.mAddress = address;
			watch.mLength = length;
			watch.mTargetIndex = mCurrentTargetIndex;
			return i;
		}
	}

	return -1;
}

int ATDebugger::AddWatchExpr(ATDebugExpNode *expr) {
	for(int i=0; i<8; ++i) {
		auto& watch = mWatches[i];

		if (watch.mLength < 0) {
			watch.mAddress = 0;
			watch.mLength = 0;
			watch.mTargetIndex = mCurrentTargetIndex;
			watch.mpExpr = expr;
			return i;
		}
	}

	return -1;
}

bool ATDebugger::ClearWatch(int idx) {
	if (idx < 0 || idx > 7)
		return false;

	auto& watch = mWatches[idx];
	watch.mLength = -1;
	watch.mTargetIndex = 0;
	watch.mpExpr.reset();
	return true;
}

void ATDebugger::ClearAllWatches() {
	for(auto& watch : mWatches) {
		watch.mLength = -1;
		watch.mpExpr.reset();
		watch.mTargetIndex = 0;
	}
}

bool ATDebugger::GetWatchInfo(int idx, ATDebuggerWatchInfo& winfo) {
	if ((unsigned)idx >= 8)
		return false;
	
	const auto& watch = mWatches[idx];
	if (watch.mLength < 0)
		return false;

	winfo.mAddress = watch.mAddress;
	winfo.mLen = watch.mLength;
	winfo.mTargetIndex = watch.mTargetIndex;
	winfo.mpExpr = watch.mpExpr;
	return true;
}

void ATDebugger::ListModules() {
	int index = 1;

	vdfastvector<const Module *> modules(mModules.size());
	std::transform(mModules.begin(), mModules.end(), modules.begin(), [](const Module& x) { return &x; });

	std::sort(modules.begin(), modules.end(),
		[](const Module *x, const Module *y) {
			return x->mId < y->mId;
		}
	);

	VDStringA s;
	for(const Module *pMod : modules) {
		const Module& mod = *pMod;

		if (mod.mbDeferredLoad) {
			ATConsolePrintf("%3d) ~%u | (symbol load deferred)           %-20s %s%s\n"
				, mod.mId
				, mod.mTargetId
				, mod.mShortName.c_str()
				, mod.mName.c_str()
				, mod.mbDirty ? "*" : "");
		} else {
			s.sprintf("%04X-%04X"
				, mod.mBase
				, mod.mBase + mod.mSize - 1);

			ATConsolePrintf("%3d) ~%u | %-13s  %-16s  %-20s %s%s\n"
				, mod.mId
				, mod.mTargetId
				, s.c_str()
				, mod.mpSymbols ? "(symbols loaded)" : "(no symbols)"
				, mod.mShortName.c_str()
				, mod.mName.c_str()
				, mod.mbDirty ? "*" : "");
		}

		++index;
	}
}

void ATDebugger::ReloadModules() {
	Modules::iterator it(mModules.begin()), itEnd(mModules.end());
	for(; it!=itEnd; ++it) {
		Module& mod = *it;

		if (!mod.mPath.empty()) {
			vdrefptr<IATSymbolStore> symStore;

			try {
				ATLoadSymbols(mod.mPath.c_str(), ~symStore);
			} catch(const MyError& e) {
				ATConsolePrintf("Unable to reload symbols from %ls: %s.\n", mod.mPath.c_str(), e.gets());
				continue;
			}

			ClearSymbolDirectives(mod.mId);
			mod.mbDeferredLoad = false;
			mod.mbDirectivesProcessed = false;
			mod.mpSymbols = symStore;
			ProcessSymbolDirectives(mod.mId);
			ATConsolePrintf("Reloaded symbols: %ls\n", mod.mPath.c_str());
		}
	}

	ResolveDeferredBreakpoints();
	NotifyEvent(kATDebugEvent_SymbolsChanged);
}

void ATDebugger::DumpCIOParameters() {
	const ATCPUEmulator& cpu = g_sim.GetCPU();
	uint8 iocb = cpu.GetX();
	unsigned iocbIdx = iocb >> 4;
	uint8 cmd = g_sim.DebugReadByte(iocb + ATKernelSymbols::ICCMD);
	char devName[3];

	if (cmd != 0x03) {
		uint8 dev = g_sim.DebugReadByte(iocb + ATKernelSymbols::ICHID);

		if (dev == 0xFF) {
			devName[0] = '-';
			devName[1] = 0;
		} else {
			dev = g_sim.DebugReadByte(dev + ATKernelSymbols::HATABS);

			if (dev > 0x20 && dev < 0x7F)
				devName[0] = (char)dev;
			else
				devName[0] = '?';

			devName[1] = ':';
			devName[2] = 0;
		}
	}

	char fn[128];
	fn[0] = 0;

	if (cmd == 0x03 || cmd >= 0x0D) {
		int idx = 0;
		uint16 bufadr = g_sim.DebugReadWord(iocb + ATKernelSymbols::ICBAL);

		while(idx < 127) {
			uint8 c = g_sim.DebugReadByte(bufadr + idx);

			if (c < 0x20 || c >= 0x7f)
				break;

			fn[idx++] = c;
		}

		fn[idx] = 0;
	}

	switch(cmd) {
		case 0x03:
			{
				const uint8 aux1 = g_sim.DebugReadByte(iocb + ATKernelSymbols::ICAX1);

				ATConsolePrintf("CIO: IOCB=%u, CMD=$03 (open), AUX1=$%02x, filename=\"%s\"\n", iocbIdx, aux1, fn);
			}
			break;

		case 0x05:
			ATConsolePrintf("CIO: IOCB=%u (%s), CMD=$05 (get record), buffer=$%04x, length=$%04x\n"
				, iocbIdx
				, devName
				, g_sim.DebugReadWord(iocb + ATKernelSymbols::ICBAL)
				, g_sim.DebugReadWord(iocb + ATKernelSymbols::ICBLL)
				);
			break;

		case 0x07:
			ATConsolePrintf("CIO: IOCB=%u (%s), CMD=$07 (get characters), buffer=$%04x, length=$%04x\n"
				, iocbIdx
				, devName
				, g_sim.DebugReadWord(iocb + ATKernelSymbols::ICBAL)
				, g_sim.DebugReadWord(iocb + ATKernelSymbols::ICBLL)
				);
			break;

		case 0x09:
			ATConsolePrintf("CIO: IOCB=%u (%s), CMD=$09 (put record)\n", iocbIdx, devName);
			break;

		case 0x0A:
			{
				const uint8 c = g_sim.GetCPU().GetA();
				ATConsolePrintf("CIO: IOCB=%u (%s), CMD=$0A (put byte): char=$%02X (%c)\n"
					, iocbIdx
					, devName
					, c
					, c >= 0x20 && c < 0x7F ? (char)c : '.');
			}
			break;
		case 0x0B:
			{
				uint16 len = g_sim.DebugReadWord(iocb + ATKernelSymbols::ICBLL);

				// Length=0 is a special case that uses the A register instead.
				if (len) {
					ATConsolePrintf("CIO: IOCB=%u (%s), CMD=$07 (put characters): buf=$%04X, len=$%04X\n"
						, iocbIdx
						, devName
						, g_sim.DebugReadWord(iocb + ATKernelSymbols::ICBAL)
						, len
						);
				} else {
					ATConsolePrintf("CIO: IOCB=%u (%s), CMD=$07 (put character): ch=$%02X\n"
						, iocbIdx
						, devName
						, g_sim.GetCPU().GetA()
						);
				}
			}
			break;

		case 0x0C:
			ATConsolePrintf("CIO: IOCB=%u (%s), CMD=$0C (close)\n", iocbIdx, devName);
			break;

		case 0x0D:
			ATConsolePrintf("CIO: IOCB=%u (%s), CMD=$0D (get status): filename=\"%s\"\n", iocbIdx, devName, fn);
			break;

		default:
			if (cmd >= 0x0E) {
				ATConsolePrintf("CIO: IOCB=%u (%s), CMD=$%02x (special): AUX=%02X,%02X; filename=\"%s\"\n", iocbIdx, devName, cmd
					, g_sim.DebugReadByte(iocb + ATKernelSymbols::ICAX1)
					, g_sim.DebugReadByte(iocb + ATKernelSymbols::ICAX1+1)
					, fn);
			} else {
				ATConsolePrintf("CIO: IOCB=%u (%s), CMD=$%02x (unknown)\n", iocbIdx, devName, cmd);
			}
			break;
	}
}

void ATDebugger::DumpSIOParameters() {
	uint8 params[12];

	for(uint32 i=0; i<12; ++i)
		params[i] = g_sim.DebugReadByte(0x300 + i);

	const uint8 ddevic = params[0];
	const uint8 dunit = params[1];
	const uint8 dcomnd = params[2];
	const uint16 dbuf = VDReadUnalignedLEU16(&params[4]);
	const uint8 dtimlo = params[6];
	const uint16 dbyt = VDReadUnalignedLEU16(&params[8]);
	const uint8 *const daux = params + 10;

	const char *cmddesc = ATDecodeSIOCommand(ddevic + dunit - 1, dcomnd, daux);

	ATConsolePrintf("SIO: Device $%02X[%u], command $%02X, buffer $%04X, length $%04X, aux $%04X timeout %4.1fs | %s\n"
		, ddevic
		, dunit
		, dcomnd
		, dbuf
		, dbyt
		, VDReadUnalignedLEU16(daux)
		, (float)dtimlo * 64.0f * (g_sim.IsVideo50Hz() ? 1.0f / 49.82f : 1.0f / 59.92f)
		, cmddesc
	);
}

void ATDebugger::SetCIOTracingEnabled(bool enabled) {
	if (enabled) {
		if (mSysBPTraceCIO)
			return;

		mSysBPTraceCIO = mpBkptManager->SetAtPC(0, ATKernelSymbols::CIOV);
	} else {
		if (!mSysBPTraceCIO)
			return;

		mpBkptManager->Clear(mSysBPTraceCIO);
		mSysBPTraceCIO = 0;
	}
}

void ATDebugger::SetSIOTracingEnabled(bool enabled) {
	if (enabled) {
		if (mSysBPTraceSIO)
			return;

		mSysBPTraceSIO = mpBkptManager->SetAtPC(0, ATKernelSymbols::SIOV);
	} else {
		if (!mSysBPTraceSIO)
			return;

		mpBkptManager->Clear(mSysBPTraceSIO);
		mSysBPTraceSIO = 0;
	}
}

void ATDebugger::GetModuleIds(vdfastvector<uint32>& ids) const {
	Modules::const_iterator it(mModules.begin()), itEnd(mModules.end());
	for(; it != itEnd; ++it) {
		ids.push_back(it->mId);
	}
}

uint32 ATDebugger::AddModule(uint32 targetId, uint32 base, uint32 size, IATSymbolStore *symbolStore, const char *name, const wchar_t *path) {
	Module newmod;
	newmod.mId = mNextModuleId++;
	newmod.mTargetId = targetId;
	newmod.mBase = base;
	newmod.mSize = size;
	newmod.mpSymbols = symbolStore;

	if (name)
		newmod.mName = name;

	if (path)
		newmod.mPath = path;

	mModules.push_back(newmod);

	return newmod.mId;
}

const char *ATDebugger::GetModuleShortName(uint32 moduleId) const {
	const Module *mod = GetModuleById(moduleId);
	if (!mod)
		return NULL;

	return mod->mShortName.c_str();
}

uint32 ATDebugger::GetModuleTargetId(uint32 moduleId) const {
	const Module *mod = GetModuleById(moduleId);
	if (!mod)
		return 0;

	return mod->mTargetId;
}

void ATDebugger::RemoveModule(uint32 base, uint32 size, IATSymbolStore *symbolStore) {
	Modules::iterator it(mModules.begin()), itEnd(mModules.end());
	for(; it!=itEnd; ++it) {
		const Module& mod = *it;

		if (mod.mBase == base && mod.mSize == size && (!symbolStore || mod.mpSymbols == symbolStore)) {
			mModules.erase(it);
			return;
		}
	}
}

void ATDebugger::AddClient(IATDebuggerClient *client, bool requestUpdate) {
	Clients::const_iterator it(std::find(mClients.begin(), mClients.end(), client));

	if (it == mClients.end()) {
		mClients.push_back(client);

		if (requestUpdate)
			UpdateClientSystemState(client);
	}
}

void ATDebugger::RemoveClient(IATDebuggerClient *client) {
	Clients::iterator it(std::find(mClients.begin(), mClients.end(), client));
	if (it != mClients.end()) {
		if (mClientsBusy) {
			*it = NULL;
			mbClientsChanged = true;
		} else {
			*it = mClients.back();
			mClients.pop_back();
		}
	}
}

void ATDebugger::RequestClientUpdate(IATDebuggerClient *client) {
	UpdateClientSystemState(client);
}

ATDebuggerSymbolLoadMode ATDebugger::GetSymbolLoadMode(bool whenEnabled) const {
	return mSymbolLoadModes[whenEnabled ? 1 : 0];
}

void ATDebugger::SetSymbolLoadMode(bool whenEnabled, ATDebuggerSymbolLoadMode mode) {
	if (mode == ATDebuggerSymbolLoadMode::Default)
		mode = whenEnabled ? ATDebuggerSymbolLoadMode::Enabled : ATDebuggerSymbolLoadMode::Deferred;

	auto& activeMode = mSymbolLoadModes[whenEnabled ? 1 : 0];

	if (activeMode != mode) {
		activeMode = mode;

		UpdateSymbolLoadMode();
	}
}

bool ATDebugger::IsSymbolLoadingEnabled() const {
	return mbSymbolLoadingEnabled;
}

uint32 ATDebugger::LoadSymbols(const wchar_t *path, bool processDirectives, const uint32 *targetIdOverride, bool loadImmediately) {
	vdrefptr<IATSymbolStore> symStore;
	const uint32 targetId = targetIdOverride ? *targetIdOverride : mCurrentTargetIndex;

	if (!wcscmp(path, L"kernel")) {
		UnloadSymbols(kModuleId_KernelROM);

		mModules.push_back(Module());
		Module& kernmod = mModules.back();
		ATCreateDefaultKernelSymbolStore(~kernmod.mpSymbols);
		kernmod.mId = kModuleId_KernelROM;
		kernmod.mTargetId = 0;
		kernmod.mBase = kernmod.mpSymbols->GetDefaultBase();
		kernmod.mSize = kernmod.mpSymbols->GetDefaultSize();
		kernmod.mShortName = "kernel";
		kernmod.mName = "Kernel ROM";
		kernmod.mbDirty = false;
		kernmod.mbDirectivesProcessed = true;

		return kModuleId_KernelROM;
	}

	if (!wcscmp(path, L"kerneldb")) {
		UnloadSymbols(kModuleId_KernelDB);

		mModules.push_back(Module());
		Module& varmod = mModules.back();
		if (g_sim.GetHardwareMode() == kATHardwareMode_5200) {
			ATCreateDefaultVariableSymbolStore5200(~varmod.mpSymbols);
			varmod.mName = "Kernel Database (5200)";
		} else {
			ATCreateDefaultVariableSymbolStore(~varmod.mpSymbols);
			varmod.mName = "Kernel Database (800)";
		}
		varmod.mShortName = "kerneldb";

		varmod.mId = kModuleId_KernelDB;
		varmod.mTargetId = 0;
		varmod.mBase = varmod.mpSymbols->GetDefaultBase();
		varmod.mSize = varmod.mpSymbols->GetDefaultSize();
		varmod.mbDirty = false;
		varmod.mbDirectivesProcessed = true;

		return kModuleId_KernelDB;
	}

	if (!wcscmp(path, L"hardware")) {
		UnloadSymbols(kModuleId_Hardware);
		mModules.insert(mModules.begin(), Module());
		Module& hwmod = mModules.front();

		if (g_sim.GetHardwareMode() == kATHardwareMode_5200) {
			ATCreateDefault5200HardwareSymbolStore(~hwmod.mpSymbols);
			hwmod.mName = "Hardware (5200)";
		} else {
			ATCreateDefaultHardwareSymbolStore(~hwmod.mpSymbols);
			hwmod.mName = "Hardware (800)";
		}

		hwmod.mShortName = "hardware";
		hwmod.mId = kModuleId_Hardware;
		hwmod.mTargetId = 0;
		hwmod.mBase = hwmod.mpSymbols->GetDefaultBase();
		hwmod.mSize = hwmod.mpSymbols->GetDefaultSize();
		hwmod.mbDirty = false;
		hwmod.mbDirectivesProcessed = true;

		return kModuleId_Hardware;
	}

	const wchar_t *fullPath = path;
	VDStringW fullPathStr;
	if (ATVFSIsFilePath(path)) {
		fullPathStr = VDGetFullPath(path);
		fullPath = fullPathStr.c_str();
	}

	uint32 moduleId;

	if (loadImmediately || !mbDeferredSymbolLoadingEnabled) {
		ATLoadSymbols(path, ~symStore);

		moduleId = AddModule(targetId, symStore->GetDefaultBase(), symStore->GetDefaultSize(), symStore, VDTextWToA(path).c_str(), fullPath);

		if (processDirectives)
			ProcessSymbolDirectives(moduleId);
	
		ResolveDeferredBreakpoints();
	} else {
		vdrefptr<ATVFSFileView> view;
		ATVFSOpenFileView(fullPath, false, ~view);

		moduleId = AddModule(targetId, 0, 0, symStore, VDTextWToA(path).c_str(), fullPath);

		GetModuleById(moduleId)->mbDeferredLoad = true;
	}
	
	return moduleId;
}

void ATDebugger::UnloadSymbols(uint32 moduleId) {
	if (!moduleId)
		return;

	Modules::iterator it(mModules.begin()), itEnd(mModules.end());
	for(; it!=itEnd; ++it) {
		const Module& mod = *it;

		if (mod.mId == moduleId) {
			ClearSymbolDirectives(mod.mId);

			mModules.erase(it);
			mbSymbolUpdatePending = true;
			return;
		}
	}
}

void ATDebugger::LoadDeferredSymbols(uint32 moduleId) {
	Module *module = GetModuleById(moduleId);
	if (!module)
		return;

	if (!module->mbDeferredLoad)
		return;

	LoadDeferredSymbols(*module);
	ResolveDeferredBreakpoints();
	NotifyEvent(kATDebugEvent_SymbolsChanged);
}

void ATDebugger::LoadAllDeferredSymbols() {
	vdfastvector<uint32> loadedModIds;

	for(Module& module : mModules) {
		if (!module.mbDeferredLoad)
			continue;

		try {
			LoadDeferredSymbols(module);
			ATConsolePrintf("Loaded: %ls\n", module.mPath.c_str());

			loadedModIds.push_back(module.mId);
		} catch(const MyError& e) {
			ATConsolePrintf("Failed: %ls (%s)\n", module.mPath.c_str(), e.c_str());
		}
	}

	if (!loadedModIds.empty()) {
		// Process symbol directives for newly loaded modules. This must be a second pass
		// as there may be links between symbols loaded together.
		for(uint32 id : loadedModIds)
			ProcessSymbolDirectives(id);

		// Try to re-resolve any deferred breakpoints now that we have more symbols.
		ResolveDeferredBreakpoints();
		NotifyEvent(kATDebugEvent_SymbolsChanged);
	}
}

void ATDebugger::UpdateSymbolLoadMode() {
	ATDebuggerSymbolLoadMode mode = mSymbolLoadModes[mbEnabled ? 1 : 0];

	VDASSERT(mode != ATDebuggerSymbolLoadMode::Default);

	mbSymbolLoadingEnabled = (mode != ATDebuggerSymbolLoadMode::Disabled);
	mbDeferredSymbolLoadingEnabled = (mode == ATDebuggerSymbolLoadMode::Deferred);
}

void ATDebugger::LoadDeferredSymbols(Module& module) {
	if (!module.mbDeferredLoad)
		return;

	module.mbDeferredLoad = false;

	ATLoadSymbols(module.mPath.c_str(), ~module.mpSymbols);

	module.mBase = module.mpSymbols->GetDefaultBase();
	module.mSize = module.mpSymbols->GetDefaultSize();
	module.mbDirectivesProcessed = false;
}

void ATDebugger::ClearSymbolDirectives(uint32 moduleId) {
	// scan all breakpoints and clear those from this module
	uint32 n = (uint32)mUserBPs.size();

	for(uint32 i=0; i<n; ++i) {
		UserBP& ubp = mUserBPs[i];

		if (ubp.mModuleId == moduleId && ubp.mSysBP != (uint32)-1)
			ClearUserBreakpoint(i);
	}
}

void ATDebugger::ProcessSymbolDirectives(uint32 id) {
	Module *mod = GetModuleById(id);
	if (!mod)
		return;

	if (mod->mbDirectivesProcessed)
		return;

	mod->mbDirectivesProcessed = true;

	if (!mod->mpSymbols)
		return;

	uint32 directiveCount = mod->mpSymbols->GetDirectiveCount();

	for(uint32 i = 0; i < directiveCount; ++i) {
		ATSymbolDirectiveInfo dirInfo;

		mod->mpSymbols->GetDirective(i, dirInfo);

		switch(dirInfo.mType) {
			case kATSymbolDirType_Assert:
				{
					vdautoptr<ATDebugExpNode> expr;

					try {
						expr = ATDebuggerParseExpression(dirInfo.mpArguments, this, mExprOpts);
						
						vdautoptr<ATDebugExpNode> expr2(ATDebuggerInvertExpression(expr));
						expr.release();
						expr.swap(expr2);

						if (expr->mType == kATDebugExpNodeType_Const)
							ATConsolePrintf("Warning: ##ASSERT expression is a constant: %s\n", dirInfo.mpArguments);

						// try to do an address to line lookup
						ATSourceLineInfo srcLineInfo;
						VDStringA fileName;

						if (mod->mpSymbols->GetLineForOffset(dirInfo.mOffset, false, srcLineInfo)) {
							const wchar_t *s = VDFileSplitPath(mod->mpSymbols->GetFileName(srcLineInfo.mFileId));
							fileName = VDTextWToA(s);
						}

						VDStringA cmd;

						if (fileName.empty())
							cmd.sprintf(".printf \\\"Assert failed at $%04X: ", dirInfo.mOffset);
						else
							cmd.sprintf(".printf \\\"Assert failed at $%04X (%s:%u): ", dirInfo.mOffset, fileName.c_str(), srcLineInfo.mLine);

						for(const char *s = dirInfo.mpArguments; *s; ++s) {
							unsigned char c = *s;

							if (c == '%')
								cmd += '%';
							else if (c == '\\' || c == '"')
								cmd += '\\';
							else if (c < 0x20 || (c >= 0x7f && c < 0xa0))
								continue;

							cmd += c;
						}

						cmd += "\"";

						uint32 sysidx = mpBkptManager->SetAtPC(0, dirInfo.mOffset);
						uint32 useridx = RegisterSystemBreakpoint(sysidx, expr, cmd.c_str(), false);
						expr.release();
						RegisterUserBreakpoint(useridx, "directives");

						mUserBPs[useridx].mModuleId = id;
					} catch(const ATDebuggerExprParseException&) {
						ATConsolePrintf("Invalid assert directive expression: %s\n", dirInfo.mpArguments);
					}
				}
				break;

			case kATSymbolDirType_Trace:
				{
					vdfastvector<char> argstore;
					vdfastvector<const char *> argv;

					ATDebuggerParseArgv(dirInfo.mpArguments, argstore, argv);

					VDStringA cmd;

					cmd = "`.printf ";

					argv.pop_back();
					for(vdfastvector<const char *>::const_iterator it(argv.begin()), itEnd(argv.end()); it != itEnd; ++it) {
						const char *arg = *it;
						const char *argEnd = arg + strlen(arg);
						bool useQuotes = false;

						if (*arg == '"') {
							++arg;

							if (argEnd != arg && argEnd[-1] == '"')
								--argEnd;

							useQuotes = true;
						} else if (strchr(arg, ';'))
							useQuotes = true;
							

						if (useQuotes)
							cmd += "\\\"";

						cmd.append(arg, (uint32)(argEnd - arg));

						if (useQuotes)
							cmd += '"';

						cmd += ' ';
					}

					uint32 sysidx = mpBkptManager->SetAtPC(0, dirInfo.mOffset);
					uint32 useridx = RegisterSystemBreakpoint(sysidx, NULL, cmd.c_str(), true);
					RegisterUserBreakpoint(useridx, "directives");

					mUserBPs[useridx].mModuleId = id;
				}
				break;
		}
	}
}

sint32 ATDebugger::ResolveSourceLocation(const char *fn, uint32 line) {
	const VDStringW fnw(VDTextAToW(fn));

	uint32 moduleId;
	uint16 fileId;

	if (!LookupFile(fnw.c_str(), moduleId, fileId))
		return -1;

	Module *mod = GetModuleById(moduleId);

	ATSourceLineInfo lineInfo = {};
	lineInfo.mFileId = fileId;
	lineInfo.mLine = line;
	lineInfo.mOffset = 0;

	uint32 modOffset;
	if (mod->mpSymbols->GetOffsetForLine(lineInfo, modOffset))
		return mod->mBase + modOffset;

	return -1;
}

sint32 ATDebugger::ResolveSymbol(const char *s, bool allowGlobal, bool allowShortBase, bool allowNakedHex) {
	// check for type prefix
	uint32 addressSpace = kATAddressSpace_CPU;
	uint32 addressLimit = 0xffff;

	if (allowGlobal) {
		if (!strncmp(s, "v:", 2)) {
			addressSpace = kATAddressSpace_VBXE;
			s += 2;
		} else if (!strncmp(s, "n:", 2)) {
			addressSpace = kATAddressSpace_ANTIC;
			s += 2;
		} else if (!strncmp(s, "x:", 2)) {
			addressSpace = kATAddressSpace_EXTRAM;
			s += 2;
		} else if (!strncmp(s, "r:", 2)) {
			addressSpace = kATAddressSpace_RAM;
			s += 2;
		} else if (!strncmp(s, "rom:", 4)) {
			addressSpace = kATAddressSpace_ROM;
			s += 2;
		} else if (!strncmp(s, "cart:", 5)) {
			addressSpace = kATAddressSpace_CART;
			s += 2;
		}

		addressLimit = ATAddressGetSpaceSize(addressSpace) - 1;
	}

	if (!vdstricmp(s, "pc"))
		return g_sim.GetCPU().GetInsnPC();

	bool forceHex = false;
	if (s[0] == '$') {
		++s;
		forceHex = true;
	} else if (addressSpace == kATAddressSpace_CPU) {
		// check for a module name
		const char *modsplit = strchr(s, '!');
		size_t modnamelen = 0;
		const char *symname = s;

		if (modsplit) {
			modnamelen = modsplit - s;
			symname = modsplit + 1;
		}

		if (!mCurrentTargetIndex) {
			Modules::const_iterator it(mModules.begin()), itEnd(mModules.end());
			for(; it!=itEnd; ++it) {
				const Module& mod = *it;

				if (mod.mTargetId != mCurrentTargetIndex)
					continue;

				if (!mod.mpSymbols)
					continue;

				// check the module name if it is present
				if (modnamelen && (mod.mShortName.size() != modnamelen || mod.mShortName.comparei(VDStringSpanA(s, s + modnamelen))))
					continue;

				sint32 offset = mod.mpSymbols->LookupSymbol(symname);

				if (offset != -1)
					return mod.mBase + offset;
			}
		}
	}

	char *t;
	int base = forceHex || (allowNakedHex && (allowShortBase ? mExprOpts.mbAllowUntaggedHex : mExprOpts.mbDefaultHex)) ? 16 : 10;
	unsigned long result = strtoul(s, &t, base);

	// check for bank
	if (*t == ':' && addressSpace == kATAddressSpace_CPU) {
		if (result > 0xff)
			return -1;

		s = t+1;
		uint32 offset = strtoul(s, &t, base);
		if (offset > 0xffff || *t)
			return -1;

		return (result << 16) + offset;
	} else if (*t == '\'' && addressSpace == kATAddressSpace_CPU && allowGlobal) {
		if (result > 0xff)
			return -1;

		s = t+1;
		uint32 offset = strtoul(s, &t, base);
		if (offset > 0xffff || *t)
			return -1;

		return (result << 16) + offset + kATAddressSpace_PORTB;
	} else {
		if (result > addressLimit || *t)
			return -1;

		return result + addressSpace;
	}
}

uint32 ATDebugger::ResolveSymbolThrow(const char *s, bool allowGlobal, bool allowShortBase) {
	sint32 v = ResolveSymbol(s, allowGlobal, allowShortBase);

	if (v < 0)
		throw MyError("Unable to evaluate: %s", s);

	return (uint32)v;
}

void ATDebugger::EnumModuleSymbols(uint32 moduleId, ATCallbackHandler1<void, const ATSymbolInfo&> callback) const {
	const Module *mod = GetModuleById(moduleId);
	if (!mod)
		return;

	IATSymbolStore *syms = mod->mpSymbols;

	if (syms) {
		const uint32 n = syms->GetSymbolCount();

		ATSymbolInfo symInfo;
		for(uint32 i=0; i<n; ++i) {
			syms->GetSymbol(i, symInfo);

			symInfo.mOffset += mod->mBase;

			callback(symInfo);
		}
	}
}

void ATDebugger::EnumSourceFiles(const vdfunction<void(const wchar_t *, uint32)>& fn) const {
	for(const Module& mod : mModules) {
		if (!mod.mpSymbols)
			continue;

		IATSymbolStore& symstore = *mod.mpSymbols;
		vdfastvector<ATSourceLineInfo> lines;
		for(uint32 i=1; i<0x10000; ++i) {
			const wchar_t *s = symstore.GetFileName((uint16)i);

			if (!s)
				break;

			lines.clear();
			symstore.GetLines(i, lines);

			fn(s, (uint32)lines.size());
		}
	}
}

uint32 ATDebugger::AddCustomModule(uint32 targetId, const char *name, const char *shortname) {
	uint32 existingId = GetCustomModuleIdByShortName(shortname);
	if (existingId)
		return existingId;

	mModules.push_back(Module());
	Module& mod = mModules.back();

	mod.mName = name;
	mod.mShortName = shortname;
	mod.mId = mNextModuleId++;
	mod.mTargetId = targetId;
	mod.mBase = 0;
	mod.mSize = 0x10000;

	vdrefptr<IATCustomSymbolStore> p;
	ATCreateCustomSymbolStore(~p);

	p->Init(0, 0x10000);

	mod.mpSymbols = p;
	mod.mbDirty = false;
	mod.mbDirectivesProcessed = true;

	return mod.mId;
}

uint32 ATDebugger::GetCustomModuleIdByShortName(const char *shortname) {
	Modules::iterator it(mModules.begin()), itEnd(mModules.end());
	for(; it!=itEnd; ++it) {
		Module& mod = *it;

		if (!vdstricmp(shortname, mod.mShortName.c_str()))
			return mod.mId;
	}

	return 0;
}

void ATDebugger::AddCustomSymbol(uint32 address, uint32 len, const char *name, uint32 rwxmode, uint32 moduleId) {
	Module *mmod = NULL;

	if (moduleId) {
		mmod = GetModuleById(moduleId);
		if (!mmod)
			return;
	} else {
		Modules::iterator it(mModules.begin()), itEnd(mModules.end());
		for(; it!=itEnd; ++it) {
			Module& mod = *it;

			if (mod.mId == kModuleId_Manual) {
				mmod = &mod;
				break;
			}
		}
	}

	if (!mmod) {
		mModules.push_back(Module());
		Module& mod = mModules.back();

		mod.mName = "Manual";
		mod.mShortName = "manual";
		mod.mId = kModuleId_Manual;
		mod.mTargetId = mCurrentTargetIndex;
		mod.mBase = 0;
		mod.mSize = 0x1000000;

		vdrefptr<IATCustomSymbolStore> p;
		ATCreateCustomSymbolStore(~p);

		p->Init(0, 0x1000000);

		mod.mpSymbols = p;
		mod.mbDirty = false;
		mmod = &mod;
	}

	IATCustomSymbolStore *css = static_cast<IATCustomSymbolStore *>(&*mmod->mpSymbols);

	css->AddSymbol(address, name, len, rwxmode);
	mmod->mbDirty = true;

	mbSymbolUpdatePending = true;
}

void ATDebugger::RemoveCustomSymbol(uint32 address) {
	Module *mmod = NULL;

	Modules::iterator it(mModules.begin()), itEnd(mModules.end());
	for(; it!=itEnd; ++it) {
		Module& mod = *it;

		if (mod.mId == kModuleId_Manual) {
			mmod = &mod;
			break;
		}
	}

	if (!mmod)
		return;

	IATCustomSymbolStore *css = static_cast<IATCustomSymbolStore *>(&*mmod->mpSymbols);

	css->RemoveSymbol(address);
	mmod->mbDirty = true;

	mbSymbolUpdatePending = true;
}

void ATDebugger::LoadCustomSymbols(const wchar_t *filename) {
	UnloadSymbols(kModuleId_Manual);

	vdrefptr<IATSymbolStore> css;
	ATLoadSymbols(filename, ~css);

	mModules.push_back(Module());
	Module& mod = mModules.back();

	mod.mName = "Manual";
	mod.mId = kModuleId_Manual;
	mod.mTargetId = mCurrentTargetIndex;
	mod.mBase = css->GetDefaultBase();
	mod.mSize = css->GetDefaultSize();
	mod.mpSymbols = css;
	mod.mbDirty = false;
	mod.mbDirectivesProcessed = true;

	ATConsolePrintf("%d symbol(s) loaded.\n", css->GetSymbolCount());

	mbSymbolUpdatePending = true;
}

void ATDebugger::SaveCustomSymbols(const wchar_t *filename) {
	Module *mmod = NULL;

	Modules::iterator it(mModules.begin()), itEnd(mModules.end());
	for(; it!=itEnd; ++it) {
		Module& mod = *it;

		if (mod.mId == kModuleId_Manual) {
			mmod = &mod;
			break;
		}
	}

	if (!mmod)
		return;

	IATCustomSymbolStore *css = static_cast<IATCustomSymbolStore *>(&*mmod->mpSymbols);

	ATSaveSymbols(filename, css);

	mmod->mbDirty = false;
}

VDStringA ATDebugger::GetAddressText(uint32 globalAddr, bool useHexSpecifier, bool addSymbolInfo) {
	VDStringA s;
	const char *prefix = useHexSpecifier ? "$" : "";

	switch(globalAddr & kATAddressSpaceMask) {
		case kATAddressSpace_CPU:
			if (globalAddr & 0xff0000)
				s.sprintf("%s%02X:%04X", prefix, (globalAddr >> 16) & 0xff, globalAddr & 0xffff);
			else
				s.sprintf("%s%04X", prefix, globalAddr & 0xffff);
			break;
		case kATAddressSpace_VBXE:
			s.sprintf("v:%s%05X", prefix, globalAddr & 0x7ffff);
			break;
		case kATAddressSpace_ANTIC:
			s.sprintf("n:%s%04X", prefix, globalAddr & 0xffff);
			break;
		case kATAddressSpace_EXTRAM:
			s.sprintf("x:%s%05X", prefix, globalAddr & 0xfffff);
			break;
		case kATAddressSpace_RAM:
			s.sprintf("r:%s%04X", prefix, globalAddr & 0xffff);
			break;
		case kATAddressSpace_ROM:
			s.sprintf("rom:%s%04X", prefix, globalAddr & 0xffff);
			break;
		case kATAddressSpace_CART:
			s.sprintf("cart:%s%04X", prefix, globalAddr & 0xffffff);
			break;
		case kATAddressSpace_PORTB:
			s.sprintf("%s%02X'%04X", prefix, (globalAddr >> 16) & 0xff, globalAddr & 0xffff);
			break;
		case kATAddressSpace_CB:
			s.sprintf("t:%s%02X'%04X", prefix, (globalAddr >> 16) & 0xff, globalAddr & 0xffff);
			break;
	}

	if (addSymbolInfo) {
		ATSymbol sym;
		if (LookupSymbol(globalAddr, kATSymbol_Any, sym)) {
			if (sym.mOffset != globalAddr)
				s.append_sprintf(" (%s+%d)", sym.mpName, globalAddr - sym.mOffset);
			else
				s.append_sprintf(" (%s)", sym.mpName);
		}
	}

	return s;
}

sint32 ATDebugger::EvaluateThrow(const char *s) {
	vdautoptr<ATDebugExpNode> node;
	try {
		node = ATDebuggerParseExpression(s, this, GetExprOpts());
	} catch(const ATDebuggerExprParseException& ex) {
		throw MyError("Unable to parse expression '%s': %s\n", s, ex.c_str());
	}

	sint32 v;
	if (!node->Evaluate(v, GetEvalContext()))
		throw MyError("Cannot evaluate '%s' in this context.", s);

	return v;
}

std::pair<bool, sint32> ATDebugger::Evaluate(ATDebugExpNode *expr) {
	sint32 v;
	if (!expr->Evaluate(v, GetEvalContext()))
		return { false, 0 };

	return { true, v };
}

ATDebugExpEvalContext ATDebugger::GetEvalContext() const {
	return GetEvalContextForTarget(mCurrentTargetIndex);
}

ATDebugExpEvalContext ATDebugger::GetEvalContextForTarget(uint32 targetIndex) const {
	ATDebugExpEvalContext ctx {};

	if (!targetIndex) {
		ctx.mpAntic = &g_sim.GetAntic();
		ctx.mpMMU = g_sim.GetMMU();
		ctx.mpXPCFn = [](void *) -> uint32 {
			const auto& cpu = g_sim.GetCPU();

			uint8 bank = cpu.GetK();
			uint32 xpc = cpu.GetPC();

			if (bank)
				xpc += (uint32)bank << 16;
			else
				xpc += cpu.GetMemory()->mpCPUReadAddressPageMap[xpc >> 8];

			return xpc;
		};
	}

	ctx.mpTarget = mDebugTargets[targetIndex];
	ctx.mpTemporaries = mExprTemporaries;
	ctx.mpClockFn = [](void *) -> uint32 { return ATSCHEDULER_GETTIME(g_sim.GetScheduler()); };
	ctx.mpCpuClockFn = [](void *) -> uint32 { return g_sim.GetCpuCycleCounter(); };
	ctx.mbAccessValid = true;
	ctx.mbAccessReadValid = false;
	ctx.mbAccessWriteValid = false;
	ctx.mAccessAddress = mExprAddress;
	ctx.mAccessValue = mExprValue;

	return ctx;
}

void ATDebugger::GetDirtyStorage(vdfastvector<ATDebuggerStorageId>& ids) const {
	Modules::const_iterator it(mModules.begin()), itEnd(mModules.end());
	for(; it!=itEnd; ++it) {
		const Module& mod = *it;

		if (mod.mId == kModuleId_Manual && mod.mbDirty) {
			ids.push_back(kATDebuggerStorageId_CustomSymbols);
		}
	}
}

bool ATDebugger::InvokeCommand(const char *name, ATDebuggerCmdParser& parser) const {
	Commands::const_iterator it = mCommands.find_as(VDStringSpanA(name));

	if (it == mCommands.end())
		return false;

	it->second(parser);
	return true;
}

void ATDebugger::AddCommand(const char *name, void (*pfn)(ATDebuggerCmdParser&)) {
	mCommands[VDStringA(name)] = pfn;
}

void ATDebugger::QueueBatchFile(const wchar_t *path) {
	VDTextInputFile tif(path);

	std::deque<VDStringA> commands;

	while(const char *s = tif.GetNextLine()) {
		while(isspace((unsigned char)*s))
			++s;

		if (!*s)
			continue;

		if (*s == '#')
			continue;

		commands.emplace_back(s);
	}

	while(!commands.empty()) {
		QueueCommandFront(commands.back().c_str(), false);
		commands.pop_back();
	}
}

void ATDebugger::QueueAutoLoadBatchFile(const wchar_t *path) {
	if (!mbSymbolLoadingEnabled)
		return;

	if (mScriptAutoLoadMode == ATDebuggerScriptAutoLoadMode::Disabled)
		return;

	// don't load from non-file VFS paths
	if (!ATVFSIsFilePath(path))
		return;

	// silently fail if file doesn't exist
	if (!VDDoesPathExist(path))
		return;
	
	if (mScriptAutoLoadMode == ATDebuggerScriptAutoLoadMode::AskToLoad) {
		if (mpScriptAutoLoadConfirmFn && !mpScriptAutoLoadConfirmFn())
			return;
	}

	LoadAllDeferredSymbols();

	try {
		if (g_sim.IsRunning()) {
			// give the debugger script a chance to run
			g_sim.Suspend();
			QueueCommandFront("`g -n", false);
		}

		QueueBatchFile(path);

		ATConsolePrintf("Loaded debugger script %ls\n", path);
	} catch(const MyError&) {
		// ignore
	}
}

void ATDebugger::DefineCommands(const ATDebuggerCmdDef *defs, size_t numDefs) {
	while(numDefs--) {
		AddCommand(defs->mpName, defs->mpFunction);
		++defs;
	}
}

bool ATDebugger::IsCommandAliasPresent(const char *alias) const {
	return mAliases.find_as(alias) != mAliases.end();
}

bool ATDebugger::MatchCommandAlias(const char *alias, const char *const *argv, int argc, vdfastvector<char>& tempstr, vdfastvector<const char *>& argptrs) const {
	Aliases::const_iterator it = mAliases.find_as(alias);

	if (it == mAliases.end())
		return false;

	AliasList::const_iterator it2(it->second.begin()), it2End(it->second.end());

	VDStringRefA patargs[10];

	for(; it2 != it2End; ++it2) {
		VDStringRefA argpat(it2->first.c_str());
		VDStringRefA pattoken;

		uint32 patargsvalid = 0;
		const char *const *wildargs = NULL;

		int patidx = 0;
		bool valid = true;

		while(!argpat.empty()) {
			if (!argpat.split(' ', pattoken)) {
				pattoken = argpat;
				argpat.clear();
			}

			// check for wild pattern
			if (pattoken == "%*") {
				wildargs = argv + patidx;
				patidx = argc;
				break;
			}

			// check for insufficient arguments
			if (patidx >= argc) {
				valid = false;
				break;
			}

			// match pattern
			VDStringRefA::const_iterator itPatToken(pattoken.begin()), itPatTokenEnd(pattoken.end());
			const char *s = argv[patidx];

			bool percent = false;
			while(itPatToken != itPatTokenEnd) {
				char c = *itPatToken++;

				if (c == '%') {
					percent = !percent;

					if (percent)
						continue;
				} else if (percent) {
					if (c >= '0' && c <= '9') {
						patargsvalid |= 1 << (c - '0');

						patargs[c - '0'] = s;
						percent = false;
						break;
					} else {
						valid = false;
						break;
					}
				}

				if (!*s || *s != c) {
					valid = false;
					break;
				}

				++s;
			}

			if (percent)
				valid = false;

			if (!valid)
				break;

			++patidx;
		}

		// check for extra args
		if (argc != patidx)
			continue;

		if (!valid)
			continue;

		// We have a match -- apply result template.
		VDStringRefA tmpl(it2->second.c_str());
		VDStringRefA tmpltoken;

		vdfastvector<int> argoffsets;

		while(!tmpl.empty()) {
			if (!tmpl.split(' ', tmpltoken)) {
				tmpltoken = tmpl;
				tmpl.clear();
			}

			if (tmpltoken == "%*") {
				if (wildargs) {
					while(const char *arg = *wildargs++) {
						argoffsets.push_back((int)tempstr.size());
						tempstr.insert(tempstr.end(), arg, arg + strlen(arg) + 1);
					}
				}

				continue;
			}

			argoffsets.push_back((int)tempstr.size());

			VDStringRefA::const_iterator itTmplToken(tmpltoken.begin()), itTmplTokenEnd(tmpltoken.end());

			bool percent = false;
			for(; itTmplToken != itTmplTokenEnd; ++itTmplToken) {
				char c = *itTmplToken;

				if (c == '%') {
					percent = !percent;

					if (percent)
						continue;
				}

				if (percent) {
					if (c >= '0' && c <= '9') {
						int idx = c - '0';

						if (patargsvalid & (1 << idx)) {
							const VDStringRefA& insarg = patargs[idx];
							tempstr.insert(tempstr.end(), insarg.begin(), insarg.end());
						}
					}

					continue;
				}

				tempstr.push_back(c);
			}

			tempstr.push_back(0);
		}

		argptrs.reserve(argoffsets.size() + 1);

		for(vdfastvector<int>::const_iterator itOff(argoffsets.begin()), itOffEnd(argoffsets.end());
			itOff != itOffEnd;
			++itOff)
		{
			argptrs.push_back(tempstr.data() + *itOff);
		}

		argptrs.push_back(NULL);

		return true;
	}

	argptrs.clear();
	return true;
}

const char *ATDebugger::GetCommandAlias(const char *alias, const char *args) const {
	Aliases::const_iterator it = mAliases.find_as(alias);

	if (it == mAliases.end())
		return NULL;

	AliasList::const_iterator it2(it->second.begin()), it2End(it->second.end());

	for(; it2 != it2End; ++it2) {
		if (it2->first == args)
			return it2->second.c_str();
	}

	return NULL;
}

void ATDebugger::SetCommandAlias(const char *alias, const char *args, const char *command) {
	if (command) {
		Aliases::iterator it(mAliases.insert_as(alias).first);

		if (args) {
			AliasList::iterator it2(it->second.begin()), it2End(it->second.end());

			for(; it2 != it2End; ++it2) {
				if (it2->first == args)
					return;
			}

			it->second.push_back(AliasList::value_type(VDStringA(args), VDStringA(command)));
		} else {
			it->second.resize(1);
			it->second.back().first = "%*";
			it->second.back().second = command;
		}
	} else {
		Aliases::iterator it(mAliases.find_as(alias));

		if (it != mAliases.end()) {
			if (args) {
				AliasList::iterator it2(it->second.begin()), it2End(it->second.end());

				for(; it2 != it2End; ++it2) {
					if (it2->first == args) {
						it->second.erase(it2);

						if (it->second.empty())
							mAliases.erase(it);
					}
				}
			} else {
				mAliases.erase(it);
			}
		}
	}
}

struct ATDebugger::AliasSorter {
	typedef std::pair<const char *, const AliasList *> value_type;

	bool operator()(const value_type& x, const value_type& y) const {
		return strcmp(x.first, y.first) < 0;
	}
};

void ATDebugger::ListCommandAliases() {
	if (mAliases.empty()) {
		ATConsoleWrite("No command aliases defined.\n");
		return;
	}

	typedef vdfastvector<std::pair<const char *, const AliasList *> > SortedAliases;
	SortedAliases sortedAliases;
	sortedAliases.reserve(mAliases.size());

	for(Aliases::const_iterator it(mAliases.begin()), itEnd(mAliases.end());
		it != itEnd;
		++it)
	{
		sortedAliases.push_back(std::make_pair(it->first.c_str(), &it->second));
	}

	std::sort(sortedAliases.begin(), sortedAliases.end(), AliasSorter());

	ATConsoleWrite("Current command aliases:\n");

	VDStringA s;
	for(SortedAliases::const_iterator it(sortedAliases.begin()), itEnd(sortedAliases.end());
		it != itEnd;
		++it)
	{
		const AliasList& al = *it->second;

		for(AliasList::const_iterator it2(al.begin()), it2End(al.end()); it2 != it2End; ++it2) {
			s = it->first;
			s += ' ';
			s += it2->first;

			if (s.size() < 10)
				s.resize(10, ' ');

			s += " -> ";
			s += it2->second;
			s += '\n';

			ATConsoleWrite(s.c_str());
		}
	}
}

void ATDebugger::ClearCommandAliases() {
	mAliases.clear();
}

void ATDebugger::OnExeQueueCmd(bool onrun, const char *s) {
	mOnExeCmds[onrun].push_back(VDStringA());
	mOnExeCmds[onrun].back() = s;
}

void ATDebugger::OnExeClear() {
	mOnExeCmds[0].clear();
	mOnExeCmds[1].clear();
}

bool ATDebugger::OnExeGetCmd(bool onrun, int index, VDStringA& s) {
	OnExeCmds& cmds = mOnExeCmds[onrun];

	if (index < 0 || (unsigned)index >= cmds.size())
		return false;

	s = cmds[index];
	return true;
}

bool ATDebugger::SetTarget(uint32 index) {
	if (index == mCurrentTargetIndex)
		return true;

	if (index >= mDebugTargets.size())
		return false;

	IATDebugTarget *target = mDebugTargets[index];

	if (!target)
		return false;

	mCurrentTargetIndex = index;
	mpCurrentTarget = target;

	mFramePC = GetPC();

	UpdateClientSystemState(nullptr);
	UpdatePrompt();

	return true;
}

void ATDebugger::GetTargetList(vdfastvector<IATDebugTarget *>& targets) {
	targets = mDebugTargets;
}

bool ATDebugger::GetSourceFilePath(uint32 moduleId, uint16 fileId, VDStringW& path) {
	Modules::const_iterator it(mModules.begin()), itEnd(mModules.end());
	for(; it!=itEnd; ++it) {
		const Module& mod = *it;

		if (mod.mId == moduleId) {
			if (!mod.mpSymbols)
				return false;

			const wchar_t *s = mod.mpSymbols->GetFileName(fileId);
			if (!s)
				return false;

			path = s;
			return true;
		}
	}

	return false;
}

bool ATDebugger::LookupSymbol(uint32 addr, uint32 flags, ATSymbol& symbol) {
	ATDebuggerSymbol symbol2;

	if (!LookupSymbol(addr, flags, symbol2))
		return false;

	symbol = symbol2.mSymbol;
	return true;
}

bool ATDebugger::LookupSymbol(uint32 addr, uint32 flags, ATDebuggerSymbol& symbol) {
	uint32 addrSpaceOffset = 0;

	if ((addr & kATAddressSpaceMask) == kATAddressSpace_PORTB) {
		addrSpaceOffset = kATAddressSpace_PORTB;
		addr &= 0xffffff;
	}

	Modules::const_iterator it(mModules.begin()), itEnd(mModules.end());
	int bestDelta = INT_MAX;
	bool valid = false;

	for(; it!=itEnd; ++it) {
		const Module& mod = *it;

		if (mod.mTargetId != mCurrentTargetIndex)
			continue;

		uint32 offset = addr - mod.mBase;

		if (offset < mod.mSize && mod.mpSymbols) {
			ATDebuggerSymbol tempSymbol;
			if (mod.mpSymbols->LookupSymbol(offset, flags, tempSymbol.mSymbol)) {
				tempSymbol.mSymbol.mOffset += mod.mBase;
				tempSymbol.mSymbol.mOffset += addrSpaceOffset;
				tempSymbol.mModuleId = mod.mId;

				int delta = (int)tempSymbol.mSymbol.mOffset - (int)addr;

				if (bestDelta > delta) {
					bestDelta = delta;
					symbol = tempSymbol;
					valid = true;

					if (!bestDelta)
						break;
				}
			}
		}
	}

	return valid;
}

bool ATDebugger::LookupLine(uint32 addr, bool searchUp, uint32& moduleId, ATSourceLineInfo& lineInfo) {
	if (mCurrentTargetIndex)
		return false;

	if ((addr & kATAddressSpaceMask) == kATAddressSpace_PORTB)
		addr &= 0xffffff;

	Modules::const_iterator it(mModules.begin()), itEnd(mModules.end());
	uint32 bestOffset = 0xFFFFFFFFUL;
	bool valid = false;

	for(; it!=itEnd; ++it) {
		const Module& mod = *it;

		if (mod.mTargetId != mCurrentTargetIndex)
			continue;

		uint32 offset = addr - mod.mBase;

		if (offset < mod.mSize && mod.mpSymbols) {
			ATSourceLineInfo tempLineInfo;

			if (mod.mpSymbols->GetLineForOffset(offset, searchUp, tempLineInfo)) {
				uint32 lineOffset = tempLineInfo.mOffset - offset;

				if (bestOffset > lineOffset) {
					bestOffset = lineOffset;

					moduleId = mod.mId;
					lineInfo = tempLineInfo;
					valid = true;
				}
			}
		}
	}

	return valid;
}

bool ATDebugger::LookupFile(const wchar_t *fileName, uint32& moduleId, uint16& fileId) {
	int bestQuality = 0;

	Modules::const_iterator it(mModules.begin()), itEnd(mModules.end());
	for(; it!=itEnd; ++it) {
		const Module& mod = *it;

		if (mod.mTargetId != mCurrentTargetIndex)
			continue;

		int q;
		uint16 fid = mod.mpSymbols->GetFileId(fileName, &q);

		if (fid && q > bestQuality) {
			bestQuality = q;
			moduleId = mod.mId;
			fileId = fid;
		}
	}

	return bestQuality > 0;
}

void ATDebugger::GetLinesForFile(uint32 moduleId, uint16 fileId, vdfastvector<ATSourceLineInfo>& lines) {
	Modules::const_iterator it(mModules.begin()), itEnd(mModules.end());
	for(; it!=itEnd; ++it) {
		const Module& mod = *it;

		if (mod.mId == moduleId) {
			mod.mpSymbols->GetLines(fileId, lines);
			break;
		}
	}
}

void ATDebugger::OnSimulatorEvent(ATSimulatorEvent ev) {
	switch(ev) {
		case kATSimEvent_VBLANK:
		case kATSimEvent_TracingLimitReached:
			return;
	}

	if (ev == kATSimEvent_FrameTick) {
		IATUIRenderer *r = g_sim.GetUIRenderer();

		if (r) {
			for(int i=0; i<8; ++i) {
				const auto& watch = mWatches[i];

				switch(watch.mLength) {
					case 2:
						{
							alignas(uint16) char buf[2];
							mDebugTargets[watch.mTargetIndex]->DebugReadMemory(watch.mAddress, buf, 2);
							r->SetWatchedValue(i, VDReadUnalignedLEU16(buf), 2);
						}
						break;
					case 1:
						r->SetWatchedValue(i, mDebugTargets[watch.mTargetIndex]->DebugReadByte(watch.mAddress), 1);
						break;
					case 0:
						{
							const ATDebugExpEvalContext& ctx = GetEvalContextForTarget(watch.mTargetIndex);

							sint32 result;
							r->SetWatchedValue(i, watch.mpExpr->Evaluate(result, ctx) ? result : 0, 0);
						}
						break;

					default:
						r->ClearWatchedValue(i);
						break;
				}
			}
		}

		return;
	}

	if (ev == kATSimEvent_VBI) {
		if (mRunState == kRunState_RunToVBI2) {
			ATCPUEmulator& cpu = g_sim.GetCPU();

			cpu.SetStepNMI();
		}

		return;
	}

	if (ev == kATSimEvent_WarmReset) {
		ClearOnResetBreakpoints();
		return;
	}

	if (ev == kATSimEvent_ColdReset) {
		ClearOnResetBreakpoints();

		if (mSysBPEEXRun) {
			mpBkptManager->Clear(mSysBPEEXRun);
			mSysBPEEXRun = 0;
		}

		static const wchar_t *const kReloadModules[]={
			L"kernel", L"kerneldb", L"hardware"
		};

		for(const wchar_t *modname : kReloadModules) {
			try {
				LoadSymbols(modname, false);
			} catch(const MyError&) {
				// ignore
			}
		}

		return;
	}

	ATCPUEmulator& cpu = g_sim.GetCPU();

	if (ev == kATSimEvent_StateLoaded) {
		ATConsoleWrite("Save state loaded.\n");

		if (mRunState)
			return;

		DumpState();
	} else if (ev == kATSimEvent_EXELoad) {
		if (!mOnExeCmds[0].empty()) {
			g_sim.Suspend();

			for(OnExeCmds::const_iterator it(mOnExeCmds[0].begin()), itEnd(mOnExeCmds[0].end());
				it != itEnd;
				++it)
			{
				QueueCommand(it->c_str(), false);
			}
		}
		return;
	} else if (ev == kATSimEvent_EXEInitSegment) {
		return;
	} else if (ev == kATSimEvent_AbnormalDMA) {
		return;
	} else if (ev == kATSimEvent_EXERunSegment) {
		if (!mOnExeCmds[1].empty()) {
			g_sim.Suspend();

			for(OnExeCmds::const_iterator it(mOnExeCmds[1].begin()), itEnd(mOnExeCmds[1].end());
				it != itEnd;
				++it)
			{
				QueueCommand(it->c_str(), false);
			}
		}

		if (!mbBreakOnEXERunAddr)
			return;

		if (mSysBPEEXRun)
			mpBkptManager->Clear(mSysBPEEXRun);

		mSysBPEEXRun = mpBkptManager->SetAtPC(0, cpu.GetPC());
		return;
	}

	if (ev == kATSimEvent_CPUPCBreakpointsUpdated) {
		NotifyEvent(kATDebugEvent_BreakpointsChanged);
		return;
	}

	if (ev == kATSimEvent_ScanlineBreakpoint && mRunState == kRunState_RunToVBI1) {
		if (g_sim.GetAntic().IsVBIEnabled()) {
			mRunState = kRunState_RunToVBI2;
			cpu.SetStepNMI();
			g_sim.SetBreakOnScanline(249);
			ATConsoleWrite("Vertical blank interrupt routine reached.\n");
			return;
		}

		// fall through and allow normal stop
	}

	if (!ATIsDebugConsoleActive()) {
		ATDebuggerOpenEvent event;
		event.mbAllowOpen = true;

		mEventOpen.Raise(this, &event);

		if (!event.mbAllowOpen)
			return;

		ATSetFullscreen(false);
		ATOpenConsole();
	}

	switch(ev) {
		case kATSimEvent_CPUSingleStep:
			if (mRunState == kRunState_RunToVBI2)
				g_sim.SetBreakOnScanline(-1);

			// fall through
		case kATSimEvent_CPUStackBreakpoint:
			SetTarget(0);
		case kATSimEvent_CPUPCBreakpoint:
			DumpState();
			InterruptRun();
			break;

		case kATSimEvent_CPUIllegalInsn:
			ATConsolePrintf("CPU: Illegal instruction hit: %04X\n", cpu.GetPC());
			DumpState();
			InterruptRun();
			break;

		case kATSimEvent_CPUNewPath:
			ATConsoleWrite("CPU: New path encountered with path break enabled.\n");
			SetTarget(0);
			DumpState();
			InterruptRun();
			break;

		case kATSimEvent_ReadBreakpoint:
		case kATSimEvent_WriteBreakpoint:
			DumpState();
			InterruptRun();
			break;

		case kATSimEvent_DiskSectorBreakpoint:
			ATConsolePrintf("DISK: Sector breakpoint hit: %d\n", g_sim.GetDiskInterface(0).GetSectorBreakpoint());
			InterruptRun();
			break;

		case kATSimEvent_EndOfFrame:
			ATConsoleWrite("End of frame reached.\n");
			InterruptRun();
			break;

		case kATSimEvent_ScanlineBreakpoint:
			ATConsoleWrite("Scanline breakpoint reached.\n");
			DumpState();
			InterruptRun();
			break;

		case kATSimEvent_VerifierFailure:
			SetTarget(0);
			DumpState();
			InterruptRun();
			break;
	}

	cpu.SetRTSBreak();

	if (ev != kATSimEvent_CPUPCBreakpointsUpdated) {
		mFramePC = GetPC();
	}

	mbClientUpdatePending = true;

	if (mbSourceMode && !mRunState)
		ActivateSourceWindow();
}

void ATDebugger::OnDeviceAdded(uint32 iid, IATDevice *dev, void *iface) {
	IATDeviceDebugTarget *devTarget = (IATDeviceDebugTarget *)iface;
	uint32 idx = 0;

	const bool enableHistory = g_sim.GetCPU().IsHistoryEnabled();

	while(IATDebugTarget *target = devTarget->GetDebugTarget(idx++)) {
		VDASSERT(std::find(mDebugTargets.begin(), mDebugTargets.end(), target) == mDebugTargets.end());

		IATDebugTargetHistory *thist = vdpoly_cast<IATDebugTargetHistory *>(target);
		thist->SetHistoryEnabled(enableHistory);

		auto itFree = std::find(mDebugTargets.begin(), mDebugTargets.end(), (IATDebugTarget *)nullptr);
		const uint32 targetIndex = itFree - mDebugTargets.begin();

		if (itFree != mDebugTargets.end())
			*itFree = target;
		else
			mDebugTargets.push_back(target);

		mpBkptManager->AttachTarget(targetIndex, target);

		if (mDebugTargets.size() == 2)
			UpdatePrompt();
	}
}

void ATDebugger::OnDeviceRemoving(uint32 iid, IATDevice *dev, void *iface) {
	IATDeviceDebugTarget *devTarget = (IATDeviceDebugTarget *)iface;
	uint32 idx = 0;

	while(IATDebugTarget *target = devTarget->GetDebugTarget(idx++)) {
		auto itFree = std::find(mDebugTargets.begin(), mDebugTargets.end(), target);

		if (itFree != mDebugTargets.end()) {
			const uint32 targetIndex = (uint32)(itFree - mDebugTargets.begin());

			// unload all modules referencing this target
			vdfastvector<uint32> modulesToUnload;
			for(const Module& mod : mModules) {
				if (mod.mTargetId == targetIndex) {
					modulesToUnload.push_back(mod.mId);
				}
			}

			for(uint32 id : modulesToUnload)
				UnloadSymbols(id);
			
			mpBkptManager->DetachTarget(targetIndex);

			// clear all watches associated with target
			for(auto& watch : mWatches) {
				if (watch.mTargetIndex == targetIndex) {
					watch.mTargetIndex = 0;
					watch.mLength = -1;
					watch.mpExpr.reset();
				}
			}

			*itFree = nullptr;

			if (mpCurrentTarget == target) {
				mpCurrentTarget = nullptr;

				while(!mpCurrentTarget) {
					mpCurrentTarget = mDebugTargets[--mCurrentTargetIndex];
				}

				UpdateClientSystemState(nullptr);
			}

			while(!mDebugTargets.empty() && !mDebugTargets.back())
				mDebugTargets.pop_back();

			if (mDebugTargets.size() == 1)
				UpdatePrompt();
		}
	}
}

void ATDebugger::OnDeviceRemoved(uint32 iid, IATDevice *dev, void *iface) {
}

void ATDebugger::ResolveDeferredBreakpoints() {
	bool breakpointsChanged = false;

	UserBPs::iterator it(mUserBPs.begin()), itEnd(mUserBPs.end());
	for(; it != itEnd; ++it) {
		UserBP& ubp = *it;

		if (ubp.mSysBP || ubp.mSource.empty())
			continue;

		sint32 addr = ResolveSourceLocation(ubp.mSource.c_str(), ubp.mSourceLine);
		if (addr < 0)
			continue;

		ubp.mSysBP = mpBkptManager->SetAtPC(ubp.mTargetIndex, (uint32)addr);
		mSysBPToUserBPMap[ubp.mSysBP] = (uint32)(it - mUserBPs.begin());
		breakpointsChanged = true;
	}

	NotifyEvent(kATDebugEvent_BreakpointsChanged);
}

void ATDebugger::UpdateClientSystemState(IATDebuggerClient *client) {
	if (!client)
		mbClientUpdatePending = false;

	ATDebuggerSystemState sysstate;

	sysstate.mExecMode = mpCurrentTarget->GetDisasmMode();

	mpCurrentTarget->GetExecState(sysstate.mExecState);

	if (mCurrentTargetIndex) {
		switch(sysstate.mExecMode) {
			case kATDebugDisasmMode_Z80:
				sysstate.mPC = sysstate.mExecState.mZ80.mPC;
				break;

			case kATDebugDisasmMode_8048:
				sysstate.mPC = sysstate.mExecState.m8048.mPC;
				break;

			case kATDebugDisasmMode_6809:
				sysstate.mPC = sysstate.mExecState.m6809.mPC;
				break;
		
			default:
				sysstate.mPC = sysstate.mExecState.m6502.mPC;
				break;
		}

		sysstate.mPCBank = 0;
		sysstate.mInsnPC = sysstate.mPC;
	} else {
		ATCPUEmulator& cpu = g_sim.GetCPU();
		sysstate.mPC = cpu.GetPC();
		sysstate.mPCBank = sysstate.mExecState.m6502.mK;
		sysstate.mInsnPC = sysstate.mExecState.m6502.mPC;
	}

	sysstate.mPCModuleId = 0;
	sysstate.mPCFileId = 0;
	sysstate.mPCLine = 0;
	sysstate.mFramePC = mFramePC;
	sysstate.mbRunning = g_sim.IsRunning();
	sysstate.mpDebugTarget = mpCurrentTarget;

	if (mbClientLastRunState != sysstate.mbRunning) {
		mbClientLastRunState = sysstate.mbRunning;

		mContinuationAddress = sysstate.mInsnPC + ((uint32)sysstate.mPCBank << 16);

		mEventRunStateChanged.Raise(this, sysstate.mbRunning);
	}

	if (!sysstate.mbRunning) {
		ATSourceLineInfo lineInfo;
		if (LookupLine(sysstate.mPC, false, sysstate.mPCModuleId, lineInfo)) {
			sysstate.mPCFileId = lineInfo.mFileId;
			sysstate.mPCLine = lineInfo.mLine;
		}
	}

	if (client)
		client->OnDebuggerSystemStateUpdate(sysstate);
	else {
		Clients::const_iterator it(mClients.begin()), itEnd(mClients.end());
		for(; it!=itEnd; ++it) {
			IATDebuggerClient *client = *it;

			client->OnDebuggerSystemStateUpdate(sysstate);
		}
	}
}

void ATDebugger::ActivateSourceWindow() {
	uint32 moduleId;
	ATSourceLineInfo lineInfo;
	IATDebuggerSymbolLookup *lookup = ATGetDebuggerSymbolLookup();
	if (!lookup->LookupLine(mFramePC, false, moduleId, lineInfo) || (uint32)mFramePC - lineInfo.mOffset >= 100)
		return;

	if (!lineInfo.mLine)
		return;

	Module *mod = GetModuleById(moduleId);
	if (mod) {
		if (std::binary_search(mod->mSilentlyIgnoredFiles.begin(), mod->mSilentlyIgnoredFiles.end(), lineInfo.mFileId))
			return;
	}

	VDStringW path;
	if (!lookup->GetSourceFilePath(moduleId, lineInfo.mFileId, path)) {
		if (ATGetUIPane(kATUIPaneId_Disassembly))
			ATActivateUIPane(kATUIPaneId_Disassembly, true);
		return;
	}

	IATSourceWindow *w = ATOpenSourceWindow(path.c_str());
	if (!w) {
		if (mod)
			mod->mSilentlyIgnoredFiles.insert(std::lower_bound(mod->mSilentlyIgnoredFiles.begin(), mod->mSilentlyIgnoredFiles.end(), lineInfo.mFileId), lineInfo.mFileId);

		return;
	}

	w->ActivateLine(lineInfo.mLine - 1);
}

const ATDebugger::Module *ATDebugger::GetModuleById(uint32 id) const {
	Modules::const_iterator it(mModules.begin()), itEnd(mModules.end());
	for(; it != itEnd; ++it) {
		const Module& mod = *it;

		if (mod.mId == id)
			return &mod;
	}

	return NULL;
}

ATDebugger::Module *ATDebugger::GetModuleById(uint32 id) {
	Modules::iterator it(mModules.begin()), itEnd(mModules.end());
	for(; it != itEnd; ++it) {
		Module& mod = *it;

		if (mod.mId == id)
			return &mod;
	}

	return NULL;
}

void ATDebugger::NotifyEvent(ATDebugEvent eventId) {
	VDVERIFY(++mClientsBusy < 100);

	// Note that this list may change on the fly.
	size_t n = mClients.size();
	for(uint32 i=0; i<n; ++i) {
		IATDebuggerClient *cb = mClients[i];

		if (cb)
			cb->OnDebuggerEvent(eventId);
	}

	VDVERIFY(--mClientsBusy >= 0);

	if (!mClientsBusy && mbClientsChanged) {
		Clients::iterator src = mClients.begin();
		Clients::iterator dst = src;
		Clients::iterator end = mClients.end();

		for(; src != end; ++src) {
			IATDebuggerClient *cb = *src;

			if (cb) {
				*dst = cb;
				++dst;
			}
		}

		if (dst != end)
			mClients.erase(dst, end);

		mbClientsChanged = false;
	}
}

void ATDebugger::OnBreakpointHit(ATBreakpointManager *sender, ATBreakpointEvent *event) {
	if (event->mIndex == mSysBPTraceCIO) {
		DumpCIOParameters();
		return;
	}

	if (event->mIndex == mSysBPTraceSIO) {
		DumpSIOParameters();
		return;
	}

	if (event->mIndex == (uint32)mSysBPEEXRun) {
		mpBkptManager->Clear(mSysBPEEXRun);
		mSysBPEEXRun = 0;
		ATConsoleWrite("Breakpoint at EXE run address hit\n");
		event->mbBreak = true;
		return;
	}

	SysBPToUserBPMap::const_iterator it(mSysBPToUserBPMap.find(event->mIndex));

	if (it == mSysBPToUserBPMap.end())
		return;

	const uint32 useridx = it->second;

	UserBP& bp = mUserBPs[useridx];

	if (bp.mpCondition) {
		ATDebugExpEvalContext context(GetEvalContext());
		context.mpTarget = mDebugTargets[event->mTargetIndex];
		context.mbAccessValid = true;
		context.mbAccessReadValid = true;
		context.mbAccessWriteValid = true;
		context.mAccessAddress = event->mAddress;
		context.mAccessValue = event->mValue;

		sint32 result;
		if (!bp.mpCondition->Evaluate(result, context) || !result)
			return;
	}

	mExprAddress = event->mAddress;
	mExprValue = event->mValue;

	if (bp.mCommand.empty()) {
		VDStringA message;
		message.sprintf("Breakpoint %s hit", GetBreakpointName(useridx).c_str());

		if (bp.mTargetIndex != mCurrentTargetIndex)
			message.append_sprintf(" on target %u", bp.mTargetIndex);

		if (bp.mbOneShot)
			message += " (one time only)";

		message += '\n';
		ATConsoleWrite(message.c_str());
	} else {
		const char *s = bp.mCommand.c_str();

		for(;;) {
			while(*s == ' ')
				++s;

			const char *start = s;

			for(;;) {
				char c = *s;

				if (!c || c == ';')
					break;

				++s;

				bool allowEscapes = false;
				if (c == '\\' && *s == '"') {
					allowEscapes = true;
					c = '"';
					++s;
				}

				if (c == '"') {
					for(;;) {
						c = *s;
						if (!c)
							break;
						++s;

						if (c == '"')
							break;

						if (c == '\\' && allowEscapes) {
							c = *s;
							if (!c)
								break;

							++s;
						}
					}
				}
			}

			if (start != s) {
				QueueCommand(VDStringA(start, s).c_str(), false);
				event->mbBreak = true;
			}

			if (!*s)
				break;

			++s;
		}

		event->mbSilentBreak = true;

		// Because responding to the breakpoint causes a stop, we need to manually
		// handle a step event.
		ATCPUEmulator& cpu = g_sim.GetCPU();
		if (cpu.GetStep()) {
			cpu.SetStep(false);

			mbClientUpdatePending = true;
			mRunState = kRunState_Stopped;
			cpu.SetRTSBreak();
			mFramePC = cpu.GetInsnPC();
		}
	}

	if (!bp.mbContinueExecution) {
		event->mbBreak = true;
		mbClientUpdatePending = true;
		SetTarget(bp.mTargetIndex);
		InterruptRun();

		mFramePC = GetPC();
	}

	if (bp.mbOneShot)
		ClearUserBreakpoint(useridx);
}

namespace {
	struct ATDebuggerStepRangeSort {
		bool operator()(const ATDebuggerStepRange& x, const ATDebuggerStepRange& y) const {
			return x.mAddr < y.mAddr;
		}
	};

	struct ATDebuggerStepRangeEndPred {
		bool operator()(const ATDebuggerStepRange& x, const ATDebuggerStepRange& y) const {
			return x.mAddr + x.mSize < y.mAddr + y.mSize;
		}
	};
}

void ATDebugger::OnTargetStepComplete(uint32 targetIndex, bool successful) {
	if (mRunState != kRunState_TargetStepInto || !successful)
		return;

	mRunState = kRunState_TargetStepComplete;
	mFramePC = GetPC();
	g_sim.PostInterruptingEvent(kATSimEvent_AnonymousInterrupt);
	g_sim.Suspend();
}

void ATDebugger::SetupRangeStep(bool stepInto, const ATDebuggerStepRange *stepRanges, uint32 stepRangeCount) {
	ATCPUEmulator& cpu = g_sim.GetCPU();
	uint32 pc = cpu.GetInsnPC();

	// copy ranges into mStepRanges
	mStepRanges.assign(stepRanges, stepRanges + stepRangeCount);

	// remove the first byte of the first range -- we assume this is the entry point
	if (!mStepRanges.empty() && mStepRanges.front().mSize) {
		--mStepRanges.front().mSize;
		++mStepRanges.front().mAddr;
	}

	// sort the ranges
	std::sort(mStepRanges.begin(), mStepRanges.end(), ATDebuggerStepRangeSort());

	// coalesce ranges
	StepRanges::iterator itDst(mStepRanges.begin()), itSrc(mStepRanges.begin()), itEnd(mStepRanges.end());
	uint32 lastEnd = 0;

	for(; itSrc != itEnd; ++itSrc) {
		// discard zero-byte ranges
		if (!itSrc->mSize)
			continue;

		// check if we can coalesce
		if (lastEnd && lastEnd >= itSrc->mSize)
			itDst[-1].mSize = itSrc->mAddr + itSrc->mSize - itDst[-1].mAddr;
		else {
			*itDst = *itSrc;
			++itDst;
		}
	}

	mStepRanges.erase(itDst, itEnd);

	// find the first range whose end is after the PC and set that as the step range
	ATDebuggerStepRange pcRange = { pc, 0 };
	StepRanges::iterator itNext = std::upper_bound(mStepRanges.begin(), mStepRanges.end(), pcRange, ATDebuggerStepRangeEndPred());

	// set up stepping
	if (itNext == mStepRanges.begin()) {
		cpu.SetStepRange(0, 0, CPUSourceStepCallback, this, true);
	} else {
		cpu.SetStepRange(itNext[-1].mAddr, itNext[-1].mSize, CPUSourceStepCallback, this, true);
	}

	mStepS = cpu.GetS();
	mbStepInto = stepInto;
}

ATCPUStepResult ATDebugger::CPUSourceStepCallback(ATCPUEmulator *cpu, uint32 pc, bool call, void *data) {
	ATDebugger *thisptr = (ATDebugger *)data;

	if (call) {
		if (!thisptr->mbStepInto)
			return kATCPUStepResult_SkipCall;

		// check if this call is one we care about
		uint32 moduleId;
		ATSourceLineInfo lineInfo;

		if (!thisptr->LookupLine(pc, false, moduleId, lineInfo))
			return kATCPUStepResult_SkipCall;

		Module *mod = thisptr->GetModuleById(moduleId);
		if (mod) {
			if (std::binary_search(mod->mSilentlyIgnoredFiles.begin(), mod->mSilentlyIgnoredFiles.end(), lineInfo.mFileId))
				return kATCPUStepResult_SkipCall;
		}

		if (pc - lineInfo.mOffset > 100)
			return kATCPUStepResult_SkipCall;

		return kATCPUStepResult_Stop;
	} else {
		// check if PC is in the current step range set
		ATDebuggerStepRange range = { pc, 1 };
		StepRanges::const_iterator it = std::upper_bound(thisptr->mStepRanges.begin(), thisptr->mStepRanges.end(), range, ATDebuggerStepRangeSort());

		if (it != thisptr->mStepRanges.begin() && (uint32)(pc - it[-1].mAddr) < it[-1].mSize) {
			// it's within range -- adjust the fast step range and continue
			cpu->SetStepRange(it[-1].mAddr, it[-1].mSize, CPUSourceStepCallback, thisptr, true);
			return kATCPUStepResult_Continue;
		}

		return kATCPUStepResult_Stop;
	}
}

void ATDebugger::InterruptRun(bool leaveRunning) {
	ATCPUEmulator& cpu = g_sim.GetCPU();

	cpu.SetStep(false);
	cpu.SetRTSBreak();
	g_sim.SetBreakOnScanline(-1);
	g_sim.SetBreakOnFrameEnd(false);

	if (leaveRunning)
		g_sim.Resume();
	else
		g_sim.Suspend();

	if (mRunState == kRunState_TargetStepInto) {
		auto *ec = vdpoly_cast<IATDebugTargetExecutionControl *>(mpCurrentTarget);

		if (ec) {
			ec->Break();
		}
	}

	if (leaveRunning)
		mRunState = kRunState_Run;
	else
		mRunState = kRunState_Stopped;
}

///////////////////////////////////////////////////////////////////////////////

ATDebuggerCmdParser::ATDebuggerCmdParser(int argc, const char *const *argv)
	: mArgs(argv, argv + argc)
{
}

const char *ATDebuggerCmdParser::GetNextArgument() {
	if (mArgs.empty())
		return nullptr;

	const char *s = mArgs.front();
	mArgs.erase(mArgs.begin());

	return s;
}

ATDebuggerCmdParser& ATDebuggerCmdParser::operator>>(ATDebuggerCmdSwitch& sw) {
	for(Args::iterator it(mArgs.begin()), itEnd(mArgs.end()); it != itEnd; ++it) {
		const char *s = *it;

		if (s[0] == '-' && !strcmp(s + 1, sw.mpName)) {
			sw.mbState = true;
			mArgs.erase(it);
			break;
		}
	}

	return *this;
}

ATDebuggerCmdParser& ATDebuggerCmdParser::operator>>(ATDebuggerCmdSwitchStrArg& sw) {
	size_t nameLen = strlen(sw.mpName);

	for(Args::iterator it(mArgs.begin()), itEnd(mArgs.end()); it != itEnd; ++it) {
		const char *s = *it;

		if (s[0] != '-')
			continue;

		if (strncmp(s + 1, sw.mpName, nameLen))
			continue;

		s += nameLen + 1;
		if (*s == ':')
			++s;
		else if (*s)
			continue;
		else {
			it = mArgs.erase(it);

			if (it == mArgs.end())
				throw MyError("Switch -%s requires an argument.", sw.mpName);

			s = *it;
		}

		mArgs.erase(it);

		const char *t = s + strlen(s);

		// strip quotes
		if (*s == '"')
			++s;

		if (t != s && t[-1] == '"')
			--t;

		sw.mbValid = true;
		sw.mValue.assign(s, t);
		break;
	}

	return *this;
}
ATDebuggerCmdParser& ATDebuggerCmdParser::operator>>(ATDebuggerCmdSwitchNumArg& sw) {
	size_t nameLen = strlen(sw.mpName);

	for(Args::iterator it(mArgs.begin()), itEnd(mArgs.end()); it != itEnd; ++it) {
		const char *s = *it;

		if (s[0] != '-')
			continue;

		if (strncmp(s + 1, sw.mpName, nameLen))
			continue;

		s += nameLen + 1;
		if (*s == ':')
			++s;
		else if (*s)
			continue;
		else {
			it = mArgs.erase(it);

			if (it == mArgs.end())
				throw MyError("Switch -%s requires a numeric argument.", sw.mpName);

			s = *it;
		}

		mArgs.erase(it);

		char *end = (char *)s;
		long v = strtol(s, &end, 10);

		if (*end)
			throw MyError("Invalid numeric switch argument: -%s:%s", sw.mpName, s);

		if (v < sw.mMinVal || v > sw.mMaxVal)
			throw MyError("Numeric switch argument out of range: -%s:%d", sw.mpName, v);

		sw.mbValid = true;
		sw.mValue = v;
		break;
	}

	return *this;
}

ATDebuggerCmdParser& ATDebuggerCmdParser::operator>>(ATDebuggerCmdSwitchExprNumArg& sw) {
	size_t nameLen = strlen(sw.mpName);

	for(Args::iterator it(mArgs.begin()), itEnd(mArgs.end()); it != itEnd; ++it) {
		const char *s = *it;

		if (s[0] != '-')
			continue;

		if (strncmp(s + 1, sw.mpName, nameLen))
			continue;

		s += nameLen + 1;
		if (*s == ':')
			++s;
		else if (*s)
			continue;
		else {
			it = mArgs.erase(it);

			if (it == mArgs.end())
				throw MyError("Switch -%s requires a numeric argument.", sw.mpName);

			s = *it;
		}

		mArgs.erase(it);

		vdautoptr<ATDebugExpNode> node;
		try {
			ATDebugExpEvalContext immContext = ATGetDebugger()->GetEvalContext();
			node = ATDebuggerParseExpression(s, ATGetDebuggerSymbolLookup(), ATGetDebugger()->GetExprOpts(), &immContext);
		} catch(const ATDebuggerExprParseException& ex) {
			throw MyError("Unable to parse expression '%s': %s\n", s, ex.c_str());
		}

		sint32 v;
		if (!node->Evaluate(v, g_debugger.GetEvalContext()))
			throw MyError("Cannot evaluate '%s' in this context.", s);

		if (v < sw.mMinVal || v > sw.mMaxVal)
			throw MyError("Numeric switch argument out of range: -%s:%d", sw.mpName, v);

		sw.mbValid = true;
		sw.mValue = v;
		break;
	}

	return *this;
}

ATDebuggerCmdParser& ATDebuggerCmdParser::operator>>(ATDebuggerCmdBool& bo) {
	if (mArgs.empty()) {
		if (bo.mbRequired)
			throw MyError("Missing boolean argument.");

		return *this;
	}

	const VDStringSpanA s(mArgs.front());
	mArgs.erase(mArgs.begin());

	bo.mbValid = true;

	if (s == "on" || s == "true")
		bo.mbValue = true;
	else if (s == "off" || s == "false")
		bo.mbValue = false;

	return *this;
}

ATDebuggerCmdParser& ATDebuggerCmdParser::operator>>(ATDebuggerCmdNumber& nu) {
	if (mArgs.empty()) {
		if (nu.mbRequired)
			throw MyError("Missing numeric argument.");

		return *this;
	}

	const char *s = mArgs.front();
	mArgs.erase(mArgs.begin());

	const char *t = s;
	char *end = (char *)s;

	int base = 10;
	if (*t == '$') {
		++t;
		base = 16;
	}

	long v = strtol(t, &end, base);

	if (*end)
		throw MyError("Invalid numeric argument: %s", s);

	if (v < nu.mMinVal || v > nu.mMaxVal)
		throw MyError("Numeric argument out of range: %d", v);

	nu.mbValid = true;
	nu.mValue = v;

	return *this;
}

ATDebuggerCmdParser& ATDebuggerCmdParser::operator>>(ATDebuggerCmdAddress& ad) {
	for(Args::iterator it(mArgs.begin()), itEnd(mArgs.end()); it != itEnd; ++it) {
		const char *s = *it;

		if (s[0] != '-') {
			if (s[0] == '*' && !s[1] && ad.mbAllowStar) {
				ad.mbStar = true;
				ad.mbValid = true;
			} else {
				ad.mAddress = g_debugger.ResolveSymbolThrow(s, ad.mbGeneral);
				ad.mbValid = true;
			}

			mArgs.erase(it);
			return *this;
		}
	}

	if (ad.mbRequired)
		throw MyError("Address parameter required.");

	return *this;
}

ATDebuggerCmdParser& ATDebuggerCmdParser::operator>>(ATDebuggerCmdLength& ln) {
	VDStringA quoteStripBuf;

	for(Args::iterator it(mArgs.begin()), itEnd(mArgs.end()); it != itEnd; ++it) {
		const char *s = *it;
		bool quoted = false;

		if (s[0] == '"') {
			quoted = true;
			++s;
		}

		if (s[0] == 'L' || s[0] == 'l') {
			++s;

			if (quoted) {
				size_t len = strlen(s);

				if (len && s[len - 1] == '"') {
					quoteStripBuf.assign(s, s + len - 1);
					s = quoteStripBuf.c_str();
				}
			}

			bool end_addr = false;

			if (*s == '>') {
				++s;

				if (!ln.mpAnchor || !ln.mpAnchor->mbValid || ln.mpAnchor->mbStar)
					throw MyError("Address end syntax cannot be used in this context.");

				end_addr = true;
			}

			sint32 v;
			vdautoptr<ATDebugExpNode> node;
			try {
				node = ATDebuggerParseExpression(s, ATGetDebuggerSymbolLookup(), ATGetDebugger()->GetExprOpts());
			} catch(const ATDebuggerExprParseException& ex) {
				throw MyError("Unable to parse expression '%s': %s\n", s, ex.c_str());
			}

			if (!node->Evaluate(v, g_debugger.GetEvalContext()))
				throw MyError("Cannot evaluate '%s' in this context.", s);

			if (end_addr) {
				if (v < 0 || (uint32)v < ln.mpAnchor->mValue)
					throw MyError("End address is prior to start address.");

				ln.mLength = v - ln.mpAnchor->mValue + 1;
			} else {
				if (v < 0)
					throw MyError("Invalid length: %s", s);

				ln.mLength = (uint32)v;
			}

			ln.mbValid = true;

			mArgs.erase(it);
			return *this;
		}
	}

	if (ln.mbRequired)
		throw MyError("Length parameter required.");

	return *this;
}

ATDebuggerCmdParser& ATDebuggerCmdParser::operator>>(ATDebuggerCmdName& nm) {
	for(Args::iterator it(mArgs.begin()), itEnd(mArgs.end()); it != itEnd; ++it) {
		const char *s = *it;

		if (s[0] != '-') {
			nm.mName = s;
			nm.mbValid = true;

			mArgs.erase(it);
			return *this;
		}
	}

	if (nm.mbRequired)
		throw MyError("Name parameter required.");

	return *this;
}

ATDebuggerCmdParser& ATDebuggerCmdParser::operator>>(ATDebuggerCmdPath& nm) {
	for(Args::iterator it(mArgs.begin()), itEnd(mArgs.end()); it != itEnd; ++it) {
		const char *s = *it;

		bool quoted = false;
		if (s[0] == '"') {
			quoted = true;
			++s;
		}

		const char *t = s + strlen(s);

		if (quoted && t != s && t[-1] == '"')
			--t;

		nm.mPath.assign(s, t);
		nm.mbValid = true;

		mArgs.erase(it);
		return *this;
	}

	if (nm.mbRequired)
		throw MyError("Path parameter required.");

	return *this;
}

ATDebuggerCmdParser& ATDebuggerCmdParser::operator>>(ATDebuggerCmdString& nm) {
	for(Args::iterator it(mArgs.begin()), itEnd(mArgs.end()); it != itEnd; ++it) {
		const char *s = *it;

		if (s[0] == '-')
			continue;

		if (!strcmp(s, "@ts")) {
			nm.mName = g_debugger.GetTempString();
		} else if (s[0] == '"') {
			++s;
			const char *t = s + strlen(s);

			if (t != s && t[-1] == '"')
				--t;

			nm.mName.assign(s, t);
		} else {
			nm.mName = s;
		}

		nm.mbValid = true;
		mArgs.erase(it);
		return *this;
	}

	if (nm.mbRequired)
		throw MyError("String parameter required.");

	return *this;
}

ATDebuggerCmdParser& ATDebuggerCmdParser::operator>>(ATDebuggerCmdQuotedString& nm) {
	for(Args::iterator it(mArgs.begin()), itEnd(mArgs.end()); it != itEnd; ++it) {
		const char *s = *it;

		if (!strcmp(s, "@ts")) {
			nm.mName = g_debugger.GetTempString();
			nm.mbValid = true;

			mArgs.erase(it);
			return *this;
		} else if (s[0] == '"') {
			++s;
			const char *t = s + strlen(s);

			if (t != s && t[-1] == '"')
				--t;

			nm.mName.assign(s, t);
			nm.mbValid = true;

			mArgs.erase(it);
			return *this;
		}
	}

	if (nm.mbRequired)
		throw MyError("Quoted string parameter required.");

	return *this;
}

ATDebuggerCmdParser& ATDebuggerCmdParser::operator>>(ATDebuggerCmdExpr& xp) {
	if (mArgs.empty()) {
		if (xp.mbRequired)
			throw MyError("Missing expression argument.");

		return *this;
	}

	const char *s = mArgs.front();
	mArgs.erase(mArgs.begin());

	VDStringA quoteStripBuf;

	if (*s == '"') {
		++s;

		size_t len = strlen(s);

		if (len && s[len - 1] == '"') {
			quoteStripBuf.assign(s, s + len - 1);
			s = quoteStripBuf.c_str();
		}
	}

	try {
		ATDebugExpEvalContext immContext = ATGetDebugger()->GetEvalContext();
		xp.mpExpr = ATDebuggerParseExpression(s, ATGetDebuggerSymbolLookup(), ATGetDebugger()->GetExprOpts(), &immContext);
	} catch(const ATDebuggerExprParseException& ex) {
		throw MyError("Unable to parse expression '%s': %s\n", s, ex.c_str());
	}

	return *this;
}

ATDebuggerCmdParser& ATDebuggerCmdParser::operator>>(ATDebuggerCmdExprNum& nu) {
	if (mArgs.empty()) {
		if (nu.mbRequired)
			throw MyError("Missing numeric argument.");

		return *this;
	}

	const char *s = mArgs.front();
	mArgs.erase(mArgs.begin());

	nu.mOriginalText = s;

	if (nu.mbAllowStar && s[0] == '*' && !s[1]) {
		nu.mbValid = true;
		nu.mbStar = true;
		nu.mValue = 0;
		return *this;
	}

	VDStringA quoteStripBuf;
	sint32 v;

	char dummy;
	long lval;
	if (1 == sscanf(s, nu.mbHexDefault ? "%lx%c" : "%ld%c", &lval, &dummy)) {
		v = lval;
	} else {
		if (*s == '"') {
			++s;

			size_t len = strlen(s);

			if (len && s[len - 1] == '"') {
				quoteStripBuf.assign(s, s + len - 1);
				s = quoteStripBuf.c_str();
			}
		}

		vdautoptr<ATDebugExpNode> node;
		try {
			// The decimal/hex option here needs to override the expression default.
			ATDebuggerExprParseOpts opts(ATGetDebugger()->GetExprOpts());

			opts.mbAllowUntaggedHex &= nu.mbHexDefault;

			node = ATDebuggerParseExpression(s, ATGetDebuggerSymbolLookup(), opts);
		} catch(const ATDebuggerExprParseException& ex) {
			throw MyError("Unable to parse expression '%s': %s\n", s, ex.c_str());
		}

		if (!node->Evaluate(v, g_debugger.GetEvalContext()))
			throw MyError("Cannot evaluate '%s' in this context.", s);
	}

	if (v < nu.mMinVal || v > nu.mMaxVal)
		throw MyError("Numeric argument out of range: %d", v);

	nu.mbValid = true;
	nu.mValue = v;

	return *this;
}

ATDebuggerCmdParser& ATDebuggerCmdParser::operator>>(ATDebuggerCmdExprAddr& nu) {
	if (mArgs.empty()) {
		if (nu.mbRequired)
			throw MyError("Missing numeric argument.");

		return *this;
	}

	const char *s = mArgs.front();
	mArgs.erase(mArgs.begin());

	if (s[0] == '*' && !s[1] && nu.mbAllowStar) {
		nu.mbStar = true;
		nu.mbValid = true;
		return *this;
	}

	VDStringA quoteStripBuf;
	sint32 v;

	if (*s == '"') {
		++s;

		size_t len = strlen(s);

		if (len && s[len - 1] == '"') {
			quoteStripBuf.assign(s, s + len - 1);
			s = quoteStripBuf.c_str();
		}
	}

	vdautoptr<ATDebugExpNode> node;
	try {
		node = ATDebuggerParseExpression(s, ATGetDebuggerSymbolLookup(), ATGetDebugger()->GetExprOpts());
	} catch(const ATDebuggerExprParseException& ex) {
		throw MyError("Unable to parse expression '%s': %s\n", s, ex.c_str());
	}

	if (!node->Evaluate(v, g_debugger.GetEvalContext()))
		throw MyError("Cannot evaluate '%s' in this context.", s);

	nu.mbValid = true;
	nu.mValue = v;

	return *this;
}

ATDebuggerCmdParser& ATDebuggerCmdParser::operator>>(int) {
	if (!mArgs.empty())
		throw MyError("Extraneous argument: %s", mArgs.front());

	return *this;
}

void ATDebuggerSerializeArgv(VDStringA& dst, const ATDebuggerCmdParser& parser) {
	ATDebuggerSerializeArgv(dst, (int)parser.GetArgumentCount(), parser.GetArguments());

}

///////////////////////////////////////////////////////////////////////////////

void ATConsoleCmdAssemble(ATDebuggerCmdParser& parser) {
	ATDebuggerCmdExprAddr addr(false, true);

	parser >> addr >> 0;

	vdrefptr<IATDebuggerActiveCommand> cmd;

	ATCreateDebuggerCmdAssemble(addr.GetValue(), ~cmd);
	g_debugger.StartActiveCommand(cmd);
}

void ATConsoleCmdTrace(ATDebuggerCmdParser& parser) {
	parser >> 0;

	ATGetDebugger()->StepInto(kATDebugSrcMode_Disasm);
}

void ATConsoleCmdGo(ATDebuggerCmdParser& parser) {
	ATDebuggerCmdSwitch swSource("s", false);
	ATDebuggerCmdSwitch swNoChange("n", false);
	parser >> swSource >> swNoChange >> 0;

	ATGetDebugger()->Run(swNoChange ? kATDebugSrcMode_Same : swSource ? kATDebugSrcMode_Source : kATDebugSrcMode_Disasm);
}

void ATConsoleCmdGoTraced(ATDebuggerCmdParser& parser) {
	ATGetDebugger()->RunTraced();
}

void ATConsoleCmdGoFrameEnd(ATDebuggerCmdParser& parser) {
	g_debugger.RunToEndOfFrame();
}

void ATConsoleCmdGoReturn(ATDebuggerCmdParser& parser) {
	ATGetDebugger()->StepOut(kATDebugSrcMode_Disasm);
}

void ATConsoleCmdGoScanline(ATDebuggerCmdParser& parser) {
	ATDebuggerCmdExprNum scanline(false, false);
	parser >> scanline >> 0;

	int scan = (int)scanline.GetValue();
	if (!scanline.IsValid())
		scan = g_debugger.GetContinuationAddress();

	if (scan < 0 || scan >= (g_sim.GetVideoStandard() == kATVideoStandard_NTSC || g_sim.GetVideoStandard() == kATVideoStandard_PAL60 ? 262 : 312)) {
		ATConsoleWrite("Invalid scanline.\n");
		return;
	}

	if (scanline.IsValid())
		g_debugger.SetContinuationAddress(scan);

	g_debugger.RunToScanline(scan);
}

void ATConsoleCmdGoVBI(ATDebuggerCmdParser& parser) {
	g_debugger.RunToVBI();
}

void ATConsoleCmdCallStack(ATDebuggerCmdParser& parser) {
	parser >> 0;

	ATGetDebugger()->DumpCallStack();
}

void ATConsoleCmdStepOver(ATDebuggerCmdParser& parser) {
	parser >> 0;

	ATGetDebugger()->StepOver(kATDebugSrcMode_Disasm);
}

////////////////////////////////////////////////////////////////////////////

namespace {
	bool ParseBreakpointSourceLocation(const char *s0, VDStringA& filename, uint32& line) {
		const char *s = s0;

		if (s[0] != '`')
			return false;

		++s;

		bool valid = false;

		const char *fnstart = s;
		const char *split = NULL;

		while(*s && *s != '`') {
			if (*s == ':')
				split = s;
			++s;
		}

		const char *fnend = split;

		line = 0;
		if (split) {
			s = split + 1;

			for(;;) {
				uint8 c = *s - '0';

				if (c >= 10)
					break;

				line = line * 10 + c;
				++s;
			}

			if (line)
				valid = true;
		}

		if (!valid)
			throw MyError("Invalid source location: %s", s0);

		filename.assign(fnstart, fnend);
		return true;
	}

	void MakeTracepointCommand(VDStringA& cmd, ATDebuggerCmdParser& parser, bool allowDeferred = true) {
		vdfastvector<const char *> args;
		int deferredCount = -1;

		while(const char *s = parser.GetNextArgument()) {
			if (!strcmp(s, "--")) {
				if (!allowDeferred)
					throw MyError("Deferred arguments cannot be used in this context.");

				if (deferredCount >= 0)
					throw MyError("'--' can only be used once in the argument list.");

				deferredCount = (int)args.size();
			}

			args.push_back(s);
		}

		if (args.empty())
			throw MyError("Trace format argument required.");

		int n = (int)args.size();
		VDStringA formatStr;
		if (deferredCount < 0) {
			cmd = "`.printf";

			for(int i=0; i<n; ++i) {
				const char *s = args[i];
				cmd += ' ';

				if (!i && !strcmp(s, "@ts")) {
					formatStr = "\"";
					formatStr += g_debugger.GetTempString();
					formatStr += '"';
					s = formatStr.c_str();
				}

				ATDebuggerSerializeArgv(cmd, 1, &s);
			}
		} else {
			cmd = "`.sprintf";

			for(int i=0; i<deferredCount; ++i) {
				const char *s = args[i];
				cmd += ' ';

				if (!i && !strcmp(s, "@ts")) {
					formatStr = "\"";
					formatStr += g_debugger.GetTempString();
					formatStr += '"';
					s = formatStr.c_str();
				}

				ATDebuggerSerializeArgv(cmd, 1, &s);
			}

			cmd += " ; `bt -o -k -q -g deferred @ra @ts";

			for(int i=deferredCount + 1; i<n; ++i) {
				const char *s = args[i];
				cmd += ' ';

				ATDebuggerSerializeArgv(cmd, 1, &s);
			}
		}
	}
}

void ATConsoleCmdBreakpt(ATDebuggerCmdParser& parser) {
	ATDebuggerCmdSwitchStrArg groupNameArg("g");
	ATDebuggerCmdSwitch clearOnResetArg("k", false);
	ATDebuggerCmdSwitch nonStopArg("n", false);
	ATDebuggerCmdSwitch oneShotArg("o", false);
	ATDebuggerCmdSwitch quietArg("q", false);
	ATDebuggerCmdString addrStr(true);
	ATDebuggerCmdQuotedString command(false);
	parser >> groupNameArg >> clearOnResetArg >> nonStopArg >> oneShotArg >> quietArg >> addrStr >> command >> 0;

	if (!g_debugger.ArePCBreakpointsSupported())
		throw MyError("PC breakpoints are not supported on the current target.");

	ATBreakpointManager *bpm = g_debugger.GetBreakpointManager();

	const char *s = addrStr->c_str();
	uint32 useridx;
	VDStringA filename;
	uint32 line;

	if (ParseBreakpointSourceLocation(s, filename, line)) {
		useridx = g_debugger.SetSourceBreakpoint(filename.c_str(), line, NULL, command.IsValid() ? command->c_str() : NULL, nonStopArg);
		g_debugger.RegisterUserBreakpoint(useridx, nullptr);

		sint32 sysidx = g_debugger.LookupUserBreakpoint(useridx);

		if (!sysidx) {
			if (!quietArg)
				ATConsolePrintf("Deferred breakpoint %s set at %s:%u.\n", g_debugger.GetBreakpointName(useridx).c_str(), filename.c_str(), line);
		} else {
			ATBreakpointInfo bpinfo;
			g_debugger.GetBreakpointManager()->GetInfo(sysidx, bpinfo);

			if (!quietArg)
				ATConsolePrintf("Breakpoint %s set at `%s:%u` ($%04X)\n", g_debugger.GetBreakpointName(useridx).c_str(), filename.c_str(), line, bpinfo.mAddress);
		}
	} else {
		const uint32 addr = (uint32)g_debugger.EvaluateThrow(s);
		if (addr >= 0x1000000 && g_debugger.GetTargetIndex()) {
			throw MyError("Global PC breakpoints are only supported on target 0.");
		}

		const uint32 sysidx = bpm->SetAtPC(g_debugger.GetTargetIndex(), addr);
		useridx = g_debugger.RegisterSystemBreakpoint(sysidx, NULL, command.IsValid() ? command->c_str() : NULL, nonStopArg);

		g_debugger.RegisterUserBreakpoint(useridx, groupNameArg.GetValue());
		if (!quietArg)
			ATConsolePrintf("Breakpoint %s set at %s.\n", g_debugger.GetBreakpointName(useridx).c_str(), g_debugger.GetAddressText(addr, true, true).c_str());
	}

	if (clearOnResetArg)
		g_debugger.SetBreakpointClearOnReset(useridx, true);

	if (oneShotArg)
		g_debugger.SetBreakpointOneShot(useridx, true);

	g_sim.NotifyEvent(kATSimEvent_CPUPCBreakpointsUpdated);
}

void ATConsoleCmdBreakptTrace(ATDebuggerCmdParser& parser) {
	ATDebuggerCmdSwitchStrArg groupNameArg("g");
	ATDebuggerCmdSwitch clearOnResetArg("k", false);
	ATDebuggerCmdSwitch oneShotArg("o", false);
	ATDebuggerCmdSwitch quietArg("q", false);
	ATDebuggerCmdString addrStr(true);
	parser >> groupNameArg >> clearOnResetArg >> oneShotArg >> quietArg >> addrStr;

	if (!g_debugger.ArePCBreakpointsSupported())
		throw MyError("PC breakpoints are not supported on the current target.");

	VDStringA tracecmd;
	MakeTracepointCommand(tracecmd, parser);

	ATBreakpointManager *bpm = g_debugger.GetBreakpointManager();

	const char *s = addrStr->c_str();
	uint32 useridx;
	VDStringA filename;
	uint32 line;

	if (ParseBreakpointSourceLocation(s, filename, line)) {
		useridx = g_debugger.SetSourceBreakpoint(filename.c_str(), line, NULL, tracecmd.c_str(), true);
		g_debugger.RegisterUserBreakpoint(useridx, groupNameArg.GetValue());

		sint32 sysidx = g_debugger.LookupUserBreakpoint(useridx);

		if (!sysidx) {
			if (!quietArg)
				ATConsolePrintf("Deferred tracepoint %s set at %s:%u.\n", g_debugger.GetBreakpointName(useridx).c_str(), filename.c_str(), line);
		} else {
			ATBreakpointInfo bpinfo;
			g_debugger.GetBreakpointManager()->GetInfo(sysidx, bpinfo);

			if (!quietArg)
				ATConsolePrintf("Tracepoint %s set at `%s:%u` ($%04X)\n", g_debugger.GetBreakpointName(useridx).c_str(), filename.c_str(), line, bpinfo.mAddress);
		}
	} else {
		const uint32 addr = (uint32)g_debugger.EvaluateThrow(s);
		const uint32 sysidx = bpm->SetAtPC(g_debugger.GetTargetIndex(), addr);
		useridx = g_debugger.RegisterSystemBreakpoint(sysidx, NULL, tracecmd.c_str(), true);
		g_debugger.RegisterUserBreakpoint(useridx, groupNameArg.GetValue());

		if (!quietArg)
			ATConsolePrintf("Tracepoint %s set at %s.\n", g_debugger.GetBreakpointName(useridx).c_str(), g_debugger.GetAddressText(addr, true, true).c_str());
	}

	if (clearOnResetArg)
		g_debugger.SetBreakpointClearOnReset(useridx, true);

	if (oneShotArg)
		g_debugger.SetBreakpointOneShot(useridx, true);

	g_sim.NotifyEvent(kATSimEvent_CPUPCBreakpointsUpdated);
}

void ATConsoleCmdBreakptTraceAccess(ATDebuggerCmdParser& parser) {
	ATDebuggerCmdSwitchStrArg groupNameArg("g");
	ATDebuggerCmdSwitch clearOnResetArg("k", false);
	ATDebuggerCmdSwitch oneShotArg("o", false);
	ATDebuggerCmdSwitch quietArg("q", false);
	ATDebuggerCmdName cmdAccessMode(true);
	ATDebuggerCmdExprAddr cmdAddress(false, true, false);
	ATDebuggerCmdLength cmdLength(1, false, &cmdAddress);

	parser >> groupNameArg >> clearOnResetArg >> oneShotArg >> quietArg >> cmdAccessMode >> cmdAddress >> cmdLength;

	if (!g_debugger.AreAccessBreakpointsSupported())
		throw MyError("Memory access breakpoints are not supported on the current target.");

	VDStringA tracecmd;
	MakeTracepointCommand(tracecmd, parser);

	bool readMode = true;
	if (*cmdAccessMode == "w")
		readMode = false;
	else if (*cmdAccessMode != "r") {
		ATConsoleWrite("Access mode must be 'r' or 'w'.\n");
		return;
	}

	ATBreakpointManager *bpm = g_debugger.GetBreakpointManager();
	const uint32 address = cmdAddress.GetValue();
	const uint32 length = cmdLength;

	if (length == 0) {
		ATConsoleWrite("Invalid breakpoint range length.\n");
		return;
	}

	uint32 sysidx;
	uint32 useridx;

	const char *modestr = readMode ? "read" : "write";

	if (length > 1) {
		sysidx = bpm->SetAccessRangeBP(address, length, readMode, !readMode);

		useridx = g_debugger.RegisterSystemBreakpoint(sysidx, NULL, tracecmd.c_str(), true);
		g_debugger.RegisterUserBreakpoint(useridx, groupNameArg.GetValue());

		if (!quietArg)
			ATConsolePrintf("Tracepoint %s set on %s at %04X-%04X.\n", g_debugger.GetBreakpointName(useridx).c_str(), modestr, address, address + length - 1);
	} else {
		sysidx = bpm->SetAccessBP(address, readMode, !readMode);

		useridx = g_debugger.RegisterSystemBreakpoint(sysidx, NULL, tracecmd.c_str(), true);
		g_debugger.RegisterUserBreakpoint(useridx, groupNameArg.GetValue());

		if (!quietArg)
			ATConsolePrintf("Tracepoint %s set on %s at %04X.\n", g_debugger.GetBreakpointName(useridx).c_str(), modestr, address);
	}

	if (clearOnResetArg)
		g_debugger.SetBreakpointClearOnReset(useridx, true);

	if (oneShotArg)
		g_debugger.SetBreakpointOneShot(useridx, true);
}

void ATConsoleCmdBreakptClear(ATDebuggerCmdParser& parser) {
	ATDebuggerCmdName nameArg(true);

	parser >> nameArg >> 0;

	auto dotPos = nameArg->find_last_of('.');

	const auto *arg = nameArg->c_str();
	VDStringA groupName;
	if (dotPos != nameArg->npos) {
		groupName.assign(*nameArg, 0, dotPos);
		arg += dotPos + 1;
	}

	if (!strcmp(arg, "*")) {
		vdfastvector<uint32> bps;
		g_debugger.GetBreakpointList(bps, groupName.c_str());

		if (!bps.empty()) {
			for(uint32 useridx : bps)
				g_debugger.ClearUserBreakpoint(useridx);
		}

		g_sim.NotifyEvent(kATSimEvent_CPUPCBreakpointsUpdated);
		ATConsolePrintf("%u breakpoint(s) cleared.\n", bps.size());
	} else {
		char dummy;
		unsigned number;
		if (1 != sscanf(arg, "%u %c", &number, &dummy))
			throw MyError("Invalid breakpoint number: %s", arg);

		sint32 useridx = g_debugger.LookupUserBreakpointByNum(number, groupName.c_str());

		if (useridx < 0)
			throw MyError("Invalid breakpoint number: %s", arg);
		else {
			const VDStringA& name = g_debugger.GetBreakpointName(useridx);

			g_debugger.ClearUserBreakpoint(useridx);
			g_sim.NotifyEvent(kATSimEvent_CPUPCBreakpointsUpdated);
			ATConsolePrintf("Breakpoint %s cleared.\n", name.c_str());
		}
	}
}

void ATConsoleCmdBreakptAccess(ATDebuggerCmdParser& parser) {
	ATDebuggerCmdSwitchStrArg groupNameArg("g");
	ATDebuggerCmdSwitch clearOnResetArg("k", false);
	ATDebuggerCmdSwitch oneShotArg("o", false);
	ATDebuggerCmdSwitch quietArg("q", false);
	ATDebuggerCmdSwitch nonStopArg("n", false);
	ATDebuggerCmdName cmdAccessMode(true);
	ATDebuggerCmdExprAddr cmdAddress(false, true, true);
	ATDebuggerCmdLength cmdLength(1, false, &cmdAddress);
	ATDebuggerCmdQuotedString command(false);

	parser >> groupNameArg >> clearOnResetArg >> nonStopArg >> oneShotArg >> quietArg >> cmdAccessMode >> cmdAddress >> cmdLength >> command >> 0;

	if (!g_debugger.AreAccessBreakpointsSupported())
		throw MyError("Memory access breakpoints are not supported on the current target.");

	bool readMode = true;
	if (*cmdAccessMode == "w")
		readMode = false;
	else if (*cmdAccessMode != "r") {
		ATConsoleWrite("Access mode must be 'r' or 'w'.\n");
		return;
	}

	ATBreakpointManager *bpm = g_debugger.GetBreakpointManager();
	if (cmdAddress.IsStar()) {
		ATBreakpointIndices indices;
		g_debugger.GetBreakpointList(indices);

		uint32 cleared = 0;
		while(!indices.empty()) {
			const uint32 useridx = indices.back();
			indices.pop_back();

			ATDebuggerBreakpointInfo info;
			g_debugger.GetBreakpointInfo(useridx, info);

			if (readMode ? info.mbBreakOnRead : info.mbBreakOnWrite) {
				g_debugger.ClearUserBreakpoint(useridx);
				++cleared;
			}
		}

		if (!quietArg) {
			if (readMode) {
				ATConsolePrintf("%u read breakpoint(s) cleared.\n", cleared);
			} else {
				ATConsolePrintf("%u write breakpoint(s) cleared.\n", cleared);
			}
		}
	} else {
		const uint32 address = cmdAddress.GetValue();
		const uint32 length = cmdLength;

		if (length == 0) {
			ATConsoleWrite("Invalid breakpoint range length.\n");
			return;
		}

		uint32 sysidx;
		uint32 useridx;

		const char *modestr = readMode ? "read" : "write";

		if (length > 1) {
			sysidx = bpm->SetAccessRangeBP(address, length, readMode, !readMode);

			useridx = g_debugger.RegisterSystemBreakpoint(sysidx, NULL, command.IsValid() ? command->c_str() : NULL, nonStopArg);
			g_debugger.RegisterUserBreakpoint(useridx, groupNameArg.GetValue());

			if (!quietArg)
				ATConsolePrintf("Breakpoint %s set on %s at %04X-%04X.\n", g_debugger.GetBreakpointName(useridx).c_str(), modestr, address, address + length - 1);
		} else {
			sysidx = bpm->SetAccessBP(address, readMode, !readMode);

			useridx = g_debugger.RegisterSystemBreakpoint(sysidx, NULL, command.IsValid() ? command->c_str() : NULL, nonStopArg);
			g_debugger.RegisterUserBreakpoint(useridx, groupNameArg.GetValue());

			if (!quietArg)
				ATConsolePrintf("Breakpoint %s set on %s at %04X.\n", g_debugger.GetBreakpointName(useridx).c_str(), modestr, address);
		}

		if (clearOnResetArg)
			g_debugger.SetBreakpointClearOnReset(useridx, true);

		if (oneShotArg)
			g_debugger.SetBreakpointOneShot(useridx, true);
	}
}

void ATConsoleCmdBreakptList(ATDebuggerCmdParser& parser) {
	ATDebuggerCmdSwitch allArg("a", false);
	ATDebuggerCmdSwitch anyTargetArg("t", false);
	ATDebuggerCmdSwitch verboseArg("v", false);

	parser >> allArg >> anyTargetArg >> verboseArg >> 0;

	if (!anyTargetArg && !g_debugger.ArePCBreakpointsSupported())
		throw MyError("Breakpoints are not supported on the current target.");

	ATConsoleWrite(verboseArg ? "User breakpoints:\n" : "Breakpoints:\n");

	vdvector<VDStringA> groups;

	if (allArg)
		groups = g_debugger.GetBreakpointGroups();
	else
		groups.push_back();

	const uint32 targetIndex = g_debugger.GetTargetIndex();

	for(const VDStringA& groupName : groups) {
		vdfastvector<uint32> indices;
		g_debugger.GetBreakpointList(indices, groupName.c_str());

		VDStringA line;
		VDStringA file;
		uint32 lineno;
		for(vdfastvector<uint32>::const_iterator it(indices.begin()), itEnd(indices.end()); it != itEnd; ++it) {
			const uint32 useridx = *it;
			ATDebuggerBreakpointInfo info;

			g_debugger.GetBreakpointInfo(useridx, info);

			if (!anyTargetArg && info.mTargetIndex != targetIndex)
				continue;

			if (allArg)
				line.sprintf("%-10s  ", g_debugger.GetBreakpointName(useridx).c_str());
			else
				line.sprintf("%3u  ", info.mNumber);

			line += info.mbClearOnReset ? 'K' : ' ';
			line += info.mbOneShot ? 'O' : ' ';
			line += ' ';

			if (info.mbDeferred)
				line += "deferred     ";
			else if (info.mbBreakOnInsn)
				line += "per-insn     ";
			else {
				if (info.mbBreakOnPC)
					line += "PC  ";
				else if (info.mbBreakOnRead) {
					if (info.mbBreakOnWrite)
						line += "RW  ";
					else
						line += "R   ";
				} else if (info.mbBreakOnWrite)
					line += "W   ";

				if (info.mLength > 1)
					line.append_sprintf("%s-%s"
						, g_debugger.GetAddressText(info.mAddress, false, false).c_str()
						, g_debugger.GetAddressText(info.mAddress + info.mLength - 1, false, false).c_str()
						);
				else
					line.append_sprintf("%s     ", g_debugger.GetAddressText(info.mAddress, false, true).c_str());
			}

			if (g_debugger.GetBreakpointSourceLocation(useridx, file, lineno))
				line.append_sprintf("  `%s:%u`", file.c_str(), lineno);

			ATDebugExpNode *node = g_debugger.GetBreakpointCondition(useridx);

			if (node) {
				VDStringA expr;
				node->ToString(expr);
				line.append_sprintf(" (when %s)", expr.c_str());
			}

			const char *cmd = g_debugger.GetBreakpointCommand(useridx);
			if (cmd) {
				line.append_sprintf(" (run command: \"%s\")", cmd);
			}

			line += '\n';
			ATConsoleWrite(line.c_str());
		}
	}

	ATDiskInterface& diskIf = g_sim.GetDiskInterface(0);
	int sb = diskIf.GetSectorBreakpoint();

	if (sb >= 0)
		ATConsolePrintf("Sector breakpoint:        %d\n", sb);

	if (verboseArg) {
		auto *const bm = g_debugger.GetBreakpointManager();

		ATConsoleWrite("\n");
		ATConsoleWrite("System breakpoints:\n");

		ATBreakpointIndices indices;
		bm->GetAll(indices);

		for(uint32 sysBp : indices) {
			ATBreakpointInfo info;
			if (bm->GetInfo(sysBp, info)) {
				const char *mode = info.mbBreakOnPC ? "PC"
					: info.mbBreakOnRead ? info.mbBreakOnWrite ? "RW" : "R"
					: info.mbBreakOnWrite ? "W"
					: "";

				if (info.mLength > 1)
					ATConsolePrintf("  ~%-2d  %04X-%04X  %-2s\n", info.mTargetIndex, info.mAddress, info.mAddress + info.mLength - 1, mode);
				else
					ATConsolePrintf("  ~%-2d  %04X       %-2s\n", info.mTargetIndex,info.mAddress, mode);
			}
		}

		ATConsoleWrite("\n");

		const auto& cpu = g_sim.GetCPU();
		ATConsolePrintf("Main CPU core breakpoints (%u present):\n", cpu.GetBreakpointCount());

		sint32 pc = -1;
		while((pc = cpu.GetNextBreakpoint(pc)) >= 0) {
			ATConsolePrintf("  %04X\n", pc);
		}
	}
}

void ATConsoleCmdBreakptSector(ATDebuggerCmdParser& parser) {
	ATDebuggerCmdExprNum num(true, false, 0, 65535);
	parser >> num >> 0;

	ATDiskInterface& diskIf = g_sim.GetDiskInterface(0);
	if (num.IsStar()) {
		diskIf.SetSectorBreakpoint(-1);
		ATConsolePrintf("Disk sector breakpoint is disabled.\n");
	} else {
		int v = num.GetValue();

		diskIf.SetSectorBreakpoint(v);
		ATConsolePrintf("Disk sector breakpoint is now %d.\n", v);
	}
}

void ATConsoleCmdBreakptSetCondition(ATDebuggerCmdParser& parser) {
	ATDebuggerCmdName nameArg(true);
	ATDebuggerCmdString exprArg(true);

	parser >> nameArg >> exprArg >> 0;

	auto dotPos = nameArg->find_last_of('.');

	const auto *arg = nameArg->c_str();
	VDStringA groupName;
	if (dotPos != nameArg->npos) {
		groupName.assign(*nameArg, 0, dotPos);
		arg += dotPos + 1;
	}

	char dummy;
	unsigned number;
	if (1 != sscanf(arg, "%u %c", &number, &dummy))
		throw MyError("Invalid breakpoint number: %s", arg);

	sint32 useridx = g_debugger.LookupUserBreakpointByNum(number, groupName.c_str());

	if (useridx < 0)
		throw MyError("Invalid breakpoint number: %s", arg);

	VDStringA s(exprArg->c_str());

	vdautoptr<ATDebugExpNode> node;
	
	try {
		node = ATDebuggerParseExpression(s.c_str(), &g_debugger, ATGetDebugger()->GetExprOpts());
	} catch(ATDebuggerExprParseException& ex) {
		throw MyError("Unable to parse expression: %s", ex.c_str());
	}
	
	g_debugger.SetBreakpointCondition(useridx, node);
}

void ATConsoleCmdBreakptExpr(ATDebuggerCmdParser& parser) {
	ATDebuggerCmdSwitchStrArg groupArg("g");
	ATDebuggerCmdSwitch clearOnResetArg("k", false);
	ATDebuggerCmdSwitch oneShotArg("o", false);
	ATDebuggerCmdSwitch nonStopArg("n", false);
	ATDebuggerCmdString expr(true);
	ATDebuggerCmdQuotedString command(false);
	parser >> groupArg >> nonStopArg >> clearOnResetArg >> expr >> command >> 0;

	VDStringA s(expr->c_str());

	vdautoptr<ATDebugExpNode> node;
	
	try {
		ATDebugExpEvalContext immContext = g_debugger.GetEvalContext();
		node = ATDebuggerParseExpression(s.c_str(), &g_debugger, ATGetDebugger()->GetExprOpts(), &immContext);
	} catch(ATDebuggerExprParseException& ex) {
		ATConsolePrintf("Unable to parse expression: %s\n", ex.c_str());
		return;
	}

	const uint32 useridx = g_debugger.SetConditionalBreakpoint(node.release(), command.IsValid() ? command->c_str() : NULL, nonStopArg);
	const sint32 usernum = g_debugger.RegisterUserBreakpoint(useridx, groupArg.GetValue());

	if (clearOnResetArg)
		g_debugger.SetBreakpointClearOnReset(useridx, true);

	if (oneShotArg)
		g_debugger.SetBreakpointOneShot(useridx, true);

	g_sim.NotifyEvent(kATSimEvent_CPUPCBreakpointsUpdated);
	
	VDStringA condstr;

	ATDebugExpNode *cond = g_debugger.GetBreakpointCondition(useridx);

	if (cond)
		cond->ToString(condstr);

	ATDebuggerBreakpointInfo info = {};

	VDVERIFY(g_debugger.GetBreakpointInfo(useridx, info));

	VDStringA msg;
	msg.sprintf("Breakpoint %u set", usernum);

	if (info.mbBreakOnRead || info.mbBreakOnWrite) {
		if (info.mLength > 1)
			msg.append_sprintf(" on %s to range $%04X-$%04X", info.mbBreakOnWrite ? "write" : "read", info.mAddress, info.mAddress + info.mLength - 1);
		else if (info.mbBreakOnRead)
			msg.append_sprintf(" on read from $%04X", info.mAddress);
		else if (info.mbBreakOnWrite)
			msg.append_sprintf(" on write to $%04X", info.mAddress);
	} else if (info.mbBreakOnPC) {
		msg.append_sprintf(" at PC=$%04X", info.mAddress);
	}

	if (cond) {
		VDStringA condstr;

		cond->ToString(condstr);
		msg.append_sprintf(" with condition: %s\n", condstr.c_str());
	} else
		msg += ".\n";

	ATConsoleWrite(msg.c_str());

	if (info.mbBreakOnInsn)
		ATConsoleWrite("Warning: Per-instruction breakpoint set. Execution will be slow.\n");
}

void ATConsoleCmdUnassemble(ATDebuggerCmdParser& parser) {
	ATDebuggerCmdSwitch noPredictArg("p", false);
	ATDebuggerCmdSwitch m8Arg("m8", false);
	ATDebuggerCmdSwitch m16Arg("m16", false);
	ATDebuggerCmdSwitch x8Arg("x8", false);
	ATDebuggerCmdSwitch x16Arg("x16", false);
	ATDebuggerCmdSwitch emulationModeArg("e", false);
	ATDebuggerCmdSwitch noLabelsArg("n", false);
	ATDebuggerCmdExprAddr address(true, false);
	ATDebuggerCmdLength length(20, false, &address);

	parser >> noPredictArg >> m8Arg >> m16Arg >> x8Arg >> x16Arg >> emulationModeArg >> noLabelsArg >> address >> length >> 0;

	const bool predict = !noPredictArg;

	uint32 addr;
	
	if (address.IsValid())
		addr = address.GetValue();
	else
		addr = g_debugger.GetContinuationAddress();

	const uint32 addrSpaceAndBank = addr & 0xFFFF0000;
	const uint8 bank = addr < 0x1000000 ? (uint8)(addr >> 16) : 0;
	uint32 n = length;

	IATDebugTarget *target = g_debugger.GetTarget();
	const auto disasmMode = target->GetDisasmMode();

	ATCPUExecState state;
	target->GetExecState(state);

	ATCPUHistoryEntry hent {};
	ATDisassembleCaptureRegisterContext(target, hent);

	if (disasmMode == kATDebugDisasmMode_65C816) {
		if (emulationModeArg) {
			hent.mbEmulation = true;
			hent.mP |= 0x30;
		} else {
			hent.mbEmulation = false;

			if (m8Arg) hent.mP |= 0x20;
			if (m16Arg) hent.mP &= ~0x20;
			if (x8Arg) hent.mP |= 0x10;
			if (x16Arg) hent.mP &= ~0x10;
		}
	}

	const bool showLabels = !noLabelsArg;

	VDStringA s;
	for(uint32 i=0; i<n; ++i) {
		if (disasmMode == kATDebugDisasmMode_6502)
			ATDisassembleCaptureInsnContext(target, addr, hent);
		else
			ATDisassembleCaptureInsnContext(target, (uint16)addr, bank, hent);

		s.clear();
		addr = addrSpaceAndBank + (uint16)ATDisassembleInsn(s, target, disasmMode, hent, false, false, true, true, showLabels, false, false, true, showLabels, true);

		if (predict)
			ATDisassemblePredictContext(hent, disasmMode);

		s += '\n';
		ATConsoleWrite(s.c_str());

		if ((i & 15) == 15 && ATConsoleCheckBreak())
			break;
	}

	g_debugger.SetContinuationAddress(((uint32)bank << 16) + (addr & 0xffff));
}

void ATConsoleCmdRegisters(ATDebuggerCmdParser& parser) {
	ATCPUEmulator& cpu = g_sim.GetCPU();

	if (parser.IsEmpty()) {
		g_debugger.DumpState(true);
		return;
	}

	ATDebuggerCmdName nameArg(true);
	ATDebuggerCmdExprNum valueArg(true);

	parser >> nameArg >> valueArg >> 0;

	sint32 v = valueArg.GetValue();

	VDStringSpanA regName(!nameArg->empty() && nameArg->operator[](0) == '@' ? nameArg->c_str() + 1 : nameArg->c_str());

	if (regName.size() == 2 && regName[0] == 't' && regName[1] >= '0' && regName[1] <= '9') {
		g_debugger.SetExprTemp(regName[1] - '0', v);
	} else {
		bool resetContinuationAddr = false;
		bool suppressExecStateUpdate = false;
		IATDebugTarget *target = g_debugger.GetTarget();

		ATCPUExecState state;
		target->GetExecState(state);

		if (target->GetDisasmMode() == kATDebugDisasmMode_8048) {
			ATCPUExecState8048& state8048 = state.m8048;
			uint8 *r = state8048.mReg[state8048.mPSW & 0x10 ? 1 : 0];

			if (regName == "pc") {
				state8048.mPC = v;
			} else if (regName == "a") {
				state8048.mA = v;
			} else if (regName == "psw") {
				state8048.mPSW = v;
			} else if (regName == "r0") {
				r[0] = v;
			} else if (regName == "r1") {
				r[1] = v;
			} else if (regName == "r2") {
				r[2] = v;
			} else if (regName == "r3") {
				r[3] = v;
			} else if (regName == "r4") {
				r[4] = v;
			} else if (regName == "r5") {
				r[5] = v;
			} else if (regName == "r6") {
				r[6] = v;
			} else if (regName == "r7") {
				r[7] = v;
			} else {
				goto unknown;
			}
		} else if (target->GetDisasmMode() == kATDebugDisasmMode_Z80) {
			ATCPUExecStateZ80& stateZ80 = state.mZ80;

			if (regName == "pc") {
				stateZ80.mPC = v;
			} else if (regName == "a") {
				stateZ80.mA = v;
			} else if (regName == "f") {
				stateZ80.mF = v;
			} else if (regName == "b") {
				stateZ80.mB = v;
			} else if (regName == "c") {
				stateZ80.mC = v;
			} else if (regName == "d") {
				stateZ80.mD = v;
			} else if (regName == "e") {
				stateZ80.mE = v;
			} else if (regName == "h") {
				stateZ80.mH = v;
			} else if (regName == "l") {
				stateZ80.mL = v;
			} else if (regName == "a'") {
				stateZ80.mAltA = v;
			} else if (regName == "f'") {
				stateZ80.mAltF = v;
			} else if (regName == "b'") {
				stateZ80.mAltB = v;
			} else if (regName == "c'") {
				stateZ80.mAltC = v;
			} else if (regName == "d'") {
				stateZ80.mAltD = v;
			} else if (regName == "e'") {
				stateZ80.mAltE = v;
			} else if (regName == "h'") {
				stateZ80.mAltH = v;
			} else if (regName == "l'") {
				stateZ80.mAltL = v;
			} else if (regName == "i") {
				stateZ80.mI = v;
			} else if (regName == "r") {
				stateZ80.mR = v;
			} else if (regName == "af") {
				stateZ80.mA = (uint8)(v >> 8);
				stateZ80.mF = (uint8)v;
			} else if (regName == "bc") {
				stateZ80.mB = (uint8)(v >> 8);
				stateZ80.mC = (uint8)v;
			} else if (regName == "de") {
				stateZ80.mD = (uint8)(v >> 8);
				stateZ80.mE = (uint8)v;
			} else if (regName == "hl") {
				stateZ80.mH = (uint8)(v >> 8);
				stateZ80.mL = (uint8)v;
			} else if (regName == "af'") {
				stateZ80.mAltA = (uint8)(v >> 8);
				stateZ80.mAltF = (uint8)v;
			} else if (regName == "bc'") {
				stateZ80.mAltB = (uint8)(v >> 8);
				stateZ80.mAltC = (uint8)v;
			} else if (regName == "de'") {
				stateZ80.mAltD = (uint8)(v >> 8);
				stateZ80.mAltE = (uint8)v;
			} else if (regName == "hl'") {
				stateZ80.mAltH = (uint8)(v >> 8);
				stateZ80.mAltL = (uint8)v;
			} else if (regName == "ix") {
				stateZ80.mIX = (uint16)v;
			} else if (regName == "iy") {
				stateZ80.mIY = (uint16)v;
			} else if (regName == "sp") {
				stateZ80.mSP = (uint16)v;
			} else {
				goto unknown;
			}
		} else if (target->GetDisasmMode() == kATDebugDisasmMode_6809) {
			ATCPUExecState6809& state6809 = state.m6809;

			if (regName == "pc") {
				state6809.mPC = v;
			} else if (regName == "a") {
				state6809.mA = v;
			} else if (regName == "b") {
				state6809.mB = v;
			} else if (regName == "x") {
				state6809.mX = v;
			} else if (regName == "y") {
				state6809.mY = v;
			} else if (regName == "u") {
				state6809.mU = v;
			} else if (regName == "s") {
				state6809.mS = v;
			} else if (regName == "dp") {
				state6809.mDP = v;
			} else if (regName == "cc") {
				state6809.mCC = v;
			} else {
				goto unknown;
			}
		} else {
			ATCPUExecState6502& state6502 = state.m6502;

			if (regName == "pc") {
				if (!g_debugger.GetTargetIndex()) {
					g_debugger.SetPC(v);
					suppressExecStateUpdate = true;
				} else
					state6502.mPC = v;
				resetContinuationAddr = true;
			} else if (regName == "x") {
				state6502.mX = (uint8)v;
				state6502.mXH = (uint8)(v >> 8);
			} else if (regName == "y") {
				state6502.mY = (uint8)v;
				state6502.mYH = (uint8)(v >> 8);
			} else if (regName == "s") {
				state6502.mS = (uint8)v;
			} else if (regName == "p") {
				state6502.mP = (uint8)v;
			} else if (regName == "a") {
				state6502.mA = (uint8)v;
			} else if (regName == "c") {
				state6502.mA = (uint8)v;
				state6502.mAH = (uint8)(v >> 8);
			} else if (regName == "d") {
				state6502.mDP = v;
			} else if (regName == "k" || regName == "pbr") {
				state6502.mK = (uint8)v;
			} else if (regName == "b" || regName == "dbr") {
				state6502.mB = (uint8)v;
			} else if (regName == "e") {
				state6502.mbEmulationFlag = (v != 0);
			} else if (regName.size() == 3 && regName[0] == 'p' && regName[1] == '.') {
				uint8 p = state6502.mP;
				uint8 mask;

				switch(regName[2]) {
					case 'n': mask = 0x80; break;
					case 'v': mask = 0x40; break;
					case 'm': mask = 0x20; break;
					case 'x': mask = 0x10; break;
					case 'd': mask = 0x08; break;
					case 'i': mask = 0x04; break;
					case 'z': mask = 0x02; break;
					case 'c': mask = 0x01; break;
					default:
						goto unknown;
				}

				if (v)
					p |= mask;
				else
					p &= ~mask;

				state6502.mP = p;
			} else {
				goto unknown;
			}

			if (resetContinuationAddr)
				g_debugger.SetContinuationAddress(((uint32)state6502.mK << 16) + state6502.mPC);
		}

		if (!suppressExecStateUpdate)
			target->SetExecState(state);
	}

	g_debugger.SendRegisterUpdate();
	return;

unknown:
	ATConsolePrintf("Unknown register '%s'\n", nameArg->c_str());
}

void ATConsoleCmdDumpATASCII(ATDebuggerCmdParser& parser) {
	ATDebuggerCmdExprAddr address(true, true);
	ATDebuggerCmdLength length(128, false, &address);

	parser >> address >> length >> 0;

	uint32 addr = (uint32)address.GetValue();
	uint32 atype = addr & kATAddressSpaceMask;
	uint32 n = length;

	if (n > 128)
		n = 128;

	char str[129];
	uint32 idx = 0;

	IATDebugTarget *target = g_debugger.GetTarget();
	while(idx < n) {
		uint8 c = target->DebugReadByte(atype + ((addr + idx) & kATAddressOffsetMask));

		if (c < 0x20 || c >= 0x7f) {
			if (!length.IsValid())
				break;

			c = '.';
		}

		str[idx++] = c;
	}

	str[idx] = 0;

	ATConsolePrintf("%s: \"%s\"\n", g_debugger.GetAddressText(addr, false).c_str(), str);
}

void ATConsoleCmdDumpINTERNAL(ATDebuggerCmdParser& parser) {
	ATDebuggerCmdExprAddr address(true, true);
	ATDebuggerCmdLength length(128, false, &address);

	parser >> address >> length >> 0;

	uint32 addr = address.GetValue();
	uint32 atype = addr & kATAddressSpaceMask;
	uint32 n = length;

	if (n > 128)
		n = 128;

	char str[129];
	uint32 idx = 0;

	IATDebugTarget *target = g_debugger.GetTarget();
	while(idx < n) {
		uint8 c = target->DebugReadByte(atype + ((addr + idx) & kATAddressOffsetMask));

		static const uint8 kXlat[4]={ 0x20, 0x60, 0x40, 0x00 };

		c ^= kXlat[(c >> 5) & 3];

		if (c < 0x20 || c >= 0x7f) {
			if (!length.IsValid())
				break;

			c = '.';
		}

		str[idx++] = c;
	}

	str[idx] = 0;

	ATConsolePrintf("%s: \"%s\"\n", g_debugger.GetAddressText(addr, false).c_str(), str);
}

void ATConsoleCmdDumpBinary(ATDebuggerCmdParser& parser) {
	ATDebuggerCmdExprAddr address(true, false);
	ATDebuggerCmdLength lengthArg(16, false, &address);
	ATDebuggerCmdSwitchStrArg charSw("c");
	ATDebuggerCmdSwitchNumArg reptSw("r", 1, 16, 1);
	ATDebuggerCmdSwitch upsideSw("u", false);

	parser >> charSw >> reptSw >> upsideSw >> address >> lengthArg >> 0;

	uint32 addr = address.IsValid() ? address.GetValue() : g_debugger.GetContinuationAddress();
	uint32 atype = addr & kATAddressSpaceMask;

	char ch[2] = { '0', '1' };

	if (charSw.IsValid()) {
		const char *s = charSw.GetValue();

		if (s[0]) {
			if (s[1]) {
				ch[0] = s[0];
				ch[1] = s[1];
			} else
				ch[1] = s[0];
		}
	}

	uint32 length = lengthArg;
	if (upsideSw)
		addr += (length - 1);

	VDStringA line;

	IATDebugTarget *target = g_debugger.GetTarget();
	uint32 rept = reptSw.GetValue();
	while(length--) {
		if (!(length & 15) && ATConsoleCheckBreak())
			break;

		uint8 v = target->DebugReadByte(atype + (addr & kATAddressOffsetMask));

		line.sprintf("%s: ", g_debugger.GetAddressText(addr, false).c_str());

		for(int i=0; i<8; ++i) {
			char c = ch[v >> 7];

			for(uint32 j=0; j<rept; ++j)
				line += c;

			v += v;
		}

		line += '\n';

		ATConsoleWrite(line.c_str());

		if (upsideSw)
			--addr;
		else
			++addr;
	}

	g_debugger.SetContinuationAddress(atype + (addr & kATAddressOffsetMask));
}

void ATConsoleCmdDumpBytes(ATDebuggerCmdParser& parser, bool internal) {
	ATDebuggerCmdExprAddr address(true, false);
	ATDebuggerCmdLength length(128, false, &address);
	ATDebuggerCmdSwitch colorArg("c", false);
	ATDebuggerCmdSwitchNumArg widthArg("w", 1, 128, 16);

	parser >> colorArg >> widthArg >> address >> length >> 0;

	IATDebugTarget *const target = g_debugger.GetTarget();

	uint32 addr = address.IsValid() ? address.GetValue() : g_debugger.GetContinuationAddress();
	uint32 atype = addr & kATAddressSpaceMask;

	char chbuf[17];

	chbuf[16] = 0;

	uint32 width = widthArg.GetValue();
	uint32 rows = (length + width - 1) / width;

	VDStringA line;
	vdblock<uint8> buf(width);

	while(rows--) {
		if (15 == (rows & 15) && ATConsoleCheckBreak())
			break;

		line = g_debugger.GetAddressText(addr, false);
		line += ':';

		for(uint32 i=0; i<width; ++i) {
			buf[i] = target->DebugReadByte(atype + ((addr + i) & kATAddressOffsetMask));

			line.append_sprintf(" %02X", buf[i]);
		}

		line += " |";

		for(uint32 i=0; i<width; ++i) {
			static const uint8 kXlat[4]={ 0x20, 0x60, 0x40, 0x00 };
			uint8 v = buf[i];

			if (internal) {
				if (colorArg)
					v &= 0x3F;

				v ^= kXlat[(v >> 5) & 3];
			} else {
				// In Gr.1/2, only the low 6 bits of the Internal code matter... so
				// here we emulate that truncation and a conversion back to ATASCII.
				//
				// ATASCII	Internal	New ATASCII
				// 00-1F	40-5F		20-3F
				// 20-3F	00-1F		20-3F
				// 40-5F	20-3F		40-5F
				// 60-7F	60-7F		40-5F

				if (colorArg) {
					static const uint8 kMode12Xlat[8] = { 0x20, 0x00, 0x00, 0x20, 0xA0, 0x80, 0x80, 0xA0 };

					v ^= kMode12Xlat[v >> 5];
				}

			}

			char c = (char)v;
			if ((uint8)(v - 0x20) >= 0x5F)
				c = '.';

			line += c;
		}

		line += "|\n";

		ATConsoleWrite(line.c_str());
		
		addr += width;
	}

	g_debugger.SetContinuationAddress(atype + (addr & kATAddressOffsetMask));
}

void ATConsoleCmdDumpBytes(ATDebuggerCmdParser& parser) {
	ATConsoleCmdDumpBytes(parser, false);
}

void ATConsoleCmdDumpBytesInternal(ATDebuggerCmdParser& parser) {
	ATConsoleCmdDumpBytes(parser, true);
}

void ATConsoleCmdDumpWords(ATDebuggerCmdParser& parser) {
	ATDebuggerCmdExprAddr address(true, false);
	ATDebuggerCmdLength length(64, false, &address);

	parser >> address >> length >> 0;

	IATDebugTarget *const target = g_debugger.GetTarget();

	uint32 addr = address.IsValid() ? address.GetValue() : g_debugger.GetContinuationAddress();
	uint32 atype = addr & kATAddressSpaceMask;

	uint32 rows = (length + 7) >> 3;

	uint8 buf[16];

	while(rows--) {
		if (15 == (rows & 15) && ATConsoleCheckBreak())
			break;

		for(int i=0; i<16; ++i) {
			uint8 v = target->DebugReadByte(atype + ((addr + i) & kATAddressOffsetMask));
			buf[i] = v;
		}

		ATConsolePrintf("%s: %04X %04X %04X %04X-%04X %04X %04X %04X\n"
			, g_debugger.GetAddressText(addr, false).c_str()
			, buf[0] + 256*buf[1]
			, buf[2] + 256*buf[3]
			, buf[4] + 256*buf[5]
			, buf[6] + 256*buf[7]
			, buf[8] + 256*buf[9]
			, buf[10] + 256*buf[11]
			, buf[12] + 256*buf[13]
			, buf[14] + 256*buf[15]);

		addr += 16;
	}

	g_debugger.SetContinuationAddress(atype + (addr & kATAddressOffsetMask));
}

void ATConsoleCmdDumpDwords(ATDebuggerCmdParser& parser) {
	ATDebuggerCmdExprAddr address(true, true);
	ATDebuggerCmdLength length(64, false, &address);

	parser >> address >> length >> 0;

	IATDebugTarget *const target = g_debugger.GetTarget();

	uint32 addr = address.GetValue();
	uint32 atype = addr & kATAddressSpaceMask;

	uint32 rows = (length + 3) >> 2;

	uint8 buf[16];

	while(rows--) {
		if (15 == (rows & 15) && ATConsoleCheckBreak())
			break;

		for(int i=0; i<16; ++i) {
			uint8 v = target->DebugReadByte(atype + ((addr + i) & kATAddressOffsetMask));
			buf[i] = v;
		}

		ATConsolePrintf("%s: %08X %08X %08X %08X\n"
			, g_debugger.GetAddressText(addr, false).c_str()
			, VDReadUnalignedLEU32(buf + 0)
			, VDReadUnalignedLEU32(buf + 4)
			, VDReadUnalignedLEU32(buf + 8)
			, VDReadUnalignedLEU32(buf + 12)
			);

		addr += 16;
	}

	g_debugger.SetContinuationAddress(atype + (addr & kATAddressOffsetMask));
}

void ATConsoleCmdDumpFloats(ATDebuggerCmdParser& parser) {
	ATDebuggerCmdExprAddr address(true, false);
	ATDebuggerCmdLength length(1, false, &address);

	parser >> address >> length >> 0;

	IATDebugTarget *const target = g_debugger.GetTarget();

	uint32 addr = address.IsValid() ? address.GetValue() : g_debugger.GetContinuationAddress();
	uint32 atype = addr & kATAddressSpaceMask;
	uint8 data[6];

	uint32 rows = length;
	while(rows--) {
		if (15 == (rows & 15) && ATConsoleCheckBreak())
			break;

		for(int i=0; i<6; ++i) {
			data[i] = target->DebugReadByte(atype + ((addr + i) & kATAddressOffsetMask));
		}

		ATConsolePrintf("%s: %02X %02X %02X %02X %02X %02X  %.10g\n"
			, g_debugger.GetAddressText(addr, false).c_str()
			, data[0]
			, data[1]
			, data[2]
			, data[3]
			, data[4]
			, data[5]
			, ATReadDecFloatAsBinary(data));

		addr += 6;
	}

	g_debugger.SetContinuationAddress(atype + (addr & kATAddressOffsetMask));
}

void ATConsoleCmdListModules(ATDebuggerCmdParser& parser) {
	g_debugger.ListModules();
}

void ATConsoleCmdListNearestSymbol(ATDebuggerCmdParser& parser) {
	ATDebuggerCmdExprAddr addrArg(true, true);

	parser >> addrArg >> 0;

	sint32 v = addrArg.GetValue();
	if (!addrArg.IsValid()) {
		ATConsolePrintf("Unable to resolve symbol.\n");
		return;
	}

	uint32 addr = (uint32)v;

	ATDebuggerSymbol sym;
	if (g_debugger.LookupSymbol(addr, kATSymbol_Any, sym)) {
		VDStringW sourceFile;
		ATSourceLineInfo lineInfo;
		uint32 moduleId;

		if (g_debugger.LookupLine(addr, false, moduleId, lineInfo) &&
			g_debugger.GetSourceFilePath(moduleId, lineInfo.mFileId, sourceFile))
		{
			ATConsolePrintf("%s = %s + %d [%ls:%d]\n", g_debugger.GetAddressText(addr, false).c_str(), sym.mSymbol.mpName, (int)addr - (int)sym.mSymbol.mOffset, sourceFile.c_str(), lineInfo.mLine);
		} else {
			ATConsolePrintf("%s = %s + %d\n", g_debugger.GetAddressText(addr, false).c_str(), sym.mSymbol.mpName, (int)addr - (int)sym.mSymbol.mOffset);
		}
	} else {
		ATConsolePrintf("No symbol found for address: %s\n", g_debugger.GetAddressText(addr, false).c_str());
	}
}

void ATConsoleCmdLogFilterEnable(ATDebuggerCmdParser& parser) {
	ATDebuggerCmdName name(true);
	parser >> name >> 0;

	bool star = (*name) == "*";

	for(ATLogChannel *p = ATLogGetFirstChannel();
		p;
		p = ATLogGetNextChannel(p))
	{
		if (star || !vdstricmp(p->GetName(), name->c_str())) {
			if (!p->IsEnabled() || p->GetTagFlags()) {
				p->SetEnabled(true);
				p->SetTagFlags(0);
				ATConsolePrintf("Enabled logging channel: %s\n", p->GetName());
			}

			if (!star)
				return;
		}
	}

	if (!star)
		ATConsolePrintf("Unknown logging channel: %s\n", name->c_str());
}

void ATConsoleCmdLogFilterDisable(ATDebuggerCmdParser& parser) {
	ATDebuggerCmdName name(true);
	parser >> name >> 0;

	bool star = (*name) == "*";

	for(ATLogChannel *p = ATLogGetFirstChannel();
		p;
		p = ATLogGetNextChannel(p))
	{
		if (star || !vdstricmp(p->GetName(), name->c_str())) {
			if (p->IsEnabled()) {
				p->SetEnabled(false);
				ATConsolePrintf("Disabled logging channel: %s\n", p->GetName());
			}

			if (!star)
				return;
		}
	}

	if (star)
		return;

	ATConsolePrintf("Unknown logging channel: %s\n", name->c_str());
}

void ATConsoleCmdLogFilterTag(ATDebuggerCmdParser& parser) {
	ATDebuggerCmdSwitch swTimestamp("t", false);
	ATDebuggerCmdSwitch swCassettePos("c", false);
	ATDebuggerCmdName name(true);
	parser >> swTimestamp >> swCassettePos >> name >> 0;

	uint32 flags = 0;

	if (swTimestamp)
		flags |= kATLogFlags_Timestamp;

	if (swCassettePos)
		flags |= kATLogFlags_CassettePos;

	if (!flags)
		flags = kATLogFlags_Timestamp;

	bool star = (*name) == "*";

	for(ATLogChannel *p = ATLogGetFirstChannel();
		p;
		p = ATLogGetNextChannel(p))
	{
		if (star || !vdstricmp(p->GetName(), name->c_str())) {
			if (!p->IsEnabled() || p->GetTagFlags() != flags) {
				p->SetEnabled(true);
				p->SetTagFlags(flags);
				ATConsolePrintf("Enabled logging channel with tagging: %s\n", p->GetName());
			}

			if (!star)
				return;
		}
	}

	if (star)
		return;

	ATConsolePrintf("Unknown logging channel: %s\n", name->c_str());
}

namespace {
	struct ChannelSortByName {
		bool operator()(const ATDebuggerLogChannel *x, const ATDebuggerLogChannel *y) const {
			return vdstricmp(x->GetName(), y->GetName()) < 0;
		}
	};
}

void ATConsoleCmdLogFilterList(ATDebuggerCmdParser& parser) {
	parser >> 0;

	typedef vdfastvector<ATDebuggerLogChannel *> Channels;
	Channels channels;

	for(ATLogChannel *p = ATLogGetFirstChannel();
		p;
		p = ATLogGetNextChannel(p))
	{
		channels.push_back(p);
	}

	std::sort(channels.begin(), channels.end(), ChannelSortByName());

	for(Channels::const_iterator it(channels.begin()), itEnd(channels.end());
		it != itEnd;
		++it)
	{
		ATDebuggerLogChannel *p = *it;
		ATConsolePrintf("%-10s  %-3s  %s\n", p->GetName(), p->IsEnabled() ? "on" : "off", p->GetDesc());
	}
}

void ATConsoleCmdVerifierTargetAdd(ATDebuggerCmdParser& parser) {
	ATDebuggerCmdExprAddr addr(false, true);
	parser >> addr >> 0;

	ATCPUVerifier *verifier = g_sim.GetVerifier();

	if (!verifier) {
		ATConsoleWrite("Verifier is not active.\n");
		return;
	}

	verifier->AddAllowedTarget(addr.GetValue());
}

void ATConsoleCmdVerifierTargetClear(ATDebuggerCmdParser& parser) {
	ATDebuggerCmdExprAddr addr(false, true, true);
	parser >> addr >> 0;

	ATCPUVerifier *verifier = g_sim.GetVerifier();

	if (!verifier) {
		ATConsoleWrite("Verifier is not active.\n");
		return;
	}

	if (addr.IsStar()) {
		verifier->RemoveAllowedTargets();
		ATConsoleWrite("All allowed targets cleared.\n");
	} else
		verifier->RemoveAllowedTarget(addr.GetValue());
}

void ATConsoleCmdVerifierTargetList(ATDebuggerCmdParser& parser) {
	parser >> 0;

	ATCPUVerifier *verifier = g_sim.GetVerifier();

	if (!verifier) {
		ATConsoleWrite("Verifier is not active.\n");
		return;
	}

	vdfastvector<uint16> targets;
	verifier->GetAllowedTargets(targets);

	ATConsoleWrite("Allowed kernel entry targets:\n");
	for(vdfastvector<uint16>::const_iterator it(targets.begin()), itEnd(targets.end()); it != itEnd; ++it) {
		ATConsolePrintf("    %s\n", g_debugger.GetAddressText(*it, false, true).c_str());
	}
}

void ATConsoleCmdVerifierTargetReset(ATDebuggerCmdParser& parser) {
	parser >> 0;

	ATCPUVerifier *verifier = g_sim.GetVerifier();

	if (!verifier) {
		ATConsoleWrite("Verifier is not active.\n");
		return;
	}

	verifier->ResetAllowedTargets();

	ATConsoleWrite("Verifier allowed targets list reset.\n");
}

void ATConsoleCmdWatchByte(ATDebuggerCmdParser& parser) {
	ATDebuggerCmdExprAddr addr(false, true);
	parser >> addr >> 0;

	int idx = g_debugger.AddWatch(addr.GetValue(), 1);

	if (idx >= 0)
		ATConsolePrintf("Watch entry %d set.\n", idx);
	else
		ATConsoleWrite("No free watch slots available.\n");
}

void ATConsoleCmdWatchWord(ATDebuggerCmdParser& parser) {
	ATDebuggerCmdExprAddr addr(false, true);
	parser >> addr >> 0;

	int idx = g_debugger.AddWatch(addr.GetValue(), 2);

	if (idx >= 0)
		ATConsolePrintf("Watch entry %d set.\n", idx);
	else
		ATConsoleWrite("No free watch slots available.\n");
}

void ATConsoleCmdWatchExpr(ATDebuggerCmdParser& parser) {
	ATDebuggerCmdExpr expr(false);

	parser >> expr >> 0;

	if (!expr.GetValue())
		return;

	int idx = g_debugger.AddWatchExpr(expr.GetValue());

	if (idx >= 0) {
		expr.DetachValue();

		ATConsolePrintf("Watch entry %d set.\n", idx);
	} else
		ATConsoleWrite("No free watch slots available.\n");
}

void ATConsoleCmdWatchClear(ATDebuggerCmdParser& parser) {
	ATDebuggerCmdExprNum numArg(false, false, 0, INT32_MAX, 0, true);

	parser >> numArg >> 0;

	if (!numArg.IsValid())
		return;

	if (numArg.IsStar()) {
		g_debugger.ClearAllWatches();
		ATConsoleWrite("All watch entries cleared.\n");
		return;
	}

	
	int idx = numArg.GetValue();
	if (!g_debugger.ClearWatch(idx)) {
		ATConsolePrintf("Invalid watch index: %d\n", idx);
		return;
	}

	ATConsolePrintf("Watch entry %d cleared.\n", idx);
}

void ATConsoleCmdWatchList(ATDebuggerCmdParser& parser) {
	ATConsoleWrite("#  Len Address\n");

	for(int i=0; i<8; ++i) {
		ATDebuggerWatchInfo winfo;
		if (g_debugger.GetWatchInfo(i, winfo)) {
			if (winfo.mpExpr) {
				VDStringA s;
				winfo.mpExpr->ToString(s);
				ATConsolePrintf("%d  %s\n", i, s.c_str());
			} else {
				ATConsolePrintf("%d  %2d  %s\n", i, winfo.mLen, g_debugger.GetAddressText(winfo.mAddress, false, true).c_str());
			}
		}
	}
}

void ATConsoleCmdSymbolAdd(ATDebuggerCmdParser& parser) {
	ATDebuggerCmdName name(true);
	ATDebuggerCmdExprAddr addr(false, true);
	ATDebuggerCmdLength len(1, false, &addr);
	ATDebuggerCmdSwitch swR("r", false);
	ATDebuggerCmdSwitch swW("w", false);

	parser >> swR >> swW >> name >> addr >> len >> 0;

	VDStringA s(name->c_str());

	for(VDStringA::iterator it(s.begin()), itEnd(s.end()); it != itEnd; ++it)
		*it = toupper((unsigned char)*it);

	g_debugger.AddCustomSymbol(addr.GetValue(), len, s.c_str(), swR ? kATSymbol_Read | kATSymbol_Execute : swW ? kATSymbol_Write : kATSymbol_Any);
}

void ATConsoleCmdSymbolClear(ATDebuggerCmdParser& parser) {
	parser >> 0;

	g_debugger.UnloadSymbols(ATDebugger::kModuleId_Manual);
	ATConsoleWrite("Custom symbols cleared.\n");
}

void ATConsoleCmdSymbolRemove(ATDebuggerCmdParser& parser) {
	ATDebuggerCmdExprAddr addr(false, true);

	parser >> addr >> 0;

	g_debugger.RemoveCustomSymbol(addr.GetValue());
}

void ATConsoleCmdSymbolRead(ATDebuggerCmdParser& parser) {
	ATDebuggerCmdName name(true);

	parser >> name >> 0;

	g_debugger.LoadCustomSymbols(VDTextAToW(name->c_str()).c_str());
}

void ATConsoleCmdSymbolWrite(ATDebuggerCmdParser& parser) {
	ATDebuggerCmdName name(true);

	parser >> name >> 0;

	g_debugger.SaveCustomSymbols(VDTextAToW(name->c_str()).c_str());
}

void ATConsoleCmdEnter(ATDebuggerCmdParser& parser) {
	ATDebuggerCmdExprAddr addrArg(true, true);

	parser >> addrArg;

	uint32 addr = (uint32)addrArg.GetValue();

	vdfastvector<uint8> data;

	while(!parser.IsEmpty()) {
		ATDebuggerCmdExprNum valArg(true);

		parser >> valArg;

		sint32 result = valArg.GetValue();
		if (result < 0 || result > 255)
			throw MyError("Value out of range: %d", result);

		data.push_back((uint8)result);
	}

	IATDebugTarget *target = g_debugger.GetTarget();
	for(uint8 v : data) {
		target->WriteByte(addr++, v);
	}
}

void ATConsoleCmdEnterWords(ATDebuggerCmdParser& parser) {
	ATDebuggerCmdExprAddr addrArg(true, true);

	parser >> addrArg;

	uint32 addr = (uint32)addrArg.GetValue();

	vdfastvector<uint16> data;

	while(!parser.IsEmpty()) {
		ATDebuggerCmdExprNum valArg(true);

		parser >> valArg;

		sint32 result = valArg.GetValue();
		if (result < 0 || result > 0xFFFF)
			throw MyError("Value out of range: %d", result);

		data.push_back((uint16)result);
	}

	IATDebugTarget *target = g_debugger.GetTarget();
	for(uint16 v : data) {
		target->WriteByte(addr++, (uint8)v);
		target->WriteByte(addr++, (uint8)(v >> 8));
	}
}

void ATConsoleCmdFill(ATDebuggerCmdParser& parser) {
	ATDebuggerCmdExprAddr addrarg(true, true);
	ATDebuggerCmdLength lenarg(0, true, &addrarg);
	ATDebuggerCmdExprNum val(true, true, 0, 255);
	
	parser >> addrarg >> lenarg >> val;

	vdfastvector<uint8> buf;
	uint8 c = val.GetValue();

	for(;;) {
		buf.push_back(c);

		ATDebuggerCmdExprNum val(false, true, 0, 255);
		parser >> val;

		if (!val.IsValid())
			break;

		c = val.GetValue();
	}

	parser >> 0;

	uint32 addrspace = addrarg.GetValue() & kATAddressSpaceMask;
	uint32 addroffset = addrarg.GetValue() & kATAddressOffsetMask;

	IATDebugTarget *target = g_debugger.GetTarget();

	const uint8 *patstart = buf.data();
	const uint8 *patend = patstart + buf.size();
	const uint8 *patsrc = patstart;
	for(uint32 len = lenarg; len; --len) {
		target->WriteByte(addrspace + addroffset, *patsrc);

		if (++patsrc == patend)
			patsrc = patstart;

		addroffset = (addroffset + 1) & kATAddressOffsetMask;
	}

	if (lenarg) {
		ATConsolePrintf("Filled %s-%s.\n"
		, g_debugger.GetAddressText(addrarg.GetValue(), false).c_str()
		, g_debugger.GetAddressText(addrspace + ((addroffset - 1) & kATAddressOffsetMask), false).c_str());
	}
}

void ATConsoleCmdFillExp(ATDebuggerCmdParser& parser) {
	ATDebuggerCmdExprAddr addrarg(true, true);
	ATDebuggerCmdLength lenarg(0, true, &addrarg);
	ATDebuggerCmdExpr valexpr(true);
	
	parser >> addrarg >> lenarg >> valexpr >> 0;

	uint32 addrspace = addrarg.GetValue() & kATAddressSpaceMask;
	uint32 addroffset = addrarg.GetValue() & kATAddressOffsetMask;

	ATDebugExpEvalContext ctx = g_debugger.GetEvalContext();
	ctx.mbAccessValid = true;
	ctx.mbAccessWriteValid = true;
	ctx.mAccessValue = 0;

	ATDebugExpNode *xpn = valexpr.GetValue();
	IATDebugTarget *target = g_debugger.GetTarget();

	for(uint32 len = lenarg; len; --len) {
		sint32 v;
		uint32 addr = addrspace + addroffset;

		ctx.mAccessAddress = addr;

		if (!xpn->Evaluate(v, ctx))
			throw MyError("Evaluation error at %s.", g_debugger.GetAddressText(addr, true).c_str());

		++ctx.mAccessValue;

		target->WriteByte(addr, (uint8)v);

		addroffset = (addroffset + 1) & kATAddressOffsetMask;
	}

	if (lenarg) {
		ATConsolePrintf("Filled %s-%s.\n"
		, g_debugger.GetAddressText(addrarg.GetValue(), false).c_str()
		, g_debugger.GetAddressText(addrspace + ((addroffset - 1) & kATAddressOffsetMask), false).c_str());
	}
}

void ATConsoleCmdMove(ATDebuggerCmdParser& parser) {
	ATDebuggerCmdExprAddr srcaddrarg(true, true);
	ATDebuggerCmdLength lenarg(0, true, &srcaddrarg);
	ATDebuggerCmdExprAddr dstaddrarg(true, true);
	
	parser >> srcaddrarg >> lenarg >> dstaddrarg >> 0;

	uint32 len = lenarg;
	uint32 srcspace = srcaddrarg.GetValue() & kATAddressSpaceMask;
	uint32 srcoffset = srcaddrarg.GetValue() & kATAddressOffsetMask;
	uint32 dstspace = dstaddrarg.GetValue() & kATAddressSpaceMask;
	uint32 dstoffset = dstaddrarg.GetValue() & kATAddressOffsetMask;

	IATDebugTarget *target = g_debugger.GetTarget();

	if (srcspace == dstspace && dstoffset >= srcoffset && dstoffset < srcoffset + len) {
		srcoffset = (srcoffset + len) & kATAddressOffsetMask;
		dstoffset = (dstoffset + len) & kATAddressOffsetMask;

		while(len--) {
			srcoffset = (srcoffset - 1) & kATAddressOffsetMask;
			dstoffset = (dstoffset - 1) & kATAddressOffsetMask;

			const uint8 c = target->DebugReadByte(srcspace + srcoffset);
			target->WriteByte(dstspace + dstoffset, c);
		}
	} else {
		while(len--) {
			const uint8 c = target->DebugReadByte(srcspace + srcoffset);
			target->WriteByte(dstspace + dstoffset, c);

			srcoffset = (srcoffset + 1) & kATAddressOffsetMask;
			dstoffset = (dstoffset + 1) & kATAddressOffsetMask;
		}
	}
}

void ATConsoleCmdHeatMapDumpAccesses(ATDebuggerCmdParser& parser) {
	ATDebuggerCmdExprAddr addrarg(false, false);
	ATDebuggerCmdLength lenarg(0, false, &addrarg);
	parser >> addrarg >> lenarg >> 0;

	if (!g_sim.IsHeatMapEnabled())
		throw MyError("Heat map is not enabled.\n");

	ATCPUHeatMap& heatmap = *g_sim.GetHeatMap();

	uint32 addr = addrarg.IsValid() ? 0 : addrarg.GetValue() & 0xffff;
	uint32 len = lenarg.IsValid() ? lenarg : 0x10000 - addrarg.GetValue();

	uint8 prevflags = 0;
	uint32 rangeaddr = 0;
	if (len) {
		for(;;) {
			uint8 flags = 0;

			if (len)
				flags = heatmap.GetMemoryAccesses(addr);

			if (flags != prevflags) {
				if (prevflags) {
					uint32 end = addr;

					if (end <= rangeaddr)
						end += 0x10000;

					ATConsolePrintf("$%04X-%04X (%4.0fK) %s %s\n"
						, rangeaddr
						, (addr - 1) & 0xffff
						, (float)(end - rangeaddr) / 1024.0f
						, prevflags & ATCPUHeatMap::kAccessRead ? "read" : "    "
						, prevflags & ATCPUHeatMap::kAccessWrite ? " write" : "     ");
				}

				rangeaddr = addr;
				prevflags = flags;
			}

			if (!len)
				break;

			addr = (addr + 1) & 0xffff;
			--len;
		}
	}
}

void ATConsoleCmdHeatMapClear(ATDebuggerCmdParser& parser) {
	parser >> 0;

	if (!g_sim.IsHeatMapEnabled())
		throw MyError("Heat map is not enabled.\n");

	ATCPUHeatMap& heatmap = *g_sim.GetHeatMap();
	heatmap.Reset();

	ATConsoleWrite("Heat map reset.\n");
}

void ATDebuggerPrintHeatMapState(VDStringA& s, uint32 code) {
	switch(code & ATCPUHeatMap::kTypeMask) {
		case ATCPUHeatMap::kTypeUnknown:
		default:
			s = "Unknown";
			break;

		case ATCPUHeatMap::kTypePreset:
			s.sprintf("Preset from $%04X", code & 0xFFFF);
			break;

		case ATCPUHeatMap::kTypeImm:
			s.sprintf("Immediate from insn at $%04X", code & 0xFFFF);
			break;

		case ATCPUHeatMap::kTypeComputed:
			s.sprintf("Computed by insn at $%04X", code & 0xFFFF);
			break;

		case ATCPUHeatMap::kTypeHardware:
			s.sprintf("Hardware register at $%04X", code & 0xFFFF);
			break;
	}
}

void ATConsoleCmdHeatMapDumpMemory(ATDebuggerCmdParser& parser) {
	ATDebuggerCmdExprAddr addrarg(false, true);
	ATDebuggerCmdLength lenarg(8, false, &addrarg);
	parser >> addrarg >> lenarg >> 0;

	if (!g_sim.IsHeatMapEnabled())
		throw MyError("Heat map is not enabled.\n");

	ATCPUHeatMap& heatmap = *g_sim.GetHeatMap();

	uint32 addr = addrarg.GetValue();
	uint32 len = lenarg;
	VDStringA s;
	for(uint32 i=0; i<len; ++i) {
		uint32 addr2 = (addr + i) & 0xffff;

		ATDebuggerPrintHeatMapState(s, heatmap.GetMemoryStatus(addr2));
		uint8 validity = heatmap.GetMemoryValidity(addr2);
		uint8 flags = heatmap.GetMemoryAccesses(addr2);
		ATConsolePrintf("$%04X: %c%c | ~%02X | %s\n"
			, addr2
			, flags & ATCPUHeatMap::kAccessRead  ? 'R' : ' '
			, flags & ATCPUHeatMap::kAccessWrite ? 'W' : ' '
			, validity
			, s.c_str());
	}
}

void ATConsoleCmdHeatMapUninit(ATDebuggerCmdParser& parser) {
	ATDebuggerCmdExprAddr addrarg(false, true);
	ATDebuggerCmdLength lenarg(1, false, &addrarg);
	parser >> addrarg >> lenarg >> 0;

	if (!g_sim.IsHeatMapEnabled())
		throw MyError("Heat map is not enabled.\n");

	ATCPUHeatMap& heatmap = *g_sim.GetHeatMap();

	heatmap.ResetMemoryRange(addrarg.GetValue(), lenarg);
}

void ATConsoleCmdHeatMapPreset(ATDebuggerCmdParser& parser) {
	ATDebuggerCmdExprAddr addrarg(false, true);
	ATDebuggerCmdLength lenarg(1, false, &addrarg);
	parser >> addrarg >> lenarg >> 0;

	if (!g_sim.IsHeatMapEnabled())
		throw MyError("Heat map is not enabled.\n");

	ATCPUHeatMap& heatmap = *g_sim.GetHeatMap();

	heatmap.PresetMemoryRange(addrarg.GetValue(), lenarg);
}

void ATConsoleCmdHeatMapTrap(ATDebuggerCmdParser& parser) {
	if (!g_sim.IsHeatMapEnabled())
		throw MyError("Heat map is not enabled.\n");

	static const struct {
		const char *mpName;
		ATCPUHeatMapTrapFlags mFlag;
		const char *mpDesc;
	} kFlags[]={
		{ "load", kATCPUHeatMapTrapFlags_Load, "Load from uninited data" },
		{ "branch", kATCPUHeatMapTrapFlags_Branch, "Branch on result calculated from uninit-derived data" },
		{ "compute", kATCPUHeatMapTrapFlags_Compute, "Computation with uninit-derived data" },
		{ "ea", kATCPUHeatMapTrapFlags_EffectiveAddress, "Indexing with uninit-derived index" },
		{ "hwstore", kATCPUHeatMapTrapFlags_HwStore, "Store to hardware register with uninit-derived data" },
	};

	ATCPUHeatMap& heatmap = *g_sim.GetHeatMap();

	if (!parser.GetArgumentCount()) {
		const ATCPUHeatMapTrapFlags earlyFlags = heatmap.GetEarlyTrapFlags();
		const ATCPUHeatMapTrapFlags normalFlags = heatmap.GetNormalTrapFlags();

		ATConsoleWrite("Type    Early  Normal  Desc\n");
		ATConsoleWrite("------------------------------------------\n");
		for(const auto& flag : kFlags) {
			ATConsolePrintf("%-7s   %3s    %3s  %s\n"
				, flag.mpName
				, (earlyFlags & flag.mFlag) ? "on" : "off"
				, (normalFlags & flag.mFlag) ? "on" : "off"
				, flag.mpDesc
				);
		}

		return;
	}

	ATDebuggerCmdName nameArg(true);
	ATDebuggerCmdName modeArg(true);
	parser >> nameArg >> modeArg >> 0;

	ATCPUHeatMapTrapFlags trapFlag = (ATCPUHeatMapTrapFlags)0;
	const char *trapFlagName = nullptr;

	if (*nameArg == "*") {
		trapFlag = kATCPUHeatMapTrapFlags_All;
	} else {
		for(const auto& flag : kFlags) {
			if (*nameArg == flag.mpName) {
				trapFlag = flag.mFlag;
				trapFlagName = flag.mpName;
				break;
			}
		}

		if (!trapFlag)
			throw MyError("Unknown trap type '%s'.", nameArg->c_str());
	}
	
	bool enableEarly;
	bool enableLate;
	const char *newState;

	if (*modeArg == "off") {
		enableEarly = false;
		enableLate = false;
		newState = "disabled";
	} else if (*modeArg == "early") {
		enableEarly = true;
		enableLate = true;
		newState = "enabled early";
	} else if (*modeArg == "on") {
		enableEarly = false;
		enableLate = true;
		newState = "enabled";
	} else
		throw MyError("Unknown trap mode '%s'.", modeArg->c_str());

	if (enableEarly)
		heatmap.SetEarlyTrapFlags((ATCPUHeatMapTrapFlags)(heatmap.GetEarlyTrapFlags() | trapFlag));
	else
		heatmap.SetEarlyTrapFlags((ATCPUHeatMapTrapFlags)(heatmap.GetEarlyTrapFlags() & ~trapFlag));

	if (enableLate)
		heatmap.SetNormalTrapFlags((ATCPUHeatMapTrapFlags)(heatmap.GetNormalTrapFlags() | trapFlag));
	else
		heatmap.SetNormalTrapFlags((ATCPUHeatMapTrapFlags)(heatmap.GetNormalTrapFlags() & ~trapFlag));

	if (trapFlagName)
		ATConsolePrintf("Trap '%s' is now %s.\n", trapFlagName, newState);
	else
		ATConsolePrintf("All traps are now %s.\n", newState);
}

void ATConsoleCmdHeatMapEnable(ATDebuggerCmdParser& parser) {
	ATDebuggerCmdBool enable(true);
	parser >> enable >> 0;

	if (g_sim.IsHeatMapEnabled() != enable) {
		g_sim.SetHeatMapEnabled(enable);

		ATConsolePrintf("Heat map is now %s.\n", enable ? "enabled" : "disabled");
	}
}

void ATConsoleCmdHeatMapRegisters(ATDebuggerCmdParser& parser) {
	parser >> 0;

	if (!g_sim.IsHeatMapEnabled())
		throw MyError("Heat map is not enabled.\n");

	ATCPUHeatMap& heatmap = *g_sim.GetHeatMap();
	ATCPUEmulator& cpu = g_sim.GetCPU();

	VDStringA s;

	ATDebuggerPrintHeatMapState(s, heatmap.GetAStatus());
	ATConsolePrintf("A = $%02X ~%02X (%s)\n", cpu.GetA(), heatmap.GetAValidity(), s.c_str());

	ATDebuggerPrintHeatMapState(s, heatmap.GetXStatus());
	ATConsolePrintf("X = $%02X ~%02X (%s)\n", cpu.GetX(), heatmap.GetXValidity(), s.c_str());

	ATDebuggerPrintHeatMapState(s, heatmap.GetYStatus());
	ATConsolePrintf("Y = $%02X ~%02X (%s)\n", cpu.GetY(), heatmap.GetYValidity(), s.c_str());
	ATConsolePrintf("P = $%02X ~%02X\n", cpu.GetP(), heatmap.GetPValidity());
}

void ATConsoleCmdInputByte(ATDebuggerCmdParser& parser) {
	ATDebuggerCmdExprAddr addrArg(false, true);
	parser >> addrArg >> 0;

	IATDebugTarget *target = g_debugger.GetTarget();

	const uint8 v = target->ReadByte(addrArg.GetValue());

	ATConsolePrintf("Read %s = $%02X\n", g_debugger.GetAddressText(addrArg.GetValue(), true).c_str(), v);
}

void ATConsoleCmdSearchCommon(ATDebuggerCmdParser& parser, const vdfunction<void(ATDebuggerCmdParser&, vdfastvector<uint8>&)>& parsefn) {
	ATDebuggerCmdExprAddr addrarg(true, true);
	ATDebuggerCmdLength lenarg(0, true, &addrarg);

	parser >> addrarg >> lenarg;

	vdfastvector<uint8> buf;

	parsefn(parser, buf);

	parser >> 0;

	uint32 len = lenarg;
	uint32 addrspace = addrarg.GetValue() & kATAddressSpaceMask;
	uint32 addroffset = addrarg.GetValue() & kATAddressOffsetMask;

	const uint8 *const pat = buf.data();
	const uint32 patlen = (uint32)buf.size();
	uint32 patoff = 0;

	if (len < patlen)
		return;

	len -= (patlen - 1);

	IATDebugTarget *target = g_debugger.GetTarget();

	for(uint32 len = lenarg; len && !ATConsoleCheckBreak(); --len) {
		uint8 v = target->DebugReadByte(addrspace + ((addroffset + patoff) & kATAddressOffsetMask));

		if (v == pat[patoff]) {
			bool validMatch = true;

			for(uint32 i = patoff + 1; i < patlen; ++i) {
				uint8 v2 = target->DebugReadByte(addrspace + ((addroffset + i) & kATAddressOffsetMask));

				if (v2 != pat[i]) {
					validMatch = false;
					patoff = i;
					break;
				}
			}

			if (validMatch) {
				for(uint32 i = 0; i < patoff; ++i) {
					uint8 v3 = target->DebugReadByte(addrspace + ((addroffset + i) & kATAddressOffsetMask));

					if (v3 != pat[i]) {
						validMatch = false;
						patoff = i;
						break;
					}
				}

				if (validMatch)
					ATConsolePrintf("Match found at: %s\n", g_debugger.GetAddressText(addrspace + (addroffset & kATAddressOffsetMask), false).c_str());
			}
		}

		++addroffset;
	}
}

void ATConsoleCmdSearch(ATDebuggerCmdParser& parser) {
	ATConsoleCmdSearchCommon(parser,
		[](ATDebuggerCmdParser& parser, vdfastvector<uint8>& buf) {
			ATDebuggerCmdExprNum val(true, true, 0, 255);
			parser >> val;
			uint8 c = val.GetValue();

			for(;;) {
				buf.push_back(c);

				ATDebuggerCmdExprNum val(false, true, 0, 255);
				parser >> val;

				if (!val.IsValid())
					break;

				c = val.GetValue();
			}
		});
}

void ATConsoleCmdSearchWord(ATDebuggerCmdParser& parser) {
	ATConsoleCmdSearchCommon(parser,
		[](ATDebuggerCmdParser& parser, vdfastvector<uint8>& buf) {
			ATDebuggerCmdExprNum val(true, true, 0, 65535);
			parser >> val;
			uint32 c = val.GetValue();

			for(;;) {
				buf.push_back((uint8)c);
				buf.push_back((uint8)(c >> 8));

				ATDebuggerCmdExprNum val(false, true, 0, 65535);
				parser >> val;

				if (!val.IsValid())
					break;

				c = val.GetValue();
			}
		});
}

void ATConsoleCmdSearchATASCII(ATDebuggerCmdParser& parser) {
	ATConsoleCmdSearchCommon(parser,
		[](ATDebuggerCmdParser& parser, vdfastvector<uint8>& buf) {
			ATDebuggerCmdQuotedString str(true);
			parser >> str;

			for(char c : str.GetValue()) {
				buf.push_back((uint8)(unsigned char)c);
			}
		});
}

void ATConsoleCmdSearchINTERNAL(ATDebuggerCmdParser& parser) {
	ATConsoleCmdSearchCommon(parser,
		[](ATDebuggerCmdParser& parser, vdfastvector<uint8>& buf) {
			ATDebuggerCmdQuotedString str(true);
			parser >> str;

			static const char kTranslationTable[4] = { 0x40, 0x20, 0x60, 0x00 };

			for(char c : str.GetValue()) {
				uint8 v = (uint8)(unsigned char)c;

				v ^= kTranslationTable[(v >> 5) & 3];

				buf.push_back(v);
			}
		});
}

class ATDebuggerCmdStaticTrace : public vdrefcounted<IATDebuggerActiveCommand> {
public:
	ATDebuggerCmdStaticTrace(uint32 initialpc, uint32 rangelo, uint32 rangehi);
	virtual bool IsBusy() const { return true; }
	virtual const char *GetPrompt() { return NULL; }

	virtual void BeginCommand(IATDebugger *debugger);
	virtual void EndCommand();
	virtual bool ProcessSubCommand(const char *s);

protected:
	enum {
		kFlagTraced = 0x01,
		kFlagLabeled = 0x02
	};

	uint32 mRangeLo;
	uint32 mRangeHi;

	vdfastdeque<uint32> mPCQueue;
	VDStringA mSymName;

	uint8 mSeenFlags[65536];
};

ATDebuggerCmdStaticTrace::ATDebuggerCmdStaticTrace(uint32 initialpc, uint32 rangelo, uint32 rangehi)
	: mRangeLo(rangelo)
	, mRangeHi(rangehi)
{
	memset(mSeenFlags, 0, sizeof mSeenFlags);
	mPCQueue.push_back(initialpc);
}

void ATDebuggerCmdStaticTrace::BeginCommand(IATDebugger *debugger) {
}

void ATDebuggerCmdStaticTrace::EndCommand() {
}

bool ATDebuggerCmdStaticTrace::ProcessSubCommand(const char *) {
	if (mPCQueue.empty())
		return false;

	IATDebugTarget *dbgtarget = g_debugger.GetTarget();

	const auto readByte = [dbgtarget](uint16 addr) { return dbgtarget->DebugReadByte(addr); };
	const auto readWord = [dbgtarget](uint16 addr) {
		return (uint16)(dbgtarget->DebugReadByte(addr) + ((uint32)dbgtarget->DebugReadByte((addr + 1) & 0xffff) << 8));
	};

	uint32 pc = mPCQueue.front();
	mPCQueue.pop_front();

	ATConsolePrintf("Tracing $%04X\n", pc);

	while(!(mSeenFlags[pc] & kFlagTraced)) {
		mSeenFlags[pc] |= kFlagTraced;

		uint8 opcode = readByte(pc);
		uint32 len = ATGetOpcodeLength(opcode);

		pc += len;
		pc &= 0xffff;

		// check for interesting opcodes
		sint32 target = -1;
		bool stop_trace = false;

		switch(opcode) {
			case 0x00:		// BRK
			case 0x40:		// RTI
			case 0x60:		// RTS
			case 0x6C:		// JMP (abs)
				stop_trace = true;
				break;

			case 0x4C:		// JMP abs
				target = readWord((pc - 2) & 0xffff);
				stop_trace = true;
				break;

			case 0x20:		// JSR abs
				target = readWord((pc - 2) & 0xffff);
				break;

			case 0x10:		// branches
			case 0x30:
			case 0x50:
			case 0x70:
			case 0x90:
			case 0xb0:
			case 0xd0:
			case 0xf0:
				target = (pc + (sint8)readByte((pc - 1) & 0xffff)) & 0xffff;
				break;
		}

		if (target >= 0 && (uint32)target >= mRangeLo && (uint32)target <= mRangeHi && !(mSeenFlags[target] & kFlagLabeled)) {
			mSeenFlags[target] |= kFlagLabeled;

			ATSymbol sym;
			if (!g_debugger.LookupSymbol(target, kATSymbol_Any, sym)) {
				mSymName.sprintf("L%04X", target);
				g_debugger.AddCustomSymbol(target, 1, mSymName.c_str(), kATSymbol_Execute);

				if (!(mSeenFlags[target] & kFlagTraced))
					mPCQueue.push_back(target);
			}
		}

		if (stop_trace)
			break;
	}

	return true;
}

void ATConsoleCmdStaticTrace(ATDebuggerCmdParser& parser) {
	ATDebuggerCmdSwitch markArg("m", false);
	ATDebuggerCmdExprAddr baseAddrArg(false, true);
	ATDebuggerCmdExprAddr restrictBase(false, false);
	ATDebuggerCmdLength restrictLength(false, true, &restrictBase);

	parser >> markArg >> baseAddrArg;

	parser >> restrictBase;
	if (restrictBase.IsValid())
		parser >> restrictLength;

	parser >> 0;

	uint32 rangeLo = 0;
	uint32 rangeHi = 0xFFFF;
	
	if (restrictBase.IsValid()) {
		rangeLo = restrictBase.GetValue();
		rangeHi = rangeLo + restrictLength;
	}

	const uint32 baseAddr = baseAddrArg.GetValue();

	if (markArg) {
		ATSymbol sym;
		if (!g_debugger.LookupSymbol(baseAddr, kATSymbol_Any, sym)) {
			VDStringA symName;
			symName.sprintf("L%04X", baseAddr);
			g_debugger.AddCustomSymbol(baseAddr, 1, symName.c_str(), kATSymbol_Execute);
		}
	}

	vdrefptr<IATDebuggerActiveCommand> acmd(new ATDebuggerCmdStaticTrace(baseAddr, rangeLo, rangeHi));

	g_debugger.StartActiveCommand(acmd);
}

void ATConsoleCmdDumpDisplayList(ATDebuggerCmdParser& parser) {
	ATDebuggerCmdExprAddr addrArg(false, false);
	ATDebuggerCmdSwitch noCollapseArg("n", false);

	parser >> noCollapseArg >> addrArg >> 0;

	uint16 addr = addrArg.IsValid() ? addrArg.GetValue() : g_sim.GetAntic().GetDisplayListPointer();

	VDStringA line;

	uint16 regionBase = (addr & 0xfc00);
	for(int i=0; i<500; ++i) {
		uint16 baseaddr = regionBase + (addr & 0x3ff);
		uint8 b = g_sim.DebugAnticReadByte(regionBase + (addr++ & 0x3ff));

		int count = 1;
		uint32 jumpAddr;

		if (((b & 0x40) && (b & 0x0f)) || (b & 15) == 1) {
			uint8 arg0 = g_sim.DebugAnticReadByte(regionBase + (addr++ & 0x3ff));
			uint8 arg1 = g_sim.DebugAnticReadByte(regionBase + (addr++ & 0x3ff));

			jumpAddr = arg0 + ((uint32)arg1 << 8);

			if ((b & 15) != 1 && !noCollapseArg) {
				while(i < 500) {
					if (g_sim.DebugAnticReadByte(regionBase + (addr & 0x3ff)) != b)
						break;

					if (g_sim.DebugAnticReadByte(regionBase + ((addr + 1) & 0x3ff)) != arg0)
						break;

					if (g_sim.DebugAnticReadByte(regionBase + ((addr + 2) & 0x3ff)) != arg1)
						break;

					++count;
					++i;
					addr += 3;
				}
			}
		} else if (!noCollapseArg) {
			while(i < 500 && g_sim.DebugAnticReadByte(regionBase + (addr & 0x3ff)) == b) {
				++count;
				++i;
				++addr;
			}
		}

		line.sprintf("  %04X: ", baseaddr);

		if (!noCollapseArg) {
			if (count > 1)
				line.append_sprintf("x%-3u ", count);
			else
				line += "     ";
		}

		switch(b & 15) {
			case 0:
				line.append_sprintf("blank%s %d", b&128 ? ".i" : "", ((b >> 4) & 7) + 1);
				break;
			case 1:
				if (b & 64) {
					line.append_sprintf("waitvbl%s %04X", b&128 ? ".i" : "", jumpAddr);
					line += '\n';
					ATConsoleWrite(line.c_str());
					return;
				} else {
					line.append_sprintf("jump%s%s %04X"
						, b&128 ? ".i" : ""
						, b&32 ? ".v" : ""
						, jumpAddr);

					regionBase = jumpAddr & 0xfc00;
					addr = jumpAddr;
				}
				break;
			default:
				line.append_sprintf("mode%s%s%s %X"
					, b&128 ? ".i" : ""
					, b&32 ? ".v" : ""
					, b&16 ? ".h" : ""
					, b&15);

				if (b & 64)
					line.append_sprintf(" @ %04X", jumpAddr);
				break;
		}

		line += '\n';
		ATConsoleWrite(line.c_str());
	}

	ATConsoleWrite("(display list too long)\n");
}

void ATConsoleCmdDumpDLHistory(ATDebuggerCmdParser& parser) {
	parser >> 0;

	const ATAnticEmulator::DLHistoryEntry *history = g_sim.GetAntic().GetDLHistory();

	ATConsolePrintf("Ycoord DLIP PFAD H V DMACTL MODE\n");
	ATConsolePrintf("--------------------------------\n");

	for(int y=0; y<262; ++y) {
		const ATAnticEmulator::DLHistoryEntry& hval = history[y];

		if (!hval.mbValid)
			continue;

		ATConsolePrintf("  %3d: %04x %04x %x %x   %02x   %02x\n"
			, y
			, hval.mDLAddress
			, hval.mPFAddress
			, hval.mHVScroll & 15
			, hval.mHVScroll >> 4
			, hval.mDMACTL
			, hval.mControl
			);
	}
}

void ATConsoleCmdDumpHistory(ATDebuggerCmdParser& parser) {
	ATDebuggerCmdSwitch switchI("i", false);
	ATDebuggerCmdSwitch switchC("c", false);
	ATDebuggerCmdSwitch switchJ("j", false);
	ATDebuggerCmdSwitchNumArg switchS("s", 0, 0x7FFFFFFF);
	ATDebuggerCmdNumber histLenArg(false, 0, 0x7FFFFFFF);
	ATDebuggerCmdName wildArg(false);

	parser >> switchI >> switchC >> switchJ >> switchS >> histLenArg >> wildArg;

	int histlen = 32;
	const char *wild = NULL;
	bool compressed = switchC;
	bool interruptsOnly = switchI;
	const bool jumpsOnly = switchJ;
	int histstart = -1;

	if (switchS.IsValid()) {
		compressed = true;
		histstart = switchS.GetValue();
	}

	if (histLenArg.IsValid()) {
		histlen = histLenArg.GetValue();

		if (wildArg.IsValid())
			wild = wildArg->c_str();
	}

	const ATCPUEmulator& cpu = g_sim.GetCPU();
	const ATDebugDisasmMode disasmMode = cpu.GetDisasmMode();
	IATDebugTarget *const target = g_debugger.GetTarget();

	uint16 predictor[4] = {0,0,0,0};
	int predictLen = 0;

	if (histstart < 0)
		histstart = histlen - 1;

	int histend = histstart - histlen + 1;
	if (histend < 0)
		histend = 0;

	uint16 nmi = g_sim.DebugReadWord(0xFFFA);
	uint16 irq = g_sim.DebugReadWord(0xFFFE);

	// Note that we are forcing target 0 here for now.
	const auto& tsdecoder = g_sim.GetTimestampDecoder();
	IATDebugTargetHistory *thist = vdpoly_cast<IATDebugTargetHistory *>(g_sim.GetDebugTarget());

	VDStringA buf;
	for(int i=histstart; i >= histend; --i) {
		const ATCPUHistoryEntry& he = cpu.GetHistory(i);
		uint16 pc = he.mPC;

		if (compressed) {
			if (pc == predictor[0] || pc == predictor[1] || pc == predictor[2] || pc == predictor[3])
				++predictLen;
			else {
				if (predictLen > 4)
					ATConsolePrintf("[%d lines omitted]\n", predictLen - 4);
				predictLen = 0;
			}

			predictor[i & 3] = pc;

			if (predictLen > 4)
				continue;
		}

		if (interruptsOnly && pc != nmi && pc != irq)
			continue;

		if (jumpsOnly) {
			bool branch = false;

			switch(he.mOpcode[0]) {
				case 0x10:	// BPL
				case 0x30:	// BMI
				case 0x50:	// BVC
				case 0x70:	// BVS
				case 0x90:	// BCC
				case 0xB0:	// BCS
				case 0xD0:	// BNE
				case 0xF0:	// BEQ
				case 0x20:	// JSR abs
				case 0x4C:	// JMP abs
				case 0x6C:	// JMP (abs)
					branch = true;
					break;

				// 65C02/65C816
				case 0x7C:	// JMP (abs,X)
				case 0x80:	// BRA rel
					if (disasmMode != kATDebugDisasmMode_6502)
						branch = true;
					break;

				// 65C02 only
				case 0x07:	// RMBn
				case 0x17:
				case 0x27:
				case 0x37:
				case 0x47:
				case 0x57:
				case 0x67:
				case 0x77:
				case 0x87:	// SMBn
				case 0x97:
				case 0xA7:
				case 0xB7:
				case 0xC7:
				case 0xD7:
				case 0xE7:
				case 0xF7:
				case 0x0F:	// BBRn
				case 0x1F:
				case 0x2F:
				case 0x3F:
				case 0x4F:
				case 0x5F:
				case 0x6F:
				case 0x7F:
				case 0x8F:	// BBSn
				case 0x9F:
				case 0xAF:
				case 0xBF:
				case 0xCF:
				case 0xDF:
				case 0xEF:
				case 0xFF:
					if (disasmMode == kATDebugDisasmMode_65C02)
						branch = true;
					break;

				// 65C816 only
				case 0x22:	// JSL long
				case 0x5C:	// JML long
				case 0x82:	// BRL rel16
				case 0xDC:	// JML [abs]
				case 0xFC:	// JSR (abs,X)
					if (disasmMode == kATDebugDisasmMode_65C816)
						branch = true;
					break;
			}

			if (!branch)
				continue;
		}

		uint32 rawts = thist->ConvertRawTimestamp(he.mCycle);
		const auto& beamPos = tsdecoder.GetBeamPosition(rawts);

		buf.sprintf("%7d) T=%05d|%3d,%3d A=%02X X=%02X Y=%02X S=%02X P=%02X (%c%c%c%c%c%c) "
			, i
			, beamPos.mFrame
			, beamPos.mY
			, beamPos.mX
			, he.mA, he.mX, he.mY, he.mS, he.mP
			, he.mP & 0x80 ? 'N' : ' '
			, he.mP & 0x40 ? 'V' : ' '
			, he.mP & 0x08 ? 'D' : ' '
			, he.mP & 0x04 ? 'I' : ' '
			, he.mP & 0x02 ? 'Z' : ' '
			, he.mP & 0x01 ? 'C' : ' '
			);

		if (he.mbIRQ && he.mbNMI)
			buf.append_sprintf("%04X: -- High level emulation --", he.mPC);
		else
			ATDisassembleInsn(buf, target, disasmMode, he, true, false, true, true, true);

		if (wild && !VDFileWildMatch(wild, buf.c_str()))
			continue;

		buf += '\n';

		ATConsoleWrite(buf.c_str());
	}

	if (predictLen > 4)
		ATConsolePrintf("[%d lines omitted]\n", predictLen - 4);
}

void ATConsoleCmdDumpDsm(ATDebuggerCmdParser& parser) {
	ATDebuggerCmdString filename(true);
	ATDebuggerCmdExprAddr addrArg(false, true);
	ATDebuggerCmdLength lenArg(0, true, &addrArg);
	ATDebuggerCmdSwitch codeBytesArg("c", false);
	ATDebuggerCmdSwitch pcAddrArg("p", false);
	ATDebuggerCmdSwitch noLabelsArg("n", false);
	ATDebuggerCmdSwitch lcopsArg("l", false);
	ATDebuggerCmdSwitch sepArg("s", false);
	ATDebuggerCmdSwitch tabsArg("t", false);

	parser >> codeBytesArg >> pcAddrArg >> noLabelsArg >> lcopsArg >> sepArg >> tabsArg >> filename >> addrArg >> lenArg >> 0;

	uint32 addr = addrArg.GetValue();
	uint32 addrEnd = addr + lenArg;

	if (addrEnd > 0x1000000)
		addrEnd = 0x1000000;

	FILE *f = fopen(filename->c_str(), "w");
	if (!f) {
		ATConsolePrintf("Unable to open file for write: %s\n", filename->c_str());
		return;
	}

	const bool showLabels = !noLabelsArg;
	const bool showCodeBytes = codeBytesArg;
	const bool showPCAddress = pcAddrArg;
	const bool lowercaseOps = lcopsArg;
	const bool separateRoutines = sepArg;
	const bool useTabs = tabsArg;

	VDStringA s;
	VDStringA t;
	ATCPUHistoryEntry hent;
	IATDebugTarget *const target = g_debugger.GetTarget();
	ATDisassembleCaptureRegisterContext(target, hent);

	const ATDebugDisasmMode disasmMode = target->GetDisasmMode();

	uint32 pc = addr;
	while(pc < addrEnd) {	
		ATDisassembleCaptureInsnContext(target, (uint16)pc, (uint8)(pc >> 16), hent);

		s.clear();
		uint32 pc2 = ATDisassembleInsn(s, target, disasmMode, hent, false, false, showPCAddress, showCodeBytes, showLabels, lowercaseOps, useTabs);
		pc2 += (pc & 0xff0000);

		while(!s.empty() && s.back() == ' ')
			s.pop_back();

		s += '\n';

		if (useTabs) {
			// detabify
			t.clear();

			int pos = 0;
			int spaces = 0;
			for(VDStringA::const_iterator it(s.begin()), itEnd(s.end()); it != itEnd; ++it, ++pos) {
				const char c = *it;

				if (c == ' ') {
					++spaces;
					continue;
				}

				if (spaces) {
					if (spaces > 1) {
						// recover start of space run
						int basepos = pos - spaces;

						// compute last tabstop at or prior to end
						int lasttab = pos & ~3;

						// computer number of spaces between last tab stop and end position
						int postspaces = pos - std::max<int>(basepos, lasttab);

						// compute number of tabs between last tabstop and start (may be negative)
						int tabs = (lasttab - basepos + 3) >> 2;

						while(tabs-- > 0)
							t += '\t';

						while(postspaces-- > 0)
							t += ' ';
					} else
						t += ' ';

					spaces = 0;
				}

				t += c;
			}

			s.swap(t);
		}

		fwrite(s.data(), s.size(), 1, f);

		if (separateRoutines) {
			switch(hent.mOpcode[0]) {
				case 0x40:	// RTI
				case 0x60:	// RTS
				case 0x4C:	// JMP abs
				case 0x6C:	// JMP (abs)
					fputc('\n', f);
					break;
			}
		}

		// check if we wrapped around
		if (pc2 < pc)
			break;

		pc = pc2;
	}

	fclose(f);

	ATConsolePrintf("Disassembled %04X-%04X to %s\n", addr, addrEnd-1, filename->c_str());
}

void ATConsoleCmdRapidus(ATDebuggerCmdParser& parser) {
	parser >> 0;

	IATDevice *sid = g_sim.GetDeviceManager()->GetDeviceByTag("rapidus");

	if (!sid) {
		ATConsoleWrite("Rapidus is not active.\n");
		return;
	}

	ATDebuggerConsoleOutput conout;
	if (auto *p = vdpoly_cast<IATDeviceDiagnostics *>(sid))
		p->DumpStatus(conout);
}

void ATConsoleCmdReadMem(ATDebuggerCmdParser& parser) {
	ATDebuggerCmdExprAddr addressArg(true, true);
	ATDebuggerCmdLength lengthArg(1, false, &addressArg);
	ATDebuggerCmdPath filename(true);
	parser >> filename >> addressArg >> lengthArg >> 0;

	uint32 addr = addressArg.GetValue();
	uint32 len = lengthArg.IsValid() ? (uint32)lengthArg : 0x0FFFFFFFU;

	const uint32 limit = (addr & kATAddressSpaceMask) + ATAddressGetSpaceSize(addr);

	if (addr >= limit)
		throw MyError("Invalid start address: %s\n", g_debugger.GetAddressText(addr, false).c_str());

	if (len > limit - addr)
		len = limit - addr;

	FILE *f = fopen(filename->c_str(), "rb");
	if (!f) {
		ATConsolePrintf("Unable to open file for read: %s\n", filename->c_str());
		return;
	}

	IATDebugTarget *target = g_debugger.GetTarget();

	uint8 buf[256];

	uint32 ptr = addr;
	uint32 remaining = len;
	uint32 actual = 0;
	while(remaining) {
		const uint32 tc = remaining > 256 ? 256 : remaining;
		int readlen = (int)fread(buf, 1, tc, f);

		if (readlen <= 0)
			break;

		uint32 ureadlen = (uint32)readlen;
		target->WriteMemory(ptr, buf, ureadlen);
		ptr += ureadlen;
		remaining -= ureadlen;
		actual += ureadlen;
	}

	fclose(f);

	ATConsolePrintf("Read %s-%s from %s\n"
		, g_debugger.GetAddressText(addr, false).c_str()
		, g_debugger.GetAddressText(addr + actual - 1, false).c_str()
		, filename->c_str());
}

void ATConsoleCmdWriteMem(ATDebuggerCmdParser& parser) {
	ATDebuggerCmdExprAddr addressArg(true, true);
	ATDebuggerCmdLength lengthArg(1, true, &addressArg);
	ATDebuggerCmdPath filename(true);
	parser >> filename >> addressArg >> lengthArg >> 0;

	uint32 addr = addressArg.GetValue();
	uint32 len = lengthArg;

	const uint32 limit = (addr & kATAddressSpaceMask) + ATAddressGetSpaceSize(addr);
	if (addr >= limit)
		throw MyError("Invalid start address: %s\n", g_debugger.GetAddressText(addr, false).c_str());

	if (len > limit - addr)
		len = limit - addr;

	FILE *f = fopen(filename->c_str(), "wb");
	if (!f) {
		ATConsolePrintf("Unable to open file for write: %s\n", filename->c_str());
		return;
	}

	IATDebugTarget *target = g_debugger.GetTarget();

	uint8 buf[256];
	uint32 ptr = addr;
	uint32 remaining = len;
	while(remaining) {
		const uint32 tc = remaining > 256 ? 256 : remaining;

		target->DebugReadMemory(ptr, buf, tc);
		fwrite(buf, tc, 1, f);

		remaining -= tc;
		ptr += tc;
	}

	fclose(f);

	ATConsolePrintf("Wrote %s-%s to %s\n"
		, g_debugger.GetAddressText(addr, false).c_str()
		, g_debugger.GetAddressText(addr + len - 1, false).c_str()
		, filename->c_str());
}

void ATConsoleCmdLoadSymbols(ATDebuggerCmdParser& parser) {
	ATDebuggerCmdPath pathArg(false);

	parser >> pathArg >> 0;

	if (pathArg.IsValid()) {
		uint32 idx = ATGetDebugger()->LoadSymbols(VDTextAToW(pathArg->c_str()).c_str(), true, nullptr, true);

		if (idx)
			ATConsolePrintf("Loaded symbol file %s.\n", pathArg->c_str());
	} else {
		ATGetDebugger()->LoadAllDeferredSymbols();
	}
}

void ATConsoleCmdUnloadSymbols(ATDebuggerCmdParser& parser) {
	ATDebuggerCmdName nameArg(true);

	parser >> nameArg >> 0;

	const char *s = nameArg->c_str();
	char *t = (char *)s;
	unsigned long id;
	
	uint32 modId = g_debugger.GetCustomModuleIdByShortName(s);
	if (modId)
		g_debugger.UnloadSymbols(modId);
	else {
		id = strtoul(s, &t, 0);

		if (*t || !id)
			throw MyError("Invalid index: %s\n", s);

		g_debugger.UnloadSymbols(id);
	}
}

void ATConsoleCmdAntic(ATDebuggerCmdParser& parser) {
	parser >> 0;

	g_sim.GetAntic().DumpStatus();
}

void ATConsoleCmdBank(ATDebuggerCmdParser& parser) {
	parser >> 0;

	uint8 portb = g_sim.GetBankRegister();
	ATConsolePrintf("Bank state: %02X\n", portb);

	ATMemoryMapState state;
	g_sim.GetMMU()->GetMemoryMapState(state);

	ATConsolePrintf("  Kernel ROM:    %s\n", state.mbKernelEnabled ? "enabled" : "disabled");
	ATConsolePrintf("  BASIC ROM:     %s\n", state.mbBASICEnabled ? "enabled" : "disabled");
	ATConsolePrintf("  CPU bank:      %s\n", state.mbExtendedCPU ? "enabled" : "disabled");
	ATConsolePrintf("  Antic bank:    %s\n", state.mbExtendedANTIC ? "enabled" : "disabled");
	ATConsolePrintf("  Self test ROM: %s\n", state.mbSelfTestEnabled ? "enabled" : "disabled");

	ATMMUEmulator *mmu = g_sim.GetMMU();
	ATConsolePrintf("Antic bank: $%06X\n", mmu->GetAnticBankBase());
	ATConsolePrintf("CPU bank:   $%06X\n", mmu->GetCPUBankBase());

	if (state.mAxlonBankMask)
		ATConsolePrintf("Axlon bank: $%02X ($%05X)\n", state.mAxlonBank, (uint32)state.mAxlonBank << 14);

	for(uint32 cartUnit = 0; cartUnit < 2; ++cartUnit) {
		int cartBank = g_sim.GetCartBank(cartUnit);

		if (cartBank >= 0)
			ATConsolePrintf("Cartridge %u bank: $%02X ($%06X)\n", cartUnit + 1, cartBank, cartBank << 13);
		else
			ATConsolePrintf("Cartridge %u bank: disabled\n", cartUnit + 1);
	}
}

void ATConsoleCmdTraceCIO(ATDebuggerCmdParser& parser) {
	ATDebuggerCmdName nameArg(false);

	parser >> nameArg >> 0;

	if (!nameArg.IsValid()) {
		ATConsolePrintf("CIO call tracing is currently %s.\n", g_debugger.IsCIOTracingEnabled() ? "on" : "off");
		return;
	}

	bool newState = false;
	if (!vdstricmp(nameArg->c_str(), "on")) {
		newState = true;
	} else if (vdstricmp(nameArg->c_str(), "off")) {
		ATConsoleWrite("Syntax: .tracecio on|off\n");
		return;
	}

	g_debugger.SetCIOTracingEnabled(newState);
	ATConsolePrintf("CIO call tracing is now %s.\n", newState ? "on" : "off");
}

void ATConsoleCmdTraceSIO(ATDebuggerCmdParser& parser) {
	ATDebuggerCmdName nameArg(false);

	parser >> nameArg >> 0;

	if (!nameArg.IsValid()) {
		ATConsolePrintf("SIO call tracing is currently %s.\n", g_debugger.IsSIOTracingEnabled() ? "on" : "off");
		return;
	}

	bool newState = false;
	if (!vdstricmp(nameArg->c_str(), "on")) {
		newState = true;
	} else if (vdstricmp(nameArg->c_str(), "off")) {
		ATConsoleWrite("Syntax: .tracesio on|off\n");
		return;
	}

	g_debugger.SetSIOTracingEnabled(newState);
	ATConsolePrintf("SIO call tracing is now %s.\n", newState ? "on" : "off");
}

void ATConsoleCmdTraceSer(ATDebuggerCmdParser& parser) {
	ATDebuggerCmdName nameArg(false);

	parser >> nameArg >> 0;

	ATPokeyEmulator& pokey = g_sim.GetPokey();
	if (!nameArg.IsValid()) {
		ATConsolePrintf("Serial I/O tracing is currently %s.\n", pokey.IsTraceSIOEnabled() ? "on" : "off");
		return;
	}

	bool newState = false;
	if (!vdstricmp(nameArg->c_str(), "on")) {
		newState = true;
	} else if (vdstricmp(nameArg->c_str(), "off")) {
		ATConsoleWrite("Syntax: .traceser on|off\n");
		return;
	}

	pokey.SetTraceSIOEnabled(newState);
	ATConsolePrintf("Serial I/O call tracing is now %s.\n", newState ? "on" : "off");
}

void ATConsoleCmdVectors(ATDebuggerCmdParser& parser) {
	parser >> 0;

	IATDebugTarget& target = *g_debugger.GetTarget();

	if (!g_debugger.GetTargetIndex()) {
		ATKernelDatabase kdb(&g_sim.GetCPUMemory());

		if (g_sim.GetHardwareMode() == kATHardwareMode_5200) {
			ATKernelDatabase5200 kdb5(&g_sim.GetCPUMemory());

			ATConsolePrintf("NMI vectors:\n");
			ATConsolePrintf("VDSLST  Display list NMI              %04X\n", (uint16)kdb5.VDSLST);
			ATConsolePrintf("VVBLKI  Vertical blank immediate      %04X\n", (uint16)kdb5.VVBLKI);
			ATConsolePrintf("VVBLKD  Vertical blank deferred       %04X\n", (uint16)kdb5.VVBLKD);
			ATConsolePrintf("\n");
			ATConsolePrintf("IRQ vectors:\n");
			ATConsolePrintf("VIMIRQ  IRQ immediate                 %04X\n", (uint16)kdb5.VIMIRQ);
			ATConsolePrintf("VKYBDI  Keyboard immediate            %04X\n", (uint16)kdb5.VKYBDI);
			ATConsolePrintf("VKYBDF  Keyboard deferred             %04X\n", (uint16)kdb5.VKYBDF);
			ATConsolePrintf("VTRIGR  Controller trigger            %04X\n", (uint16)kdb5.VTRIGR);
			ATConsolePrintf("VBRKOP  Break instruction             %04X\n", (uint16)kdb5.VBRKOP);
			ATConsolePrintf("VSERIN  Serial I/O receive ready      %04X\n", (uint16)kdb5.VSERIN);
			ATConsolePrintf("VSEROR  Serial I/O transmit ready     %04X\n", (uint16)kdb5.VSEROR);
			ATConsolePrintf("VSEROC  Serial I/O transmit complete  %04X\n", (uint16)kdb5.VSEROC);
			ATConsolePrintf("VTIMR1  POKEY timer 1                 %04X\n", (uint16)kdb5.VTIMR1);
			ATConsolePrintf("VTIMR2  POKEY timer 2                 %04X\n", (uint16)kdb5.VTIMR2);
			ATConsolePrintf("VTIMR4  POKEY timer 4                 %04X\n", (uint16)kdb5.VTIMR4);
		} else {
			ATConsolePrintf("NMI vectors:\n");
			ATConsolePrintf("VDSLST  Display list NMI              %04X\n", (uint16)kdb.VDSLST);
			ATConsolePrintf("VVBLKI  Vertical blank immediate      %04X\n", (uint16)kdb.VVBLKI);
			ATConsolePrintf("VVBLKD  Vertical blank deferred       %04X\n", (uint16)kdb.VVBLKD);
			ATConsolePrintf("\n");
			ATConsolePrintf("IRQ vectors:\n");
			ATConsolePrintf("VIMIRQ  IRQ immediate                 %04X\n", (uint16)kdb.VIMIRQ);
			ATConsolePrintf("VKEYBD  Keyboard                      %04X\n", (uint16)kdb.VKEYBD);
			ATConsolePrintf("VSERIN  Serial I/O receive ready      %04X\n", (uint16)kdb.VSERIN);
			ATConsolePrintf("VSEROR  Serial I/O transmit ready     %04X\n", (uint16)kdb.VSEROR);
			ATConsolePrintf("VSEROC  Serial I/O transmit complete  %04X\n", (uint16)kdb.VSEROC);
			ATConsolePrintf("VPRCED  Serial I/O proceed            %04X\n", (uint16)kdb.VPRCED);
			ATConsolePrintf("VINTER  Serial I/O interrupt          %04X\n", (uint16)kdb.VINTER);
			ATConsolePrintf("VBREAK  Break instruction             %04X\n", (uint16)kdb.VBREAK);
			ATConsolePrintf("VTIMR1  POKEY timer 1                 %04X\n", (uint16)kdb.VTIMR1);
			ATConsolePrintf("VTIMR2  POKEY timer 2                 %04X\n", (uint16)kdb.VTIMR2);
			ATConsolePrintf("VTIMR4  POKEY timer 4                 %04X\n", (uint16)kdb.VTIMR4);
			ATConsolePrintf("VPIRQ   PBI device interrupt          %04X\n", (uint16)kdb.VPIRQ);
		}

		ATConsolePrintf("\n");
	}

	if (target.GetDisasmMode() == kATDebugDisasmMode_65C816) {
		uint16_t natvecs[9];
		target.DebugReadMemory(0xFFE4, natvecs, sizeof natvecs);

		ATConsolePrintf("Native COP     %04X\n", VDFromLE16(natvecs[0]));		// FFE4
		ATConsolePrintf("Native BRK     %04X\n", VDFromLE16(natvecs[1]));		// FFE6
		ATConsolePrintf("Native ABORT   %04X\n", VDFromLE16(natvecs[2]));		// FFE8
		ATConsolePrintf("Native NMI     %04X\n", VDFromLE16(natvecs[3]));		// FFEA
		ATConsolePrintf("Native IRQ     %04X\n", VDFromLE16(natvecs[5]));		// FFEE
		ATConsolePrintf("COP            %04X\n", VDFromLE16(natvecs[8]));		// FFF4
	}

	uint16_t emuvecs[3];
	target.DebugReadMemory(0xFFFA, emuvecs, sizeof emuvecs);

	ATConsolePrintf("NMI            %04X\n", VDFromLE16(emuvecs[0]));
	ATConsolePrintf("Reset          %04X\n", VDFromLE16(emuvecs[1]));
	ATConsolePrintf("IRQ            %04X\n", VDFromLE16(emuvecs[2]));
}

void ATConsoleCmdGTIA(ATDebuggerCmdParser& parser) {
	parser >> 0;

	g_sim.GetGTIA().DumpStatus();
}

void ATConsoleCmdPokey(ATDebuggerCmdParser& parser) {
	parser >> 0;

	g_sim.GetPokey().DumpStatus();
}

void ATConsoleCmdColdReset(ATDebuggerCmdParser& parser) {
	parser >> 0;

	g_sim.ColdReset();
}

void ATConsoleCmdWarmReset(ATDebuggerCmdParser& parser) {
	parser >> 0;

	g_sim.WarmReset();
}

void ATConsoleCmdBeam(ATDebuggerCmdParser& parser) {
	parser >> 0;

	ATAnticEmulator& antic = g_sim.GetAntic();
	ATConsolePrintf("Antic position: %d,%d\n", antic.GetBeamX(), antic.GetBeamY()); 
}

void ATConsoleCmdPathRecord(ATDebuggerCmdParser& parser) {
	ATDebuggerCmdBool mode(false);
	parser >> mode >> 0;

	ATCPUEmulator& cpu = g_sim.GetCPU();
	if (mode.IsValid())
		cpu.SetPathfindingEnabled(mode);

	ATConsolePrintf("CPU path recording is now %s.\n", cpu.IsPathfindingEnabled() ? "on" : "off");
}

void ATConsoleCmdPathReset(ATDebuggerCmdParser& parser) {
	parser >> 0;

	g_sim.GetCPU().ResetAllPaths();
}

void ATConsoleCmdPathDump(ATDebuggerCmdParser& parser) {
	ATDebuggerCmdPath pathArg(true);

	parser >> pathArg >> 0;

	// create symbol table based on paths
	vdrefptr<IATCustomSymbolStore> pSymbolStore;
	ATCreateCustomSymbolStore(~pSymbolStore);

	ATCPUEmulator& cpu = g_sim.GetCPU();
	sint32 addr = -1;
	for(;;) {
		addr = cpu.GetNextPathInstruction(addr);
		if (addr < 0)
			break;

		if (cpu.IsPathStart(addr)) {
			char buf[16];
			sprintf(buf, "L%04X", (uint16)addr);

			pSymbolStore->AddSymbol(addr, buf);
		}
	}

	const uint32 moduleId = g_debugger.AddModule(0, 0, 0x10000, pSymbolStore, NULL, NULL);

	FILE *f = fopen(pathArg->c_str(), "w");
	if (!f) {
		ATConsolePrintf("Unable to open file for write: %s\n", pathArg->c_str());
		g_debugger.UnloadSymbols(moduleId);
		return;
	} else {
		sint32 addr = -1;
		char buf[256];
		for(;;) {
			addr = cpu.GetNextPathInstruction(addr);
			if (addr < 0)
				break;

			ATDisassembleInsn(buf, addr, true);
			fputs(buf, f);
		}
	}
	fclose(f);

	g_debugger.UnloadSymbols(moduleId);

	ATConsolePrintf("Paths dumped to %s\n", pathArg->c_str());
}

void ATConsoleCmdPathBreak(ATDebuggerCmdParser& parser) {
	ATDebuggerCmdName nameArg(false);

	parser >> nameArg >> 0;

	ATCPUEmulator& cpu = g_sim.GetCPU();

	if (!nameArg.IsValid()) {
		ATConsolePrintf("Breaking on new paths is %s.\n", cpu.IsPathBreakEnabled() ? "on" : "off");
		return;
	}

	bool newState = false;
	if (!_stricmp(nameArg->c_str(), "on")) {
		newState = true;
	} else if (_stricmp(nameArg->c_str(), "off")) {
		ATConsoleWrite("Syntax: .pathbreak on|off\n");
		return;
	}

	cpu.SetPathBreakEnabled(newState);
	ATConsolePrintf("Breaking on new paths is now %s.\n", newState ? "on" : "off");
}

void ATConsoleCmdLoadKernelSymbols(ATDebuggerCmdParser& parser) {
	ATDebuggerCmdPath pathArg(false);

	parser >> pathArg >> 0;

	if (!pathArg.IsValid()) {
		ATConsoleWrite("Syntax: .loadksym <filename>\n");
		return;
	}

	vdrefptr<IATSymbolStore> symbols;
	try {
		ATLoadSymbols(VDTextAToW(pathArg->c_str()).c_str(), ~symbols);
	} catch(const MyError& e) {
		throw MyError("Unable to load symbols from %s: %s", pathArg->c_str(), e.gets());
	}

	g_debugger.AddModule(0, 0xD800, 0x2800, symbols, "Kernel", NULL);
	ATConsolePrintf("Kernel symbols loaded: %s\n", pathArg->c_str());
}

void ATConsoleCmdDiskOrder(ATDebuggerCmdParser& parser) {
	if (parser.IsEmpty()) {
		ATConsoleWrite("Syntax: .diskorder <sector> <indices>...\n");
		return;
	}

	ATDebuggerCmdExprNum sectorArg(true, false, 0, 65535);
	parser >> sectorArg;

	ATDiskEmulator& disk = g_sim.GetDiskDrive(0);
	ATDiskInterface& diskIf = g_sim.GetDiskInterface(0);
	uint32 sector = (uint32)sectorArg.GetValue();
	uint32 phantomCount = diskIf.GetSectorPhantomCount(sector);

	if (!phantomCount) {
		ATConsolePrintf("Invalid sector number: %u\n", sector);
		return;
	}

	if (parser.IsEmpty()) {
		for(uint32 i=0; i<phantomCount; ++i)
			disk.SetForcedPhantomSector(sector, i, -1);

		ATConsolePrintf("Automatic sector ordering restored for sector %u.\n", sector);
		return;
	}

	vdfastvector<uint8> indices;

	while(!parser.IsEmpty()) {
		ATDebuggerCmdExprNum numArg(true, false, 0, 255);

		parser >> numArg;

		uint32 index = (uint32)numArg.GetValue();

		if (!index || index > phantomCount) {
			ATConsolePrintf("Invalid phantom sector index: %u\n", index);
			return;
		}

		uint8 i8 = (uint8)(index - 1);
		if (std::find(indices.begin(), indices.end(), i8) != indices.end()) {
			ATConsolePrintf("Invalid repeated phantom sector index: %u\n", index);
			return;
		}

		indices.push_back(i8);
	}

	for(uint32 i=0; i<phantomCount; ++i) {
		vdfastvector<uint8>::const_iterator it(std::find(indices.begin(), indices.end(), i));

		if (it == indices.end())
			disk.SetForcedPhantomSector(sector, i, -1);
		else
			disk.SetForcedPhantomSector(sector, i, (int)(it - indices.begin()));
	}
}

namespace {
	struct SecInfo {
		int mVirtSec;
		int mPhantomIndex;
		ATDiskInterface::SectorInfo mInfo;

		bool operator<(const SecInfo& x) const {
			return mInfo.mRotPos < x.mInfo.mRotPos;
		}
	};
}

void ATConsoleCmdDiskTrack(ATDebuggerCmdParser& parser) {
	if (parser.IsEmpty()) {
		ATConsoleWrite("Syntax: .disktrack <track>...\n");
		return;
	}

	ATDebuggerCmdSwitchNumArg diskNoArg("d", 1, 15, 1);
	ATDebuggerCmdExprNum trackArg(true, false, 0, 65535);
	parser >> diskNoArg >> trackArg >> 0;

	ATDiskInterface& diskIf = g_sim.GetDiskInterface(diskNoArg.GetValue() - 1);
	uint32 track = (uint32)trackArg.GetValue();

	ATConsolePrintf("Track %d\n", track);

	uint32 vsecBase = track * 18 + 1;

	vdfastvector<SecInfo> sectors;

	for(uint32 i=0; i<18; ++i) {
		uint32 vsec = vsecBase + i;
		uint32 phantomCount = diskIf.GetSectorPhantomCount(vsec);

		for(uint32 phantomIdx = 0; phantomIdx < phantomCount; ++phantomIdx) {
			ATDiskInterface::SectorInfo si0;

			if (diskIf.GetSectorInfo(vsec, phantomIdx, si0)) {
				SecInfo& si = sectors.push_back();

				si.mVirtSec = vsec;
				si.mPhantomIndex = phantomIdx;
				si.mInfo = si0;
			}
		}
	}

	std::sort(sectors.begin(), sectors.end());

	vdfastvector<SecInfo>::const_iterator it(sectors.begin()), itEnd(sectors.end());
	for(; it != itEnd; ++it) {
		const SecInfo& si = *it;

		ATConsolePrintf("%4d/%d   %5.3f  $%02X  %s%s%s%s%s\n", si.mVirtSec, si.mPhantomIndex + 1, si.mInfo.mRotPos, si.mInfo.mFDCStatus
			, (si.mInfo.mFDCStatus & 0x18) == 0x10 ? " data-crc" : ""
			, (si.mInfo.mFDCStatus & 0x18) == 0x00 ? " address-crc" : ""
			, (si.mInfo.mFDCStatus & 0x18) == 0x08 ? " missing" : ""
			, (si.mInfo.mFDCStatus & 0x04) == 0x00 ? " long" : ""
			, (si.mInfo.mFDCStatus & 0x20) == 0x00 ? " deleted" : ""
		);
	}
}

void ATConsoleCmdDiskDumpSec(ATDebuggerCmdParser& parser) {
	ATDebuggerCmdSwitchNumArg driveSw("d", 1, 15, 1);
	ATDebuggerCmdExprNum sectorArg(true, false, 1, 65535);

	parser >> driveSw >> sectorArg >> 0;

	ATDiskInterface& diskIf = g_sim.GetDiskInterface(driveSw.GetValue() - 1);
	IATDiskImage *image = diskIf.GetDiskImage();
	
	if (!image)
		throw MyError("No disk image is mounted for drive D%u:.", driveSw.GetValue());

	uint32 sector = sectorArg.GetValue();
	if (!sector || sector > image->GetVirtualSectorCount())
		throw MyError("Invalid sector for disk image: %u.", sector);

	ATDiskVirtualSectorInfo vsi;
	image->GetVirtualSectorInfo(sector - 1, vsi);

	if (!vsi.mNumPhysSectors) {
		ATConsolePrintf("No physical sectors for virtual sector %d / $%X.\n", sector, sector);
		return;
	}

	for(uint32 pn = 0; pn < vsi.mNumPhysSectors; ++pn) {
		ATDiskPhysicalSectorInfo psi;
		image->GetPhysicalSectorInfo(vsi.mStartPhysSector + pn, psi);
		uint32 len = psi.mPhysicalSize;
			
		uint8 buf[8192];
		image->ReadPhysicalSector(vsi.mStartPhysSector + pn, buf, 8192);

		ATConsolePrintf("Sector %d / $%X (%u bytes) #%d:\n", sector, sector, len, pn + 1);

		VDStringA line;
		for(uint32 i=0; i<len; i+=16) {
			line.sprintf("%03X:", i);

			uint32 count = std::min<uint32>(len - i, 16);
			for(uint32 j=0; j<count; ++j)
				line.append_sprintf("%c%02X", j==8 ? '-' : ' ', buf[i+j]);

			line.resize(4 + 3*16 + 1, ' ');
			line += '|';

			for(uint32 j=0; j<count; ++j) {
				uint8 c = buf[i+j];

				if (c < 0x20 || c >= 0x7F)
					c = '.';

				line += (char)c;
			}

			line.resize(4 + 3*16 + 2 + 16, ' ');
			line += '|';
			line += '\n';

			ATConsoleWrite(line.c_str());
		}
	}
}

void ATConsoleCmdDiskReadSec(ATDebuggerCmdParser& parser) {
	ATDebuggerCmdExprNum num(true, false, 0);
	ATDebuggerCmdExprAddr addr(true, true, false);
	ATDebuggerCmdSwitchNumArg driveSw("d", 1, 15, 1);
	ATDebuggerCmdExprNum sectorArg(true, false, 1, 65535);

	parser >> driveSw >> sectorArg >> addr >> 0;

	ATDiskInterface& diskIf = g_sim.GetDiskInterface(driveSw.GetValue() - 1);
	IATDiskImage *image = diskIf.GetDiskImage();
	
	if (!image)
		throw MyError("No disk image is mounted for drive D%u:.", driveSw.GetValue());

	uint32 sector = sectorArg.GetValue();
	if (!sector || sector > image->GetVirtualSectorCount())
		throw MyError("Invalid sector count for disk image: %u.", sector);

	uint8 buf[8192];
	uint32 len = image->ReadVirtualSector(sector - 1, buf, 8192);

	uint32 addrhi = addr.GetValue() & kATAddressSpaceMask;
	uint32 addrlo = addr.GetValue();

	for(uint32 i=0; i<len; ++i) {
		g_sim.DebugGlobalWriteByte(addrhi + (addrlo & kATAddressOffsetMask), buf[i]);
		++addrlo;
	}

	ATConsolePrintf("Read sector %u to %s-%s.\n", sector, g_debugger.GetAddressText(addr.GetValue(), false).c_str(),
		g_debugger.GetAddressText(addrhi + ((addrlo - 1) & kATAddressOffsetMask), false).c_str());
}

void ATConsoleCmdDiskWriteSec(ATDebuggerCmdParser& parser) {
	ATDebuggerCmdExprNum num(true, false, 0);
	ATDebuggerCmdExprAddr addr(true, true, false);
	ATDebuggerCmdSwitchNumArg driveSw("d", 1, 15, 1);
	ATDebuggerCmdExprNum sectorArg(true, false, 1, 65535);

	parser >> driveSw >> sectorArg >> addr >> 0;

	ATDiskInterface& disk = g_sim.GetDiskInterface(driveSw.GetValue() - 1);
	IATDiskImage *image = disk.GetDiskImage();
	
	if (!image)
		throw MyError("No disk image is mounted for drive D%u:.", driveSw.GetValue());

	uint32 sector = sectorArg.GetValue();
	if (!sector || sector > image->GetVirtualSectorCount())
		throw MyError("Invalid sector count for disk image: %u.", sector);
	
	uint32 len = image->GetSectorSize(sector - 1);
	vdblock<uint8> buf(len);

	uint32 addrhi = addr.GetValue() & kATAddressSpaceMask;
	uint32 addrlo = addr.GetValue();

	for(uint32 i=0; i<len; ++i) {
		buf[i] = g_sim.DebugGlobalReadByte(addrhi + (addrlo & kATAddressOffsetMask));
		++addrlo;
	}

	image->WriteVirtualSector(sector - 1, buf.data(), len);
	disk.OnDiskModified();

	ATConsolePrintf("Wrote to %s-%s to sector %u.\n", g_debugger.GetAddressText(addr.GetValue(), false).c_str(),
		g_debugger.GetAddressText(addrhi + ((addrlo - 1) & kATAddressOffsetMask), false).c_str(), sector);
}

void ATConsoleCmdCasLogData(ATDebuggerCmdParser& parser) {
	parser >> 0;

	ATCassetteEmulator& cas = g_sim.GetCassette();

	bool newSetting = !cas.IsLogDataEnabled();
	cas.SetLogDataEnable(newSetting);

	ATConsolePrintf("Verbose cassette read data logging is now %s.\n", newSetting ? "enabled" : "disabled");
}

void ATConsoleCmdDumpPIAState(ATDebuggerCmdParser& parser) {
	parser >> 0;

	g_sim.GetPIA().DumpState();
}

void ATConsoleCmdDumpVBXEState(ATDebuggerCmdParser& parser) {
	parser >> 0;

	ATVBXEEmulator *vbxe = g_sim.GetVBXE();

	if (!vbxe)
		ATConsoleWrite("VBXE is not enabled.\n");
	else
		vbxe->DumpStatus();
}

void ATConsoleCmdDumpVBXEBL(ATDebuggerCmdParser& parser) {
	parser >> 0;

	ATVBXEEmulator *vbxe = g_sim.GetVBXE();

	if (!vbxe)
		ATConsoleWrite("VBXE is not enabled.\n");
	else
		vbxe->DumpBlitList();
}

void ATConsoleCmdDumpVBXEXDL(ATDebuggerCmdParser& parser) {
	parser >> 0;

	ATVBXEEmulator *vbxe = g_sim.GetVBXE();

	if (!vbxe)
		ATConsoleWrite("VBXE is not enabled.\n");
	else
		vbxe->DumpXDL();
}

void ATConsoleCmdVBXETraceBlits(ATDebuggerCmdParser& parser) {
	ATDebuggerCmdName nameArg(false);
	parser >> nameArg >> 0;

	ATVBXEEmulator *vbxe = g_sim.GetVBXE();

	if (!vbxe) {
		ATConsoleWrite("VBXE is not enabled.\n");
		return;
	}

	if (nameArg.IsValid()) {
		bool newState = false;
		if (!vdstricmp(nameArg->c_str(), "on")) {
			newState = true;
		} else if (vdstricmp(nameArg->c_str(), "off")) {
			ATConsoleWrite("Syntax: .vbxe_traceblits on|off\n");
			return;
		}

		vbxe->SetBlitLoggingEnabled(newState);
	}

	ATConsolePrintf("VBXE blit tracing is currently %s.\n", vbxe->IsBlitLoggingEnabled() ? "on" : "off");
}

void ATConsoleCmdIOCB(ATDebuggerCmdParser& parser) {
	parser >> 0;

	ATConsoleWrite("CIO IOCBs:\n");
	ATConsoleWrite(" #  Dev      Cd St Bufr PutR BfLn X1 X2 X3 X4 X5 X6\n");

	VDStringA s;

	for(int i=-1; i<=7; ++i) {
		uint16 addr;

		if (i < 0) {
			s = "ZP  ";
			addr = ATKernelSymbols::ICHIDZ;
		} else {
			s.sprintf("%2d  ", i);
			addr = ATKernelSymbols::ICHID + 16*i;
		}

		uint8 iocb[16];
		for(int j=0; j<16; ++j)
			iocb[j] = g_sim.DebugReadByte(addr + j);

		bool driveValid = false;
		if (iocb[0] == 0x7F) {
			// provisional open - ICAX3 contains the device name, ICAX4 the SIO address
			s.append_sprintf("$%02X~%c", iocb[13], iocb[12]);

			if (iocb[1] > 1)
				s.append_sprintf("%u", iocb[1]);

			s += ':';
		} else if (iocb[0] != 0xFF) {
			uint8 specifier = g_sim.DebugReadByte(ATKernelSymbols::HATABS + iocb[0]);
			if (specifier >= 0x20 && specifier < 0x7F) {
				driveValid = true;

				if (iocb[1] <= 1)
					s.append_sprintf("%c:", specifier);
				else
					s.append_sprintf("%c%d:", specifier, iocb[1]);
			}
		}

		size_t pad = s.size();
		while(pad < 13) {
			++pad;
			s.push_back(' ');
		}

		s.append_sprintf("%02X %02X %02X%02X %02X%02X %02X%02X %02X %02X %02X %02X %02X %02X\n"
			, iocb[2]			// command
			, iocb[3]			// status
			, iocb[5], iocb[4]	// buffer pointer
			, iocb[7], iocb[6]	// put routine
			, iocb[9], iocb[8]	// buffer length
			, iocb[10]
			, iocb[11]
			, iocb[12]
			, iocb[13]
			, iocb[14]
			, iocb[15]
			);

		ATConsoleWrite(s.c_str());
	}
}

void ATConsoleCmdBasic(ATDebuggerCmdParser& parser) {
	parser >> 0;

	IATDebugTarget *target = g_debugger.GetTarget();

	uint16 basicdat[9] = {0};
	target->DebugReadMemory(0x80, basicdat, 18);

	uint16 lomem	= VDFromLE16(basicdat[0]);
	uint16 vntp		= VDFromLE16(basicdat[1]);
	uint16 vvtp		= VDFromLE16(basicdat[3]);
	uint16 stmtab	= VDFromLE16(basicdat[4]);
	uint16 stmcur	= VDFromLE16(basicdat[5]);
	uint16 starp	= VDFromLE16(basicdat[6]);
	uint16 runstk	= VDFromLE16(basicdat[7]);
	uint16 memtop	= VDFromLE16(basicdat[8]);

	ATConsoleWrite("BASIC table pointers:\n");
	ATConsolePrintf("  LOMEM   Low memory bound      %04X\n", lomem);
	ATConsolePrintf("  VNTP    Variable name table   %04X (%d bytes)\n", vntp, vvtp - vntp);
	ATConsolePrintf("  VVTP    Variable value table  %04X (%d bytes)\n", vvtp, stmtab - vvtp);
	ATConsolePrintf("  STMTAB  Statement table       %04X (%d bytes)\n", stmtab, starp - stmtab);
	ATConsolePrintf("  STMCUR  Current statement     %04X\n", stmcur);
	ATConsolePrintf("  STARP   String/array table    %04X (%d bytes)\n", starp, runstk - starp);
	ATConsolePrintf("  RUNSTK  Runtime stack         %04X (%d bytes)\n", runstk, memtop - runstk);
	ATConsolePrintf("  MEMTOP  Top of used memory    %04X\n", memtop);
}

void ATConsoleCmdBasicDumpLine(ATDebuggerCmdParser& parser) {
	ATDebuggerCmdExprAddr addrArg(true, false, true);
	ATDebuggerCmdSwitchExprNumArg offsetArg("o", 0, 255);
	ATDebuggerCmdSwitch continuousArg("c", false);
	ATDebuggerCmdSwitch tbxlArg("t", false);
	ATDebuggerCmdSwitch tokenArg("k", false);

	parser >> tokenArg >> tbxlArg >> continuousArg >> offsetArg >> addrArg >> 0;
	
	IATDebugTarget *target = g_debugger.GetTarget();

	uint16 basicdat[9] = {0};
	target->DebugReadMemory(0x80, basicdat, 18);

	VDStringA line;
	uint32 addr = addrArg.IsStar() ? VDFromLE16(basicdat[5]) : addrArg.IsValid() ? addrArg.GetValue() : g_debugger.GetContinuationAddress();

	// use STMTAB if address is 0
	if (!addr)
		addr = VDFromLE16(basicdat[4]);

	const uint16 vntp = VDFromLE16(basicdat[1]);
	const uint16 vvtp = VDFromLE16(basicdat[3]);
	int lastLineNo = -1;
	const bool tbxl = tbxlArg;

	for(;;) {
		bool brokenLine = false;

		uint8 lineInfo[3] = {0};

		target->DebugReadMemory(addr, lineInfo, 3);

		const uint16 lineNumber = VDReadUnalignedLEU16(&lineInfo[0]);
		int lineLen = lineInfo[2];

		line.sprintf("%s: %u ", g_debugger.GetAddressText(addr, true).c_str(), lineNumber);

		if (lineNumber > 32768 || lineNumber <= lastLineNo) {
			line += "[invalid line number]";
			brokenLine = true;
		} else if (lineLen < 3) {
			line += "[invalid line length]";
			brokenLine = true;
		} else {
			int offset = 3;

			lastLineNo = lineNumber;

			g_debugger.SetContinuationAddress(addr + lineLen);

			while(offset < lineLen) {
				// read statement offset
				const uint8 statementOffset = target->DebugReadByte(addr + offset++);

				if (statementOffset > lineLen || statementOffset < offset)
					break;

				// check if we need to mark
				if (offsetArg.IsValid() && offset == offsetArg.GetValue())
					line += ">>";

				const uint8 statementToken = target->DebugReadByte(addr + offset++);

				if (tokenArg) {
					line += '\n';
					ATConsoleWrite(line.c_str());
					line.sprintf("$%04X:    $%02X $%02X   ", (addr + offset - 2) & 0xFFFF, statementOffset, statementToken);
				}

				static const char* const kStatements[]={
					/* 0x00 */ "REM",
					/* 0x01 */ "DATA",
					/* 0x02 */ "INPUT",
					/* 0x03 */ "COLOR",
					/* 0x04 */ "LIST",
					/* 0x05 */ "ENTER",
					/* 0x06 */ "LET",
					/* 0x07 */ "IF",
					/* 0x08 */ "FOR",
					/* 0x09 */ "NEXT",
					/* 0x0A */ "GOTO",
					/* 0x0B */ "GO TO",
					/* 0x0C */ "GOSUB",
					/* 0x0D */ "TRAP",
					/* 0x0E */ "BYE",
					/* 0x0F */ "CONT",
					/* 0x10 */ "COM",
					/* 0x11 */ "CLOSE",
					/* 0x12 */ "CLR",
					/* 0x13 */ "DEG",
					/* 0x14 */ "DIM",
					/* 0x15 */ "END",
					/* 0x16 */ "NEW",
					/* 0x17 */ "OPEN",
					/* 0x18 */ "LOAD",
					/* 0x19 */ "SAVE",
					/* 0x1A */ "STATUS",
					/* 0x1B */ "NOTE",
					/* 0x1C */ "POINT",
					/* 0x1D */ "XIO",
					/* 0x1E */ "ON",
					/* 0x1F */ "POKE",
					/* 0x20 */ "PRINT",
					/* 0x21 */ "RAD",
					/* 0x22 */ "READ",
					/* 0x23 */ "RESTORE",
					/* 0x24 */ "RETURN",
					/* 0x25 */ "RUN",
					/* 0x26 */ "STOP",
					/* 0x27 */ "POP",
					/* 0x28 */ "?",
					/* 0x29 */ "GET",
					/* 0x2A */ "PUT",
					/* 0x2B */ "GRAPHICS",
					/* 0x2C */ "PLOT",
					/* 0x2D */ "POSITION",
					/* 0x2E */ "DOS",
					/* 0x2F */ "DRAWTO",
					/* 0x30 */ "SETCOLOR",
					/* 0x31 */ "LOCATE",
					/* 0x32 */ "SOUND",
					/* 0x33 */ "LPRINT",
					/* 0x34 */ "CSAVE",
					/* 0x35 */ "CLOAD",
					/* 0x36 */ "",
					/* 0x37 */ "ERROR -",
				};

				static const char* const kStatementsBXE[]={
					// BASIC XE
					/* 0x38 */ "WHILE",
					/* 0x39 */ "ENDWHILE",
					/* 0x3A */ "TRACEOFF",
					/* 0x3B */ "TRACE",
					/* 0x3C */ "ELSE",
					/* 0x3D */ "ENDIF",
					/* 0x3E */ "DPOKE",
					/* 0x3F */ "LOMEM",
					/* 0x40 */ "DEL",
					/* 0x41 */ "RPUT",
					/* 0x42 */ "RGET",
					/* 0x43 */ "BPUT",
					/* 0x44 */ "BGET",
					/* 0x45 */ "TAB",
					/* 0x46 */ "CP",
					/* 0x47 */ "ERASE",
					/* 0x48 */ "PROTECT",
					/* 0x49 */ "UNPROTECT",
					/* 0x4A */ "DIR",
					/* 0x4B */ "RENAME",
					/* 0x4C */ "MOVE",
					/* 0x4D */ "MISSILE",
					/* 0x4E */ "PMCLR",
					/* 0x4F */ "PMCOLOR",
					/* 0x50 */ "PMGRAPHICS",
					/* 0x51 */ "PMMOVE",
					/* 0x52 */ "PMWIDTH",
					/* 0x53 */ "SET",
					/* 0x54 */ "LVAR",
					/* 0x55 */ "RENUM",
					/* 0x56 */ "FAST",
					/* 0x57 */ "LOCAL",
					/* 0x58 */ "EXTEND",
					/* 0x59 */ "PROCEDURE",
					/* 0x5A */ 0,
					/* 0x5B */ "CALL",
					/* 0x5C */ "SORTUP",
					/* 0x5D */ "SORTDOWN",
					/* 0x5E */ "EXIT",
					/* 0x5F */ "NUM",
					/* 0x60 */ "HITCLR",
					/* 0x61 */ "INVERSE",
					/* 0x62 */ "NORMAL",
					/* 0x63 */ "BLOAD",
					/* 0x64 */ "BSAVE",
				};

				static const char* const kStatementsTBXL[]={
					// TurboBASIC XL
					/* 0x38 */ "DPOKE",
					/* 0x39 */ "MOVE",
					/* 0x3A */ "-MOVE",
					/* 0x3B */ "*F",
					/* 0x3C */ "REPEAT",
					/* 0x3D */ "UNTIL",
					/* 0x3E */ "WHILE",
					/* 0x3F */ "WEND",
					/* 0x40 */ "ELSE",
					/* 0x41 */ "ENDIF",
					/* 0x42 */ "BPUT",
					/* 0x43 */ "BGET",
					/* 0x44 */ "FILLTO",
					/* 0x45 */ "DO",
					/* 0x46 */ "LOOP",
					/* 0x47 */ "EXIT",
					/* 0x48 */ "DIR",
					/* 0x49 */ "LOCK",
					/* 0x4A */ "UNLOCK",
					/* 0x4B */ "RENAME",
					/* 0x4C */ "DELETE",
					/* 0x4D */ "PAUSE",
					/* 0x4E */ "TIME$=",
					/* 0x4F */ "PROC",
					/* 0x50 */ "EXEC",
					/* 0x51 */ "ENDPROC",
					/* 0x52 */ "FCOLOR",
					/* 0x53 */ "*L",
					/* 0x54 */ "---",
					/* 0x55 */ "RENUM",
					/* 0x56 */ "DEL",
					/* 0x57 */ "DUMP",
					/* 0x58 */ "TRACE",
					/* 0x59 */ "TEXT",
					/* 0x5A */ "BLOAD",
					/* 0x5B */ "BRUN",
					/* 0x5C */ "GO#",
					/* 0x5D */ "#",
					/* 0x5E */ "*B",
					/* 0x5F */ "PAINT",
					/* 0x60 */ "CLS",
					/* 0x61 */ "DSOUND",
					/* 0x62 */ "CIRCLE",
					/* 0x63 */ "%PUT",
					/* 0x64 */ "%GET",
				};

				const char *statementName = nullptr;

				if (statementToken < 0x38)
					statementName = kStatements[statementToken];
				else if (tbxl) {
					if (statementToken < vdcountof(kStatementsTBXL)+0x38)
						statementName = kStatementsTBXL[statementToken - 0x38];
				} else {
					if (statementToken < vdcountof(kStatementsBXE)+0x38)
						statementName = kStatementsBXE[statementToken - 0x38];
				}

				if (!statementName) {
					line.append_sprintf("[invalid statement token: $%02X]", statementToken);
					break;
				}

				if (statementToken == 0x36) {
					if (tokenArg)
						line += "(let)";
				} else {
					line += statementName;
					line += ' ';
				}

				// check for REM, DATA, and syntax error cases
				if (statementToken == 0x37 || statementToken == 0x01 || statementToken == 0) {
					while(offset < lineLen) {
						uint8 c = target->DebugReadByte(addr + offset++) & 0x7f;

						if (c < 0x20 || c >= 0x7F)
							line.append_sprintf("{%02X}", c);
						else
							line += (char)c;
					}

					line.append_sprintf(" {end $%04X}", (addr + offset) & 0xffff);
					break;
				}

				// process operator/function/variable tokens
				// note that an IF...THEN will abruptly end the statement after the THEN without
				// any function tokens
				uint8 buf[6];

				while(offset < statementOffset) {
					// check if we need to mark
					if (offsetArg.IsValid() && offset == offsetArg.GetValue())
						line += ">>";

					const uint8 token = target->DebugReadByte(addr + offset++);
					uint8 token2 = 0;

					if (token == 0x16) {
						line.append_sprintf(" {end $%04X}", (addr + offset) & 0xffff);
						goto line_end;
					}

					if (tbxl && !token)
						token2 = target->DebugReadByte(addr + offset++);

					if (tokenArg) {
						line += '\n';
						ATConsoleWrite(line.c_str());

						if (tbxl && !token)
							line.sprintf("$%04X:      $%02X $%02X    ", (addr + offset - 2) & 0xFFFF, token, token2);
						else
							line.sprintf("$%04X:      $%02X        ", (addr + offset - 1) & 0xFFFF, token);
					}

					if (token == 0x14) {
						line += ": ";
						break;
					}

					switch(token) {
						case 0x0D:
							for(int i=0; i<6; ++i)
								buf[i] = target->DebugReadByte(addr + offset++);

							line.append_sprintf("$%04X", (int)ATReadDecFloatAsBinary(buf));
							break;

						case 0x0E:
							for(int i=0; i<6; ++i)
								buf[i] = target->DebugReadByte(addr + offset++);

							line.append_sprintf("%G", ATReadDecFloatAsBinary(buf));
							break;

						case 0x0F:
							line += '"';
							{
								uint8 len = target->DebugReadByte(addr + offset++);

								while(len--) {
									uint8 c = target->DebugReadByte(addr + offset++);

									if (c < 0x20 || c >= 0x7F)
										line.append_sprintf("{%02X}", c);
									else
										line += (char)c;
								}
							}
							line += '"';
							break;

						case 0x12:	line += ','; break;
						case 0x13:	line += '$'; break;
						case 0x15:	line += ';'; break;
						case 0x17:	line += " GOTO "; break;
						case 0x18:	line += " GOSUB "; break;
						case 0x19:	line += " TO "; break;
						case 0x1A:	line += " STEP "; break;
						case 0x1B:	line += " THEN "; break;
						case 0x1C:	line += '#'; break;
						case 0x1D:	line += "<="; break;
						case 0x1E:	line += "<>"; break;
						case 0x1F:	line += ">="; break;
						case 0x20:	line += '<'; break;
						case 0x21:	line += '>'; break;
						case 0x22:	line += '='; break;
						case 0x23:	line += '^'; break;
						case 0x24:	line += '*'; break;
						case 0x25:	line += '+'; break;
						case 0x26:	line += '-'; break;
						case 0x27:	line += '/'; break;
						case 0x28:	line += " NOT "; break;
						case 0x29:	line += " OR "; break;
						case 0x2A:	line += " AND "; break;
						case 0x2B:	line += '('; break;
						case 0x2C:	line += ')'; break;
						case 0x2D:	line += '='; break;
						case 0x2E:	line += '='; break;
						case 0x2F:	line += "<="; break;
						case 0x30:	line += "<>"; break;
						case 0x31:	line += ">="; break;
						case 0x32:	line += '<'; break;
						case 0x33:	line += '>'; break;
						case 0x34:	line += '='; break;
						case 0x35:	line += '+'; break;
						case 0x36:	line += '-'; break;
						case 0x37:	line += '('; break;
						case 0x38:	break;
						case 0x39:	break;
						case 0x3A:
						case 0x3B:
							line += '(';
							break;
						case 0x3C:	line += ','; break;
						case 0x3D:	line += "STR$"; break;
						case 0x3E:	line += "CHR$"; break;
						case 0x3F:	line += "USR"; break;
						case 0x40:	line += "ASC"; break;
						case 0x41:	line += "VAL"; break;
						case 0x42:	line += "LEN"; break;
						case 0x43:	line += "ADR"; break;
						case 0x44:	line += "ATN"; break;
						case 0x45:	line += "COS"; break;
						case 0x46:	line += "PEEK"; break;
						case 0x47:	line += "SIN"; break;
						case 0x48:	line += "RND"; break;
						case 0x49:	line += "FRE"; break;
						case 0x4A:	line += "EXP"; break;
						case 0x4B:	line += "LOG"; break;
						case 0x4C:	line += "CLOG"; break;
						case 0x4D:	line += "SQR"; break;
						case 0x4E:	line += "SGN"; break;
						case 0x4F:	line += "ABS"; break;
						case 0x50:	line += "INT"; break;
						case 0x51:	line += "PADDLE"; break;
						case 0x52:	line += "STICK"; break;
						case 0x53:	line += "PTRIG"; break;
						case 0x54:	line += "STRIG"; break;

						// BASIC XE / TurboBASIC XL
						case 0x55:	line += tbxl ? "DPEEK" : "USING"; break;
						case 0x56:	line += tbxl ? "&" : "%"; break;
						case 0x57:	line += "!"; break;
						case 0x58:	line += tbxl ? "INSTR" : "&"; break;
						case 0x59:	line += tbxl ? "INKEY$" : ";"; break;
						case 0x5A:	line += tbxl ? " EXOR " : "BUMP("; break;
						case 0x5B:	line += tbxl ? "HEX$" : "FIND("; break;
						case 0x5C:	line += tbxl ? "DEC" : "HEX$"; break;
						case 0x5D:	line += tbxl ? " DIV " : "RANDOM("; break;
						case 0x5E:	line += tbxl ? "FRAC" : "DPEEK"; break;
						case 0x5F:	line += tbxl ? "TIME$" : "SYS"; break;
						case 0x60:	line += tbxl ? "TIME" : "VSTICK"; break;
						case 0x61:	line += tbxl ? " MOD " : "HSTICK"; break;
						case 0x62:	line += tbxl ? " EXEC " : "PMADR"; break;
						case 0x63:	line += tbxl ? "RND" : "ERR"; break;
						case 0x64:	line += tbxl ? "RAND" : "TAB"; break;
						case 0x65:	line += tbxl ? "TRUNC" : "PEN"; break;
						case 0x66:	line += tbxl ? "%0" : "LEFT$("; break;
						case 0x67:	line += tbxl ? "%1" : "RIGHT$("; break;
						case 0x68:	line += tbxl ? "%2" : "MID$("; break;
						case 0x69:	if (!tbxl) goto invalid; line += "%3"; break;
						case 0x6A:	if (!tbxl) goto invalid; line += " GO# "; break;
						case 0x6B:	if (!tbxl) goto invalid; line += "UINSTR"; break;
						case 0x6C:	if (!tbxl) goto invalid; line += "ERR"; break;
						case 0x6D:	if (!tbxl) goto invalid; line += "ERL"; break;

						default:
							// Function token $00 is special in TurboBASIC XL. It indicates an extended
							// variable.
							if (token < 0x80 && (!tbxl || token)) {
invalid:
								line.append_sprintf(" [invalid token $%02X]", token);
								brokenLine = true;
								goto line_end;
							}

							{
								bool varValid = false;

								if (vntp < vvtp) {
									uint16 vaddr = vntp;
									uint8 vidx;
									
									if (token)
										vidx = token - 0x80;
									else
										vidx = 0x80 + token2;

									while(vidx-- > 0) {
										while(vaddr < vvtp && !(target->DebugReadByte(vaddr++) & 0x80))
											;
									}

									if (vaddr < vvtp) {
										size_t curLen = line.size();

										varValid = true;

										for(int i=0; i<16; ++i) {
											if (vaddr >= vvtp) {
												varValid = false;
												break;
											}

											uint8 b = target->DebugReadByte(vaddr++);
											uint8 c = b & 0x7f;

											if (c < 0x20 || c >= 0x7f) {
												varValid = false;
												break;
											}

											line += (char)c;

											if (b & 0x80)
												break;
										}

										if (!varValid)
											line.resize((uint32)curLen);
									}
								}

								if (!varValid)
									line.append_sprintf("[V%02X]", token);
							}
							break;
					}
				}
			}
		}

line_end:
		line += '\n';
		ATConsoleWrite(line.c_str());

		if (brokenLine || !continuousArg || lineNumber >= 32768)
			break;

		addr = (uint16)g_debugger.GetContinuationAddress();
	}
}

void ATConsoleCmdBasicDumpStack(ATDebuggerCmdParser& parser) {
	ATDebuggerCmdSwitch tbxlArg("t", false);
	ATDebuggerCmdSwitch addrArg("a", false);
	parser >> tbxlArg >> addrArg >> 0;

	const bool tbxl = tbxlArg;
	const bool absAddr = addrArg;

	const uint32 runstk = g_sim.DebugReadWord(0x8E);
	const uint32 memtop2 = g_sim.DebugReadWord(0x90);

	if (memtop2 < runstk)
		throw MyError("Invalid runtime stack ($%04X > $%04X)", runstk, memtop2);

	uint32 sp = memtop2;

	while(sp - runstk >= 4) {
		uint8 lineref[4];

		sp -= 4;

		for(int i=0; i<4; ++i)
			lineref[i] = g_sim.DebugReadByte(sp + i);

		const uint16 lineno = VDReadUnalignedLEU16(lineref+1);
		if (tbxl) {
			if (lineref[0]) {
				const char *type = "?";

				switch(lineref[0]) {
					case 0x18: type = "RETURN"; break;
					case 0x3C: type = "UNTIL"; break;
					case 0x3E: type = "WEND"; break;
					case 0x45: type = "LOOP"; break;
					case 0x50: type = "ENDPROC"; break;
				}

				if (absAddr)
					ATConsolePrintf("$%04X  %s to line %u ($%04X) offset %u\n", sp, type, g_sim.DebugReadWord(lineno), lineno, lineref[3]);
				else
					ATConsolePrintf("$%04X  %s to line %u offset %u\n", sp, type, lineno, lineref[3]);
			} else {
				if (sp - runstk < 13)
					break;

				sp -= 13;

				uint8 stepdat[13];
				for(int i=0; i<13; ++i)
					stepdat[i] = g_sim.DebugReadByte(sp + i);

				double limit = ATReadDecFloatAsBinary(stepdat);
				double step = ATReadDecFloatAsBinary(stepdat + 6);

				if (absAddr)
					ATConsolePrintf("$%04X  FOR V%02X TO %g STEP %g at line %u ($%04X) offset %u\n", sp, stepdat[12], limit, step, g_sim.DebugReadWord(lineno), lineno, lineref[3]);
				else
					ATConsolePrintf("$%04X  FOR V%02X TO %g STEP %g at line %u offset %u\n", sp, stepdat[12], limit, step, lineno, lineref[3]);
			}
		} else {
			if (lineref[0] < 0x80) {
				if (absAddr)
					ATConsolePrintf("$%04X  RETURN to line %u ($%04X) offset %u\n", sp, g_sim.DebugReadWord(lineno), lineno, lineref[3]);
				else
					ATConsolePrintf("$%04X  RETURN to line %u offset %u\n", sp, lineno, lineref[3]);
			} else {
				if (sp - runstk < 12)
					break;

				sp -= 12;

				uint8 stepdat[12];
				for(int i=0; i<12; ++i)
					stepdat[i] = g_sim.DebugReadByte(sp + i);

				double limit = ATReadDecFloatAsBinary(stepdat);
				double step = ATReadDecFloatAsBinary(stepdat + 6);

				ATConsolePrintf("$%04X  FOR V%02X TO %g STEP %g at line %u offset %u\n", sp, lineref[0], limit, step, VDReadUnalignedLEU16(lineref+1), lineref[3]);
			}
		}
	}
}

void ATConsoleCmdBasicVars(ATDebuggerCmdParser& parser) {
	parser >> 0;

	ATConsoleWrite("BASIC variables:\n");

	uint16 lomem = g_sim.DebugReadWord(0x0080);
	uint16 vntp = g_sim.DebugReadWord(0x0082);
	uint16 vvtp = g_sim.DebugReadWord(0x0086);
	uint16 stmtab = g_sim.DebugReadWord(0x0088);
	uint16 starp = g_sim.DebugReadWord(0x008c);

	// validate tables
	if (lomem > vntp || vntp > vvtp || vvtp > stmtab) {
		ATConsoleWrite("Tables are invalid. See .basic output.\n");
		return;
	}

	VDStringA s;
	for(uint32 i=0; i<256; ++i) {
		if (i < 128)
			s.sprintf("$%02X    ", i + 0x80);
		else
			s.sprintf("$00%02X  ", i - 0x80);

		for(int j=0; j<64; ++j) {
			uint8 c = g_sim.DebugReadByte(vntp++);
			if (!c)
				return;

			uint8 d = c & 0x7F;

			if ((uint8)(d - 0x20) < 0x5F)
				s += d;
			else
				s.append_sprintf("<%02X>", d);

			if (c & 0x80)
				break;
		}

		uint16 valptr = vvtp + i*8;
		uint8 dat[8];

		for(int j=0; j<8; ++j)
			dat[j] = g_sim.DebugReadByte(valptr+j);

		while(s.length() < 12)
			s += ' ';

		if (dat[0] >= 0xC0) {
			if (dat[0] & 0x02)
				s.append_sprintf("label -> $%04X", VDReadUnalignedLEU16(dat+2));
			else if (dat[0] & 0x01)
				s.append_sprintf("proc -> $%04X", VDReadUnalignedLEU16(dat+2));
			else
				s += "label/proc -> not resolved yet";
		} else if (dat[0] & 0x80) {
			if (!(dat[0] & 0x01)) {
				s += " = undimensioned";
			} else if (dat[0] & 0x02) {
				s.append_sprintf(" = len=%u, capacity=%u, address=$%04x (absolute)", VDReadUnalignedLEU16(dat+4), VDReadUnalignedLEU16(dat+6), VDReadUnalignedLEU16(dat+2));
			} else {
				s.append_sprintf(" = len=%u, capacity=%u, address=$%04x (relative)", VDReadUnalignedLEU16(dat+4), VDReadUnalignedLEU16(dat+6), VDReadUnalignedLEU16(dat+2) + starp);
			}
		} else if (dat[0] & 0x40) {
			if (!(dat[0] & 0x01))
				s += " = undimensioned";
			else if (dat[0] & 0x02)
				s.append_sprintf(" = size=%ux%u, address=$%04x (absolute)", VDReadUnalignedLEU16(dat+4), VDReadUnalignedLEU16(dat+6), VDReadUnalignedLEU16(dat+2));
			else
				s.append_sprintf(" = size=%ux%u, address=$%04x (relative)", VDReadUnalignedLEU16(dat+4), VDReadUnalignedLEU16(dat+6), VDReadUnalignedLEU16(dat+2) + starp);
		} else {
			s.append_sprintf(" = %g", ATReadDecFloatAsBinary(dat+2));
		}

		s += '\n';

		ATConsoleWrite(s.c_str());
	}
}

void ATConsoleCmdBasicRebuildVnt(ATDebuggerCmdParser& parser) {
	ATDebuggerCmdSwitch tbxlSw("t", false);
	parser >> tbxlSw >> 0;

	uint16 pointers[9];

	for(int i=0; i<9; ++i)
		pointers[i] = g_sim.DebugReadWord(0x80 + 2*i);

	if (pointers[3] < pointers[1])
		throw MyError("Invalid variable name table region.");

	if (pointers[4] < pointers[3])
		throw MyError("Invalid variable value table region.");

	if (pointers[6] < pointers[4])
		throw MyError("Invalid statement table region.");

	if (pointers[7] < pointers[6])
		throw MyError("Invalid string/array region.");

	if (pointers[8] < pointers[7])
		throw MyError("Invalid runtime stack region.");

	const uint16 vvtp = pointers[3];
	const uint16 stmtab = pointers[4];

	if ((stmtab - vvtp) & 7)
		throw MyError("Invalid variable value table region ($%04X-%04X)", vvtp, stmtab - 1);

	vdfastvector<uint8> vnt;

	const bool tbxl = tbxlSw;
	uint32 vvptr = vvtp;
	int typecnt[3] = { 0, 0, 0 };
	int varidx = 0;
	while(vvptr < stmtab) {
		uint8 vartype = g_sim.DebugReadByte(vvptr) >> 6;

		if (vartype == 3 && !tbxl)
			ATConsolePrintf("WARNING: Variable $%02X has invalid type.\n", varidx + 0x80);

		int varnameidx = typecnt[vartype]++;

		if (varnameidx >= 26) {
			vnt.push_back((uint8)(0x41 + (varnameidx / 26)));
			varnameidx %= 26;
		}

		vnt.push_back((uint8)(0x41 + varnameidx));

		switch(vartype) {
			case 0:
			case 3:
				vnt.back() |= 0x80;
				break;

			case 2:
				vnt.push_back(0xA4);	// $
				break;

			case 1:
				vnt.push_back(0xA8);	// inverted (
				break;
		}

		vvptr += 8;
		++varidx;
	}

	vnt.push_back(0);

	// relocate upper tables starting at VVTP to make room
	uint32 vntNewSize = (uint32)vnt.size();
	uint32 upperRegionSize = pointers[8] - pointers[3];
	uint32 upperRegionOldBase = pointers[3];
	uint32 upperRegionNewBase = pointers[1] + vntNewSize;

	if (upperRegionOldBase < upperRegionNewBase) {
		for(uint32 i = upperRegionSize; i; --i)
			g_sim.DebugGlobalWriteByte((upperRegionNewBase + i - 1) & 0xffff, g_sim.DebugReadByte(upperRegionOldBase + i - 1));
	} else if (upperRegionOldBase > upperRegionNewBase) {
		for(uint32 i = 0; i < upperRegionSize; ++i)
			g_sim.DebugGlobalWriteByte(upperRegionNewBase + i, g_sim.DebugReadByte(upperRegionOldBase + i));
	}

	sint32 delta = (sint32)upperRegionNewBase - (sint32)upperRegionOldBase;
	for(int i=3; i<9; ++i)
		pointers[i] += delta;

	// set VNTD to just below VVTP
	pointers[2] = pointers[3] - 1;

	// write in new VNT
	for(uint32 i=0; i<vntNewSize; ++i)
		g_sim.DebugGlobalWriteByte((pointers[1] + i) & 0xffff, vnt[i]);

	// rewrite pointers 2-9
	for(int i=2; i<9; ++i) {
		g_sim.DebugGlobalWriteByte(0x80 + 2*i, pointers[i] & 0xff);
		g_sim.DebugGlobalWriteByte(0x81 + 2*i, pointers[i] >> 8);
	}

	// reset APPMHI
	g_sim.DebugGlobalWriteByte(ATKernelSymbols::APPMHI, pointers[8] & 0xff);
	g_sim.DebugGlobalWriteByte(ATKernelSymbols::APPMHI + 1, pointers[8] >> 8);
}

void ATConsoleCmdBasicRebuildVvt(ATDebuggerCmdParser& parser) {
	parser >> 0;

	uint16 pointers[9];

	for(int i=0; i<9; ++i)
		pointers[i] = g_sim.DebugReadWord(0x80 + 2*i);

	if (pointers[3] < pointers[1])
		throw MyError("Invalid variable name table region.");

	if (pointers[4] < pointers[3])
		throw MyError("Invalid variable value table region.");

	if (pointers[6] < pointers[4])
		throw MyError("Invalid statement table region.");

	if (pointers[7] < pointers[6])
		throw MyError("Invalid string/array region.");

	if (pointers[8] < pointers[7])
		throw MyError("Invalid runtime stack region.");

	const uint16 vntp = pointers[1];
	const uint16 vvtp = pointers[3];
	const uint16 stmtab = pointers[4];

	if ((stmtab - vvtp) & 7)
		throw MyError("Invalid variable value table region ($%04X-%04X)", vvtp, stmtab - 1);

	vdfastvector<uint8> vnt;

	uint32 vvptr = vvtp;
	uint32 vnptr = vntp;
	int typecnt[3] = { 0, 0, 0 };
	uint8 varIdx = 0;
	while(vnptr < vvtp) {
		uint8 c = g_sim.DebugReadByte(vnptr++);

		if (!c)
			break;

		if (!(c & 0x80))
			continue;

		if (vvtp >= stmtab || stmtab - vvtp < 8) {
			ATConsoleWrite("WARNING: Reached end of VVT before end of VNT. Stopping.\n");
			break;
		}

		uint8 origMode = g_sim.DebugReadByte(vvptr);
		uint8 origIndex = g_sim.DebugReadByte(vvptr + 1);

		if (origIndex != varIdx) {
			ATConsolePrintf("Variable $%02X has invalid index. Fixing.\n", varIdx + 0x80);
			g_sim.DebugGlobalWriteByte(vvptr + 1, varIdx);
		}

		uint8 mode = origMode & 0x03;
		if (c == 0xA4)		// inverted $
			mode |= 0x80;
		else if (c == 0xA8)	// inverted (
			mode |= 0x40;
		else
			mode = 0;

		if (mode != origMode) {
			ATConsolePrintf("Variable $%02X has invalid mode. Correcting from $%02X to $%02X.\n", varIdx + 0x80, origMode, mode);
			g_sim.DebugGlobalWriteByte(vvptr, mode);
		}

		vvptr += 8;
		++varIdx;
	}
}

void ATConsoleCmdBasicSave(ATDebuggerCmdParser& parser) {
	ATDebuggerCmdString path(true);
	parser >> path >> 0;

	uint16 pointers[9];

	for(int i=0; i<9; ++i)
		pointers[i] = g_sim.DebugReadWord(0x80 + 2*i);

	// Atari BASIC revision B has a nasty bug that causes the argument stack region to
	// grow by 16 bytes every time it is written, so we check for < and not != here.
	if (pointers[1] - pointers[0] < 0x100)
		throw MyError("Invalid argument stack region.");

	if (pointers[3] < pointers[1])
		throw MyError("Invalid variable name table region.");

	if (pointers[4] < pointers[3])
		throw MyError("Invalid variable value table region.");

	if (pointers[6] < pointers[4])
		throw MyError("Invalid statement table region.");

	if (pointers[7] < pointers[6])
		throw MyError("Invalid string/array region.");

	if (pointers[8] < pointers[7])
		throw MyError("Invalid runtime stack region.");

	const uint16 vvtp = pointers[3];
	const uint16 stmtab = pointers[4];

	if ((stmtab - vvtp) & 7)
		throw MyError("Invalid variable value table region ($%04X-%04X)", vvtp, stmtab - 1);

	VDFile f(path->c_str(), nsVDFile::kWrite | nsVDFile::kDenyRead | nsVDFile::kCreateAlways);

	// check for Atari BASIC rev.B bug -- if we don't fix this, then we have to write
	// garbage at the end as all revs of Atari BASIC attempt a read of the STARP offset
	// minus 256 bytes.
	if (pointers[1] - pointers[0] > 0x100)
		ATConsolePrintf("WARNING: Program has oversized argument stack area of %u bytes due to BASIC rev.B bug. Fixing.\n", pointers[1] - pointers[0]);

	// convert pointers to relative form and write out 7 pointers
	uint16 relocptrs[7];
	for(int i=2; i<7; ++i)
		relocptrs[i] = pointers[i] - pointers[1] + 0x100;

	relocptrs[0] = 0;
	relocptrs[1] = 0x100;

	f.write(relocptrs, 14);

	// write out VNTP to STARP
	uint32 prglen = pointers[6] - pointers[1];
	vdblock<uint8> buf(prglen);

	for(uint32 i=0; i<prglen; ++i)
		buf[i] = g_sim.DebugReadByte(pointers[1] + i);

	f.write(buf.data(), prglen);

	ATConsolePrintf("Wrote %u bytes to %s.\n", prglen + 14, path->c_str());
}

void ATConsoleCmdMap(ATDebuggerCmdParser& parser) {
	parser >> 0;

	ATMemoryManager& memman = *g_sim.GetMemoryManager();

	memman.DumpStatus();
}

void ATConsoleCmdEcho(ATDebuggerCmdParser& parser) {
	VDStringA line;

	while(const char *s = parser.GetNextArgument()) {
		if (!line.empty())
			line += ' ';

		const char *t = s + strlen(s);

		if (*s == '"') {
			++s;

			if (t != s && t[-1] == '"')
				--t;
		}

		line.append(s, t);
	}

	line += '\n';
	ATConsoleWrite(line.c_str());
}

VDStringA ATConsoleCmdPrintfCore(ATDebuggerCmdParser& parser) {
	VDStringA line;
	const char *s = parser.GetNextArgument();

	if (!s)
		return line;

	VDStringA format;
	if (*s == '"') {
		++s;

		const char *end = s + strlen(s);

		if (end != s && end[-1] == '"')
			--end;

		format.assign(s, end);
		s = format.c_str();
	} else if (!strcmp(s, "@ts"))
		s = g_debugger.GetTempString();

	for(;;) {
		char c = *s++;

		if (!c)
			break;

		if (c != '%') {
			line += c;
			continue;
		}

		// check for escape
		c = *s++;
		if (c == '%') {
			line += c;
			continue;
		}

		// parse flags
		bool zeroPad = false;
		bool useSign = false;
		bool positiveSpace = false;
		bool leftAlign = false;
		bool altForm = false;

		for(;;) {
			if (c == '0')
				zeroPad = true;
			else if (c == '#')
				altForm = true;
			else if (c == '+')
				useSign = true;
			else if (c == ' ')
				positiveSpace = true;
			else if (c == '-')
				leftAlign = true;
			else
				break;

			c = *s++;
		}

		// parse width
		uint32 width = 0;
		if (c == '*') {
			const char *widtharg = parser.GetNextArgument();
			if (!widtharg) {
				line += "<error: width value missing>";
				break;
			}

			vdautoptr<ATDebugExpNode> node;

			try {
				node = ATDebuggerParseExpression(widtharg, &g_debugger, ATGetDebugger()->GetExprOpts());
			} catch(const ATDebuggerExprParseException& ex) {
				line.append_sprintf("<error: %s>", ex.c_str());
				break;
			}

			const ATDebugExpEvalContext& ctx = g_debugger.GetEvalContext();
			sint32 widthval;
			if (!node->Evaluate(widthval, ctx)) {
				line.append("<evaluation error>");
				break;
			}

			// clamp the width value to something reasonable
			if (widthval < 0)
				widthval = 0;
			else if (widthval > 128)
				widthval = 128;

			width = widthval;

			c = *s++;
		} else if (c >= '1' && c <= '9') {
			do {
				width = (width * 10) + (unsigned)(c - '0');

				if (width >= 100)
					width = 100;

				c = *s++;
			} while(c >= '0' && c <= '9');
		}

		// check for precision
		int precision = -1;
		if (c == '.') {
			precision = 0;

			if (*s == '*') {
				++s;

				const char *precarg = parser.GetNextArgument();
				if (!precarg) {
					line += "<error: precision value missing>";
					break;
				}

				vdautoptr<ATDebugExpNode> node;

				try {
					node = ATDebuggerParseExpression(precarg, &g_debugger, ATGetDebugger()->GetExprOpts());
				} catch(const ATDebuggerExprParseException& ex) {
					line.append_sprintf("<error: %s>", ex.c_str());
					break;
				}

				const ATDebugExpEvalContext& ctx = g_debugger.GetEvalContext();
				sint32 precval;
				if (!node->Evaluate(precval, ctx)) {
					line.append("<evaluation error>");
					break;
				}

				// clamp the precision value to something reasonable
				if (precval < 0)
					precval = 0;
				else if (precval > 128)
					precval = 128;

				precision = precval;
				c = *s++;
			} else {
				for(;;) {
					c = *s++;

					if (c < '0' || c > '9')
						break;

					precision = (precision * 10) + (int)(c - '0');
					if (precision >= 100)
						precision = 100;
				}
			}
		}

		// evaluate value
		sint32 value = 0;

		const char *arg = parser.GetNextArgument();
		if (!arg) {
			line += "<error: value missing>";
			break;
		}

		vdautoptr<ATDebugExpNode> node;

		try {
			node = ATDebuggerParseExpression(arg, &g_debugger, ATGetDebugger()->GetExprOpts());
		} catch(const ATDebuggerExprParseException& ex) {
			line.append_sprintf("<error: %s>", ex.c_str());
			break;
		}

		const ATDebugExpEvalContext& ctx = g_debugger.GetEvalContext();
		if (!node->Evaluate(value, ctx)) {
			line.append("<evaluation error>");
			break;
		}

		// check for width modifier and truncate value appropriately
		if (c == 'h') {
			c = *s++;

			if (c == 'h') {
				value &= 0xff;
				c = *s++;
			} else {
				value &= 0xffff;
			}
		} else if (c == 'l') {
			c = *s++;
		}

		// process format character
		if (!c)
			break;

		switch(c) {
			case 'b':	// binary
				{
					// left align value
					uint32 digits = 32;

					if (!value)
						digits = 1;
					else {
						while(value >= 0) {
							value += value;
							--digits;
						}
					}

					// left-pad if necessary
					uint32 natWidth = digits;
					uint32 precPadWidth = 0;

					if (precision >= 0 && digits < (uint32)precision) {
						precPadWidth = (uint32)precision - digits;
						natWidth = precision;
					}

					char padChar = (zeroPad && precision < 0) ? '0' : ' ';
					uint32 padWidth = (natWidth < width) ? width - natWidth : 0;

					if (padWidth && !leftAlign) {
						do {
							line += padChar;
						} while(--padWidth);
					}

					while(precPadWidth--)
						line += '0';

					// shift out bits
					while(digits) {
						line += (char)('0' - (value >> 31));
						value += value;
						--digits;
					}

					if (padWidth) {
						do {
							line += padChar;
						} while(--padWidth);
					}
				}
				break;

			case 'c':	// ASCII character
				{
					// left-pad if necessary
					uint32 padWidth = (width > 1) ? width - 1 : 0;

					if (padWidth && !leftAlign) {
						do {
							line += ' ';
						} while(--padWidth);
					}

					value &= 0xff;
					if (value < 0x20 || value >= 0x7f)
						value = '.';

					line += (char)value;

					if (padWidth) {
						do {
							line += ' ';
						} while(--padWidth);
					}
				}
				break;

			case 'd':	// signed decimal
			case 'i':	// signed decimal
				{
					// left align value
					uint32 uvalue = (uint32)(value < 0 ? -value : value);
					uint32 digits;

					if (!uvalue)
						digits = 1;
					else if (uvalue >= 1000000000)
						digits = 10;
					else {
						digits = 9;

						while(uvalue < 100000000) {
							uvalue *= 10;
							--digits;
						}
					}

					// left-pad if necessary
					uint32 natWidth = digits;
					uint32 precPadWidth = 0;

					if (precision >= 0 && digits < (uint32)precision) {
						natWidth = precision;
						precPadWidth = (uint32)precision - digits;
					}

					if (useSign || positiveSpace || value < 0)
						++natWidth;

					char padChar = (zeroPad && precision < 0) ? '0' : ' ';
					uint32 padWidth = (natWidth < width) ? width - natWidth : 0;

					if (padWidth && !leftAlign) {
						do {
							line += padChar;
						} while(--padWidth);
					}

					if (value < 0)
						line += '-';
					else if (useSign)
						line += '+';
					else if (positiveSpace)
						line += ' ';

					while(precPadWidth--)
						line += '0';

					// shift out digits
					if (uvalue >= 1000000000) {
						line += uvalue / 1000000000;
						uvalue %= 1000000000;
					}

					while(digits) {
						line += (char)((uvalue / 100000000) + '0');
						uvalue %= 100000000;
						uvalue *= 10;
						--digits;
					}

					if (padWidth) {
						do {
							line += padChar;
						} while(--padWidth);
					}
				}
				break;

			case 'e':	// float (exponential)
			case 'f':	// float (natural)
			case 'g':	// float (general)
				{
					double d = ATDebugReadDecFloatAsBinary(g_sim.GetCPUMemory(), value);
					const char format[]={'%', '*', '.', '*', c, 0};

					line.append_sprintf(format, width, precision >= 0 ? precision : 10, d);
				}
				break;

			case 's':	// string
			case 'S':	// string
				{
					const bool useHighByte = (c == 'S');
					uint32 addrSpace = value & kATAddressSpaceMask;
					uint32 addrOffset = value;

					if (precision < 0)
						precision = 8;

					uint32 uprec = (uint32)precision;

					if (useHighByte) {
						for(uint32 i=0; i<uprec; ++i) {
							uint8 c = g_sim.DebugGlobalReadByte(((addrOffset + i) & kATAddressOffsetMask) + addrSpace);

							if (c & 0x80) {
								uprec = i + 1;
								break;
							}
						}
					}

					uint32 padWidth = uprec < width ? width - uprec : 0;

					if (padWidth && !leftAlign) {
						do {
							line += ' ';
						} while(--padWidth);
					}

					for(uint32 i=0; i<uprec; ++i) {
						uint8 c = g_sim.DebugGlobalReadByte((addrOffset++ & kATAddressOffsetMask) + addrSpace);

						if (useHighByte)
							c &= 0x7F;

						if (c < 0x20 || c > 0x7f)
							c = '.';

						line += (char)c;
					}

					if (padWidth) {
						do {
							line += ' ';
						} while(--padWidth);
					}
				}
				break;

			case 'u':	// unsigned decimal
				{
					// left align value
					uint32 uvalue = (uint32)value;
					uint32 digits;

					if (!uvalue)
						digits = 1;
					else if (uvalue >= 1000000000)
						digits = 10;
					else {
						digits = 9;

						while(uvalue < 100000000) {
							uvalue *= 10;
							--digits;
						}
					}

					// left-pad if necessary
					uint32 natWidth = digits;
					uint32 precPadWidth = 0;

					if (precision >= 0 && digits < (uint32)precision) {
						precPadWidth = (uint32)precision - digits;
						natWidth = precision;
					}

					char padChar = (zeroPad && precision < 0) ? '0' : ' ';
					uint32 padWidth = (natWidth < width) ? width - natWidth : 0;

					if (padWidth && !leftAlign) {
						do {
							line += padChar;
						} while(--padWidth);
					}

					while(precPadWidth--)
						line += '0';

					// shift out digits
					if (uvalue >= 1000000000) {
						line += (char)((uvalue / 1000000000) + '0');
						uvalue %= 1000000000;
						--digits;
					}

					while(digits) {
						line += (char)((uvalue / 100000000) + '0');
						uvalue %= 100000000;
						uvalue *= 10;
						--digits;
					}

					if (padWidth) {
						do {
							line += padChar;
						} while(--padWidth);
					}
				}
				break;

			case 'x':	// hexadecimal lowercase
			case 'X':	// hexadecimal uppercase
				{
					// left align value
					uint32 uvalue = (uint32)value;
					uint32 digits = 8;

					if (!uvalue)
						digits = 1;
					else {
						while(!(uvalue & 0xf0000000)) {
							uvalue <<= 4;

							--digits;
						}
					}

					// left-pad if necessary
					uint32 natWidth = digits;
					uint32 precPadWidth = 0;

					if (precision >= 0 && digits < (uint32)precision) {
						precPadWidth = (uint32)precision - digits;
						natWidth = precision;
					}

					if (altForm)
						natWidth += 2;

					char padChar = (zeroPad && precision < 0) ? '0' : ' ';
					uint32 padWidth = (natWidth < width) ? width - natWidth : 0;

					if (padWidth && !leftAlign) {
						do {
							line += padChar;
						} while(--padWidth);
					}

					if (altForm) {
						line += '0';
						line += c;
					}

					while(precPadWidth--)
						line += '0';

					// shift out digits
					static const char kHexTableLo[16] = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'a', 'b', 'c', 'd', 'e', 'f' };
					static const char kHexTableHi[16] = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F' };
					const char *hextab = (c == 'X') ? kHexTableHi : kHexTableLo;

					while(digits) {
						line += hextab[uvalue >> 28];
						uvalue <<= 4;
						--digits;
					}

					if (padWidth) {
						do {
							line += padChar;
						} while(--padWidth);
					}
				}
				break;

			case 'y':
				{
					// decode symbol
					ATDebuggerSymbol sym;
					VDStringA temp;
					if (g_debugger.LookupSymbol(value, kATSymbol_Any, sym)) {
						sint32 disp = (sint32)value - sym.mSymbol.mOffset;
						if (abs(disp) > 10)
							temp.sprintf("%s%c%X", sym.mSymbol.mpName, disp < 0 ? '-' : '+', abs(disp));
						else if (disp)
							temp.sprintf("%s%+d", sym.mSymbol.mpName, disp);
						else
							temp = sym.mSymbol.mpName;
					} else
						temp.sprintf("$%04X", value);

					// left-pad if necessary
					uint32 len = (uint32)temp.size();

					if (precision >= 0 && len > (uint32)precision)
						len = (uint32)precision;

					uint32 padWidth = (len < width) ? width - len : 0;

					if (padWidth && !leftAlign) {
						do {
							line += ' ';
						} while(--padWidth);
					}

					line.append(temp, 0, len);

					if (padWidth) {
						do {
							line += ' ';
						} while(--padWidth);
					}
				}
				break;

			default:
				line += "<invalid format mode>";
				continue;
		}
	}

	return line;
}

void ATConsoleCmdPrintf(ATDebuggerCmdParser& parser) {
	VDStringA s = ATConsoleCmdPrintfCore(parser);

	s += '\n';
	ATConsoleWrite(s.c_str());
}

void ATConsoleCmdSprintf(ATDebuggerCmdParser& parser) {
	VDStringA s = ATConsoleCmdPrintfCore(parser);

	g_debugger.SetTempString(s.c_str());
}

namespace {
	void LZCompress(vdfastvector<uint8>& dst, const uint8 *src, uint32 len) {
		vdblock<int> htchainbuf(65536);
		int *const htchain = htchainbuf.data();
		int ht[256];

		for(int i=0; i<256; ++i)
			ht[i] = -1;

		for(int i=0; i<65536; ++i)
			htchain[i] = -1;

		uint32 literals = 0;
		const uint8 *litptr = NULL;

		if (len < 4) {
			literals = len;
		} else {
			uint32 pos = 0;
			uint32 lenm3 = len > 3 ? len - 3 : 0;
			uint8 hc = src[0] + src[1] + src[2] + src[3];

			while(pos < lenm3) {
				// search hash chain
				int minpos = pos > 65536 ? (int)pos - 65536 : 0;
				const uint8 *curptr = src + pos;
				int bestlen = 3;
				int bestdist = 0;
				int maxmatch = len - pos;

				int testpos = ht[hc];
				while(testpos >= minpos) {
					const uint8 *testptr = src + testpos;

					if (testptr[bestlen] == curptr[bestlen]) {
						int matchlen = 0;

						while(matchlen < maxmatch && testptr[matchlen] == curptr[matchlen])
							++matchlen;

						if (matchlen >= 4) {
							if (matchlen > maxmatch)
								matchlen = maxmatch;

							int dist = (pos - testpos) - 1;
							if (matchlen > bestlen) {
								bestlen = matchlen;
								bestdist = dist;
							}
						}
					}

					int nextpos = htchain[testpos & 0xffff];
					if (nextpos >= testpos)
						break;

					testpos = nextpos;
				}

				if (bestlen >= 4) {
					const uint8 control = (literals >= 15 ? 0xf0 : literals << 4) + (bestlen >= 19 ? 0x0f : bestlen - 4);
					dst.push_back(control);

					// write literals
					uint32 count = literals;
					if (count >= 15) {
						count -= 15;

						while(count >= 255) {
							dst.push_back(255);
							count -= 255;
						}

						dst.push_back(count);
					}

					if (literals)
						dst.insert(dst.end(), litptr, litptr + literals);

					literals = 0;

					// write matchlen extension
					if (bestlen >= 19) {
						count = bestlen - 19;

						while(count >= 255) {
							dst.push_back(255);
							count -= 255;
						}

						dst.push_back(count);
					}

					// write offset
					dst.push_back((uint8)bestdist);
					dst.push_back((uint8)(bestdist >> 8));
				} else {
					if (!literals)
						litptr = src + pos;

					++literals;
					bestlen = 1;
				}

				do {
					htchain[pos & 0xffff] = ht[hc];
					ht[hc] = pos;

					hc -= src[pos];
					hc += src[pos + 4];
					++pos;
				} while(--bestlen);
			}

			if (!literals)
				litptr = src + pos;
			literals += len - pos;
		}

		if (literals < 15) {
			dst.push_back(literals << 4);

			if (literals)
				dst.insert(dst.end(), litptr, litptr + literals);
		} else {
			uint32 count = literals;

			dst.push_back(0xf0);
			count -= 15;

			while(count >= 0xff) {
				dst.push_back(0xff);
				count -= 0xff;
			}

			dst.push_back(count);
			dst.insert(dst.end(), litptr, litptr + literals);
		}

		// test compressed data
#ifdef DEBUG
		const size_t plen = dst.size();
		const uint8 *psrc = dst.data();
		const uint8 *p = psrc;
		uint32 ulen = 0;

		while(p < psrc + plen) {
			VDASSERT(p - psrc <= ulen);

			uint8 c = *p++;
			uint32 litlen = c >> 4;

			if (litlen >= 15) {
				do {
					litlen += *p++;
				} while(p[-1] == 0xff);
			}

			VDASSERT(!memcmp(src + ulen, p, litlen));

			ulen += litlen;
			p += litlen;

			if (ulen == len)
				break;

			VDASSERT(ulen <= len);

			uint32 matchlen = (c & 15) + 4;
			
			if (matchlen >= 19) {
				do {
					matchlen += *p++;
				} while(p[-1] == 0xff);
			}

			uint32 dist = VDReadUnalignedLEU16(p);
			p += 2;

			VDASSERT(ulen >= dist+1);
			VDASSERT(!memcmp(src + ulen, src + ulen - 1 - dist, matchlen));

			ulen += matchlen;
			VDASSERT(ulen <= len);
		}
#endif
	}
}

void ATConsoleCmdDumpSnap(ATDebuggerCmdParser& parser) {
	ATDebuggerCmdName name(true);
	ATDebuggerCmdSwitch swUncompressed("u", false);
	parser >> swUncompressed >> name >> 0;

	vdfastvector<uint8> buf;
	ATLoadMiscResource(IDR_DISKLOADER128, buf);

	vdfastvector<uint8> rawMemory(0x10000);
	uint8 *mem = rawMemory.data();
	memcpy(mem, g_sim.GetRawMemory(), 0x10000);

	ATCPUEmulator& cpu = g_sim.GetCPU();
	uint8 *p = buf.data();

	uint8 *gtiabase = p + VDReadUnalignedLEU16(&p[0x314]);
	uint8 *pokeybase = gtiabase + 0x1e;
	uint8 *anticbase = gtiabase + 0x28;

	ATGTIARegisterState gtstate;
	g_sim.GetGTIA().GetRegisterState(gtstate);

	memcpy(gtiabase, gtstate.mReg, 30);

	ATAnticRegisterState anstate;
	g_sim.GetAntic().GetRegisterState(anstate);

	anticbase[0] = anstate.mCHACTL;
	anticbase[1] = anstate.mDLISTL;
	anticbase[2] = anstate.mDLISTH;
	anticbase[3] = anstate.mHSCROL;
	anticbase[4] = anstate.mVSCROL;
	anticbase[5] = 0;
	anticbase[6] = anstate.mPMBASE;
	anticbase[7] = 0;
	anticbase[8] = anstate.mCHBASE;

	ATPokeyRegisterState postate;
	g_sim.GetPokey().GetRegisterState(postate);

	memcpy(pokeybase, postate.mReg, 9);
	pokeybase[9] = postate.mReg[0x0F];		// SKCTL

	uint8 regS = cpu.GetS();
	const uint8 stubSize = p[0x031c];
	const uint8 stubOffset = (uint8)(regS - 2) < stubSize ? 0x100 - stubSize : regS - 2 - stubSize;

	// Write return address and flags for RTI. Note that we may possibly wrap around the
	// stack doing this.
	uint32 pc = cpu.GetInsnPC();
	mem[0x100 + regS] = (uint8)(pc >> 8);
	--regS;
	mem[0x100 + regS] = (uint8)pc;
	--regS;
	mem[0x100 + regS] = cpu.GetP();
	--regS;

	ATPIAState piastate;
	g_sim.GetPIA().GetState(piastate);

	const uint8 dataToInject[]={
		cpu.GetA(),
		cpu.GetX(),
		cpu.GetY(),
		regS,
		(uint8)stubOffset,
		(uint8)(piastate.mCRB ^ 0x04),
		piastate.mCRB & 0x04 ? piastate.mDDRB : piastate.mORB,
		piastate.mCRB & 0x04 ? piastate.mORB : piastate.mDDRB,
		anstate.mDMACTL,
		anstate.mNMIEN,
	};

	for(uint8 i = 0; i < (uint8)sizeof(dataToInject); ++i)
		buf[VDReadUnalignedLEU16(p + 0x300 + i*2) % buf.size()] = dataToInject[i];

	memcpy(mem + 0x100 + stubOffset, p + 0x031d, stubSize);

	VDFile f(name->c_str(), nsVDFile::kWrite | nsVDFile::kDenyRead | nsVDFile::kCreateAlways | nsVDFile::kSequential);

	const uint8 kATRHeader[]={
		0x96, 0x02,
		0x00, 0x00,
		0x80, 0x00,
		0x00,
		0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
	};

	vdfastvector<uint8> diskImage;
	diskImage.insert(diskImage.end(), kATRHeader, kATRHeader + sizeof kATRHeader);
	diskImage.insert(diskImage.end(), p, p + 0x300);

	static const struct RangeInfo {
		uint16 mRealAddress1;
		uint16 mLength1;
		uint16 mRealAddress2;
		uint16 mLength2;
		uint16 mUnpackAddress;
		uint16 mLZLoadAddress;
	} kRanges[]={
		{ 0xC000, 0x1000, 0xD800, 0x2800, 0x4000, 0x4100 },	// $4100-78FF -> $4000-77FF
		{ 0x0A00, 0xB500, 0x0000, 0x0000, 0x0A00, 0x0B00 },	// $0B00-BFFF -> $0A00-BEFF
		{ 0x0000, 0x0A00, 0xBF00, 0x0100, 0x4000, 0x4100 },	// $4100-4BFF -> $4000-4AFF -> $0000-09FF, $BF00-BFFF
	};

	vdfastvector<uint8> packSrcBuf;
	vdfastvector<uint8> packBuf;
	for(size_t i=0; i<vdcountof(kRanges); ++i) {
		const uint32 blklen1 = kRanges[i].mLength1;
		const uint32 blklen2 = kRanges[i].mLength2;
		const uint32 blklen = blklen1 + blklen2;

		packSrcBuf.resize(blklen);

		memcpy(packSrcBuf.data(), mem + kRanges[i].mRealAddress1, blklen1);
		memcpy(packSrcBuf.data() + blklen1, mem + kRanges[i].mRealAddress2, blklen2);

		if (!swUncompressed)
			LZCompress(packBuf, packSrcBuf.data(), blklen);

		const uint32 packLen = (uint32)packBuf.size();

		struct LoaderRangeInfo {
			uint8 mPageStart;
			uint8 mPageCount;
			uint8 mLoadPageStart;
			uint8 mLoadPageCount;
			uint16 mLZStart;
		} lrinfo = {0};

		if (swUncompressed || packLen > blklen - 0x100) {
			diskImage.insert(diskImage.end(), packSrcBuf.begin(), packSrcBuf.end());

			lrinfo.mPageStart = (uint8)(kRanges[i].mUnpackAddress >> 8);
			lrinfo.mPageCount = (uint8)(blklen >> 8);
			lrinfo.mLoadPageStart = lrinfo.mPageStart;
			lrinfo.mLoadPageCount = lrinfo.mPageCount;
			lrinfo.mLZStart = 0;

			ATConsolePrintf("$%04X-%04X -> uncompressed\n", kRanges[i].mRealAddress1, kRanges[i].mRealAddress1 + blklen - 1);
		} else {
			// pad upward to next page boundary
			if (packLen & 0xff)
				diskImage.resize(diskImage.size() + (0x100 - (packLen & 0xff)), 0);

			// write packed data
			diskImage.insert(diskImage.end(), packBuf.begin(), packBuf.end());

			// update metadata
			const uint32 pageCount = (packLen + 0xff) >> 8;

			lrinfo.mPageStart = (uint8)(kRanges[i].mUnpackAddress >> 8);
			lrinfo.mPageCount = (uint8)(blklen >> 8);
			lrinfo.mLoadPageStart = (uint8)(((kRanges[i].mLZLoadAddress + blklen) >> 8) - pageCount);
			lrinfo.mLoadPageCount = (uint8)pageCount;
			lrinfo.mLZStart = VDToLE16(kRanges[i].mLZLoadAddress + blklen - packLen);

			ATConsolePrintf("$%04X-%04X -> $%04X\n", kRanges[i].mRealAddress1, kRanges[i].mRealAddress1 + blklen - 1, packLen);
		}

		// write metadata
		memcpy(diskImage.data() + 0x10 + VDReadUnalignedLEU16(&p[0x316]) + 6*i, &lrinfo, 6);

		packBuf.clear();
	}

	// update load progress
	const uint32 totalSectorCount = ((uint32)diskImage.size() - 0x310) >> 7;
	const uint32 progressStep = 0x1400 / totalSectorCount;

	diskImage[0x10 + VDReadUnalignedLEU16(&p[0x318])] = (uint8)progressStep;
	diskImage[0x10 + VDReadUnalignedLEU16(&p[0x31a])] = (uint8)(progressStep >> 8);

	// update paragraph count in ATR header
	VDWriteUnalignedLEU16(diskImage.data() + 2, (uint16)((diskImage.size() - 0x10) >> 4));

	f.write(diskImage.data(), (long)diskImage.size());

	ATConsolePrintf("Booter written to: %s\n", name->c_str());
}

void ATConsoleCmdEval(const char *s) {
	if (!s) {
		ATConsoleWrite("Missing expression. (Use .help if you want command help.)\n");
		return;
	}

	const ATDebugExpEvalContext& ctx = g_debugger.GetEvalContext();

	vdautoptr<ATDebugExpNode> node;

	try {
		ATDebuggerExprParseOpts opts = ATGetDebugger()->GetExprOpts();

		// We always assume an expression here.
		opts.mbAllowUntaggedHex = false;

		node = ATDebuggerParseExpression(s, &g_debugger, opts);
	} catch(ATDebuggerExprParseException& ex) {
		ATConsolePrintf("Unable to parse expression: %s\n", ex.c_str());
		return;
	}

	ATConsolePrintf("%s = ", s);

	sint32 result;
	if (!node)
		ATConsoleWrite("(parse error)\n");
	else if (!node->Evaluate(result, ctx))
		ATConsoleWrite("(evaluation error)\n");
	else if (node->IsAddress() && (result & kATAddressSpaceMask))
		ATConsolePrintf("%d (%s)\n", result, g_debugger.GetAddressText(result, true, true).c_str());
	else
		ATConsolePrintf("%d ($%0*X)\n", result, (uint32)result >= 0x10000 ? 8 : 4, result);
}

void ATConsoleCmdDumpHelp(ATDebuggerCmdParser& parser) {
	vdfastvector<uint8> helpdata;
	if (!ATLoadMiscResource(IDR_DEBUG_HELP, helpdata)) {
		ATConsoleWrite("Unable to load help.\n");
		return;
	}

	const char *cmd = parser.GetNextArgument();

	VDMemoryStream ms(helpdata.data(), (uint32)helpdata.size());
	VDTextStream ts(&ms);

	bool enabled = !cmd;
	bool anyout = false;

	for(;;) {
		const char *s = ts.GetNextLine();

		if (!s)
			break;

		char c = *s;

		if (c) {
			++s;

			if (*s == ' ')
				++s;

			if (c == '^' || c == '+' || c == '>') {
				if (cmd) {
					if (c == '>')
						continue;

					if (enabled)
						break;

					const char *t = s;
					for(;;) {
						const char *cmdstart = t;
						while(*t && *t != ' ' && *t != ',')
							++t;
						const char *cmdend = t;

						VDStringSpanA cmdcheck(cmdstart, cmdend);

						if (cmdcheck.comparei(cmd) == 0)
							enabled = true;

						if (*t != ',')
							break;

						++t;
						while(*t == ' ')
							++t;
					}
				} else if (c == '^') {
					continue;
				}
			} else if (c == '!') {
				if (cmd)
					continue;
			} else if (c != '.') {
				if (!cmd)
					continue;

				if (enabled)
					anyout = true;
			}
		} else if (!cmd)
			continue;

		if (!enabled)
			continue;

		ATConsolePrintf("%s\n", s);
	}

	if (cmd && !anyout) {
		ATConsoleWrite("\n");
		ATConsolePrintf("  No detailed help available for command: %s.\n", cmd);
	}
}

void ATConsoleCmdDumpSIO(ATDebuggerCmdParser& parser) {
	ATDebuggerCmdSwitch brief("b", false);
	parser >> brief >> 0;

	uint8 dcb[12];

	for(int i=0; i<12; ++i)
		dcb[i] = g_sim.DebugReadByte(ATKernelSymbols::DDEVIC + i);

	if (brief) {
		VDStringA s;
		s.sprintf("SIO: Device $%02X:%02X, Command $%02X:$%04X"
			, dcb[0], dcb[1], dcb[2]
			, VDReadUnalignedLEU16(&dcb[10])
		);

		if (dcb[3] & 0xc0) {
			switch(dcb[3] & 0xc0) {
				case 0x40:
					s += ", Read ";
					break;
				case 0x80:
					s += ", Write";
					break;
				case 0xc0:
					s += ", R/W  ";
					break;
			}

			s.append_sprintf(" len $%04X -> $%04X"
				, VDReadUnalignedLEU16(&dcb[8])
				, VDReadUnalignedLEU16(&dcb[4]));
		}

		s += '\n';
		ATConsoleWrite(s.c_str());
	} else {
		ATConsolePrintf("DDEVIC    Device ID   = $%02x\n", dcb[0]);
		ATConsolePrintf("DUNIT     Device unit = $%02x\n", dcb[1]);
		ATConsolePrintf("DCOMND    Command     = $%02x\n", dcb[2]);
		ATConsolePrintf("DSTATS    Status      = $%02x\n", dcb[3]);
		ATConsolePrintf("DBUFHI/LO Buffer      = $%04x\n", dcb[4] + 256 * dcb[5]);
		ATConsolePrintf("DTIMLO    Timeout     = $%02x\n", dcb[6]);
		ATConsolePrintf("DBYTHI/LO Length      = $%04x\n", dcb[8] + 256 * dcb[9]);
		ATConsolePrintf("DAUXHI/LO Sector      = $%04x\n", dcb[10] + 256 * dcb[11]);
	}
}

void ATConsoleCmdDumpPCLink(ATDebuggerCmdParser& parser) {
	parser >> 0;

	IATDevice *pcl = g_sim.GetDeviceManager()->GetDeviceByTag("pclink");

	if (!pcl) {
		ATConsoleWrite("PCLink is not active.\n");
		return;
	}

	ATDebuggerConsoleOutput conout;
	if (auto *p = vdpoly_cast<IATDeviceDiagnostics *>(pcl))
		p->DumpStatus(conout);
}

void ATConsoleCmdSDXLoadSymbols(ATDebuggerCmdParser& parser) {
	ATDebuggerCmdExprAddr addr(false, false);
	parser >> addr >> 0;

	uint16 linkAddr = addr.IsValid() ? addr.GetValue() : g_sim.DebugReadWord(ATKernelSymbols::DOSVEC) + 0x127;

	linkAddr = g_sim.DebugReadWord(linkAddr);

	uint32 prevSdxModuleId = g_debugger.GetCustomModuleIdByShortName("sdx");
	if (prevSdxModuleId)
		g_debugger.UnloadSymbols(prevSdxModuleId);

	uint32 sdxModuleId = g_debugger.AddCustomModule(0, "SpartaDOS X Symbol Table", "sdx");
	uint32 found = 0;

	vdhashset<uint16> foundAddresses;
	while(linkAddr >= 0x0200 && linkAddr < 0xfffa - 13) {
		if (!foundAddresses.insert(linkAddr).second)
			break;

		uint8 symbol[13];
		for(int i=0; i<13; ++i)
			symbol[i] = g_sim.DebugReadByte(linkAddr + i);

		// validate name
		for(int i=0; i<8; ++i) {
			uint8 c = symbol[2 + i];

			if (c < 0x20 || c >= 0x7f)
				goto stop;
		}

		// parse out name
		const char *s = (const char *)(symbol + 2);
		const char *t = (const char *)(symbol + 10);

		while(t > s && t[-1] == ' ')
			--t;

		if (t == s)
			goto stop;

		// add symbol
		VDStringA name(s, t);
		g_debugger.AddCustomSymbol(VDReadUnalignedLEU16(symbol + 11), 1, name.c_str(), kATSymbol_Any, sdxModuleId);
		++found;

		linkAddr = VDReadUnalignedLEU16(symbol);
	}

stop:
	ATConsolePrintf("%u symbols added.\n", found);
}

void ATConsoleCmdIDE(ATDebuggerCmdParser& parser) {
	parser >> 0;

	auto *ide = g_sim.GetDeviceManager()->GetInterface<ATIDEEmulator>();
	if (!ide) {
		ATConsoleWrite("IDE not active.\n");
		return;
	}

	ide->DumpStatus();
}

void ATConsoleCmdIDEDumpSec(ATDebuggerCmdParser& parser) {
	ATDebuggerCmdExprNum num(true, false, 0);
	ATDebuggerCmdSwitch swL("l", false);
	parser >> swL >> num >> 0;

	auto *ide = g_sim.GetDeviceManager()->GetInterface<ATIDEEmulator>();
	if (!ide) {
		ATConsoleWrite("IDE not active.\n");
		return;
	}

	uint8 buf[512];
	ide->DebugReadSector(num.GetValue(), buf, 512);

	VDStringA s;
	int step = swL ? 2 : 1;
	for(int i=0; i<512; i += 32) {
		s.sprintf("%03x:", swL ? i >> 1 : i);
		
		for(int j=0; j<32; j += step)
			s.append_sprintf(" %02x", buf[i+j]);

		s += " |";
		for(int j=0; j<32; j += step) {
			uint8 c = buf[i+j];

			if ((uint32)(c - 0x20) >= 0x5f)
				c = '.';

			s += c;
		}

		s += "|\n";

		ATConsoleWrite(s.c_str());
	}
}

void ATConsoleCmdIDEReadSec(ATDebuggerCmdParser& parser) {
	ATDebuggerCmdExprNum num(true, false, 0);
	ATDebuggerCmdExprAddr addr(true, true, false);
	ATDebuggerCmdSwitch swL("l", false);
	parser >> swL >> num >> addr >> 0;

	auto *ide = g_sim.GetDeviceManager()->GetInterface<ATIDEEmulator>();
	if (!ide) {
		ATConsoleWrite("IDE not active.\n");
		return;
	}

	uint8 buf[512];
	ide->DebugReadSector(num.GetValue(), buf, 512);

	uint32 addrhi = addr.GetValue() & kATAddressSpaceMask;
	uint32 addrlo = addr.GetValue();

	int step = swL ? 2 : 1;
	for(int i=0; i<512; i += step) {
		g_sim.DebugGlobalWriteByte(addrhi + (addrlo & kATAddressOffsetMask), buf[i]);
		++addrlo;
	}
}

void ATConsoleCmdIDEWriteSec(ATDebuggerCmdParser& parser) {
	ATDebuggerCmdExprNum num(true, false, 0);
	ATDebuggerCmdExprAddr addr(true, true, false);
	ATDebuggerCmdSwitch swL("l", false);
	parser >> swL >> num >> addr >> 0;

	auto *ide = g_sim.GetDeviceManager()->GetInterface<ATIDEEmulator>();
	if (!ide) {
		ATConsoleWrite("IDE not active.\n");
		return;
	}

	uint32 addrhi = addr.GetValue() & kATAddressSpaceMask;
	uint32 addrlo = addr.GetValue();

	uint8 buf[512];
	if (swL) {
		for(int i=0; i<512; i += 2) {
			buf[i] = g_sim.DebugGlobalReadByte(addrhi + (addrlo & kATAddressOffsetMask));
			buf[i+1] = 0xFF;

			++addrlo;
		}
	} else {
		for(int i=0; i<512; ++i) {
			buf[i] = g_sim.DebugGlobalReadByte(addrhi + (addrlo & kATAddressOffsetMask));

			++addrlo;
		}
	}

	ide->DebugWriteSector(num.GetValue(), buf, 512);
}

void ATConsoleCmdRunBatchFile(ATDebuggerCmdParser& parser) {
	ATDebuggerCmdPath path(true);
	parser >> path >> 0;

	g_debugger.QueueBatchFile(VDTextAToW(path->c_str()).c_str());
}

void ATConsoleCmdOnExeLoad(ATDebuggerCmdParser& parser) {
	VDStringA cmd;
	ATDebuggerSerializeArgv(cmd, parser);

	g_debugger.OnExeQueueCmd(false, cmd.c_str());
}

void ATConsoleCmdOnExeRun(ATDebuggerCmdParser& parser) {
	VDStringA cmd;
	ATDebuggerSerializeArgv(cmd, parser);

	g_debugger.OnExeQueueCmd(true, cmd.c_str());
}

void ATConsoleCmdOnExeClear(ATDebuggerCmdParser& parser) {
	VDStringA cmd;
	ATDebuggerSerializeArgv(cmd, parser);

	g_debugger.OnExeClear();
	ATConsoleWrite("On-EXE commands cleared.\n");
}

void ATConsoleCmdOnExeList(ATDebuggerCmdParser& parser) {
	parser >> 0;

	VDStringA s;

	for(int i=0; i<2; ++i) {
		if (i)
			ATConsoleWrite("Executed prior to EXE run:\n");
		else
			ATConsoleWrite("Executed prior to EXE load:\n");

		for(int j=0; g_debugger.OnExeGetCmd(i != 0, j, s); ++j)
			ATConsolePrintf("    %s\n", s.c_str());

		ATConsoleWrite("\n");
	}
}

void ATConsoleCmdSourceMode(ATDebuggerCmdParser& parser) {
	ATDebuggerCmdName name(false);
	parser >> name >> 0;

	if (name.IsValid()) {
		if (*name == "on") {
			g_debugger.SetSourceMode(kATDebugSrcMode_Source);
		} else if (*name == "off") {
			g_debugger.SetSourceMode(kATDebugSrcMode_Disasm);
		} else
			throw MyError("Unknown source mode: %s\n", name->c_str());
	}

	ATConsolePrintf("Source debugging mode is now %s.\n", g_debugger.IsSourceModeEnabled() ? "on" : "off");
}

void ATConsoleCmdDS1305(ATDebuggerCmdParser& parser) {
	parser >> 0;

	IATDevice *side = g_sim.GetDeviceManager()->GetDeviceByTag("side");

	if (!side)
		side = g_sim.GetDeviceManager()->GetDeviceByTag("side2");

	ATUltimate1MBEmulator *ult = g_sim.GetUltimate1MB();

	if (!side && !ult)
		throw MyError("Neither SIDE nor Ultimate1MB are enabled.");

	if (side) {
		ATConsoleWrite("\nSIDE:\n");

		ATDebuggerConsoleOutput conout;
		if (auto *p = vdpoly_cast<IATDeviceDiagnostics *>(side))
			p->DumpStatus(conout);
	}

	if (ult) {
		ATConsoleWrite("\nUltimate1MB:\n");
		
		ATDebuggerConsoleOutput output;
		ult->DumpRTCStatus(output);
	}
}

void ATConsoleCmdTape(ATDebuggerCmdParser& parser) {
	parser >> 0;

	ATCassetteEmulator& tape = g_sim.GetCassette();
	if (!tape.IsLoaded())
		throw MyError("No cassette tape mounted.");

	ATConsolePrintf("Current position:  %u/%u (%.3fs / %.3fs)\n"
		, tape.GetSamplePos()
		, tape.GetSampleLen()
		, tape.GetPosition()
		, tape.GetLength());

	ATConsolePrintf("Motor state:       %s / %s / %s\n"
		, tape.IsPlayEnabled() ? "play" : "stop"
		, tape.IsMotorEnabled() ? "enabled" : "disabled"
		, tape.IsMotorRunning() ? "running" : "stopped");
}

void ATConsoleCmdTapeData(ATDebuggerCmdParser& parser) {
	ATDebuggerCmdSwitch swD("d", false);
	ATDebuggerCmdSwitch swT("t", false);
	ATDebuggerCmdSwitchNumArg swB("b", 1, 6000, 600);
	ATDebuggerCmdSwitchNumArg swR("r", -10000000, +10000000);
	ATDebuggerCmdSwitchNumArg swP("p", 0, +10000000);
	ATDebuggerCmdSwitchNumArg swS("s", 0, +10000000);
	ATDebuggerCmdSwitch swY("y", false);
	ATDebuggerCmdLength lengthArg(0, false, nullptr);
	parser >> swD >> swT >> swB >> swP >> swS >> swY >> lengthArg;
	if (!swP.IsValid())
		parser >> swR;
	parser >> 0;

	ATCassetteEmulator& tape = g_sim.GetCassette();
	IATCassetteImage *pImage = tape.GetImage();
	if (!tape.IsLoaded() || !pImage)
		throw MyError("No cassette tape mounted.");

	uint32 pos = tape.GetSamplePos();
	uint32 len = tape.GetSampleLen();

	if (swR.IsValid()) {
		sint32 deltapos = VDRoundToInt32((float)swR.GetValue() * kATCassetteDataSampleRate / 1000.0f);

		if (deltapos < 0) {
			if (pos <= (uint32)-deltapos)
				pos = 0;
			else
				pos += deltapos;
		} else
			pos += deltapos;
	} else if (swP.IsValid()) {
		pos = VDRoundToInt32((float)swP.GetValue() * (kATCassetteDataSampleRate / 1000.0f));
	} else if (swS.IsValid()) {
		pos = swS.GetValue();
	}

	const bool bypassFSK = swY;

	if (swT || swB.IsValid()) {
		uint32 poslimit = pos + (int)(kATCassetteDataSampleRate * 30);
		if (poslimit > len)
			poslimit = len;

		uint32 replimit = lengthArg.IsValid() ? lengthArg : 50;
		bool first = true;
		bool prevBit = true;

		uint32 pos2 = pos;

		if (swB.IsValid()) {
			const int bitsPerSampleFP8 = (int)((float)swB.GetValue() / kATCassetteDataSampleRate * 256 + 0.5f);
			int stepAccum = 0;
			uint32 bitIdx = 0;
			bool bitPhase = false;
			bool dataOnly = swD;
			uint8 data = 0;

			uint32 lastDataPos = pos2;

			const int kMinGapReportTime = (int)(kATCassetteDataSampleRate / 10);

			VDStringA s;
			while(pos2 < poslimit) {
				bool nextBit = pImage->GetBit(pos2, 2, 1, prevBit, bypassFSK);

				stepAccum += bitsPerSampleFP8;

				if (nextBit != prevBit) {
					bitPhase = false;
					stepAccum = 0;
				}

				if (stepAccum >= 0x80) {
					stepAccum -= 0x80;

					bitPhase = !bitPhase;

					if (bitPhase) {
						if (!dataOnly || bitIdx == 9) {
							if ((int)(pos2 - lastDataPos) >= kMinGapReportTime) {
								ATConsolePrintf("-- gap of %u samples (%.1fms) --\n"
									, pos2 - lastDataPos
									, (float)(pos2 - lastDataPos) * 1000.0f / kATCassetteDataSampleRate);
							}

							lastDataPos = pos2;

							s.sprintf("%u (%.6fs / +%.6fs): bit[%u] = %c"
								, pos2
								, (float)pos2 / kATCassetteDataSampleRate
								, (float)(pos2 - pos) / kATCassetteDataSampleRate
								, bitIdx
								, nextBit ? '1' : '0');
						}

						bool dataByte = false;

						if (bitIdx == 0) {
							// start bit -- must be space
							if (!nextBit)
								++bitIdx;
						} else if (bitIdx < 9) {
							// data bit -- can be zero or one
							++bitIdx;
							data = (data >> 1) + (nextBit ? 0x80 : 0x00);
						} else if (bitIdx == 9) {
							// stop bit -- must be mark bit
							if (nextBit) {
								s.append_sprintf(" | data = $%02x (ok)", data);
								bitIdx = 0;
							} else {
								s.append_sprintf(" | data = $%02x (framing error)", data);
								++bitIdx;
							}

							dataByte = true;
						} else {
							if (nextBit)
								bitIdx = 0;
						}

						if (!dataOnly || dataByte) {
							s += '\n';
							ATConsoleWrite(s.c_str());

							if (!--replimit)
								break;
						}
					}
				}

				prevBit = nextBit;
				first = false;
				++pos2;
			}
		} else {
			uint32 lastTransition = pos2;

			while(pos2 < poslimit) {
				bool nextBit = pImage->GetBit(pos2, 2, 1, prevBit, bypassFSK);

				if (first || nextBit != prevBit) {

					ATConsolePrintf("%u (%.6fs) | %+4u (+%.6fs) [%+4u]: %c\n"
						, pos2
						, (float)pos2 / kATCassetteDataSampleRate
						, pos2 - pos
						, (float)(pos2 - pos) / kATCassetteDataSampleRate
						, pos2 - lastTransition
						, nextBit ? '1' : '0');

					first = false;
					prevBit = nextBit;
					lastTransition = pos2;

					if (!--replimit)
						break;
				}

				++pos2;
			}
		}

		if (pos2 >= len)
			ATConsoleWrite("End of tape reached.\n");
	} else {
		char buf[62];
		bool bit = true;

		for(uint32 i = 0; i < 61; ++i) {
			uint32 pos2 = pos + i;

			if (pos2 < 30)
				buf[i] = '.';
			else {
				pos2 -= 30;

				if (pos2 >= len) {
					buf[i] = '.';
				} else {
					bit = pImage->GetBit(pos2, 2, 1, bit, bypassFSK);

					buf[i] = bit ? '1' : '0';
				}
			}
		}

		buf[61] = 0;

		ATConsolePrintf("%s\n", buf);
		ATConsolePrintf("%30s^ %u/%u\n", "", pos, len);
	}
}

void ATConsoleCmdSID(ATDebuggerCmdParser& parser) {
	parser >> 0;

	IATDevice *sid = g_sim.GetDeviceManager()->GetDeviceByTag("slightsid");

	if (!sid) {
		ATConsoleWrite("SlightSID is not active.\n");
		return;
	}

	ATDebuggerConsoleOutput conout;
	if (auto *p = vdpoly_cast<IATDeviceDiagnostics *>(sid))
		p->DumpStatus(conout);
}

void ATConsoleCmdCovox(ATDebuggerCmdParser& parser) {
	parser >> 0;

	IATDevice *covox = g_sim.GetDeviceManager()->GetDeviceByTag("covox");

	if (!covox) {
		ATConsoleWrite("Covox is not active.\n");
		return;
	}

	ATDebuggerConsoleOutput conout;
	if (auto *p = vdpoly_cast<IATDeviceDiagnostics *>(covox))
		p->DumpStatus(conout);
}

void ATConsoleCmdUltimate(ATDebuggerCmdParser& parser) {
	parser >> 0;

	ATUltimate1MBEmulator *ult = g_sim.GetUltimate1MB();

	if (!ult) {
		ATConsoleWrite("Ultimate1MB is not active.\n");
		return;
	}

	ATDebuggerConsoleOutput output;
	ult->DumpStatus(output);
}

void ATConsoleCmdPBI(ATDebuggerCmdParser& parser) {
	ATPBIManager& pbi = g_sim.GetPBIManager();

	ATConsolePrintf("PBI select register:   $%02x\n", pbi.GetSelectRegister());
	ATConsolePrintf("PBI math pack overlay: %s\n", pbi.IsROMOverlayActive() ? "enabled" : "disabled");
}

void ATConsoleCmdBase(ATDebuggerCmdParser& parser) {
	ATDebuggerCmdName name(false);

	parser >> name >> 0;

	ATDebuggerExprParseOpts opts(g_debugger.GetExprOpts());

	if (name.IsValid()) {
		if (*name == "dec" || *name == "10") {
			opts.mbAllowUntaggedHex = false;
			opts.mbDefaultHex = false;

		} else if (*name == "hex" || *name == "16") {
			opts.mbAllowUntaggedHex = true;
			opts.mbDefaultHex = true;

		} else if (*name == "mixed") {
			opts.mbAllowUntaggedHex = true;
			opts.mbDefaultHex = false;

		} else {
			throw MyError("Unrecognized number base mode: %s.", name->c_str());
		}
	}

	g_debugger.SetExprOpts(opts);

	if (opts.mbDefaultHex)
		ATConsoleWrite("Numeric base is set to hex.\n");
	else if (opts.mbAllowUntaggedHex)
		ATConsoleWrite("Numeric base is set to mixed.\n");
	else
		ATConsoleWrite("Numeric base is set to decimal.\n");
}

void ATConsoleCmdReload(ATDebuggerCmdParser& parser) {
	parser >> 0;

	g_debugger.ReloadModules();
}

void ATConsoleCmdNetstat(ATDebuggerCmdParser& parser) {
	parser >> 0;

	IATDevice *dc = g_sim.GetDeviceManager()->GetDeviceByTag("dragoncart");

	if (!dc)
		throw MyError("No network emulation active.");

	ATDebuggerConsoleOutput conout;
	if (auto *p = vdpoly_cast<IATDeviceDiagnostics *>(dc))
		p->DumpStatus(conout);
}

void ATConsoleCmdNetPCap(ATDebuggerCmdParser& parser) {
	ATDebuggerCmdPath pathArg(true);

	parser >> pathArg >> 0;

	IATDevice *dc = g_sim.GetDeviceManager()->GetDeviceByTag("dragoncart");

	if (!dc)
		throw MyError("No network emulation active.");

	vdpoly_cast<ATDragonCartEmulator *>(dc)->OpenPacketTrace(VDTextAToW(pathArg->c_str()).c_str());

	ATConsolePrintf("Packet trace opened: %s\n", pathArg->c_str());
}

void ATConsoleCmdNetPCapClose(ATDebuggerCmdParser& parser) {
	parser >> 0;

	IATDevice *dc = g_sim.GetDeviceManager()->GetDeviceByTag("dragoncart");

	if (!dc)
		throw MyError("No network emulation active.");

	vdpoly_cast<ATDragonCartEmulator *>(dc)->ClosePacketTrace();
}

void ATConsoleCmdCIODevs(ATDebuggerCmdParser& parser) {
	parser >> 0;

	ATConsolePrintf("Device  Handler table address\n");
	for(int i=0; i<15*3; i += 3) {
		uint8 c = g_sim.DebugReadByte(ATKernelSymbols::HATABS + i);

		if (c < 0x20 || c >= 0x7F)
			break;

		uint16 addr = g_sim.DebugReadWord(ATKernelSymbols::HATABS + 1 + i);

		ATConsolePrintf("  %c:    %s\n", c, g_debugger.GetAddressText(addr, true, true).c_str());
	}
}

void ATConsoleCmdCRC(ATDebuggerCmdParser& parser) {
	ATDebuggerCmdSwitchExprNumArg initialArg("i", -0x7FFFFFFF-1, 0x7FFFFFFF, -1);
	ATDebuggerCmdExprAddr addrarg(true, true);
	ATDebuggerCmdLength lenarg(0, true, &addrarg);
	
	parser >> initialArg >> addrarg >> lenarg >> 0;

	uint32 addrspace = addrarg.GetValue() & kATAddressSpaceMask;
	uint32 addroffset = addrarg.GetValue() & kATAddressOffsetMask;

	IATDebugTarget *target = g_debugger.GetTarget();
	uint16 crc16 = (uint16)initialArg.GetValue();
	uint32 crc32 = (uint32)initialArg.GetValue();

	for(uint32 len = lenarg; len; --len) {
		uint8 c = target->DebugReadByte(addrspace + addroffset);
			
		crc16 ^= (uint16)c << 8;
		for(int i=0; i<8; ++i) {
			crc16 = (crc16 << 1) ^ (crc16 & 0x8000 ? 0x1021 : 0);
		}

		crc32 ^= c;
		for(int i=0; i<8; ++i) {
			crc32 = (crc32 >> 1) ^ (crc32 & 1 ? 0xedb88320 : 0);
		}

		addroffset = (addroffset + 1) & kATAddressOffsetMask;
	}

	crc32 = ~crc32;

	ATConsolePrintf("%s + L%X:\n", g_debugger.GetAddressText(addrarg.GetValue(), true).c_str(), (uint32)lenarg);
	ATConsolePrintf("CRC-16-CCITT   $%04X\n", crc16);
	ATConsolePrintf("CRC-32         $%08X\n", crc32);
}

void ATConsoleCmdSum(ATDebuggerCmdParser& parser) {
	ATDebuggerCmdExprAddr addrarg(true, true);
	ATDebuggerCmdSwitch wordsw("w", false);
	ATDebuggerCmdLength lenarg(0, true, &addrarg);
	
	parser >> wordsw >> addrarg >> lenarg >> 0;

	uint32 addrspace = addrarg.GetValue() & kATAddressSpaceMask;
	uint32 addroffset = addrarg.GetValue() & kATAddressOffsetMask;

	uint32 sum = 0;
	uint32 wrapsum = 0;

	IATDebugTarget *target = g_debugger.GetTarget();

	if (wordsw) {
		for(uint32 len = lenarg; len; --len) {
			uint8 c1 = target->DebugReadByte(addrspace + addroffset);
			uint8 c2 = target->DebugReadByte(addrspace + addroffset + 1);
			uint32 c = ((uint32)c2 << 8) + c1;
			
			sum += c;
			wrapsum += c;
			wrapsum += (wrapsum >> 16);
			wrapsum &= 0xffff;

			addroffset = (addroffset + 2) & kATAddressOffsetMask;
		}

		ATConsolePrintf("Sum[%s + L%x] = $%04x (checksum = $%04x, inv swap = $%04x)\n", g_debugger.GetAddressText(addrarg.GetValue(), true).c_str(), (uint32)lenarg, sum, wrapsum,
			~VDSwizzleU16(wrapsum) & 0xffff);
	} else {
		for(uint32 len = lenarg; len; --len) {
			uint8 c = target->DebugReadByte(addrspace + addroffset);
			
			sum += c;
			wrapsum += c;
			wrapsum += (wrapsum >> 8);
			wrapsum &= 0xff;

			addroffset = (addroffset + 1) & kATAddressOffsetMask;
		}

		ATConsolePrintf("Sum[%s + L%x] = $%02x (checksum = $%02x)\n", g_debugger.GetAddressText(addrarg.GetValue(), true).c_str(), (uint32)lenarg, sum, wrapsum);
	}
}

void ATConsoleCmdAliasA8(ATDebuggerCmdParser& parser) {
	parser >> 0;

	static const char *kA8Aliases[][3]={
		{ "cont", "", "g" },
		{ "show", "", "r" },
		{ "stack", "", "k" },
		{ "setpc", "%1", "r pc %1" },
		{ "seta", "%1", "r a %1" },
		{ "sets", "%1", "r s %1" },
		{ "setx", "%1", "r x %1" },
		{ "sety", "%1", "r y %1" },
		{ "setn", "%1", "r p.n %1" },
		{ "setv", "%1", "r p.v %1" },
		{ "setd", "%1", "r p.d %1" },
		{ "seti", "%1", "r p.i %1" },
		{ "setz", "%1", "r p.z %1" },
		{ "setc", "%1", "r p.c %1" },
		{ "setn", "", "r p.n 1" },
		{ "setv", "", "r p.v 1" },
		{ "setd", "", "r p.d 1" },
		{ "seti", "", "r p.i 1" },
		{ "setz", "", "r p.z 1" },
		{ "setc", "", "r p.c 1" },
		{ "clrn", "", "r p.n 0" },
		{ "clrv", "", "r p.v 0" },
		{ "clrd", "", "r p.d 0" },
		{ "clri", "", "r p.i 0" },
		{ "clrz", "", "r p.z 0" },
		{ "clrc", "", "r p.c 0" },
		{ "c", "%1 %*", "e %1 %*" },
		{ "d", "", "u" },
		{ "d", "%1", "u %1" },
		{ "f", "%1 %2 %3 %*", "f %1 L>%2 %3 %*" },
		{ "m", "", "db" },
		{ "m", "%1", "db %1" },
		{ "m", "%1 %2", "db %1 L>%2" },
		{ "s", "%1 %2 %3 %*", "s %1 L>%2 %3 %*" },
		{ "sum", "%1 %2", ".sum %1 L>%2" },
		{ "bpc", "%1", "bp %1" },
		{ "history", "", "h" },
		{ "g", "", "t" },
		{ "r", "", "gr" },
		{ "b", "", "bl" },
		{ "b", "?", ".help bp" },
		{ "b", "c", "bc *" },
		{ "b", "d %1", "bc %1" },
		{ "b", "pc=%1", "bp %1" },
		{ "bpc", "%1", "bp %1" },
		{ "antic", "", ".antic" },
		{ "gtia", "", ".gtia" },
		{ "pia", "", ".pia" },
		{ "pokey", "", ".pokey" },
		{ "dlist", "", ".dumpdlist" },
		{ "dlist", "%1", ".dumpdlist %1" },
		{ "labels", "%1", ".loadsym %1" },
		{ "coldstart", "", ".restart" },
		{ "warmstart", "", ".warmreset" },
		{ "help", "", ".help" },
		{ "?", "", ".help" },
		{ "?", "%*", "? %*" },
	};

	for(size_t i=0; i<sizeof(kA8Aliases)/sizeof(kA8Aliases[0]); ++i) {
		g_debugger.SetCommandAlias(kA8Aliases[i][0], kA8Aliases[i][1], kA8Aliases[i][2]);
	}

	ATConsoleWrite("Atari800-compatible command aliases set.\n");
}

void ATConsoleCmdAliasClearAll(ATDebuggerCmdParser& parser) {
	parser >> 0;

	g_debugger.ClearCommandAliases();

	ATConsoleWrite("Command aliases cleared.\n");
}

void ATConsoleCmdAliasList(ATDebuggerCmdParser& parser) {
	parser >> 0;

	g_debugger.ListCommandAliases();
}

namespace {
	bool IsValidAliasName(const VDStringSpanA& name) {
		if (name.empty())
			return false;

		VDStringSpanA::const_iterator it(name.begin()), itEnd(name.end());

		char c = *it;

		if (c == '.') {
			++it;

			if (it == itEnd)
				return false;

			c = *it;
		}

		if (!isalpha((unsigned char)c))
			return false;

		while(it != itEnd) {
			c = *it;

			if (!isalnum((unsigned char)c) && c != '_')
				return false;

			++it;
		}

		return true;
	}
}

void ATConsoleCmdAliasSet(ATDebuggerCmdParser& parser) {
	ATDebuggerCmdName alias(true);
	ATDebuggerCmdName command(false);

	parser >> alias >> command >> 0;

	if (!IsValidAliasName(*alias))
		throw MyError("Invalid alias name: %s\n", alias->c_str());

	const bool existing = g_debugger.IsCommandAliasPresent(alias->c_str());

	VDStringA aliascmd(command->c_str());
	aliascmd += " %*";

	if (command.IsValid()) {
		g_debugger.SetCommandAlias(alias->c_str(), NULL, aliascmd.c_str());

		ATConsolePrintf(existing ? "Redefined alias: %s.\n" : "Defined alias: %s.\n", alias->c_str());
	} else if (existing) {
		g_debugger.SetCommandAlias(alias->c_str(), NULL, NULL);

		ATConsolePrintf("Deleted alias: %s.\n", alias->c_str());
	} else {
		ATConsolePrintf("Unknown alias: %s.\n", alias->c_str());
	}
}

void ATConsoleCmdAliasPattern(ATDebuggerCmdParser& parser) {
	ATDebuggerCmdString aliaspat(true);
	ATDebuggerCmdString aliastmpl(false);

	parser >> aliaspat >> aliastmpl >> 0;

	VDStringRefA patname;
	VDStringRefA patargs(*aliaspat);

	if (!patargs.split(' ', patname)) {
		patname = patargs;
		patargs.clear();
	}

	VDStringA alias(patname);
	if (!IsValidAliasName(patname)) {
		throw MyError("Invalid alias name: %s\n", alias.c_str());
	}

	VDStringA aliasargs(patargs);

	const bool existing = g_debugger.GetCommandAlias(alias.c_str(), aliasargs.c_str()) != NULL;
	if (aliastmpl.IsValid()) {
		g_debugger.SetCommandAlias(alias.c_str(), aliasargs.c_str(), aliastmpl->c_str());

		ATConsolePrintf(existing ? "Redefined alias: %s %s.\n" : "Defined alias: %s %s.\n", alias.c_str(), aliasargs.c_str());
	} else if (existing) {
		g_debugger.SetCommandAlias(alias.c_str(), aliasargs.c_str(), NULL);

		ATConsolePrintf("Deleted alias: %s %s.\n", alias.c_str(), aliasargs.c_str());
	} else {
		ATConsolePrintf("Unknown alias: %s %s.\n", alias.c_str(), aliasargs.c_str());
	}
}

void ATConsoleCmdTarget(const char *cmd, int argc, const char **argv) {
	// Command formats we support:
	//
	// ~		Display targets
	// ~0		Display target 0 status
	// ~0s		Switch default to target 0

	// We're guaranteed that *cmd == '~'.
	if (!cmd[1]) {
		vdfastvector<IATDebugTarget *> targets;

		g_debugger.GetTargetList(targets);

		ATConsoleWrite("ID  TimeSkew  Type                Name\n");

		uint32 n = (uint32)targets.size();
		for(uint32 i=0; i<n; ++i) {
			IATDebugTarget *target = targets[i];

			if (target) {
				const auto disasmMode = target->GetDisasmMode();
				const char *disasmModeStr = "?";
				float clockMultiplier = 1.0f;

				switch(disasmMode) {
					case kATDebugDisasmMode_6502:	disasmModeStr = "6502"; break;
					case kATDebugDisasmMode_65C02:	disasmModeStr = "65C02"; break;
					case kATDebugDisasmMode_65C816:	disasmModeStr = "65C816"; break;
					case kATDebugDisasmMode_Z80:	disasmModeStr = "Z80"; break;
					case kATDebugDisasmMode_8048:	disasmModeStr = "8048"; clockMultiplier = 15.0f; break;
					case kATDebugDisasmMode_6809:	disasmModeStr = "6809"; break;
				}

				VDStringA typeStr(disasmModeStr);

				if (auto *history = vdpoly_cast<IATDebugTargetHistory *>(target))
					typeStr.append_sprintf(" @ %.3gMHz", history->GetTimestampFrequency() * clockMultiplier / 1000000.0);

				ATConsolePrintf("%2u  %7d   %-19s %s\n", i, target->GetTimeSkew(), typeStr.c_str(), target->GetName());
			}
		}

		return;
	} else if (isdigit((unsigned char)cmd[1])) {
		char c1, c2;
		unsigned idx;
		int r = sscanf(cmd + 1, "%u%c%c", &idx, &c1, &c2);

		if (r == 1) {
			return;
		} else if (r == 2 && c1 == 's') {
			if (!g_debugger.SetTarget(idx)) {
				ATConsolePrintf("Invalid target ID: %u.\n", idx);
				return;
			}

			IATDebugTarget *target = g_debugger.GetTarget();

			g_debugger.SetContinuationAddress(g_debugger.GetPC());
			ATConsolePrintf("Target now set to %u:%s.\n", idx, target->GetName());
			return;
		}
	}

	ATConsolePrintf("Unknown target command: %s.\n", cmd);
}

void ATConsoleQueueCommand(const char *s) {
	g_debugger.QueueCommand(s, true);
}

void ATConsoleExecuteCommand(const char *s, bool echo) {
	IATDebuggerActiveCommand *acmd = g_debugger.GetActiveCommand();

	if (acmd) {
		g_debugger.ExecuteCommand(s);
		return;
	}

	if (echo) {
		ATConsolePrintf("%s> ", g_debugger.GetPrompt());
		ATConsolePrintf("%s\n", s);
	} else if (!*s)
		return;

	vdfastvector<char> tempstr;
	vdfastvector<const char *> argptrs;

	int argc = ATDebuggerParseArgv(s, tempstr, argptrs);

	VDStringA tempcmd;

	if (!argc) {
		if (!echo)
			return;

		tempcmd = g_debugger.GetRepeatCommand();
		s = tempcmd.c_str();
		argc = ATDebuggerParseArgv(s, tempstr, argptrs);

		if (!argc)
			return;
	} else {
		if (echo)
			g_debugger.SetRepeatCommand(argptrs[0]);
	}

	const char **argv = argptrs.data();
	const char *cmd = argv[0];

	if (*cmd == '`')
		++cmd;
	else if (*cmd == '~') {
		ATConsoleCmdTarget(cmd, argc-1, argv+1);
		return;
	} else {
		vdfastvector<char> tempstr2;
		vdfastvector<const char *> argptrs2;

		if (g_debugger.MatchCommandAlias(cmd, argv+1, argc-1, tempstr2, argptrs2)) {
			if (argptrs2.empty()) {
				ATConsolePrintf("Incorrect parameters for alias '%s'.\n", cmd);
				return;
			}

			tempstr.swap(tempstr2);
			argptrs.swap(argptrs2);

			argc = (int)argptrs.size() - 1;
			argv = argptrs.data();
			cmd = argv[0];
		}
	}

	const char *argstart = argc > 1 ? s + (argv[1] - tempstr.data()) : NULL;

	ATDebuggerCmdParser parser(argc-1, argv+1);

	if (g_debugger.InvokeCommand(cmd, parser))
		return;
	
	if (!strcmp(cmd, "?")) {
		ATConsoleCmdEval(argstart);
	} else {
		ATConsolePrintf("Unrecognized command '%s'. \".help\" for help\n", cmd);
	}
}

///////////////////////////////////////////////////////////////////////////

void ATConsoleCmdLoadObj(ATDebuggerCmdParser& parser) {
	ATDebuggerCmdString path(true);

	parser >> path >> 0;

	VDFileStream fs(path->c_str());
	VDBufferedStream bs(&fs, 4096);

	ATConsolePrintf("Loading %s:\n", path->c_str());

	uint8 buf[1024];
	sint32 actual;
	for(;;) {
		actual = bs.ReadData(buf, 2);

		if (actual != 2)
			break;

		uint16 startAddr = VDReadUnalignedLEU16(buf);
		if (startAddr == 0xFFFF)
			continue;

		actual = bs.ReadData(buf, 2);
		if (actual != 2)
			break;

		uint16 endAddr = VDReadUnalignedLEU16(buf);

		if (endAddr < startAddr) {
			ATConsolePrintf("WARNING: Stopping at invalid header: $%04X > $%04X\n", startAddr, endAddr);
			actual = 0;
			break;
		}

		// check if this range covers RUNAD or INITAD, and skip it if so
		sint32 pos = (sint32)bs.Pos();
		sint32 len = endAddr + 1 - startAddr;
		if (startAddr >= 0x2E0 && endAddr <= 0x2E3) {
			ATConsolePrintf("Skipping: %04X-%04X to %04X-%04X\n", pos, pos + len - 1, startAddr, endAddr);

			bs.Skip(len);
		} else {
			ATConsolePrintf("Reading %04X-%04X to %04X-%04X\n", pos, pos + len - 1, startAddr, endAddr);

			while(len) {
				sint32 tc = len > sizeof buf ? sizeof buf : len;
				bs.Read(buf, tc);

				g_debugger.WriteMemoryCPU(startAddr, buf, tc);
				startAddr += tc;
				len -= tc;
			}
		}
	}

	if (actual)
		ATConsoleWrite("WARNING: Stopping at truncated header.\n");
}

void ATConsoleCmdExamineSymbols(ATDebuggerCmdParser& parser) {
	ATDebuggerCmdString pattern(true);

	parser >> pattern >> 0;

	vdfastvector<char> patbuf(pattern->c_str(), pattern->c_str() + pattern->size() + 1);
	char *pat = patbuf.data();
	char *modpat = strchr(pat, '!');
	char *namepat = pat;

	if (modpat) {
		*modpat = 0;
		namepat = modpat + 1;
		modpat = pat;
	}

	vdfastvector<uint32> moduleIds;

	g_debugger.GetModuleIds(moduleIds);

	const uint32 targetId = g_debugger.GetTargetIndex();

	for(vdfastvector<uint32>::const_iterator it(moduleIds.begin()), itEnd(moduleIds.end());
		it != itEnd;
		++it)
	{
		const uint32 moduleId = *it;

		if (g_debugger.GetModuleTargetId(moduleId) != targetId)
			continue;

		const char *modname = g_debugger.GetModuleShortName(moduleId);
		if (!modname)
			continue;

		if (modpat && !VDFileWildMatch(modpat, modname))
			continue;

		struct OnSymbol {
			void operator()(const ATSymbolInfo& symInfo) {
				if (!mpNamePat || VDFileWildMatch(mpNamePat, symInfo.mpName))
					ATConsolePrintf("%-10s  %s!%s\n", g_debugger.GetAddressText(symInfo.mOffset, true).c_str(), mpModuleName, symInfo.mpName);
			}

			const char *mpModuleName;
			const char *mpNamePat;
		} itfn = { modname, namepat };

		g_debugger.EnumModuleSymbols(moduleId, ATBINDCALLBACK(&itfn, &OnSymbol::operator()));
	}
}

void ATConsoleCmdDma(ATDebuggerCmdParser& parser) {
	parser >> 0;

	g_sim.GetAntic().DumpDMAPattern();
}

void ATConsoleCmdDmaMap(ATDebuggerCmdParser& parser) {
	parser >> 0;

	g_sim.GetAntic().DumpDMAActivityMap();
}

void ATConsoleCmdDmaBuf(ATDebuggerCmdParser& parser) {
	parser >> 0;

	g_sim.GetAntic().DumpDMALineBuffer();
}

void ATConsoleCmdProfileBeginFrame(ATDebuggerCmdParser& parser) {
	parser >> 0;

	auto *p = g_sim.GetProfiler();

	if (p)
		p->BeginFrame();
}

void ATConsoleCmdProfileEndFrame(ATDebuggerCmdParser& parser) {
	parser >> 0;

	auto *p = g_sim.GetProfiler();

	if (p)
		p->EndFrame();
}

void ATConsoleCmdKMKJZIDE(ATDebuggerCmdParser& parser) {
	parser >> 0;

	auto *dm = g_sim.GetDeviceManager();
	IATDevice *pcl = dm->GetDeviceByTag("kmkjzide");

	if (!pcl) {
		pcl = dm->GetDeviceByTag("kmkjzide2");

		if (!pcl) {
			ATConsoleWrite("KMK/JZ IDE / IDEPlus is not active.\n");
			return;
		}
	}

	ATDebuggerConsoleOutput conout;
	if (auto *p = vdpoly_cast<IATDeviceDiagnostics *>(pcl))
		p->DumpStatus(conout);
}

void ATConsoleCmdDumpInterface(ATDebuggerCmdParser& parser, uint32 iid, const vdfunction<void(void *, ATConsoleOutput&)>& fn) {
	parser >> 0;

	auto devices = g_sim.GetDeviceManager()->GetDevices(false, false);
	bool firstDevice = true;
	ATDebuggerConsoleOutput conout;

	for(IATDevice *dev : devices) {
		void *ifc = dev->AsInterface(iid);

		if (ifc) {
			if (firstDevice)
				firstDevice = false;
			else
				conout.WriteLine("");

			ATDeviceInfo info;
			dev->GetDeviceInfo(info);

			conout("%ls:", info.mpDef->mpName);
			fn(ifc, conout);
		}
	}
}

template<class T>
void ATConsoleCmdDumpInterface(ATDebuggerCmdParser& parser) {
	ATConsoleCmdDumpInterface(parser, T::kTypeID,
		[](void *p, auto& out) {
			((T *)p)->DumpStatus(out);
		}
	);
}

void ATConsoleCmdRIOT(ATDebuggerCmdParser& parser) {
	ATConsoleCmdDumpInterface<ATRIOT6532Emulator>(parser);
}

void ATConsoleCmdFDC(ATDebuggerCmdParser& parser) {
	ATConsoleCmdDumpInterface<ATFDCEmulator>(parser);
}

void ATConsoleCmdCTC(ATDebuggerCmdParser& parser) {
	ATConsoleCmdDumpInterface<ATCTCEmulator>(parser);
}

void ATConsoleCmdLogClose(ATDebuggerCmdParser& parser) {
	parser >> 0;

	ATConsoleCloseLogFile();
}

void ATConsoleCmdLogOpen(ATDebuggerCmdParser& parser) {
	ATDebuggerCmdPath path(true);
	parser >> path >> 0;

	ATConsoleOpenLogFile(VDTextAToW(path->c_str()).c_str());
}

void ATDebuggerInitCommands() {
	static constexpr ATDebuggerCmdDef kCommands[]={
		{ "a",					ATConsoleCmdAssemble },
		{ "a8",					ATConsoleCmdAliasA8 },
		{ "ac",					ATConsoleCmdAliasClearAll },
		{ "al",					ATConsoleCmdAliasList },
		{ "ap",					ATConsoleCmdAliasPattern },
		{ "as",					ATConsoleCmdAliasSet },
		{ "ba",					ATConsoleCmdBreakptAccess },
		{ "bc",					ATConsoleCmdBreakptClear },
		{ "bl",					ATConsoleCmdBreakptList },
		{ "bp",					ATConsoleCmdBreakpt },
		{ "bs",					ATConsoleCmdBreakptSector },
		{ "bsc",				ATConsoleCmdBreakptSetCondition },
		{ "bt",					ATConsoleCmdBreakptTrace },
		{ "bta",				ATConsoleCmdBreakptTraceAccess },
		{ "bx",					ATConsoleCmdBreakptExpr },
		{ "da",					ATConsoleCmdDumpATASCII },
		{ "db",					ATConsoleCmdDumpBytes },
		{ "dbi",				ATConsoleCmdDumpBytesInternal },
		{ "dd",					ATConsoleCmdDumpDwords },
		{ "df",					ATConsoleCmdDumpFloats },
		{ "di",					ATConsoleCmdDumpINTERNAL },
		{ "dw",					ATConsoleCmdDumpWords },
		{ "dy",					ATConsoleCmdDumpBinary },
		{ "e",					ATConsoleCmdEnter },
		{ "eb",					ATConsoleCmdEnter },
		{ "ew",					ATConsoleCmdEnterWords },
		{ "f",					ATConsoleCmdFill },
		{ "fbx",				ATConsoleCmdFillExp },
		{ "g",					ATConsoleCmdGo },
		{ "gf",					ATConsoleCmdGoFrameEnd },
		{ "gr",					ATConsoleCmdGoReturn },
		{ "gs",					ATConsoleCmdGoScanline },
		{ "gt",					ATConsoleCmdGoTraced },
		{ "gv",					ATConsoleCmdGoVBI },
		{ "h",					ATConsoleCmdDumpHistory },
		{ "hma",				ATConsoleCmdHeatMapDumpAccesses },
		{ "hmc",				ATConsoleCmdHeatMapClear },
		{ "hmd",				ATConsoleCmdHeatMapDumpMemory },
		{ "hme",				ATConsoleCmdHeatMapEnable },
		{ "hmr",				ATConsoleCmdHeatMapRegisters },
		{ "hmp",				ATConsoleCmdHeatMapPreset },
		{ "hmt",				ATConsoleCmdHeatMapTrap },
		{ "hmu",				ATConsoleCmdHeatMapUninit },
		{ "ib",					ATConsoleCmdInputByte },
		{ "k",					ATConsoleCmdCallStack },
		{ "lm",					ATConsoleCmdListModules },
		{ "lfe",				ATConsoleCmdLogFilterEnable },
		{ "lfd",				ATConsoleCmdLogFilterDisable },
		{ "lfl",				ATConsoleCmdLogFilterList },
		{ "lft",				ATConsoleCmdLogFilterTag },
		{ "ln",					ATConsoleCmdListNearestSymbol },
		{ "m",					ATConsoleCmdMove },
		{ "o",					ATConsoleCmdStepOver },
		{ "r",					ATConsoleCmdRegisters },
		{ "s",					ATConsoleCmdSearch },
		{ "sw",					ATConsoleCmdSearchWord },
		{ "sa",					ATConsoleCmdSearchATASCII },
		{ "si",					ATConsoleCmdSearchINTERNAL },
		{ "st",					ATConsoleCmdStaticTrace },
		{ "t",					ATConsoleCmdTrace },
		{ "u",					ATConsoleCmdUnassemble },
		{ "vta",				ATConsoleCmdVerifierTargetAdd },
		{ "vtc",				ATConsoleCmdVerifierTargetClear },
		{ "vtl",				ATConsoleCmdVerifierTargetList },
		{ "vtr",				ATConsoleCmdVerifierTargetReset },
		{ "wb",					ATConsoleCmdWatchByte },
		{ "wc",					ATConsoleCmdWatchClear },
		{ "wl",					ATConsoleCmdWatchList },
		{ "ww",					ATConsoleCmdWatchWord },
		{ "wx",					ATConsoleCmdWatchExpr },
		{ "x",					ATConsoleCmdExamineSymbols },
		{ "ya",					ATConsoleCmdSymbolAdd },
		{ "yc",					ATConsoleCmdSymbolClear },
		{ "yd",					ATConsoleCmdSymbolRemove },
		{ "yr",					ATConsoleCmdSymbolRead },
		{ "yw",					ATConsoleCmdSymbolWrite },
		{ ".antic",				ATConsoleCmdAntic },
		{ ".bank",				ATConsoleCmdBank },
		{ ".base",				ATConsoleCmdBase },
		{ ".basic",				ATConsoleCmdBasic },
		{ ".basic_dumpline",	ATConsoleCmdBasicDumpLine },
		{ ".basic_dumpstack",	ATConsoleCmdBasicDumpStack },
		{ ".basic_rebuildvnt",	ATConsoleCmdBasicRebuildVnt },
		{ ".basic_rebuildvvt",	ATConsoleCmdBasicRebuildVvt },
		{ ".basic_save",		ATConsoleCmdBasicSave },
		{ ".basic_vars",		ATConsoleCmdBasicVars },
		{ ".batch",				ATConsoleCmdRunBatchFile },
		{ ".beam",				ATConsoleCmdBeam },
		{ ".caslogdata",		ATConsoleCmdCasLogData },
		{ ".ciodevs",			ATConsoleCmdCIODevs },
		{ ".covox",				ATConsoleCmdCovox },
		{ ".crc",				ATConsoleCmdCRC },
		{ ".ctc",				ATConsoleCmdCTC },
		{ ".diskdumpsec",		ATConsoleCmdDiskDumpSec },
		{ ".diskorder",			ATConsoleCmdDiskOrder },
		{ ".diskreadsec",		ATConsoleCmdDiskReadSec },
		{ ".disktrack",			ATConsoleCmdDiskTrack },
		{ ".diskwritesec",		ATConsoleCmdDiskWriteSec },
		{ ".dlhistory",			ATConsoleCmdDumpDLHistory },
		{ ".ds1305",			ATConsoleCmdDS1305 },
		{ ".dma",				ATConsoleCmdDma },
		{ ".dmamap",			ATConsoleCmdDmaMap },
		{ ".dmabuf",			ATConsoleCmdDmaBuf },
		{ ".dumpdlist",			ATConsoleCmdDumpDisplayList },
		{ ".dumpdsm",			ATConsoleCmdDumpDsm },
		{ ".dumpsnap",			ATConsoleCmdDumpSnap },
		{ ".echo",				ATConsoleCmdEcho },
		{ ".fdc",				ATConsoleCmdFDC },
		{ ".gtia",				ATConsoleCmdGTIA },
		{ ".help",				ATConsoleCmdDumpHelp },
		{ ".history",			ATConsoleCmdDumpHistory },
		{ ".ide",				ATConsoleCmdIDE },
		{ ".ide_dumpsec",		ATConsoleCmdIDEDumpSec },
		{ ".ide_rdsec",			ATConsoleCmdIDEReadSec },
		{ ".ide_wrsec",			ATConsoleCmdIDEWriteSec },
		{ ".iocb",				ATConsoleCmdIOCB },
		{ ".kmkjzide",			ATConsoleCmdKMKJZIDE },
		{ ".loadksym",			ATConsoleCmdLoadKernelSymbols },
		{ ".loadobj",			ATConsoleCmdLoadObj },
		{ ".loadsym",			ATConsoleCmdLoadSymbols },
		{ ".logclose",			ATConsoleCmdLogClose },
		{ ".logopen",			ATConsoleCmdLogOpen },
		{ ".map",				ATConsoleCmdMap },
		{ ".netstat",			ATConsoleCmdNetstat },
		{ ".netpcap",			ATConsoleCmdNetPCap },
		{ ".netpcapclose",		ATConsoleCmdNetPCapClose },
		{ ".onexeload",			ATConsoleCmdOnExeLoad },
		{ ".onexerun",			ATConsoleCmdOnExeRun },
		{ ".onexelist",			ATConsoleCmdOnExeList },
		{ ".onexeclear",		ATConsoleCmdOnExeClear },
		{ ".pathbreak",			ATConsoleCmdPathBreak },
		{ ".pathdump",			ATConsoleCmdPathDump },
		{ ".pathrecord",		ATConsoleCmdPathRecord },
		{ ".pathreset",			ATConsoleCmdPathReset },
		{ ".pclink",			ATConsoleCmdDumpPCLink },
		{ ".pbi",				ATConsoleCmdPBI },
		{ ".pia",				ATConsoleCmdDumpPIAState },
		{ ".pokey",				ATConsoleCmdPokey },
		{ ".printf",			ATConsoleCmdPrintf },
		{ ".profile_beginframe",	ATConsoleCmdProfileBeginFrame},
		{ ".profile_endframe",		ATConsoleCmdProfileEndFrame},
		{ ".rapidus",			ATConsoleCmdRapidus },
		{ ".readmem",			ATConsoleCmdReadMem },
		{ ".reload",			ATConsoleCmdReload },
		{ ".restart",			ATConsoleCmdColdReset },
		{ ".riot",				ATConsoleCmdRIOT },
		{ ".sourcemode",		ATConsoleCmdSourceMode },
		{ ".tape",				ATConsoleCmdTape },
		{ ".tapedata",			ATConsoleCmdTapeData },
		{ ".tracecio",			ATConsoleCmdTraceCIO },
		{ ".tracesio",			ATConsoleCmdTraceSIO },
		{ ".traceser",			ATConsoleCmdTraceSer },
		{ ".sdx_loadsyms",		ATConsoleCmdSDXLoadSymbols },
		{ ".sid",				ATConsoleCmdSID },
		{ ".sio",				ATConsoleCmdDumpSIO },
		{ ".sprintf",			ATConsoleCmdSprintf },
		{ ".sum",				ATConsoleCmdSum },
		{ ".ultimate",			ATConsoleCmdUltimate },
		{ ".unloadsym",			ATConsoleCmdUnloadSymbols },
		{ ".vbxe",				ATConsoleCmdDumpVBXEState },
		{ ".vbxe_xdl",			ATConsoleCmdDumpVBXEXDL },
		{ ".vbxe_bl",			ATConsoleCmdDumpVBXEBL },
		{ ".vbxe_traceblits",	ATConsoleCmdVBXETraceBlits },
		{ ".vectors",			ATConsoleCmdVectors },
		{ ".warmreset",			ATConsoleCmdWarmReset },
		{ ".writemem",			ATConsoleCmdWriteMem },
	};

	ATGetDebugger()->DefineCommands(kCommands, vdcountof(kCommands));
}
