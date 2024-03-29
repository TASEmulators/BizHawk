/* * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * *
 *   Mupen64plus - eventloop.c                                             *
 *   Mupen64Plus homepage: http://code.google.com/p/mupen64plus/           *
 *   Copyright (C) 2008-2009 Richard Goedeken                              *
 *   Copyright (C) 2008 Ebenblues Nmn Okaygo Tillin9                       *
 *                                                                         *
 *   This program is free software; you can redistribute it and/or modify  *
 *   it under the terms of the GNU General Public License as published by  *
 *   the Free Software Foundation; either version 2 of the License, or     *
 *   (at your option) any later version.                                   *
 *                                                                         *
 *   This program is distributed in the hope that it will be useful,       *
 *   but WITHOUT ANY WARRANTY; without even the implied warranty of        *
 *   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the         *
 *   GNU General Public License for more details.                          *
 *                                                                         *
 *   You should have received a copy of the GNU General Public License     *
 *   along with this program; if not, write to the                         *
 *   Free Software Foundation, Inc.,                                       *
 *   51 Franklin Street, Fifth Floor, Boston, MA 02110-1301, USA.          *
 * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * */

#include <stdio.h>
#include <stdlib.h>
#include <SDL.h>
#if ! SDL_VERSION_ATLEAST(1,3,0)

#define SDL_SCANCODE_ESCAPE SDLK_ESCAPE
#define SDL_NUM_SCANCODES SDLK_LAST
#define SDL_SCANCODE_F5 SDLK_F5
#define SDL_SCANCODE_F7 SDLK_F7
#define SDL_SCANCODE_F9 SDLK_F9
#define SDL_SCANCODE_F10 SDLK_F10
#define SDL_SCANCODE_F11 SDLK_F11
#define SDL_SCANCODE_F12 SDLK_F12
#define SDL_SCANCODE_P SDLK_p
#define SDL_SCANCODE_M SDLK_m
#define SDL_SCANCODE_RIGHTBRACKET SDLK_RIGHTBRACKET
#define SDL_SCANCODE_LEFTBRACKET SDLK_LEFTBRACKET
#define SDL_SCANCODE_F SDLK_f
#define SDL_SCANCODE_SLASH SDLK_SLASH
#define SDL_SCANCODE_G SDLK_g
#define SDL_SCANCODE_RETURN SDLK_RETURN
#define SDL_SCANCODE_0 SDLK_0
#define SDL_SCANCODE_1 SDLK_1
#define SDL_SCANCODE_2 SDLK_2
#define SDL_SCANCODE_3 SDLK_3
#define SDL_SCANCODE_4 SDLK_4
#define SDL_SCANCODE_5 SDLK_5
#define SDL_SCANCODE_6 SDLK_6
#define SDL_SCANCODE_7 SDLK_7
#define SDL_SCANCODE_8 SDLK_8
#define SDL_SCANCODE_9 SDLK_9
#define SDL_SCANCODE_UNKNOWN SDLK_UNKNOWN

#endif

#define M64P_CORE_PROTOTYPES 1
#include "main.h"
#include "eventloop.h"
#include "sdl_key_converter.h"
#include "util.h"
#include "api/callbacks.h"
#include "api/config.h"
#include "api/m64p_config.h"
#include "plugin/plugin.h"
#include "r4300/interupt.h"
#include "r4300/reset.h"

/* version number for CoreEvents config section */
#define CONFIG_PARAM_VERSION 1.00

static m64p_handle l_CoreEventsConfig = NULL;

/*********************************************************************************************************
* static variables and definitions for eventloop.c
*/

#define kbdFullscreen "Kbd Mapping Fullscreen"
#define kbdStop "Kbd Mapping Stop"
#define kbdPause "Kbd Mapping Pause"
#define kbdSave "Kbd Mapping Save State"
#define kbdLoad "Kbd Mapping Load State"
#define kbdIncrement "Kbd Mapping Increment Slot"
#define kbdReset "Kbd Mapping Reset"
#define kbdSpeeddown "Kbd Mapping Speed Down"
#define kbdSpeedup "Kbd Mapping Speed Up"
#define kbdScreenshot "Kbd Mapping Screenshot"
#define kbdMute "Kbd Mapping Mute"
#define kbdIncrease "Kbd Mapping Increase Volume"
#define kbdDecrease "Kbd Mapping Decrease Volume"
#define kbdForward "Kbd Mapping Fast Forward"
#define kbdAdvance "Kbd Mapping Frame Advance"
#define kbdGameshark "Kbd Mapping Gameshark"

