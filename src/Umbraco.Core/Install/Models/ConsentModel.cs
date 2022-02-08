using System.Runtime.Serialization;

namespace Umbraco.Cms.Core.Install.Models
{
    [DataContract(Name = "consent")]
    public class ConsentModel
    {
        [DataMember(Name = "consentGranted")]
        public bool ConsentGranted { get; set; }
    }
}
