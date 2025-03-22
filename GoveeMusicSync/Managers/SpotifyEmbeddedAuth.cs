using SpotifyAPI.Web.Auth;
using SpotifyAPI.Web;
using System.IO;

namespace GoveeMusicSync.Managers
{
    class SpotifyEmbeddedAuth
    {

        //https://johnnycrazy.github.io/SpotifyAPI-NET/docs/authorization_code

        private EmbedIOAuthServer? _server;
        private readonly string clientID;
        private readonly string clientSecret;
        public event Func<object, SpotifyClient, Task>? SpotifyClientGenerated;

        public SpotifyEmbeddedAuth(string clientID, string clientSecret)
        {
            this.clientID = clientID;
            this.clientSecret = clientSecret;
        }


        public async Task Auth(bool refresh)
        {

            string? token = getTokenFromFile();
            if (token != null && !refresh)
            {
                SpotifyClient spotifyClient = new SpotifyClient(token);
                if (spotifyClient != null)
                {
                    this.SpotifyClientGenerated?.Invoke(this, spotifyClient);
                    return;
                }
            }

            _server = new EmbedIOAuthServer(new Uri("http://localhost:5543/callback"), 5543);
            await _server.Start();

            _server.AuthorizationCodeReceived += OnAuthorizationCodeReceived;
            _server.ErrorReceived += OnErrorReceived;

            var request = new LoginRequest(_server.BaseUri, clientID, LoginRequest.ResponseType.Code)
            {
                Scope = [Scopes.UserReadPlaybackState, Scopes.UserReadCurrentlyPlaying]
            };

            BrowserUtil.Open(request.ToUri());

        }

        private async Task OnAuthorizationCodeReceived(object sender, AuthorizationCodeResponse response)
        {
            if (_server != null)
                await _server.Stop();

            var config = SpotifyClientConfig.CreateDefault();
            var tokenResponse = await new OAuthClient(config).RequestToken(
              new AuthorizationCodeTokenRequest(
                clientID, clientSecret, response.Code, new Uri("http://localhost:5543/callback")
              )
            );

            saveTokenToFile(tokenResponse.AccessToken);
            this.SpotifyClientGenerated?.Invoke(this, new SpotifyClient(tokenResponse.AccessToken));
        }

        private async Task OnErrorReceived(object sender, string error, string state)
        {
            Console.WriteLine($"Aborting authorization, error received: {error}");
            if (_server != null)
                await _server.Stop();
        }

        private void saveTokenToFile(string token)
        {
            File.WriteAllText("token.txt", token);
        }

        private string? getTokenFromFile()
        {
            if (File.Exists("token.txt"))
            {
                return File.ReadAllText("token.txt");

            }

            return null;
        }
    }
}
