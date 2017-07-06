using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using SitecoreSidekick.Services.Interface;

namespace SitecoreSidekick.Services
{
	public class HttpClientService : IHttpClientService
	{
		private readonly HttpClient _client;

		public HttpClientService(string baseUrl = null)
		{
			_client = new HttpClient();			
			if (!string.IsNullOrWhiteSpace(baseUrl))
				_client.BaseAddress = new Uri(baseUrl);
		}

		public async Task<string> Post(string url, string content)
		{
			using (var sc = new StringContent(content))
			{
				var response = await _client.PostAsync(url, sc).ConfigureAwait(false);
				return Encoding.UTF8.GetString(await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false));
			}
		}
	}
}
