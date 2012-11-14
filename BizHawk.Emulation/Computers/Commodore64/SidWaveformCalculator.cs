using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64
{
	class SidWaveformCalculator
	{
		struct CombinedWaveformConfig
		{
			public float bias;
			public float pulseStrength;
			public float topBit;
			public float distance;
			public float stMix;

			public CombinedWaveformConfig(float newBias, float newPS, float newtB, float newDistance, float newStMix)
			{
				bias = newBias;
				pulseStrength = newPS;
				topBit = newtB;
				distance = newDistance;
				stMix = newStMix;
			}
		}

		private static CombinedWaveformConfig[] cfgArray = new CombinedWaveformConfig[]
		{
			new CombinedWaveformConfig(0.880815f, 0.0f, 0.0f, 0.3279614f, 0.5999545f),
			new CombinedWaveformConfig(0.8924618f, 2.014781f, 1.003332f, 0.02992322f, 0.0f),
			new CombinedWaveformConfig(0.8646501f, 1.712586f, 1.137704f, 0.02845423f, 0.0f),
			new CombinedWaveformConfig(0.9527834f, 1.794777f, 0.0f, 0.09806272f, 0.7752482f)
		};

		public short[][] BuildTable()
		{
			short[][] wftable = new short[8][];
			for (int i = 0; i < 8; i++)
				wftable[i] = new short[4096];

			for (int accumulator = 0; accumulator < 1 << 24; accumulator += 1 << 12)
			{
				int idx = (accumulator >> 12);
				wftable[0][idx] = 0xFFF;
				wftable[1][idx] = (short)((accumulator & 0x800000) == 0 ? idx << 1 : (idx ^ 0xFFF) << 1);
				wftable[2][idx] = (short)idx;
				wftable[3][idx] = CalculateCombinedWaveForm(cfgArray[0], 3, accumulator);
				wftable[4][idx] = 0xFFF;
				wftable[5][idx] = CalculateCombinedWaveForm(cfgArray[1], 5, accumulator);
				wftable[6][idx] = CalculateCombinedWaveForm(cfgArray[2], 6, accumulator);
				wftable[7][idx] = CalculateCombinedWaveForm(cfgArray[3], 7, accumulator);
			}

			return wftable;
		}

		private short CalculateCombinedWaveForm(CombinedWaveformConfig config, int waveform, int accumulator)
		{
			float[] o = new float[12];

			for (int i = 0; i < 12; i++)
			{
				o[i] = (accumulator >> 12 & (1 << i)) != 0 ? 1.0f : 0.0f;
			}

			if ((waveform & 3) == 1)
			{
				bool top = (accumulator & 0x800000) != 0;
				for (int i = 11; i > 0; i--)
				{
					o[i] = top ? 1.0f - o[i - 1] : o[i - 1];
				}
			}

			if ((waveform & 3) == 3)
			{
				o[0] *= config.stMix;
				for (int i = 1; i < 12; i++)
				{
					o[i] = o[i - 1] * (1.0f - config.stMix) + o[i] * config.stMix;
				}
			}

			o[11] *= config.topBit;

			if (waveform == 3 || waveform > 4)
			{
				float[] distanceTable = new float[12 * 2 + 1];
				for (int i = 0; i <= 12; i++)
				{
					distanceTable[12 + i] = distanceTable[12 - i] = 1.0f / (1.0f + i * i * config.distance);
				}

				float[] tmp = new float[12];
				for (int i = 0; i < 12; i++)
				{
					float avg = 0.0f;
					float n = 0.0f;
					for (int j = 0; j < 12; j++)
					{
						float weight = distanceTable[i - j + 12];
						avg += o[j] * weight;
						n += weight;
					}
					if (waveform > 4)
					{
						float weight = distanceTable[i - 12 + 12];
						avg += config.pulseStrength * weight;
						n += weight;
					}
					tmp[i] = (o[i] + avg / n) * 0.5f;
				}

				for (int i = 0; i < 12; i++)
				{
					o[i] = tmp[i];
				}
			}

			int value = 0;
			for (int i = 0; i < 12; i++)
			{
				if (o[i] - config.bias > 0.0f)
				{
					value |= 1 << i;
				}
			}

			return (short)value;
		}
	}
}
