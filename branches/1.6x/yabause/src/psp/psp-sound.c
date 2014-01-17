/*  src/psp/psp-sound.c: PSP sound output module
    Copyright 2009-2010 Andrew Church

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

#include "common.h"

#include "../memory.h"
#include "../scsp.h"

#include "config.h"
#include "me.h"
#include "me-utility.h"
#include "sys.h"
#include "psp-sound.h"

/* Macro for uncached access to variables / structure fields */
#define UNCACHED(var) (*((typeof(&var))((uint32_t)(&var) | 0x40000000)))

/*************************************************************************/
/************************* Configuration options *************************/
/*************************************************************************/

/**
 * DUMP_AUDIO:  When defined, the program will write a dump of all audio
 * data sent to the hardware (except filler data sent when the emulator
 * falls behind real time) to "audio.pcm" in the current directory.
 */
// #define DUMP_AUDIO

/*************************************************************************/
/************************* Interface definition **************************/
/*************************************************************************/

/* Interface function declarations (must come before interface definition) */

static int psp_sound_init(void);
static void psp_sound_deinit(void);
static int psp_sound_reset(void);
static int psp_sound_change_video_format(int vertfreq);
static void psp_sound_update_audio(u32 *leftchanbuffer, u32 *rightchanbuffer,
                                   u32 num_samples);
static u32 psp_sound_get_audio_space(void);
static void psp_sound_mute_audio(void);
static void psp_sound_unmute_audio(void);
static void psp_sound_set_volume(int volume);

/*-----------------------------------------------------------------------*/

/* Module interface definition */

SoundInterface_struct SNDPSP = {
    .id                = SNDCORE_PSP,
    .Name              = "PSP Sound Interface",
    .Init              = psp_sound_init,
    .DeInit            = psp_sound_deinit,
    .Reset             = psp_sound_reset,
    .ChangeVideoFormat = psp_sound_change_video_format,
    .UpdateAudio       = psp_sound_update_audio,
    .GetAudioSpace     = psp_sound_get_audio_space,
    .MuteAudio         = psp_sound_mute_audio,
    .UnMuteAudio       = psp_sound_unmute_audio,
    .SetVolume         = psp_sound_set_volume,
};

/*************************************************************************/
/****************************** Local data *******************************/
/*************************************************************************/

/* Playback rate in Hz (unchangeable) */
#define PLAYBACK_RATE  44100

/* Playback buffer size in samples (larger values = less chance of skipping
 * but greater lag) */
#define BUFFER_SIZE  256

/* Number of BUFFER_SIZE-sized audio buffers in the playback buffer (see
 * description below) */
#define NUM_BUFFERS  8

/*
 * Playback buffer descriptor, implementing a lockless ring buffer with the
 * following semantics:
 *    WRITER (main program):
 *       (1) Waits for .write_ready[.next_write] to become nonzero.
 *       (2) Writes BUFFER_SIZE samples of data into .buffer[.next_write].
 *       (3) Sets .write_ready[.next_write] to zero.
 *       (4) Increments .next_write to point to the next audio buffer.
 *    READER (playback thread):
 *       (1) Waits for .write_ready[.next_play] to become zero.
 *       (2) Submits .buffer[.next_play] to the OS (which blocks until the
 *           previous buffer finishes playing).
 *       (3) Sets .write_ready[.cur_play] to nonzero.
 *       (4) Sets .cur_play to .next_play.
 *       (5) Increments .next_play to point to the next audio buffer.
 *
 * Note that at least three audio buffers are required in the ring buffer:
 *    - One currently being played by the hardware.
 *    - One queued for playback by the hardware.
 *    - One into which the main program is writing.
 * A minimum of at least four buffers is recommended to allow overflow
 * room, since the sample generation rate will typically not be locked to
 * the hardware playback rate.
 */
typedef struct PSPSoundBufferDesc_ {
    __attribute__((aligned(64))) int16_t buffer[NUM_BUFFERS][BUFFER_SIZE*2];
    /* Keep this on its own cache line for uncached access by both SC and ME */
    __attribute__((aligned(64)))
    volatile uint8_t write_ready[NUM_BUFFERS];
                      // When nonzero, data can be stored in buffer[next_write]
    /* Start a new cache line here (these are written by the ME) */
    __attribute__((aligned(64)))
    unsigned int next_write;
                      // Index of next buffer to store data into
    unsigned int saved_samples;
                      // Number of samples accumulated in next_write buffer
    /* Start another new cache line here (these are written by the SC) */
    __attribute__((aligned(64)))
    int started;      // Nonzero if channel is playing
    int channel;      // Channel number allocated for this buffer
    unsigned int cur_play;
                      // Index of buffer currently being played by the hardware
    unsigned int next_play;
                      // Index of next buffer to submit for playback
    /* Internal use: */
    SceUID thread;    // Playback thread handle
    int stop;         // Flag to tell thread to terminate
} PSPSoundBufferDesc;
static PSPSoundBufferDesc stereo_buffer;

