/*  Copyright 2006 Anders Montonen

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

#include <Carbon/Carbon.h>

#include "cpustatus.h"
#include "../sh2core.h"

/* Dialog field IDs */

#define SLAVE_ID_OFFSET 30

#define R0_ID   1
#define R1_ID   2
#define R2_ID   3
#define R3_ID   4
#define R4_ID   5
#define R5_ID   6
#define R6_ID   7
#define R7_ID   8
#define R8_ID   9
#define R9_ID   10
#define R10_ID  11
#define R11_ID  12
#define R12_ID  13
#define R13_ID  14
#define R14_ID  15
#define R15_ID  16
#define MACH_ID 17
#define MACL_ID 18
#define GBR_ID  19
#define VBR_ID  20
#define PC_ID   21
#define PR_ID   22
#define SR_ID   23

static WindowRef CPUStatusWindow;

static OSStatus CPUStatusWindowEventHandler(EventHandlerCallRef myHandler,
                                            EventRef theEvent,
                                            void *userData)
{
    OSStatus    result = eventNotHandledErr;
    WindowRef   window;
    MenuRef     menu;
    
    switch(GetEventKind(theEvent))
    {
    case kEventWindowClose:
        GetEventParameter(theEvent, kEventParamDirectObject, typeWindowRef,
                          0, sizeof(typeWindowRef), 0, &window);
        DisposeWindow(window);
        menu = GetMenuRef(1);
        ChangeMenuItemAttributes(menu, 4, 0, kMenuItemAttrHidden);
        ChangeMenuItemAttributes(menu, 5, kMenuItemAttrHidden, 0);
        
        result = noErr;
        break;
    }
    
    return result;
}

void ShowCPUStatusWindow(void)
{
    IBNibRef  nib;
    
    EventTypeSpec eventList[] = { {kEventClassWindow, kEventWindowClose} };
    
    CreateNibReference(CFSTR("cpustatus"), &nib);
    CreateWindowFromNib(nib, CFSTR("Window"), &CPUStatusWindow);
    ShowWindow(CPUStatusWindow);
    
    InstallWindowEventHandler(CPUStatusWindow,
                              NewEventHandlerUPP(CPUStatusWindowEventHandler),
                              GetEventTypeCount(eventList),
                              eventList, CPUStatusWindow, NULL);
    
    UpdateCPUStatusWindow();
}

void HideCPUStatusWindow(void)
{
    DisposeWindow(CPUStatusWindow);
}

static void SetSRString(u32 SR, CFMutableStringRef s)
{
    int ii;
    
    for(ii = 0; ii < 32; ii++)
    {
        if(SR & 0x80000000)
            CFStringAppendCString(s, "1", kCFStringEncodingASCII);
        else
            CFStringAppendCString(s, "0", kCFStringEncodingASCII);
        
        SR <<= 1;
    }
}

static void SetRegisterValue(const int controlId, CFStringRef s)
{
    ControlID   id;
    ControlRef  control;
    
    id.signature = 'cpus';
    id.id = controlId;
    GetControlByID(CPUStatusWindow, &id, &control);
    SetControlData(control, kControlEditTextPart,
                   kControlEditTextCFStringTag, sizeof(CFStringRef), &s);
    
}

void UpdateCPUStatusWindow(void)
{
    CFStringRef s;
    CFMutableStringRef ms;
    sh2regs_struct master = {0};
    sh2regs_struct slave = {0};
    int ii = 0;
    int srNumber = 0;
    
    if(MSH2)
        SH2GetRegisters(MSH2, &master);
    
    if(SSH2)
        SH2GetRegisters(SSH2, &slave);
    
    /* Master registers */
    for(ii = 0; ii < 16; ii++)
    {
        s = CFStringCreateWithFormat(kCFAllocatorDefault, NULL,
                                     CFSTR("%x"), master.R[ii]);
        SetRegisterValue(ii+1, s);
    }

    s = CFStringCreateWithFormat(kCFAllocatorDefault, NULL,
                                 CFSTR("%x"), master.MACH);
    SetRegisterValue(MACH_ID, s);
    s = CFStringCreateWithFormat(kCFAllocatorDefault, NULL,
                                 CFSTR("%x"), master.MACL);
    SetRegisterValue(MACL_ID, s);
    s = CFStringCreateWithFormat(kCFAllocatorDefault, NULL,
                                 CFSTR("%x"), master.GBR);
    SetRegisterValue(GBR_ID, s);
    s = CFStringCreateWithFormat(kCFAllocatorDefault, NULL,
                                 CFSTR("%x"), master.VBR);
    SetRegisterValue(VBR_ID, s);
    s = CFStringCreateWithFormat(kCFAllocatorDefault, NULL,
                                 CFSTR("%x"), master.PC);
    SetRegisterValue(PC_ID, s);
    s = CFStringCreateWithFormat(kCFAllocatorDefault, NULL,
                                 CFSTR("%x"), master.PR);
    SetRegisterValue(PR_ID, s);
    
    ms = CFStringCreateMutable(kCFAllocatorDefault, 32);
    SetSRString(master.SR.all, ms);
    SetRegisterValue(SR_ID, ms);
    
    /* Slave registers */
    for(ii = 0; ii < 16; ii++)
    {
        s = CFStringCreateWithFormat(kCFAllocatorDefault, NULL,
                                     CFSTR("%x"), slave.R[ii]);
        SetRegisterValue(ii+1+SLAVE_ID_OFFSET, s);
    }

    s = CFStringCreateWithFormat(kCFAllocatorDefault, NULL,
                                 CFSTR("%x"), slave.MACH);
    SetRegisterValue(MACH_ID+SLAVE_ID_OFFSET, s);
    s = CFStringCreateWithFormat(kCFAllocatorDefault, NULL,
                                 CFSTR("%x"), slave.MACL);
    SetRegisterValue(MACL_ID+SLAVE_ID_OFFSET, s);
    s = CFStringCreateWithFormat(kCFAllocatorDefault, NULL,
                                 CFSTR("%x"), slave.GBR);
    SetRegisterValue(GBR_ID+SLAVE_ID_OFFSET, s);
    s = CFStringCreateWithFormat(kCFAllocatorDefault, NULL,
                                 CFSTR("%x"), slave.VBR);
    SetRegisterValue(VBR_ID+SLAVE_ID_OFFSET, s);
    s = CFStringCreateWithFormat(kCFAllocatorDefault, NULL,
                                 CFSTR("%x"), slave.PC);
    SetRegisterValue(PC_ID+SLAVE_ID_OFFSET, s);
    s = CFStringCreateWithFormat(kCFAllocatorDefault, NULL,
                                 CFSTR("%x"), slave.PR);
    SetRegisterValue(PR_ID+SLAVE_ID_OFFSET, s);
    ms = CFStringCreateMutable(kCFAllocatorDefault, 32);
    SetSRString(slave.SR.all, ms);
    SetRegisterValue(SR_ID+SLAVE_ID_OFFSET, ms);
}
