using System;
using System.Collections.Generic;
using System.Text;
using FluentDateTime;

namespace TaskControl
{
	internal static class TimeFunctions
	{
		internal static DateTime AddDateTime(TimeUnit timeUnit, DateTime dateTime, long count)
		{
			var datetimetrunced = TruncDateTime(timeUnit, dateTime);


			switch (timeUnit)
			{
				case TimeUnit.Week:
					return datetimetrunced.AddDays(7 * count);
				case TimeUnit.Month:
					return datetimetrunced.AddMonths(1 * (int)count);
				case TimeUnit.Year:
					return datetimetrunced.AddYears(1 * (int)count);
				case TimeUnit.Day:
					return datetimetrunced.AddDays(1 * count);
				case TimeUnit.Hour:
					return datetimetrunced.AddHours(1 * count);
				case TimeUnit.Minute:
					return datetimetrunced.AddMinutes(1 * count);
				case TimeUnit.Second:
					return datetimetrunced.AddSeconds(1 * count);
				default:
					throw new NotImplementedException($"{timeUnit.ToString()} esetén a függvény nem használható.");
			}
		}


		internal static DateTime TruncDateTime(TimeUnit timeUnit, DateTime dateTime)
		{
			switch (timeUnit)
			{
				case TimeUnit.Week:
					return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day).Previous(DayOfWeek.Monday);
				case TimeUnit.Month:
					return new DateTime(dateTime.Year, dateTime.Month, 1);
				case TimeUnit.Year:
					return new DateTime(dateTime.Year, 1, 1);
				case TimeUnit.Day:
					return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day);
				case TimeUnit.Hour:
					return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, 0, 0);
				case TimeUnit.Minute:
					return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, dateTime.Minute, 0);
				case TimeUnit.Second:
					return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, dateTime.Minute, dateTime.Second);
				default:
					throw new NotImplementedException($"{timeUnit.ToString()} esetén a függvény nem használható.");
			}
		}

		internal static DateTime RoundUpDateTime(TimeUnit timeUnit, DateTime dateTime)
		{
			if (dateTime == TruncDateTime(timeUnit, dateTime))
				return dateTime;
			return AddDateTime(timeUnit, dateTime, 1);
		}

		internal static long TimeCountBetween(TimeUnit timeUnit, DateTime dateTime1, DateTime dateTime2)
		{
			//ha frodított a sorrend, akkor megfordítva hívom meg
			if (dateTime2 < dateTime1)
				return TimeCountBetween(timeUnit, dateTime2, dateTime1);

			TimeSpan span = dateTime2.AddTicks(1).Subtract(dateTime1);
			switch (timeUnit)
			{
				case TimeUnit.Week:
				case TimeUnit.Month:
				case TimeUnit.Year:
					//case TimeUnit.Day:

					long count = 0;
					for (count = 0; AddDateTime(timeUnit, dateTime1, count) <= dateTime2; count++)
					{
						var a2 = AddDateTime(timeUnit, dateTime1, count);
						;
					}
					var a = AddDateTime(timeUnit, dateTime1, count);
					return count - 1;
				case TimeUnit.Day:
					return (long)Math.Truncate(span.TotalDays);
				case TimeUnit.Hour:
					return (long)Math.Truncate(span.TotalHours);
				case TimeUnit.Minute:
					return (long)Math.Truncate(span.TotalMinutes);
				case TimeUnit.Second:
					return (long)Math.Truncate(span.TotalSeconds);
				default:
					throw new NotImplementedException($"{timeUnit.ToString()} esetén a függvény nem használható.");
			}
		}
		internal static string TimeRangesToString(IEnumerable<TimeRange> timeRanges, TimeUnit timeUnit = TimeUnit.Day, TimeType timeType = TimeType.Interval, bool showEndTimes = false)
		{
			var sb = new StringBuilder();

			foreach (var tr in timeRanges)
			{
				if (showEndTimes)
				{
					if (timeType == TimeType.Interval)
						sb.AppendLine($"{tr.DateTimeStart.ToString("yyyy.MM.dd HH:mm:ss.fff")} - {tr.RealEndOfTimeRange(timeUnit, timeType).ToString("yyyy.MM.dd HH:mm:ss.fff")}	({ tr.DurationCount})");
					else
						sb.AppendLine($"{tr.DateTimeStart.ToString("yyyy.MM.dd HH:mm:ss")} ({ tr.DurationCount})");
				} else
					sb.AppendLine($"{tr.DateTimeStart.ToString("yyyy.MM.dd HH:mm:ss")} ({ tr.DurationCount})");
			}
			return sb.ToString();
		}
	}
}
