using System;
using System.Diagnostics;

namespace Creatio.Linq.QueryGeneration.Util
{
	/// <summary>
	/// Switches logging states and toggles logging based on options.
	/// </summary>
	internal static class LogState
	{
		[ThreadStatic] 
		private static LogOptions _options;
		[ThreadStatic]
		private static QueryProcessOperation _operation;
		[ThreadStatic]
		private static Stopwatch _stopwatch;
		[ThreadStatic]
		private static bool _loggingEnabled;

		/// <summary>
		/// Gets if logging is allowed at current moment.
		/// </summary>
		public static bool LoggingEnabled => _loggingEnabled;

		/// <summary>
		/// Gets logging options.
		/// </summary>
		public static LogOptions Options => _options;

		/// <summary>
		/// Begin logging session.
		/// </summary>
		public static IDisposable BeginSession(LogOptions options)
		{
			_options = options ?? throw new ArgumentNullException(nameof(options));

			return new DisposeAction(EndSession);
		}

		/// <summary>
		/// Begin query process operation. 
		/// </summary>
		public static void BeginOperation(QueryProcessOperation operation)
		{
			if(_operation != QueryProcessOperation.None)
			{
				EndOperation();
			}
			
			_operation = operation;
			OnOperationStarted();
		}

		/// <summary>
		/// End query process operation. Automatically called if new operation started.
		/// </summary>
		public static void EndOperation()
		{
			OnOperationFinished();
			_operation = QueryProcessOperation.None;
		}

		private static void EndSession()
		{
			EndOperation();
			_options = LogOptions.None;
		}

		private static void OnOperationStarted()
		{
			switch (_operation)
			{
				case QueryProcessOperation.None:
					LogWriter.EndScope();
					return;

				case QueryProcessOperation.LinqParse:
					if(_options.LogLinqVisitor)
					{
						SetLoggingEnabled(true);
					}

					if (_options.LogLinqVisitorTime)
					{
						StartPerformanceMeasurement();
					}

					return;

				case QueryProcessOperation.EsqGeneration:
					if (_options.LogSqlQuery)
					{
						SetLoggingEnabled(true);
					}
					
					if (_options.LogQueryGenerationTime)
					{
						StartPerformanceMeasurement();
					}

					return;

				case QueryProcessOperation.Executing:
					if (_options.LogQueryExecutionTime)
					{
						StartPerformanceMeasurement();
					}
					
					return;

			}
		}
		
		private static void OnOperationFinished()
		{
			_stopwatch?.Stop();

			switch (_operation)
			{
				case QueryProcessOperation.LinqParse:
					if (_options.LogLinqVisitorTime)
					{
						PrintResult($"*** LINQ parsing time: {_stopwatch?.ElapsedMilliseconds} ms.");
					}
					return;
				
				case QueryProcessOperation.EsqGeneration:
					if (_options.LogQueryGenerationTime)
					{
						PrintResult($"*** ESQ generation time: {_stopwatch?.ElapsedMilliseconds} ms.");
					}

					return;
				
				case QueryProcessOperation.Executing:
					if (_options.LogQueryExecutionTime)
					{
						PrintResult($"*** Query execution time: {_stopwatch?.ElapsedMilliseconds} ms.");
					}
					
					return;
					
			}

			_stopwatch = null;
			LogWriter.EndScope();
		}

		private static void SetLoggingEnabled(bool enabled)
		{
			_loggingEnabled = enabled;
			if (enabled)
			{
				LogWriter.BeginScope(_options.LogAction);
			}
			else
			{
				LogWriter.EndScope();
			}
		}
		
		private static void StartPerformanceMeasurement()
		{
			_stopwatch = new Stopwatch();
			_stopwatch.Start();
		}

		private static void PrintResult(string message)
		{
			Trace.WriteLine("");
			Trace.WriteLine(message);
			Trace.WriteLine("");
		}

	}
}