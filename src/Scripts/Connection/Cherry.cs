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
		
		LogSend(HttpMethod.Get, path);
		
		Result<T> result = new();

		try
		{
			HttpResponseMessage response = await Client.GetAsync(path);
			string content = await GetResponseContentOrThrow(response);
			result.data = JsonConvert.DeserializeObject<T>(content);
		}
		catch (Exception exception)
		{
			LogError(HttpMethod.Get, path, exception.Message);
			result.error = exception.Message;
		}

		return result;
	}

	public async Task<Result<T>> Post<T>(object data)
	{
		string path = GetPath();

		LogSend(HttpMethod.Post, path);

		Result<T> result = new();

		try
		{
			HttpResponseMessage response = await Client.PostAsync(path, ToJsonPayload(data));
			string content = await GetResponseContentOrThrow(response);
			result.data = JsonConvert.DeserializeObject<T>(content);
		}
		catch (Exception exception)
		{
			LogError(HttpMethod.Post, path, exception.Message);
			result.error = exception.Message;
		}

		return result;
	}

	public async Task<Result<T>> Put<T>(object data)
	{
		string path = GetPath();

		LogSend(HttpMethod.Put, path);

		Result<T> result = new();

		try
		{
			HttpResponseMessage response = await Client.PutAsync(path, ToJsonPayload(data));
			string content = await GetResponseContentOrThrow(response);
			result.data = JsonConvert.DeserializeObject<T>(content);
		}
		catch (Exception exception)
		{
			LogError(HttpMethod.Put, path, exception.Message);
			result.error = exception.Message;
		}

		return result;
	}

	public async Task<Result<T>> Delete<T>()
	{
		string path = GetPath();

		LogSend(HttpMethod.Delete, path);

		Result<T> result = new();

		try
		{
			HttpResponseMessage response = await Client.DeleteAsync(path);
			string content = await GetResponseContentOrThrow(response);
			result.data = JsonConvert.DeserializeObject<T>(content);
		}
		catch (Exception exception)
		{
			LogError(HttpMethod.Delete, path, exception.Message);
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
	
	private async Task<string> GetResponseContentOrThrow(HttpResponseMessage response)
	{
		string content = await response.Content.ReadAsStringAsync();
		
		if (response.IsSuccessStatusCode)
			return content;
		
		string code = $"{response.StatusCode} ({response.ReasonPhrase})";

		Exception exception = string.IsNullOrEmpty(content)
			? new Exception(code)
			: new Exception($"{code}: {content}");

		throw exception;
	}

	private StringContent ToJsonPayload(object data)
	{
		string json = JsonConvert.SerializeObject(data);
		StringContent payload = new(json, Encoding.UTF8, "application/json");
		return payload;
	}
	
	private void LogSend(HttpMethod method, string path)
	{
		Logger.Log($"[SENT] [{method} {path}]");
	}
	
	private void LogError(HttpMethod method, string path, string message)
	{
		Logger.Log($"Error [{method} {path}]: {message}");
	}
}