#ifndef __MDFN_DRIVERS_MEMDEBUGGER_H
#define __MDFN_DRIVERS_MEMDEBUGGER_H

void MemDebugger_Draw(MDFN_Surface *surface, const MDFN_Rect *rect, const MDFN_Rect *screen_rect);
void MemDebugger_SetActive(bool newia);
#ifndef HEADLESS
int MemDebugger_Event(const SDL_Event *event);
#endif

bool MemDebugger_Init(void);

#endif
