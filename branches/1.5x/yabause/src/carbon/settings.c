/*  Copyright 2006 Guillaume Duhamel
    Copyright 2006 Anders Montonen
    Copyright 2010 Alex Marshall

    This file is part of Yabause.

    Yabause is free software; you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation; either version 2 of the License, or
    (at your option) any later version.

    Yabause is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with Yabause; if not, write to the Free Software
    Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA 02110-1301  USA
*/

#include <unistd.h>
#include <Carbon/Carbon.h>
#include <ApplicationServices/ApplicationServices.h>
#include "settings.h"

#define		TAB_ID 	 	128
#define		TAB_SIGNATURE	'tabs'
int tabList[] = {129, 130, 131, 132};

int loadtype = 0;
ControlRef oldTab;

int mystrnlen(char* in, int maxlen)
{
	int len;
	for(len = 0; (*in != 0) && (len < maxlen); len++, in++);
	return len;
}

unsigned int mytoi(char* in)
{
	unsigned int out = 0;
	int length = 0;
	int i;
	int format = 0;		/* Decimal */
	if((in[0] == '0') && (in[1] == 'x')) {
		in += 2;
		format = 1;	/* Hexadecimal */
	}else if(in[0] == '$') {
		in += 1;
		format = 1;	/* Hexadecimal */
	}else if((in[0] == 'H') && (in[1] == '\'')) {
		in += 2;
		format = 1;	/* Hexadecimal */
	}
	length = mystrnlen(in, 11);
	for(i = 0; i < length; i++) {
		switch(format) {
			case 0:	/* Decimal */
				out *= 10;
				if((in[i] >= '0') && (in[i] <= '9'))
					out += in[i] - '0';
				break;
			case 1:	/* Hexadecimal */
				out <<= 4;
				if((in[i] >= '0') && (in[i] <= '9'))
					out += in[i] - '0';
				if((in[i] >= 'A') && (in[i] <= 'F'))
					out += (in[i] - 'A') + 0xA;
				if((in[i] >= 'a') && (in[i] <= 'f'))
					out += (in[i] - 'a') + 0xA;
				break;
		}
	}
	return out;
}

void SelectItemOfTabControl(ControlRef tabControl)
{
    ControlRef userPane;
    ControlID controlID;

    GetControlID(tabControl, &controlID);
    if (controlID.id != TAB_ID) return;

    controlID.signature = TAB_SIGNATURE;

    controlID.id = tabList[GetControlValue(tabControl) - 1];
    GetControlByID(GetControlOwner(tabControl), &controlID, &userPane);
       
    DisableControl(oldTab);
    SetControlVisibility(oldTab, false, false);
    EnableControl(userPane);
    SetControlVisibility(userPane, true, true);
    oldTab = userPane;

    Draw1Control(tabControl);
}

pascal OSStatus TabEventHandler(EventHandlerCallRef inHandlerRef, EventRef inEvent, void *inUserData)
{
    ControlRef control;

    GetEventParameter(inEvent, kEventParamDirectObject, typeControlRef,
	NULL, sizeof(ControlRef), NULL, &control );

    SelectItemOfTabControl(control);
    
    return eventNotHandledErr;
}

void InstallTabHandler(WindowRef window)
{
    EventTypeSpec	controlSpec = { kEventClassControl, kEventControlHit };
    ControlRef 		tabControl;
    ControlID 		controlID;
    int i;

    controlID.signature = TAB_SIGNATURE;

    for(i = 0;i < 4;i++) {
       controlID.id = tabList[i];
       GetControlByID(window, &controlID, &tabControl);
       DisableControl(tabControl);
       SetControlVisibility(tabControl, false, false);
    }

    controlID.id = TAB_ID;
    GetControlByID(window, &controlID, &tabControl);

    InstallControlEventHandler(tabControl,
                        NewEventHandlerUPP( TabEventHandler ),
                        1, &controlSpec, 0, NULL);

    SetControl32BitValue(tabControl, 1);

    SelectItemOfTabControl(tabControl); 
}

