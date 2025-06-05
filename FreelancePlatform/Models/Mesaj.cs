namespace FreelancePlatform.Models
{
    public class Mesaj
    {
        public int MesajID { get; set; }
        public int ProjeID { get; set; }
        public string GonderenEmail { get; set; }
        public string AliciEmail { get; set; }
        public string GonderenAdSoyad { get; set; }
        public string AliciAdSoyad { get; set; }
        public string MesajIcerik { get; set; }
        public DateTime GonderimTarihi { get; set; }
        public bool Okundu { get; set; } = false;
        public bool OkunduMu { get; set; } = false;

    }



}
