//
// System time handlers
//
// by James Hammons
// (C) 2010 Underground Software
//
// JLH = James Hammons <jlhamm@acm.org>
//
// Who  When        What
// ---  ----------  -------------------------------------------------------------
// JLH  01/16/2010  Created this log ;-)
//

//
// STILL TO DO:
//
// - Handling for an event that occurs NOW
//

#include "event.h"

#include <stdint.h>
#include "log.h"


//#define EVENT_LIST_SIZE       512
#define EVENT_LIST_SIZE       32


// Now, a bit of weirdness: It seems that the number of lines displayed on the screen
// makes the effective refresh rate either 30 or 25 Hz!

// NOTE ABOUT TIMING SYSTEM DATA STRUCTURES:

// A queue won't work for this system because we can't guarantee that an event will go
// in with a time that is later than the ones already queued up. So we just use a simple
// list.

// Although if we used an insertion sort we could, but it wouldn't work for adjusting
// times... (For that, you would have to remove the event then reinsert it.)

struct Event
{
	bool valid;
	int eventType;
	double eventTime;
	void (* timerCallback)(void);
};


static Event eventList[EVENT_LIST_SIZE];
static Event eventListJERRY[EVENT_LIST_SIZE];
static uint32_t nextEvent;
static uint32_t nextEventJERRY;
static uint32_t numberOfEvents;


void InitializeEventList(void)
{
	for(uint32_t i=0; i<EVENT_LIST_SIZE; i++)
	{
		eventList[i].valid = false;
		eventListJERRY[i].valid = false;
	}

	numberOfEvents = 0;
	WriteLog("EVENT: Cleared event list.\n");
}


// Set callback time in µs. This is fairly arbitrary, but works well enough for our purposes.
//We just slap the next event into the list in the first available slot, no checking, no nada...
void SetCallbackTime(void (* callback)(void), double time, int type/*= EVENT_MAIN*/)
{
	if (type == EVENT_MAIN)
	{
		for(uint32_t i=0; i<EVENT_LIST_SIZE; i++)
		{
			if (!eventList[i].valid)
			{
//WriteLog("EVENT: Found callback slot #%u...\n", i);
				eventList[i].timerCallback = callback;
				eventList[i].eventTime = time;
				eventList[i].eventType = type;
				eventList[i].valid = true;
				numberOfEvents++;

				return;
			}
		}

		WriteLog("EVENT: SetCallbackTime() failed to find an empty slot in the main list (%u events)!\n", numberOfEvents);
	}
	else
	{
		for(uint32_t i=0; i<EVENT_LIST_SIZE; i++)
		{
			if (!eventListJERRY[i].valid)
			{
//WriteLog("EVENT: Found callback slot #%u...\n", i);
				eventListJERRY[i].timerCallback = callback;
				eventListJERRY[i].eventTime = time;
				eventListJERRY[i].eventType = type;
				eventListJERRY[i].valid = true;
				numberOfEvents++;

				return;
			}
		}

		WriteLog("EVENT: SetCallbackTime() failed to find an empty slot in the main list (%u events)!\n", numberOfEvents);
	}
}


void RemoveCallback(void (* callback)(void))
{
	for(uint32_t i=0; i<EVENT_LIST_SIZE; i++)
	{
		if (eventList[i].valid && eventList[i].timerCallback == callback)
		{
			eventList[i].valid = false;
			numberOfEvents--;

			return;
		}
		else if (eventListJERRY[i].valid && eventListJERRY[i].timerCallback == callback)
		{
			eventListJERRY[i].valid = false;
			numberOfEvents--;

			return;
		}
	}
}


void AdjustCallbackTime(void (* callback)(void), double time)
{
	for(uint32_t i=0; i<EVENT_LIST_SIZE; i++)
	{
		if (eventList[i].valid && eventList[i].timerCallback == callback)
		{
			eventList[i].eventTime = time;

			return;
		}
		else if (eventListJERRY[i].valid && eventListJERRY[i].timerCallback == callback)
		{
			eventListJERRY[i].eventTime = time;

			return;
		}
	}
}


