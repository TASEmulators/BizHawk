auto RI::serialize(serializer& s) -> void {
  s(io.mode);
  s(io.config);
  s(io.currentLoad);
  s(io.select);
  s(io.refresh);
  s(io.latency);
  s(io.readError);
  s(io.writeError);
}
