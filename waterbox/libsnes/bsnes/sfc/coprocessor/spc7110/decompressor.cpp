//SPC7110 decompressor
//original implementation: neviksti
//optimized implementation: talarubi

struct Decompressor {
  SPC7110& spc7110;

  Decompressor(SPC7110& spc7110) : spc7110(spc7110) {}

  auto read() -> uint8 {
    return spc7110.dataromRead(offset++);
  }

  //inverse morton code transform: unpack big-endian packed pixels
  //returns odd bits in lower half; even bits in upper half
  auto deinterleave(uint64 data, uint bits) -> uint32 {
    data = data & (1ull << bits) - 1;
    data = 0x5555555555555555ull & (data << bits | data >> 1);
    data = 0x3333333333333333ull & (data | data >> 1);
    data = 0x0f0f0f0f0f0f0f0full & (data | data >> 2);
    data = 0x00ff00ff00ff00ffull & (data | data >> 4);
    data = 0x0000ffff0000ffffull & (data | data >> 8);
    return data | data >> 16;
  }

  //extract a nibble and move it to the low four bits
  auto moveToFront(uint64 list, uint nibble) -> uint64 {
    for(uint64 n = 0, mask = ~15; n < 64; n += 4, mask <<= 4) {
      if((list >> n & 15) != nibble) continue;
      return list = (list & mask) + (list << 4 & ~mask) + nibble;
    }
    return list;
  }

  auto initialize(uint mode, uint origin) -> void {
    for(auto& root : context) for(auto& node : root) node = {0, 0};
    bpp = 1 << mode;
    offset = origin;
    bits = 8;
    range = Max + 1;
    input = read();
    input = input << 8 | read();
    output = 0;
    pixels = 0;
    colormap = 0xfedcba9876543210ull;
  }

  auto decode() -> void {
    for(uint pixel = 0; pixel < 8; pixel++) {
      uint64 map = colormap;
      uint diff = 0;

      if(bpp > 1) {
        uint pa = (bpp == 2 ? pixels >>  2 & 3 : pixels >>  0 & 15);
        uint pb = (bpp == 2 ? pixels >> 14 & 3 : pixels >> 28 & 15);
        uint pc = (bpp == 2 ? pixels >> 16 & 3 : pixels >> 32 & 15);

        if(pa != pb || pb != pc) {
          uint match = pa ^ pb ^ pc;
          diff = 4;                        //no match; all pixels differ
          if((match ^ pc) == 0) diff = 3;  //a == b; pixel c differs
          if((match ^ pb) == 0) diff = 2;  //c == a; pixel b differs
          if((match ^ pa) == 0) diff = 1;  //b == c; pixel a differs
        }

        colormap = moveToFront(colormap, pa);

        map = moveToFront(map, pc);
        map = moveToFront(map, pb);
        map = moveToFront(map, pa);
      }

      for(uint plane = 0; plane < bpp; plane++) {
        uint bit = bpp > 1 ? 1 << plane : 1 << (pixel & 3);
        uint history = bit - 1 & output;
        uint set = 0;

        if(bpp == 1) set = pixel >= 4;
        if(bpp == 2) set = diff;
        if(plane >= 2 && history <= 1) set = diff;

        auto& ctx = context[set][bit + history - 1];
        auto& model = evolution[ctx.prediction];
        uint8 lps_offset = range - model.probability;
        bool symbol = input >= (lps_offset << 8);  //test only the MSB

        output = output << 1 | (symbol ^ ctx.swap);

        if(symbol == MPS) {          //[0 ... range-p]
          range = lps_offset;        //range = range-p
        } else {                     //[range-p+1 ... range]
          range -= lps_offset;       //range = p-1, with p < 0.75
          input -= lps_offset << 8;  //therefore, always rescale
        }

        while(range <= Max / 2) {    //scale back into [0.75 ... 1.5]
          ctx.prediction = model.next[symbol];

          range <<= 1;
          input <<= 1;

          if(--bits == 0) {
            bits = 8;
            input += read();
          }
        }

        if(symbol == LPS && model.probability > Half) ctx.swap ^= 1;
      }

      uint index = output & (1 << bpp) - 1;
      if(bpp == 1) index ^= pixels >> 15 & 1;

      pixels = pixels << bpp | (map >> 4 * index & 15);
    }

    if(bpp == 1) result = pixels;
    if(bpp == 2) result = deinterleave(pixels, 16);
    if(bpp == 4) result = deinterleave(deinterleave(pixels, 32), 32);
  }

