using System;
using System.IO;
using Microsoft.Win32;

namespace LX.Common
{
#if EXTERN
	public
#else
	internal
#endif
	static class LXIniFile
	{
		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		private static string GetElixirLocalPath()
		{
			const string defaultPath = @"C:\winlx";

			using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\LX-Line\Elixir"))
			{
				if (key is null)
				{
					return defaultPath;
				}

				if (key.GetValue("LocalPath") is string val)
				{
					return val;
				}

				return defaultPath;
			}
		}

		/// <summary>
		/// Visszaadja a beállításokat tartalmazó ini file útvonalát
		/// </summary>
		/// <param name="iniFileName">Keresendő ini file neve</param>
		/// <returns>Teljes elérési útvonal</returns>
		public static string GetIniPath(string iniFileName)
		{
			string[] paths = {
				// user profil könyvtárában
				Path.Combine(Environment.GetEnvironmentVariable("USERPROFILE") ?? string.Empty, iniFileName),
				// registry alapján
				Path.Combine(GetElixirLocalPath(), iniFileName),
				// windows könyvtárban
				Path.Combine(Environment.GetEnvironmentVariable("SYSTEMROOT") ?? string.Empty, iniFileName),
			};

			foreach (string item in paths)
			{
				if (File.Exists(item))
				{
					return item;
				}
			}

			// nem található
			return iniFileName;
		}
	}
}
