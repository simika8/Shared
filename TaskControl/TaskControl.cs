using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FluentDateTime;

namespace TaskControl
{
	public class TaskControl
	{

		protected List<TaskControl_TimeRange> TimeRanges { get; set; } = new List<TaskControl_TimeRange>();

		public TimeUnit TimeUnit { get; }
		

		public TaskControl(TimeUnit timeUnit)
		{
			this.TimeUnit = timeUnit;
			//this.TimeType = timeType;
		}


		/// <summary>
		/// Invertálja a Timeranges-t. Olyan időintervallumokat tartalmazó TimeControl -t ad vissza, ami a beadott időintevallumon belül az összes meg nem jelölt időszakot tartalmazza.
		/// </summary>
		public TaskControl GetInverseTimeControl(DateTime dateTime1, DateTime dateTime2, TimeType timeType)
			=>GetInverseTimeControl(TimeRange.CalcTimeRange(dateTime1, dateTime2, timeType, TimeUnit));
		

		/// <summary>
		/// Invertálja a Timeranges-t. Olyan időintervallumokat tartalmazó TimeControl -t ad vissza, ami a beadott dátumok közötti összes meg nem jelölt időszakot tartalmazza.
		/// </summary>
		public TaskControl GetInverseTimeControl(TimeRange timeRange)
		{
			var res = new TaskControl(TimeUnit);

			ReloadData();
			if (TimeUnit == TimeUnit.Infinity)
			{
				if (TimeRanges.Any())
					res.RemoveTimeRange(TimeRange.Infinity());
				else
					res.AddTimeRange(TimeRange.Infinity());
			}
			else
			{
				//Ez az az időpont, amelyik már éppen NINCS benne a vizsgált időszakban
				DateTime realStartTime = TimeFunctions.TruncDateTime(TimeUnit, timeRange.DateTimeStart);
				DateTime realEndTime = timeRange.EndOfTimeRange(TimeUnit);


				/*if (TimeType == TimeType.Moment)//Ha időpontot vizsgálok, akkor a záró időpontot kiterjesztem a megadott időpontot tartalmazó időszak végéig.
					realEndTime = AddDateTime(realEndTime, 1);
				else//Ha időszakot vizsgálok, akkor a kapott endtime paraméterben levő időpontot még intervallumon belülinek veszem, ezért a záró időpontot kiterjesztem 1 legkisebb időegységgel.
					realEndTime = realEndTime/*.AddTicks(1)*/
				;
				//realEndTime = 

				var trInInterval = TimeRanges.Where(x => x.EndOfTimeRange(TimeUnit) > realStartTime && x.DateTimeStart < realEndTime).OrderBy(x => x.DateTimeStart);


				//első időszak előtti rész
				var first = trInInterval.FirstOrDefault();
				if (first != null && first.DateTimeStart > realStartTime)
				{
					var tr = newTaskControl_TimeRange(realStartTime, TimeCountBetween(realStartTime, first.DateTimeStart));
					res.TimeRanges.Add(tr);

				}


				TaskControl_TimeRange? prevTimeRange = null;

				//hozzáadom az egyes időintervallumok között kihagyott időszakokat
				foreach (var actTimeRange in trInInterval)
				{
					if (prevTimeRange != null)
					{
						var starttime = prevTimeRange.EndOfTimeRange(TimeUnit);
						var tr = newTaskControl_TimeRange(starttime, TimeCountBetween(starttime, actTimeRange.DateTimeStart));

						if (tr.DurationCount > 0)
							res.TimeRanges.Add(tr);
					}
					prevTimeRange = actTimeRange;
				}

				//hozzáadom az utolsó utáni-t
				var last = trInInterval.LastOrDefault();
				if (last != null && AddDateTime(last.DateTimeStart, last.DurationCount) < realEndTime)
				{
					var starttime = last.EndOfTimeRange(TimeUnit);
					var tr = newTaskControl_TimeRange(starttime, TimeCountBetween(starttime, realEndTime));
					if (tr.DurationCount > 0)
						res.TimeRanges.Add(tr);
				}

				if (!trInInterval.Any())
				{
					var tr = newTaskControl_TimeRange(realStartTime, TimeCountBetween(realStartTime, realEndTime));
					res.TimeRanges.Add(tr);
				}
			}
			return res;
		}

