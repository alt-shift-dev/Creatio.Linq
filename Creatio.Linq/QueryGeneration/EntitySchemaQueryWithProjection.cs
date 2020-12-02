using System;
using System.Threading;
using Terrasoft.Core.DB;
using Terrasoft.Core.Entities;

namespace Creatio.Linq.QueryGeneration
{
	public class EntitySchemaQueryWithProjection: EntitySchemaQuery
	{
		private Func<Entity, object> _resultProjector;

		public EntitySchemaQueryWithProjection()
		{
		}

		public EntitySchemaQueryWithProjection(EntitySchema rootSchema) : base(rootSchema)
		{
		}

		public EntitySchemaQueryWithProjection(EntitySchema rootSchema, CancellationToken cancellationToken) : base(rootSchema, cancellationToken)
		{
		}

		public EntitySchemaQueryWithProjection(EntitySchemaManager entitySchemaManager, string sourceSchemaName) : base(entitySchemaManager, sourceSchemaName)
		{
		}

		public EntitySchemaQueryWithProjection(EntitySchemaQuery source) : base(source)
		{
		}

		public void SetResultProjector(Func<Entity, object> resultProjector)
		{
			_resultProjector = resultProjector ?? throw new ArgumentNullException(nameof(resultProjector));
		}

		public TResult Project<TResult>(Entity entity)
		{
			if (null == entity) throw new ArgumentNullException(nameof(entity));
			if (null == _resultProjector) throw new InvalidOperationException("Entity result projection is not defined.");

			if (typeof(TResult) == typeof(DynamicEntity))
			{
				return (TResult)(object)new DynamicEntity(entity);
			}

			var result = _resultProjector(entity);
			try
			{
				return (TResult) result;
			}
			catch
			{
				throw new InvalidOperationException($"Unable to convert type {result.GetType()} to target projection type {typeof(TResult)}.");
			}

		}
	}
}