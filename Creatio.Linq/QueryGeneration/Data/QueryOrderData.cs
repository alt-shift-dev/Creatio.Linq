using System;

namespace Creatio.Linq.QueryGeneration.Data
{
	/// <summary>
	/// Query order column and direction.
	/// </summary>
	internal class QueryOrderData
	{
		public string ColumnPath { get; set; }

		public bool Descending { get; set; }

		public QueryOrderData(string columnPath, bool descending)
		{
			if(string.IsNullOrEmpty(columnPath)) throw new ArgumentNullException(nameof(columnPath));

			ColumnPath = columnPath;
			Descending = descending;
		}
	}
}