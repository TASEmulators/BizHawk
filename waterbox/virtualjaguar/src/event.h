//
// EVENT.H: System timing support functionality
//
// by James Hammons
//

#ifndef __EVENT_H__
#define __EVENT_H__

enum { EVENT_MAIN, EVENT_JERRY };

//NTSC Timings...
#define RISC_CYCLE_IN_USEC			0.03760684198
#define M68K_CYCLE_IN_USEC			(RISC_CYCLE_IN_USEC * 2)
//PAL Timings
#define RISC_CYCLE_PAL_IN_USEC		0.03760260812
#define M68K_CYCLE_PAL_IN_USEC		(RISC_CYCLE_PAL_IN_USEC * 2)

#define HORIZ_PERIOD_IN_USEC_NTSC	63.555555555
#define HORIZ_PERIOD_IN_USEC_PAL	64.0

#define USEC_TO_RISC_CYCLES(u) (uint32_t)(((u) / (vjs.hardwareTypeNTSC ? RISC_CYCLE_IN_USEC : RISC_CYCLE_PAL_IN_USEC)) + 0.5)
#define USEC_TO_M68K_CYCLES(u) (uint32_t)(((u) / (vjs.hardwareTypeNTSC ? M68K_CYCLE_IN_USEC : M68K_CYCLE_PAL_IN_USEC)) + 0.5)

void InitializeEventList(void);
void SetCallbackTime(void (* callback)(void), double time, int type = EVENT_MAIN);
void RemoveCallback(void (* callback)(void));
void AdjustCallbackTime(void (* callback)(void), double time);
double GetTimeToNextEvent(int type = EVENT_MAIN);
void HandleNextEvent(int type = EVENT_MAIN);

#endif	// __EVENT_H__
