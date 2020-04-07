using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace LX.Common.Extensions
{
	/// <summary>
	/// Nagyon hasznos kiterjesztések
	/// </summary>
#if EXTERN
	public
#else
	internal
#endif
	static class Ext
	{
		private static class NativeMethods
		{
			[System.Runtime.InteropServices.DllImport("wininet.dll", SetLastError = true)]
			public static extern bool InternetGetConnectedState(out int lpdwFlags, int dwReserved);
		}

		/// <summary>
		/// Adott érték két megadott érték közé esik-e
		/// </summary>
		/// <typeparam name="T">Összehasonlítható típus</typeparam>
		/// <param name="self">Adott érték</param>
		/// <param name="start">Nagyobb vagy egyenlő ennél</param>
		/// <param name="end">Kisebb vagy egyenlő ennél</param>
		/// <returns></returns>
#if NETFX_45
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
		public static bool IsBetween<T>(this T self, T start, T end) where T : IComparable, IComparable<T>
			=> self.CompareTo(start) >= 0 && self.CompareTo(end) <= 0;


		/// <summary>
		/// Felsorolt objektumot inicializál 0 elemmel, ha az null
		/// </summary>
		/// <typeparam name="T">Felsorolt típus</typeparam>
		/// <returns>Eredeti objektum, vagy nulla elemű felsorolt objektum</returns>
#if NETFX_45
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
		public static IEnumerable<T> Coalesce<T>(this IEnumerable<T> self)
			=> self ?? Enumerable.Empty<T>();

		/// <summary>
		/// Visszaadja egy adott változó értékét, vagy a típus szerinti default értékét, ha a feltétel teljesül
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="self">Vizsgálandó érték</param>
		/// <param name="predicate">Feltétel</param>
		/// <param name="def">Alapértelmezett érték felülbírálása</param>
		/// <returns><paramref name="def"/> vagy az eredeti érték</returns>
#if NETFX_45
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
		public static T DefaultIf<T>(this T self, Predicate<T> predicate, T def = default)
			=> predicate(self) ? def : self;

		/// <summary>
		/// Visszaadja egy objektum típusát, Nullable típus esetében pedig a Nullable alatti típusát
		/// </summary>
		/// <returns>Objektum típusa</returns>
		public static Type NGetType(this object self, bool propertyType = false)
		{
			if (self == null)
			{
				return null;
			}

			Type type;
			if (self is PropertyInfo info && propertyType)
			{
				type = info.PropertyType;
			}
			else
			{
				type = self.GetType();
			}

			return Nullable.GetUnderlyingType(type) ?? type;
		}

		/// <summary>
		/// Értéket adott típusra alakít át (generikus verzió)
		/// </summary>
		/// <typeparam name="T">Cél típus</typeparam>
		/// <typeparam name="TFrom">Bemeneti típus</typeparam>
		/// <param name="self">Érték</param>
		/// <param name="parser">Egyedi konvertáló függvény</param>
		/// <param name="def">Alapértelmezett érték</param>
		/// <returns>Átalakított érték</returns>
		public static T NGetValue<T, TFrom>(this TFrom self, Func<TFrom, T> parser = null, T def = default)
		{
			if (self == null || DBNull.Value.Equals(self))
			{
				return def;
			}

			var type = typeof(T);

			try
			{
				if (parser == null)
				{
					return (T)Convert.ChangeType(self, Nullable.GetUnderlyingType(type) ?? type);
				}

				return parser(self);
			}
			catch
			{
				return def;
			}
		}

		/// <summary>
		/// Értéket adott típusra alakít át (dinamikus verzió)
		/// </summary>
		/// <param name="self">Érték</param>
		/// <param name="toType">Cél típus</param>
		/// <returns>Átalakított érték</returns>
		public static object NGetValue(this object self, Type toType)
		{
			if (self == null || DBNull.Value.Equals(self))
			{
				return default;
			}

			try
			{
				return Convert.ChangeType(self, Nullable.GetUnderlyingType(toType) ?? toType);
			}
			catch
			{
				return default;
			}
		}


#if NETFX_45
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
		public static Uri Append(this Uri uri, params string[] paths)
		{
			return new Uri(paths.Aggregate(uri.ToString(), Func), UriKind.RelativeOrAbsolute);
			static string Func(string current, string path) => $"{current.TrimEnd('/')}/{path.TrimStart('/')}";
		}

		public static void FromXml(this DataTable self, byte[] xmlBytes)
		{
			using (var ms = new MemoryStream(xmlBytes))
			{
				ms.Position = 0;
				self.ReadXml(ms);
			}
		}

		public static byte[] ToXml(this DataTable self)
		{
			byte[] result;

			if (self.TableName.IsNullOrWhitespace())
			{
				self.TableName = "Table";
			}

			using (var ms = new MemoryStream())
			using (var xw = XmlWriter.Create(ms, new XmlWriterSettings { Encoding = Encoding.UTF8 }))
			{
				self.WriteXml(xw, XmlWriteMode.WriteSchema);
				xw.Flush();
				result = ms.ToArray();
			}

			return result;
		}

		/// <summary>
		/// Adott típusnak megfelelő objektumot készít byte tömbben tárolt XML-ből
		/// </summary>
		/// <typeparam name="T">Típus</typeparam>
		/// <param name="self">Byte tömb</param>
		/// <returns>Objektum</returns>
		public static T XDeserialize<T>(this byte[] self)
		{
			if (0 == self.Length)
			{
				return default;
			}

			T data = default;

			using (var ms = new MemoryStream(self))
			{
				var xmlSerializer = new XmlSerializer(typeof(T));
				try
				{
					using (var reader = XmlReader.Create(ms, new XmlReaderSettings { XmlResolver = null }))
					{
						data = (T)xmlSerializer.Deserialize(reader);
					}
				}
				catch /* (Exception ex) */ { /* ignored */ }
			}

			return data;
		}

		/// <summary>
		/// XML deszerializálása fileból
		/// </summary>
		/// <param name="fileName">Bemeneti XML file</param>
		/// <returns>Deszerializált objektum</returns>
		public static T XDeserializeFile<T>(string fileName)
		{
			byte[] bytes = File.ReadAllBytes(fileName);
			return bytes.XDeserialize<T>();
		}

		/// <summary>
		/// Objektum XML fileba szerializálása
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="self"></param>
		/// <param name="fileName"></param>
		/// <param name="xmlWriterSettings"></param>
		public static void XSerializeToFile<T>(this T self, string fileName, XmlWriterSettings xmlWriterSettings = null)
		{
			var xmlSerializer = new XmlSerializer(self.GetType());

			if (xmlWriterSettings == null)
			{
				xmlWriterSettings = new XmlWriterSettings
				{
					Indent = false,
					Encoding = Encoding.UTF8
				};
			}

			using (var xmlWriter = XmlWriter.Create(fileName, xmlWriterSettings))
			{
				xmlSerializer.Serialize(xmlWriter, self);
				xmlWriter.Flush();
			}
		}

		/// <summary>
		/// Típus XML szerializálása byte tömbbe
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="self"></param>
		/// <returns></returns>
		public static byte[] XSerialize<T>(this T self)
		{
			var xmlSerializer = new XmlSerializer(self.GetType());
			using (var ms = new MemoryStream())
			using (var xw = XmlWriter.Create(ms, new XmlWriterSettings { Indent = false, }))
			{
				xmlSerializer.Serialize(xw, self);
				xw.Flush();
				return ms.ToArray();
			}
		}

		/// <summary>
		/// Típus bináris szerializálása byte tömbbe
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="t"></param>
		/// <returns></returns>
		public static byte[] BSerialize<T>(this T t)
		{
			byte[] res;

			var formatter = new BinaryFormatter();
			using (var ms = new MemoryStream())
			{
				formatter.Serialize(ms, t);
				ms.Flush();
				res = ms.ToArray();
			}

			return res;
		}

		public static T BDeserialize<T>(this byte[] bytes)
		{
			var formatter = new BinaryFormatter();
			using (var ms = new MemoryStream(bytes))
			{
				return (T)formatter.Deserialize(ms);
			}
		}

		/// <summary>
		/// Elkapja a HTTP kivételeket és egyszerű válaszként adja vissza
		/// </summary>
		/// <returns>HTTP response</returns>
		public static HttpWebResponse GetResponseNoException(this HttpWebRequest self) => self.GetResponseNoException(out _);

		/// <summary>
		/// Elkapja a HTTP kivételeket és egyszerű válaszként adja vissza
		/// </summary>
		/// <returns>HTTP response</returns>
		public static HttpWebResponse GetResponseNoException(this HttpWebRequest self, out WebException webException)
		{
			try
			{
				webException = null;
				return (HttpWebResponse)self.GetResponse();
			}
			catch (WebException we) when (we.Response is HttpWebResponse)
			{
				webException = we;
				return (HttpWebResponse)we.Response;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="self"></param>
		/// <returns></returns>
		public static string GetEnumDescription(this Enum self)
		{
			string enumValueAsString = self.ToString();

			var type = self.GetType();
			var fieldInfo = type.GetField(enumValueAsString);
			object[] attributes = fieldInfo.GetCustomAttributes(typeof(System.ComponentModel.DescriptionAttribute), false);

			if (attributes.Length <= 0)
			{
				return enumValueAsString;
			}

			var attribute = (System.ComponentModel.DescriptionAttribute)attributes[0];
			return attribute.Description;
		}

		private static string GetDescription(Type type)
		{
			var descriptions = (System.ComponentModel.DescriptionAttribute[])
				type.GetCustomAttributes(typeof(System.ComponentModel.DescriptionAttribute), false);

			return descriptions.Length == 0 ? type.Name : descriptions[0].Description;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="self"></param>
		/// <returns></returns>
		public static string GetDescription<T>(this T self)
		{
			var type = self.GetType();
			return GetDescription(type);
		}

		public static string GetDescription<T>()
		{
			var type = typeof(T);
			return GetDescription(type);
		}

		/// <summary>
		/// Lemezre ment file(okat) a beágyazott erőforrásokból
		/// </summary>
		/// <param name="outputDir">Hova</param>
		/// <param name="resourceLocation">Honnan</param>
		/// <param name="files">Mit/miket</param>
		public static void ExtractEmbeddedResource(string outputDir, string resourceLocation, string[] files)
		{
			foreach (string file in files)
			{
				using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceLocation + @"." + file))
				{
					int count = 0;
					string fileName = Path.Combine(outputDir, file);

					while (File.Exists(fileName))
					{
						try
						{
							File.Move(fileName, $"{fileName}.old{++count}");
							File.Delete($"{fileName}.old{count}");
						}
						catch
						{
							// ignored
						}

						if (count > 20)
						{
							break;
						}
					}

					var fileStream = new FileStream(fileName, FileMode.Create);

					if (stream != null)
					{
						for (int i = 0; i < stream.Length; i++)
						{
							fileStream.WriteByte((byte)stream.ReadByte());
						}
					}

					fileStream.Close();
				}
			}
		}

#if NETFX_45
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
		public static bool FileFitsMask(this string self, string fileMask)
			=> new Regex(new StringBuilder(fileMask)
					.Replace(".", "\\.")
					.Replace("*", ".*")
					.Replace("?", ".")
					.Replace("$", "\\$")
					.ToString(),
					RegexOptions.IgnoreCase
				).IsMatch(self);

		public static string ToXmlString(this XDocument self)
		{
			if (self.Root == null)
			{
				return string.Empty;
			}

			string result;

			var ms = new MemoryStream();
			var ws = new XmlWriterSettings
			{
				Indent = true,
				IndentChars = "\t",
				Encoding = Encoding.UTF8
			};

			using (var xw = XmlWriter.Create(ms, ws))
			{
				self.Save(xw);
				xw.Flush();
			}

			using (var sr = new StreamReader(ms))
			{
				ms.Seek(0, SeekOrigin.Begin);
				result = sr.ReadToEnd();
			}

			return result;
		}

		public static byte[] ToXmlBinary(this XDocument self)
		{
			if (self.Root == null)
			{
				return null;
			}

			var ms = new MemoryStream();
			var ws = new XmlWriterSettings
			{
				Indent = true,
				IndentChars = "\t",
				Encoding = Encoding.UTF8
			};

			using (var xw = XmlWriter.Create(ms, ws))
			{
				self.Save(xw);
				xw.Flush();
			}

			byte[] result = ms.ToArray();
			return result;
		}

		/// <summary>
		/// Adminként fut-e a program
		/// </summary>
		public static bool IsAdministrator()
		{
			var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
			var principal = new System.Security.Principal.WindowsPrincipal(identity);
			return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
		}

		/// <summary>
		/// Program admin módban történő indítása
		/// </summary>
		/// <param name="args">Parancssori paraméterek</param>
		public static void RestartAsAdmin(string[] args)
		{
			string exeName = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
			var startInfo = new System.Diagnostics.ProcessStartInfo(exeName)
			{
				Verb = "runas",
				Arguments = string.Join(" ", args)
			};

			System.Diagnostics.Process.Start(startInfo);
		}

		#region Byte tömbben keresés

		/// <summary>
		/// Visszaadja a byte szekvencia kezdőpozícióit a byte tömbben
		/// </summary>
		/// <param name="self">Bemeneti byte tömb</param>
		/// <param name="candidate">Keresendő byte sorozat</param>
#if NETFX_45
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
		public static IEnumerable<int> Locate(this byte[] self, byte[] candidate)
			=> Locate(self, candidate, 0, self.Length);

		/// <summary>
		/// Visszaadja a byte szekvencia kezdőpozícióit a byte tömbben
		/// </summary>
		/// <param name="self">Bemeneti byte tömb</param>
		/// <param name="candidate">Keresendő byte sorozat</param>
		/// <param name="startPos">Opcionális kezdőpozíció</param>
		/// <param name="endPos">Opcionális végpozíció</param>
		/// <exception cref="ArgumentException">Az endPos nem lehet kisebb a startPosnál.</exception>
		public static IEnumerable<int> Locate(this byte[] self, byte[] candidate, int startPos, int endPos)
		{
			if (endPos < startPos)
			{
				throw new ArgumentException("Az endPos nem lehet kisebb a startPosnál!");
			}

			for (int i = startPos; i < endPos; i++)
			{
				if (!IsMatch(self, i, candidate))
				{
					continue;
				}

				yield return i;
			}
		}

		/// <summary>
		/// Megnézi, hogy adott position helyen lévő byte sorozat az arrayban megegyezik-e a candidate sorozattal
		/// </summary>
		/// <param name="array">Amiben keresünk</param>
		/// <param name="position">Ahol keresünk</param>
		/// <param name="candidate">Mit keresünk</param>
		/// <returns>Megegyezik-e</returns>
		public static bool IsMatch(byte[] array, int position, byte[] candidate)
		{
			if (candidate.Length > array.Length - position)
			{
				return false;
			}

			return !candidate.Where((current, i) => array[position + i] != current).Any();
		}

		#endregion Byte tömbben keresés

		/// <summary>
		/// Új példány létrehozása reflectionnal
		/// </summary>
		/// <param name="t">Osztály típus</param>
		/// <returns>Objektum példány</returns>
		public static object GetNewObject(this Type t)
			=> t.GetConstructor(ArrayExt.Empty<Type>())?.Invoke(ArrayExt.Empty<object>());

#if NETFX_45
		public static async System.Threading.Tasks.Task<WebResponse> GetResponseAsync(this WebRequest request, System.Threading.CancellationToken ct)
		{
			using (ct.Register(request.Abort, false))
			{
				try
				{
					var response = await request.GetResponseAsync();
					return response;
				}
				catch (WebException ex) when (ct.IsCancellationRequested)
				{
					// WebException is thrown when request.Abort() is called
					// the WebException will be available as Exception.InnerException
					throw new OperationCanceledException(ex.Message, ex, ct);
				}
			}
		}
#endif

		/// <summary>
		/// Internet elérhetőség ellenőrzése
		/// </summary>
		/// <returns></returns>
		public static bool IsConnectedToInternet()
		{
			bool returnValue;

			try
			{
				returnValue = NativeMethods.InternetGetConnectedState(out int _, 0);
			}
			catch
			{
				returnValue = false;
			}

			return returnValue;
		}

		/// <summary>
		/// Host string darabolása
		/// </summary>
		/// <param name="host">Be-/kimeneti string</param>
		/// <param name="port">FTP port</param>
		/// <param name="passive">Passzív mód</param>
		public static string ParseHost(string host, out int? port, out bool passive)
		{
			port = null;
			passive = false;

			if (!host.Contains(":"))
			{
				return host;
			}

			var parts = Regex.Match(host, @"^(?<host>.+?(?=:))(?::(?<port>\d+))?.*$");
			if (parts.Groups["port"].Success)
			{
				if (int.TryParse(parts.Groups["port"].Value, out int p))
				{
					port = p;
				}
			}

			passive = host.IndexOf(":passive", StringComparison.OrdinalIgnoreCase) > -1;
			return parts.Groups["host"].Value;
		}

		public static void ParseHost(ref string host, out int port, out bool passive)
		{
			host = ParseHost(host, out int? p, out passive);
			port = p ?? 0;
		}

		/// <summary>
		/// Értéktípus példányt castol Nullable verzióra
		/// </summary>
		/// <typeparam name="T">Érték típus</typeparam>
		/// <param name="self"></param>
		/// <returns></returns>
#if NETFX_45
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
		public static T? AsNullable<T>(this T self) where T : struct => self;

		/// <summary>
		/// Felsorolt kulcs érték párból Dictionary-t képez kulcs és érték alapján
		/// </summary>
		/// <typeparam name="TKey">Kulcs típusa</typeparam>
		/// <typeparam name="TValue">Érték típusa</typeparam>
		/// <param name="self">Felsorolt kulcs érték párból</param>
		/// <returns>Dictionary eredmény</returns>
		public static Dictionary<TKey, TValue> AsDictionary<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> self)
			=> self.ToDictionary(i => i.Key, i => i.Value);

		/// <summary>
		/// Megkeresi az adott kulcshoz tartozó értéket a Dictionaryban, ha nem találja, akkor a defaultot adja vissza
		/// </summary>
		/// <typeparam name="TKey">Kulcs típusa</typeparam>
		/// <typeparam name="TValue">Érték típusa</typeparam>
		/// <param name="self">Adott dictionary</param>
		/// <param name="key">Keresett kulcs</param>
		/// <returns></returns>
		public static TValue GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> self, TKey key)
			=> self.TryGetValue(key, out var value) ? value : default;

		/// <summary>
		/// Két double érték egyenlőség vizsgálata
		/// </summary>
		/// <param name="first">Első szám</param>
		/// <param name="second">Második szám</param>
		/// <param name="epsilon">Epszilon az összehasonlításhoz</param>
		/// <returns>Egyenlő-e (nagyjából) a két szám</returns>
		public static bool SameValue(double first, double second, double? epsilon = null)
		{
			if (epsilon is null)
			{
				epsilon = Math.Max(Math.Min(Math.Abs(first), Math.Abs(second)) * double.Epsilon, double.Epsilon);
			}

			return first > second
				? first - second <= epsilon
				: second - first <= epsilon;
		}
	}
}
