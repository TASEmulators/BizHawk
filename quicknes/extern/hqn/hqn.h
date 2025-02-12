#ifndef __HQN_H__
#define __HQN_H__

#include <cstdint>
#include <stdio.h>
#include <nesInstance.hpp>

#define BLIT_SIZE 65536

// Creating emulator instance

namespace hqn
{

typedef const char *error_t;

class HQNState;

class HQNListener
{
public:
    HQNListener() = default;
    virtual ~HQNListener() = default;

    virtual void onLoadROM(HQNState *state, const char *filename) = 0;
    virtual void onAdvanceFrame(HQNState *state) = 0;
    virtual void onLoadState(HQNState *state) = 0;
};

/*
State which is maintained by the emulator driver.

This should normally be obtained using hqn_get_state() if you are in a lua
function.
*/
class HQNState
{
public:

    /* A reference to the emulator instance. */
    emulator_t *m_emu;

    static const int32_t *NES_VIDEO_PALETTE;

    /**
     * Constructor.
     */
    HQNState();
    ~HQNState();

    void setEmulatorPointer(void* const emuPtr) { m_emu = (emulator_t*)emuPtr; }

    /*
    The joypad data for the two joypads available to an NES.
    This is directly available because I'm lazy.
    */
    uint32_t joypad[2];

    /* Get the emulator this state uses. */
    inline emulator_t *emu() const
    { return m_emu; }


    /*
    Advance the emulator by one frame. If sleep is true and there is a frame
    limit set advanceFrame() will sleep in order to limit the framerate.
    Returns NULL or error string.
    */
	error_t advanceFrame(bool sleep=true);

    /*
    Save the game state. This can be restored at any time.
    */
    error_t saveState(void *dest, size_t size, size_t *size_out);

    /*
    Get the size (bytes) of a savestate.
    Use this to allocate enough space for the saveState method.
    */
    error_t saveStateSize(size_t *size) const;

    /*
    Load the emulator state from data.
    This should be data produced by saveState().
    */
    error_t loadState(const char *data, size_t size);


	error_t setSampleRate(int rate);

    /**
     * Set a limit to the framerate.
     * 0 means no limit.
     */
    void setFramerate(int fps);

    /**
     * Get the current framerate limit.
     */
    int getFramerate() const;

    /**
     * Get the calculated frames per second.
     */
    double getFPS() const;

    inline HQNListener *getListener() const
    { return m_listener; }

    inline void setListener(HQNListener *l)
    { m_listener = l; }

    /**
     * Get the state of the keyboard. Use this to update the keyboard state.
     */
    inline uint8_t *getKeyboard()
    { return m_keyboard; }

private:
    /* ROM file stored in memory because reasons */
    uint8_t *m_romData;
    size_t m_romSize;
    /* Minimum milliseconds between each frame. */
    uint32_t m_msPerFrame;
    /* time value of previous frame. */
    uint32_t m_prevFrame;
    /* The listener */
    HQNListener *m_listener;
    /* Keyboard state */
    uint8_t m_keyboard[256];
    /* Number of frames we've processed */
    uint32_t m_frames;
    /* How long it took to process the previous frame */
    uint32_t m_frameTime;
    uint32_t m_initialFrame;
};

/*
Print the usage message.
@param filename used to specify the name of the exe file, may be NULL.
*/
void printUsage(const char *filename);

} // end namespace hqn

// Copied from bizinterface.cpp in BizHawk/quicknes
inline void saveBlit(const void *ePtr, int32_t *dest, const int32_t *colors, int cropleft, int croptop, int cropright, int cropbottom)
{
    // what is the point of the 256 color bitmap and the dynamic color allocation to it?
    // why not just render directly to a 512 color bitmap with static palette positions?
//    emulator_t *e = m_emu; // e was a parameter but since this is now part of a class, it's just in here
//    const int srcpitch = e->frame().pitch;
//    const unsigned char *src = e->frame().pixels;
//    const unsigned char *const srcend = src + (e->image_height - cropbottom) * srcpitch;
//
//    const short *lut = e->frame().palette;
//
//    const int rowlen = 256 - cropleft - cropright;
//
//    src += cropleft;
//    src += croptop * srcpitch;
//
//    for (; src < srcend; src += srcpitch)
//    {
//        for (int i = 0; i < rowlen; i++)
//        {
//            *dest++ = colors[lut[src[i]]];
//        }
//    }

 const emulator_t *e = (emulator_t*) ePtr;
 const unsigned char *in_pixels = e->frame().pixels;
 if (in_pixels == NULL) return;
 int32_t *out_pixels = dest;

 for (unsigned h = 0; h < emulator_t::image_height;  h++, in_pixels += e->frame().pitch, out_pixels += emulator_t::image_width)
  for (unsigned w = 0; w < emulator_t::image_width; w++)
  {
     unsigned col = e->frame().palette[in_pixels[w]];
     const emulator_t::rgb_t& rgb = e->nes_colors[col];
     unsigned r = rgb.red;
     unsigned g = rgb.green;
     unsigned b = rgb.blue;
     out_pixels[w] = (r << 16) | (g << 8) | (b << 0);
  }
}

#endif /* __HQN_H__ */