  auto serialize(serializer& s) -> void {
    for(auto& root : context) {
      for(auto& node : root) {
        s.integer(node.prediction);
        s.integer(node.swap);
      }
    }

    s.integer(bpp);
    s.integer(offset);
    s.integer(bits);
    s.integer(range);
    s.integer(input);
    s.integer(output);
    s.integer(pixels);
    s.integer(colormap);
    s.integer(result);
  }

  enum : uint { MPS = 0, LPS = 1 };
  enum : uint { One = 0xaa, Half = 0x55, Max = 0xff };

  struct ModelState {
    uint8 probability;  //of the more probable symbol (MPS)
    uint8 next[2];      //next state after output {MPS, LPS}
  };
  static ModelState evolution[53];

  struct Context {
    uint8 prediction;   //current model state
    uint8 swap;         //if 1, exchange the role of MPS and LPS
  } context[5][15];     //not all 75 contexts exists; this simplifies the code

  uint bpp;             //bits per pixel (1bpp = 1; 2bpp = 2; 4bpp = 4)
  uint offset;          //SPC7110 data ROM read offset
  uint bits;            //bits remaining in input
  uint16 range;         //arithmetic range: technically 8-bits, but Max+1 = 256
  uint16 input;         //input data from SPC7110 data ROM
  uint8 output;
  uint64 pixels;
  uint64 colormap;      //most recently used list
  uint32 result;        //decompressed word after calling decode()
};

Decompressor::ModelState Decompressor::evolution[53] = {
  {0x5a, { 1, 1}}, {0x25, { 2, 6}}, {0x11, { 3, 8}},
  {0x08, { 4,10}}, {0x03, { 5,12}}, {0x01, { 5,15}},

  {0x5a, { 7, 7}}, {0x3f, { 8,19}}, {0x2c, { 9,21}},
  {0x20, {10,22}}, {0x17, {11,23}}, {0x11, {12,25}},
  {0x0c, {13,26}}, {0x09, {14,28}}, {0x07, {15,29}},
  {0x05, {16,31}}, {0x04, {17,32}}, {0x03, {18,34}},
  {0x02, { 5,35}},

  {0x5a, {20,20}}, {0x48, {21,39}}, {0x3a, {22,40}},
  {0x2e, {23,42}}, {0x26, {24,44}}, {0x1f, {25,45}},
  {0x19, {26,46}}, {0x15, {27,25}}, {0x11, {28,26}},
  {0x0e, {29,26}}, {0x0b, {30,27}}, {0x09, {31,28}},
  {0x08, {32,29}}, {0x07, {33,30}}, {0x05, {34,31}},
  {0x04, {35,33}}, {0x04, {36,33}}, {0x03, {37,34}},
  {0x02, {38,35}}, {0x02, { 5,36}},

  {0x58, {40,39}}, {0x4d, {41,47}}, {0x43, {42,48}},
  {0x3b, {43,49}}, {0x34, {44,50}}, {0x2e, {45,51}},
  {0x29, {46,44}}, {0x25, {24,45}},

  {0x56, {48,47}}, {0x4f, {49,47}}, {0x47, {50,48}},
  {0x41, {51,49}}, {0x3c, {52,50}}, {0x37, {43,51}},
};
