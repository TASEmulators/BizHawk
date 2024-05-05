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

#include <cmath>

#include "bspf.hxx"
#include "Logger.hxx"

#include "Console.hxx"
#include "OSystem.hxx"
#include "Settings.hxx"

#include "ThreadDebugging.hxx"
#include "FBSurfaceBizhawk.hxx"
#include "FBBackendBizhawk.hxx"

// - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
FBBackendBIZHAWK::FBBackendBIZHAWK(OSystem& osystem)
  : myOSystem{osystem}
{
}

// - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
FBBackendBIZHAWK::~FBBackendBIZHAWK()
{

}

// - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
void FBBackendBIZHAWK::queryHardware(vector<Common::Size>& fullscreenRes,
                                  vector<Common::Size>& windowedRes,
                                  VariantList& renderers)
{

}

// - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
bool FBBackendBIZHAWK::isCurrentWindowPositioned() const
{
  return true;
}

// - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
Common::Point FBBackendBIZHAWK::getCurrentWindowPos() const
{
  Common::Point pos;


  return pos;
}

// - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
Int32 FBBackendBIZHAWK::getCurrentDisplayIndex() const
{


  return 0;
}

// - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
bool FBBackendBIZHAWK::setVideoMode(const VideoModeHandler::Mode& mode,
                                 int winIdx, const Common::Point& winPos)
{

  return true;
}


// - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
bool FBBackendBIZHAWK::createRenderer(const VideoModeHandler::Mode& mode)
{

  return true;
}

// - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
void FBBackendBIZHAWK::setTitle(string_view title)
{
}

// - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
string FBBackendBIZHAWK::about() const
{
  return "";
}

// - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
void FBBackendBIZHAWK::showCursor(bool show)
{
}

// - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
void FBBackendBIZHAWK::grabMouse(bool grab)
{
}

// - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
bool FBBackendBIZHAWK::fullScreen() const
{
  return true;
}

// - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
int FBBackendBIZHAWK::refreshRate() const
{
  return 0;
}

// - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
void FBBackendBIZHAWK::renderToScreen()
{

}

// - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
void FBBackendBIZHAWK::setWindowIcon()
{
}

// - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
unique_ptr<FBSurface> FBBackendBIZHAWK::createSurface(
  uInt32 w,
  uInt32 h,
  ScalingInterpolation inter,
  const uInt32* data
) const
{
  return make_unique<FBSurfaceBIZHAWK>
      (const_cast<FBBackendBIZHAWK&>(*this), w, h, inter, data);
}

// - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
void FBBackendBIZHAWK::readPixels(uInt8* buffer, size_t pitch,
                               const Common::Rect& rect) const
{
}

// - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
void FBBackendBIZHAWK::clear()
{
}

// - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
void FBBackendBIZHAWK::detectFeatures()
{
}

// - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
bool FBBackendBIZHAWK::detectRenderTargetSupport()
{
  return 0;
}

// - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
void FBBackendBIZHAWK::determineDimensions()
{
}
