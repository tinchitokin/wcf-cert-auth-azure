using System;
using System.Runtime.Serialization;

namespace SmartHotel.Registration.Wcf.Models
{
    [DataContract]
    public class RegistrationDaySummary
    {
        [DataMember]
        public DateTime Date { get; set; }

        [DataMember]
        public int CheckIns { get; set; }

        [DataMember]
        public int CheckOuts { get; set; }
    }
}