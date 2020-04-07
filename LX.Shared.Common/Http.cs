using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using LX.Common.Extensions;

namespace LX.Common
{
#if EXTERN
	public
#else
	internal
#endif
	static class Http
	{
		public class FileParameter
		{
			public string FilePath { get; }
			public string RemoteFileName { get; }
			public string ContentType { get; }
			public FileParameter(string filePath, string remoteFileName = null) : this(filePath, remoteFileName, null) { }
			public FileParameter(string filePath, string remoteFileName, string contenttype)
			{
				FilePath = filePath;
				RemoteFileName = remoteFileName;
				ContentType = contenttype;
			}
		}

		public static Task<(bool IsSuccess, byte[] Response)> Post(Uri uri, params (string Param, object Value)[] postParams)
			=> Post(uri, -1, default, postParams);

		public static Task<(bool IsSuccess, byte[] Response)> Post(Uri uri, CancellationToken ct, params (string Param, object Value)[] postParams)
			=> Post(uri, -1, ct, postParams);

		public static Task<(bool IsSuccess, byte[] Response)> Post(Uri uri, int? timeout, params (string Param, object Value)[] postParams)
			=> Post(uri, timeout, default, postParams);

		public static async Task<(bool IsSuccess, byte[] Response)> Post(Uri uri, int? timeout, CancellationToken ct, params (string Param, object Value)[] postParams)
		{
			using (var httpClient = new HttpClient())
			{
				if (timeout.HasValue && timeout > -1)
				{
					httpClient.Timeout = TimeSpan.FromMilliseconds(timeout.Value);
				}

				using (var form = new MultipartFormDataContent())
				{
					if (postParams.Any())
					{
						foreach (var param in postParams)
						{
							if (param.Value is FileParameter fileParameter)
							{
								byte[] bytes = await Task.Run(() => File.ReadAllBytes(fileParameter.FilePath), ct).ConfigureAwait(false);
								var b = new ByteArrayContent(bytes);
								form.Add(b, param.Param, fileParameter.RemoteFileName);
							}
							else
							{
								var s = new StringContent(param.Value.ToString());
								form.Add(s, param.Param);
							}
						}
					}

					using (var resp = await httpClient.PostAsync(uri, form, ct))
					{
						return (resp.IsSuccessStatusCode, await resp.Content.ReadAsByteArrayAsync());
					}
				}
			}
		}

		public static async Task<byte[]> Get(Uri uri, int? timeout = null, CancellationToken ct = default)
		{
			byte[] response;

			var httpWebRequest = WebRequest.Create(uri);
			httpWebRequest.Method = WebRequestMethods.Http.Get;
			if (timeout.HasValue && timeout > -1)
			{
				httpWebRequest.Timeout = timeout.Value;
			}

			using (var httpResponse = await httpWebRequest.GetResponseAsync(ct))
			using (var stream = httpResponse.GetResponseStream())
			{
				response = stream.ReadToEnd();
			}

			return response;
		}
	}
}
