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
    using System.Threading;
    using System.Threading.Tasks;

    #endregion

    sealed class Wait
    {
        public static readonly Task<bool> SucceededTask = System.Threading.Tasks.Task.FromResult(true);

        readonly TaskCompletionSource<bool> _taskCompletionSource;
        readonly TimeSpan? _timeout;
        CancellationTokenSource _timeoutCancellationSource;
        IDisposable _cancellationRegistration;

        public Wait() : this(null) {}
        public Wait(TimeSpan timeout) :
            this(timeout != Timeout.InfiniteTimeSpan ? timeout : (TimeSpan?) null) { }

        Wait(TimeSpan? timeout)
        {
            if (timeout?.Ticks < 0) throw new ArgumentOutOfRangeException(nameof(timeout));

            _taskCompletionSource      = new TaskCompletionSource<bool>();
            _timeout                   = timeout;
            _timeoutCancellationSource = timeout?.Ticks >= 0
                                       ? new CancellationTokenSource()
                                       : null;
        }

        bool HasConcluded => _taskCompletionSource.Task.IsCompleted;

        public void OnTimeout(Action<Wait> action) =>
            OnTimeout(action, false);

        public void OnTimeout(Action<Wait> action, bool useSynchronizationContext)
        {
            if (_timeout == null || HasConcluded)
                return;

            var timeout = _timeout.Value;
            if (timeout == TimeSpan.Zero)
            {
                OnTimedOut(action);
                return;
            }

            _timeoutCancellationSource.Token.Register(() => OnTimedOut(action), useSynchronizationContext);
            _timeoutCancellationSource.CancelAfter(timeout);
        }

        void OnTimedOut(Action<Wait> action)
        {
            if (TryConcludeAsSignaled(false))
                action(this);
        }

        public void OnCancellation(CancellationToken cancellationToken, Action<Wait> action) =>
            OnCancellation(cancellationToken, action, false);

        public void OnCancellation(CancellationToken cancellationToken,
                                   Action<Wait> action,
                                   bool useSynchronizationContext)
        {
            if (_cancellationRegistration != null)
                throw new InvalidOperationException();

            if (!cancellationToken.CanBeCanceled || HasConcluded)
                return;

            if (cancellationToken.IsCancellationRequested)
            {
                OnCanceled(action);
                return;
            }

            // There is a chance that while setting up the following
            // cancellation registration, the wait will already have concluded.
            // And while conclusion disposes the cancellation registration,
            // the assignment below could come late and continue to be in
            // effect (which is not ideal and should be addressed in a future
            // release). In any event, what's paramount is that no matter how
            // many times the callback will fire, the conclusion won't change.

            _cancellationRegistration = cancellationToken.Register(() => OnCanceled(action), useSynchronizationContext);
        }

        void OnCanceled(Action<Wait> action)
        {
            if (TryCancel())
                action(this);
        }

        public Task<bool> Task => _taskCompletionSource.Task;

        public bool TrySignal() => TryConcludeAsSignaled(true);

        bool TryConcludeAsSignaled(bool signaled)
        {
            if (!_taskCompletionSource.TrySetResult(signaled))
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

        public static void ValidateTimeoutArgument(TimeSpan timeout, string paramName = null)
        {
            if (timeout == Timeout.InfiniteTimeSpan || timeout.Ticks >= 0)
                return;
            throw new ArgumentOutOfRangeException(paramName ?? nameof(timeout));
        }
   }
}