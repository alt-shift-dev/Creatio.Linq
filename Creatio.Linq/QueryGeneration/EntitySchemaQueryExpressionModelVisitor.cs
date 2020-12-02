using System;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using Creatio.Linq.QueryGeneration.Data;
using Remotion.Linq;
using Remotion.Linq.Clauses;
using Remotion.Linq.Clauses.Expressions;
using Remotion.Linq.Clauses.ResultOperators;
using Terrasoft.Common;
using Terrasoft.Core.Entities;

namespace Creatio.Linq.QueryGeneration
{
	internal class EntitySchemaQueryExpressionModelVisitor: QueryModelVisitorBase
	{
		private readonly QueryPartsAggregator _queryParts = new QueryPartsAggregator();

		public static QueryData GenerateEntitySchemaQueryData(QueryModel queryModel)
		{
			var visitor = new EntitySchemaQueryExpressionModelVisitor();
			visitor.VisitQueryModel(queryModel);
			return visitor.GetEntitySchemaQueryData();
		}

		public QueryData GetEntitySchemaQueryData()
		{
			return new QueryData(_queryParts);
		}

		public override void VisitResultOperator(ResultOperatorBase resultOperator, QueryModel queryModel, int index)
		{
			Trace.WriteLine($"VisitResultOperator: {resultOperator}");

			// First()
			if (resultOperator is FirstResultOperator)
			{
				_queryParts.Take = 1;
				return;
			}

			// Count()
			if (resultOperator is CountResultOperator || resultOperator is LongCountResultOperator)
			{
				_queryParts.ReturnCount = true;
			}

			// Take()
			if (resultOperator is TakeResultOperator takeOperator)
			{
				var exp = takeOperator.Count;

				if (exp.NodeType == ExpressionType.Constant)
				{
					_queryParts.Take = (int) ((ConstantExpression) exp).Value;
				}
				else
				{
					throw new NotSupportedException("Currently not supporting methods or variables in the Skip or Take clause.");
				}
			}

			// Skip()
			if (resultOperator is SkipResultOperator skipResult)
			{
				var exp = skipResult.Count;

				if (exp.NodeType == ExpressionType.Constant)
				{
					_queryParts.Skip = (int) ((ConstantExpression) exp).Value;
				}
				else
				{
					throw new NotSupportedException("Currently not supporting methods or variables in the Skip or Take clause.");
				}
			}
		}

		public override void VisitMainFromClause(MainFromClause fromClause, QueryModel queryModel)
		{
			Trace.WriteLine($"VisitMainFromClause: {fromClause}");
			base.VisitMainFromClause(fromClause, queryModel);

			var subQueryExpression = fromClause.FromExpression as SubQueryExpression;
			if (subQueryExpression == null)
			{
				return;
			}

			Trace.WriteLine($"Visiting SubQueryExpression: {subQueryExpression}");
			VisitQueryModel(subQueryExpression.QueryModel);
		}

		public override void VisitSelectClause(SelectClause selectClause, QueryModel queryModel)
		{
			using (_queryParts.PushAggregationMode(QueryPartAggregationMode.Select))
			{
				Trace.WriteLine($"VisitSelectClause: {selectClause}");

				UpdateEntitySchemaQueryExpression(selectClause.Selector);

				base.VisitSelectClause(selectClause, queryModel);
			}
		}

		public override void VisitWhereClause(WhereClause whereClause, QueryModel queryModel, int index)
		{
			using (_queryParts.PushAggregationMode(QueryPartAggregationMode.Where))
			{
				using (_queryParts.PushFilter())
				{
					_queryParts.SetFilterLogicalOperation(LogicalOperationStrict.And);

					Trace.WriteLine($"VisitWhereClause: {whereClause}");
					UpdateEntitySchemaQueryExpression(whereClause.Predicate);
				}

				base.VisitWhereClause(whereClause, queryModel, index);
			}
		}

		public override void VisitOrderByClause(OrderByClause orderByClause, QueryModel queryModel, int index)
		{
			using (_queryParts.PushAggregationMode(QueryPartAggregationMode.OrderBy))
			{
				Trace.WriteLine($"VisitOrderByClause: {orderByClause}");

				foreach (var ordering in orderByClause.Orderings)
				{
					_queryParts.SetSortOrder(ordering.OrderingDirection == OrderingDirection.Desc);
					UpdateEntitySchemaQueryExpression(ordering.Expression);
				}

				base.VisitOrderByClause(orderByClause, queryModel, index);
			}
			
		}

		public override void VisitJoinClause(JoinClause joinClause, QueryModel queryModel, int index)
		{
			Trace.WriteLine($"VisitJoinClause: {joinClause.InnerKeySelector}, {joinClause.OuterKeySelector}");

			UpdateEntitySchemaQueryExpression(joinClause.InnerKeySelector);
			UpdateEntitySchemaQueryExpression(joinClause.OuterKeySelector);
			throw new NotImplementedException("VisitJoinClause");
			base.VisitJoinClause(joinClause, queryModel, index);
		}

		public override void VisitAdditionalFromClause(AdditionalFromClause fromClause, QueryModel queryModel, int index)
		{
			Trace.WriteLine($"VisitAdditionalFromClause {fromClause}");
			
			base.VisitAdditionalFromClause(fromClause, queryModel, index);
		}

		public override void VisitGroupJoinClause(GroupJoinClause groupJoinClause, QueryModel queryModel, int index)
		{
			Trace.WriteLine($"VisitGroupJoinClause: {groupJoinClause}");
			throw new NotSupportedException("VisitGroupJoinClause");
			base.VisitGroupJoinClause(groupJoinClause, queryModel, index);
		}

		private void UpdateEntitySchemaQueryExpression(Expression expression)
		{
			EntitySchemaQueryExpressionTreeVisitor.SetupQueryParts(expression, _queryParts);
		}
	}
}