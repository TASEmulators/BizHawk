namespace Heuristics {

auto Memory::text() const -> string {
  string output;
  output.append("    memory\n");
  output.append("      type: ", _type, "\n");
  output.append("      size: 0x", hex(_size), "\n");
  output.append("      content: ", _content, "\n");
if(_manufacturer)
  output.append("      manufacturer: ", _manufacturer, "\n");
if(_architecture)
  output.append("      architecture: ", _architecture, "\n");
if(_identifier)
  output.append("      identifier: ", _identifier, "\n");
if(_volatile)
  output.append("      volatile\n");
  return output;
}

auto Oscillator::text() const -> string {
  string output;
  output.append("    oscillator\n");
  output.append("      frequency: ", _frequency, "\n");
  return output;
}

auto Slot::text() const -> string {
  string output;
  output.append("    slot\n");
  output.append("      type: ", _type, "\n");
  return output;
}

}
