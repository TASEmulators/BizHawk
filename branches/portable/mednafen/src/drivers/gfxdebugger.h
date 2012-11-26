#ifndef __MDFN_DRIVERS_GFXDEBUGGER_H
#define __MDFN_DRIVERS_GFXDEBUGGER_H

void GfxDebugger_Draw(MDFN_Surface *surface, const MDFN_Rect *rect, const MDFN_Rect *screen_rect);
void GfxDebugger_SetActive(bool newia);
#ifndef HEADLESS
int GfxDebugger_Event(const SDL_Event *event);
#endif

#endif
