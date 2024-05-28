struct Thread;

struct Scheduler {
  enum class Mode : u32 {
    Run,
    Synchronize,
    SynchronizePrimary,
    SynchronizeAuxiliary,
  };

  Scheduler() = default;
  Scheduler(const Scheduler&) = delete;
  auto operator=(const Scheduler&) = delete;

  auto reset() -> void;
  auto threads() const -> u32;
  auto thread(u32 threadID) const -> maybe<Thread&>;
  auto uniqueID() const -> u32;
  auto minimum() const -> u64;
  auto maximum() const -> u64;

  auto append(Thread& thread) -> bool;
  auto remove(Thread& thread) -> void;

  auto power(Thread& thread) -> void;
  auto enter(Mode mode = Mode::Run) -> Event;
  auto exit(Event event) -> void;

  auto synchronizing() const -> bool;
  auto synchronize() -> void;

  auto getSynchronize() -> bool;
  auto setSynchronize(bool) -> void;

private:
  cothread_t _host = nullptr;     //program thread (used to exit scheduler)
  cothread_t _resume = nullptr;   //resume thread (used to enter scheduler)
  cothread_t _primary = nullptr;  //primary thread (used to synchronize components)
  Mode _mode = Mode::Run;
  Event _event = Event::Step;
  vector<Thread*> _threads;
  bool _synchronize = false;

  friend struct Thread;
};

extern Scheduler scheduler;
