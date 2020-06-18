#include <stdbool.h>
#include <stdio.h>
#include "SDL_stdinc.h"
#include "audio.h"
#include "sound.h"
#include "configuration.h"
#include "crossbar.h"
#include "dmaSnd.h"

int nAudioFrequency = 44100;

/*-----------------------------------------------------------------------*/
/**
 * Set audio playback frequency variable, pass as PLAYBACK_xxxx
 */
void Audio_SetOutputAudioFreq(int nNewFrequency)
{
	/* Do not reset sound system if nothing has changed! */
	if (nNewFrequency != nAudioFrequency)
	{
		/* Set new frequency */
		nAudioFrequency = nNewFrequency;

		if (Config_IsMachineFalcon())
		{
			/* Compute Ratio between host computer sound frequency and Hatari's sound frequency. */
			Crossbar_Compute_Ratio();
		}
		else if (!Config_IsMachineST())
		{
			/* Adapt LMC filters to this new frequency */
			DmaSnd_Init_Bass_and_Treble_Tables();
		}

		/* Re-open SDL audio interface if necessary: */
		// if (bSoundWorking)
		// {
		// 	Audio_UnInit();
		// 	Audio_Init();
		// }
	}

	/* Apply YM2149 C10 low pass filter ? (except if forced to NONE) */
	if ( YM2149_LPF_Filter != YM2149_LPF_FILTER_NONE )
	{
		if ( Config_IsMachineST() && nAudioFrequency >= 40000 )
			YM2149_LPF_Filter = YM2149_LPF_FILTER_LPF_STF;
		else
			YM2149_LPF_Filter = YM2149_LPF_FILTER_PWM;
	}
}

void Audio_Lock(void)
{}
void Audio_Unlock(void)
{}

int SoundBufferSize = 1024 / 4;			/* Size of sound buffer (in samples) */
int CompleteSndBufIdx;				/* Replay-index into MixBuffer */
