using System.Collections.Generic;

namespace FreelancePlatform.Models
{
    public class MesajDetayViewModel
    {
        public int ProjeID { get; set; }
        public string ProjeBaslik { get; set; }
        public string GirisYapanEmail { get; set; }
        public string GirisYapanAdSoyad { get; set; }
        public string KarsiTarafEmail { get; set; }
        public string KarsiTarafAdSoyad { get; set; }
        public List<Mesaj> Mesajlar { get; set; }
    }
}

