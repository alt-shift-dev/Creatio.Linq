using System;

namespace Creatio.Linq.QueryGeneration.Data
{
	internal class QueryFilterLevelTrigger: IDisposable
	{
		private readonly QueryPartsAggregator _aggregator;

		public QueryFilterLevelTrigger(QueryPartsAggregator aggregator)
		{
			_aggregator = aggregator ?? throw new ArgumentNullException(nameof(aggregator));
		}

		public void Dispose()
		{
			_aggregator.PopFilter();
		}
	}
}