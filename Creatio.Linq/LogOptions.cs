using System;
using System.Diagnostics;

namespace Creatio.Linq
{
	/// <summary>
	/// If you want to debug query generation process.
	/// </summary>
	public class LogOptions
	{
		/// <summary>
		/// When set to true dumps all re-linq visitor events to <see cref="LogAction"/>.
		/// </summary>
		public bool LogLinqVisitor { get; set; }
		
		/// <summary>
		/// When set to true aggregated query parts will be dumped to <see cref="LogAction"/>, serialized to JSON.
		/// </summary>
		public bool LogAggregatedParts { get; set; }
		
		/// <summary>
		/// When set to true final SQL query will be dumped to <see cref="LogAction"/>.
		/// </summary>
		public bool LogSqlQuery { get; set; }


		/// <summary>
		/// When set to true measures time taken by LINQ parsing process and dumps it to <see cref="LogAction"/>.
		/// </summary>
		public bool LogLinqVisitorTime { get; set; }

		/// <summary>
		/// When set to true measures time taken to generate ESQ and dumps it to <see cref="LogAction"/>.
		/// </summary>
		public bool LogQueryGenerationTime { get; set; }

		/// <summary>
		/// When set to true measures time taken to execute ESQ and dumps it to <see cref="LogAction"/>.
		/// </summary>
		public bool LogQueryExecutionTime { get; set; }

		/// <summary>
		/// Called when new log item is about to be written.
		/// </summary>
		public Action<string> LogAction { get; set; }

		/// <summary>
		/// Creates default <see cref="LogOptions"/> which dumps everything to standard trace.
		/// </summary>
		public static LogOptions ToTrace =>
			new LogOptions
			{
				LogLinqVisitor = true,
				LogAggregatedParts = true,
				LogSqlQuery = true,
				LogLinqVisitorTime = true,
				LogQueryGenerationTime = true,
				LogQueryExecutionTime = true,
				LogAction = message => Trace.WriteLine(message)
			};

		/// <summary>
		/// Creates default <see cref="LogOptions"/> which dumps performance timings to standard trace.
		/// </summary>
		public static LogOptions ToTracePerformanceOnly =>
			new LogOptions
			{
				LogLinqVisitorTime = true,
				LogQueryGenerationTime = true,
				LogQueryExecutionTime = true,
				LogAction = message => Trace.WriteLine(message)
			};

		/// <summary>
		/// Empty log options
		/// </summary>
		internal static LogOptions None =>
			new LogOptions
			{
				LogAction = message => { }
			};
	}
}