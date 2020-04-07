using System;

namespace TaskControl
{
	public class TimeRange
	{
		public DateTime DateTimeStart { get; private set; }
		public long DurationCount { get; private set; }
		public TimeUnit TimeUnit { get; private set; }
		public TimeRange(DateTime dateTimeStart, long durationCount, TimeUnit timeUnit)
		{
			DateTimeStart = TimeFunctions.TruncDateTime(TimeUnit, dateTimeStart);
			DurationCount = durationCount;
			this.TimeUnit = timeUnit;
		}
		public static TimeRange Infinity() => new TimeRange(DateTime.MinValue, long.MaxValue, TimeUnit.Infinity);


		public static TimeRange CalcTimeRange(DateTime dateTime1, DateTime dateTime2, TimeType timeType, TimeUnit timeUnit)
		{
			if (dateTime1 > dateTime2)
				return CalcTimeRange(dateTime2, dateTime1, timeType, timeUnit);

			if (timeType == TimeType.Moment)
			{
				dateTime1 = TimeFunctions.RoundUpDateTime(timeUnit, dateTime1);
				dateTime2 = TimeFunctions.AddDateTime(timeUnit, TimeFunctions.TruncDateTime(timeUnit, dateTime2), 1);
			}
			else
			{
				dateTime1 = TimeFunctions.TruncDateTime(timeUnit, dateTime1);
				dateTime2 = dateTime2.AddTicks(-1);
			}
			long durationCount = TimeFunctions.TimeCountBetween(timeUnit, dateTime1, dateTime2);
			TimeRange timeRange = new TimeRange(dateTime1, durationCount, timeUnit);
			return timeRange;
		}


		/// <summary>
		/// Az időintervallum végét adja vissza felfelé kerekítve a következő időszakasz kezdetére.
		/// </summary>
		public DateTime EndOfTimeRange(TimeUnit timeUnit)
		{
			return TimeFunctions.AddDateTime(timeUnit, this.DateTimeStart, this.DurationCount);
		}

		public DateTime RealEndOfTimeRange(TimeUnit timeUnit, TimeType timetype)
		{
			if (timetype == TimeType.Moment)//Ha időpontot vizsgálok, akkor a záró időpontot kiterjesztem a megadott időpontot tartalmazó időszak végéig.
				return TimeFunctions.AddDateTime(timeUnit, EndOfTimeRange(timeUnit), -1).AddTicks(1);
			else//Ha időszakot vizsgálok, akkor a kapott endtime paraméterben levő időpontot még intervallumon belülinek veszem, ezért a záró időpontot kiterjesztem 1 legkisebb időegységgel.
				return EndOfTimeRange(timeUnit).AddTicks(-1);
		}
	}
}
