/*  Copyright 2005 Guillaume Duhamel
	Copyright 2005-2006 Theo Berkau
	Copyright 2008 Filipe Azevedo

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

#ifdef HAVE_LIBSDL
#ifdef __APPLE__
	#include <SDL/SDL.h>
#else
	#include "SDL.h"
#endif

#include "debug.h"
#include "persdljoy.h"

#define SDL_MAX_AXIS_VALUE 0x110000
#define SDL_MIN_AXIS_VALUE 0x100000
#define SDL_MEDIUM_AXIS_VALUE (int)(32768 / 2)
#define SDL_BUTTON_PRESSED 1
#define SDL_BUTTON_RELEASED 0

int PERSDLJoyInit(void);
void PERSDLJoyDeInit(void);
int PERSDLJoyHandleEvents(void);
void PERSDLJoyNothing(void);

u32 PERSDLJoyScan(void);
void PERSDLJoyFlush(void);
void PERSDLKeyName(u32 key, char * name, int size);

PerInterface_struct PERSDLJoy = {
PERCORE_SDLJOY,
"SDL Joystick Interface",
PERSDLJoyInit,
PERSDLJoyDeInit,
PERSDLJoyHandleEvents,
PERSDLJoyNothing,
PERSDLJoyScan,
1,
PERSDLJoyFlush
#ifdef PERKEYNAME
,PERSDLKeyName
#endif
};

typedef struct {
	SDL_Joystick* mJoystick;
	s16* mScanStatus;
} PERSDLJoystick;

unsigned int SDL_PERCORE_INITIALIZED = 0;
unsigned int SDL_PERCORE_JOYSTICKS_INITIALIZED = 0;
PERSDLJoystick* SDL_PERCORE_JOYSTICKS = 0;

//////////////////////////////////////////////////////////////////////////////

int PERSDLJoyInit(void) {
	int i, j;

	// does not need init if already done
	if ( SDL_PERCORE_INITIALIZED )
	{
		return 0;
	}
	
	// init joysticks
	if ( SDL_InitSubSystem( SDL_INIT_JOYSTICK ) == -1 )
	{
		return -1;
	}
	
	// ignore joysticks event in sdl event loop
	SDL_JoystickEventState( SDL_IGNORE );
	
	// open joysticks
	SDL_PERCORE_JOYSTICKS_INITIALIZED = SDL_NumJoysticks();
	SDL_PERCORE_JOYSTICKS = malloc(sizeof(PERSDLJoystick) * SDL_PERCORE_JOYSTICKS_INITIALIZED);
	for ( i = 0; i < SDL_PERCORE_JOYSTICKS_INITIALIZED; i++ )
	{
		SDL_Joystick* joy = SDL_JoystickOpen( i );
		
		SDL_JoystickUpdate();
		
		SDL_PERCORE_JOYSTICKS[ i ].mJoystick = joy;
		SDL_PERCORE_JOYSTICKS[ i ].mScanStatus = joy ? malloc(sizeof(s16) * SDL_JoystickNumAxes( joy )) : 0;
		
		if ( joy )
		{
			for ( j = 0; j < SDL_JoystickNumAxes( joy ); j++ )
			{
				SDL_PERCORE_JOYSTICKS[ i ].mScanStatus[ j ] = SDL_JoystickGetAxis( joy, j );
			}
		}
	}
	
	// success
	SDL_PERCORE_INITIALIZED = 1;
	return 0;
}

//////////////////////////////////////////////////////////////////////////////

void PERSDLJoyDeInit(void) {
	// close joysticks
	if ( SDL_PERCORE_INITIALIZED == 1 )
	{
		int i;
		for ( i = 0; i < SDL_PERCORE_JOYSTICKS_INITIALIZED; i++ )
		{
			if ( SDL_JoystickOpened( i ) )
			{
				SDL_JoystickClose( SDL_PERCORE_JOYSTICKS[ i ].mJoystick );
				free( SDL_PERCORE_JOYSTICKS[ i ].mScanStatus );
			}
		}
		free( SDL_PERCORE_JOYSTICKS );
	}
	
	SDL_PERCORE_JOYSTICKS_INITIALIZED = 0;
	SDL_PERCORE_INITIALIZED = 0;
	
	// close sdl joysticks
	SDL_QuitSubSystem( SDL_INIT_JOYSTICK );
}

//////////////////////////////////////////////////////////////////////////////

void PERSDLJoyNothing(void) {
}

//////////////////////////////////////////////////////////////////////////////

int PERSDLJoyHandleEvents(void) {
	int joyId;
	int i;
	SDL_Joystick* joy;
	Sint16 cur;
	Uint8 buttonState;
	
	// update joysticks states
	SDL_JoystickUpdate();
	
	// check each joysticks
	for ( joyId = 0; joyId < SDL_PERCORE_JOYSTICKS_INITIALIZED; joyId++ )
	{
		joy = SDL_PERCORE_JOYSTICKS[ joyId ].mJoystick;
		
		if ( !joy )
		{
			continue;
		}
		
		// check axis
		for ( i = 0; i < SDL_JoystickNumAxes( joy ); i++ )
		{
			cur = SDL_JoystickGetAxis( joy, i );
			
			if ( cur < -SDL_MEDIUM_AXIS_VALUE )
			{
				PerKeyUp( (joyId << 18) | SDL_MAX_AXIS_VALUE | i );
				PerKeyDown( (joyId << 18) | SDL_MIN_AXIS_VALUE | i );
			}
			else if ( cur > SDL_MEDIUM_AXIS_VALUE )
			{
				PerKeyUp( (joyId << 18) | SDL_MIN_AXIS_VALUE | i );
				PerKeyDown( (joyId << 18) | SDL_MAX_AXIS_VALUE | i );
			}
			else
			{
				PerKeyUp( (joyId << 18) | SDL_MIN_AXIS_VALUE | i );
				PerKeyUp( (joyId << 18) | SDL_MAX_AXIS_VALUE | i );
			}
		}
		
		// check buttons
		for ( i = 0; i < SDL_JoystickNumButtons( joy ); i++ )
		{
			buttonState = SDL_JoystickGetButton( joy, i );
			
			if ( buttonState == SDL_BUTTON_PRESSED )
			{
				PerKeyDown( (joyId << 18) | (i +1) );
			}
			else if ( buttonState == SDL_BUTTON_RELEASED )
			{
				PerKeyUp( (joyId << 18) | (i +1) );
			}
		}
	}
	
	// execute yabause
	if ( YabauseExec() != 0 )
	{
		return -1;
	}
	
	// return success
	return 0;
}

//////////////////////////////////////////////////////////////////////////////

u32 PERSDLJoyScan( void ) {
	// init vars
	int joyId;
	int i;
	SDL_Joystick* joy;
	Sint16 cur;
	
	// update joysticks states
	SDL_JoystickUpdate();
	
	// check each joysticks
	for ( joyId = 0; joyId < SDL_PERCORE_JOYSTICKS_INITIALIZED; joyId++ )
	{
		joy = SDL_PERCORE_JOYSTICKS[ joyId ].mJoystick;
		
		if ( !joy )
		{
			continue;
		}
	
		// check axis
		for ( i = 0; i < SDL_JoystickNumAxes( joy ); i++ )
		{
			cur = SDL_JoystickGetAxis( joy, i );

			if ( cur != SDL_PERCORE_JOYSTICKS[ joyId ].mScanStatus[ i ] )
			{
				if ( cur < -SDL_MEDIUM_AXIS_VALUE )
				{
					return (joyId << 18) | SDL_MIN_AXIS_VALUE | i;
				}
				else if ( cur > SDL_MEDIUM_AXIS_VALUE )
				{
					return (joyId << 18) | SDL_MAX_AXIS_VALUE | i;
				}
			}
		}

		// check buttons
		for ( i = 0; i < SDL_JoystickNumButtons( joy ); i++ )
		{
			if ( SDL_JoystickGetButton( joy, i ) == SDL_BUTTON_PRESSED )
			{
				return (joyId << 18) | (i +1);
				break;
			}
		}
	}

	return 0;
}

void PERSDLJoyFlush(void) {
}

void PERSDLKeyName(u32 key, char * name, UNUSED int size)
{
	sprintf(name, "%x", (int)key);
}

#endif
