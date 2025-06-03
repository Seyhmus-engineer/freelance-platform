namespace FreelancePlatform.Models
{
    public class Basvuru
    {
        public int BasvuruID { get; set; }
        public int ProjeID { get; set; }
        public string ProjeBaslik { get; set; }
        public string ProjeYayinlayanAdSoyad { get; set; } = string.Empty;
        public string FreelancerAdSoyad { get; set; }  
        public string FreelancerEmail { get; set; } = string.Empty;
        public string Mesaj { get; set; } = string.Empty;
        public decimal TeklifTutari { get; set; }
        public string ParaBirimi { get; set; }    
        public string TeklifMesaji { get; set; }   
        public DateTime BasvuruTarihi { get; set; } = DateTime.Now;
    }
}
