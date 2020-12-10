using System;
using System.Collections.Generic;
using System.Reflection;
using Terrasoft.Common;
using Terrasoft.Core.Entities;

namespace Creatio.Linq.QueryGeneration.Data.States
{
	/// <summary>
	/// Switches states of query collector.
	/// </summary>
	internal class QueryCollectorState: IDisposable
	{
		private QueryPartsAggregator _aggregator;
		private Stack<QueryCollectorStateBase> _states = new Stack<QueryCollectorStateBase>();
		private QueryCollectorStateBase _currentState;

		/// <summary>
		/// Returns path to recently added column (if applicable).
		/// </summary>
		public string LastColumn => _currentState?.LastColumnPath;

		public QueryCollectorState(QueryPartsAggregator aggregator)
		{
			_aggregator = aggregator ?? throw new ArgumentNullException(nameof(aggregator));
			SetAggregationMode(QueryPartAggregationMode.Aggregate);
		}

		public void SetComparison(FilterComparisonType comparison, object value)
		{
			_currentState.SetComparison(comparison, value);
		}

		public void SetColumn(string columnPath, Type columnType)
		{
			_currentState.SetColumn(columnPath, columnType);
		}

		public void SetSortOrder(bool @descending)
		{
			_currentState.SetSortOrder(@descending);
		}

		public void SetNegative()
		{
			_currentState.SetNegative();
		}

		public void SetColumnAlias(int position, string alias)
		{
			_currentState.SetColumnAlias(position, alias);
		}

		public IDisposable PushFilter(LogicalOperationStrict? operation)
		{
			_currentState.PushFilter(operation);
			return new QueryModeRestorer(PopFilter);
		}

		public void PopFilter()
		{
			_currentState.PopFilter();
		}

		public IDisposable PushResultElement()
		{
			_currentState.PushColumn();

			return new QueryModeRestorer(PopResultElement);
		}

		public void PopResultElement()
		{
			_currentState.PopColumn();
		}

		public void SetResultConstructor(ConstructorInfo constructorInfo)
		{
			_aggregator.SetResultConstructor(constructorInfo);
		}

		/// <summary>
		/// Sets aggregation mode.
		/// </summary>
		/// <param name="mode">New aggregation mode.</param>
		public void SetAggregationMode(QueryPartAggregationMode mode)
		{
			_states.Push(_currentState);

			switch (mode)
			{
				case QueryPartAggregationMode.Where:
					_currentState = new QueryCollectorStateWhere(_aggregator);
					return;

				case QueryPartAggregationMode.OrderBy:
					_currentState = new QueryCollectorStateOrderBy(_aggregator);
					return;

				case QueryPartAggregationMode.Select:
					_currentState = new QueryCollectorStateSelect(_aggregator);
					return;

				case QueryPartAggregationMode.GroupBy:
					_currentState = new QueryCollectorStateGroupBy(_aggregator);
					return;

				case QueryPartAggregationMode.Aggregate:
					_currentState = new QueryCollectorStateAggregation(_aggregator);
					return;

				default:
					throw new InvalidOperationException($"Aggregation mode {mode} is not yet supported by QueryPartsAggregator.");
			}
		}

		/// <summary>
		/// Sets new aggregation mode.
		/// </summary>
		public IDisposable PushAggregationMode(QueryPartAggregationMode mode)
		{
			SetAggregationMode(mode);

			return new QueryModeRestorer(PopAggregationMode);
		}

		/// <summary>
		/// Restores previous aggregation mode.
		/// </summary>
		public void PopAggregationMode()
		{
			if (null == _currentState)
			{
				throw new InvalidOperationException($"Unable to restore previous aggregation mode, history is empty.");
			}

			_currentState.Dispose();
			_currentState = _states.Pop();
		}

		public void Dispose()
		{
			if (null != _currentState)
			{
				PopAggregationMode();
			}
		}
	}
}