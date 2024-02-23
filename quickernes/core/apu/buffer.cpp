// Emu 0.7.0. http://www.slack.net/~ant/libs/

#include "buffer.hpp"
#include "apu.hpp"

/* Library Copyright (C) 2003-2006 Shay Green. This library is free software;
you can redistribute it and/or modify it under the terms of the GNU Lesser
General Public License as published by the Free Software Foundation; either
version 2.1 of the License, or (at your option) any later version. This
module is distributed in the hope that it will be useful, but WITHOUT ANY
WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR
A PARTICULAR PURPOSE.  See the GNU Lesser General Public License for more
details. You should have received a copy of the GNU Lesser General Public
License along with this library; if not, write to the Free Software
Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA */

namespace quickerNES
{

// Buffer

Buffer::Buffer() : Multi_Buffer(1) {}

Buffer::~Buffer() {}

Multi_Buffer *set_apu(Buffer *buf, Apu *apu)
{
  buf->set_apu(apu);
  return buf;
}

void Buffer::enable_nonlinearity(bool b)
{
  if (b)
    clear();

  Apu *apu = nonlin.enable(b, &tnd);
  apu->osc_output(0, &buf);
  apu->osc_output(1, &buf);
}

const char *Buffer::set_sample_rate(long rate, int msec)
{
  enable_nonlinearity(nonlin.enabled); // reapply
  buf.set_sample_rate(rate, msec);
  tnd.set_sample_rate(rate, msec);
  return Multi_Buffer::set_sample_rate(buf.sample_rate(), buf.length());
}

void Buffer::clock_rate(long rate)
{
  buf.clock_rate(rate);
  tnd.clock_rate(rate);
}

void Buffer::bass_freq(int freq)
{
  buf.bass_freq(freq);
  tnd.bass_freq(freq);
}

void Buffer::clear()
{
  nonlin.clear();
  buf.clear();
  tnd.clear();
}

Buffer::channel_t Buffer::channel(int i)
{
  channel_t c;
  c.center = &buf;
  if (2 <= i && i <= 4)
    c.center = &tnd; // only use for triangle, noise, and dmc
  c.left = c.center;
  c.right = c.center;
  return c;
}

void Buffer::end_frame(blip_time_t length, bool)
{
  buf.end_frame(length);
  tnd.end_frame(length);
}

long Buffer::samples_avail() const
{
  return buf.samples_avail();
}

long Buffer::read_samples(blip_sample_t *out, long count)
{
  count = nonlin.make_nonlinear(tnd, count);
  if (count)
  {
    Blip_Reader lin;
    Blip_Reader nonlin;

    int lin_bass = lin.begin(buf);
    int nonlin_bass = nonlin.begin(tnd);

    if (out != 0)
    {
      for (int n = count; n--;)
      {
        int s = lin.read() + nonlin.read();
        lin.next(lin_bass);
        nonlin.next(nonlin_bass);
        *out++ = s;

        if ((int16_t)s != s)
          out[-1] = 0x7FFF - (s >> 24);
      }
    }
    else
    {
      // only run accumulators, do not output audio
      for (int n = count; n--;)
      {
        lin.next(lin_bass);
        nonlin.next(nonlin_bass);
      }
    }

    lin.end(buf);
    nonlin.end(tnd);

    buf.remove_samples(count);
    tnd.remove_samples(count);
  }

  return count;
}

void Buffer::SaveAudioBufferState()
{
  SaveAudioBufferStatePrivate();
  nonlin.SaveAudioBufferState();
  buf.SaveAudioBufferState();
  tnd.SaveAudioBufferState();
}

void Buffer::RestoreAudioBufferState()
{
  RestoreAudioBufferStatePrivate();
  nonlin.RestoreAudioBufferState();
  buf.RestoreAudioBufferState();
  tnd.RestoreAudioBufferState();
}

// Nonlinearizer

Nonlinearizer::Nonlinearizer()
{
  apu = nullptr;
  enabled = true;

  float const gain = 0x7fff * 1.3f;
  // don't use entire range, so any overflow will stay within table
  int const range = (int)((double)table_size * Apu::nonlinear_tnd_gain());
  for (int i = 0; i < table_size; i++)
  {
    int const offset = table_size - range;
    int j = i - offset;
    float n = 202.0f / (range - 1) * j;
    float d = 0;
    // Prevent division by zero
    if (n)
      d = gain * 163.67f / (24329.0f / n + 100.0f);
    int out = (int)d;
    table[j & (table_size - 1)] = out;
  }
  extra_accum = 0;
  extra_prev = 0;
}

Apu *Nonlinearizer::enable(bool b, Blip_Buffer *buf)
{
  apu->osc_output(2, buf);
  apu->osc_output(3, buf);
  apu->osc_output(4, buf);
  enabled = b;
  if (b)
    apu->enable_nonlinear(1.0);
  else
    apu->volume(1.0);
  return apu;
}

#define ENTRY(s) table[(s) >> (blip_sample_bits - table_bits - 1) & (table_size - 1)]

long Nonlinearizer::make_nonlinear(Blip_Buffer &buf, long count)
{
  long avail = buf.samples_avail();
  if (count > avail)
    count = avail;
  if (count && enabled)
  {
    Blip_Buffer::buf_t_ *p = buf.buffer_;
    long accum = this->accum;
    long prev = this->prev;
    for (unsigned n = count; n; --n)
    {
      long entry = ENTRY(accum);
      accum += *p;
      *p++ = (entry - prev) << (blip_sample_bits - 16);
      prev = entry;
    }

    this->prev = prev;
    this->accum = accum;
  }

  return count;
}

void Nonlinearizer::clear()
{
  accum = 0;
  prev = ENTRY(86016000); // avoid thump due to APU's triangle dc bias
                          // TODO: still results in slight clicks and thumps
}

void Nonlinearizer::SaveAudioBufferState()
{
  extra_accum = accum;
  extra_prev = prev;
}

void Nonlinearizer::RestoreAudioBufferState()
{
  accum = extra_accum;
  prev = extra_prev;
}

} // namespace quickerNES