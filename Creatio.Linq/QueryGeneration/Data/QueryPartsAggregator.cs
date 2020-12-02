using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Remotion.Linq.Clauses;
using Terrasoft.Common;
using Terrasoft.Core.Entities;

namespace Creatio.Linq.QueryGeneration.Data
{
	/// <summary>
	/// Aggregates query info.
	/// </summary>
	internal class QueryPartsAggregator
	{
		private QueryPartAggregationMode _mode = QueryPartAggregationMode.Undefined;

		private List<QueryOrderData> _orders = new List<QueryOrderData>();
		private List<QuerySelectColumnData> _select = new List<QuerySelectColumnData>();
		private QueryFilterCollection _filters = new QueryFilterCollection();
		private QueryFilterCollection _currentFilter;
		private ConstructorInfo _resultTypeConstructor;

		private string _filterColumn = null;
		private bool _sortDescending = false;

		public QueryFilterCollection Filters => _filters;

		public IReadOnlyList<QueryOrderData> Orders => _orders.AsReadOnly();

		public IReadOnlyList<QuerySelectColumnData> Select => _select.AsReadOnly();

		/// <summary>
		/// Page size.
		/// </summary>
		public int? Take { get; set; }

		/// <summary>
		/// Page start.
		/// </summary>
		public int? Skip { get; set; }

		public bool ReturnCount = false;

		public string LastFilterColumn => _filterColumn;

		internal QueryPartAggregationMode Mode => _mode;

		public QueryPartsAggregator()
		{
			_currentFilter = _filters;
		}

		/// <summary>
		/// Whether query returns anonymous class with projection
		/// </summary>
		public bool UseResultProjection => _resultTypeConstructor != null;

		/// <summary>
		/// Gets ConstructorInfo provided by re-linq for generating query result items.
		/// </summary>
		public ConstructorInfo ResultProjectionCtor => _resultTypeConstructor;

		/// <summary>
		/// Add query filter.
		/// </summary>
		public void AddFilter(QueryFilterData filter)
		{
			_currentFilter.AddFilter(filter ?? throw new ArgumentNullException(nameof(filter)));
			_filterColumn = null;
		}

		/// <summary>
		/// Add query sort.
		/// </summary>
		/// <param name="order"></param>
		public void AddOrder(QueryOrderData order)
		{
			_orders.Add(order ?? throw new ArgumentNullException(nameof(order)));
		}

		/// <summary>
		/// Add column to query results.
		/// </summary>
		/// <param name="select"></param>
		public void AddSelect(QuerySelectColumnData select)
		{
			_select.Add(select ?? throw new ArgumentNullException(nameof(select)));
		}

		/// <summary>
		/// Sets aggregation mode.
		/// </summary>
		/// <param name="mode"></param>
		public void SetAggregationMode(QueryPartAggregationMode mode)
		{
			_mode = mode;
			_sortDescending = false;
			_filterColumn = null;
		}

		public void SetFilterLogicalOperation(LogicalOperationStrict operation)
		{
			_currentFilter.LogicalOperation = operation;
		}

		
		public void SetColumn(string columnPath, Type columnType)
		{
			if (_mode == QueryPartAggregationMode.OrderBy)
			{
				AddOrder(new QueryOrderData(columnPath, _sortDescending));
				_sortDescending = false;
			}

			if (_mode == QueryPartAggregationMode.Select)
			{
				AddSelect(new QuerySelectColumnData(columnPath, columnType));
			}

			if (_mode == QueryPartAggregationMode.Where)
			{
				_filterColumn = columnPath;
			}
		}

		public void SetSortOrder(bool descending)
		{
			_sortDescending = descending;
		}

		public void SetNegative()
		{
			_currentFilter.Negative = true;
		}

		public void SetResultConstructor(ConstructorInfo constructorInfo)
		{
			_resultTypeConstructor = constructorInfo ?? throw new ArgumentNullException(nameof(constructorInfo));
		}

		public IDisposable PushAggregationMode(QueryPartAggregationMode mode)
		{
			return new QueryModeTrigger(this, mode);
		}

		public IDisposable PushFilter()
		{
			Trace.WriteLine($"PushFilter, current column: {_filterColumn}");
			_currentFilter = _currentFilter.PushCollection();

			return new QueryFilterLevelTrigger(this);
		}

		public void PopFilter()
		{
			Trace.WriteLine($"PopFilter, current column: {_filterColumn}");

			// handle situations with boolean columns being used as logical operation, e.g.:
			// .Where(item => item.Column<bool>("IsActive"))
			// since no comparison operator is set up re-linq will not call VisitBinary()
			if (null == _currentFilter.Current && !string.IsNullOrEmpty(_filterColumn))
			{
				_currentFilter.AddFilter(new QueryFilterData(_filterColumn, true, FilterComparisonType.Equal));
			}

			_currentFilter = _currentFilter.TryUniteWithParent();
		}
	
	}
}