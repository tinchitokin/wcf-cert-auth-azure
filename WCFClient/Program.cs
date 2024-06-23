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
        static readonly string keyVaultUri = ConfigurationManager.AppSettings["KeyVaultUri"];
        static readonly string certName = ConfigurationManager.AppSettings["CertName"];
        static readonly string subjectName = ConfigurationManager.AppSettings["SubjectName"];
        static readonly string wshttpBinding = "WSHttpBinding_IService";
        static readonly string basichttpBinding = "BasicHttpBinding_IService";

        public static async Task Main(string[] args)
        {
            await CallWcfServiceAsync(wshttpBinding);
            await CallWcfServiceAsync(basichttpBinding);
            Console.ReadLine();
        }

        private static async Task CallWcfServiceAsync(string binding)
        {
            var client = await CreateServiceClientAsync(binding);

            try
            {
                var result = await client.GetTodayRegistrationsAsync();

                Console.WriteLine($"Today's Registrations from {binding}:");
                foreach (var registration in result)
                {
                    Console.WriteLine($"- {registration.CustomerName}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception calling service: {ex.Message}\n\r Proxy state: {client.State}");
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
        }

        private static async Task<ServiceClient> CreateServiceClientAsync(string bindingName)
        {
            var client = new ServiceClient(bindingName);

            if (!bindingName.Contains(basichttpBinding))
            {
                await SetClientCertificateAsync(client);
            }
            return client;
        }

        private static async Task SetClientCertificateAsync(ServiceClient client)
        {
            X509Certificate2 certificate = await GetCertificateAsync();
            if (certificate != null)
            {
                client.ClientCredentials.ClientCertificate.Certificate = certificate;
            }
            else
            {
                Console.WriteLine("Certificate not found. Please check the certificate details.");
            }
        }

        private static async Task<X509Certificate2> GetCertificateAsync()
        {
            // Attempt to retrieve from local store first
            X509Certificate2 certificate = GetCertificate();
            if (certificate != null) return certificate;

            // If not found, attempt to retrieve from Key Vault
            return await GetCertificateFromKeyVault();
        }

        private static async Task<X509Certificate2> GetCertificateFromKeyVault()
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
            var certificates = store.Certificates.Find(X509FindType.FindBySubjectName, subjectName, false);
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