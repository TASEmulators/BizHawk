auto MSU1::serialize(serializer& s) -> void {
  Thread::serialize(s);

  s.integer(io.dataSeekOffset);
  s.integer(io.dataReadOffset);

  s.integer(io.audioPlayOffset);
  s.integer(io.audioLoopOffset);

  s.integer(io.audioTrack);
  s.integer(io.audioVolume);

  s.integer(io.audioResumeTrack);
  s.integer(io.audioResumeOffset);

  s.boolean(io.audioError);
  s.boolean(io.audioPlay);
  s.boolean(io.audioRepeat);
  s.boolean(io.audioBusy);
  s.boolean(io.dataBusy);

  dataOpen();
  audioOpen();
}
