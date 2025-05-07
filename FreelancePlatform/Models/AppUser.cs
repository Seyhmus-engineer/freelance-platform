namespace FreelancePlatform.Models
{
    public class AppUser
    {
        public int KullaniciID { get; set; }
        public string AdSoyad { get; set; } = string.Empty;
        public string EmailAdres { get; set; } = string.Empty;
        public string Sifre { get; set; } = string.Empty;
        public string Rol { get; set; } = string.Empty;
    }
}
