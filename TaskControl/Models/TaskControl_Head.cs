using System;
using System.Collections.Generic;
using System.Text;

namespace TaskControl
{
	public class TaskControl_Head
	{
		public long Id { get; set; }
		/// <summary>nyilvántartási főkategória. pl Statements export</summary>
		public Domain Domain { get; set; }

		/// <summary>Moment = 0,Interval = 1,</summary>
		public TimeType TimeType { get; set; }

		/// <summary>
		/// Second = 0,Minute = 1,Hour = 2,Day = 3,Week = 4,Month = 5,Year = 6, null: 
		/// </summary>
		public TimeUnit TimeUnit { get; set; }

		//public long CycleStartOffsetSeconds { get; set; }
		/// <summary>
		/// Nyilvántartási elemek. Pl Ha a StatementsExporton belül telephelyenként kell nyilvántartani, mely exportok készültek el, akkor ott minden telephelyre felveszünk egy Item-et. Ha adatbázis szinten csak egyetlen nyilvántartás kell, akkor egy Itemet veszünk fel.
		/// </summary>
		public virtual ICollection<TaskControl_Item> TaskControl_Items { get; set; } = new HashSet<TaskControl_Item>();
	}
}
