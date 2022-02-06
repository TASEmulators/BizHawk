#pragma once

//a bad implementation of inflate from zlib/minizip
//todo: replace with Talarubi's version

#include <setjmp.h>

namespace nall::Decode {

namespace puff {
  inline auto puff(u8* dest, u32* destlen, u8* source, u32* sourcelen) -> s32;
}

inline auto inflate(u8* target, u32 targetLength, const u8* source, u32 sourceLength) -> bool {
  u32 tl = targetLength, sl = sourceLength;
  s32 result = puff::puff((u8*)target, &tl, (u8*)source, &sl);
  return result == 0;
}

namespace puff {

enum : u32 {
  MAXBITS   =  15,
  MAXLCODES = 286,
  MAXDCODES =  30,
  FIXLCODES = 288,
  MAXCODES  = MAXLCODES + MAXDCODES,
};

struct state {
  u8* out;
  u32 outlen;
  u32 outcnt;

  u8* in;
  u32 inlen;
  u32 incnt;
  s32 bitbuf;
  s32 bitcnt;

  jmp_buf env;
};

struct huffman {
  s16* count;
  s16* symbol;
};

inline auto bits(state* s, s32 need) -> s32 {
  s32 val;

  val = s->bitbuf;
  while(s->bitcnt < need) {
    if(s->incnt == s->inlen) longjmp(s->env, 1);
    val |= (s32)(s->in[s->incnt++]) << s->bitcnt;
    s->bitcnt += 8;
  }

  s->bitbuf = (s32)(val >> need);
  s->bitcnt -= need;

  return (s32)(val & ((1L << need) - 1));
}

inline auto stored(state* s) -> s32 {
  u32 len;

  s->bitbuf = 0;
  s->bitcnt = 0;

  if(s->incnt + 4 > s->inlen) return 2;
  len  = s->in[s->incnt++];
  len |= s->in[s->incnt++] << 8;
  if(s->in[s->incnt++] != (~len & 0xff) ||
     s->in[s->incnt++] != ((~len >> 8) & 0xff)
  ) return 2;

  if(s->incnt + len > s->inlen) return 2;
  if(s->out != nullptr) {
    if(s->outcnt + len > s->outlen) return 1;
    while(len--) s->out[s->outcnt++] = s->in[s->incnt++];
  } else {
    s->outcnt += len;
    s->incnt += len;
  }

  return 0;
}

inline auto decode(state* s, huffman* h) -> s32 {
  s32 len, code, first, count, index, bitbuf, left;
  s16* next;

  bitbuf = s->bitbuf;
  left = s->bitcnt;
  code = first = index = 0;
  len = 1;
  next = h->count + 1;
  while(true) {
    while(left--) {
      code |= bitbuf & 1;
      bitbuf >>= 1;
      count = *next++;
      if(code - count < first) {
        s->bitbuf = bitbuf;
        s->bitcnt = (s->bitcnt - len) & 7;
        return h->symbol[index + (code - first)];
      }
      index += count;
      first += count;
      first <<= 1;
      code <<= 1;
      len++;
    }
    left = (MAXBITS + 1) - len;
    if(left == 0) break;
    if(s->incnt == s->inlen) longjmp(s->env, 1);
    bitbuf = s->in[s->incnt++];
    if(left > 8) left = 8;
  }

  return -10;
}

inline auto construct(huffman* h, s16* length, s32 n) -> s32 {
  s32 symbol, len, left;
  s16 offs[MAXBITS + 1];

  for(len = 0; len <= MAXBITS; len++) h->count[len] = 0;
  for(symbol = 0; symbol < n; symbol++) h->count[length[symbol]]++;
  if(h->count[0] == n) return 0;

  left = 1;
  for(len = 1; len <= MAXBITS; len++) {
    left <<= 1;
    left -= h->count[len];
    if(left < 0) return left;
  }

  offs[1] = 0;
  for(len = 1; len < MAXBITS; len++) offs[len + 1] = offs[len] + h->count[len];

  for(symbol = 0; symbol < n; symbol++) {
    if(length[symbol] != 0) h->symbol[offs[length[symbol]]++] = symbol;
  }

  return left;
}

inline auto codes(state* s, huffman* lencode, huffman* distcode) -> s32 {
  s32 symbol, len;
  u32 dist;
  static const s16 lens[29] = {
    3, 4, 5, 6, 7, 8, 9, 10, 11, 13, 15, 17, 19, 23, 27, 31,
    35, 43, 51, 59, 67, 83, 99, 115, 131, 163, 195, 227, 258
  };
  static const s16 lext[29] = {
    0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 2, 2, 2, 2,
    3, 3, 3, 3, 4, 4, 4, 4, 5, 5, 5, 5, 0
  };
  static const s16 dists[30] = {
    1, 2, 3, 4, 5, 7, 9, 13, 17, 25, 33, 49, 65, 97, 129, 193,
    257, 385, 513, 769, 1025, 1537, 2049, 3073, 4097, 6145,
    8193, 12289, 16385, 24577
  };
  static const s16 dext[30] = {
    0, 0, 0, 0, 1, 1, 2, 2, 3, 3, 4, 4, 5, 5, 6, 6,
    7, 7, 8, 8, 9, 9, 10, 10, 11, 11,
    12, 12, 13, 13
  };

  do {
    symbol = decode(s, lencode);
    if(symbol < 0) return symbol;
    if(symbol < 256) {
      if(s->out != nullptr) {
        if(s->outcnt == s->outlen) return 1;
        s->out[s->outcnt] = symbol;
      }
      s->outcnt++;
    } else if(symbol > 256) {
      symbol -= 257;
      if(symbol >= 29) return -10;
      len = lens[symbol] + bits(s, lext[symbol]);

      symbol = decode(s, distcode);
      if(symbol < 0) return symbol;
      dist = dists[symbol] + bits(s, dext[symbol]);
      #ifndef INFLATE_ALLOW_INVALID_DISTANCE_TOO_FAR
      if(dist > s->outcnt) return -11;
      #endif

      if(s->out != nullptr) {
        if(s->outcnt + len > s->outlen) return 1;
        while(len--) {
          s->out[s->outcnt] =
          #ifdef INFLATE_ALLOW_INVALID_DISTANCE_TOO_FAR
          dist > s->outcnt ? 0 :
          #endif
          s->out[s->outcnt - dist];
          s->outcnt++;
        }
      } else {
        s->outcnt += len;
      }
    }
  } while(symbol != 256);

  return 0;
}

inline auto fixed(state* s) -> s32 {
  static s32 virgin = 1;
  static s16 lencnt[MAXBITS + 1], lensym[FIXLCODES];
  static s16 distcnt[MAXBITS + 1], distsym[MAXDCODES];
  static huffman lencode, distcode;

  if(virgin) {
    s32 symbol = 0;
    s16 lengths[FIXLCODES];

    lencode.count = lencnt;
    lencode.symbol = lensym;
    distcode.count = distcnt;
    distcode.symbol = distsym;

    for(; symbol <       144; symbol++) lengths[symbol] = 8;
    for(; symbol <       256; symbol++) lengths[symbol] = 9;
    for(; symbol <       280; symbol++) lengths[symbol] = 7;
    for(; symbol < FIXLCODES; symbol++) lengths[symbol] = 8;
    construct(&lencode, lengths, FIXLCODES);

    for(symbol = 0; symbol < MAXDCODES; symbol++) lengths[symbol] = 5;
    construct(&distcode, lengths, MAXDCODES);

    virgin = 0;
  }

  return codes(s, &lencode, &distcode);
}

inline auto dynamic(state* s) -> s32 {
  s32 nlen, ndist, ncode, index, err;
  s16 lengths[MAXCODES];
  s16 lencnt[MAXBITS + 1], lensym[MAXLCODES];
  s16 distcnt[MAXBITS + 1], distsym[MAXDCODES];
  huffman lencode, distcode;
  static const s16 order[19] = {
    16, 17, 18, 0, 8, 7, 9, 6, 10, 5, 11, 4, 12, 3, 13, 2, 14, 1, 15
  };

  lencode.count = lencnt;
  lencode.symbol = lensym;
  distcode.count = distcnt;
  distcode.symbol = distsym;

  nlen = bits(s, 5) + 257;
  ndist = bits(s, 5) + 1;
  ncode = bits(s, 4) + 4;
  if(nlen > MAXLCODES || ndist > MAXDCODES) return -3;

  for(index = 0; index < ncode; index++) lengths[order[index]] = bits(s, 3);
  for(; index < 19; index++) lengths[order[index]] = 0;

  err = construct(&lencode, lengths, 19);
  if(err != 0) return -4;

  index = 0;
  while(index < nlen + ndist) {
    s32 symbol, len;

    symbol = decode(s, &lencode);
    if(symbol < 16) {
      lengths[index++] = symbol;
    } else {
      len = 0;
      if(symbol == 16) {
        if(index == 0) return -5;
        len = lengths[index - 1];
        symbol = 3 + bits(s, 2);
      } else if(symbol == 17) {
        symbol = 3 + bits(s, 3);
      } else {
        symbol = 11 + bits(s, 7);
      }
      if(index + symbol > nlen + ndist) return -6;
      while(symbol--) lengths[index++] = len;
    }
  }

  if(lengths[256] == 0) return -9;

  err = construct(&lencode, lengths, nlen);
  if(err < 0 || (err > 0 && nlen - lencode.count[0] != 1)) return -7;

  err = construct(&distcode, lengths + nlen, ndist);
  if(err < 0 || (err > 0 && ndist - distcode.count[0] != 1)) return -8;

  return codes(s, &lencode, &distcode);
}

inline auto puff(u8* dest, u32* destlen, u8* source, u32* sourcelen) -> s32 {
  state s;
  s32 last, type, err;

  s.out = dest;
  s.outlen = *destlen;
  s.outcnt = 0;

  s.in = source;
  s.inlen = *sourcelen;
  s.incnt = 0;
  s.bitbuf = 0;
  s.bitcnt = 0;

  if(setjmp(s.env) != 0) {
    err = 2;
  } else {
    do {
      last = bits(&s, 1);
      type = bits(&s, 2);
      err = type == 0 ? stored(&s)
          : type == 1 ? fixed(&s)
          : type == 2 ? dynamic(&s)
          : -1;
      if(err != 0) break;
    } while(!last);
  }

  if(err <= 0) {
    *destlen = s.outcnt;
    *sourcelen = s.incnt;
  }

  return err;
}

}

}
