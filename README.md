# Worms

![Build Status](https://img.shields.io/appveyor/ci/raboof/worms.svg)
![MyGet](https://img.shields.io/myget/raboof/v/Worms.svg?label=myget)

Worms is a .NET PCL (Portable Class Library) that provides awaitable
synchronization primitives for use with code employing the [`Task`][task]
abstraction for modeling asynchronous operations.

Wait operations on all synchronization primitives support waiting with a
time-out specification as well as cancellation via a `CancellationToken`.

## Semaphore

```c#
public class Semaphore
{
    public Semaphore();
    public Semaphore(int initialCount);

    public Task WaitAsync();
    public Task<bool> WaitAsync(TimeSpan timeout);
    public Task<bool> WaitAsync(CancellationToken cancellationToken);
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

## AutoResetEvent

```c#
public class AutoResetEvent
{
    public AutoResetEvent();

    public Task<bool> WaitAsync();
    public Task<bool> WaitAsync(CancellationToken cancellationToken);
    public Task<bool> WaitAsync(TimeSpan timeout);
    public Task<bool> WaitAsync(TimeSpan timeout, CancellationToken cancellationToken);

    public void Set();

    public int WaitCount { get; }
    public bool IsSet { get; }
}
```


  [task]: https://msdn.microsoft.com/en-us/library/system.threading.tasks.task(v=vs.110).aspx