CFStringRef get_settings(WindowRef window, int i) {
	ControlID id;
	ControlRef control;
	CFStringRef s;

	id.signature = 'conf';
	id.id = i;
	GetControlByID(window, &id, &control);
	GetControlData(control, kControlEditTextPart,
		kControlEditTextCFStringTag, sizeof(CFStringRef), &s, NULL);

	return s;
}

CFStringRef get_settings_c(WindowRef window, int i) {
	ControlID id;
	ControlRef control;
	CFStringRef s;

	id.signature = 'conf';
	id.id = i;
	GetControlByID(window, &id, &control);
	s = CFStringCreateWithFormat(kCFAllocatorDefault, NULL,
		CFSTR("%d"), GetControl32BitValue(control));

	return s;
}

void set_settings(WindowRef window, int i, CFStringRef s) {
	ControlID id;
	ControlRef control;

	if (s) {
		id.signature = 'conf';
		id.id = i;
		GetControlByID(window, &id, &control);
		SetControlData(control, kControlEditTextPart,
			kControlEditTextCFStringTag, sizeof(CFStringRef), &s);
	}
}

void set_settings_c(WindowRef window, int i, CFStringRef s) {
	ControlID id;
	ControlRef control;

	if (s) {
		id.signature = 'conf';
		id.id = i;
		GetControlByID(window, &id, &control);
		SetControl32BitValue(control, CFStringGetIntValue(s));
	}
}

void save_settings(WindowRef window) {
	PerPad_struct * pad;
	int i;
	CFStringRef s;

	CFPreferencesSetAppValue(CFSTR("BiosPath"), get_settings(window, 1),
		kCFPreferencesCurrentApplication);
	CFPreferencesSetAppValue(CFSTR("CDROMDrive"), get_settings(window, 2),
		kCFPreferencesCurrentApplication);
	CFPreferencesSetAppValue(CFSTR("CDROMCore"), get_settings_c(window, 3),
		kCFPreferencesCurrentApplication);
	CFPreferencesSetAppValue(CFSTR("Region"), get_settings_c(window, 4),
		kCFPreferencesCurrentApplication);
	CFPreferencesSetAppValue(CFSTR("VideoCore"), get_settings_c(window, 5),
		kCFPreferencesCurrentApplication);
	CFPreferencesSetAppValue(CFSTR("SoundCore"), get_settings_c(window, 6),
		kCFPreferencesCurrentApplication);
	CFPreferencesSetAppValue(CFSTR("CartPath"), get_settings(window, 7),
		kCFPreferencesCurrentApplication);
	CFPreferencesSetAppValue(CFSTR("CartType"), get_settings_c(window, 8),
		kCFPreferencesCurrentApplication);
	CFPreferencesSetAppValue(CFSTR("BackupRamPath"),
		get_settings(window, 9), kCFPreferencesCurrentApplication);
	CFPreferencesSetAppValue(CFSTR("MpegRomPath"),
		get_settings(window, 10), kCFPreferencesCurrentApplication);
    CFPreferencesSetAppValue(CFSTR("AutoFrameSkip"),
        get_settings_c(window, 11), kCFPreferencesCurrentApplication);

	PerPortReset();
	pad = PerPadAdd(&PORTDATA1);

	i = 0;
	while(PerPadNames[i]) {
		s = get_settings(window, 31 + i);
		CFPreferencesSetAppValue(
			CFStringCreateWithCString(0, PerPadNames[i], 0),
			s, kCFPreferencesCurrentApplication);
		PerSetKey(CFStringGetIntValue(s), i, pad);
		i++;
	}

	CFPreferencesAppSynchronize(kCFPreferencesCurrentApplication);
}

