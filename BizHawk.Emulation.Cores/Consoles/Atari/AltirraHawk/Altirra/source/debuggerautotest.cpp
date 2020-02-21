//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2009-2018 Avery Lee
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
//	You should have received a copy of the GNU General Public License along
//	with this program. If not, see <http://www.gnu.org/licenses/>.

#include <stdafx.h>
#include <vd2/system/vdalloc.h>
#include <at/atcore/devicemanager.h>
#include <at/atcore/deviceparent.h>
#include <at/atcore/propertyset.h>
#include <at/atdebugger/argparse.h>
#include <at/atui/uicommandmanager.h>
#include "console.h"
#include "debugger.h"
#include "simulator.h"
#include "uiaccessors.h"

extern ATSimulator g_sim;

bool ATUISaveFrame(const wchar_t *path);

void ATDebuggerCmdAutotestAssert(ATDebuggerCmdParser& parser) {
	ATDebuggerCmdExprNum exprArg(true);
	ATDebuggerCmdString exprMessage(false);
	parser >> exprArg >> exprMessage >> 0;

	if (!exprArg.GetValue()) {
		ATGetDebugger()->Break();

		throw MyError("Assertion failed: %s", exprMessage.IsValid() ? exprMessage->c_str() : exprArg.GetOriginalText());
	}
}

void ATDebuggerCmdAutotestExit(ATDebuggerCmdParser& parser) {
	ATUIExit(true);
}

void ATDebuggerCmdAutotestClearDevices(ATDebuggerCmdParser& parser) {
	parser >> 0;

	g_sim.GetDeviceManager()->RemoveAllDevices(false);
}

void ATDebuggerCmdAutotestAddDevice(ATDebuggerCmdParser& parser) {
	ATDebuggerCmdName busArg(true);
	ATDebuggerCmdName tagArg(true);

	parser >> busArg >> tagArg >> 0;

	ATDeviceManager& devMgr = *g_sim.GetDeviceManager();
	ATParsedDevicePath busRef {};

	if (*busArg != "/") {
		busRef = devMgr.ParsePath(busArg->c_str());

		if (!busRef.mpDeviceBus)
			throw MyError("Invalid bus reference: %s.", busArg->c_str());
	}

	const ATDeviceDefinition *def = devMgr.GetDeviceDefinition(tagArg->c_str());
	if (!def)
		throw MyError("Unknown device definition: %s.", tagArg->c_str());

	ATPropertySet pset;
	IATDevice *newDev = devMgr.AddDevice(def, pset, busRef.mpDeviceBus != nullptr, false);

	try {
		if (busRef.mpDeviceBus) {
			busRef.mpDeviceBus->AddChildDevice(newDev);

			if (!newDev->GetParent())
				throw MyError("Unable to add device %s to bus: %s.", tagArg->c_str(), busArg->c_str());
		}
	} catch(...) {
		devMgr.RemoveDevice(newDev);
		throw;
	}

	ATConsolePrintf("Added new device: %s", devMgr.GetPathForDevice(newDev).c_str());
}

void ATDebuggerCmdAutotestRemoveDevice(ATDebuggerCmdParser& parser) {
	ATDebuggerCmdName pathArg(true);

	parser >> pathArg >> 0;

	ATDeviceManager& devMgr = *g_sim.GetDeviceManager();
	ATParsedDevicePath deviceRef = devMgr.ParsePath(pathArg->c_str());

	if (!deviceRef.mpDevice)
		throw MyError("Invalid device reference: %s.\n", pathArg->c_str());

	devMgr.RemoveDevice(deviceRef.mpDevice);

	vdfastvector<IATDevice *> devs;
	devMgr.MarkAndSweep(nullptr, 0, devs);

	for(IATDevice *child : devs)
		devMgr.RemoveDevice(child);
}

void ATDebuggerCmdAutotestListDevices(ATDebuggerCmdParser& parser) {
	parser >> 0;

	ATDeviceManager& dm = *g_sim.GetDeviceManager();

	VDStringA s;

	const auto printEntry = [&](uint32 indent, const char *path, const wchar_t *desc) {
		s.clear();
		s.append(indent, ' ');
		s += '/';
		s += path;
		s.append(25 - std::min<size_t>(s.size(), 25), ' ');
		s.append_sprintf("%ls\n", desc);
		ATConsoleWrite(s.c_str());

	};

	const auto printDevice = [&](IATDevice *dev, uint32 indent, const auto& self) -> void {
		s.clear();
		s.append(indent, ' ');
		
		ATDeviceInfo info;
		dev->GetDeviceInfo(info);

		printEntry(indent, info.mpDef->mpTag, info.mpDef->mpName);

		IATDeviceParent *devParent = vdpoly_cast<IATDeviceParent *>(dev);
		if (devParent) {
			for(uint32 i=0; ; ++i) {
				IATDeviceBus *bus = devParent->GetDeviceBus(i);
				if (!bus)
					break;

				printEntry(indent + 2, bus->GetBusTag(), bus->GetBusName());

				vdfastvector<IATDevice *> children;
				bus->GetChildDevices(children);

				for(IATDevice *child : children) {
					self(child, indent + 4, self);
				}
			}
		}
	};

	for(IATDevice *dev : dm.GetDevices(true, true)) {
		printDevice(dev, 0, printDevice);
	}
}

