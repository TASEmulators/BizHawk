#pragma once

#include <nall/beat/archive/node.hpp>

namespace nall::Beat::Archive {

struct Container {
  Container(array_view<uint8_t> = {});
  ~Container();

  auto isCompressed() const -> bool { return (bool)compression.type; }
  auto isSigned() const -> bool { return (bool)signature.type; }
  auto isEncrypted() const -> bool { return (bool)encryption.type; }

  auto compressLZSA() -> void;
  auto signEd25519(uint256_t privateKey) -> void;
  auto encryptXChaCha20(uint256_t privateKey, uint192_t nonce = 0) -> void;

  auto validate() -> bool;
  auto decryptXChaCha20(uint256_t privateKey) -> bool;
  auto verifyEd25519(uint256_t publicKey) -> bool;
  auto decompressLZSA() -> bool;

  auto append(string name, string location) -> shared_pointer<Node>;
  auto appendPath(string name) -> shared_pointer<Node>;
  auto appendFile(string name, array_view<uint8_t> memory) -> shared_pointer<Node>;
  auto remove(string name) -> bool;
  auto find(string name) -> shared_pointer<Node>;
  auto sort() -> void;

  auto begin() { return nodes.begin(); }
  auto end() { return nodes.end(); }

  auto begin() const { return nodes.begin(); }
  auto end() const { return nodes.end(); }

  auto rbegin() { return nodes.rbegin(); }
  auto rend() { return nodes.rend(); }

  auto rbegin() const { return nodes.rbegin(); }
  auto rend() const { return nodes.rend(); }

  vector<shared_pointer<Node>> nodes;
  vector<uint8_t> memory;
  string metadata;

  struct Compression {
    string type;
  } compression;

  struct Signature {
    string type;
    uint256_t privateKey = 0;
    uint256_t publicKey = 0;
    uint512_t value = 0;
  } signature;

  struct Encryption {
    string type;
    uint256_t privateKey = 0;
    uint192_t nonce = 0;
  } encryption;
};

Container::Container(array_view<uint8_t> memory) {
  this->memory.resize(memory.size());
  nall::memory::copy(this->memory.data(), memory.data(), memory.size());
}

Container::~Container() {
  metadata = {};
  signature = {};
  encryption = {};
}

//

auto Container::compressLZSA() -> void {
  compression.type = "lzsa";
}

auto Container::signEd25519(uint256_t privateKey) -> void {
  signature.type = "ed25519";
  signature.privateKey = privateKey;
}

auto Container::encryptXChaCha20(uint256_t privateKey, uint192_t nonce) -> void {
  if(!nonce) {
    CSPRNG::XChaCha20 csprng;
    nonce = csprng.random<uint192_t>();
  }

  encryption.type = "xchacha20";
  encryption.privateKey = privateKey;
  encryption.nonce = nonce;
}

//

auto Container::validate() -> bool {
  array_view<uint8_t> memory = this->memory;
  if(memory.size() < 44) return false;  //8 (metadata size) + 32 (SHA256) + 4 (signature)

  if(memory[memory.size() - 4] != 'B') return false;
  if(memory[memory.size() - 3] != 'P') return false;
  if(memory[memory.size() - 2] != 'A') return false;
  if(memory[memory.size() - 1] != '1') return false;

  auto sha256 = memory.readl<uint256_t>(memory.size() - 36, 32);
  if(Hash::SHA256({memory.data(), memory.size() - 36}).value() != sha256) return false;

  auto size = memory.readl<uint64_t>(memory.size() - 44, 8);

  if(size & 1ull << 63) {
    size -= 1ull << 63;
    metadata = memory.view(memory.size() - 44 - size, size);
    uint64_t offset = memory.size() - 44 - size;
    for(auto& byte : metadata) byte ^= offset++;
  } else {
    metadata = memory.view(memory.size() - 44 - size, size);
  }

  auto document = BML::unserialize(metadata);

  if(auto node = document["archive/encryption"]) {
    if(node.text() == "xchacha20") {
      encryption.type = node.text();
      encryption.nonce = Decode::Base<57, uint192_t>(node["nonce"].text());
    }
  }

  if(auto node = document["archive/signature"]) {
    if(node.text() == "ed25519") {
      signature.type = node.text();
      signature.publicKey = Decode::Base<57, uint256_t>(node["publicKey"].text());
      signature.value = Decode::Base<57, uint512_t>(node["value"].text());
    }
  }

  if(auto node = document["archive/compression"]) {
    compression.type = node.text();
  }

  return true;
}

auto Container::decryptXChaCha20(uint256_t privateKey) -> bool {
  encryption.privateKey = privateKey;
  Cipher::XChaCha20 xchacha20{encryption.privateKey, encryption.nonce};
  auto size = memory.readl<uint64_t>(memory.size() - 44, 8);
  memory = xchacha20.decrypt(memory.view(0, memory.size() - 44 - size));
  return true;
}

auto Container::verifyEd25519(uint256_t publicKey) -> bool {
  EllipticCurve::Ed25519 ed25519;
  auto size = memory.readl<uint64_t>(memory.size() - 44, 8);
  return ed25519.verify(memory.view(0, memory.size() - 44 - size), signature.value, publicKey);
}

auto Container::decompressLZSA() -> bool {
  memory = Decode::LZSA(memory);
  return (bool)memory;
}

//

auto Container::append(string name, string location) -> shared_pointer<Node> {
  for(auto& node : nodes) if(node->name == name) return {};
  if(auto node = Node::create(name, location)) return nodes.append(node), node;
  return {};
}

auto Container::appendPath(string name) -> shared_pointer<Node> {
  for(auto& node : nodes) if(node->name == name) return {};
  if(auto node = Node::createPath(name)) return nodes.append(node), node;
  return {};
}

auto Container::appendFile(string name, array_view<uint8_t> memory) -> shared_pointer<Node> {
  for(auto& node : nodes) if(node->name == name) return {};
  if(auto node = Node::createFile(name, memory)) return nodes.append(node), node;
  return {};
}

auto Container::remove(string name) -> bool {
  if(auto offset = nodes.find([&](auto& node) { return node->name == name; })) return nodes.remove(*offset), true;
  return false;
}

auto Container::find(string name) -> shared_pointer<Node> {
  if(auto offset = nodes.find([&](auto& node) { return node->name == name; })) return nodes[*offset];
  return {};
}

auto Container::sort() -> void {
  nodes.sort([&](auto& lhs, auto& rhs) { return string::icompare(lhs->name, rhs->name) < 0; });
}

}
