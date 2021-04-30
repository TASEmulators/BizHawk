#pragma once

#include <nall/beat/archive/node.hpp>
#include <nall/beat/archive/container.hpp>

namespace nall::Beat::Archive {

auto extract(Container& container) -> bool {
  function<void (Markup::Node)> extract = [&](auto metadata) {
    if(metadata.name() != "path" && metadata.name() != "file") return;
    shared_pointer<Node> node = new Node;
    if(node->unserialize(container.memory, metadata)) {
      container.nodes.append(node);
    }
    if(metadata.name() != "path") return;
    for(auto node : metadata) extract(node);
  };

  container.nodes.reset();
  auto document = BML::unserialize(container.metadata);
  for(auto node : document["archive"]) extract(node);
  container.sort();

  return true;
}

}
