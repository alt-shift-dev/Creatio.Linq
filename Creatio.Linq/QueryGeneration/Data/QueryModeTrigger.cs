using System;

namespace Creatio.Linq.QueryGeneration.Data
{
	internal class QueryModeTrigger: IDisposable
	{
		private readonly QueryPartsAggregator _aggregator;
		private readonly QueryPartAggregationMode _prevMode;

		internal QueryModeTrigger(QueryPartsAggregator aggregator, QueryPartAggregationMode mode)
		{
			_aggregator = aggregator ?? throw new ArgumentNullException(nameof(aggregator));
			_prevMode = _aggregator.Mode;

			_aggregator.SetAggregationMode(mode);
		}

		public void Dispose()
		{
			_aggregator.SetAggregationMode(_prevMode);
		}
	}
}