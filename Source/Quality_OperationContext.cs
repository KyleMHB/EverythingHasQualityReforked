using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace ConfigurableQuality
{
    // Only MoveNext/Dispose execute inside the operation: a suspended iterator must not
    // leave its recipe inputs installed while another recipe is enumerated.
    internal sealed class QualityOperationContext<T>
    {
        private readonly T fallback;
        private readonly ThreadLocal<Stack<T>> values = new ThreadLocal<Stack<T>>(() => new Stack<T>());
        public QualityOperationContext(T fallback) { this.fallback = fallback; }
        public T Current => values.Value.Count > 0 ? values.Value.Peek() : fallback;
        public IEnumerable<TItem> Wrap<TItem>(IEnumerable<TItem> source, T value) => new Sequence<TItem>(this, source, value);
        private TResult Run<TResult>(T value, Func<TResult> action)
        {
            values.Value.Push(value);
            try { return action(); }
            finally { values.Value.Pop(); }
        }
        private sealed class Sequence<TItem> : IEnumerable<TItem>
        {
            private readonly QualityOperationContext<T> owner;
            private readonly IEnumerable<TItem> source;
            private readonly T value;
            public Sequence(QualityOperationContext<T> owner, IEnumerable<TItem> source, T value)
            { this.owner = owner; this.source = source; this.value = value; }
            public IEnumerator<TItem> GetEnumerator() => owner.Run(value, () => new Enumerator(owner, source.GetEnumerator(), value));
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            private sealed class Enumerator : IEnumerator<TItem>
            {
                private readonly QualityOperationContext<T> owner;
                private readonly IEnumerator<TItem> source;
                private readonly T value;
                public Enumerator(QualityOperationContext<T> owner, IEnumerator<TItem> source, T value)
                { this.owner = owner; this.source = source; this.value = value; }
                public TItem Current => source.Current;
                object IEnumerator.Current => Current;
                public bool MoveNext() => owner.Run(value, source.MoveNext);
                public void Reset() { owner.Run(value, () => { source.Reset(); return true; }); }
                public void Dispose() { owner.Run(value, () => { source.Dispose(); return true; }); }
            }
        }
    }
}
