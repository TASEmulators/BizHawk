//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2009-2019 Avery Lee
//
//	This program is free software; you can redistribute it and/or modify
//	it under the terms of the GNU General Public License as published by
//	the Free Software Foundation; either version 2 of the License, or
//	(at your option) any later version.
//
//	This program is distributed in the hope that it will be useful,
//	but WITHOUT ANY WARRANTY; without even the implied warranty of
//	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//	GNU General Public License for more details.
//
//	You should have received a copy of the GNU General Public License along
//	with this program. If not, see <http://www.gnu.org/licenses/>.

#include <stdafx.h>
#include <vd2/system/math.h>
#include <vd2/system/vdstl.h>
#include <vd2/VDDisplay/internal/screenfx.h>

void VDDisplayCreateGammaRamp(uint32 *gammaTex, uint32 len, bool enableInputConversion, bool useAdobeRgb, float outputGamma) {
	for(uint32 i=0; i<len; ++i) {
		float x = (float)i / (float)len;

		if (enableInputConversion) {
			if (useAdobeRgb) {
				x = powf(x, 1.0f / 2.2f);
			} else {
				x = (x < 0.0031308f) ? x * 12.92f : 1.055f * powf(x, 1.0f / 2.4f) - 0.055f;
			}
		}

		float y = powf(x, 1.0f / outputGamma);
		uint32 px = (uint32)(y * 255.0f + 0.5f) * 0x01010101;

		gammaTex[i] = px;
	}
}

void VDDisplayCreateScanlineMaskTexture(uint32 *scanlineTex, ptrdiff_t pitch, uint32 srcH, uint32 dstH, uint32 texSize, float intensity) {
	vdblock<float> rawMask(dstH);

	// Compute the stepping rate over the scanline mask pattern and check if we are
	// undersampling the mask (vertical resolution below 2 pixel rows per scanline).
	// Since the mask is a raised cosine, we can trivially apply a brickwall low pass
	// filter to it: just reduce it to DC-only below the Nyquist rate.

	float dvdy = (float)srcH / (float)dstH;
	if (dvdy <= 0.5f) {
		// Straight mapping would place the peak of the raised cosine in the center of
		// each scanline. We shift the pattern up by 1/4 scanline so that the two halves
		// of the scanline are full bright and full dark.

		float v = 0.25f + dvdy * 0.5f;

		for(uint32 i=0; i<dstH; ++i) {
			float y = 0.5f - 0.5f * cosf((v - floorf(v)) * nsVDMath::kfTwoPi);
			v += dvdy;

			rawMask[i] = y;
		}
	} else {
		std::fill(std::begin(rawMask), std::end(rawMask), 0.5f);
	}

	// Apply scanline intensity setting and convert the mask from linear to gamma
	// space. The intensity setting is adjusted so that the specified level is
	// achieved in gamma space.

	intensity *= intensity;
	for(float& y : rawMask) {
		y = y * (1.0f - intensity) + intensity;
		y = sqrtf(y);
	}

	// Convert the mask to texels.
	for(uint32 i=0; i<dstH; ++i) {
		float y = rawMask[i];

		uint32 px = (uint32)(y * 255.0f + 0.5f) * 0x01010101;

		scanlineTex[i] = px;
	}

	// Repeat the last entry to the end of the texture so it clamps cleanly.
	if (dstH < texSize)
		std::fill(scanlineTex + dstH, scanlineTex + texSize, scanlineTex[dstH - 1]); 
}

