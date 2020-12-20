namespace Creatio.Linq.QueryGeneration.Util
{
	/// <summary>
	/// Current query process operation, used for logging.
	/// </summary>
	internal enum QueryProcessOperation
	{
		/// <summary>
		/// Procrastinating.
		/// </summary>
		None,
		
		/// <summary>
		/// Parsing query with re-linq.
		/// </summary>
		LinqParse,
		
		/// <summary>
		/// Generating ESQ.
		/// </summary>
		EsqGeneration,
		
		/// <summary>
		/// Executing query.
		/// </summary>
		Executing,
	}
}