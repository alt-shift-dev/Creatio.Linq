using System;
using System.Collections.Generic;
using System.Diagnostics;
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

		public static void SetupQueryParts(Expression linqExpression, QueryCollectorState state)
		{
			var visitor = new EntitySchemaQueryExpressionTreeVisitor(state);
			visitor.Visit(linqExpression);
		}

		public EntitySchemaQueryExpressionTreeVisitor(QueryCollectorState state)
		{
			_state = state ?? throw new ArgumentNullException(nameof(state));
		}

		protected override Expression VisitBinary(BinaryExpression expression)
		{
			Trace.WriteLine($"VisitBinary: {expression}");
			ConvertBinaryExpression(expression);
			Trace.WriteLine("End VisitBinary");
			return expression;
		}

		private void ConvertBinaryExpression(BinaryExpression expression)
		{
			Trace.WriteLine($"ConvertBinary: [{expression.Left}], operation {expression.NodeType}, [{expression.Right}]");

			if (ComparisonMappings.TryGetValue(expression.NodeType, out var comparisonType))
			{
				var leftOperand = Evaluate(expression.Left);
				var rightOperand = Evaluate(expression.Right);
				var column = _state.LastColumn;

				// Where(item => item == 123) should work same way as Where(item => 123 == item)
				var value = column == leftOperand.ToString()
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

				Trace.WriteLine($"Assuming comparison: {leftOperand} {comparisonType} {rightOperand}");
				return;
			}

			if (LogicalOperations.TryGetValue(expression.NodeType, out var logicalOperation))
			{
				using (_state.PushFilter(logicalOperation))
				{
					VisitLogicalOperandExpression(expression.Left);
					VisitLogicalOperandExpression(expression.Right);
				}

				Trace.WriteLine($"Assuming logical operation: {logicalOperation}");
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

		protected override Expression VisitMember(MemberExpression expression)
		{
			Trace.WriteLine($"VisitMember: {expression}");

			//  it is not recognizing Length as a method but as a Member, so let's specifically deal with it here I guess
			if (expression.Member.Name == "Length")
			{
				var lastPart = ((MemberExpression)expression.Expression).Member.Name.Split('.').LastOrDefault();

				Trace.WriteLine($"Requested: {lastPart}.Length");
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

				Trace.WriteLine($"Requested member: {memberName}");
			}

			return expression;
		}

		protected override Expression VisitConstant(ConstantExpression expression)
		{
			Trace.WriteLine($"VisitConstant: {expression}");

			_arguments.Push(expression.Value);

			return expression;
		}

		protected override Expression VisitNew(NewExpression expression)
		{
			Trace.WriteLine($"VisitNew: {expression}");

			_state.SetResultConstructor(expression.Constructor);
			// for new { c.Name, c.Title, c.Title.Length }

			// expression.Members has all the property names of the anonymous type
			//  e.g. String Name, String Title, Int32 Length

			// expression.Arguments has the expressions for getting the value, so these need to be run through the Visit stuff to get their 
			//  e.g. [10001].Name, [10001].Title, [10001].Title.Length

			foreach (var arg in expression.Arguments)
			{
				using (_state.PushColumn())
				{
					Trace.WriteLine($"Select new argument: {arg}");
					Visit(arg);
				}
			}


			return expression;
		}


		protected override Expression VisitMethodCall(MethodCallExpression expression)
		{
			Trace.WriteLine($"VisitMethodCall: {expression}, method {expression.Method.Name}, args: {string.Join(", ",expression.Arguments.Select(a => a.ToString()))}");

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

		protected override Expression VisitUnary(UnaryExpression expression)
		{
			Trace.WriteLine($"VisitUnary: {expression}");

			if (expression.NodeType == ExpressionType.Not)
			{
				_state.SetNegative();
			}

			Visit(expression.Operand);

			return expression;
		}

		protected override Expression VisitQuerySourceReference(QuerySourceReferenceExpression expression)
		{
			Trace.WriteLine($"VisitQuerySourceReference: {expression}");
			return expression;
		}

		protected override Expression VisitSubQuery(SubQueryExpression expression)
		{
			Trace.WriteLine($"VisitSubQuery: {expression}");
			Visit(expression.QueryModel.MainFromClause.FromExpression);

			foreach (var resultOperator in expression.QueryModel.ResultOperators)
			{
				VisitResultOperator(resultOperator);
			}

			Visit(expression.QueryModel.SelectClause.Selector);
			return expression;
		}

		private void VisitResultOperator(ResultOperatorBase resultOperator)
		{
			Trace.WriteLine($"VisitResultOperator: {resultOperator}, type {resultOperator.GetType().Name}");

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

		protected override Expression VisitNewArray(NewArrayExpression expression)
		{
			Trace.WriteLine($"VisitNewArray: {expression}");
			foreach (var nestedExpression in expression.Expressions)
			{
				Visit(nestedExpression);
			}

			return expression;
		}

		protected override Expression VisitExtension(Expression expression)
		{
			Trace.WriteLine($"VisitExtension: {expression}");
			return expression;
		}


		// Called when a LINQ expression type is not handled above.
		protected override Exception CreateUnhandledItemException<T>(T unhandledItem, string visitMethod)
		{
			string itemText = FormatUnhandledItem(unhandledItem);
			var message = string.Format("The expression '{0}' (type: {1}) is not supported by this LINQ provider.", itemText, typeof(T));
			return new NotSupportedException(message);
		}

		private static string FormatUnhandledItem<T>(T unhandledItem)
		{
			var itemAsExpression = unhandledItem as Expression;
			return /*itemAsExpression != null ? FormattingExpressionTreeVisitor.Format(itemAsExpression) :*/ unhandledItem.ToString();
		}

		private object Evaluate(Expression expr)
		{
			Visit(expr);
			
			return _arguments.Pop();
		}

		private void VisitLogicalOperandExpression(Expression expression)
		{
			Trace.WriteLine($"VisitLogicalOperand: {expression}");

			if (!LogicalOperations.TryGetValue(expression.NodeType, out _))
			{
				Trace.WriteLine("NOTE!!! Correct refactoring here.");
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