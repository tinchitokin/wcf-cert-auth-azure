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
        private static string keyVaultUri = ConfigurationManager.AppSettings["KeyVaultUri"];
        private static string certName = ConfigurationManager.AppSettings["CertName"];
        private static string subjectName = ConfigurationManager.AppSettings["SubjectName"];
        private static string wshttpBinding = "WSHttpBinding_IService";
        private static string basichttpBinding = "BasicHttpBinding_IService";

        public static void Main(string[] args)
        {
            CallWcfService(wshttpBinding);
            CallWcfService(basichttpBinding);
        }

        private static void CallWcfService(string binding)
        {
            var client = CreateServiceClient(binding);

            try
            {
                var result = client.GetTodayRegistrations();

                Registration[] todayRegistrations = result;

                Console.WriteLine($"Today's Registrations from {binding}.");
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

            Console.ReadLine();
        }

        private static ServiceClient CreateServiceClient(string bindingName)
        {
            var client = new ServiceClient(bindingName);
            SetClientCertificate(client, bindingName);
            return client;
        }

        private static void SetClientCertificate(ServiceClient client, string binding)
        {
            X509Certificate2 certificate = null;

            if (binding.Contains(basichttpBinding))
            {
                certificate = GetCertificate();  // Use this method if the certificate is stored in the local machine store
            }
            else
            {
                certificate = GetCertificateFromKeyVault().Result;
                if (certificate != null)
                {
                    client.ClientCredentials.ClientCertificate.Certificate = certificate;
                }
            }

            if (certificate == null)
            {
                Console.WriteLine("Certificate not found. Please check the certificate details.");
            }
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