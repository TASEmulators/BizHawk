inline auto Scheduler::reset() -> void {
  _threads.reset();
}

inline auto Scheduler::threads() const -> u32 {
  return _threads.size();
}

inline auto Scheduler::thread(u32 uniqueID) const -> maybe<Thread&> {
  for(auto& thread : _threads) {
    if(thread->_uniqueID == uniqueID) return *thread;
  }
  return {};
}

//if threads A and B both have a clock value of 0, it is ambiguous which should run first.
//to resolve this, a uniqueID is assigned to each thread when appended to the scheduler.
//the first unused ID is selected, to avoid the uniqueID growing in an unbounded fashion.
inline auto Scheduler::uniqueID() const -> u32 {
  u32 uniqueID = 0;
  while(thread(uniqueID)) uniqueID++;
  return uniqueID;
}

//find the clock time of the furthest behind thread.
inline auto Scheduler::minimum() const -> u64 {
  u64 minimum = (u64)-1;
  for(auto& thread : _threads) {
    minimum = min(minimum, thread->_clock - thread->_uniqueID);
  }
  return minimum;
}

//find the clock time of the furthest ahead thread.
inline auto Scheduler::maximum() const -> u64 {
  u64 maximum = 0;
  for(auto& thread : _threads) {
    maximum = max(maximum, thread->_clock - thread->_uniqueID);
  }
  return maximum;
}

inline auto Scheduler::append(Thread& thread) -> bool {
  if(_threads.find(&thread)) return false;
  thread._uniqueID = uniqueID();
  thread._clock = maximum() + thread._uniqueID;
  _threads.append(&thread);
  return true;
}

inline auto Scheduler::remove(Thread& thread) -> void {
  _threads.removeByValue(&thread);
}

//power cycle and soft reset events: assigns the primary thread and resets all thread clocks.
inline auto Scheduler::power(Thread& thread) -> void {
  _primary = _resume = thread.handle();
  for(auto& thread : _threads) {
    thread->_clock = thread->_uniqueID;
  }
}

inline auto Scheduler::enter(Mode mode) -> Event {
  if(mode == Mode::Run) {
    _mode = mode;
    _host = co_active();
    co_switch(_resume);
    platform->event(_event);
    return _event;
  }

  if(mode == Mode::Synchronize) {
    //run all threads to safe points, starting with the primary thread.
    for(auto& thread : _threads) {
      if(thread->handle() == _primary) {
        _mode = Mode::SynchronizePrimary;
        _host = co_active();
        do {
          co_switch(_resume);
          platform->event(_event);
        } while(_event != Event::Synchronize);
      }
    }
    for(auto& thread : _threads) {
      if(thread->handle() != _primary) {
        _mode = Mode::SynchronizeAuxiliary;
        _host = co_active();
        _resume = thread->handle();
        do {
          co_switch(_resume);
          platform->event(_event);
        } while(_event != Event::Synchronize);
      }
    }
    return Event::Synchronize;
  }

  return Event::None;
}

inline auto Scheduler::exit(Event event) -> void {
  //subtract the minimum time from all threads to prevent clock overflow.
  auto reduce = minimum();
  for(auto& thread : _threads) {
    thread->_clock -= reduce;
  }

  //return to the thread that entered the scheduler originally.
  _event = event;
  _resume = co_active();
  co_switch(_host);
}

//used to prevent auxiliary threads from blocking during synchronization.
//for instance, a secondary CPU waiting on an interrupt from the primary CPU.
//as other threads are not run during synchronization, this would otherwise cause a deadlock.
inline auto Scheduler::synchronizing() const -> bool {
  return _mode == Mode::SynchronizeAuxiliary;
}

//marks a safe point (typically the beginning of the entry point) of a thread.
//the scheduler may exit at these points for the purpose of synchronization.
inline auto Scheduler::synchronize() -> void {
  if(co_active() == _primary) {
    if(_mode == Mode::SynchronizePrimary) return exit(Event::Synchronize);
  } else {
    if(_mode == Mode::SynchronizeAuxiliary) return exit(Event::Synchronize);
  }
}

inline auto Scheduler::getSynchronize() -> bool {
  return _synchronize;
}

inline auto Scheduler::setSynchronize(bool synchronize) -> void {
  _synchronize = synchronize;
}
