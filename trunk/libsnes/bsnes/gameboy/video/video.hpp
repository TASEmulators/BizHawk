struct Video {
  enum class Format : unsigned { RGB30, RGB24, RGB16, RGB15 };
  unsigned *palette;

  unsigned palette_dmg(unsigned color) const;
  unsigned palette_sgb(unsigned color) const;
  unsigned palette_cgb(unsigned color) const;

  void generate(Format format);
  Video();
  ~Video();

private:
  static const double monochrome[4][3];
};

extern Video video;