typedef enum {joyFullscreen,
              joyStop,
              joyPause,
              joySave,
              joyLoad,
              joyIncrement,
              joyScreenshot,
              joyMute,
              joyIncrease,
              joyDecrease,
              joyForward,
              joyGameshark
} eJoyCommand;

static const char *JoyCmdName[] = { "Joy Mapping Fullscreen",
                                    "Joy Mapping Stop",
                                    "Joy Mapping Pause",
                                    "Joy Mapping Save State",
                                    "Joy Mapping Load State",
                                    "Joy Mapping Increment Slot",
                                    "Joy Mapping Screenshot",
                                    "Joy Mapping Mute",
                                    "Joy Mapping Increase Volume",
                                    "Joy Mapping Decrease Volume",
                                    "Joy Mapping Fast Forward",
                                    "Joy Mapping Gameshark"};

static const int NumJoyCommands = sizeof(JoyCmdName) / sizeof(const char *);

static int JoyCmdActive[16];  /* if extra joystick commands are added above, make sure there is enough room in this array */

static int GamesharkActive = 0;

/*********************************************************************************************************
* static functions for eventloop.c
*/

/** MatchJoyCommand
 *    This function processes an SDL event and updates the JoyCmdActive array if the
 *    event matches with the given command.
 *
 *    If the event activates a joystick command which was not previously active, then
 *    a 1 is returned.  If the event de-activates an command, a -1 is returned.  Otherwise
 *    (if the event does not match the command or active status didn't change), a 0 is returned.
 */
static int MatchJoyCommand(const SDL_Event *event, eJoyCommand cmd)
{
    const char *event_str = ConfigGetParamString(l_CoreEventsConfig, JoyCmdName[cmd]);
    int dev_number, input_number, input_value;
    char axis_direction;

    /* Empty string or non-joystick command */
    if (event_str == NULL || strlen(event_str) < 4 || event_str[0] != 'J')
        return 0;

    /* Evaluate event based on type of joystick input expected by the given command */
    switch (event_str[2])
    {
        /* Axis */
        case 'A':
            if (event->type != SDL_JOYAXISMOTION)
                return 0;
            if (sscanf(event_str, "J%dA%d%c", &dev_number, &input_number, &axis_direction) != 3)
                return 0;
            if (dev_number != event->jaxis.which || input_number != event->jaxis.axis)
                return 0;
            if (axis_direction == '+')
            {
                if (event->jaxis.value >= 15000 && JoyCmdActive[cmd] == 0)
                {
                    JoyCmdActive[cmd] = 1;
                    return 1;
                }
                else if (event->jaxis.value <= 8000 && JoyCmdActive[cmd] == 1)
                {
                    JoyCmdActive[cmd] = 0;
                    return -1;
                }
                return 0;
            }
            else if (axis_direction == '-')
            {
                if (event->jaxis.value <= -15000 && JoyCmdActive[cmd] == 0)
                {
                    JoyCmdActive[cmd] = 1;
                    return 1;
                }
                else if (event->jaxis.value >= -8000 && JoyCmdActive[cmd] == 1)
                {
                    JoyCmdActive[cmd] = 0;
                    return -1;
                }
                return 0;
            }
            else return 0; /* invalid axis direction in configuration parameter */
            break;
        /* Hat */
        case 'H':
            if (event->type != SDL_JOYHATMOTION)
                return 0;
            if (sscanf(event_str, "J%dH%dV%d", &dev_number, &input_number, &input_value) != 3)
                return 0;
            if (dev_number != event->jhat.which || input_number != event->jhat.hat)
                return 0;
            if ((event->jhat.value & input_value) == input_value && JoyCmdActive[cmd] == 0)
            {
                JoyCmdActive[cmd] = 1;
                return 1;
            }
            else if ((event->jhat.value & input_value) != input_value  && JoyCmdActive[cmd] == 1)
            {
                JoyCmdActive[cmd] = 0;
                return -1;
            }
            return 0;
            break;
        /* Button. */
        case 'B':
            if (event->type != SDL_JOYBUTTONDOWN && event->type != SDL_JOYBUTTONUP)
                return 0;
            if (sscanf(event_str, "J%dB%d", &dev_number, &input_number) != 2)
                return 0;
            if (dev_number != event->jbutton.which || input_number != event->jbutton.button)
                return 0;
            if (event->type == SDL_JOYBUTTONDOWN && JoyCmdActive[cmd] == 0)
            {
                JoyCmdActive[cmd] = 1;
                return 1;
            }
            else if (event->type == SDL_JOYBUTTONUP && JoyCmdActive[cmd] == 1)
            {
                JoyCmdActive[cmd] = 0;
                return -1;
            }
            return 0;
            break;
        default:
            /* bad configuration parameter */
            return 0;
    }

    /* impossible to reach this point */
    return 0;
}

