using System;
using System.Collections.Generic;
using System.Text;

namespace TaskControl
{

	public enum Domain
	{
		#region TaskControl Domainek
		TestMoment = 1,
		TestInterval = 2,
		TestSeconds = 3,
		TestInfinity = 4,

		StatementsExport = 1000,
		StatementsImport = 1001,
		#endregion
	}
	/// <summary>
	/// Second = 0,Minute = 1,Hour = 2,Day = 3,Week = 4,Month = 5,Year = 6,Infinity = 7,
	/// </summary>
	public enum TimeUnit: short
	{
		Second = 0,
		Minute = 1,
		Hour = 2,
		Day = 3,
		Week = 4,
		Month = 5,
		Year = 6,
		Infinity = 7,
	}

	/// <summary>
	/// Moment = 0,Intrerval = 1,
	/// </summary>
	public enum TimeType : short
	{
		Moment = 0,
		Interval = 1,
	}



}
