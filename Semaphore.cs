#region Copyright (c) 2015 Atif Aziz. All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
#endregion

namespace Worms
{
    #region Imports

    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using AngryArrays;
    using Interlocker;
    using Mannex;
    using Interlocked = Interlocker.Interlocked;

    #endregion

    [DebuggerDisplay("Count = {FreeCount}, WaitCount = {WaitCount}")]
    public class Semaphore : IDisposable
    {
        sealed class State
        {
            public readonly int Count;
            public readonly Wait[] Waits;

            public State(int count = 0, Wait[] waits = null)
            {
                if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
                Count = count;
                Waits = waits ?? EmptyArray<Wait>.Value;
            }

            public State Adjust(int by = 0, Wait[] waits = null) =>
                new State(Count + by, waits ?? Waits);
            public State DecrementCount(int by = 1) => Adjust(-by);
            public State WithWaits(Wait[] waits) => Adjust(0, waits);
        }

        readonly Interlocked<State> _state;

        public Semaphore() : this(0) {}

        public Semaphore(int initialCount)
        {
            if (initialCount < 0) throw new ArgumentOutOfRangeException(nameof(initialCount));
            _state = Interlocked.Create(new State(initialCount));
        }

        public int FreeCount => _state.Value.Count;
        public int WaitCount => _state.Value.Waits.Length;

        public Task WaitAsync() =>
            WaitAsync(Timeout.InfiniteTimeSpan, CancellationToken.None);

        public Task<bool> WaitAsync(TimeSpan timeout) => WaitAsync(timeout, CancellationToken.None);

        public Task<bool> WaitAsync(CancellationToken cancellationToken) =>
            WaitAsync(Timeout.InfiniteTimeSpan, cancellationToken);

        public Task<bool> WaitAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            var isTimeoutFinitie = timeout != Timeout.InfiniteTimeSpan;
            if (timeout.Ticks < 0 && isTimeoutFinitie)
                throw new ArgumentOutOfRangeException(nameof(timeout), timeout, null);

            var result = _state.Update(state =>
            {
                if (state.Count > 0)
                {
                    return Tuple.Create(state.DecrementCount(), new
                    {
                        Wait  = (Wait) null,
                        Task  = Task.FromResult(true),
                    });
                }   // ReSharper disable once RedundantIfElseBlock
                else
                {
                    var tcs = new TaskCompletionSource<bool>();
                    var wait = new Wait(tcs, isTimeoutFinitie && timeout != TimeSpan.Zero);
                    return Tuple.Create(state.WithWaits(state.Waits.Concat(new[] { wait }).ToArray()), new
                    {
                        Wait = wait,
                        tcs.Task,
                    });
                }
            });

            {
                var wait = result.Wait;
                if (wait != null)
                {
                    if (isTimeoutFinitie)
                    {
                        if (timeout == TimeSpan.Zero)
                        {
                            if (wait.TryConclude(Wait.Conclusion.TimedOut))
                                TryRemoveWait(wait);
                        }

                        WaitTimeout(wait, timeout,
                                    cancellationToken.CanBeCanceled
                                    ? CancellationTokenSource.CreateLinkedTokenSource(wait.TimeoutCancellationToken, cancellationToken).Token
                                    : wait.TimeoutCancellationToken)
                            .IgnoreFault();
                    }

                    if (cancellationToken.CanBeCanceled)
                        wait.OnCancellation(cancellationToken, TryRemoveWait);
                }
            }

            return result.Task;
        }

        async Task WaitTimeout(Wait wait, TimeSpan timeout, CancellationToken cancellationToken)
        {
            await Task.Delay(timeout, cancellationToken).ConfigureAwait(false);
            if (wait.TryConclude(Wait.Conclusion.TimedOut))
                TryRemoveWait(wait);
        }

        void TryRemoveWait(Wait wait) =>
            _state.Update(state => state.WithWaits(state.Waits.TryRemove(wait)));

        public void Signal() => Signal(1);

        public void Signal(int count)
        {
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
            if (count == 0)
                return;

            var waits = _state.Update(state =>
            {
                var leftover = Math.Max(count - state.Waits.Length, 0);
                var parts = state.Waits.Partition(count, (head, tail) => new
                {
                    Completed  = head,
                    Incomplete = tail,
                });
                return Tuple.Create(state.Adjust(leftover, parts.Incomplete),
                                    parts.Completed);
            });

            foreach (var wait in waits)
                wait.TryConclude(Wait.Conclusion.Signaled);
        }

        public int Block() => Withdraw(int.MaxValue);

        public int Withdraw(int count) =>
            _state.Update(state =>
            {
                var adjusted = Math.Min(count, state.Count);
                return Tuple.Create(state = state.DecrementCount(adjusted),
                                    state.Count);
            });

        void IDisposable.Dispose() { /* NOP */ }
    }
}