/*********************************************************************************************************
* global functions
*/

void event_initialize(void)
{
    int i;
    /* set initial state of all joystick commands to 'off' */
    for (i = 0; i < NumJoyCommands; i++)
        JoyCmdActive[i] = 0;
}

int event_set_core_defaults(void)
{
    float fConfigParamsVersion;
    int bSaveConfig = 0;

    if (ConfigOpenSection("CoreEvents", &l_CoreEventsConfig) != M64ERR_SUCCESS || l_CoreEventsConfig == NULL)
    {
        DebugMessage(M64MSG_ERROR, "Failed to open CoreEvents config section.");
        return 0; /* fail */
    }

    if (ConfigGetParameter(l_CoreEventsConfig, "Version", M64TYPE_FLOAT, &fConfigParamsVersion, sizeof(float)) != M64ERR_SUCCESS)
    {
        DebugMessage(M64MSG_WARNING, "No version number in 'CoreEvents' config section. Setting defaults.");
        ConfigDeleteSection("CoreEvents");
        ConfigOpenSection("CoreEvents", &l_CoreEventsConfig);
        bSaveConfig = 1;
    }
    else if (((int) fConfigParamsVersion) != ((int) CONFIG_PARAM_VERSION))
    {
        DebugMessage(M64MSG_WARNING, "Incompatible version %.2f in 'CoreEvents' config section: current is %.2f. Setting defaults.", fConfigParamsVersion, (float) CONFIG_PARAM_VERSION);
        ConfigDeleteSection("CoreEvents");
        ConfigOpenSection("CoreEvents", &l_CoreEventsConfig);
        bSaveConfig = 1;
    }
    else if ((CONFIG_PARAM_VERSION - fConfigParamsVersion) >= 0.0001f)
    {
        /* handle upgrades */
        float fVersion = CONFIG_PARAM_VERSION;
        ConfigSetParameter(l_CoreEventsConfig, "Version", M64TYPE_FLOAT, &fVersion);
        DebugMessage(M64MSG_INFO, "Updating parameter set version in 'CoreEvents' config section to %.2f", fVersion);
        bSaveConfig = 1;
    }

    ConfigSetDefaultFloat(l_CoreEventsConfig, "Version", CONFIG_PARAM_VERSION,  "Mupen64Plus CoreEvents config parameter set version number.  Please don't change this version number.");
    /* Keyboard presses mapped to core functions */
    ConfigSetDefaultInt(l_CoreEventsConfig, kbdStop, sdl_native2keysym(SDL_SCANCODE_ESCAPE),          "SDL keysym for stopping the emulator");
    ConfigSetDefaultInt(l_CoreEventsConfig, kbdFullscreen, sdl_native2keysym(SDL_NUM_SCANCODES),      "SDL keysym for switching between fullscreen/windowed modes");
    ConfigSetDefaultInt(l_CoreEventsConfig, kbdSave, sdl_native2keysym(SDL_SCANCODE_F5),              "SDL keysym for saving the emulator state");
    ConfigSetDefaultInt(l_CoreEventsConfig, kbdLoad, sdl_native2keysym(SDL_SCANCODE_F7),              "SDL keysym for loading the emulator state");
    ConfigSetDefaultInt(l_CoreEventsConfig, kbdIncrement, sdl_native2keysym(SDL_SCANCODE_UNKNOWN),    "SDL keysym for advancing the save state slot");
    ConfigSetDefaultInt(l_CoreEventsConfig, kbdReset, sdl_native2keysym(SDL_SCANCODE_F9),             "SDL keysym for resetting the emulator");
    ConfigSetDefaultInt(l_CoreEventsConfig, kbdSpeeddown, sdl_native2keysym(SDL_SCANCODE_F10),        "SDL keysym for slowing down the emulator");
    ConfigSetDefaultInt(l_CoreEventsConfig, kbdSpeedup, sdl_native2keysym(SDL_SCANCODE_F11),          "SDL keysym for speeding up the emulator");
    ConfigSetDefaultInt(l_CoreEventsConfig, kbdScreenshot, sdl_native2keysym(SDL_SCANCODE_F12),       "SDL keysym for taking a screenshot");
    ConfigSetDefaultInt(l_CoreEventsConfig, kbdPause, sdl_native2keysym(SDL_SCANCODE_P),              "SDL keysym for pausing the emulator");
    ConfigSetDefaultInt(l_CoreEventsConfig, kbdMute, sdl_native2keysym(SDL_SCANCODE_M),               "SDL keysym for muting/unmuting the sound");
    ConfigSetDefaultInt(l_CoreEventsConfig, kbdIncrease, sdl_native2keysym(SDL_SCANCODE_RIGHTBRACKET),"SDL keysym for increasing the volume");
    ConfigSetDefaultInt(l_CoreEventsConfig, kbdDecrease, sdl_native2keysym(SDL_SCANCODE_LEFTBRACKET), "SDL keysym for decreasing the volume");
    ConfigSetDefaultInt(l_CoreEventsConfig, kbdForward, sdl_native2keysym(SDL_SCANCODE_F),            "SDL keysym for temporarily going really fast");
    ConfigSetDefaultInt(l_CoreEventsConfig, kbdAdvance, sdl_native2keysym(SDL_SCANCODE_SLASH),        "SDL keysym for advancing by one frame when paused");
    ConfigSetDefaultInt(l_CoreEventsConfig, kbdGameshark, sdl_native2keysym(SDL_SCANCODE_G),          "SDL keysym for pressing the game shark button");
    /* Joystick events mapped to core functions */
    ConfigSetDefaultString(l_CoreEventsConfig, JoyCmdName[joyStop], "",       "Joystick event string for stopping the emulator");
    ConfigSetDefaultString(l_CoreEventsConfig, JoyCmdName[joyFullscreen], "", "Joystick event string for switching between fullscreen/windowed modes");
    ConfigSetDefaultString(l_CoreEventsConfig, JoyCmdName[joySave], "",       "Joystick event string for saving the emulator state");
    ConfigSetDefaultString(l_CoreEventsConfig, JoyCmdName[joyLoad], "",       "Joystick event string for loading the emulator state");
    ConfigSetDefaultString(l_CoreEventsConfig, JoyCmdName[joyIncrement], "",  "Joystick event string for advancing the save state slot");
    ConfigSetDefaultString(l_CoreEventsConfig, JoyCmdName[joyScreenshot], "", "Joystick event string for taking a screenshot");
    ConfigSetDefaultString(l_CoreEventsConfig, JoyCmdName[joyPause], "",      "Joystick event string for pausing the emulator");
    ConfigSetDefaultString(l_CoreEventsConfig, JoyCmdName[joyMute], "",       "Joystick event string for muting/unmuting the sound");
    ConfigSetDefaultString(l_CoreEventsConfig, JoyCmdName[joyIncrease], "",   "Joystick event string for increasing the volume");
    ConfigSetDefaultString(l_CoreEventsConfig, JoyCmdName[joyDecrease], "",   "Joystick event string for decreasing the volume");
    ConfigSetDefaultString(l_CoreEventsConfig, JoyCmdName[joyForward], "",    "Joystick event string for fast-forward");
    ConfigSetDefaultString(l_CoreEventsConfig, JoyCmdName[joyGameshark], "",  "Joystick event string for pressing the game shark button");

    if (bSaveConfig)
        ConfigSaveSection("CoreEvents");

    return 1;
}

