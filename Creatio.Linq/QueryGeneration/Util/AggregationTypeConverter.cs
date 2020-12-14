using System;
using System.Collections.Generic;
using System.Linq;
using Terrasoft.Common;

namespace Creatio.Linq.QueryGeneration.Util
{
	/// <summary>
	/// Converts LINQ aggregation function name to ESQ's <see cref="AggregationTypeStrict"/>.
	/// </summary>
	public class AggregationTypeConverter
	{
		private static Dictionary<AggregationTypeStrict, string> _aggregationTypeMap =
			new Dictionary<AggregationTypeStrict, string>
			{
				[AggregationTypeStrict.Min] = "min",
				[AggregationTypeStrict.Max] = "max",
				[AggregationTypeStrict.Avg] = "average",
				[AggregationTypeStrict.Sum] = "sum",
				[AggregationTypeStrict.Count] = "count"
			};

		/// <summary>
		/// For given LINQ aggregation function name returns ESQ's aggregation type.
		/// </summary>
		public static AggregationTypeStrict FromString(string aggregationTypeString)
		{
			if (string.IsNullOrEmpty(aggregationTypeString))
			{
				throw new ArgumentNullException(nameof(aggregationTypeString));
			}

			var aggFunc = aggregationTypeString.ToLowerInvariant();

			if (!_aggregationTypeMap.ContainsValue(aggFunc))
			{
				throw new InvalidOperationException($"Aggregation function {aggregationTypeString} is not supported.");
			}

			return _aggregationTypeMap
				.First(kv => kv.Value == aggFunc)
				.Key;
		}
	}
}