void load_settings(WindowRef window) {
	int i;

	set_settings(window, 1, CFPreferencesCopyAppValue(CFSTR("BiosPath"),
		kCFPreferencesCurrentApplication));
	set_settings(window, 2, CFPreferencesCopyAppValue(CFSTR("CDROMDrive"),
		kCFPreferencesCurrentApplication));
	set_settings_c(window, 3, CFPreferencesCopyAppValue(CFSTR("CDROMCore"),
		kCFPreferencesCurrentApplication));
	set_settings_c(window, 4, CFPreferencesCopyAppValue(CFSTR("Region"),
		kCFPreferencesCurrentApplication));
	set_settings_c(window, 5, CFPreferencesCopyAppValue(CFSTR("VideoCore"),
		kCFPreferencesCurrentApplication));
	set_settings_c(window, 6, CFPreferencesCopyAppValue(CFSTR("SoundCore"),
		kCFPreferencesCurrentApplication));
	set_settings(window, 7, CFPreferencesCopyAppValue(CFSTR("CartPath"),
		kCFPreferencesCurrentApplication));
	set_settings_c(window, 8, CFPreferencesCopyAppValue(CFSTR("CartType"),
		kCFPreferencesCurrentApplication));
	set_settings(window, 9,
		CFPreferencesCopyAppValue(CFSTR("BackupRamPath"),
		kCFPreferencesCurrentApplication));
	set_settings(window, 10, CFPreferencesCopyAppValue(CFSTR("MpegRomPath"),
		kCFPreferencesCurrentApplication));
    set_settings_c(window, 11, CFPreferencesCopyAppValue(CFSTR("AutoFrameSkip"),
        kCFPreferencesCurrentApplication));

	i = 0;
	while(PerPadNames[i]) {
		set_settings(window, 31 + i, CFPreferencesCopyAppValue(
			CFStringCreateWithCString(0, PerPadNames[i], 0),
			kCFPreferencesCurrentApplication));
		i++;
	}
}

int load_file_core(char* file, char* addr, int type)
{
	unsigned int adr;
	int ret = -1;
	if(addr == NULL)
		adr = 0;
	else
		adr = mytoi(addr);
	switch(type) {
		case 0:
			ret = MappedMemoryLoad(file, adr);
			break;
		case 1:
			MappedMemoryLoadExec(file, adr);
			ret = 0;
			break;
	}
	return ret;
}

void load_file(WindowRef window, int type) {
	char addrbuf[12];
	char filebuf[256];
	int ret = -1;
	CFStringGetCString(get_settings(window, 1), filebuf, 256, kCFStringEncodingUTF8);
	CFStringGetCString(get_settings(window, 2), addrbuf,  12, kCFStringEncodingUTF8);
	ret = load_file_core(filebuf, addrbuf, type);
	(void)(ret);	/* We need to do something about bad return values... */
}

OSStatus SettingsWindowEventHandler (EventHandlerCallRef myHandler, EventRef theEvent, void* userData)
{
  OSStatus result = eventNotHandledErr;

  switch (GetEventKind (theEvent))
    {
    case kEventWindowClose:
      {
	WindowRef window;
        GetEventParameter(theEvent, kEventParamDirectObject, typeWindowRef,
	  0, sizeof(typeWindowRef), 0, &window);

	save_settings(window);

        DisposeWindow(window);
      }
      result = noErr;
      break;

    }
 
  return (result);
}

OSStatus BrowseHandler(EventHandlerCallRef h, EventRef event, void* data) {
	NavDialogRef dialog;
	NavDialogCreationOptions options;

	NavGetDefaultDialogCreationOptions(&options);
	NavCreateChooseFileDialog(&options, NULL, NULL, NULL, NULL,
		NULL, &dialog);
	NavDialogRun(dialog);

	if (NavDialogGetUserAction(dialog) == kNavUserActionChoose) {
		NavReplyRecord reply;
		FSRef fileAsFSRef;
		CFURLRef fileAsCFURLRef = NULL;
		CFStringRef s;

		NavDialogGetReply(dialog, &reply);

		AEGetNthPtr(&(reply.selection), 1, typeFSRef,
			NULL, NULL, &fileAsFSRef, sizeof(FSRef), NULL);

		NavDisposeReply(&reply);
		NavDialogDispose(dialog);
	
		fileAsCFURLRef = CFURLCreateFromFSRef(NULL, &fileAsFSRef);
		s = CFURLCopyFileSystemPath(fileAsCFURLRef, kCFURLPOSIXPathStyle);

		CFShow(s);

		SetControlData(data, kControlEditTextPart,
			kControlEditTextCFStringTag, sizeof(CFStringRef), &s);
    		Draw1Control(data);
	}

	return noErr;
}

