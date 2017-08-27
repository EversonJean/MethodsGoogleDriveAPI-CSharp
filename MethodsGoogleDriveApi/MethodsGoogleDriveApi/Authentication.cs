using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System.IO;
using System.Reflection;
using System.Threading;

namespace MethodsGoogleDriveApi
{
    public class Authentication
    {
        public static UserCredential Authenticate()
        {
            UserCredential credentials;

            using (var stream = new FileStream("client_id.json", FileMode.Open, FileAccess.Read))
            {
                var currentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                var directoryCredentials = Path.Combine(currentDirectory, "credential");

                credentials = GoogleWebAuthorizationBroker.AuthorizeAsync(GoogleClientSecrets.Load(stream).Secrets,
                    new[] { DriveService.Scope.Drive }, "user", CancellationToken.None,
                    new FileDataStore(directoryCredentials, true)).Result;
            }
            return credentials;
        }

        public static DriveService OpenService(UserCredential credentials)
        {
            return new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credentials
            });
        }
    }
}