/* Mute flag (used by Mute and UnMute methods) */
static int muted = 1;

#ifdef DUMP_AUDIO
/* Audio output file */
static int dump_fd;
#endif

/*----------------------------------*/

/* Uncached pointer to sound RAM (to save generating it on every access) */
static uint8_t *SoundRam_uncached;

/*-----------------------------------------------------------------------*/

/* Local function declarations */

static FASTCALL u8 psp_SoundRamReadByte(u32 address);
static FASTCALL u16 psp_SoundRamReadWord(u32 address);
static FASTCALL u32 psp_SoundRamReadLong(u32 address);
static FASTCALL void psp_SoundRamWriteByte(u32 address, u8 data);
static FASTCALL void psp_SoundRamWriteWord(u32 address, u16 data);
static FASTCALL void psp_SoundRamWriteLong(u32 address, u32 data);

static int start_channel(PSPSoundBufferDesc *buffer_desc);
void stop_channel(PSPSoundBufferDesc *buffer_desc);
static int playback_thread(SceSize args, void *argp);

/*************************************************************************/
/************************** Interface functions **************************/
/*************************************************************************/

/**
 * psp_sound_init:  Initialize the sound interface.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     Zero on success, negative on error
 */
static int psp_sound_init(void)
{
    if (stereo_buffer.started) {
        /* Already initialized! */
        return 0;
    }

#ifdef DUMP_AUDIO
    dump_fd = sceIoOpen("audio.pcm",
                        PSP_O_WRONLY | PSP_O_CREAT | PSP_O_TRUNC, 0600);
    if (dump_fd < 0) {
        DMSG("open(audio.pcm): %s", psp_strerror(dump_fd));
        dump_fd = 0;
    }
#endif

    if (!start_channel(&stereo_buffer)) {
        DMSG("Failed to start playback");
        return -1;
    }

    /* If the Media Engine is in use, reassign the sound RAM access
     * functions so we read/write through the cache as appropriate. */
    if (me_available && config_get_use_me()) {
        SoundRam_uncached = (uint8_t *)((uintptr_t)SoundRam | 0x40000000);
        unsigned int i;
        for (i = 0x5A0; i < 0x5B0; i++) {
            ReadByteList [i] = psp_SoundRamReadByte;
            ReadWordList [i] = psp_SoundRamReadWord;
            ReadLongList [i] = psp_SoundRamReadLong;
            WriteByteList[i] = psp_SoundRamWriteByte;
            WriteWordList[i] = psp_SoundRamWriteWord;
            WriteLongList[i] = psp_SoundRamWriteLong;
        }
    }

    return 0;
}

/*-----------------------------------------------------------------------*/

/**
 * psp_sound_deinit:  Shut down the sound interface.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     None
 */
static void psp_sound_deinit(void)
{
    stop_channel(&stereo_buffer);

    unsigned int i;
    for (i = 0x5A0; i < 0x5B0; i++) {
        ReadByteList [i] = SoundRamReadByte;
        ReadWordList [i] = SoundRamReadWord;
        ReadLongList [i] = SoundRamReadLong;
        WriteByteList[i] = SoundRamWriteByte;
        WriteWordList[i] = SoundRamWriteWord;
        WriteLongList[i] = SoundRamWriteLong;
    }
}

/*-----------------------------------------------------------------------*/

/**
 * psp_sound_reset:  Reset the sound interface.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     Zero on success, negative on error
 */
static int psp_sound_reset(void)
{
    /* Nothing to do */
    return 0;
}

/*-----------------------------------------------------------------------*/

/**
 * psp_sound_change_video_format:  Handle a change in the video refresh
 * frequency.
 *
 * [Parameters]
 *     vertfreq: New refresh frequency (Hz)
 * [Return value]
 *     Zero on success, negative on error
 */
static int psp_sound_change_video_format(int vertfreq)
{
    /* Nothing to do */
    return 0;
}

/*************************************************************************/

/**
 * psp_sound_update_audio:  Output audio data.
 *
 * [Parameters]
 *      leftchanbuffer: Left channel sample array, as _signed_ 16-bit samples
 *     rightchanbuffer: Right channel sample array, as _signed_ 16-bit samples
 *         num_samples: Number of samples in sample arrays
 * [Return value]
 *     None
 */