static int get_saveslot_from_keysym(int keysym)
{
    switch (keysym) {
    case SDL_SCANCODE_0:
        return 0;
    case SDL_SCANCODE_1:
        return 1;
    case SDL_SCANCODE_2:
        return 2;
    case SDL_SCANCODE_3:
        return 3;
    case SDL_SCANCODE_4:
        return 4;
    case SDL_SCANCODE_5:
        return 5;
    case SDL_SCANCODE_6:
        return 6;
    case SDL_SCANCODE_7:
        return 7;
    case SDL_SCANCODE_8:
        return 8;
    case SDL_SCANCODE_9:
        return 9;
    default:
        return -1;
    }
}

/*********************************************************************************************************
* sdl keyup/keydown handlers
*/

void event_sdl_keydown(int keysym, int keymod)
{
    input.keyDown(keymod, keysym);
}

void event_sdl_keyup(int keysym, int keymod)
{
	input.keyUp(keymod, keysym);
}

int event_gameshark_active(void)
{
    return GamesharkActive;
}

void event_set_gameshark(int active)
{
    // if boolean value doesn't change then just return
    if (!active == !GamesharkActive)
        return;

    // set the button state
    GamesharkActive = (active ? 1 : 0);

    // notify front-end application that gameshark button state has changed
    StateChanged(M64CORE_INPUT_GAMESHARK, GamesharkActive);
}

