#pragma once

namespace nall::Encode {

struct WAV {
  static auto stereo_16bit(const string& filename, array_view<int16_t> left, array_view<int16_t> right, uint frequency) -> bool {
    if(left.size() != right.size()) return false;
    static uint channels = 2;
    static uint bits = 16;
    static uint samples = left.size();

    file_buffer fp;
    if(!fp.open(filename, file::mode::write)) return false;

    fp.write('R');
    fp.write('I');
    fp.write('F');
    fp.write('F');
    fp.writel(4 + (8 + 16) + (8 + samples * 4), 4);

    fp.write('W');
    fp.write('A');
    fp.write('V');
    fp.write('E');

    fp.write('f');
    fp.write('m');
    fp.write('t');
    fp.write(' ');
    fp.writel(16, 4);
    fp.writel(1, 2);
    fp.writel(channels, 2);
    fp.writel(frequency, 4);
    fp.writel(frequency * channels * bits, 4);
    fp.writel(channels * bits, 2);
    fp.writel(bits, 2);

    fp.write('d');
    fp.write('a');
    fp.write('t');
    fp.write('a');
    fp.writel(samples * 4, 4);
    for(uint sample : range(samples)) {
      fp.writel(left[sample], 2);
      fp.writel(right[sample], 2);
    }

    return true;
  }
};

}
