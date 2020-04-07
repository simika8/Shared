using System;
using System.Collections.Generic;
using System.Linq;
using Database.EFCore;
using FluentDateTime;
using Microsoft.EntityFrameworkCore;

namespace TaskControl
{
	public class DbTaskControl : TaskControl
	{
		//public TaskControl TaskControl { get; set; }
		private TaskControl_Item TaskControl_Item { get; set; } = null!;

		private ITaskControlContext Db { get; set; } = null!;

		private Domain TaskControlDomain { get; set; }
		private string ItemCode { get; set; }
		private DateTime? ItemDateTimeStart { get; set; }
		
		public TimeType TimeType { get; }

		protected override void ReloadData()
		{
			base.ReloadData();
			TimeRanges = Db.TaskControl_TimeRanges.Where(x => x.Item == TaskControl_Item).OrderBy(x => x.DateTimeStart).ToList();
		}
		protected override void SaveChanges()
		{
			base.SaveChanges();
			Db.SaveChanges();
		}

		private TaskControl_Head TaskControl_Head { get; set; } = null!;


		public DbTaskControl(ITaskControlContext db, TimeUnit timeUnit, TimeType timeType, Domain taskControlDomain, string itemCode = "", DateTime? itemDateTimeStart = null) : base(timeUnit)
		{
			this.Db = db;

			this.TaskControlDomain = taskControlDomain;
			this.ItemCode = itemCode;
			this.ItemDateTimeStart = itemDateTimeStart;
			this.TimeType = timeType;
			Load();
		}

		private void Load()
		{
			TaskControl_Head = Db.TaskControl_Heads
				.SingleOrDefault(x => x.Domain == TaskControlDomain);
			if (TaskControl_Head == default(TaskControl_Head))
			{//Ha még nem volt ilyen, akkor létrehozom
				TaskControl_Head = new TaskControl_Head()
				{
					Domain = TaskControlDomain,
					TimeUnit = TimeUnit,
					TimeType = TimeType,
				};
				Db.TaskControl_Heads.Add(TaskControl_Head);

				TaskControl_Item = new TaskControl_Item()
				{
					Code = ItemCode,
					Head = TaskControl_Head,
					DateTimeStart = ItemDateTimeStart ?? DateTime.Now,
				};
				TaskControl_Head.TaskControl_Items.Add(TaskControl_Item);

			}
			else
			{//Ha megtaláltam, ellenőrzöm a beállításait, megkeresem tci-t
				if (TaskControl_Head.TimeUnit != TimeUnit)
					throw new Exception($"TaskControl.Load hiba {TaskControl_Head.TimeUnit.ToString()} != {TimeUnit.ToString()}.");
				if (TaskControl_Head.TimeType != TimeType)
					throw new Exception($"TaskControl.Load hiba {TaskControl_Head.TimeType.ToString()} != {TimeType.ToString()}.");

				//TaskControl_Head.TaskControl_Items.Load();
				TaskControl_Item = Db.TaskControl_Items.GetData(x => { x.Code = ItemCode; x.HeadId = TaskControl_Head.Id; });

				if (ItemDateTimeStart != null)
					TaskControl_Item.DateTimeStart = ItemDateTimeStart ?? DateTime.Now;
			}
		}

		protected override void AddTimeRangeToList(TaskControl_TimeRange? timeRange)
		{
			if (timeRange == null)
				return;
			base.AddTimeRangeToList(timeRange);
			if (TaskControl_Item != null)
				TaskControl_Item.TimeRanges.Add(timeRange);
		}

		protected override void RemoveTimeRangeFromList(TaskControl_TimeRange timeRangeToMerge)
		{
			base.RemoveTimeRangeFromList(timeRangeToMerge);
			if (TaskControl_Item != null)
				TaskControl_Item.TimeRanges.Remove(timeRangeToMerge);
		}

		public TaskControl GetShortage(DateTime? endTime = null) 
			=> GetInverseTimeControl(TaskControl_Item.DateTimeStart, endTime ?? DateTime.Now, TimeType);


		public IEnumerable<(string, TaskControl)> GetFullShortage(DateTime? endTime = null)
		{
			var taskControl_Items = Db.TaskControl_Items
				.Where(x => x.HeadId == TaskControl_Head.Id);

			if (TimeUnit == TimeUnit.Infinity)
			{
			
				var valami = Db.TaskControl_Items
					.Where(x => x.HeadId == TaskControl_Head.Id)
					.Include(x => x.TimeRanges)
					//.SelectMany(x => x.TimeRanges, (i, tr) => new {i, tr})
					//.Where(x => !x.tr.Any())
					.ToHashSet()
					.Where(x => !x.TimeRanges.Any());

				foreach (var taskControl_Item in valami)
				{
					var res = new TaskControl(TimeUnit);
					res.AddTimeRange(TimeRange.Infinity());
					yield return (taskControl_Item.Code, res);
				}
			}
			else
			{
				foreach (var taskControl_Item in taskControl_Items)
				{
					var res = new DbTaskControl(Db, TimeUnit, TimeType, TaskControlDomain, taskControl_Item.Code, taskControl_Item.DateTimeStart)
						.GetShortage(endTime);
					yield return (taskControl_Item.Code, res);
				}
			}

		}

	}
}
