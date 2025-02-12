#ifndef __HQN_SURFACE_H__
#define __HQN_SURFACE_H__

#include <SDL_surface.h>
#include "hqn.h"
#include <cstdint>

namespace hqn
{

extern const float pi;

float fastSin(float x);
float fastCos(float x);

struct Color
{
	union
	{
		struct { uint8_t r, g, b, a; };
		uint32_t bits;
	};
};

enum BlendMode
{
    HQN_BLENDMODE_NONE,
    HQN_BLENDMODE_BLEND,
    HQN_BLENDMODE_ADD,
    HQN_BLENDMODE_MOD,
};

/**
 * A surface which provides drawing operations.
 * The format is always RGBA.
 */
class Surface
{
public:
    #define HQN_DEFAULT_PTSIZE 12

    typedef Color (*BlendFunction)(Color src, Color dst);

    Surface(size_t width, size_t height);
    ~Surface();

    inline int getWidth() const
    { return m_width; }
    inline int getHeight() const
    { return m_height; }

    void drawRect(int x, int y, size_t w, size_t h, Color color);
    void fillRect(int x, int y, size_t w, size_t h, Color fg, Color bg);
    
    void drawCircle(int x, int y, size_t radius, Color color);
    void fillCircle(int x, int y, size_t radius, Color fg, Color bg);
    
    void drawOval(int x, int y, size_t w, size_t h, Color color);
    void fillOval(int x, int y, size_t w, size_t h, Color fg, Color bg);

    /**
     * Draw a line. If the line goes outside the edges of the screen it will
     * not be drawn.
     */
    void fastLine(int x1, int y1, int x2, int y2, Color color);

    /** 
     * Slower than fastLine but will still draw lines which are partially
     * offscreen. Note that safeLine is likely significantly slower than
     * fastLine because it checks if each pixel is in bounds.
     */
    void safeLine(int x1, int y1, int x2, int y2, Color color);


    /**
     * Set all pixels in the surface to the given color.
     */
    void clear(Color color);

    void setPixel(int x, int y, Color color);
    int32_t getPixel(int x, int y);

    /**
     * Get a reference to the pixels.
     */
    inline Color *getPixels() const
    { return m_pixels; }

    inline size_t getDataSize() const
    { return m_width * m_height * 4; }

    void setBlendMode(BlendMode mode);
    BlendMode getBlendMode() const;

private:

    //////////////////
    // Private Data //
    //////////////////

    // RGBA formatted pixels
    Color *m_pixels;
	SDL_Surface *m_surface;
    int m_width;
    int m_height;
    BlendMode m_blend;
    BlendFunction m_blendFunc;
    
    // Set the pixel directly. Performs blending.
    // Inlined because it's small and more efficient to inline it.
    inline void rawset(int x, int y, Color src)
    {
        // Yay for no branching
        Color *destPtr = &m_pixels[x + y * m_width];
        *destPtr = m_blendFunc(src, *destPtr);
    }

    void line(int x1, int y1, int x2, int y2, Color color);

    // Blend functions
    static Color blendNone(Color, Color);
    static Color blendBlend(Color, Color);
    static Color blendAdd(Color, Color);
    static Color blendMod(Color, Color);
};

}

#endif
