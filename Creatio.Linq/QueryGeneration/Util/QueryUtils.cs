namespace Creatio.Linq.QueryGeneration.Util
{
	/// <summary>
	/// Utils for supporting query generation.
	/// </summary>
	public class QueryUtils
	{

		/// <summary>
		/// Column alias when query is grouped by single column.
		/// </summary>
		public static string SingleGroupColumnAlias => "Key";
		
		/// <summary>
		/// Generate name for index columns after grouping.
		/// </summary>
		public static string GetIndexMemberName(int index)
		{
			return $"Key->{index}";
		}

		/// <summary>
		/// Generate name for anonymous class columns after grouping.
		/// </summary>
		public static string GetAliasMemberName(string columnAlias)
		{
			return $"Key->{columnAlias}";
		}
	}
}