template<bool T_State>
void ATDebuggerCmdAutotestCmdOffOn(ATDebuggerCmdParser& parser) {
	ATDebuggerCmdName nameArg(true);
	parser >> nameArg >> 0;

	const ATUICommand *cmd = ATUIGetCommandManager().GetCommand(nameArg->c_str());
	if (!cmd)
		throw MyError("Unknown UI command: %s", nameArg->c_str());

	if (cmd->mpTestFn && !cmd->mpTestFn())
		return;

	if (!cmd->mpStateFn)
		return;

	ATUICmdState state = cmd->mpStateFn();

	const bool isOn = (state == kATUICmdState_Checked);

	if (isOn != T_State)
		cmd->mpExecuteFn();
}

void ATDebuggerCmdAutotestCmd(ATDebuggerCmdParser& parser) {
	ATDebuggerCmdName nameArg(true);
	parser >> nameArg >> 0;

	const ATUICommand *cmd = ATUIGetCommandManager().GetCommand(nameArg->c_str());
	if (!cmd)
		throw MyError("Unknown UI command: %s", nameArg->c_str());

	if (cmd->mpTestFn && !cmd->mpTestFn())
		return;

	cmd->mpExecuteFn();
}

void ATDebuggerCmdAutotestBootImage(ATDebuggerCmdParser& parser) {
	ATDebuggerCmdPath pathArg(true);
	parser >> pathArg >> 0;

	ATUIBootImage(VDTextAToW(pathArg->c_str()).c_str());
}

class ATDebuggerActiveCmdCheckWaitScreen final : public vdrefcounted<IATDebuggerActiveCommand> {
public:
	ATDebuggerActiveCmdCheckWaitScreen(bool wait, uint32 waitChecksum);

	virtual bool IsBusy() const override { return true; }
	virtual const char *GetPrompt() override { return ""; }
	virtual void BeginCommand(IATDebugger *debugger) override;
	virtual void EndCommand() override;
	virtual bool ProcessSubCommand(const char *s) override;

private:
	void ProcessFrame(const VDPixmap& px);

	ATGTIARawFrameFn mRawFrameFn;
	bool mbMatched;
	IATDebugger *mpDebugger;
	bool mbWait;
	uint32 mWaitChecksum;
};

ATDebuggerActiveCmdCheckWaitScreen::ATDebuggerActiveCmdCheckWaitScreen(bool wait, uint32 waitChecksum) {
	mbMatched = false;
	mbWait = wait;
	mWaitChecksum = waitChecksum;
}

void ATDebuggerActiveCmdCheckWaitScreen::BeginCommand(IATDebugger *debugger) {
	mRawFrameFn = [this](const VDPixmap& px) { ProcessFrame(px); };
	g_sim.GetGTIA().AddRawFrameCallback(&mRawFrameFn);
	mpDebugger = debugger;
	mpDebugger->Run(kATDebugSrcMode_Same);
}

void ATDebuggerActiveCmdCheckWaitScreen::EndCommand() {
	g_sim.GetGTIA().RemoveRawFrameCallback(&mRawFrameFn);
}

bool ATDebuggerActiveCmdCheckWaitScreen::ProcessSubCommand(const char *s) {
	if (!mbMatched)
		return true;

	return false;
}

void ATDebuggerActiveCmdCheckWaitScreen::ProcessFrame(const VDPixmap& px) {
	uint32 sum1 = 0;
	uint32 sum2 = 0;

	const uint8 *p = (const uint8 *)px.data;

	for(sint32 y = 0; y < px.h; ++y) {
		uint32 sum3 = 0;
		uint32 sum4 = 0;

		for(sint32 x = 0; x < px.w; ++x) {
			sum3 += p[x];
			sum4 += sum3;
		}

		sum1 = (sum1 + sum3) % 65535;
		sum2 = (sum2 + sum4) % 65535;

		p += px.pitch;
	}

	uint32 sum = sum1 + (sum2 << 16);


	if (mbWait) {
		if (sum == mWaitChecksum)
			mbMatched = true;
	} else {
		ATConsolePrintf("Image checksum: %08X\n", sum);
		mbMatched = true;
	}

	if (mbMatched)
		mpDebugger->Stop();
}