static void psp_sound_update_audio(u32 *leftchanbuffer, u32 *rightchanbuffer,
                                   u32 num_samples)
{
    const unsigned int next_write = stereo_buffer.next_write;

    if (!leftchanbuffer || !rightchanbuffer
     || !UNCACHED(stereo_buffer.write_ready[next_write])
     || num_samples == 0
     || num_samples > BUFFER_SIZE - stereo_buffer.saved_samples
    ) {
        if (!meUtilityIsME()) {  // Can't write to stderr on the ME
            DMSG("Invalid parameters: %p %p %u (status: wr=%d ss=%d)",
                 leftchanbuffer, rightchanbuffer, (unsigned int)num_samples,
                 UNCACHED(stereo_buffer.write_ready[next_write]),
                 stereo_buffer.saved_samples);
        }
        return;
    }

    const int32_t *in_l = (int32_t *)leftchanbuffer;
    const int32_t *in_r = (int32_t *)rightchanbuffer;
    int16_t *out =
        &stereo_buffer.buffer[next_write][stereo_buffer.saved_samples * 2];

    uint32_t i;
    for (i = 0; i < num_samples; i++) {
        const int32_t lval = *in_l++;
        const int32_t rval = *in_r++;
        *out++ = bound(lval, -0x8000, 0x7FFF);
        *out++ = bound(rval, -0x8000, 0x7FFF);
    }

    stereo_buffer.saved_samples += num_samples;
    if (stereo_buffer.saved_samples >= BUFFER_SIZE) {
        if (meUtilityIsME()) {
            /* Make sure the playback thread sees all the audio data */
            meUtilityDcacheWritebackInvalidateAll();
        }
        UNCACHED(stereo_buffer.write_ready[next_write]) = 0;
        stereo_buffer.saved_samples = 0;
        stereo_buffer.next_write = (next_write + 1) % NUM_BUFFERS;
    }
}

/*-----------------------------------------------------------------------*/

/**
 * psp_sound_get_audio_space:  Return the number of samples immediately
 * available for outputting audio data.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     Number of samples available
 */
static u32 psp_sound_get_audio_space(void)
{
    if (UNCACHED(stereo_buffer.write_ready[stereo_buffer.next_write])) {
        return BUFFER_SIZE - stereo_buffer.saved_samples;
    } else {
        return 0;
    }
}

/*************************************************************************/

/**
 * psp_sound_mute_audio:  Disable audio output.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     None
 */
static void psp_sound_mute_audio(void)
{
    muted = 1;
}

/*-----------------------------------------------------------------------*/

/**
 * psp_sound_mute_audio:  Enable audio output.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     None
 */
static void psp_sound_unmute_audio(void)
{
    muted = 0;
}

/*-----------------------------------------------------------------------*/

/**
 * psp_sound_set_volume:  Set the audio output volume.
 *
 * [Parameters]
 *     volume: New volume (0-100, 100 = full volume)
 * [Return value]
 *     None
 */
static void psp_sound_set_volume(int volume)
{
    const int pspvol = (PSP_AUDIO_VOLUME_MAX * volume + 50) / 100;
    if (stereo_buffer.started) {
        sceAudioChangeChannelVolume(stereo_buffer.channel, pspvol, pspvol);
    }
}

/*************************************************************************/
/********************* PSP-local interface functions *********************/
/*************************************************************************/

/**
 * psp_sound_pause:  Stop audio output.  Called when the system is being
 * suspended.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     None
 */
void psp_sound_pause(void)
{
    if (stereo_buffer.started) {
        sceKernelSuspendThread(stereo_buffer.thread);
    }
}

/*-----------------------------------------------------------------------*/

/**
 * psp_sound_unpause:  Resume audio output.  Called when the system is
 * resuming from a suspend.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     None
 */
void psp_sound_unpause(void)
{
    if (stereo_buffer.started) {
        sceKernelResumeThread(stereo_buffer.thread);
    }
}

/*-----------------------------------------------------------------------*/

/**
 * psp_sound_exit:  Terminate all playback in preparation for exiting.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     None
 */
void psp_sound_exit(void)
{
    if (stereo_buffer.started) {
        stop_channel(&stereo_buffer);
    }
}

/*************************************************************************/
/* Sound RAM access functions (for use when the 68k is running on the ME) */
/*************************************************************************/