		/// <summary>
		/// visszaadja a beállított időintervallumokat
		/// </summary>
		public IEnumerable<TimeRange> GetTimeRanges()
		{
			ReloadData();
			foreach (var actTimeRange in TimeRanges)
			{
				yield return actTimeRange;
			}
		}

		/// <summary>
		/// visszaadja a beállított időintervallumokat úgy, hogy szétdarabolja őket TimeUnit alapján
		/// </summary>
		public IEnumerable<TimeRange> GetDetailedTimeRanges()
		{
			ReloadData();
			if (TimeUnit == TimeUnit.Infinity)
			{
				yield return TimeRanges.First();
			}
			else
			{

				foreach (var actTimeRange in TimeRanges)
				{
					if (actTimeRange.DurationCount == 1)
						yield return actTimeRange;
					else
					{
						for (var i = 0; i < actTimeRange.DurationCount; i++)
						{
							var tr = new TimeRange(AddDateTime(actTimeRange.DateTimeStart, i), 1, TimeUnit);
							yield return tr;
						}
					}
				}
			}
		}

		
		public string TimeRangesToString(bool showEndTimes = false, TimeType timeType = TimeType.Interval)
		{
			ReloadData();
			if (TimeUnit == TimeUnit.Infinity)
			{
				return TimeRanges.Any().ToString();
			}
			else
			{
				return TimeFunctions.TimeRangesToString(TimeRanges, TimeUnit, timeType, showEndTimes);
			}
		}


		/// <summary>
		/// Az adott időintervallumot hozzáadja a listában szereplőkhöz. Ha összeér, vagy belelóg egy vagy több már megjelölt időintervallumba, akkor összevonja az egybeérő időintervallumokat.
		/// </summary>
		public void AddTimeRange(TimeRange timeRange)
		{
			ReloadData();

			if (TimeUnit == TimeUnit.Infinity)
			{
				if (!TimeRanges.Any())
					AddTimeRangeToList(TaskControl_TimeRange.Infinity());

			}
			else
			{

				var tcTimeRange = newTaskControl_TimeRange(timeRange.DateTimeStart, timeRange.DurationCount);

				/*var prev = TimeRanges.Where(x => x.DateTimeStart <= timeRange.DateTimeStart).OrderByDescending(x => x.DateTimeStart).FirstOrDefault();
				var next = TimeRanges.Where(x => x.DateTimeStart >= timeRange.DateTimeStart).OrderBy(x => x.DateTimeStart).FirstOrDefault();
				AddTimeRangeToList(timeRange);

				//előző bejegyzéshez vizsgálat+mergelés
				MergeInList(timeRange, prev);

				//következő bejegyzéshez vizsgálat+mergelés
				MergeInList(timeRange, next);*/


				//kitörlöm az összes intervallumot, amit teljesen befed a hozzáadandó intervallum.
				var toDelete = TimeRanges
					.Where(x => x.DateTimeStart >= tcTimeRange.DateTimeStart && x.EndOfTimeRange(TimeUnit) <= tcTimeRange.EndOfTimeRange(TimeUnit))
					.ToList();
				foreach (var tr in toDelete)
				{
					RemoveTimeRangeFromList(tr);
				}

				//megnézem, van e olyan intervallum, amit a hozzáadandó időszak kezdetével összeér. 
				var startIntersecting = TimeRanges
					.Where(x => x.DateTimeStart <= tcTimeRange.DateTimeStart && x.EndOfTimeRange(TimeUnit) >= tcTimeRange.DateTimeStart)
					.FirstOrDefault();
				//megnézem, van e olyan intervallum, amit a hozzáadandó időszak végével összeér.
				var endIntersecting = TimeRanges
					.Where(x => x.DateTimeStart <= tcTimeRange.EndOfTimeRange(TimeUnit) && x.EndOfTimeRange(TimeUnit) >= tcTimeRange.EndOfTimeRange(TimeUnit))
					.FirstOrDefault();

				var worktr = newTaskControl_TimeRange(tcTimeRange.DateTimeStart, tcTimeRange.DurationCount);
				// Hozzáadoma listához az új időintervallumot
				AddTimeRangeToList(worktr);

				//megnézem, van e olyan intervallum, amit a hozzáadandó időszak kezdetével összeér. Ha van ilyen, akkor összevonom a hozzáadandó időszakkal.
				var mergedtr = MergeInList(worktr, startIntersecting);

				//megnézem, van e olyan intervallum, amit a hozzáadandó időszak végével összeér. Ha van ilyen, akkor összevonom a hozzáadandó időszakkal.
				MergeInList(mergedtr, endIntersecting);
			}

			//Az új időintervallumot felveszem a listába
			SaveChanges();
		}