void VDDisplayDistortionMapping::Init(float viewAngleX, float viewRatioY, float viewAspect) {
	// The distortion algorithm works as follows:
	//
	//	- The screen is modeled as the front surface of an ellipsoid. At reduced distortion,
	//	  the size of the ellipsoid is reduced in one or both axes so that a smaller angle of
	//	  the ellipsoid is seen. When vertical distortion is disabled, it is infinitely tall
	//	  (a cylinder).
	//
	//	- For rendering, we need a reverse mapping from screen to image. A view ray is
	//	  constructed from the eye through the view plane and intersected against the
	//	  ellipsoid. A second ray representing the electron beam is then constructed and
	//	  reprojected onto the source image. The horizontal/vertical deflection slopes for
	//	  the beam are assumed to be linear with the image position, which makes this a
	//	  second projection. (Evaluation of physical accuracy is left to the reader.)
	//
	//	- For other rendering, such as for overlays, we need a forward mapping from image to
	//	  screen. As it happens, this is basically the same as the reverse mapping: construct
	//	  a projection ray through the image point, intersect it against the ellipsoid, and
	//	  reproject to the screen.
	//
	// The ellipsoid is sized by first starting with a sphere sized appropriately to be
	// inscribed within the dest area, then scaled according to the distortion view
	// angles. The view angle determines the angular amount of the ellipsoid subtended
	// horizontally by the image, and the view ratio Y parameter then sets the aspect
	// ratio of this projection (as a ratio of area, NOT angle).
	//
	// The destination area is assumed to have the same aspect ratio as the source, so the
	// destination aspect ratio is used to adjust the ellipsoid size to compensate for the
	// common aspect ratio.

	// Compute inverse half sizes for an ellipsoid with the right aspect ratio in view space.
	// Inverse radii are more convenient for the math and handle infinite radii, which are
	// necessary for an undistorted axis.

	const float invRadiusX = sinf(viewAngleX * (nsVDMath::kfPi / 180.0f) * 0.5f);
	const float invRadiusY = invRadiusX * viewRatioY / viewAspect;

	// The critical path we need to support is the reverse mapping since it is used in the
	// fragment program to map back from the screen to the image. This is the basic
	// algorithm:
	//
	//	unitSpherePos.xy = (screenPos - 0.5)*2 / radii
	//	unitSpherePos.z = sqrt(1 - dot(unitSpherePos.xy, unitSpherePos.xy))
	//	imagePos = unitSpherePos.xy * radii / unitSpherePos.z * imageScale
	//
	// The reverse mapping can be massaged for the pixel shader:
	//
	//	r = radii
	//	s = imageScale
	//	v = screenPos - 0.5
	//	v2 = v / (r * s)
	//	k = 1 / (2*s)^2 = (1*s^2)/4
	//	imagePos = v * rsqrt(k - dot(v2, v2))
	//
	// With imageScale set to the smaller of the two values that map (0.5,1) and (1,0.5)
	// to (x,1) and (1,y), inscribing the mapped image within the dest rect:
	//
	//	imageScale = 0.5 * sqrt(1 - 1/min(radii.x, radii.y)^2)

	const float maxInvRadius = std::min(invRadiusX, invRadiusY);
	const float invImageScale = 2.0f / sqrtf(1.0 - maxInvRadius*maxInvRadius);

	mScaleX = invRadiusX * invImageScale;
	mScaleY = invRadiusY * invImageScale;
	mSqRadius = invImageScale * invImageScale / 4.0f;
}

bool VDDisplayDistortionMapping::MapImageToScreen(vdfloat2& pt) const {
	using namespace nsVDMath;

	// clamp source point
	vdfloat2 pt2 { std::clamp(pt.x, 0.0f, 1.0f), std::clamp(pt.y, 0.0f, 1.0f) };

	bool valid = (pt2 == pt);
	
	// Forward projection:
	//
	//	-- warp coordinate system to change ellipsoid to unit sphere
	//	v = (imagePos - 0.5) / (r*s)
	//
	//	-- intersect image ray against the unit sphere by vec normalize
	//	z = sqrt(1 + dot(v.xy, v.xy))
	//	v /= z
	//
	//	-- unwarp the coordinate system and rebias from [-1,1] to [0,1]
	//	v = v * r / 2 - 0.5
	//
	// The slightly tricky part is that either axis of r can be infinite, so we must
	// avoid explicitly multiplying by it with some slight rearrangement. The rest
	// is just using the derived constants instead of r and s directly.

	vdfloat2 v = (pt2 - vdfloat2{0.5f, 0.5f});
	vdfloat2 v2 = v * vdfloat2{mScaleX, mScaleY};
	pt = v * sqrtf(mSqRadius / (1.0f + dot(v2, v2))) + vdfloat2{0.5f, 0.5f};

	return valid;
}

bool VDDisplayDistortionMapping::MapScreenToImage(vdfloat2& pt) const {
	using namespace nsVDMath;

	// convert point to ray cast from center of sphere to point on screen plane
	vdfloat2 v = pt - vdfloat2{0.5f, 0.5f};

	// intersect ray against sphere and reproject to source -- see Init() for
	// full derivation
	vdfloat2 v2 = v * vdfloat2{mScaleX, mScaleY};
	float d = std::max(1e-5f, mSqRadius - dot(v2, v2));

	v /= sqrtf(d);

	// clip the ray at the nearer of the X or Y border intersections
	float dx = fabsf(v.x);
	float dy = fabsf(v.y);
	float dmax = std::max(dx, dy);

	bool valid = true;
	if (dmax > 0.5f) {
		v /= 2.0f * dmax;
		valid = false;
	}

	// convert to source point
	pt = v + vdfloat2{0.5f, 0.5f};

	return valid;
}

