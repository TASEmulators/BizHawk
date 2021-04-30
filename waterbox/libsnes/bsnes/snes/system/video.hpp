struct Video {
  enum class Format : unsigned { RGB30, RGB24, RGB16, RGB15 };
public:
	void update();

private:
  bool hires;
  unsigned line_width[240];

  void scanline();
  void init();

  static const uint8_t cursor[15 * 15];
  void draw_cursor(uint16_t color, int x, int y);

  friend class System;
};

extern Video video;
