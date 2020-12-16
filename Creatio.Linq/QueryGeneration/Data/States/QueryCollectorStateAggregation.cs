using System;
using System.Diagnostics;
using Creatio.Linq.QueryGeneration.Data.Fragments;
using Creatio.Linq.QueryGeneration.Util;

namespace Creatio.Linq.QueryGeneration.Data.States
{
	/// <summary>
	/// Defines behavior of <see cref="QueryPartCollector"/> when re-linq is visiting aggregation clause.
	/// </summary>
	internal class QueryCollectorStateAggregation: QueryCollectorStateBase
	{
		public QueryCollectorStateAggregation(QueryPartCollector aggregator) : base(aggregator)
		{
			Trace.WriteLine("QueryCollectorStateAggregation::ctor");
		}

		public override void SetFunction(string methodName, object value)
		{
			if (string.IsNullOrEmpty(methodName)) throw new ArgumentNullException(nameof(methodName));
			
			Aggregator.SetResultAggregationType(AggregationTypeConverter.FromString(methodName));
		}

		public override void Dispose()
		{
			Trace.WriteLine("QueryCollectorStateAggregation::Dispose()");
		}
	}
}