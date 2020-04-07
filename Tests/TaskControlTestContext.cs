using Database.EFCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using TaskControl;

namespace TaskControl
{
	public class TaskControlTestContext : DbContext, ITaskControlContext
	{

		public DbSet<TaskControl_Head> TaskControl_Heads { get; set; } = null!;
		public DbSet<TaskControl_Item> TaskControl_Items { get; set; } = null!;
		public DbSet<TaskControl_TimeRange> TaskControl_TimeRanges { get; set; } = null!;


		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			base.OnModelCreating(modelBuilder);
		}

		private static readonly ILoggerFactory s_loggerFactory = LoggerFactory.Create(b => b.SetMinimumLevel(LogLevel.Information).AddConsole(c => c.IncludeScopes = true));
		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
		{

			Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

			base.OnConfiguring(optionsBuilder);

			optionsBuilder
				.UseLoggerFactory(s_loggerFactory);

			//string database = "TaskControlTest";
			string database = "TaskControlTest";
			optionsBuilder
				.UseInMemoryDatabase(databaseName: database);

			/*string connstr = $@"Server=(localdb)\mssqllocaldb;Database={database};Trusted_Connection=True;MultipleActiveResultSets=true";

			optionsBuilder
				.UseSqlServer(connstr);*/


		}
	}
}
