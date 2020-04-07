using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Serialization;
using System.Text;
using LX.Common.Database.ConnectionProvider;
using LX.Common.Database.Extensions;
using LX.Common.Database.Settings;
using LX.Common.EventHandlers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LX.Common.Database
{
	/// <summary>
	/// eCall üzenet típusa
	/// </summary>
#if EXTERN
	public
#else
	internal
#endif
	enum ECallType
	{
		/// <summary>
		/// Hibabejelentés
		/// </summary>
		[EnumMember(Value = "error")]
		Error,

		/// <summary>
		/// Panaszbejelentés
		/// </summary>
		[EnumMember(Value = "complaint")]
		Complaint,

		/// <summary>
		/// Kérés
		/// </summary>
		[EnumMember(Value = "request")]
		Request,

		/// <summary>
		/// Információ átadás
		/// </summary>
		[EnumMember(Value = "info")]
		Info,

		/// <summary>
		/// Automata küldés (programok hibajelentése)
		/// </summary>
		[EnumMember(Value = "auto")]
		Auto,
	}

#if EXTERN
	public
#else
	internal
#endif
	readonly struct ConnectionParams
	{
		public string Url { get; }

		public string Usernamne { get; }

		public string Password { get; }
		public ConnectionParams(string url, string usernamne, string password)
		{
			Url = url;
			Usernamne = usernamne;
			Password = password;
		}
	}

	/// <summary>
	/// Egy eCall üzenet adattagjait foglalja magába
	/// </summary>
#if EXTERN
	public
#else
	internal
#endif
	struct ECallStatus
	{
		/// <summary>
		/// CRM státusz id
		/// </summary>
		[JsonProperty("id")]
		public int Id { get; set; }

		/// <summary>
		/// Elixír ecall id
		/// </summary>
		[JsonProperty("elixir_ecall_id")]
		public int ECallId { get; set; }

		/// <summary>
		/// CRM ecall id
		/// </summary>
		[JsonProperty("crm_ecall_id")]
		public int CRMECallId { get; set; }

		/// <summary>
		/// Válasz dátuma
		/// </summary>
		[JsonProperty("add_date")]
		public DateTime AddDate { get; set; }

		/// <summary>
		/// Ecall új státusza
		/// </summary>
		[JsonProperty("status")]
		public string Status { get; set; }

		/// <summary>
		/// Státusz üzenet
		/// </summary>
		[JsonProperty("desc")]
		public string Description { get; set; }

		public ECallStatus(int id, int eCallId, int cRMECallId, DateTime addDate, string status, string description)
		{
			Id = id;
			ECallId = eCallId;
			CRMECallId = cRMECallId;
			AddDate = addDate;
			Status = status;
			Description = description;
		}
	}

	/// <summary>
	/// eCall paraméterek
	/// </summary>
#if EXTERN
	public
#else
	internal
#endif
	readonly struct ECallData
	{
		/// <summary>
		/// ECall azonosító, ha van
		/// </summary>
		[JsonProperty("elixir_ecall_id")]
		public int? Id { get; }

		/// <summary>
		/// Létrehozás dátuma
		/// </summary>
		[JsonProperty("write_date")]
		public DateTime Created { get; }

		/// <summary>
		/// Tárgy
		/// </summary>
		[JsonProperty("text")]
		public string Subject { get; }

		/// <summary>
		/// Küldő email címe
		/// </summary>
		[JsonProperty("sender")]
		public string Sender { get; }

		/// <summary>
		/// Üzenet
		/// </summary>
		[JsonProperty("comment")]
		public string Comment { get; }

		/// <summary>
		/// info/request/complaint/error
		/// </summary>
		[JsonIgnore]
		public ECallType Type { get; }

		[JsonProperty("type")]
		public string TypeString => Type.ToString().ToLowerInvariant();

		public ECallData(string subject, string sender, string comment, ECallType type = ECallType.Auto)
		{
			Id = null;
			Created = DateTime.Now;
			Subject = subject;
			Sender = sender;
			Comment = comment;
			// id nélkül csak automata lehet a típus
			Type = Id is null ? ECallType.Auto : type;
		}
	}

#if EXTERN
	public
#else
	internal
#endif
	static class ECall
	{
		private static void InsertNewStatuses(IReadOnlyCollection<ECallStatus> eCallStatuses)
		{
			const string sql = // SQL
			#region SQL
@"UPDATE OR INSERT INTO t_ecall_statuses (ecall_id, status_id, crm_ecall_id, add_date, status, desciption)
VALUES (@ecall_id, @status_id, @crm_ecall_id, @add_date, @status, @desciption)
MATCHING (status_id);";
			#endregion SQL

			var connProv = new LXConnectionProvider();
			using var conn = connProv.GetOpenConnection();
			using var trans = conn.BeginTransaction();

			foreach (var eCallStatus in eCallStatuses)
			{
				connProv.Sql(sql)
					.AddParam("ecall_id", eCallStatus.ECallId)
					.AddParam("status_id", eCallStatus.Id)
					.AddParam("crm_ecall_id", eCallStatus.CRMECallId)
					.AddParam("add_date", eCallStatus.AddDate)
					.AddParam("status", eCallStatus.Status)
					.AddParam("desciption", eCallStatus.Description)
					.ExecSql(trans);
			}

			trans.Commit();
		}

		private static void SendNewStatusNotifications(IReadOnlyCollection<ECallStatus> eCallStatuses)
		{
			const string sql = // SQL
			#region SQL
@"execute block as
declare variable ec_id integer;
declare variable u_id integer;
declare variable ec_subject type of column t_ecalls.ec_subject;
begin
	for select ec_id, u_id, ec_subject
		from t_ecalls ec
		join t_user on ec_uid = u_id
	where ec_id in ({0})
	into :ec_id, :u_id, :ec_subject
	do begin
		execute procedure p_sendmessage_ext(:u_id, 'Új státuszfrissítés érkezett a(z) ""'|| :ec_subject ||'"" című eCall üzenethez.', null, 214, null, :ec_id);
	end
end";
			#endregion SQL

			LXDb.New(string.Format(sql, string.Join(",", eCallStatuses.Select(s => s.ECallId).Distinct()))).ExecSql();
		}
		private static void UpdateECallsWithCRMIds(IReadOnlyCollection<(int? Id, int? CRMeCallId)> ecallIdPairs)
		{
			const string sql = "UPDATE t_ecalls SET ec_crm_ecallid = @ec_crm_ecallid where ec_id = @ec_id;";

			var connProv = new LXConnectionProvider();
			using var conn = connProv.GetOpenConnection();
			using var trans = conn.BeginTransaction();

			foreach (var (Id, CRMeCallId) in ecallIdPairs)
			{
				connProv.Sql(sql)
					.AddParam("ec_crm_ecallid", CRMeCallId)
					.AddParam("ec_id", Id)
					.ExecSql(trans);
			}

			trans.Commit();
		}

		private static string ProcessResponseJson(JObject json)
		{
			if (json["success"].Value<bool>())
			{
				// response

				// ecalls
				var ecallIdPairs = json["response"]["ecalls"]
					.Select(e => (Id: e["elixir_ecall_id"].Value<int?>(), CRMeCallId: e["crm_ecall_id"].Value<int?>()))
					.ToArray();

				if (ecallIdPairs.Any())
				{
					UpdateECallsWithCRMIds(ecallIdPairs);
				}

				// statuses
				var statuses = json["response"]["statuses"]
					.Select(e => e.ToObject<ECallStatus>())
					.ToArray();

				if (statuses.Any())
				{
					// új/módosított státuszok elmentése
					InsertNewStatuses(statuses);

					// értesítések küldése
					SendNewStatusNotifications(statuses);
				}

				return null;
			}
			else
			{
				return json["error"].Value<string>();
			}
		}

		public static (bool Success, string Result) SendECall(in ConnectionParams connectionParams, in ECallData eCallData)
		{
			// ha ki van kapcsolva az automata üzenetek küldése,
			// akkor nem történik meg a POST
			if (ECallType.Auto == eCallData.Type && "IGEN" == ConfigSync.Get("ECALL_DISABLE_AUTO"))
			{
				return (true, string.Empty);
			}

			var json = new JObject(
				new JProperty("module", "ecall"),
				new JProperty("ecalls",
					new JArray(
						JObject.FromObject(eCallData)
					)
				)
			);

			using var http = new HttpClient();
			using var content = new StringContent(json.ToString(), Encoding.UTF8, "application/json");

			byte[] byteArray = Encoding.ASCII.GetBytes(string.Concat(connectionParams.Usernamne, ":", connectionParams.Password));
			http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

			bool success;
			string msg;
			try
			{
				var res = http.PostAsync(connectionParams.Url, content).GetAwaiter().GetResult();
				var resultJson = JObject.Parse(res.Content.ReadAsStringAsync().GetAwaiter().GetResult());
				success = (HttpStatusCode.OK == res.StatusCode) && (resultJson["success"]?.Value<bool>() ?? false);
				msg = ProcessResponseJson(resultJson);
			}
			catch (Exception ex)
			{
				success = false;
				msg = $"{ex.GetType().Name} - {ex.Message}";
				MessageHandler.SendMessage(nameof(ECall), ex.ToString(), MessageHandler.MessageType.Error);
			}

			return (success, msg);
		}
	}
}
