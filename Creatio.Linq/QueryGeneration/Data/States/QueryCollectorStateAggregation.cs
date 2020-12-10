using System.Diagnostics;

namespace Creatio.Linq.QueryGeneration.Data.States
{
	internal class QueryCollectorStateAggregation: QueryCollectorStateBase
	{
		public QueryCollectorStateAggregation(QueryPartsAggregator aggregator) : base(aggregator)
		{
			Trace.WriteLine("QueryCollectorStateAggregation::ctor");
		}

		public override void Dispose()
		{
			Trace.WriteLine("QueryCollectorStateAggregation::Dispose()");
		}
	}
}