void ATDebuggerCmdAutotestCheckScreen(ATDebuggerCmdParser& parser) {
	parser >> 0;

	ATGetDebugger()->StartActiveCommand(new ATDebuggerActiveCmdCheckWaitScreen(false, 0));
}

void ATDebuggerCmdAutotestWaitScreen(ATDebuggerCmdParser& parser) {
	ATDebuggerCmdExprNum checksum(true);
	parser >> checksum >> 0;

	ATGetDebugger()->StartActiveCommand(new ATDebuggerActiveCmdCheckWaitScreen(true, checksum.GetValue()));
}

class ATDebuggerActiveCmdCheckWait final : public vdrefcounted<IATDebuggerActiveCommand> {
public:
	ATDebuggerActiveCmdCheckWait(vdautoptr<ATDebugExpNode> expr);

	virtual bool IsBusy() const override { return true; }
	virtual const char *GetPrompt() override { return ""; }
	virtual void BeginCommand(IATDebugger *debugger) override;
	virtual void EndCommand() override;
	virtual bool ProcessSubCommand(const char *s) override;

private:
	void OnFrameTick();

	vdautoptr<ATDebugExpNode> mpExpr;
	IATDebugger *mpDebugger;
	uint32 mEventId = 0;
	bool mbCompleted = false;
};

ATDebuggerActiveCmdCheckWait::ATDebuggerActiveCmdCheckWait(vdautoptr<ATDebugExpNode> expr) {
	mpExpr = std::move(expr);
}

void ATDebuggerActiveCmdCheckWait::BeginCommand(IATDebugger *debugger) {
	mpDebugger = debugger;

	mEventId = g_sim.GetEventManager()->AddEventCallback(kATSimEvent_FrameTick, [this] { OnFrameTick(); });
	mpDebugger->Run(kATDebugSrcMode_Same);
}

void ATDebuggerActiveCmdCheckWait::EndCommand() {
	g_sim.GetEventManager()->RemoveEventCallback(mEventId);
}

bool ATDebuggerActiveCmdCheckWait::ProcessSubCommand(const char *s) {
	return !mbCompleted;
}

void ATDebuggerActiveCmdCheckWait::OnFrameTick() {
	if (!mbCompleted && mpDebugger->Evaluate(mpExpr).second) {
		mpDebugger->Stop();
		mbCompleted = true;
	}
}

void ATDebuggerCmdAutotestWait(ATDebuggerCmdParser& parser) {
	ATDebuggerCmdExpr cond(true);
	parser >> cond >> 0;

	ATGetDebugger()->StartActiveCommand(new ATDebuggerActiveCmdCheckWait(vdautoptr<ATDebugExpNode>(cond.DetachValue())));
}

void ATDebuggerCmdAutotestSaveImage(ATDebuggerCmdParser& parser) {
	ATDebuggerCmdString path(true);
	parser >> path >> 0;

	if (!ATUISaveFrame(VDTextAToW(path->c_str()).c_str()))
		throw MyError("No framebuffer available.");
}

void ATDebuggerInitAutotestCommands() {
	static constexpr ATDebuggerCmdDef kCommands[]={
		{ ".autotest_assert",				ATDebuggerCmdAutotestAssert },
		{ ".autotest_exit",					ATDebuggerCmdAutotestExit },
		{ ".autotest_cleardevices",			ATDebuggerCmdAutotestClearDevices },
		{ ".autotest_listdevices",			ATDebuggerCmdAutotestListDevices },
		{ ".autotest_adddevice",			ATDebuggerCmdAutotestAddDevice },
		{ ".autotest_removedevice",			ATDebuggerCmdAutotestRemoveDevice },
		{ ".autotest_cmd",					ATDebuggerCmdAutotestCmd },
		{ ".autotest_cmdoff",				ATDebuggerCmdAutotestCmdOffOn<false> },
		{ ".autotest_cmdon",				ATDebuggerCmdAutotestCmdOffOn<true> },
		{ ".autotest_bootimage",			ATDebuggerCmdAutotestBootImage },
		{ ".autotest_checkscreen",			ATDebuggerCmdAutotestCheckScreen },
		{ ".autotest_waitscreen",			ATDebuggerCmdAutotestWaitScreen },
		{ ".autotest_wait",					ATDebuggerCmdAutotestWait },
		{ ".autotest_saveimage",			ATDebuggerCmdAutotestSaveImage },
	};

	ATGetDebugger()->DefineCommands(kCommands, vdcountof(kCommands));
}
