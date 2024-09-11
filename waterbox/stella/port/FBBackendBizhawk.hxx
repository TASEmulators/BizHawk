//============================================================================
//
//   SSSS    tt          lll  lll
//  SS  SS   tt           ll   ll
//  SS     tttttt  eeee   ll   ll   aaaa
//   SSSS    tt   ee  ee  ll   ll      aa
//      SS   tt   eeeeee  ll   ll   aaaaa  --  "An Atari 2600 VCS Emulator"
//  SS  SS   tt   ee      ll   ll  aa  aa
//   SSSS     ttt  eeeee llll llll  aaaaa
//
// Copyright (c) 1995-2024 by Bradford W. Mott, Stephen Anthony
// and the Stella Team
//
// See the file "License.txt" for information on usage and redistribution of
// this file, and for a DISCLAIMER OF ALL WARRANTIES.
//============================================================================

#ifndef FB_BACKEND_BIZHAWK_HXX
#define FB_BACKEND_BIZHAWK_HXX

class OSystem;
class FBSurfaceBIZHAWK;

#include "bspf.hxx"
#include "FBBackend.hxx"

/**
  This class implements a standard BIZHAWK 2D, hardware accelerated framebuffer
  backend.  Behind the scenes, it may be using Direct3D, OpenGL(ES), etc.

  @author  Stephen Anthony
*/
class FBBackendBIZHAWK : public FBBackend
{
  public:
    /**
      Creates a new BIZHAWK framebuffer
    */
    explicit FBBackendBIZHAWK(OSystem& osystem);
    ~FBBackendBIZHAWK() override;

  public:

    /**
      Is the renderer initialized?
     */
    bool isInitialized() const { return true; }

    /**
      Does the renderer support render targets?
     */
    bool hasRenderTargetSupport() const { return true;; }

    /**
      Transform from window to renderer coordinates, x direction
     */
    int scaleX(int x) const override { return 1; }

    /**
      Transform from window to renderer coordinates, y direction
     */
    int scaleY(int y) const override { return 1; }

  protected:
    /**
      Updates window title.

      @param title  The title of the application / window
    */
    void setTitle(string_view title) override;

    /**
      Shows or hides the cursor based on the given boolean value.
    */
    void showCursor(bool show) override;

    /**
      Answers if the display is currently in fullscreen mode.
    */
    bool fullScreen() const override;

    /**
      This method is called to retrieve the R/G/B data from the given pixel.

      @param pixel  The pixel containing R/G/B data
      @param r      The red component of the color
      @param g      The green component of the color
      @param b      The blue component of the color
    */
    FORCE_INLINE void getRGB(uInt32 pixel, uInt8* r, uInt8* g, uInt8* b) const override
      { }

    /**
      This method is called to retrieve the R/G/B/A data from the given pixel.

      @param pixel  The pixel containing R/G/B data
      @param r      The red component of the color
      @param g      The green component of the color
      @param b      The blue component of the color
      @param a      The alpha component of the color.
    */
    FORCE_INLINE void getRGBA(uInt32 pixel, uInt8* r, uInt8* g, uInt8* b, uInt8* a) const override
      { }

    /**
      This method is called to map a given R/G/B triple to the screen palette.

      @param r  The red component of the color.
      @param g  The green component of the color.
      @param b  The blue component of the color.
    */
    inline uInt32 mapRGB(uInt8 r, uInt8 g, uInt8 b) const override
      { return 0; }

    /**
      This method is called to map a given R/G/B/A triple to the screen palette.

      @param r  The red component of the color.
      @param g  The green component of the color.
      @param b  The blue component of the color.
      @param a  The alpha component of the color.
    */
    inline uInt32 mapRGBA(uInt8 r, uInt8 g, uInt8 b, uInt8 a) const override
      { return 0; }

    /**
      This method is called to get a copy of the specified ARGB data from the
      viewable FrameBuffer area.  Note that this isn't the same as any
      internal surfaces that may be in use; it should return the actual data
      as it is currently seen onscreen.

      @param buffer  A copy of the pixel data in ARGB8888 format
      @param pitch   The pitch (in bytes) for the pixel data
      @param rect    The bounding rectangle for the buffer
    */
    void readPixels(uInt8* buffer, size_t pitch,
                    const Common::Rect& rect) const override;

    /**
      This method is called to query if the current window is not centered
      or fullscreen.

      @return  True, if the current window is positioned
    */
    bool isCurrentWindowPositioned() const override;

    /**
      This method is called to query the video hardware for position of
      the current window

      @return  The position of the currently displayed window
    */
    Common::Point getCurrentWindowPos() const override;

    /**
      This method is called to query the video hardware for the index
      of the display the current window is displayed on

      @return  the current display index or a negative value if no
               window is displayed
    */
    Int32 getCurrentDisplayIndex() const override;

    /**
      Clear the frame buffer.
    */
    void clear() override;

    /**
      This method is called to query and initialize the video hardware
      for desktop and fullscreen resolution information.  Since several
      monitors may be attached, we need the resolution for all of them.

      @param fullscreenRes  Maximum resolution supported in fullscreen mode
      @param windowedRes    Maximum resolution supported in windowed mode
      @param renderers      List of renderer names (internal name -> end-user name)
    */
    void queryHardware(vector<Common::Size>& fullscreenRes,
                       vector<Common::Size>& windowedRes,
                       VariantList& renderers) override;

    /**
      This method is called to change to the given video mode.

      @param mode   The video mode to use
      @param winIdx The display/monitor that the window last opened on
      @param winPos The position that the window last opened at

      @return  False on any errors, else true
    */
    bool setVideoMode(const VideoModeHandler::Mode& mode,
                      int winIdx, const Common::Point& winPos) override;

    /**
      This method is called to create a surface with the given attributes.

      @param w      The requested width of the new surface.
      @param h      The requested height of the new surface.
      @param inter  Interpolation mode
      @param data   If non-null, use the given data values as a static surface
    */
    unique_ptr<FBSurface>
        createSurface(
          uInt32 w,
          uInt32 h,
          ScalingInterpolation inter,
          const uInt32* data
        ) const override;

    /**
      Grabs or ungrabs the mouse based on the given boolean value.
    */
    void grabMouse(bool grab) override;

    /**
      This method is called to provide information about the backend.
    */
    string about() const override;

    /**
      Create a new renderer if required.

      @return  False on any errors, else true
    */
    bool createRenderer(const VideoModeHandler::Mode& mode);

    /**
      This method must be called after all drawing is done, and indicates
      that the buffers should be pushed to the physical screen.
    */
    void renderToScreen() override;

    /**
      Retrieve the current display's refresh rate, or 0 if no window.
    */
    int refreshRate() const override;

    /**
      After the renderer has been created, detect the features it supports.
     */
    void detectFeatures();

    /**
      Detect render target support.
     */
    bool detectRenderTargetSupport();

    /**
      Determine window and renderer dimensions.
     */
    void determineDimensions();

    /**
      Set the icon for the main SDL window.
    */
    void setWindowIcon();

  private:
    OSystem& myOSystem;

    // Center setting of current window
    bool myCenter{false};

  private:
    // Following constructors and assignment operators not supported
    FBBackendBIZHAWK() = delete;
    FBBackendBIZHAWK(const FBBackendBIZHAWK&) = delete;
    FBBackendBIZHAWK(FBBackendBIZHAWK&&) = delete;
    FBBackendBIZHAWK& operator=(const FBBackendBIZHAWK&) = delete;
    FBBackendBIZHAWK& operator=(FBBackendBIZHAWK&&) = delete;
};

#endif
