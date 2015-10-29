#region License, Terms and Author(s)
//
// Mannex - Extension methods for .NET
// Copyright (c) 2009 Atif Aziz. All rights reserved.
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

// ReSharper disable PartialTypeWithSinglePart

namespace Mannex
{
    using System;

    /// <summary>
    /// Extension methods for <see cref="Delegate"/>.
    /// </summary>

    static partial class DelegateExtensions
    {
        /// <summary>
        /// Sequentially invokes each delegate in the invocation list as
        /// <see cref="EventHandler{TEventArgs}"/> and ignores exceptions
        /// thrown during the invocation of any one handler (continuing
        /// with the next handler in the list).
        /// </summary>

        public static void InvokeAsEventHandlerWhileIgnoringErrors<T>(this Delegate del, object sender, T args)
            where T : EventArgs
        {
            if (del == null) throw new ArgumentNullException("del");
            // ReSharper disable once PossibleInvalidCastExceptionInForeachLoop
            foreach (EventHandler<T> handler in del.GetInvocationList())
                try { handler(sender, args); } catch { /* ignored */ }
        }
    }
}
namespace Mannex
{
    using System;

    /// <summary>
    /// Extension methods for <see cref="Array"/> sub-types.
    /// </summary>

    static partial class ArrayExtensions
    {
        static class Empty<T>
        {
            public static readonly T[] Value = new T[0];
        }

        /// <summary>
        /// Partitions the array in two parts at the given index where the
        /// first part contains items up to (excluding) the index and the
        /// second contains items from the index and onward.
        /// </summary>

        public static TResult Partition<T, TResult>(this T[] array, int index,
            Func<T[], T[], TResult> selector)
        {
            if (array == null) throw new ArgumentNullException("array");
            if (selector == null) throw new ArgumentNullException("selector");
            if (index < 0) throw new ArgumentOutOfRangeException("index", index, null);

            index = Math.Min(index, array.Length);
            var left = index > 0 ? new T[index] : Empty<T>.Value;
            if (left.Length > 0)
                Array.Copy(array, 0, left, 0, left.Length);
            var rightCount = array.Length - index;
            var right = index + rightCount <= array.Length ? new T[rightCount] : Empty<T>.Value;
            if (right.Length > 0)
                Array.Copy(array, index, right, 0, right.Length);
            return selector(left, right);
        }
    }
}