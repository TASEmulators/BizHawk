#include "hqn_gui_controller.h"
#include <algorithm>
#include <cstring>
#include <SDL.h>

#define DEFAULT_WIDTH 256
#define DEFAULT_HEIGHT 240

namespace hqn
{

const char *DEFAULT_WINDOW_TITLE = "HeadlessQuickNES";


const SDL_Rect NES_BLIT_RECT = { 0, 0, DEFAULT_WIDTH, DEFAULT_HEIGHT };

// Determine the letterboxing required to display something on screen.
// The original code is basically magic.
// aspectW/H and windowW/H are the aspect ratio and window size, they are
// inputs. The view* parameters are the outputs and correspond to the
// calculated area in the window where the view should be.
void determineLetterbox(double aspectW, double aspectH, double windowW,
    double windowH, int &viewX, int &viewY, int &viewW, int &viewH)
{
    double scale = std::min(windowW / aspectW, windowH / aspectH);
    viewW = (int)(aspectW * scale);
    viewH = (int)(aspectH * scale);
    viewX = (int)(windowW - viewW) / 2;
    viewY = (int)(windowH - viewH) / 2;
}

/*
-- Returns: scale, scissorX, scissorY, scissorW, scissorH, offsetX, offsetY
function xl.calculateViewport(sizes, winW, winH, scaleInterval, screenFlex )
    local gameW,gameH = sizes.w, sizes.h
    local screenW,screenH = winW + screenFlex, winH + screenFlex
    local scale = math.min(screenW / gameW, screenH / gameH)
    scale = math.floor(scale * scaleInterval) / scaleInterval
    local scissorW, scissorH = gameW * scale, gameH * scale
    local scissorX, scissorY = (winW - scissorW) / 2, (winH - scissorH) / 2
    local offsetX, offsetY = scissorX / scale, scissorY / scale
    return scale, scissorX, scissorY, scissorW, scissorH, offsetX, offsetY
end
*/

GUIController::GUIController(HQNState &state)
:m_state(state)
{
    m_tex = nullptr;
    m_texOverlay = nullptr;
    m_renderer = nullptr;
    m_window = nullptr;
    m_overlay = nullptr;
    m_closeOp = CLOSE_QUIT;
    m_isFullscreen = false;
}

GUIController::~GUIController()
{
    if (m_tex)
        SDL_DestroyTexture(m_tex);
    if (m_texOverlay)
        SDL_DestroyTexture(m_texOverlay);
    if (m_renderer)
        SDL_DestroyRenderer(m_renderer);
    if (m_window)
        SDL_DestroyWindow(m_window);
    m_state.setListener(nullptr);
}

GUIController *GUIController::create(HQNState &state, SDL_Window* window)
{
    GUIController *self = new GUIController(state);
    if (!self->init(window))
    {
        delete self;
        return nullptr;
    }
    else
    {
        return self;
    }
}

bool GUIController::init(SDL_Window* window)
{
    m_window = window;
    if (!(m_renderer = SDL_CreateRenderer(m_window, -1, SDL_RENDERER_ACCELERATED)))
        return false;
    if (!(m_tex = SDL_CreateTexture(m_renderer, SDL_PIXELFORMAT_ARGB8888,
        SDL_TEXTUREACCESS_STREAMING, 256, 256)))
        return false;
    // Set the clear color now rather than later
    SDL_SetRenderDrawColor(m_renderer, 0, 0, 0, 255);
    // Default the scale to 1
    if (!setScale(1))
        return false;
    return true;
}

bool GUIController::setScale(int scale)
{
    if (scale < 1 || scale > 5)
        return false;
    if (m_isFullscreen)
        setFullscreen(false, false); // reset to windowed and don't bother changing the overlay
    int winW = DEFAULT_WIDTH * scale;
    int winH = DEFAULT_HEIGHT * scale;

    // Change the window size
    SDL_SetWindowSize(m_window, winW, winH);
    
    if (!onResize(winW, winH, true))
        return false;

    // update the overlay destination
    m_overlayDest = { 0, 0, winW, winH };
    m_nesDest = { 0, 0, winW, winH };

    // Update internal scale variable
    m_scale = scale;
    return true;
}

int GUIController::getScale() const
{ return m_scale; }

void GUIController::setFullscreen(bool full, bool adjustOverlay)
{
    m_isFullscreen = full;
    SDL_SetWindowFullscreen(m_window, full ? SDL_WINDOW_FULLSCREEN_DESKTOP : 0);
    int w, h;
    SDL_GetWindowSize(m_window, &w, &h);
    onResize((size_t)w, (size_t)h, adjustOverlay);
}

bool GUIController::isFullscreen() const
{
    return m_isFullscreen;
}

bool GUIController::onResize(size_t w, size_t h, bool adjustOverlay)
{
    // first calculate letterboxing
    int viewX, viewY, viewW, viewH;
    determineLetterbox(DEFAULT_WIDTH, DEFAULT_HEIGHT, w, h,
        viewX, viewY, viewW, viewH);
    // make sure images are put in the right places
    m_overlayDest = { viewX, viewY, viewW, viewH };
    m_nesDest = { viewX, viewY, viewW, viewH };
    if (adjustOverlay && !resizeOverlay(viewW, viewH))
        return false;
    return true;
}

bool GUIController::resizeOverlay(size_t w, size_t h)
{
    
    
    // destroy the overlay and corresponding texture
    if (m_overlay)
    {
        // first check if the overlay is already the correct size
        if (m_overlay->getWidth() == (int)w 
            && m_overlay->getHeight() == (int)h)
            return true;
        else
            delete m_overlay;
    }
    if (m_texOverlay)
        SDL_DestroyTexture(m_texOverlay);
    // Now re-create them
    m_overlay = new Surface(w, h);
    if (!(m_texOverlay = SDL_CreateTexture(m_renderer, SDL_PIXELFORMAT_ARGB8888,
        SDL_TEXTUREACCESS_STREAMING, w, h)))
        return false;
    SDL_SetTextureBlendMode(m_texOverlay, SDL_BLENDMODE_BLEND);
    return true;
}

void GUIController::update(bool readNES)
{
    void *nesPixels = nullptr;
    int pitch = 0;

    if (SDL_LockTexture(m_tex, nullptr, &nesPixels, &pitch) < 0) return;

    SDL_UnlockTexture(m_tex);

    // render to screen
    SDL_RenderClear(m_renderer);
    SDL_RenderCopy(m_renderer, m_tex, &NES_BLIT_RECT, &m_nesDest);
    SDL_RenderPresent(m_renderer);
    // Process any outstanding events
    processEvents();
}

void GUIController::update_blit(const int32_t* blit, SDL_Surface* base, SDL_Surface* button_a, SDL_Surface* button_b, SDL_Surface* button_select, SDL_Surface* button_start, SDL_Surface* button_up, SDL_Surface* button_down, SDL_Surface* button_left, SDL_Surface* button_right)
{
    void *nesPixels = nullptr;
    int pitch = 0;

    if (SDL_LockTexture(m_tex, nullptr, &nesPixels, &pitch) < 0) return;

    memcpy(nesPixels, blit, sizeof(int32_t) * BLIT_SIZE);
    SDL_UnlockTexture(m_tex);

    const SDL_Rect OVERLAY_BLIT_RECT_SRC = { 0, 0, 169, 53 };
    const SDL_Rect OVERLAY_BLIT_RECT_DST = { 343, 425, 169, 53 };

    // render to screen
    SDL_RenderClear(m_renderer);
    SDL_RenderCopy(m_renderer, m_tex, &NES_BLIT_RECT, &m_nesDest);

    if (base != NULL)
    {
     auto overlayTex = SDL_CreateTextureFromSurface(m_renderer, base);
     SDL_RenderCopy(m_renderer, overlayTex, &OVERLAY_BLIT_RECT_SRC, &OVERLAY_BLIT_RECT_DST);
     SDL_DestroyTexture(overlayTex);
    }

    if (button_a != NULL)
    {
     auto overlayTex = SDL_CreateTextureFromSurface(m_renderer, button_a);
     SDL_RenderCopy(m_renderer, overlayTex, &OVERLAY_BLIT_RECT_SRC, &OVERLAY_BLIT_RECT_DST);
     SDL_DestroyTexture(overlayTex);
    }

    if (button_b != NULL)
    {
     auto overlayTex = SDL_CreateTextureFromSurface(m_renderer, button_b);
     SDL_RenderCopy(m_renderer, overlayTex, &OVERLAY_BLIT_RECT_SRC, &OVERLAY_BLIT_RECT_DST);
     SDL_DestroyTexture(overlayTex);
    }

    if (button_select != NULL)
    {
     auto overlayTex = SDL_CreateTextureFromSurface(m_renderer, button_select);
     SDL_RenderCopy(m_renderer, overlayTex, &OVERLAY_BLIT_RECT_SRC, &OVERLAY_BLIT_RECT_DST);
     SDL_DestroyTexture(overlayTex);
    }

    if (button_start != NULL)
    {
     auto overlayTex = SDL_CreateTextureFromSurface(m_renderer, button_start);
     SDL_RenderCopy(m_renderer, overlayTex, &OVERLAY_BLIT_RECT_SRC, &OVERLAY_BLIT_RECT_DST);
     SDL_DestroyTexture(overlayTex);
    }

    if (button_up != NULL)
    {
     auto overlayTex = SDL_CreateTextureFromSurface(m_renderer, button_up);
     SDL_RenderCopy(m_renderer, overlayTex, &OVERLAY_BLIT_RECT_SRC, &OVERLAY_BLIT_RECT_DST);
     SDL_DestroyTexture(overlayTex);
    }

    if (button_down != NULL)
    {
     auto overlayTex = SDL_CreateTextureFromSurface(m_renderer, button_down);
     SDL_RenderCopy(m_renderer, overlayTex, &OVERLAY_BLIT_RECT_SRC, &OVERLAY_BLIT_RECT_DST);
     SDL_DestroyTexture(overlayTex);
    }

    if (button_left != NULL)
    {
     auto overlayTex = SDL_CreateTextureFromSurface(m_renderer, button_left);
     SDL_RenderCopy(m_renderer, overlayTex, &OVERLAY_BLIT_RECT_SRC, &OVERLAY_BLIT_RECT_DST);
     SDL_DestroyTexture(overlayTex);
    }

    if (button_right != NULL)
    {
     auto overlayTex = SDL_CreateTextureFromSurface(m_renderer, button_right);
     SDL_RenderCopy(m_renderer, overlayTex, &OVERLAY_BLIT_RECT_SRC, &OVERLAY_BLIT_RECT_DST);
     SDL_DestroyTexture(overlayTex);
    }

    SDL_RenderPresent(m_renderer);

    // Process any outstanding events
    processEvents();
}

void GUIController::onAdvanceFrame(HQNState *state)
{
    update(true);
}

void GUIController::setTitle(const char *title)
{
    SDL_SetWindowTitle(m_window, title);
}

void GUIController::setCloseOperation(CloseOperation closeop)
{
    m_closeOp = closeop;
}

GUIController::CloseOperation GUIController::getCloseOperation() const
{
    return m_closeOp;
}

void GUIController::processEvents()
{
    bool quit = false;
    SDL_Event event;
    while (SDL_PollEvent(&event))
    {
        switch(event.type)
        {
        case SDL_QUIT:
            quit = true;
            break;
        }
    }
    if (quit)
    {
        if (m_closeOp & CLOSE_QUIT)
        {
            exit(0);
        }
        if (m_closeOp & CLOSE_DELETE)
        {
            m_state.setListener(nullptr);
            delete this;
        }
    }
}

void GUIController::onLoadROM(HQNState *state, const char *filename) {} // unimportant
void GUIController::onLoadState(HQNState *state) {} // also unimportant

}
