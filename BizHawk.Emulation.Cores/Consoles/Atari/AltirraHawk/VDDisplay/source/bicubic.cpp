#include <stdafx.h>
#include <vd2/system/math.h>
#include "bicubic.h"

// Theory of operation:
//
// The cubic filter is composed of two cubic splines back to back,
// with the following constraints:
//
// f0(0) = 1
// f0(1) = 0
// f0'(0) = 0
// f0'(1) = A
// f1'(0) = A
// f1(0) = 0
// f1(1) = 0
// f1'(1) = 0
//
// A is the sharpness parameter and varies from a soft -0.5 to a stiff
// -1.0. Given these constraints, we can produce the cubic polynomials:
//
// f0(t) = (A+2)t^3 - (A+3)t^2 + 1
// f1(t) = At^3 - 2At^2 + At
//
// To produce a 4-tap filter, we also need to sample the other reflected
// side of the kernel, which means evaluating these at 1-t:
//
// f2(t) = f0(1-t) = -(A+2)t^3 + (2A+3)t^2 - At
// f3(t) = f1(1-t) = -At^3 + At^2
//
// These four cubic polynomials, when evaluated at a value of t, produce
// a sampled kernel:
//
//
//                     _______
//                  __/      .\__
//                 /         .   \
//                /          .    \
//               /.          .     \ 
//       f1     / .          .      \     f3
// -|----------|----------|----------|----------|
//    \._     /     f0         f2     \ .   __/
//       \___/                         \.__/
//
// Now, we could throw these into a filter texture and evaluate them on
// the shader, but there is a nasty problem: in order to get clean samples
// we would need to sample the source texture in point mode, and as it turns
// out, the filter texture has a discontinuity exactly where we also need
// the texture samplers to step. This doesn't always happen exactly synchronized
// and the result is a very ugly glitch.
//
// One solution to this problem is to store an offset to push the texture
// samples to the intended location, ensuring that the graphics hardware
// samples where we computed the filter kernel. Since the texture only has
// four components, we need to steal one and compute the missing tap in the
// shader. The advantage of doing this is that we don't have to worry about
// renormalizing the texture. However, the disadvantage is that we need to
// do dependent texture reads.
//
// The alternative solution, which we compute the filter texture for here,
// is to use linear texture interpolation to help us compute the center taps
// in a glitchless manner. The center two taps, driven by f0/f2, are the only
// ones that are problematic as f1(t) and f3(t) are both 0 at the cut-over
// point. The trick is noticing that the curve traced by f0/f2 is always
// greater or equal than the value of a triangle signal. This means that we
// can sample the center point twice in both bilinear and point sampling
// modes and interpolate between the two to get the desired weighting between
// the center taps.
//
// That leaves the outer taps, which are always zero or negative. What we do
// here is weight the two outer taps against each other and then blend that
// result with the center taps. Because the filter kernel always sums to 1,
// any increase in the weight on the outer taps must be exactly balanced by
// the weights on the inner taps. Therefore, we encode a texture as follows:
//
// red = sum of outer tap weights
// green = difference between triangle and nearest center tap weight
// blue = lerp factor between outer taps
//
// As it turns out, the sum of the two outer taps is -Ad(1-d), and the relative
// weighting between the two taps is linear. That takes care of the red and blue
// encodings. The green encoding for the center tap weighting is non-trivial
// and so we just compute both taps and divide one by the sum.
//
// The range for the outer tap weighting (red term) is at most 0.25, so we
// store it scaled. The green term is similarly small.

void VDDisplayCreateBicubicTexture(uint32 *dst, int w, int srcw, bool swapRB) {
	double dudx = (double)srcw / (double)w;
	double u = dudx * 0.5;
	double u0 = 0.5;
	double ud0 = 1.5;
	double ud1 = (double)srcw - 1.5;
	double u1 = (double)srcw - 0.5;

	for(int x = 0; x < w; ++x) {
		double ut = u;
		if (ut < u0)
			ut = u0;
		else if (ut > u1)
			ut = u1;
		int ix = VDFloorToInt(ut - 0.5);
		double d = ut - ((double)ix + 0.5);

		static const double A = -0.75;
		double c1 = (( (A+2.0)*d -     A-3.0)*d      )*d + 1.0;
		double c2 = ((-(A+2.0)*d + 2.0*A+3.0)*d -   A)*d;

		double blue		= 1-d;
		double green	= d < 0.5 ? c1 / (c1 + c2) - (1.0-d) : c2 / (c1 + c2) - d;
		double red		= -A*d*(1-d);

		if (ut < ud0 || ut > ud1) {
			red = 0;
			green = 0;
			blue = 0;
		}

		green *= 4;
		red *= 4;

		uint8 ib = VDClampedRoundFixedToUint8Fast((float)blue);
		uint8 ig = VDClampedRoundFixedToUint8Fast((float)green);
		uint8 ir = VDClampedRoundFixedToUint8Fast((float)red);

		if (swapRB)
			dst[x] = (uint32)ir + ((uint32)ig << 8) + ((uint32)ib << 16);
		else
			dst[x] = (uint32)ib + ((uint32)ig << 8) + ((uint32)ir << 16);

		u += dudx;
	}
}