/**
 * psp_SoundRamRead{Byte,Word,Long}:  Sound RAM read access functions for
 * use when the sound CPU is being emulated on the Media Engine.  These
 * functions access sound RAM using uncached pointers.
 *
 * 2Mbit mode (MEM4MB == 0) is not supported by these functions.
 *
 * [Parameters]
 *     address: Address to read from
 * [Return value]
 *     Data loaded from given address
 */
static FASTCALL u8 psp_SoundRamReadByte(u32 address)
{
    address &= 0x7FFFF;
    return T2ReadByte(SoundRam_uncached, address);
}

static FASTCALL u16 psp_SoundRamReadWord(u32 address)
{
    address &= 0x7FFFF;
    return T2ReadWord(SoundRam_uncached, address);
}

static FASTCALL u32 psp_SoundRamReadLong(u32 address)
{
    address &= 0x7FFFF;
    return T2ReadLong(SoundRam_uncached, address);
}

/*-----------------------------------------------------------------------*/

/**
 * psp_SoundRamWrite{Byte,Word,Long}:  Sound RAM write access functions for
 * use when the sound CPU is being emulated on the Media Engine.  These
 * functions do _not_ access sound RAM using uncached pointers; data is
 * assumed to be flushed by the SCSP at periodic intervals.
 *
 * 2Mbit mode (MEM4MB == 0) is not supported by these functions.
 *
 * [Parameters]
 *     address: Address to write to
 *        data: Data to store
 * [Return value]
 *     None
 */
static FASTCALL void psp_SoundRamWriteByte(u32 address, u8 data)
{
    address &= 0x7FFFF;
    if (address < config_get_me_uncached_boundary()) {
        T2WriteByte(SoundRam_uncached, address, data);
    } else {
        T2WriteByte(SoundRam, address, data);
    }
    M68KWriteNotify(address, 1);
}

static FASTCALL void psp_SoundRamWriteWord(u32 address, u16 data)
{
    address &= 0x7FFFF;
    if (address < config_get_me_uncached_boundary()) {
        T2WriteWord(SoundRam_uncached, address, data);
    } else {
        T2WriteWord(SoundRam, address, data);
    }
    M68KWriteNotify(address, 2);
}

static FASTCALL void psp_SoundRamWriteLong(u32 address, u32 data)
{
    address &= 0x7FFFF;
    if (address < config_get_me_uncached_boundary()) {
        T2WriteLong(SoundRam_uncached, address, data);
    } else {
        T2WriteLong(SoundRam, address, data);
    }
    M68KWriteNotify(address, 4);
}

/*************************************************************************/
/****************** Low-level audio channel management *******************/
/*************************************************************************/

/**
 * start_channel:  Allocate a new channel and starts playback.
 *
 * [Parameters]
 *     buffer_desc: Playback buffer descriptor
 * [Return value]
 *     Nonzero on success, zero on error
 */
static int start_channel(PSPSoundBufferDesc *buffer_desc)
{
    if (!buffer_desc) {
        DMSG("buffer_desc == NULL");
        return 0;
    }
    if (buffer_desc->started) {
        DMSG("Buffer is already started!");
        return 0;
    }

    /* Allocate a hardware channel */
    buffer_desc->channel = sceAudioChReserve(
        PSP_AUDIO_NEXT_CHANNEL, BUFFER_SIZE, PSP_AUDIO_FORMAT_STEREO
    );
    if (buffer_desc->channel < 0) {
        DMSG("Failed to allocate channel: %s",
             psp_strerror(buffer_desc->channel));
        return 0;
    }

    /* Initialize the ring buffer */
    buffer_desc->cur_play = NUM_BUFFERS - 1;
    buffer_desc->next_play = 0;
    buffer_desc->next_write = 0;
    int i;
    for (i = 0; i < NUM_BUFFERS; i++) {
        buffer_desc->write_ready[i] = 1;
    }
    buffer_desc->stop = 0;
    /* Also write everything out of the cache so it's ready for the ME */
    sceKernelDcacheWritebackAll();

    /* Start the playback thread */
    char thname[100];
    snprintf(thname, sizeof(thname), "YabauseSoundCh%d", buffer_desc->channel);
    SceUID handle = sys_start_thread(thname, playback_thread,
                                     THREADPRI_SOUND, 0x1000,
                                     sizeof(buffer_desc), &buffer_desc);
    if (handle < 0) {
        DMSG("Failed to create thread: %s", psp_strerror(handle));
        sceAudioChRelease(buffer_desc->channel);
        return 0;
    }
    buffer_desc->thread = handle;

    /* Success */
    buffer_desc->started = 1;
    return 1;
}

