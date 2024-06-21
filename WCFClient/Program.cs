using Azure;
using Azure.Identity;
using Azure.Security.KeyVault.Certificates;
using Azure.Security.KeyVault.Secrets;
using System;
using System.Configuration;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;
using System.Threading.Tasks;
using WCFClient.ServiceReference1;

namespace WCFClient
{
    public class Program
    {
        private static string keyVaultUri = ConfigurationManager.AppSettings["ida:KeyVaultUri"];
        private static string certName = ConfigurationManager.AppSettings["ida:CertName"];

        public static void Main(string[] args)
        {
            var client = new ServiceClient("WSHttpBinding_IService");

            // Set the client certificate
            SetClientCertificate(client);

            try
            {
                var result = ExecuteWithRetry(() => client.GetTodayRegistrations());

                Registration[] todayRegistrations = result;

                Console.WriteLine("Today's Registrations:");
                foreach (var registration in todayRegistrations)
                {
                    Console.WriteLine($"- {registration.CustomerName}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error calling service: {ex.Message}");
            }
            finally
            {
                if (client.State == CommunicationState.Faulted)
                {
                    client.Abort();
                }
                else
                {
                    client.Close();
                }
            }

            Console.Read();
        }

        public static T ExecuteWithRetry<T>(Func<T> operation, int maxRetries = 3)
        {
            int attempt = 0;
            while (true)
            {
                try
                {
                    return operation();
                }
                catch (Exception ex) when (ex is TimeoutException || ex is CommunicationException)
                {
                    attempt++;
                    if (attempt >= maxRetries)
                    {
                        throw; // Rethrow the exception after the last attempt
                    }
                    // Optionally, implement a backoff strategy here
                    Console.WriteLine($"Attempt {attempt} failed, retrying...");
                }
            }
        }

        private static async Task CallAsyncMethods(ServiceClient client)
        {
            try
            {
                Registration[] todayRegistrationsAsync = await client.GetTodayRegistrationsAsync();
                Console.WriteLine("Today's Registrations (Async):");
                foreach (var registration in todayRegistrationsAsync)
                {
                    Console.WriteLine($"- {registration.CustomerName}");
                }

                RegistrationDaySummary todaySummaryAsync = await client.GetTodayRegistrationSummaryAsync();
                Console.WriteLine($"Today's Summary (Async): Check-ins {todaySummaryAsync.CheckIns}, Check-outs {todaySummaryAsync.CheckOuts}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error calling async service: {ex.Message}");
            }
        }

        private static void SetClientCertificate(ServiceClient client)
        {
            //var certificate = GetCertificate(); // Use this method if the certificate is stored in the local machine store

            X509Certificate2 certificate = GetCertificateFromKeyVault(certName).Result;
            if (certificate != null)
            {
                client.ClientCredentials.ClientCertificate.Certificate = certificate;
            }
            else
            {
                Console.WriteLine("Certificate not found. Please check the certificate details.");
            }
        }

        private static async Task<X509Certificate2> GetCertificateFromKeyVault(string certName)
        {
            var keyVaultUriBuilder = new UriBuilder(keyVaultUri);
            var keyVaultUrl = keyVaultUriBuilder.Uri;

            var credential = new DefaultAzureCredential();
            var certificateClient = new CertificateClient(keyVaultUrl, credential);

            try
            {
                // Retrieve the certificate with its policy directly
                KeyVaultCertificateWithPolicy certificate = await certificateClient.GetCertificateAsync(certName);

                // Check if the certificate contains a private key
                if (certificate.Policy.Exportable == true)
                {
                    // Download the certificate's secret which contains the private key
                    var secretClient = new SecretClient(keyVaultUrl, credential);
                    KeyVaultSecret secret = await secretClient.GetSecretAsync(certName);

                    // Convert the secret value to a byte array and create an X509Certificate2 object
                    byte[] certBytes = Convert.FromBase64String(secret.Value);
                    return new X509Certificate2(certBytes, (string)null, X509KeyStorageFlags.MachineKeySet);
                }
                else
                {
                    Console.WriteLine("The certificate does not contain a private key or is not marked as exportable.");
                    return null;
                }
            }
            catch (RequestFailedException ex)
            {
                Console.WriteLine($"An error occurred while retrieving the certificate: {ex.Message}");
                return null;
            }
        }

        private static X509Certificate2 GetCertificate()
        {
            X509Store store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadOnly);
            var certificates = store.Certificates.Find(X509FindType.FindBySubjectName, "mydomain.com", false);
            store.Close();

            if (certificates.Count > 0)
            {
                return certificates[0];
            }
            else
            {
                return null;
            }
        }
    }
}