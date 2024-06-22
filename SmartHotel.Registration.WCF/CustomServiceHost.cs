using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using System;
using System.Configuration;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;
using System.ServiceModel.Description;

namespace SmartHotel.Registration.Wcf
{
    public class CustomServiceHost : ServiceHost
    {
        private static X509Certificate2 _cachedCertificate = null;
        private static readonly object _cacheLock = new object();

        public CustomServiceHost(Type serviceType, params Uri[] baseAddresses)
            : base(serviceType, baseAddresses)
        {
        }

        protected override void ApplyConfiguration()
        {
            base.ApplyConfiguration();
            SetupServiceCertificate();
        }

        private void SetupServiceCertificate()
        {
            var certificate = RetrieveCertificateFromKeyVault();
            if (Description.Behaviors.Find<ServiceCredentials>() is ServiceCredentials credentials)
            {
                credentials.ServiceCertificate.Certificate = certificate;
            }
        }

        private X509Certificate2 RetrieveCertificateFromKeyVault()
        {
            // Check if the certificate is already cached
            if (_cachedCertificate != null)
            {
                return _cachedCertificate;
            }

            lock (_cacheLock)
            {
                // Double-check locking
                if (_cachedCertificate != null)
                {
                    return _cachedCertificate;
                }

                try
                {
                    var keyVaultUrl = ConfigurationManager.AppSettings["KeyVaultUrl"];
                    var certificateName = ConfigurationManager.AppSettings["DomainName"]; ;
                    var client = new SecretClient(new Uri(keyVaultUrl), new DefaultAzureCredential());
                    Console.WriteLine("Retrieving certificate from Azure Key Vault...");
                    KeyVaultSecret secret = client.GetSecretAsync(certificateName).GetAwaiter().GetResult();
                    var bytes = Convert.FromBase64String(secret.Value);
                    _cachedCertificate = new X509Certificate2(bytes, (string)null, X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.Exportable);
                    return _cachedCertificate;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error retrieving certificate: {ex.Message}");
                    throw; // Rethrow the exception to ensure the caller is aware an error occurred.
                }
            }
        }
    }
}