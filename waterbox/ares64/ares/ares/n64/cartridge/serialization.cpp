auto Cartridge::serialize(serializer& s) -> void {
  s(ram);
  s(eeprom);
  s(flash);
  s(rtc);
  s(sc64);
}
