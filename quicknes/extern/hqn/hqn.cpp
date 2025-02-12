#include <cmath>
#include <SDL_timer.h>
#include "hqn.h"

namespace hqn
{

// Function to initalize the video palette
int32_t *_initF_VideoPalette()
{
    static int32_t VideoPalette[512];
    const emulator_t::rgb_t *palette = emulator_t::nes_colors;
    for (int i = 0; i < 512; i++)
    {
        VideoPalette[i] = palette[i].red << 16 | palette[i].green << 8
            | palette[i].blue | 0xff000000;
    }
    return VideoPalette;
}

// Initialize the video palette
const int32_t *HQNState::NES_VIDEO_PALETTE = _initF_VideoPalette();

// Constructor
HQNState::HQNState()
{
    m_emu = new emulator_t();
    joypad[0] = 0x00;
    joypad[1] = 0x00;

	m_listener = nullptr;
    m_romData = nullptr;
    m_romSize = 0;

	m_emu->set_sample_rate(44100);

    m_prevFrame = 0;
    m_msPerFrame = 0;
    m_initialFrame = SDL_GetTicks();
}

// Destructor
HQNState::~HQNState()
{
}

error_t HQNState::setSampleRate(int rate)
{
	const char *ret = m_emu->set_sample_rate(rate);
	if (!ret)
		m_emu->set_equalizer(emulator_t::nes_eq);
	return ret;
}


error_t HQNState::saveState(void *dest, size_t size, size_t *size_out)
{
    return 0;
}

error_t HQNState::saveStateSize(size_t *size) const
{
    return 0;
}

error_t HQNState::loadState(const char *data, size_t size)
{
    return 0;
}

// Advance the emulator
error_t HQNState::advanceFrame(bool sleep)
{
    Uint32 ticks;
    ticks = SDL_GetTicks();
    Uint32 wantTicks = m_prevFrame + m_msPerFrame;
    if (wantTicks > ticks)
    {
        SDL_Delay(wantTicks - ticks);
    }
    // m_frameTime = wantTicks - m_prevFrame;
    // error_t result = m_emu->emulate_frame(joypad[0], joypad[1]);
    if (m_listener)
        m_listener->onAdvanceFrame(this);
    ticks = SDL_GetTicks();
    m_frameTime = ticks - m_prevFrame;
    m_prevFrame = ticks;
    return 0;
}

void HQNState::setFramerate(int fps)
{
    if (fps == 0)
    {
        m_msPerFrame = 0;
    }
    else
    {
        m_msPerFrame = (long)(1000.0 / fps);
    }   
}

int HQNState::getFramerate() const
{
    if (m_msPerFrame)
    {
        return (int)(1000.0 / m_msPerFrame);
    }
    else
    {
        return 0;
    }
}

double HQNState::getFPS() const
{
    double ft = m_frameTime ? m_frameTime : 1;
    double fps = 1000.0 / ft;
    // round to 2 decimal places
    fps = std::floor(fps * 100) / 100;
    return fps;
}


} // end namespace hqn
