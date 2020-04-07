using System;
using System.Collections.Generic;
using System.Text;

namespace TaskControl
{	public class TaskControl_TimeRange: TimeRange
	{
		public TaskControl_TimeRange(DateTime dateTimeStart, long durationCount, TimeUnit timeUnit) : base(dateTimeStart, durationCount, timeUnit)
		{
		    
		}
		public static new TaskControl_TimeRange Infinity() => new TaskControl_TimeRange(DateTime.MinValue, long.MaxValue, TimeUnit.Infinity);

		public long Id { get; set; }
		public long ItemId { get; set; }
		public TaskControl_Item Item { get; set; } = null!;
	}
}
