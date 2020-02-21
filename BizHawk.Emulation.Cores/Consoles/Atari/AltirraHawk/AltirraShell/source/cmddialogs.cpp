#include <stdafx.h>
#include <at/atui/uicommandmanager.h>
#include "globals.h"
#include "serialconfig.h"

bool ATSUIShowDialogSerialOptions(VDGUIHandle hParent, ATSSerialConfig& cfg);

extern HWND g_hwnd;

const ATUICommand kATSUIDialogCommands[]={
	{
		"Settings.SerialOptionsDialog",
		[]() {
			ATSSerialConfig cfg;

			ATSGetSerialConfig(cfg);
			if (ATSUIShowDialogSerialOptions((VDGUIHandle)g_hwnd, cfg))
				ATSSetSerialConfig(cfg);
		}
	}
};

void ATSUIRegisterDialogCommands(ATUICommandManager& cm) {
	cm.RegisterCommands(kATSUIDialogCommands, vdcountof(kATSUIDialogCommands));
}
