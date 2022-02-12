auto SM5K::fetch() -> n8 {
  timerStep();
  return ROM[PC++];
}
