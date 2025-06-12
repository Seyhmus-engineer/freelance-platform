using Google.Cloud.Firestore;

namespace FreelancePlatform.Models
{
    [FirestoreData]
    public class Proje
    {
        [FirestoreProperty]
        public int ProjeID { get; set; }

        [FirestoreProperty]
        public string Baslik { get; set; } = string.Empty;

        [FirestoreProperty]
        public string Aciklama { get; set; } = string.Empty;

        [FirestoreProperty]
        public double Butce { get; set; }

        [FirestoreProperty]
        public string ParaBirimi { get; set; } = "TL";

        [FirestoreProperty]
        public DateTime OlusturmaTarihi { get; set; } = DateTime.Now;

        [FirestoreProperty]
        public string YayinlayanEmail { get; set; } = string.Empty;

        [FirestoreProperty]
        public string YayinlayanAdSoyad { get; set; } = string.Empty;
    }
}
