using System;
using System.Diagnostics;
using Creatio.Linq.QueryGeneration.Data.Fragments;
using Terrasoft.Common;
using Terrasoft.Core.Entities;

namespace Creatio.Linq.QueryGeneration.Data.States
{
	/// <summary>
	/// Defines behavior of <see cref="QueryPartsAggregator"/> when re-linq is visiting Where clause.
	/// </summary>
	internal class QueryCollectorStateWhere: QueryCollectorStateBase
	{
		private QueryFilterCollection _currentFilter;
		private string _filterColumn = null;


		public QueryCollectorStateWhere(QueryPartsAggregator aggregator) : base(aggregator)
		{
			Trace.WriteLine("Entering Where state.");
			_currentFilter = aggregator.Filters;
		}

		public override string LastColumnPath => _filterColumn;

		public override void SetComparison(FilterComparisonType comparison, object value)
		{
			_currentFilter.AddFilter(new QueryFilterData(_filterColumn, value, comparison));
			_filterColumn = null;
		}

		public override void SetColumn(string columnPath, Type columnType)
		{
			_filterColumn = columnPath;
		}

		public override void SetSortOrder(bool descending)
		{
			throw new NotImplementedException();
		}

		public override void SetNegative()
		{
			_currentFilter.Negative = true;
		}

		public override void SetColumnAlias(int position, string alias)
		{
			throw new NotImplementedException();
		}

		public override void PushFilter(LogicalOperationStrict? operation)
		{
			_currentFilter = _currentFilter.PushCollection();
			if (operation.HasValue)
			{
				_currentFilter.LogicalOperation = operation.Value;
			}
		}

		public override void PopFilter()
		{
			// handle situations with boolean columns being used as logical operation, e.g.:
			// .Where(item => item.Column<bool>("IsActive"))
			// since no comparison operator is set up re-linq will not call VisitBinary()
			if (null == _currentFilter.Current && !string.IsNullOrEmpty(_filterColumn))
			{
				_currentFilter.AddFilter(new QueryFilterData(_filterColumn, true, FilterComparisonType.Equal));
			}

			_currentFilter = _currentFilter.TryUniteWithParent();
		}

		public override void PushColumn()
		{
			throw new NotImplementedException();
		}

		public override void PopColumn()
		{
			throw new NotImplementedException();
		}

		public override void Dispose()
		{
			Trace.WriteLine("Disposing Where state.");
		}
	}
}