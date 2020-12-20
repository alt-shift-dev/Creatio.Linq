using System.Linq;
using System.Linq.Expressions;
using Remotion.Linq;
using Remotion.Linq.Parsing.Structure;
using Terrasoft.Core;

namespace Creatio.Linq
{
	/// <summary>
	/// Main entry point to LINQ query.
	/// </summary>
	public class EntitySchemaQueryable<T>: QueryableBase<T>
	{
		public EntitySchemaQueryable(UserConnection userConnection, string schemaName)
			: base(QueryParser.CreateDefault(), CreateExecutor(userConnection, schemaName))
		{
		}

		public EntitySchemaQueryable(UserConnection userConnection, string schemaName, LogOptions logOptions)
			: base(QueryParser.CreateDefault(), CreateExecutor(userConnection, schemaName, logOptions))
		{
		}

		private static IQueryExecutor CreateExecutor(UserConnection userConnection, string schemaName, LogOptions logOptions = null)
		{
			return new EntitySchemaQueryExecutor(userConnection, schemaName, logOptions);
		}

		// This constructor is called indirectly by LINQ's query methods, just pass to base.
		public EntitySchemaQueryable(IQueryParser queryParser, IQueryExecutor executor) : base(queryParser, executor)
		{
		}

		// This constructor is called indirectly by LINQ's query methods, just pass to base.
		public EntitySchemaQueryable(IQueryProvider provider) : base(provider)
		{
		}

		// This constructor is called indirectly by LINQ's query methods, just pass to base.
		public EntitySchemaQueryable(IQueryProvider provider, Expression expression) : base(provider, expression)
		{
		}
	}
}