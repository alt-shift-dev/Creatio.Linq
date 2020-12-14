using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Terrasoft.Common;
using Terrasoft.Core.Entities;
using Terrasoft.Core.Process.Tracing;

namespace Creatio.Linq.QueryGeneration.Data.States
{
	/// <summary>
	/// Switches states of query collector.
	/// </summary>
	internal class QueryCollectorState: IDisposable
	{
		private QueryPartCollector _aggregator;
		private Stack<QueryCollectorStateBase> _states = new Stack<QueryCollectorStateBase>();
		private QueryCollectorStateBase _currentState;

		/// <summary>
		/// Returns path to recently added column (if applicable).
		/// </summary>
		public string LastColumn => _currentState?.LastColumnPath;

		public QueryCollectorState(QueryPartCollector aggregator)
		{
			_aggregator = aggregator ?? throw new ArgumentNullException(nameof(aggregator));
			SetAggregationMode(QueryCollectionState.Aggregate);
		}

		public void SetComparison(FilterComparisonType comparison, object value)
		{
			Trace.WriteLine($"* SetComparison: {comparison} {value}");
			_currentState.SetComparison(comparison, value);
		}

		public void SetFunction(string methodName, object arg)
		{
			Trace.WriteLine($"* SetFunction: {methodName} {arg}");
			_currentState.SetFunction(methodName, arg);
		}

		public void SetColumn(string columnPath)
		{
			Trace.WriteLine($"* SetColumn: {columnPath}");
			_currentState.SetColumn(columnPath);
		}

		public void SetSortOrder(bool descending)
		{
			Trace.WriteLine($"* SetOrder: descending: {descending}");
			_currentState.SetSortOrder(descending);
		}

		public void SetNegative()
		{
			Trace.WriteLine($"* SetNegative");
			_currentState.SetNegative();
		}

		public void SetColumnAlias(int position, string alias)
		{
			Trace.WriteLine($"* SetColumnAlias: {alias} ({position})");
			_currentState.SetColumnAlias(position, alias);
		}

		public IDisposable PushFilter(LogicalOperationStrict? operation)
		{
			Trace.WriteLine($"-> PushFilter: {operation}");
			_currentState.PushFilter(operation);
			return new QueryModeRestorer(PopFilter);
		}

		public void PopFilter()
		{
			Trace.WriteLine("<- PopFilter");
			_currentState.PopFilter();
		}

		public IDisposable PushColumn()
		{
			Trace.WriteLine("-> PushColumn");
			_currentState.PushColumn();

			return new QueryModeRestorer(PopColumn);
		}

		public void PopColumn()
		{
			Trace.WriteLine("<- PopColumn");
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
		public void SetAggregationMode(QueryCollectionState mode)
		{
			_states.Push(_currentState);

			switch (mode)
			{
				case QueryCollectionState.Where:
					_currentState = new QueryCollectorStateWhere(_aggregator);
					return;

				case QueryCollectionState.OrderBy:
					_currentState = new QueryCollectorStateOrderBy(_aggregator);
					return;

				case QueryCollectionState.Select:
					_currentState = new QueryCollectorStateSelect(_aggregator);
					return;

				case QueryCollectionState.GroupBy:
					_currentState = new QueryCollectorStateGroupBy(_aggregator);
					return;

				case QueryCollectionState.Aggregate:
					_currentState = new QueryCollectorStateAggregation(_aggregator);
					return;

				default:
					throw new InvalidOperationException($"Aggregation mode {mode} is not yet supported by QueryPartCollector.");
			}
		}

		/// <summary>
		/// Sets new aggregation mode.
		/// </summary>
		public IDisposable PushCollectorMode(QueryCollectionState mode)
		{
			Trace.WriteLine($"-> PushCollectorMode: {mode}");

			SetAggregationMode(mode);

			return new QueryModeRestorer(PopCollectorMode);
		}

		/// <summary>
		/// Restores previous aggregation mode.
		/// </summary>
		public void PopCollectorMode()
		{
			Trace.WriteLine($"<- PopCollectorMode");

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
				PopCollectorMode();
			}
		}
	}
}