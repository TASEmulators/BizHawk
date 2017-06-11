class SufamiTurbo {
public:
  struct Slot {
    MappedRAM rom;
    MappedRAM ram;
  } slotA, slotB;

  void load();
  void unload();

	SufamiTurbo();
};

extern SufamiTurbo sufamiturbo;
