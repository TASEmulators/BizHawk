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

#ifndef FBSURFACE_BIZHAWK_HXX
#define FBSURFACE_BIZHAWK_HXX

#include "bspf.hxx"
#include "FBSurface.hxx"
#include "FBBackendBizhawk.hxx"

/**
  An FBSurface suitable for the BIZHAWK Render2D API, making use of hardware
  acceleration behind the scenes.

  @author  Stephen Anthony
*/
class FBSurfaceBIZHAWK : public FBSurface
{
  public:
    FBSurfaceBIZHAWK(FBBackendBIZHAWK& backend, uInt32 width, uInt32 height,
                  ScalingInterpolation inter, const uInt32* staticData);
    ~FBSurfaceBIZHAWK() override;

    // Most of the surface drawing primitives are implemented in FBSurface;
    // the ones implemented here use SDL-specific code for extra performance
    //
    void fillRect(uInt32 x, uInt32 y, uInt32 w, uInt32 h, ColorId color) override;

    uInt32 width() const override;
    uInt32 height() const override;

    const Common::Rect& srcRect() const override;
    const Common::Rect& dstRect() const override;
    void setSrcPos(uInt32 x, uInt32 y) override;
    void setSrcSize(uInt32 w, uInt32 h) override;
    void setSrcRect(const Common::Rect& r) override;
    void setDstPos(uInt32 x, uInt32 y) override;
    void setDstSize(uInt32 w, uInt32 h) override;
    void setDstRect(const Common::Rect& r) override;

    void setVisible(bool visible) override;

    void translateCoords(Int32& x, Int32& y) const override;
    bool render() override;
    void invalidate() override;
    void invalidateRect(uInt32 x, uInt32 y, uInt32 w, uInt32 h) override;

    void reload() override;
    void resize(uInt32 width, uInt32 height) override;

    void setScalingInterpolation(ScalingInterpolation) override;

  protected:
    void applyAttributes() override;

    void createSurface(uInt32 width, uInt32 height, const uInt32* data);

    void reinitializeBlitter(bool force = false);

    // Following constructors and assignment operators not supported
    FBSurfaceBIZHAWK() = delete;
    FBSurfaceBIZHAWK(const FBSurfaceBIZHAWK&) = delete;
    FBSurfaceBIZHAWK(FBSurfaceBIZHAWK&&) = delete;
    FBSurfaceBIZHAWK& operator=(const FBSurfaceBIZHAWK&) = delete;
    FBSurfaceBIZHAWK& operator=(FBSurfaceBIZHAWK&&) = delete;

  private:

    bool myIsVisible{true};
    bool myIsStatic{false};

    Common::Rect mySrcGUIR, myDstGUIR;
};

#endif