OSStatus KeyConfigHandler(EventHandlerCallRef h, EventRef event, void* data) {
	UInt32 key;
	CFStringRef s;
        GetEventParameter(event, kEventParamKeyCode,
		typeUInt32, NULL, sizeof(UInt32), NULL, &key);
    s = CFStringCreateWithFormat(NULL, NULL, CFSTR("%d"), key);
	SetControlData(data, kControlEditTextPart,
		kControlEditTextCFStringTag, sizeof(CFStringRef), &s);
	Draw1Control(data);

	return noErr;
}

void InstallBrowseHandler(WindowRef myWindow, const SInt32 ControllerId,
                          const SInt32 ControlledId)
{
    EventTypeSpec flist[] = {
      { kEventClassControl, kEventControlHit }
    };
    ControlID  Id;
    ControlRef Controller, Controlled;
    
    Id.signature = 'conf';
    Id.id = ControllerId;
    GetControlByID(myWindow, &Id, &Controller);
    Id.id = ControlledId;
    GetControlByID(myWindow, &Id, &Controlled);
    InstallControlEventHandler(Controller, NewEventHandlerUPP(BrowseHandler),
      GetEventTypeCount(flist), flist, Controlled, NULL);
}

WindowRef CreateSettingsWindow() {

  WindowRef myWindow;
  IBNibRef nib;

  EventTypeSpec eventList[] = {
    { kEventClassWindow, kEventWindowClose }
  };

  CreateNibReference(CFSTR("preferences"), &nib);
  CreateWindowFromNib(nib, CFSTR("Dialog"), &myWindow);

  load_settings(myWindow);

  InstallTabHandler(myWindow);

  {
    int i;
    ControlRef control, controlled;
    ControlID id;
    EventTypeSpec elist[] = {
      { kEventClassKeyboard, kEventRawKeyDown },
      { kEventClassKeyboard, kEventRawKeyUp }
    };

    id.signature = 'conf';
    i = 0;
    while(PerPadNames[i]) {
      id.id = 31 + i;
      GetControlByID(myWindow, &id, &control);

      InstallControlEventHandler(control, NewEventHandlerUPP(KeyConfigHandler),
	GetEventTypeCount(elist), elist, control, NULL);
      i++;
    }

    InstallBrowseHandler(myWindow, 50, 1);  /* BIOS */
    InstallBrowseHandler(myWindow, 51, 2);  /* CDROM */
    InstallBrowseHandler(myWindow, 52, 7);  /* Cartridge ROM */
    InstallBrowseHandler(myWindow, 53, 9);  /* Memory */
    InstallBrowseHandler(myWindow, 54, 10); /* MPEG ROM */
  }

  ShowWindow(myWindow);

  InstallWindowEventHandler(myWindow,
			    NewEventHandlerUPP (SettingsWindowEventHandler),
			    GetEventTypeCount(eventList),
			    eventList, myWindow, NULL);

  return myWindow;
}

OSStatus LoadWindowEventHandler (EventHandlerCallRef myHandler, EventRef theEvent, void* userData)
{
  OSStatus result = eventNotHandledErr;
  switch (GetEventKind (theEvent))
    {
    case kEventWindowClose:
      {
        WindowRef window;
        GetEventParameter(theEvent, kEventParamDirectObject, typeWindowRef,
          0, sizeof(typeWindowRef), 0, &window);

        load_file(window, loadtype);
			
        DisposeWindow(window);
      }
      result = noErr;
      break;
    }
  return (result);
}

WindowRef CreateLoadWindow(int type) {
  WindowRef myWindow;
  IBNibRef nib;
  int* hack;
  EventTypeSpec eventList[] = {
    { kEventClassWindow, kEventWindowClose }
  };

  CreateNibReference(CFSTR("load_dialog"), &nib);
  CreateWindowFromNib(nib, CFSTR("Dialog"), &myWindow);

  InstallTabHandler(myWindow);
  hack = malloc(sizeof(int));
  loadtype = type;
  InstallBrowseHandler(myWindow, 50, 1);  /* File */
  ShowWindow(myWindow);

  InstallWindowEventHandler(myWindow,
                            NewEventHandlerUPP (LoadWindowEventHandler),
                            GetEventTypeCount(eventList),
                            eventList, myWindow, NULL);
  return myWindow;
}
