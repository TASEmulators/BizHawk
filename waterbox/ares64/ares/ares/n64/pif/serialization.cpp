auto PIF::serialize(serializer& s) -> void {
  s(ram);

  s(io.romLockout);
}
