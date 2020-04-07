using System.Configuration.Install;
using System.Diagnostics;
using System.Reflection;
using System.Security.Principal;

namespace LX.Shared.WinService
{
#if EXTERN
	public
#else
	internal
#endif
	static class SelfInstaller
	{
		private static readonly string s_exePath = Assembly.GetExecutingAssembly().Location;

		public static bool InstallMe(string[] args)
		{
			try
			{
				if (!IsAdministrator())
				{
					RestartAsAdmin(args);
				}
				else
				{
					ManagedInstallerClass.InstallHelper(new[] { s_exePath });
				}
			}
			catch
			{
				return false;
			}

			return true;
		}

		public static bool UninstallMe(string[] args)
		{
			try
			{
				if (!IsAdministrator())
				{
					RestartAsAdmin(args);
				}
				else
				{
					ManagedInstallerClass.InstallHelper(new[] { "/u", s_exePath });
				}
			}
			catch
			{
				return false;
			}

			return true;
		}

		/// <summary>
		/// Adminként fut-e a program
		/// </summary>
		private static bool IsAdministrator()
		{
			using (var identity = WindowsIdentity.GetCurrent())
			{
				var principal = new WindowsPrincipal(identity);
				return principal.IsInRole(WindowsBuiltInRole.Administrator);
			}
		}

		private static void RestartAsAdmin(string[] args)
		{
			string exeName = Process.GetCurrentProcess().MainModule.FileName;

			var startInfo = new ProcessStartInfo(exeName)
			{
				Verb = "runas",
				Arguments = string.Join(" ", args)
			};

			Process.Start(startInfo);
		}
	}
}
