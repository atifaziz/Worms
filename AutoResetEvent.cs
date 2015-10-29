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
    using System.Threading.Tasks;
    using AngryArrays.Push;
    using AngryArrays.Shift;
    using Interlocker;

    #endregion

    // ReSharper disable once PartialTypeWithSinglePart

    [DebuggerDisplay("IsSet = {IsSet}, WaitCount = {WaitCount}")]
    partial class AutoResetEvent
    {
        sealed class State
        {
            public readonly bool IsSet;
            public readonly TaskCompletionSource<bool>[] Waits;

            public State(bool isSet, TaskCompletionSource<bool>[] waits)
            {
                IsSet = isSet;
                Waits = waits;
            }

            public State WithWaits(TaskCompletionSource<bool>[] waits) => new State(IsSet, waits);
            public State WithSignaled(bool signaled) => new State(signaled, Waits);
            public Tuple<State, T> With<T>(T value) => Tuple.Create(this, value);
        }

        readonly Interlocked<State> _state = Interlocked.Create(new State(false, ZeroWaits));

        public int WaitCount => _state.Value.Waits.Length;
        public bool IsSet => _state.Value.IsSet;

        static readonly TaskCompletionSource<bool>[] ZeroWaits = new TaskCompletionSource<bool>[0];
        readonly static Task CompletedTask = Task.FromResult(true);

        public Task WaitAsync()
        {
            return _state.Update(s =>
            {
                TaskCompletionSource<bool> tcs;
                return !s.IsSet
                        ? s.WithWaits(s.Waits.Push(tcs = new TaskCompletionSource<bool>())).With((Task) tcs.Task)
                        : s.WithSignaled(false).With(CompletedTask);
            });
        }

        public void Set()
        {
            var wait =
                _state.Update(s => s.Waits.Length > 0
                                 ? s.Waits.Shift((w, waits) => s.WithWaits(waits).With(w))
                                 : s.WithSignaled(true).With((TaskCompletionSource<bool>) null));
            wait?.SetResult(true);
        }
    }
}
