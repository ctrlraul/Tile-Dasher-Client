using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;
using TD.Lib;

namespace TD.Connection;

public class Cherry
{
	private static readonly Logger Logger = new("Request");
	private readonly HttpClient Client;
	private readonly string Endpoint;
	private readonly Dictionary<string, object> queryParams = new();
    
	
	public Cherry(HttpClient client, string endpoint)
	{
		Endpoint = endpoint;
		Client = client;
	}
	

	public Cherry Query(string key, object value)
	{
		queryParams.Add(key, value);
		return this;
	}
	

	public async Task<Result<T>> Get<T>()
	{
		string path = GetPath();
		
		Logger.Log($"* --> [{HttpMethod.Get} {path}]");
		
		Result<T> result = new();

		try
		{
			HttpResponseMessage response = await Client.GetAsync(path);
			string content = await response.Content.ReadAsStringAsync();

			if (!response.IsSuccessStatusCode)
			{
				string code = $"{response.StatusCode} ({response.ReasonPhrase})";
				
				if (string.IsNullOrEmpty(content))
					throw new Exception(code);
				
				throw new Exception($"{code}: {content}");
			}
			
			result.data = JsonConvert.DeserializeObject<T>(content);
		}
		catch (Exception exception)
		{
			Logger.Log($"Error [{HttpMethod.Get} {path}]: {exception.Message}");
			result.error = exception.Message;
		}

		return result;
	}

	public async Task<Result<T>> Post<T>(object data)
	{
		string path = GetPath();

		Logger.Log($"* --> [{HttpMethod.Post} {path}]");

		Result<T> result = new();

		try
		{
			string json = JsonConvert.SerializeObject(data);
			StringContent payload = new(json, Encoding.UTF8, "application/json");
			HttpResponseMessage response = await Client.PostAsync(path, payload);
			string content = await response.Content.ReadAsStringAsync();
			
			Logger.Log("content:", content);
			
			if (!response.IsSuccessStatusCode)
			{
				string code = $"{response.StatusCode} ({response.ReasonPhrase})";

				if (string.IsNullOrEmpty(content))
					throw new Exception(code);

				throw new Exception($"{code}: {content}");
			}

			result.data = JsonConvert.DeserializeObject<T>(content);
		}
		catch (Exception exception)
		{
			Logger.Log($"Error [{HttpMethod.Post} {path}]: {exception.Message}");
			result.error = exception.Message;
		}

		return result;
	}


	private string GetPath()
	{
		string fullPath = Endpoint;

		if (queryParams.Count > 0)
			fullPath += "?" + string.Join("&", queryParams.Select(ToQuery));

		return fullPath;
	}
	
	private string ToQuery(KeyValuePair<string, object> kvp)
	{
		return HttpUtility.UrlEncode(kvp.Key) + "=" + HttpUtility.UrlEncode(kvp.Value.ToString());
	}
}