using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Versioning;

namespace mRemoteNG.Connection
{
    [SupportedOSPlatform("windows")]
    public class ConnectionInfoComparer<TProperty>(Func<ConnectionInfo, TProperty> sortExpression) : IComparer<ConnectionInfo> where TProperty : IComparable<TProperty>
    {
        private readonly Func<ConnectionInfo, TProperty> _sortExpression = sortExpression;
        public ListSortDirection SortDirection { get; set; } = ListSortDirection.Ascending;

        public int Compare(ConnectionInfo? x, ConnectionInfo? y)
        {
            if (x == null && y == null) return 0;
            if (x == null) return SortDirection == ListSortDirection.Ascending ? -1 : 1;
            if (y == null) return SortDirection == ListSortDirection.Ascending ? 1 : -1;

            return SortDirection == ListSortDirection.Ascending ? CompareAscending(x, y) : CompareDescending(x, y);
        }

        private int CompareAscending(ConnectionInfo x, ConnectionInfo y)
        {
            return _sortExpression(x).CompareTo(_sortExpression(y));
        }

        private int CompareDescending(ConnectionInfo x, ConnectionInfo y)
        {
            return _sortExpression(y).CompareTo(_sortExpression(x));
        }
    }
}