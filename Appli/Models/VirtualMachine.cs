using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace jadorelecloudgaming.Models
{
    public class VirtualMachine
    {
        public int id { get; set; }

        [DisplayName("Nom")]
        public string name { get; set; }

        [DisplayName("Adresse IP Publique")]
        public string ip { get; set; }

        [DisplayName("Login")]
        public string login { get; set; }

        [DisplayName("Mot de passe")]
        public string password { get; set; }

        public bool Powered { get; set; }
    }
}
