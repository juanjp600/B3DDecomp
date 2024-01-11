using System.Collections;

namespace Blitz3DDecomp.HighLevel;

sealed class HighLevelSection
{
    private sealed class SubList : IList<Statement>
    {
        private readonly HighLevelSection owner;

        public SubList(HighLevelSection owner)
        {
            this.owner = owner;
        }

        private int StartIndex
        {
            get
            {
                var indexOfSelfInOwnerSectionList = owner.Owner.HighLevelSections.IndexOf(owner);

                int startIndex = 0;
                for (int i = 0; i < indexOfSelfInOwnerSectionList; i++)
                {
                    startIndex += owner.Owner.HighLevelSections[i].Statements.Count;
                }
                return startIndex;
            }
        }

        public IEnumerator<Statement> GetEnumerator()
        {
            int startIndex = StartIndex;
            for (int i = 0; i < Count; i++)
            {
                yield return owner.Owner.HighLevelStatements[startIndex + i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();

        public void Add(Statement item)
        {
            int startIndex = StartIndex;
            owner.Owner.HighLevelStatements.Insert(startIndex + Count, item);
            count++;
        }

        public void Clear()
        {
            int startIndex = StartIndex;
            owner.Owner.HighLevelStatements.RemoveRange(startIndex, Count);
            count = 0;
        }

        public bool Contains(Statement item)
        {
            int startIndex = StartIndex;
            for (int i = 0; i < Count; i++)
            {
                if (owner.Owner.HighLevelStatements[startIndex + i].Equals(item)) { return true; }
            }
            return false;
        }

        public void CopyTo(Statement[] array, int arrayIndex)
        {
            if (array.Length == 0) { return; }
            owner.Owner.HighLevelStatements.CopyTo(
                index: StartIndex,
                array: array,
                arrayIndex: arrayIndex,
                count: Count);
        }

        public bool Remove(Statement item)
        {
            int startIndex = StartIndex;
            for (int i = 0; i < Count; i++)
            {
                if (owner.Owner.HighLevelStatements[startIndex + i].Equals(item))
                {
                    owner.Owner.HighLevelStatements.RemoveAt(startIndex + i);
                    count--;
                    return true;
                }
            }
            return false;
        }

        private int count;
        public int Count => count;

        public bool IsReadOnly => false;

        public int IndexOf(Statement item)
        {
            int startIndex = StartIndex;
            for (int i = 0; i < Count; i++)
            {
                if (owner.Owner.HighLevelStatements[startIndex + i].Equals(item)) { return i; }
            }
            return -1;
        }

        public void Insert(int index, Statement item)
        {
            CheckIndexWithinRange(index);
            owner.Owner.HighLevelStatements.Insert(StartIndex + index, item);
            count++;
        }

        public void RemoveAt(int index)
        {
            CheckIndexWithinRange(index);
            owner.Owner.HighLevelStatements.RemoveAt(StartIndex + index);
            count--;
        }

        public Statement this[int index]
        {
            get
            {
                CheckIndexWithinRange(index);
                return owner.Owner.HighLevelStatements[StartIndex + index];
            }
            set
            {
                CheckIndexWithinRange(index);
                owner.Owner.HighLevelStatements[StartIndex + index] = value;
            }
        }

        private void CheckIndexWithinRange(int index)
        {
            if (index < 0 || index >= Count) { throw new ArgumentOutOfRangeException(nameof(index)); }
        }
    }

    public readonly string Name;
    public readonly IList<Statement> Statements;
    public readonly Function Owner;

    public HighLevelSection(Function owner, string name)
    {
        Name = name;
        Statements = new SubList(this);
        Owner = owner;
    }
}