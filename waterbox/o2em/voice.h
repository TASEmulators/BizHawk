#ifndef __VOICE_H
#define __VOICE_H

void load_voice_samples(char *path);
void update_voice(void);
void trigger_voice(int addr);
void reset_voice(void);
void set_voice_bank(int bank);
int get_voice_status(void);

#endif

