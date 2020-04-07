using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace TaskControl
{
	public class TaskControl_Item
	{
		public long Id { get; set; }
		public long HeadId { get; set; }
		public TaskControl_Head Head { get; set; } = null!;


		/// <summary>Ha egy taskcontrolhed bejegyzés (pl statements export) több független nyilvántartást tartalmaz 
		/// (pl telephelyenként egyet) akkor ide a kerül a nyilvántartást azonosító szöveg (pl statements export telephelyenként tartja 
		/// nyilván mi van készen, így itt telephely azonosító van).</summary>
		/// Ha csak 1 Item tartozik a TaskControl_Head-hez, akkor itt lehet pl üresstring
		[MaxLength(100)]
		public string Code { get; set; } = null!;

		//időpont/intervallum nyilvántartás (TaskControl_Head.TaskType = Interval vagy Moment esetén használható)
		public virtual ICollection<TaskControl_TimeRange> TimeRanges { get; set; } = new HashSet<TaskControl_TimeRange>();

		//időpont/intervallum nyilvántartás (TaskControl_Head.TaskType = Interval vagy Moment esetén használható)
		public DateTime DateTimeStart { get; set; }
	}
}
