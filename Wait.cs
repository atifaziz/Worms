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
        CancellationTokenSource _timeoutCancellation;
        IDisposable _cancellationRegistration;

        public Wait(bool canTimeout)
        {
            _taskCompletionSource = new TaskCompletionSource<bool>();
            _timeoutCancellation = canTimeout ? new CancellationTokenSource() : null;
        }

        public void OnCancellation(CancellationToken cancellationToken, Action<Wait> action)
        {
            if (_cancellationRegistration != null)
                throw new InvalidOperationException();

            if (!cancellationToken.CanBeCanceled)
                return;

            _cancellationRegistration = cancellationToken.Register(() =>
            {
                if (TryCancel())
                    action(this);
            });
        }

        public Task<bool> Task => _taskCompletionSource.Task;

        public CancellationToken TimeoutCancellationToken =>
            _timeoutCancellation?.Token ?? CancellationToken.None;

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
            Cleaner.Clear(ref _cancellationRegistration)?.Dispose();
            var timeoutCancellation = Cleaner.Clear(ref _timeoutCancellation);
            timeoutCancellation?.Cancel();
            timeoutCancellation?.Dispose();
        }
    }
}