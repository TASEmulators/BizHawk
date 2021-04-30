namespace Heuristics {

struct BSMemory {
  BSMemory(vector<uint8_t>& data, string location);
  explicit operator bool() const;
  auto manifest() const -> string;

private:
  vector<uint8_t>& data;
  string location;
};

BSMemory::BSMemory(vector<uint8_t>& data, string location) : data(data), location(location) {
}

BSMemory::operator bool() const {
  return data.size() >= 0x8000;
}

auto BSMemory::manifest() const -> string {
  if(!operator bool()) return {};

  string output;
  output.append("game\n");
  output.append("  sha256: ", Hash::SHA256(data).digest(), "\n");
  output.append("  label:  ", Location::prefix(location), "\n");
  output.append("  name:   ", Location::prefix(location), "\n");
  output.append("  board\n");
  output.append(Memory{}.type("Flash").size(data.size()).content("Program").text());
  return output;
}

}
