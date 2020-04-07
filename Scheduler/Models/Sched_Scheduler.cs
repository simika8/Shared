using System;
using System.Collections.Generic;
using System.Text;

namespace Scheduler
{
	public class Sched_Scheduler:IScheduler
	{
		public int Id { get; set; }

		public TaskType TaskType { get; set; }

		/// <summary>task guin/logokban megjelenő neve</summary>
		public string? Name { get; set; }

		/// <summary>aktív-e a task</summary>
		public bool Active { get; set; } = true;

		/// <summary>mikor futott utoljára, kapcsolódikra kattintva nullozódik</summary>
		public DateTime? LastWorkTime { get; set; }

		/// <summary>részfeladatok lekérdezésének gyakorisága másodpercben</summary>
		public double? Freq { get; set; }

	}
}
