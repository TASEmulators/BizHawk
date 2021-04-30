#pragma once

#include <nall/beat/archive/node.hpp>
#include <nall/beat/archive/container.hpp>

namespace nall::Beat::Archive {

auto create(Container& container, string name) -> vector<uint8_t> {
  auto& metadata = container.metadata;
  metadata = {};
  metadata.append("archive: ", Location::file(name), "\n");

  vector<uint8_t> memory;

  container.sort();
  for(auto& node : container.nodes) {
    if(node->isFile()) {
      node->offset = memory.size();
      memory.append(node->memory);
    }
    metadata.append(node->metadata());
  }

  metadata.append("  size: ", memory.size(), "\n");

  if(container.compression.type == "lzsa") {
    memory = Encode::LZSA(memory);
    metadata.append("  compression: lzsa\n");
    metadata.append("    size: ", memory.size(), "\n");
  }

  if(container.signature.type == "ed25519") {
    EllipticCurve::Ed25519 ed25519;
    container.signature.publicKey = ed25519.publicKey(container.signature.privateKey);
    container.signature.value = ed25519.sign(memory, container.signature.privateKey);

    metadata.append("  signature: ed25519\n");
    metadata.append("    publicKey: ", Encode::Base<57>(container.signature.publicKey), "\n");
    metadata.append("    value: ", Encode::Base<57>(container.signature.value), "\n");
  }

  for(auto& byte : metadata) memory.append(byte);
  memory.appendl((uint64_t)metadata.size(), 8);

  auto sha256 = Hash::SHA256(memory).value();
  memory.appendl((uint256_t)sha256, 32);

  memory.append('B');
  memory.append('P');
  memory.append('A');
  memory.append('1');

  if(container.encryption.type == "xchacha20") {
    Cipher::XChaCha20 xchacha20{container.encryption.privateKey, container.encryption.nonce};
    memory = xchacha20.encrypt(memory);

    metadata = {};
    metadata.append("archive\n");
    metadata.append("  encryption: xchacha20\n");
    metadata.append("    nonce: ", Encode::Base<57>(container.encryption.nonce), "\n");

    if(container.signature.type == "ed25519") {
      EllipticCurve::Ed25519 ed25519;
      container.signature.value = ed25519.sign(memory, container.signature.privateKey);

      metadata.append("  signature: ed25519\n");
    //metadata.append("    publicKey: ", Encode::Base<57>(container.signature.publicKey), "\n");
      metadata.append("    value: ", Encode::Base<57>(container.signature.value), "\n");
    }

    for(auto& byte : metadata) memory.append(byte ^ memory.size());
    memory.appendl((uint64_t)metadata.size() | 1ull << 63, 8);

    auto sha256 = Hash::SHA256(memory).value();
    memory.appendl((uint256_t)sha256, 32);

    memory.append('B');
    memory.append('P');
    memory.append('A');
    memory.append('1');
  }

  return memory;
}

}