//
// Since our list is unordered WRT time, we have to search it to find the next event
// Returns time to next event & sets nextEvent to that event
//
double GetTimeToNextEvent(int type/*= EVENT_MAIN*/)
{
#if 0
	double time = 0;
	bool firstTime = true;

	for(uint32 i=0; i<EVENT_LIST_SIZE; i++)
	{
		if (eventList[i].valid)
		{
			if (firstTime)
				time = eventList[i].eventTime, nextEvent = i, firstTime = false;
			else
			{
				if (eventList[i].eventTime < time)
					time = eventList[i].eventTime, nextEvent = i;
			}
		}
	}
#else
	if (type == EVENT_MAIN)
	{
		double time = eventList[0].eventTime;
		nextEvent = 0;

		for(uint32_t i=1; i<EVENT_LIST_SIZE; i++)
		{
			if (eventList[i].valid && (eventList[i].eventTime < time))
			{
				time = eventList[i].eventTime;
				nextEvent = i;
			}
		}

		return time;
	}
	else
	{
		double time = eventListJERRY[0].eventTime;
		nextEventJERRY = 0;

		for(uint32_t i=1; i<EVENT_LIST_SIZE; i++)
		{
			if (eventListJERRY[i].valid && (eventListJERRY[i].eventTime < time))
			{
				time = eventListJERRY[i].eventTime;
				nextEventJERRY = i;
			}
		}

		return time;
	}
#endif
}


void HandleNextEvent(int type/*= EVENT_MAIN*/)
{
	if (type == EVENT_MAIN)
	{
		double elapsedTime = eventList[nextEvent].eventTime;
		void (* event)(void) = eventList[nextEvent].timerCallback;

		for(uint32_t i=0; i<EVENT_LIST_SIZE; i++)
		{
	//We can skip the check & just subtract from everything, since the check is probably
	//just as heavy as the code after and we won't use the elapsed time from an invalid event anyway.
	//		if (eventList[i].valid)
				eventList[i].eventTime -= elapsedTime;
		}

		eventList[nextEvent].valid = false;			// Remove event from list...
		numberOfEvents--;

		(*event)();
	}
	else
	{
		double elapsedTime = eventListJERRY[nextEventJERRY].eventTime;
		void (* event)(void) = eventListJERRY[nextEventJERRY].timerCallback;

		for(uint32_t i=0; i<EVENT_LIST_SIZE; i++)
		{
	//We can skip the check & just subtract from everything, since the check is probably
	//just as heavy as the code after and we won't use the elapsed time from an invalid event anyway.
	//		if (eventList[i].valid)
				eventListJERRY[i].eventTime -= elapsedTime;
		}

		eventListJERRY[nextEventJERRY].valid = false;	// Remove event from list...
		numberOfEvents--;

		(*event)();
	}
}


/*
void OPCallback(void)
{
	DoFunkyOPStuffHere();

	SetCallbackTime(OPCallback, HORIZ_PERIOD_IN_USEC);
}

void VICallback(void)
{
	double oneFrameInUsec = 16666.66666666;
	SetCallbackTime(VICallback, oneFrameInUsec / numberOfLines);
}

void JaguarInit(void)
{
	double oneFrameInUsec = 16666.66666666;
	SetCallbackTime(VICallback, oneFrameInUsec / numberOfLines);
	SetCallbackTime(OPCallback, );
}

void JaguarExec(void)
{
	while (true)
	{
		double timeToNextEvent = GetTimeToNextEvent();

		m68k_execute(USEC_TO_M68K_CYCLES(timeToNextEvent));
		GPUExec(USEC_TO_RISC_CYCLES(timeToNextEvent));
		DSPExec(USEC_TO_RISC_CYCLES(timeToNextEvent));

		if (!HandleNextEvent())
			break;
	}
}

// NOTES: The timers count RISC cycles, and when the dividers count down to zero they can interrupt either the DSP and/or CPU.

// NEW:
// TOM Programmable Interrupt Timer handler
// NOTE: TOM's PIT is only enabled if the prescaler is != 0
//       The PIT only generates an interrupt when it counts down to zero, not when loaded!

void TOMResetPIT()
{
	// Need to remove previous timer from the queue, if it exists...
	RemoveCallback(TOMPITCallback);

	if (TOMPITPrescaler)
	{
		double usecs = (TOMPITPrescaler + 1) * (TOMPITDivider + 1) * RISC_CYCLE_IN_USEC;
		SetCallbackTime(TOMPITCallback, usecs);
	}
}

void TOMPITCallback(void)
{
	INT1_RREG |= 0x08;                         // Set TOM PIT interrupt pending
	GPUSetIRQLine(GPUIRQ_TIMER, ASSERT_LINE);  // It does the 'IRQ enabled' checking

	if (INT1_WREG & 0x08)
		m68k_set_irq(2);                       // Generate 68K NMI

	TOMResetPIT();
}

// Small problem with this approach: If a timer interrupt is already pending,
// the pending timer needs to be replaced with the new one! (Taken care of above, BTW...)

TOMWriteWord(uint32 address, uint16 data)
{
	if (address == PIT0)
	{
		TOMPITPrescaler = data;
		TOMResetPIT();
	}
	else if (address == PIT1)
	{
		TOMPITDivider = data;
		TOMResetPIT();
	}
}

*/
