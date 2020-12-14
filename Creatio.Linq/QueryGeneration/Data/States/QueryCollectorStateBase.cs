using System;
using Terrasoft.Common;
using Terrasoft.Core.Entities;

namespace Creatio.Linq.QueryGeneration.Data.States
{
	/// <summary>
	/// Base class for handling <see cref="QueryPartCollector"/> states.
	/// </summary>
	internal abstract class QueryCollectorStateBase: IDisposable
	{
		protected QueryPartCollector Aggregator { get; set; }

		protected QueryCollectorStateBase(QueryPartCollector aggregator)
		{
			Aggregator = aggregator ?? throw new ArgumentNullException(nameof(aggregator));
		}

		public virtual string LastColumnPath => null;

		public virtual void SetComparison(FilterComparisonType comparison, object value)
		{
			throw new NotSupportedException($"State handler '{GetType().Name}' does not support method {nameof(SetComparison)}");
		}

		public virtual void SetFunction(string methodName, object value)
		{
			throw new NotSupportedException($"State handler '{GetType().Name}' does not support method {nameof(SetFunction)}");
		}

		public virtual void SetColumn(string columnPath)
		{
			throw new NotSupportedException($"State handler '{GetType().Name}' does not support method {nameof(SetColumn)}");
		}

		public virtual void SetSortOrder(bool descending)
		{
			throw new NotSupportedException($"State handler '{GetType().Name}' does not support method {nameof(SetSortOrder)}");
		}

		public virtual void SetNegative()
		{
			throw new NotSupportedException($"State handler '{GetType().Name}' does not support method {nameof(SetNegative)}");
		}

		public virtual void SetColumnAlias(int position, string alias)
		{
			throw new NotSupportedException($"State handler '{GetType().Name}' does not support method {nameof(SetColumnAlias)}");
		}

		public virtual void PushFilter(LogicalOperationStrict? operation)
		{
			throw new NotSupportedException($"State handler '{GetType().Name}' does not support method {nameof(PushFilter)}");
		}

		public virtual void PopFilter()
		{
			throw new NotSupportedException($"State handler '{GetType().Name}' does not support method {nameof(PopFilter)}");
		}

		public virtual void PushColumn()
		{
			throw new NotSupportedException($"State handler '{GetType().Name}' does not support method {nameof(PushColumn)}");
		}

		public virtual void PopColumn()
		{
			throw new NotSupportedException($"State handler '{GetType().Name}' does not support method {nameof(PopColumn)}");
		}

		/// <summary>
		/// Triggered when current aggregation mode being changed.
		/// </summary>
		public abstract void Dispose();
	}
}