using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using LX.Common.Extensions;

namespace LX.Common
{
	using IniStruct = Dictionary</* section */ string, Dictionary</* property */ string, /* value */ string>>;

#if EXTERN
	public
#else
	internal
#endif
	class IniReader
	{
		private const string CryptoKey = "TABYAFIA4QBuAHQAbwB0AHQASABhAGwA";
		private readonly IniStruct _ini = new IniStruct(StringComparer.InvariantCultureIgnoreCase);
		private static readonly IEnumerable<byte> s_encriptionSignature = new[] { (byte)'.', (byte)'i' };

		private static string Decode(string encodedValue) =>
			AesCrypto.AesDecryptString(encodedValue, Encoding.Unicode.GetString(Convert.FromBase64String(CryptoKey)), Encoding.Unicode);

		private static byte[] ReadFirstTwoByteOfFile(string fileName)
		{
			byte[] firstTwoByte = ArrayExt.Empty<byte>();
			try
			{
				using (var iniFile = File.OpenRead(fileName))
				{
					firstTwoByte = new byte[2];
					iniFile.Read(firstTwoByte, 0, 2);
				}
			}
			catch {/* ignored */}

			return firstTwoByte;
		}

		private static string LoadIniText(bool isEncryptedFile, string fileName)
		{
			try
			{
				if (!isEncryptedFile)
				{
					return File.ReadAllText(fileName);
				}

				string encryptedFile = string.Concat(File.ReadAllText(fileName).Skip(2));
				return Decode(encryptedFile);
			}
			catch
			{
				return string.Empty;
			}
		}

		/// <summary>
		/// Ini fájlkezelő
		/// </summary>
		/// <param name="fileName">filenév</param>
		/// <exception cref="FileNotFoundException">Nem található az ini file.</exception>
		public IniReader(string fileName)
		{
			bool isEncryptedFile = ReadFirstTwoByteOfFile(fileName).SequenceEqual(s_encriptionSignature);

			string txt = LoadIniText(isEncryptedFile, fileName);

			var currentSection = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);

			_ini[string.Empty] = currentSection;

			var iniLines = txt.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries).Where(t => !t.IsNullOrWhitespace()).Select(t => t.Trim());

			foreach (string line in iniLines)
			{
				if (line.StartsWith(";"))
				{
					continue;
				}

				if (line.StartsWith("[") && line.EndsWith("]"))
				{
					currentSection = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
					_ini[line.Substring(1, line.LastIndexOf("]", StringComparison.Ordinal) - 1)] = currentSection;
					continue;
				}

				int idx = line.IndexOf("=", StringComparison.Ordinal);
				if (idx == -1)
				{
					currentSection[line] = string.Empty;
				}
				else
				{
					currentSection[line.Substring(0, idx)] = line.Substring(idx + 1);
				}
			}
		}

		public string GetValue(string key) => GetValue(key, string.Empty, string.Empty);

		public string GetValue(string key, string section) => GetValue(key, section, string.Empty);

		public string GetValue(string key, string section, string @default)
		{
			if (!_ini.ContainsKey(section))
			{
				return @default;
			}

			return !_ini[section].ContainsKey(key)
				? @default
				: _ini[section][key];
		}

		public string[] GetKeys(string section)
		{
			if (!_ini.ContainsKey(section))
			{
				return ArrayExt.Empty<string>();
			}

			return !_ini.ContainsKey(section)
				? ArrayExt.Empty<string>()
				: _ini[section].Keys.ToArray();
		}

		public string[] GetSections() => _ini.Keys.Where(t => !string.IsNullOrEmpty(t)).ToArray();
	}
}
