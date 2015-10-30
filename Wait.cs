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

    #endregion

    sealed class Wait
    {
        public enum Conclusion { Signaled, TimedOut }

        readonly TaskCompletionSource<bool> _taskCompletionSource;
        CancellationTokenSource _timeoutCancellationSource;
        IDisposable _cancellationRegistration;

        public Wait(bool canTimeout)
        {
            _taskCompletionSource = new TaskCompletionSource<bool>();
            _timeoutCancellationSource = canTimeout ? new CancellationTokenSource() : null;
        }

        bool HasConcluded => _taskCompletionSource.Task.IsCompleted;

        public void TimeoutAfter(TimeSpan delay, Action<Wait> action)
        {
            if (_timeoutCancellationSource == null) throw new InvalidOperationException();

            if (delay == Timeout.InfiniteTimeSpan || HasConcluded)
                return;

            if (delay == TimeSpan.Zero)
            {
                OnTimeout(action);
                return;
            }

            _timeoutCancellationSource.Token.Register(() => OnTimeout(action));
            _timeoutCancellationSource.CancelAfter(delay);
        }

        void OnTimeout(Action<Wait> action)
        {
            if (TryConclude(Conclusion.TimedOut))
                action(this);
        }

        public void OnCancellation(CancellationToken cancellationToken, Action<Wait> action)
        {
            if (_cancellationRegistration != null)
                throw new InvalidOperationException();

            if (!cancellationToken.CanBeCanceled || HasConcluded)
                return;

            // There is a chance that while setting up the following
            // cancellation registration, the wait will already have concluded.
            // And while conclusion disposes the cancellation registration,
            // the assignment below could come late and continue to be in
            // effect (which is not ideal and should be addressed in a future
            // release). In any event, what's paramount is that no matter how
            // many times the callback will fire, the conclusion won't change.

            _cancellationRegistration = cancellationToken.Register(() =>
            {
                if (TryCancel())
                    action(this);
            }); // TODO useSynchronizationContext: false
        }

        public Task<bool> Task => _taskCompletionSource.Task;

        public bool TryConclude(Conclusion conclusion)
        {
            Debug.Assert(Enum.IsDefined(typeof(Conclusion), conclusion));
            if (!_taskCompletionSource.TrySetResult(conclusion == Conclusion.Signaled))
                return false;
            OnConcluded();
            return true;
        }

        bool TryCancel()
        {
            if (!_taskCompletionSource.TrySetCanceled())
                return false;
            OnConcluded();
            return true;
        }

        void OnConcluded()
        {
            Cleaner.Clear(ref _timeoutCancellationSource)?.Dispose();
            Cleaner.Clear(ref _cancellationRegistration)?.Dispose();
        }
    }
}