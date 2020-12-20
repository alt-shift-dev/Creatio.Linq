using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Creatio.Linq.QueryGeneration.Data;
using Creatio.Linq.QueryGeneration.Data.States;
using Creatio.Linq.QueryGeneration.Util;
using Remotion.Linq.Clauses;
using Remotion.Linq.Clauses.Expressions;
using Remotion.Linq.Clauses.ResultOperators;
using Remotion.Linq.Parsing;
using Terrasoft.Common;
using Terrasoft.Core.Entities;
using MethodCallExpression = System.Linq.Expressions.MethodCallExpression;

namespace Creatio.Linq.QueryGeneration
{
	/// <summary>
	/// ESQ expression tree visitor.
	/// Called when re-linq enters internal expression tree within From(), Select(), OrderBy(), etc clauses.
	/// </summary>
	internal class EntitySchemaQueryExpressionTreeVisitor: ThrowingExpressionVisitor
	{
		// maps expression types to esq filter comparison types
		protected static readonly Dictionary<ExpressionType, FilterComparisonType> ComparisonMappings = 
			new Dictionary<ExpressionType, FilterComparisonType>
			{
				[ExpressionType.Equal] = FilterComparisonType.Equal,
				[ExpressionType.NotEqual] = FilterComparisonType.NotEqual,
				[ExpressionType.GreaterThan] = FilterComparisonType.Greater,
				[ExpressionType.GreaterThanOrEqual] = FilterComparisonType.GreaterOrEqual,
				[ExpressionType.LessThan] = FilterComparisonType.Less,
				[ExpressionType.LessThanOrEqual] = FilterComparisonType.LessOrEqual,
			};

		// same for logical operations
		protected static readonly Dictionary<ExpressionType, LogicalOperationStrict> LogicalOperations = 
			new Dictionary<ExpressionType, LogicalOperationStrict>
			{
				[ExpressionType.And] = LogicalOperationStrict.And,
				[ExpressionType.AndAlso] = LogicalOperationStrict.And,
				[ExpressionType.Or] = LogicalOperationStrict.Or,
				[ExpressionType.OrElse] = LogicalOperationStrict.Or,
			};

		private QueryCollectorState _state;
		private Stack<object> _arguments = new Stack<object>();

		/// <summary>
		/// Proceeds with query parts collection.
		/// </summary>
		public static void SetupQueryParts(Expression linqExpression, QueryCollectorState state)
		{
			var visitor = new EntitySchemaQueryExpressionTreeVisitor(state);
			visitor.Visit(linqExpression);
		}

		/// <summary>
		/// Initializes new instance of <see cref="EntitySchemaQueryExpressionTreeVisitor"/>.
		/// </summary>
		public EntitySchemaQueryExpressionTreeVisitor(QueryCollectorState state)
		{
			_state = state ?? throw new ArgumentNullException(nameof(state));
		}

		/// <inheritdoc />
		protected override Expression VisitBinary(BinaryExpression expression)
		{
			LogWriter.WriteLine($"VisitBinary: {expression}");
			ProcessBinaryExpression(expression);
			LogWriter.WriteLine("End VisitBinary");
			return expression;
		}

