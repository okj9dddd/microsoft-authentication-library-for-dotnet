using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Test.LabInfrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit
{
    internal static class StringExtensions
    {
        public static SecureString ToSecureString(this string val)
        {
            var secureString = new SecureString();
            val.ToCharArray().ToList().ForEach(c => secureString.AppendChar(c));
            return secureString;
        }
    }

    [TestClass]
    public class ManualPerfTest
    {
        [TestMethod]
        public async Task TestAcquireTokenSilentPerformanceAsync()
        {
            const string clientId = "f0e0429e-060c-42d3-9375-913eb7c7a62d";
            const string tenantId = "72f988bf-86f1-41af-91ab-2d7cd011db47";

            string[] scopes = new[] { "https://graph.microsoft.com/user.read" };

            var api = new LabServiceApi(new KeyVaultSecretsProvider());
            var labUser = api.GetLabResponse(
                new UserQuery
                {
                    UserType = UserType.Member,
                    IsFederatedUser = false
                }).User;

            Console.WriteLine($"Received LabUser: {labUser.Upn} from LabServiceApi.");

            var app = PublicClientApplicationBuilder
                .Create(clientId)
                .WithAuthority(new Uri($"https://login.microsoftonline.com/{tenantId}/"), true)
                .WithLogging((LogLevel level, string message, bool containsPii) =>
                {
                    Console.WriteLine("{0}: {1}", level, message);
                })
                .Build();

            var result = await app
                .AcquireTokenByUsernamePassword(scopes, labUser.Upn, labUser.Password.ToSecureString())
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            IEnumerable<IAccount> accounts = await app.GetAccountsAsync().ConfigureAwait(false);

            for (int i = 0; i < 5000; i++)
            {
                Console.Write($"Attempt {i}: ");
                var sw = Stopwatch.StartNew();
                var result2 = await app.AcquireTokenSilent(scopes, accounts.FirstOrDefault()).ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
                Console.WriteLine(result2.AccessToken);
                sw.Stop();
                Console.WriteLine($"Elapsed: {sw.ElapsedMilliseconds}ms");
            }
        }

        internal static class Utilities
        {
            internal static (string[], IPublicClientApplication) GetPublicClient(
            string resource,
            string tenant,
            Uri baseAuthority,
            bool validateAuthority,
            string clientId,
            string cacheFilename,
            string cacheDirectory,
            string serviceName,
            string accountName)
            {
                // tenant can be null
                resource = resource ?? throw new ArgumentNullException(nameof(resource));

                Console.WriteLine($"Using resource: '{resource}', tenant:'{tenant}'");

                var scopes = new string[] { resource + "/.default" };

                Console.WriteLine($"Using scopes: '{string.Join(",", scopes)}'");

                var authority = $"{baseAuthority.AbsoluteUri}{tenant}";
                Console.WriteLine($"GetPublicClient for authority: '{authority}' ValidateAuthority: '{validateAuthority}'");

                Uri authorityUri = new Uri(authority);
                var appBuilder = PublicClientApplicationBuilder
                    .Create(clientId)
                    .WithAuthority(authorityUri, validateAuthority)
                    .WithLogging((Microsoft.Identity.Client.LogLevel level, string message, bool containsPii) =>
                    {
                        Console.WriteLine("{0}: {1}", level, message);
                    });

                var app = appBuilder.Build();
                Console.WriteLine($"Built public client");

                //var storageCreationPropsBuilder = new MsalStorageCreationPropertiesBuilder(cacheFilename, cacheDirectory);
                //storageCreationPropsBuilder = storageCreationPropsBuilder.WithMacKeyChain(serviceName, accountName);
                //var storageCreationProps = storageCreationPropsBuilder.Build();

                // This hooks up our custom cache onto the one used by MSAL
                //var cacheHelper = new MsalCacheHelper(storageCreationProps);
                //cacheHelper.RegisterCache(app.UserTokenCache);

                Console.WriteLine($"Cache registered");

                return (scopes, app);
            }
        }

        [TestMethod]
        public async Task DoStuffAsync()
        {
            string resource = "https://management.core.windows.net/";
            string tenant = "organizations";
            Uri baseAuthority = new Uri("https://login.microsoftonline.com/");
            bool validateAuthority = false;
            string clientId = "872cd9fa-d31f-45e0-9eab-6e460a02d1f1";
            string cacheFileName = "msal.cache";
            string cacheDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), ".IdentityService");

            (var scopes, var app) = Utilities.GetPublicClient(resource, tenant, baseAuthority, validateAuthority, clientId, cacheFileName, cacheDirectory, null, null);
            var result1 = await app.AcquireTokenInteractive(scopes, null).ExecuteAsync().ConfigureAwait(false);
            var account = result1.Account;

            for (int i = 0; i < 5; i++)
            {
                Console.Write($"Attempt {i}: ");
                var sw = Stopwatch.StartNew();
                var result2 = await app.AcquireTokenSilent(scopes, account).ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
                sw.Stop();
                Console.WriteLine($"{sw.ElapsedMilliseconds}ms");
            }

        }


        [TestMethod]
        public async Task TestAcquireTokenSilentPerformance2Async()
        {
            const string clientId = "872cd9fa-d31f-45e0-9eab-6e460a02d1f1";
            //const string tenantId = "72f988bf-86f1-41af-91ab-2d7cd011db47";

            string[] scopes = new[] { "https://management.core.windows.net/.default" };

            var api = new LabServiceApi(new KeyVaultSecretsProvider());
            var labUser = api.GetLabResponse(
                new UserQuery
                {
                    UserType = UserType.Member,
                    IsFederatedUser = false
                }).User;

            Console.WriteLine($"Received LabUser: {labUser.Upn} from LabServiceApi.");

            var app = PublicClientApplicationBuilder
                .Create(clientId)
                .WithAuthority(new Uri($"https://login.microsoftonline.com/organizations/"), true)
                .WithLogging((LogLevel level, string message, bool containsPii) =>
                {
                    Console.WriteLine("{0}: {1}", level, message);
                })
                .Build();

            var result = await app
                .AcquireTokenByUsernamePassword(scopes, labUser.Upn, labUser.Password.ToSecureString())
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            IEnumerable<IAccount> accounts = await app.GetAccountsAsync().ConfigureAwait(false);

            for (int i = 0; i < 5000; i++)
            {
                Console.Write($"Attempt {i}: ");
                var sw = Stopwatch.StartNew();
                var result2 = await app.AcquireTokenSilent(scopes, accounts.FirstOrDefault()).ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
                Console.WriteLine(result2.AccessToken);
                sw.Stop();
                Console.WriteLine($"Elapsed: {sw.ElapsedMilliseconds}ms");
            }
        }
    }
}
