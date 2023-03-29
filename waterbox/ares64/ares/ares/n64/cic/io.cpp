
auto CIC::writeBit(n1 data) -> void {
  fifo.write(data);
  poll();
}

auto CIC::writeNibble(n4 data) -> void {
  fifo.writeNibble(data);
  poll();
}

auto CIC::readBit() -> n1 {
  if(fifo.empty()) cic.poll();
  return fifo.read();
}

auto CIC::readNibble() -> n4 {
  if (fifo.empty()) cic.poll();
  return fifo.readNibble();
}

