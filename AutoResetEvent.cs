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
    using System.Threading;
    using System.Threading.Tasks;
    using AngryArrays.Push;
    using AngryArrays.Shift;
    using Interlocker;
    using Interlocked = Interlocker.Interlocked;

    #endregion

    [DebuggerDisplay("IsSet = {IsSet}, WaitCount = {WaitCount}")]
    public class AutoResetEvent
    {
        sealed class State
        {
            public readonly bool IsSet;
            public readonly Wait[] Waits;

            public State(bool isSet, Wait[] waits)
            {
                IsSet = isSet;
                Waits = waits;
            }

            public State WithWaits(Wait[] waits) => new State(IsSet, waits);
            public State WithSignaled(bool signaled) => new State(signaled, Waits);
            public Tuple<State, T> With<T>(T value) => Tuple.Create(this, value);
        }

        readonly Interlocked<State> _state = Interlocked.Create(new State(false, ZeroWaits));

        public int WaitCount => _state.Value.Waits.Length;
        public bool IsSet => _state.Value.IsSet;

        static readonly Wait[] ZeroWaits = new Wait[0];

        public Task<bool> WaitAsync() => WaitAsync(CancellationToken.None);

        public Task<bool> WaitAsync(CancellationToken cancellationToken) =>
            WaitAsync(Timeout.InfiniteTimeSpan, cancellationToken);

        public Task<bool> WaitAsync(TimeSpan timeout) => WaitAsync(timeout, CancellationToken.None);

        public Task<bool> WaitAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            Wait.ValidateTimeoutArgument(timeout);

            var wait = _state.Update(s =>
            {
                Wait w;
                return !s.IsSet
                     ? s.WithWaits(s.Waits.Push(w = new Wait(timeout))).With(w)
                     : s.WithSignaled(false).With((Wait) null);
            });

            if (wait == null)
                return Wait.SucceededTask;

            if (cancellationToken.CanBeCanceled)
                wait.OnCancellation(cancellationToken, TryRemoveWait);

            wait.OnTimeout(TryRemoveWait);

            return wait.Task;
        }

        void TryRemoveWait(Wait wait) =>
            _state.Update(state => state.WithWaits(state.Waits.TryRemove(wait)));

        public void Set()
        {
            var wait =
                _state.Update(s => s.Waits.Length > 0
                                 ? s.Waits.Shift((w, waits) => s.WithWaits(waits).With(w))
                                 : s.WithSignaled(true).With((Wait) null));
            wait?.TrySignal();
        }
    }
}
