namespace Creatio.Linq.QueryGeneration.Data
{
	/// <summary>
	/// Defines query part aggregation mode.
	/// </summary>
	public enum QueryPartAggregationMode
	{
		/// <summary>
		/// Aggregation mode not defined.
		/// </summary>
		Undefined = -1,

		/// <summary>
		/// Currently in Where clause.
		/// </summary>
		Where,

		/// <summary>
		/// Currently in OrderBy clause.
		/// </summary>
		OrderBy,

		/// <summary>
		/// Currently in Select clause.
		/// </summary>
		Select,

		/// <summary>
		/// Currently in GroupBy clause.
		/// </summary>
		GroupBy,

		/// <summary>
		/// Currently in Count/Sum/Min/Max/Avg clause.
		/// </summary>
		Aggregate,
	}
}