		/// <summary>
		/// Az adott időintervallumot kiveszi a listából.
		/// </summary>
		public void RemoveTimeRange(TimeRange timeRange)
		{
			ReloadData();

			if (TimeUnit == TimeUnit.Infinity)
			{
				foreach (var tr in TimeRanges.ToList())
					RemoveTimeRangeFromList(tr);
			}
			else
			{
				var tcTimeRange = newTaskControl_TimeRange(timeRange.DateTimeStart, timeRange.DurationCount);
				//1. megnézem, van e olyan intervallum, amit a törlendő időszak kezdetével összeér. Ha van ilyen, kettébontom 2 időszakra.

				var startIntersecting = TimeRanges
					.Where(x => x.DateTimeStart <= tcTimeRange.DateTimeStart && x.EndOfTimeRange(TimeUnit) >= tcTimeRange.DateTimeStart)
					.FirstOrDefault();
				SplitInList(startIntersecting, tcTimeRange.DateTimeStart);
				//2. megnézem, van e olyan intervallum, amit a törlendő időszak végével összeér. Ha van ilyen, kettébontom 2 időszakra.
				var entIntersecting = TimeRanges
					.Where(x => x.DateTimeStart <= tcTimeRange.EndOfTimeRange(TimeUnit) && x.EndOfTimeRange(TimeUnit) >= tcTimeRange.EndOfTimeRange(TimeUnit))
					.FirstOrDefault();
				SplitInList(entIntersecting, tcTimeRange.EndOfTimeRange(TimeUnit));

				//3. kitörlöm az összes intervallumot, amit teljesen befed a törlendő intervallum.
				var toDelete = TimeRanges
					.Where(x => x.DateTimeStart >= tcTimeRange.DateTimeStart && x.EndOfTimeRange(TimeUnit) <= tcTimeRange.EndOfTimeRange(TimeUnit))
					.ToList();
				foreach (var tr in toDelete)
				{
					RemoveTimeRangeFromList(tr);
				}
			}
			//Az új időintervallumot felveszem a listába
			SaveChanges();
		}


		protected virtual void ReloadData()
		{

		}

		protected virtual void SaveChanges()
		{
			TimeRanges = TimeRanges.OrderBy(x => x.DateTimeStart).ToList();
		}

		protected virtual void AddTimeRangeToList(TaskControl_TimeRange? timeRange)
		{
			if (timeRange == null)
				return;

			TimeRanges.Add(timeRange);
		}
		//private void AddTimeRangeToList(TimeRange timeRange) => AddTimeRangeToList(new TaskControl_TimeRange(timeRange.DateTimeStart, timeRange.DurationCount));

		protected virtual void RemoveTimeRangeFromList(TaskControl_TimeRange timeRangeToMerge)
		{
			TimeRanges.Remove(timeRangeToMerge);
		}