		/// <summary>
		/// Processes binary expression (e.g. comparison operator)
		/// </summary>
		private void ProcessBinaryExpression(BinaryExpression expression)
		{
			LogWriter.WriteLine($"ConvertBinary: [{expression.Left}], operation {expression.NodeType}, [{expression.Right}]");

			if (ComparisonMappings.TryGetValue(expression.NodeType, out var comparisonType))
			{
				var leftOperand = Evaluate(expression.Left);
				var rightOperand = Evaluate(expression.Right);
				var column = _state.LastColumn;

				// Where(item => item == 123) should work same way as Where(item => 123 == item)
				var value = column == leftOperand?.ToString()
					? rightOperand
					: leftOperand;

				if (null == value)
				{
					FilterComparisonType nullComparisonType;
					switch (comparisonType)
					{
						case FilterComparisonType.Equal:
							nullComparisonType = FilterComparisonType.IsNull;
							break;

						case FilterComparisonType.NotEqual:
							nullComparisonType = FilterComparisonType.IsNotNull;
							break;

						default:
							throw new InvalidOperationException($"Cannot apply {comparisonType} operator to NULL argument.");
					}

					comparisonType = nullComparisonType;
				}

				_state.SetComparison(comparisonType, value);

				LogWriter.WriteLine($"Assuming comparison: {leftOperand} {comparisonType} {rightOperand}");
				return;
			}

			if (LogicalOperations.TryGetValue(expression.NodeType, out var logicalOperation))
			{
				using (_state.PushFilter(logicalOperation))
				{
					VisitLogicalOperandExpression(expression.Left);
					VisitLogicalOperandExpression(expression.Right);
				}

				LogWriter.WriteLine($"Assuming logical operation: {logicalOperation}");
				return;
			}

			if (expression.NodeType == ExpressionType.ArrayIndex)
			{
				var index = (int)Evaluate(expression.Right);
				_state.SetColumn(QueryUtils.GetIndexMemberName(index));
				
				return;
			}

			throw new InvalidOperationException($"Operation {expression} is not supported by this LINQ provider.");
		}

		/// <inheritdoc />
		protected override Expression VisitMember(MemberExpression expression)
		{
			LogWriter.WriteLine($"VisitMember: {expression}");

			//  it is not recognizing Length as a method but as a Member, so let's specifically deal with it here I guess
			if (expression.Member.Name == "Length")
			{
				var lastPart = ((MemberExpression)expression.Expression).Member.Name.Split('.').LastOrDefault();

				LogWriter.WriteLine($"Requested: {lastPart}.Length");
			}
			else
			{
				var declaringType = expression.Expression.Type;
				if (!declaringType.IsAnonymousType() && !declaringType.IsLinqGrouping())
				{
					throw new InvalidOperationException($"Unable to evaluate {expression.Expression.Type.Name}.{expression.Member.Name}. " +
					                                $"Only group columns and explicitly defined columns (via Column<T>(\"ColumnName\")) are supported.");
				}

				Visit(expression.Expression);

				var memberName = expression.Member.Name;

				_state.SetColumn(memberName);

				LogWriter.WriteLine($"Requested member: {memberName}");
			}

			return expression;
		}

		/// <inheritdoc />
		protected override Expression VisitConstant(ConstantExpression expression)
		{
			LogWriter.WriteLine($"VisitConstant: {expression}");

			_arguments.Push(expression.Value);

			return expression;
		}

		/// <inheritdoc />
		protected override Expression VisitNew(NewExpression expression)
		{
			LogWriter.WriteLine($"VisitNew: {expression}");

			_state.SetResultConstructor(expression.Constructor);
			
			foreach (var arg in expression.Arguments)
			{
				using (_state.PushColumn())
				{
					LogWriter.WriteLine($"Select new argument: {arg}");
					Visit(arg);
				}
			}


			return expression;
		}

		/// <inheritdoc />
		protected override Expression VisitMethodCall(MethodCallExpression expression)
		{
			LogWriter.WriteLine($"VisitMethodCall: {expression}, method {expression.Method.Name}, args: {string.Join(", ",expression.Arguments.Select(a => a.ToString()))}");

			var method = expression.Method;

			switch (method.Name)
			{
				case "Contains":
				case "StartsWith":
				case "EndsWith":
					var obj = Evaluate(expression.Object);
					var pattern = Evaluate(expression.Arguments[0]);

					_state.SetColumn(obj.ToString());
					_state.SetFunction(method.Name, pattern);

					return expression;

				case "Min":
				case "Max":
				case "Count":
				case "Average":
				case "Sum":
					using (_state.PushCollectorMode(QueryCollectionState.Aggregate))
					{
						object value = null;

						if (expression.Arguments.Count > 1)
						{
							throw new InvalidOperationException($"Unsupported number of aggregate function arguments: {expression.Arguments.Count}");
						}
						if (expression.Arguments.Count > 0)
						{
							value = Evaluate(expression.Arguments[0]);
						}

						_state.SetFunction(method.Name, value);
					}

					return expression;

				case "Column":
					var columnName = Evaluate(expression.Arguments[0]).ToString();

					_arguments.Push(columnName);
					_state.SetColumn(columnName);

					return expression;
			}

			return base.VisitMethodCall(expression); // throws
		}

