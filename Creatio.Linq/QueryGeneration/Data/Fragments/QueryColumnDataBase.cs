namespace Creatio.Linq.QueryGeneration.Data.Fragments
{
	/// <summary>
	/// Base class for query columns.
	/// </summary>
	internal abstract class QueryColumnDataBase
	{
		/// <summary>
		/// ESQ column path.
		/// </summary>
		public string ColumnPath { get; set; }
	}
}