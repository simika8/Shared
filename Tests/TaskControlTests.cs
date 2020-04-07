using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TaskControl
{
	[TestClass]
	public class TaskControlTests
	{
		private TimeUnit TimeUnit { get; set; } = TimeUnit.Day;
		private TimeRange tr1 { get => new TimeRange(new DateTime(2020, 03, 02), 2, TimeUnit); }
		private TimeRange tr2 { get => new TimeRange(new DateTime(2020, 03, 05), 2, TimeUnit); }
		private TimeRange tr3 { get => new TimeRange(new DateTime(2020, 03, 08), 2, TimeUnit); }
		private TimeRange tr4 { get => new TimeRange(new DateTime(2020, 03, 11), 2, TimeUnit); }
		private TimeRange tr5 { get => new TimeRange(new DateTime(2020, 03, 14), 2, TimeUnit); }

		private TimeRange tr6 { get => new TimeRange(new DateTime(2020, 03, 5), 8, TimeUnit); }
		private TimeRange tr7 { get => new TimeRange(new DateTime(2020, 03, 6), 6, TimeUnit); }
		private TimeRange tr8 { get => new TimeRange(new DateTime(2020, 03, 4), 10, TimeUnit); }


		private TimeRange trfull { get => new TimeRange(new DateTime(2020, 03, 01), 16, TimeUnit); }
		private TimeRange trhalf { get => new TimeRange(new DateTime(2020, 03, 01), 8, TimeUnit); }

		public void InitTaskControl(TaskControl tc)
		{
			tc.RemoveTimeRange(trfull);
			tc.AddTimeRange(tr1);
			tc.AddTimeRange(tr2);
			tc.AddTimeRange(tr3);
			tc.AddTimeRange(tr4);
			tc.AddTimeRange(tr5);
		}

		private int AssertFunc(TaskControl tc, string expected, int errorNumber)
		{
			Assert.AreEqual("\r\n" + expected, "\r\n" + tc.TimeRangesToString(), $"Error {errorNumber.ToString()}");
			return 0;
		}

		[TestInitialize]
		public void TestInitialize()
		{
			using var db = new TaskControlTestContext();
			db.TaskControl_Heads.RemoveRange(db.TaskControl_Heads.Where(x => x.Domain == Domain.TestMoment).ToList());
			db.SaveChanges();
		}


		[TestMethod]
		public void AddRemoveTimeRange()
		{
			var tc = new TaskControl(TimeUnit.Day);

			AddRemoveTimeRangeCore(tc, AssertFunc);
		}

		[TestMethod]
		public void DbMomentAddRemoveTimeRange()
		{

			using var db = new TaskControlTestContext();

			var tc = new DbTaskControl(db, TimeUnit.Day, TimeType.Moment, Domain.TestMoment);

			int AssertFuncLoad(TaskControl tc, string expected, int errorNumber)
			{
				var tcLoaded = new DbTaskControl(db, TimeUnit.Day, TimeType.Moment, Domain.TestMoment);
				return AssertFunc(tcLoaded, expected, errorNumber);
			}
			AddRemoveTimeRangeCore(tc, AssertFuncLoad);
		}

		[TestMethod]
		public void DbIntervalAddRemoveTimeRange()
		{

			using var db = new TaskControlTestContext();

			var tc = new DbTaskControl(db, TimeUnit.Day, TimeType.Interval, Domain.TestInterval);

			int AssertFuncLoad(TaskControl tc, string expected, int errorNumber)
			{
				var tcLoaded = new DbTaskControl(db, TimeUnit.Day, TimeType.Interval, Domain.TestInterval);
				return AssertFunc(tcLoaded, expected, errorNumber);
			}
			AddRemoveTimeRangeCore(tc, AssertFuncLoad);
		}


		[TestMethod]
		public void AAADbInit()
		{
		}

		[TestMethod]
		public void IntervalInvert()
		{
			InvertCore(new TaskControl(TimeUnit.Day), TimeType.Interval);
		}

		[TestMethod]
		public void MomentInvert()
		{
			InvertCore(new TaskControl(TimeUnit.Day), TimeType.Moment);
		}

		[TestMethod]
		public void InvertHalfRangeTime1()
		{
			var dt1 = new DateTime(2020, 03, 13, 00, 00, 01);
			var dt2 = new DateTime(2020, 03, 16, 00, 00, 01);
			InvertHalfRangeTimeCore(dt1, dt2);
		}

		[TestMethod]
		public void InvertHalfRangeTime2()
		{
			var dt1 = new DateTime(2020, 03, 13, 12, 00, 00);
			var dt2 = new DateTime(2020, 03, 16, 12, 00, 00);
			InvertHalfRangeTimeCore(dt1, dt2);
		}

		[TestMethod]
		public void InvertHalfRangeTime3()
		{
			var dt1 = new DateTime(2020, 03, 13, 23, 59, 59);
			var dt2 = new DateTime(2020, 03, 16, 23, 59, 59);
			InvertHalfRangeTimeCore(dt1, dt2);
		}

		[TestMethod]
		public void InvertHalfRangeTime4()
		{
			var dt1 = new DateTime(2020, 03, 13, 00, 00, 01);
			var dt2 = new DateTime(2020, 03, 16, 23, 59, 59);
			InvertHalfRangeTimeCore(dt1, dt2);
		}

		[TestMethod]
		public void InvertHalfRangeTime5()
		{
			var dt1 = new DateTime(2020, 03, 13, 23, 59, 59);
			var dt2 = new DateTime(2020, 03, 16, 00, 00, 01);
			InvertHalfRangeTimeCore(dt1, dt2);
		}

		public void InvertHalfRangeTimeCore(DateTime dt1, DateTime dt2)
		{
			var tcMoment = new TaskControl(TimeUnit.Day);
			var tcInterval = new TaskControl(TimeUnit.Day);
			TaskControl tci;

			string expected;

			InitTaskControl(tcMoment);
			tci = tcMoment.GetInverseTimeControl(dt1, dt2, TimeType.Moment);
			expected = "2020.03.16 00:00:00 (1)\r\n";
			AssertFunc(tci, expected, 1);

			InitTaskControl(tcInterval);
			tci = tcInterval.GetInverseTimeControl(dt1, dt2, TimeType.Interval);
			expected = "2020.03.13 00:00:00 (1)\r\n";
			AssertFunc(tci, expected, 2);

			InitTaskControl(tcMoment);
			tcMoment.RemoveTimeRange(tr5);
			tci = tcMoment.GetInverseTimeControl(dt1, dt2, TimeType.Moment);
			expected = "2020.03.14 00:00:00 (3)\r\n";
			AssertFunc(tci, expected, 3);

			InitTaskControl(tcInterval);
			tcMoment.RemoveTimeRange(tr5);
			tci = tcMoment.GetInverseTimeControl(dt1, dt2, TimeType.Interval);
			expected = "2020.03.13 00:00:00 (3)\r\n";
			AssertFunc(tci, expected, 4);
		}

		[TestMethod]
		public void Seconds()
		{
			TimeUnit timeUnit = TimeUnit.Second;

			var tc = new TaskControl(timeUnit);
			TaskControl tci;

			string expected;
			string actual;
			var dtNow = new DateTime(2020, 04, 04, 22, 26, 31, 823);

			var trs1 = TimeRange.CalcTimeRange(new DateTime(2020, 01, 01), new DateTime(2020, 03, 08, 08, 08, 08, 08), TimeType.Interval, timeUnit);
			var trs2 = TimeRange.CalcTimeRange(new DateTime(2020, 03, 01), dtNow, TimeType.Interval, timeUnit);
			tc.AddTimeRange(trs1);
			tci = tc.GetInverseTimeControl(trs2);
			expected = $"2020.03.08 08:08:08.000 - 2020.04.04 22:26:31.000";
			var tri = tci.GetTimeRanges().First();
			actual = $"{tri.DateTimeStart:yyyy.MM.dd HH:mm:ss.fff} - {tri.EndOfTimeRange(timeUnit).ToString("yyyy.MM.dd HH:mm:ss.fff")}";
			Assert.AreEqual("\r\n" + expected, "\r\n" + actual);

		}

		[TestMethod]
		public void DbSpeed()
		{
			var rnd = new Random();
			using var db = new TaskControlTestContext();
			var tc = new DbTaskControl(db, TimeUnit.Second, TimeType.Interval, Domain.TestSeconds, "Client1", new DateTime(2019, 01, 01));
			for (int i = 0; i < 1000; i++)
			{
				//új nap
				var dtNap = new DateTime(2020, 01, 01).AddDays(i);

				//0 és 1 óra között lekérdezzük az adathiányt, amit elküldünk az ügyfélnek
				var dtHiany = dtNap.AddSeconds(rnd.Next(3600));
				var tcHiany = tc.GetShortage(dtHiany);


				//szimuláció: Az adatpótlást szerdánként, csütörtökönként vasárnaponként nem kapjuk meg, többi napon igen
				if (dtNap.DayOfWeek != DayOfWeek.Wednesday && dtNap.DayOfWeek != DayOfWeek.Thursday && dtNap.DayOfWeek != DayOfWeek.Sunday)
				{
					foreach (var trPotlas in tcHiany.GetTimeRanges())
					{
						tc.AddTimeRange(trPotlas);
					}
				}

				//szimuláció: vasránap kivételével zárás után (16-17 óra körül kapunk adatot az adott napról)
				if (dtNap.DayOfWeek != DayOfWeek.Sunday)
				{
					var dtAutoKuldes = dtNap.AddHours(16).AddSeconds(rnd.Next(3600));
					var trAutoKuldes = TimeRange.CalcTimeRange(dtNap, dtAutoKuldes, TimeType.Interval, TimeUnit.Second);
					tc.AddTimeRange(trAutoKuldes);
				}
			}

		}

		[TestMethod]
		public void InfinityMode()
		{
			string expected;
			using var db = new TaskControlTestContext();
			var tc = new DbTaskControl(db, TimeUnit.Infinity, TimeType.Interval, Domain.TestInfinity, "Work1", new DateTime(2019, 01, 01));

			tc.AddTimeRange(null);
			expected = true.ToString();
			AssertFunc(tc, expected, 1);

			var tci = tc.GetInverseTimeControl(TimeRange.Infinity());
			expected = false.ToString();
			AssertFunc(tci, expected, 2);

			tc.RemoveTimeRange(null);
			expected = false.ToString();
			AssertFunc(tc, expected, 3);

			var tci2 = tc.GetInverseTimeControl(TimeRange.Infinity());
			expected = true.ToString();
			AssertFunc(tci2, expected, 4);
		}

		[TestMethod]
		public void GetFullShortage()
		{
			TestInitialize();
			string expected;
			string actual = "";

			using var db = new TaskControlTestContext();
			
			var tc0 = new DbTaskControl(db, TimeUnit.Day, TimeType.Interval, Domain.TestInterval, "", new DateTime(2020, 03, 01));
			tc0.RemoveTimeRange(trfull);

			var tc1 = new DbTaskControl(db, TimeUnit.Day, TimeType.Interval, Domain.TestInterval, "Work1", new DateTime(2020, 03, 01));
			InitTaskControl(tc1);
			tc1.AddTimeRange(tr6);

			var tc2 = new DbTaskControl(db, TimeUnit.Day, TimeType.Interval, Domain.TestInterval, "Work2", new DateTime(2020, 03, 01));
			InitTaskControl(tc2);
			tc2.RemoveTimeRange(tr6);

			expected = $":2020.03.01 00:00:00 (16)\r\n\r\nWork1:2020.03.01 00:00:00 (1)\r\n2020.03.04 00:00:00 (1)\r\n2020.03.13 00:00:00 (1)\r\n2020.03.16 00:00:00 (1)\r\n"
				+"\r\nWork2:2020.03.01 00:00:00 (1)\r\n2020.03.04 00:00:00 (10)\r\n2020.03.16 00:00:00 (1)\r\n\r\n";

			var fullShortage = tc1.GetFullShortage(new DateTime(2020, 03, 17));
			foreach (var shortage in fullShortage)
			{
				actual += shortage.Item1 + ":" + shortage.Item2.TimeRangesToString() + "\r\n";
			}
			Assert.AreEqual("\r\n" + expected, "\r\n" + actual);


		}
		[TestMethod]
		public void GetFullShortageInfinity()
		{
			string expected;
			string actual = "";

			using var db = new TaskControlTestContext();
			var tc1 = new DbTaskControl(db, TimeUnit.Infinity, TimeType.Interval, Domain.TestInfinity, "Work1");
			tc1.AddTimeRange(TimeRange.Infinity());

			var tc2 = new DbTaskControl(db, TimeUnit.Infinity, TimeType.Interval, Domain.TestInfinity, "Work2");
			tc2.RemoveTimeRange(TimeRange.Infinity());

			var tc3 = new DbTaskControl(db, TimeUnit.Infinity, TimeType.Interval, Domain.TestInfinity, "Work3");
			tc3.AddTimeRange(TimeRange.Infinity());

			var tc4 = new DbTaskControl(db, TimeUnit.Infinity, TimeType.Interval, Domain.TestInfinity, "Work4");
			tc4.RemoveTimeRange(TimeRange.Infinity());

			expected = $"Work2: True\r\nWork4: True\r\n";

			var fullShortage = tc1.GetFullShortage();
			foreach (var shortage in fullShortage)
			{
				actual += shortage.Item1 + ": " + shortage.Item2.TimeRangesToString() + "\r\n";
			}
			Assert.AreEqual("\r\n" + expected, "\r\n" + actual);
		}


		public void InvertCore(TaskControl tc, TimeType timeType)
		{
			TaskControl tci;

			string expected;

			InitTaskControl(tc);
			tci = tc.GetInverseTimeControl(tr6);
			expected = "2020.03.07 00:00:00 (1)\r\n2020.03.10 00:00:00 (1)\r\n";
			AssertFunc(tci, expected, 1);

			InitTaskControl(tc);
			tci = tc.GetInverseTimeControl(tr7);
			expected = "2020.03.07 00:00:00 (1)\r\n2020.03.10 00:00:00 (1)\r\n";
			AssertFunc(tci, expected, 2);

			InitTaskControl(tc);
			tci = tc.GetInverseTimeControl(tr8);
			expected = "2020.03.04 00:00:00 (1)\r\n2020.03.07 00:00:00 (1)\r\n2020.03.10 00:00:00 (1)\r\n2020.03.13 00:00:00 (1)\r\n";
			AssertFunc(tci, expected, 3);

			InitTaskControl(tc);
			tci = tc.GetInverseTimeControl(tr3);
			expected = "";
			AssertFunc(tci, expected, 4);

			InitTaskControl(tc);
			tc.AddTimeRange(tr6);
			tci = tc.GetInverseTimeControl(tr6);
			expected = "";
			AssertFunc(tci, expected, 5);

			InitTaskControl(tc);
			tc.RemoveTimeRange(tr6);
			tci = tc.GetInverseTimeControl(tr6);
			expected = "2020.03.05 00:00:00 (8)\r\n";
			AssertFunc(tci, expected, 6);


			tc.RemoveTimeRange(trfull);
			tci = tc.GetInverseTimeControl(trfull);
			expected = "2020.03.01 00:00:00 (16)\r\n";
			AssertFunc(tci, expected, 7);

			tc.AddTimeRange(trfull);
			tci = tc.GetInverseTimeControl(trfull);
			expected = "";
			AssertFunc(tci, expected, 8);

			tc.AddTimeRange(trfull);
			tc.RemoveTimeRange(trhalf);
			tci = tc.GetInverseTimeControl(trfull);
			expected = "2020.03.01 00:00:00 (8)\r\n";
			AssertFunc(tci, expected, 9);

			tci = tci.GetInverseTimeControl(trfull);
			expected = "2020.03.09 00:00:00 (8)\r\n";
			AssertFunc(tci, expected, 10);
		}

		public void AddRemoveTimeRangeCore(TaskControl tc, Func<TaskControl, string, int, int> assertFunc)
		{
			string expected;

			InitTaskControl(tc);
			expected = "2020.03.02 00:00:00 (2)\r\n2020.03.05 00:00:00 (2)\r\n2020.03.08 00:00:00 (2)\r\n2020.03.11 00:00:00 (2)\r\n2020.03.14 00:00:00 (2)\r\n";
			assertFunc(tc, expected, 0);

			InitTaskControl(tc);
			tc.AddTimeRange(tr6);
			expected = "2020.03.02 00:00:00 (2)\r\n2020.03.05 00:00:00 (8)\r\n2020.03.14 00:00:00 (2)\r\n";
			assertFunc(tc, expected, 1);

			InitTaskControl(tc);
			tc.AddTimeRange(tr7);
			expected = "2020.03.02 00:00:00 (2)\r\n2020.03.05 00:00:00 (8)\r\n2020.03.14 00:00:00 (2)\r\n";
			assertFunc(tc, expected, 2);


			InitTaskControl(tc);
			tc.AddTimeRange(tr8);
			expected = "2020.03.02 00:00:00 (14)\r\n";
			assertFunc(tc, expected, 3);


			InitTaskControl(tc);
			tc.RemoveTimeRange(tr6);
			expected = "2020.03.02 00:00:00 (2)\r\n2020.03.14 00:00:00 (2)\r\n";
			assertFunc(tc, expected, 4);

			InitTaskControl(tc);
			tc.RemoveTimeRange(tr7);
			expected = "2020.03.02 00:00:00 (2)\r\n2020.03.05 00:00:00 (1)\r\n2020.03.12 00:00:00 (1)\r\n2020.03.14 00:00:00 (2)\r\n";
			assertFunc(tc, expected, 5);

			InitTaskControl(tc);
			tc.RemoveTimeRange(tr8);
			expected = "2020.03.02 00:00:00 (2)\r\n2020.03.14 00:00:00 (2)\r\n";
			assertFunc(tc, expected, 6);


			InitTaskControl(tc);
			tc.AddTimeRange(tr8);
			tc.RemoveTimeRange(tr6);
			expected = "2020.03.02 00:00:00 (3)\r\n2020.03.13 00:00:00 (3)\r\n";
			assertFunc(tc, expected, 7);

			InitTaskControl(tc);
			tc.AddTimeRange(tr6);
			tc.RemoveTimeRange(tr7);
			expected = "2020.03.02 00:00:00 (2)\r\n2020.03.05 00:00:00 (1)\r\n2020.03.12 00:00:00 (1)\r\n2020.03.14 00:00:00 (2)\r\n";
			assertFunc(tc, expected, 8);


			InitTaskControl(tc);
			tc.AddTimeRange(tr6);
			tc.RemoveTimeRange(tr7);
			expected = "2020.03.02 00:00:00 (2)\r\n2020.03.05 00:00:00 (1)\r\n2020.03.12 00:00:00 (1)\r\n2020.03.14 00:00:00 (2)\r\n";
			assertFunc(tc, expected, 8);
		}

	}
}
