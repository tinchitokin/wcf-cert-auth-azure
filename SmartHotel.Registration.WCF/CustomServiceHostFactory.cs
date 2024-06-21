using System;
using System.ServiceModel;
using System.ServiceModel.Activation;

namespace SmartHotel.Registration.Wcf
{
    public class CustomServiceHostFactory : ServiceHostFactory
    {
        protected override ServiceHost CreateServiceHost(Type serviceType, Uri[] baseAddresses)
        {
            return new CustomServiceHost(serviceType, baseAddresses);
        }
    }
}