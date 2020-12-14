using System;
using System.Collections.Generic;
using System.Diagnostics;
using Creatio.Linq.QueryGeneration.Data.Fragments;
using Terrasoft.Common;
using Terrasoft.Core.Entities;

namespace Creatio.Linq.QueryGeneration.Data.States
{
	/// <summary>
	/// Defines behavior of <see cref="QueryPartCollector"/> when re-linq is visiting Where clause.
	/// </summary>
	internal class QueryCollectorStateWhere: QueryCollectorStateBase
	{
		private QueryFilterCollection _currentFilter;
		private string _filterColumn = null;
		private Dictionary<string, FilterComparisonType> _comparisonMap = new Dictionary<string, FilterComparisonType>
		{
			["StartsWith"] = FilterComparisonType.StartWith,
			["Contains"] = FilterComparisonType.Contain,
			["EndsWith"] = FilterComparisonType.EndWith,
		};


		public QueryCollectorStateWhere(QueryPartCollector aggregator) : base(aggregator)
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

		public override void SetColumn(string columnPath)
		{
			_filterColumn = columnPath;
		}

		public override void SetNegative()
		{
			_currentFilter.Negative = true;
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

		public override void SetFunction(string methodName, object value)
		{
			if (!_comparisonMap.ContainsKey(methodName))
			{
				throw new InvalidOperationException($"Function '{methodName}' is not supported in Where() clause.");
			}

			SetComparison(_comparisonMap[methodName], value);
		}

		public override void Dispose()
		{
			Trace.WriteLine("Disposing Where state.");
		}
	}
}