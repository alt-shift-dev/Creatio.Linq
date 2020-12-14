namespace Creatio.Linq.QueryGeneration.Util
{
	/// <summary>
	/// Utils for supporting query generation.
	/// </summary>
	public class QueryUtils
	{
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