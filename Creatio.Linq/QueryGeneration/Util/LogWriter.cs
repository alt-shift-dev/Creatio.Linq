using System;
using System.Linq;

namespace Creatio.Linq.QueryGeneration.Util
{
	/// <summary>
	/// Allows to toggle query debug logging.
	/// </summary>
	internal static class LogWriter
	{
		[ThreadStatic]
		private static Action<string> _messageWriter;
		
		[ThreadStatic]
		private static int _indentLevel = 0;
		
		/// <summary>
		/// Enables forwarding log messages from <see cref="WriteLine"/> to <see cref="messageWriter"/>.
		/// </summary>
		public static void BeginScope(Action<string> messageWriter)
		{
			_messageWriter = messageWriter;
		}

		/// <summary>
		/// Disables forwarding log messages.
		/// </summary>
		public static void EndScope()
		{
			_messageWriter = null;
			_indentLevel = 0;
		}

		/// <summary>
		/// Write log line.
		/// </summary>
		public static void WriteLine(string message)
		{
			if (string.IsNullOrEmpty(message) || null == _messageWriter)
			{
				return;
			}
			
			if (message.StartsWith("<-") && _indentLevel > 0) _indentLevel--;

			var indent = string.Concat(Enumerable.Repeat("  ", _indentLevel));
			_messageWriter.Invoke(indent + message);

			if (message.StartsWith("->")) _indentLevel++;
		}
	}
}