/*-----------------------------------------------------------------------*/

/**
 * stop_channel:  Stop playback from the given playback buffer.
 *
 * [Parameters]
 *     buffer_desc: Playback buffer descriptor
 * [Return value]
 *     None
 */
void stop_channel(PSPSoundBufferDesc *buffer_desc)
{
    if (!buffer_desc) {
        DMSG("buffer_desc == NULL");
        return;
    }
    if (!buffer_desc->started) {
        DMSG("Buffer has not been started!");
        return;
    }

    /* Signal the thread to stop, then wait for it (if we try to stop the
     * thread in the middle of an audio write, we won't be able to free
     * the hardware channel) */
    buffer_desc->stop = 1;
    int tries;
    for (tries = (1000 * (2*BUFFER_SIZE)/PLAYBACK_RATE); tries > 0; tries--) {
        if (sys_delete_thread_if_stopped(buffer_desc->thread, NULL)) {
            break;
        }
        sceKernelDelayThread(1000);  // Wait for 1ms before trying again
    }

    if (!tries) {
        /* The thread didn't stop on its own, so terminate it with
         * extreme prejudice */
        sceKernelTerminateDeleteThread(buffer_desc->thread);
        sceAudioChRelease(buffer_desc->channel);
        memset(buffer_desc, 0, sizeof(*buffer_desc));
    }
}

/*************************************************************************/

/**
 * playback_thread:  Sound playback thread.  Continually sends the ring
 * buffer data to the OS until signaled to stop.
 *
 * [Parameters]
 *     args: Thread argument size
 *     argp: Thread argument pointer
 * [Return value]
 *     Always zero
 */
static int playback_thread(SceSize args, void *argp)
{
    PSPSoundBufferDesc * const buffer_desc = *(PSPSoundBufferDesc **)argp;

    /* Temporary buffer for dummy audio data when the emulator falls behind
     * real time (filled with the last sample sent to avoid clicks).  This
     * thread is only launched once, so "static" is safe. */
    static uint32_t dummy_buffer[BUFFER_SIZE];  // 1 stereo sample = 32 bits
    static uint32_t last_sample;                // Last stereo sample played

    while (!buffer_desc->stop) {
        const unsigned int next_play = buffer_desc->next_play;
//static int x;int now=sceKernelGetSystemTimeLow();if(now-x>100000){printf("--- audio stat: %u %u %u %u cp=%u np=%u nw=%u\n",UNCACHED(buffer_desc->write_ready[0]),UNCACHED(buffer_desc->write_ready[1]),UNCACHED(buffer_desc->write_ready[2]),UNCACHED(buffer_desc->write_ready[3]),buffer_desc->cur_play,next_play,UNCACHED(buffer_desc->next_write));x=now;}
        if (!UNCACHED(buffer_desc->write_ready[next_play])) {  // i.e., ready for playback
            const void *buffer = buffer_desc->buffer[next_play];
            last_sample = ((const uint32_t *)buffer)[BUFFER_SIZE - 1];
            sceAudioOutputBlocking(buffer_desc->channel, muted ? 0 : 0x8000,
                                   buffer);
#ifdef DUMP_AUDIO
            sceIoWrite(dump_fd, buffer, BUFFER_SIZE*4);
#endif
            UNCACHED(buffer_desc->write_ready[buffer_desc->cur_play]) = 1;
            buffer_desc->cur_play = next_play;
            buffer_desc->next_play = (next_play + 1) % NUM_BUFFERS;
        } else {
            const uint32_t sample = last_sample;  // Help out optimizer
            uint32_t *ptr32 = dummy_buffer;
            unsigned int i;
            for (i = 0; i < BUFFER_SIZE; i += 8) {
                ptr32[i+0] = sample;
                ptr32[i+1] = sample;
                ptr32[i+2] = sample;
                ptr32[i+3] = sample;
                ptr32[i+4] = sample;
                ptr32[i+5] = sample;
                ptr32[i+6] = sample;
                ptr32[i+7] = sample;
            }
            sceAudioOutputBlocking(buffer_desc->channel, muted ? 0 : 0x8000,
                                   dummy_buffer);
        }
    }

    sceAudioChRelease(buffer_desc->channel);
    memset(buffer_desc, 0, sizeof(*buffer_desc));
    return 0;
}

/*************************************************************************/

/*
 * Local variables:
 *   c-file-style: "stroustrup"
 *   c-file-offsets: ((case-label . *) (statement-case-intro . *))
 *   indent-tabs-mode: nil
 * End:
 *
 * vim: expandtab shiftwidth=4:
 */
