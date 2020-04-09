using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace Scheduler
{
	public abstract class BaseHostedService : IHostedService, IDisposable
	{
		private Timer _timer = null!;
		protected IServiceProvider ServiceProvider { get; set; }
		private static object LockObject = new object();

		public BaseHostedService(IServiceProvider serviceProvider)
		{
			this.ServiceProvider = serviceProvider;
		}

		public Task StartAsync(CancellationToken stoppingToken)
		{
			_timer = new Timer(DoWork!, null, TimeSpan.Zero, getPeriod());

			return Task.CompletedTask;
		}

		protected virtual TimeSpan getPeriod()
		{
			return TimeSpan.FromSeconds(60);
		}

		/// <summary>
		/// Ez hajtódik végre ciklikusan
		/// </summary>
		/// <param name="state"></param>
		protected abstract void Execute(object state);

		private void DoWork(object state)
		{
			if (System.Threading.Monitor.TryEnter(LockObject, 1000))
			{
				try
				{
					Execute(state);
				}
				finally
				{
					System.Threading.Monitor.Exit(LockObject);
				}
			}
		}

		public Task StopAsync(CancellationToken stoppingToken)
		{
			_timer?.Change(Timeout.Infinite, 0);

			return Task.CompletedTask;
		}

		public void Dispose()
		{
			_timer?.Dispose();
		}
	}
}
