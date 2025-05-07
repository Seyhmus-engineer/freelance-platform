namespace FreelancePlatform.Models
{
    public class Mesaj
    {
        public int MesajID { get; set; }
        public int ProjeID { get; set; } // Hangi projeye ait
        public string GonderenEmail { get; set; } = string.Empty;
        public string AliciEmail { get; set; } = string.Empty;
        public string Icerik { get; set; } = string.Empty;
        public DateTime Tarih { get; set; } = DateTime.Now;
    }
}
