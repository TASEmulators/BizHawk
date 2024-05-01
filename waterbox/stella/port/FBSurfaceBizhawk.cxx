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

#include "FBSurfaceBizhawk.hxx"

#include "Logger.hxx"
#include "ThreadDebugging.hxx"
#include "bk_blitter/BlitterFactory.hxx"

namespace {
  BlitterFactory::ScalingAlgorithm scalingAlgorithm(ScalingInterpolation inter)
  {
    switch (inter) {
      case ScalingInterpolation::none:
        return BlitterFactory::ScalingAlgorithm::nearestNeighbour;

      case ScalingInterpolation::blur:
        return BlitterFactory::ScalingAlgorithm::bilinear;

      case ScalingInterpolation::sharp:
        return BlitterFactory::ScalingAlgorithm::quasiInteger;

      default:
        throw runtime_error("unreachable");
    }
  }
} // namespace

// - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
FBSurfaceBIZHAWK::FBSurfaceBIZHAWK(FBBackendBIZHAWK& backend,
                             uInt32 width, uInt32 height,
                             ScalingInterpolation inter,
                             const uInt32* staticData)
  : myBackend{backend},
    myInterpolationMode{inter}
{
  //cerr << width << " x " << height << '\n';
  createSurface(width, height, staticData);
}

// - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
FBSurfaceBIZHAWK::~FBSurfaceBIZHAWK()
{
  ASSERT_MAIN_THREAD;

  if(mySurface)
  {
    SDL_FreeSurface(mySurface);
    mySurface = nullptr;
  }
}

// - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
void FBSurfaceBIZHAWK::fillRect(uInt32 x, uInt32 y, uInt32 w, uInt32 h, ColorId color)
{
  ASSERT_MAIN_THREAD;

  // Fill the rectangle
  SDL_Rect tmp;
  tmp.x = x;
  tmp.y = y;
  tmp.w = w;
  tmp.h = h;
  SDL_FillRect(mySurface, &tmp, myPalette[color]);
}

// - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
uInt32 FBSurfaceBIZHAWK::width() const
{
  return mySurface->w;
}

// - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
uInt32 FBSurfaceBIZHAWK::height() const
{
  return mySurface->h;
}

// - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
const Common::Rect& FBSurfaceBIZHAWK::srcRect() const
{
  return mySrcGUIR;
}

// - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
const Common::Rect& FBSurfaceBIZHAWK::dstRect() const
{
  return myDstGUIR;
}

// - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
void FBSurfaceBIZHAWK::setSrcPos(uInt32 x, uInt32 y)
{
  if(setSrcPosInternal(x, y))
    reinitializeBlitter();
}

// - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
void FBSurfaceBIZHAWK::setSrcSize(uInt32 w, uInt32 h)
{
  if(setSrcSizeInternal(w, h))
    reinitializeBlitter();
}

// - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
void FBSurfaceBIZHAWK::setSrcRect(const Common::Rect& r)
{
  const bool posChanged = setSrcPosInternal(r.x(), r.y()),
             sizeChanged = setSrcSizeInternal(r.w(), r.h());

  if(posChanged || sizeChanged)
    reinitializeBlitter();
}

// - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
void FBSurfaceBIZHAWK::setDstPos(uInt32 x, uInt32 y)
{
  if(setDstPosInternal(x, y))
    reinitializeBlitter();
}

// - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
void FBSurfaceBIZHAWK::setDstSize(uInt32 w, uInt32 h)
{
  if(setDstSizeInternal(w, h))
    reinitializeBlitter();
}

// - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
void FBSurfaceBIZHAWK::setDstRect(const Common::Rect& r)
{
  const bool posChanged = setDstPosInternal(r.x(), r.y()),
             sizeChanged = setDstSizeInternal(r.w(), r.h());

  if(posChanged || sizeChanged)
    reinitializeBlitter();
}

// - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
void FBSurfaceBIZHAWK::setVisible(bool visible)
{
  myIsVisible = visible;
}

// - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
void FBSurfaceBIZHAWK::translateCoords(Int32& x, Int32& y) const
{
  x -= myDstR.x;  x /= myDstR.w / mySrcR.w;
  y -= myDstR.y;  y /= myDstR.h / mySrcR.h;
}

// - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
bool FBSurfaceBIZHAWK::render()
{
  if (!myBlitter) reinitializeBlitter();

  if(myIsVisible && myBlitter)
  {
    myBlitter->blit(*mySurface);

    return true;
  }
  return false;
}

// - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
void FBSurfaceBIZHAWK::invalidate()
{
  ASSERT_MAIN_THREAD;

  SDL_FillRect(mySurface, nullptr, 0);
}

// - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
void FBSurfaceBIZHAWK::invalidateRect(uInt32 x, uInt32 y, uInt32 w, uInt32 h)
{
  ASSERT_MAIN_THREAD;

  // Clear the rectangle
  SDL_Rect tmp;
  tmp.x = x;
  tmp.y = y;
  tmp.w = w;
  tmp.h = h;
  // Note: Transparency has to be 0 to clear the rectangle foreground
  //  without affecting the background display.
  SDL_FillRect(mySurface, &tmp, 0);
}

// - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
void FBSurfaceBIZHAWK::reload()
{
  reinitializeBlitter(true);
}

// - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
void FBSurfaceBIZHAWK::resize(uInt32 width, uInt32 height)
{
  ASSERT_MAIN_THREAD;

  if(mySurface)
    SDL_FreeSurface(mySurface);

  // NOTE: Currently, a resize changes a 'static' surface to 'streaming'
  //       No code currently does this, but we should at least check for it
  if(myIsStatic)
    Logger::error("Resizing static texture!");

  createSurface(width, height, nullptr);
}

// - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
void FBSurfaceBIZHAWK::createSurface(uInt32 width, uInt32 height,
                                  const uInt32* data)
{
  ASSERT_MAIN_THREAD;

  // Create a surface in the same format as the parent GL class
  const SDL_PixelFormat& pf = myBackend.pixelFormat();

  mySurface = SDL_CreateRGBSurface(0, width, height,
      pf.BitsPerPixel, pf.Rmask, pf.Gmask, pf.Bmask, pf.Amask);
  //SDL_SetSurfaceBlendMode(mySurface, SDL_BLENDMODE_ADD); // default: SDL_BLENDMODE_BLEND

  // We start out with the src and dst rectangles containing the same
  // dimensions, indicating no scaling or re-positioning
  setSrcPosInternal(0, 0);
  setDstPosInternal(0, 0);
  setSrcSizeInternal(width, height);
  setDstSizeInternal(width, height);

  ////////////////////////////////////////////////////
  // These *must* be set for the parent class
  myPixels = static_cast<uInt32*>(mySurface->pixels);
  myPitch = mySurface->pitch / pf.BytesPerPixel;
  ////////////////////////////////////////////////////

  myIsStatic = data != nullptr;
  if(myIsStatic)
    SDL_memcpy(mySurface->pixels, data,
               static_cast<size_t>(mySurface->w) * mySurface->h * 4);

  reload();  // NOLINT
}

// - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
void FBSurfaceBIZHAWK::reinitializeBlitter(bool force)
{
  if (force)
    myBlitter.reset();

  if (!myBlitter && myBackend.isInitialized())
    myBlitter = BlitterFactory::createBlitter(
        myBackend, scalingAlgorithm(myInterpolationMode));

  if (myBlitter)
    myBlitter->reinitialize(mySrcR, myDstR, myAttributes,
                            myIsStatic ? mySurface : nullptr);
}

// - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
void FBSurfaceBIZHAWK::applyAttributes()
{
  reinitializeBlitter();
}

// - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
void FBSurfaceBIZHAWK::setScalingInterpolation(ScalingInterpolation interpolation)
{
  if (interpolation == ScalingInterpolation::sharp &&
      (
        static_cast<int>(mySrcGUIR.h()) >= myBackend.scaleY(myDstGUIR.h()) ||
        static_cast<int>(mySrcGUIR.w()) >= myBackend.scaleX(myDstGUIR.w())
      )
  )
    interpolation = ScalingInterpolation::blur;

  if (interpolation == myInterpolationMode) return;

  myInterpolationMode = interpolation;
  reload();
}
