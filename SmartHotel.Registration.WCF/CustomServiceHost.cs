using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using System;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Threading.Tasks;

namespace SmartHotel.Registration.Wcf
{
    public class CustomServiceHost : ServiceHost
    {
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
            var certificate = RetrieveCertificateFromKeyVaultAsync().GetAwaiter().GetResult();
            if (this.Description.Behaviors.Find<ServiceCredentials>() is ServiceCredentials credentials)
            {
                credentials.ServiceCertificate.Certificate = certificate;
            }
        }

        private async Task<X509Certificate2> RetrieveCertificateFromKeyVaultAsync()
        {
            var keyVaultUrl = "https://kvmm.vault.azure.net/";
            var certificateName = "mydomain";
            var client = new SecretClient(new Uri(keyVaultUrl), new DefaultAzureCredential());
            KeyVaultSecret secret = await client.GetSecretAsync(certificateName);
            var bytes = Convert.FromBase64String(secret.Value);
            // Specify X509KeyStorageFlags
            return new X509Certificate2(bytes, (string)null, X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.Exportable);
        }
    }
}