		private TaskControl_TimeRange? MergeInList(TaskControl_TimeRange? timeRange1, TaskControl_TimeRange? timeRange2)
		{
			if (timeRange1 == null)
				return timeRange2;
			if (timeRange2 == null)
				return timeRange1;
			//ha az időintervallumok nem érnek össze
			if (!Mergeble(timeRange1, timeRange2))
				return null;

			//kiszedem a listából
			RemoveTimeRangeFromList(timeRange1);
			RemoveTimeRangeFromList(timeRange2);

			//az új időintervallumot kiterjesztem a mergelendő időszakkal
			var mergedTimeRange = Merge(timeRange1, timeRange2);

			AddTimeRangeToList(mergedTimeRange);
			return mergedTimeRange;
		}
		private void SplitInList(TaskControl_TimeRange timeRange, DateTime splittime)
		{
			if (timeRange == null)
				return;

			//ha az időintervallumok összeérnek
			if (Splitable(timeRange, splittime))
			{
				//kiszedem a listából
				RemoveTimeRangeFromList(timeRange);

				//szétbontom
				var splittedTimeRanges = Split(timeRange, splittime);

				//felveszem listába
				AddTimeRangeToList(splittedTimeRanges.Item1);

				AddTimeRangeToList(splittedTimeRanges.Item2);
			}
		}






		private bool Splitable(TaskControl_TimeRange timeRange, DateTime splittime)
		{
			return splittime > timeRange.DateTimeStart && splittime < timeRange.EndOfTimeRange(TimeUnit);
		}
		private Tuple<TaskControl_TimeRange, TaskControl_TimeRange?> Split(TaskControl_TimeRange timeRange, DateTime splittime)
		{
			if (!Splitable(timeRange, splittime))
				throw new Exception("Cannot split");

			TaskControl_TimeRange timeRange1 = newTaskControl_TimeRange(timeRange.DateTimeStart, TimeCountBetween(timeRange.DateTimeStart, splittime));

			TaskControl_TimeRange timeRange2 = newTaskControl_TimeRange(splittime, TimeCountBetween(splittime, timeRange.EndOfTimeRange(TimeUnit)));

			return new Tuple<TaskControl_TimeRange, TaskControl_TimeRange?>(timeRange1, timeRange2);
		}

		private bool Mergeble(TaskControl_TimeRange timeRange1, TaskControl_TimeRange timeRange2)
		{
			//ha frodított a sorrend, akkor megfordítva hívom meg
			if (timeRange2.DateTimeStart < timeRange1.DateTimeStart)
				return Mergeble(timeRange2, timeRange1);

			return timeRange1.EndOfTimeRange(TimeUnit) >= timeRange2.DateTimeStart;
		}
		private TaskControl_TimeRange Merge(TaskControl_TimeRange timeRange1, TaskControl_TimeRange timeRange2)
		{

			//ha frodított a sorrend, akkor megfordítva hívom meg
			if (timeRange2.DateTimeStart < timeRange1.DateTimeStart)
				return Merge(timeRange2, timeRange1);


			var timeRange1End = timeRange1.EndOfTimeRange(TimeUnit);
			var timeRange2End = timeRange2.EndOfTimeRange(TimeUnit);


			//timeRange1 teljesen befedi timeRange2-t, akkor timeRange1-et adom vissza
			if (timeRange1End >= timeRange2End)
				return timeRange1;


			if (timeRange1End < timeRange2.DateTimeStart)
			{//Ha nem érnek össze az időszakok, akkor nem lehet mergelni
				throw new Exception($"Merge Error.");
			}
			else
			{//ha egymásra lógnak, vagy összeérnek az időszakok
				var res = newTaskControl_TimeRange(timeRange1.DateTimeStart, TimeCountBetween(timeRange1.DateTimeStart, timeRange2End));
				return res;
			}
		}

		private long TimeCountBetween(DateTime dateTime1, DateTime dateTime2) => TimeFunctions.TimeCountBetween(TimeUnit, dateTime1, dateTime2);

		private DateTime AddDateTime(DateTime dateTime, long count) => TimeFunctions.AddDateTime(TimeUnit, dateTime, count);

		private TaskControl_TimeRange newTaskControl_TimeRange(DateTime dateTimeStart, long durationCount) => new TaskControl_TimeRange(dateTimeStart, durationCount, TimeUnit);

	}
}
