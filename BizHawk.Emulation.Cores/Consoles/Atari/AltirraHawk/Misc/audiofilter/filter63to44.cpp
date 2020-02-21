#include <math.h>
#include <stdio.h>

const double kInFreq = 63920.8;
const double kOutFreq = 44100.0;
const double kPi = 3.1415926535;

const int NC = 64;
const int NP = 64;

double filt(double t) {
	t = fabs(t);

	double z = kPi * t * 30000.0 / kInFreq;
	double x = z > 1e-10 ? sin(z) / z : 1;
	double y = 0.42 + 0.5*cos(2*kPi*t / (double)NC) + 0.08*cos(4*kPi*t / (double)NC);

	return x * y;
}

double g_filtscale[8]={
	1.0/32.0,
	1.0/32.0,
	1.0/32.0,
	1.0/1.0,
	1.0/1.0,
	1.0/32.0,
	1.0/32.0,
	1.0/32.0,
};


int main(int argc, char **argv) {
	for(int phase=0; phase<=NP; ++phase) {
		double sum = 0;
		double coeff[NC];
		for(int i=0; i<NC; ++i) {
			double c = filt(-(double)phase / (double)NP + 1.0 - (double)(NC >> 1) + (double)i);
			sum += c;
			coeff[i] = c;
		}

		double remaining = 1.0;
		int ifilt[NC];
		for(int j=0; j<NC; ++j) {
			int i = j & 1 ? (NC - 1) - (j >> 1) : (j >> 1);
			double raw = coeff[i];
			double scale = raw * (fabs(sum) > 1e-10 ? remaining / sum : 1.0);

			double x = scale;
			int ival = (int)(x * 32768.0 / g_filtscale[i >> 3] + 0.5);
			double rval = ival / 32768.0 * g_filtscale[i >> 3];

			sum -= raw;
			remaining -= rval;

			ifilt[i] = ival;
		}

		printf("{");
		for(int i=0; i<NC; ++i)
			printf(",%d"+(i==0), ifilt[i]);
		printf("},\n");
	}

	return 0;
}

