auto OBC1::serialize(serializer& s) -> void {
  s.array(ram.data(), ram.size());

  s.integer(status.address);
  s.integer(status.baseptr);
  s.integer(status.shift);
}
