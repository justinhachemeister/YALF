﻿using System;
using System.Collections.Generic;
using Yalf.LogEntries;

namespace Yalf.Reporting.Formatters
{
    public class DelimitedValuesFormatter : ILogFormatter, ISingleLineOutputLogFormatter
    {
        private const String DefaultDelimiter = ",";
        private readonly DefaultFormatter _default;
        private readonly DelayedFormatterService _delayedService;

        public DelimitedValuesFormatter() : this(DefaultFormatter.DefaultIndentChar, DefaultFormatter.DefaultDateTimeFormat, "Yalf", DefaultDelimiter) { }

        public DelimitedValuesFormatter(String logContext, String delimiter) : this(DefaultFormatter.DefaultIndentChar, DefaultFormatter.DefaultDateTimeFormat, logContext, delimiter) { }

        public DelimitedValuesFormatter(Char indentChar, String dateTimeFormatText, String logContext, String delimiter)
        {
            _default = new DefaultFormatter(indentChar, dateTimeFormatText);
            _delayedService = new DelayedFormatterService(dateTimeFormatText);
            this.LogContext = logContext;
            this.Delimiter = delimiter;
        }

        public String Delimiter { get; set; }
        public string LogContext { get; private set; }

        public string DateTimeFormat
        {
            get { return _default.DateTimeFormat; }
        }

        public char IndentChar
        {
            get { throw new NotImplementedException(String.Format("There is no indent for a [{0}]", this.GetType().FullName)); }
        }

        public string Indent(int level)
        {
            return null;
        }

        public string FormatThread(ThreadData logEntry, ILogFilters filters)
        {
            return null;
        }

        public string FormatMethodEntry(int threadId, int level, int lineNo, MethodEntry logEntry, ILogFilters filters, bool displayEnabled)
        {
            // entry details are merged with exit details
            return _delayedService.HandleMethodEntry(logEntry, displayEnabled);
        }

        public string FormatMethodExit(int threadId, int level, int lineNo, MethodExit logEntry, ILogFilters filters, bool displayEnabled)
        {
            throw new NotImplementedException(String.Format("{0} does not need to immplement this method, use the ISingleLineOutputLogFormatter.FormatMethodExitForSingleLineOutput interface method so the calls are in the right order.", this.GetType().Name));
        }

        public IList<OrderedOutput> FormatMethodExitForSingleLineOutput(int threadId, int level, int lineNo, MethodExit logEntry, ILogFilters filters, bool displayEnabled)
        {
            Func<DateTime, String> lineBuilder = null;

            if (displayEnabled)
                lineBuilder = CreateLineBuilderGenerator(threadId, level, logEntry, filters);
            
            return _delayedService.HandleMethodExit(logEntry, lineNo, filters, lineBuilder, displayEnabled);
        }

        private Func<DateTime, string> CreateLineBuilderGenerator(int threadId, int level, MethodExit logEntry, ILogFilters filters)
        {
            var returnValue = (logEntry.ReturnRecorded && !filters.HideMethodReturnValue) ? logEntry.ReturnValue : "";
            return startTime => BuildOutputLine("Method", logEntry.MethodName, returnValue, startTime, logEntry.ElapsedMs, level, threadId);
        }

        public string FormatException(int threadId, int level, int lineNo, ExceptionTrace logEntry, ILogFilters filters)
        {
            var stackTrace = (logEntry.StackTrace == null) ? "" : logEntry.StackTrace.Replace(Environment.NewLine, " ");
            return _delayedService.HandleException(this.BuildOutputLine("Exception", logEntry.Message.Replace(Environment.NewLine, " "), stackTrace, logEntry.Time, 0, level, threadId));
        }

        public string FormatLogEvent(int threadId, int level, int lineNo, LogEvent logEntry, ILogFilters filters, bool displayEnabled)
        {
            if (!displayEnabled)
                return null;

            return _delayedService.HandleLogEvent(this.BuildOutputLine("Log", logEntry.Message, "", logEntry.Time, 0, level, threadId));
        }

        private string BuildOutputLine(string LogType, string title, string details, DateTime timeStamp, double duration, int level, int threadId)
        {
            return String.Join(this.Delimiter,
                new[] { this.LogContext, LogType, title, details, timeStamp.ToString(this.DateTimeFormat), duration.ToString("0.####"), level.ToString(), threadId.ToString() }
            );
        }
    }
}