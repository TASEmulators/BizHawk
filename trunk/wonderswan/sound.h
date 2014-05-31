#ifndef __WSWAN_SOUND_H
#define __WSWAN_SOUND_H

#include "system.h"
#include <blip/Blip_Buffer.h>

namespace MDFN_IEN_WSWAN
{

class Sound
{
public:
	Sound();
	~Sound();

	int32 Flush(int16 *SoundBuf, const int32 MaxSoundFrames);

	void SetMultiplier(double multiplier);
	bool SetRate(uint32 rate);

	void Write(uint32, uint8);
	uint8 Read(uint32);
	void Reset();
	void CheckRAMWrite(uint32 A);

private:
	Blip_Synth<blip_good_quality, 256> WaveSynth;
	Blip_Synth<blip_med_quality, 256> NoiseSynth;
	Blip_Synth<blip_good_quality, 256 * 15> VoiceSynth;

	Blip_Buffer *sbuf[2]; // = { NULL };

	uint16 period[4];
	uint8 volume[4]; // left volume in upper 4 bits, right in lower 4 bits
	uint8 voice_volume;

	uint8 sweep_step, sweep_value;
	uint8 noise_control;
	uint8 control;
	uint8 output_control;

	int32 sweep_8192_divider;
	uint8 sweep_counter;
	uint8 SampleRAMPos;

	int32 sample_cache[4][2];

	int32 last_v_val;

	uint8 HyperVoice;
	int32 last_hv_val;

	int32 period_counter[4];
	int32 last_val[4][2]; // Last outputted value, l&r
	uint8 sample_pos[4];
	uint16 nreg;
	uint32 last_ts;

private:
	void Update();

public:
	System *sys;
	template<bool isReader>void SyncState(NewState *ns);

};

}

#endif
