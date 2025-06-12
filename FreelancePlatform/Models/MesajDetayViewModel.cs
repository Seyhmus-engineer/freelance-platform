namespace FreelancePlatform.Models
{
    public class MesajDetayViewModel
    {
        public int ProjeID { get; set; }
        public string ProjeBaslik { get; set; }
        public string GirisYapanEmail { get; set; }
        public string KarsiTarafEmail { get; set; }
        public List<Mesaj> Mesajlar { get; set; }
    }
}
