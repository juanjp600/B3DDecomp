using System.Collections;
using Blitz3DDecomp.LowLevel;

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

        public IEnumerator<Statement> GetEnumerator()
        {
            int startIndex = owner.StartIndex;
            for (int i = 0; i < Count; i++)
            {
                yield return owner.Owner.HighLevelStatements[startIndex + i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();

        public void Add(Statement item)
        {
            int startIndex = owner.StartIndex;
            owner.Owner.HighLevelStatements.Insert(startIndex + Count, item);
            count++;
        }

        public void Clear()
        {
            int startIndex = owner.StartIndex;
            owner.Owner.HighLevelStatements.RemoveRange(startIndex, Count);
            count = 0;
        }

        public bool Contains(Statement item)
        {
            int startIndex = owner.StartIndex;
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
                index: owner.StartIndex,
                array: array,
                arrayIndex: arrayIndex,
                count: Count);
        }

        public bool Remove(Statement item)
        {
            int startIndex = owner.StartIndex;
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
            int startIndex = owner.StartIndex;
            for (int i = 0; i < Count; i++)
            {
                if (owner.Owner.HighLevelStatements[startIndex + i].Equals(item)) { return i; }
            }
            return -1;
        }

        public void Insert(int index, Statement item)
        {
            if (index < 0 || index > Count) { throw new ArgumentOutOfRangeException(nameof(index)); }
            owner.Owner.HighLevelStatements.Insert(owner.StartIndex + index, item);
            count++;
        }

        public void RemoveAt(int index)
        {
            CheckIndexWithinRange(index);
            owner.Owner.HighLevelStatements.RemoveAt(owner.StartIndex + index);
            count--;
        }

        public Statement this[int index]
        {
            get
            {
                CheckIndexWithinRange(index);
                return owner.Owner.HighLevelStatements[owner.StartIndex + index];
            }
            set
            {
                CheckIndexWithinRange(index);
                owner.Owner.HighLevelStatements[owner.StartIndex + index] = value;
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

    public int StartIndex
    {
        get
        {
            var indexOfSelfInOwnerSectionList = Owner.HighLevelSections.IndexOf(this);

            int startIndex = 0;
            for (int i = 0; i < indexOfSelfInOwnerSectionList; i++)
            {
                startIndex += Owner.HighLevelSections[i].Statements.Count;
            }
            return startIndex;
        }
    }

    public HighLevelSection? PreviousSection
    {
        get
        {
            var indexOfSelf = Owner.HighLevelSections.IndexOf(this);
            if (indexOfSelf <= 0) { return null; }
            return Owner.HighLevelSections[indexOfSelf - 1];
        }
    }

    public HighLevelSection? NextSection
    {
        get
        {
            var indexOfSelf = Owner.HighLevelSections.IndexOf(this);
            if (indexOfSelf < 0) { return null; }
            if (indexOfSelf >= Owner.HighLevelSections.Count - 1) { return null; }
            return Owner.HighLevelSections[indexOfSelf + 1];
        }
    }

    public AssemblySection? LinkedAssemblySection
        => Owner.AssemblySectionsByName.GetValueOrDefault(Name);

    public HighLevelSection(Function owner, string name)
    {
        Name = name;
        Statements = new SubList(this);
        Owner = owner;
    }

    public static string CleanupSectionName(string lowLevelName, Function function)
    {
        var retVal = lowLevelName;
        if (retVal.EndsWith(function.CoreSymbolName)) { retVal = retVal[..^function.CoreSymbolName.Length]; }

        if (!lowLevelName.StartsWith("_l_", StringComparison.Ordinal)) { return "section_" + retVal; }

        for (int i = 3; i < lowLevelName.Length; i++)
        {
            if (!char.IsDigit(lowLevelName[i])) { return retVal[i..]; }
        }
        return "section" + retVal;
    }
}