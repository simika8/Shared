using System;
using System.Collections.Generic;
using System.Text;
using LX.Common.Database.ConnectionProvider;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;


namespace Database.EFCore
{
	public class LxContext : DbContext
	{
		private static readonly ILoggerFactory s_loggerFactory = LoggerFactory.Create(b => b.SetMinimumLevel(LogLevel.Information).AddConsole(c => c.IncludeScopes = true));
		/// <summary>
		/// Fő Projektenkénti külön adatbázis használat esetén adatbázisnév
		/// </summary>
		public string DatabaseName { get; set; } = null!;


		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
		{
			Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

			base.OnConfiguring(optionsBuilder);
			
			optionsBuilder
				.UseLoggerFactory(s_loggerFactory);

			//optionsBuilder.EnableSensitiveDataLogging();
		}
	}
}
