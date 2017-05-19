using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BabelFish.Services
{
    public class Authentication
    {
        public const string FetchTokenUri = "https://api.cognitive.microsoft.com/sts/v1.0";

        private readonly string subscriptionKey;
        private readonly Timer accessTokenRenewer;

        private string token;

        //Access token expires every 10 minutes. Renew it every 9 minutes only.
        private const int RefreshTokenDuration = 9;

        public Authentication(string subscriptionKey)
        {
            this.subscriptionKey = subscriptionKey;

            // renew the token every specfied minutes
            accessTokenRenewer = new Timer(new TimerCallback(OnTokenExpiredCallback),
                                           this,
                                           TimeSpan.FromMinutes(RefreshTokenDuration),
                                           TimeSpan.FromMilliseconds(-1));
        }

        public async Task<string> GetAccessTokenAsync()
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                token = await FetchTokenAsync(FetchTokenUri, subscriptionKey);
            }

            return token;
        }

        private async Task RenewAccessTokenAsync()
        {
            token = await FetchTokenAsync(FetchTokenUri, subscriptionKey);
            Debug.WriteLine("Renewed token.");
        }

        private async void OnTokenExpiredCallback(object stateInfo)
        {
            try
            {
                await RenewAccessTokenAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed renewing access token. Details: {ex.Message}");
            }
            finally
            {
                try
                {
                    accessTokenRenewer.Change(TimeSpan.FromMinutes(RefreshTokenDuration), TimeSpan.FromMilliseconds(-1));
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to reschedule the timer to renew access token. Details: {ex.Message}");
                }
            }
        }

        private async Task<string> FetchTokenAsync(string fetchUri, string subscriptionKey)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", subscriptionKey);
                var uriBuilder = new UriBuilder(fetchUri);
                uriBuilder.Path += "/issueToken";

                var result = await client.PostAsync(uriBuilder.Uri.AbsoluteUri, null);
                return await result.Content.ReadAsStringAsync();
            }
        }
    }
}
