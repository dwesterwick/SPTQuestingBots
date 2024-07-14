using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPTQuestingBots.Models
{
    public class EnumeratorCollection : IEnumerator, IDisposable, IEnumerator<object>
    {
        private object modifiedEnumerator = null;
        private IEnumerator originalEnumerator = null;
        private IEnumerator prefixEnumerator = null;
        private IEnumerator suffixEnumerator = null;

        public EnumeratorCollection(IEnumerator original, IEnumerator prefix = null, IEnumerator suffix = null)
        {
            originalEnumerator = original;
            prefixEnumerator = prefix;
            suffixEnumerator = suffix;
        }

        object IEnumerator<object>.Current { get { return modifiedEnumerator; } }
        object IEnumerator.Current { get { return modifiedEnumerator; } }

        bool IEnumerator.MoveNext() { return MoveNext(); }
        public bool MoveNext()
        {
            if (modifiedEnumerator != null)
            {
                return false;
            }

            modifiedEnumerator = combineEnumerators();
            return true;
        }

        private IEnumerator combineEnumerators()
        {
            yield return prefixEnumerator;
            yield return originalEnumerator;
            yield return suffixEnumerator;
        }

        void IDisposable.Dispose() { Dispose(); }
        public void Dispose() { }

        void IEnumerator.Reset() { Reset(); }
        public void Reset() { throw new NotImplementedException(); }
    }
}
