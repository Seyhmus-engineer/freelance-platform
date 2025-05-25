namespace FreelancePlatform.Models
{
    public class Proje
    {
        public int ProjeID { get; set; }
        public string Baslik { get; set; } = string.Empty;
        public string Aciklama { get; set; } = string.Empty;
        public decimal Butce { get; set; }
        public string ParaBirimi { get; set; } = "TL";

        public DateTime OlusturmaTarihi { get; set; } = DateTime.Now;
        public string YayınlayanEmail { get; set; } = string.Empty;
        public string YayinlayanAdSoyad { get; set; } = string.Empty;
    }
}