		/// <inheritdoc />
		protected override Expression VisitUnary(UnaryExpression expression)
		{
			LogWriter.WriteLine($"VisitUnary: {expression}");

			if (expression.NodeType == ExpressionType.Not)
			{
				_state.SetNegative();
			}

			Visit(expression.Operand);

			return expression;
		}

		/// <inheritdoc />
		protected override Expression VisitQuerySourceReference(QuerySourceReferenceExpression expression)
		{
			LogWriter.WriteLine($"VisitQuerySourceReference: {expression}");
			return expression;
		}

		/// <inheritdoc />
		protected override Expression VisitSubQuery(SubQueryExpression expression)
		{
			LogWriter.WriteLine($"VisitSubQuery: {expression}");
			Visit(expression.QueryModel.MainFromClause.FromExpression);

			foreach (var resultOperator in expression.QueryModel.ResultOperators)
			{
				VisitResultOperator(resultOperator);
			}

			Visit(expression.QueryModel.SelectClause.Selector);
			return expression;
		}

		/// <summary>
		/// Processes result operator (overall query aggregation function).
		/// </summary>
		private void VisitResultOperator(ResultOperatorBase resultOperator)
		{
			LogWriter.WriteLine($"VisitResultOperator: {resultOperator}, type {resultOperator.GetType().Name}");

			switch (resultOperator)
			{
				case MinResultOperator _:
					_state.SetFunction("Min", null);
					break;

				case MaxResultOperator _:
					_state.SetFunction("Max", null);
					break;

				case AverageResultOperator _:
					_state.SetFunction("Average", null);
					break;

				case CountResultOperator _:
					_state.SetFunction("Count", null);
					break;

				case SumResultOperator _:
					_state.SetFunction("Sum", null);
					break;

				case ContainsResultOperator containsResult:
					Visit(containsResult.Item);

					var columnPath = (string)_arguments.Pop();
					var value = _arguments.Pop();

					_state.SetColumn(columnPath);
					_state.SetComparison(FilterComparisonType.Equal, value);
					break;

			}
		}

		/// <inheritdoc />
		protected override Expression VisitNewArray(NewArrayExpression expression)
		{
			LogWriter.WriteLine($"VisitNewArray: {expression}");
			foreach (var nestedExpression in expression.Expressions)
			{
				Visit(nestedExpression);
			}

			return expression;
		}

		/// <inheritdoc />
		protected override Expression VisitExtension(Expression expression)
		{
			LogWriter.WriteLine($"VisitExtension: {expression}");
			return expression;
		}


		// Called when a LINQ expression type is not handled above.
		protected override Exception CreateUnhandledItemException<T>(T unhandledItem, string visitMethod)
		{
			string itemText = unhandledItem.ToString();
			var message = $"The expression '{itemText}' (type: {typeof(T)}) is not supported by this LINQ provider.";
			return new NotSupportedException(message);
		}

		/// <summary>
		/// Evaluates expression and returns result.
		/// </summary>
		private object Evaluate(Expression expr)
		{
			Visit(expr);
			
			return _arguments.Pop();
		}

		/// <summary>
		/// Processes logical operation expression.
		/// </summary>
		private void VisitLogicalOperandExpression(Expression expression)
		{
			LogWriter.WriteLine($"VisitLogicalOperand: {expression}");

			if (!LogicalOperations.TryGetValue(expression.NodeType, out _))
			{
				using (_state.PushFilter(LogicalOperationStrict.And))
				{
					Visit(expression);
				}
			}
			else
			{
				Visit(expression);
			}
		}
	}
}