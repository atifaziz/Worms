# Worms

[![Build Status][build-badge]][builds]
[![MyGet][myget-badge]][edge-pkgs]

Worms is a .NET PCL (Portable Class Library) that provides awaitable
synchronization primitives for use with code employing the [`Task`][task]
abstraction for modeling asynchronous operations.

Wait operations on all synchronization primitives support waiting with a
time-out specification as well as cancellation via a `CancellationToken`.

## Installation

*Bleeding edge* package for the [latest build][builds] can be installed
from MyGet using the following command-line:

    nuget install Worms -Pre -Source https://www.myget.org/F/raboof/api/v2

## Synchronization Primitives

### Semaphore

```c#
public class Semaphore
{
    public Semaphore();
    public Semaphore(int initialCount);

    public Task WaitAsync();
    public Task WaitAsync(CancellationToken cancellationToken);
    public Task<bool> WaitAsync(TimeSpan timeout);
    public Task<bool> WaitAsync(TimeSpan timeout, CancellationToken cancellationToken);

    public void Signal();
    public void Signal(int count);

    public int Block();
    public int Withdraw(int count);

    public int FreeCount { get; }
    public int WaitCount { get; }
}
```

In addition to waiting and signaling like in any traditional semaphore
implementation, Worm's version also supports reducing its counter via
`Withdraw` as well as flooring the count to zero via `Block`.

### AutoResetEvent

```c#
public class AutoResetEvent
{
    public AutoResetEvent();

    public Task WaitAsync();
    public Task WaitAsync(CancellationToken cancellationToken);
    public Task<bool> WaitAsync(TimeSpan timeout);
    public Task<bool> WaitAsync(TimeSpan timeout, CancellationToken cancellationToken);

    public void Set();

    public int WaitCount { get; }
    public bool IsSet { get; }
}
```


  [task]: https://msdn.microsoft.com/en-us/library/system.threading.tasks.task(v=vs.110).aspx
  [build-badge]: https://img.shields.io/appveyor/ci/raboof/worms.svg
  [myget-badge]: https://img.shields.io/myget/raboof/v/Worms.svg?label=myget
  [edge-pkgs]: https://www.myget.org/feed/raboof/package/nuget/Worms
  [builds]: https://ci.appveyor.com/project/raboof/worms
