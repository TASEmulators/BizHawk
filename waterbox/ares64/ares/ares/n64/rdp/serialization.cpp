auto RDP::serialize(serializer& s) -> void {
  Thread::serialize(s);

  s(command.start);
  s(command.end);
  s(command.current);
  s(command.clock);
  s(command.bufferBusy);
  s(command.pipeBusy);
  s(command.tmemBusy);
  s(command.source);
  s(command.freeze);
  s(command.flush);
  s(command.ready);

  s(io.bist.check);
  s(io.bist.go);
  s(io.bist.done);
  s(io.bist.fail);

  s(io.test.enable);
  s(io.test.address);
  s(io.test.data);
}
