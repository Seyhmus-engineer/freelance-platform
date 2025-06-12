using FirebaseAdmin;
using FreelancePlatform.Models;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text.Json;
using FirebaseAdmin.Auth;

namespace FreelancePlatform.Helpers
{
    public class FirebaseHelper
    {
        private static bool isInitialized = false;
        private static FirestoreDb? firestoreDb;

        public static void InitializeFirebase()
        {
            if (isInitialized)
                return;

            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "firebase-key.json");
            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", path);

            if (FirebaseApp.DefaultInstance == null)
            {
                FirebaseApp.Create(new AppOptions()
                {
                    Credential = GoogleCredential.FromFile(path),
                });
            }

            firestoreDb = FirestoreDb.Create("freelanceplatform-1155a");
            isInitialized = true;
        }

        public static async Task AddDataAsync(string koleksiyon, string belgeId, object veri)
        {
            InitializeFirebase();
            DocumentReference docRef = firestoreDb!.Collection(koleksiyon).Document(belgeId);
            await docRef.SetAsync(veri);
        }

        public static async Task<Dictionary<string, object>?> GetDataAsync(string koleksiyon, string belgeId)
        {
            InitializeFirebase();
            DocumentReference docRef = firestoreDb!.Collection(koleksiyon).Document(belgeId);
            DocumentSnapshot snapshot = await docRef.GetSnapshotAsync();
            return snapshot.Exists ? snapshot.ToDictionary() : null;
        }
        public static async Task<AppUser?> GetUserByEmailAsync(string email)
        {
            InitializeFirebase();
            DocumentReference docRef = firestoreDb!.Collection("kullanicilar").Document(email);
            DocumentSnapshot snapshot = await docRef.GetSnapshotAsync();

            if (!snapshot.Exists)
                return null;

            var dict = snapshot.ToDictionary();
            if (dict == null)
                return null;

            return new AppUser
            {
                KullaniciID = dict.ContainsKey("KullaniciID") ? Convert.ToInt32(dict["KullaniciID"]) : 0,
                AdSoyad = dict.ContainsKey("AdSoyad") ? dict["AdSoyad"]?.ToString() ?? "" : "",
                EmailAdres = dict.ContainsKey("EmailAdres") ? dict["EmailAdres"]?.ToString() ?? "" : "",
                Sifre = dict.ContainsKey("Sifre") ? dict["Sifre"]?.ToString() ?? "" : "",
                Rol = dict.ContainsKey("Rol") ? dict["Rol"]?.ToString() ?? "" : "",
                ProfilResmiBase64 = dict.ContainsKey("ProfilResmiBase64") ? dict["ProfilResmiBase64"]?.ToString() ?? "" : ""
            };
        }


        /// <summary>
        /// Tüm kullanıcıları çeken ve Dictionary döndüren yardımcı metot
        /// </summary>
        public static async Task<Dictionary<string, AppUser>> GetAllUsersAsync()
        {
            InitializeFirebase();
            var snapshot = await firestoreDb!.Collection("kullanicilar").GetSnapshotAsync();
            var users = new Dictionary<string, AppUser>();

            foreach (var doc in snapshot.Documents)
            {
                var dict = doc.ToDictionary();
                if (dict != null && dict.ContainsKey("KullaniciID"))
                {
                    var user = new AppUser
                    {
                        KullaniciID = dict.ContainsKey("KullaniciID") ? Convert.ToInt32(dict["KullaniciID"]) : 0,
                        AdSoyad = dict.ContainsKey("AdSoyad") ? dict["AdSoyad"]?.ToString() ?? "" : "",
                        EmailAdres = dict.ContainsKey("EmailAdres") ? dict["EmailAdres"]?.ToString() ?? "" : "",
                        Sifre = dict.ContainsKey("Sifre") ? dict["Sifre"]?.ToString() ?? "" : "",
                        Rol = dict.ContainsKey("Rol") ? dict["Rol"]?.ToString() ?? "" : "",
                        ProfilResmiBase64 = dict.ContainsKey("ProfilResmiBase64") ? dict["ProfilResmiBase64"]?.ToString() ?? "" : ""
                    };
                    users[doc.Id] = user;
                }
            }

            return users;
        }


        /// <summary>
        /// Firebase'deki en büyük KullaniciID değerini bulur.
        /// </summary>
        public static async Task<int> GetMaxKullaniciIDAsync()
        {
            var users = await GetAllUsersAsync();
            if (users.Count == 0) return 0;
            return users.Values.Max(u => u.KullaniciID);
        }


        //Şifre güncelleme işlemleri
        public static async Task<bool> UpdateUserPasswordByEmailAsync(string email, string yeniSifre)
        {
            var firebaseKeyPath = Path.Combine(AppContext.BaseDirectory, "../../firebase-key.json");

            var firebaseConfig = new FirebaseAdmin.AppOptions()
            {
                Credential = Google.Apis.Auth.OAuth2.GoogleCredential.FromFile(firebaseKeyPath)
            };

            if (FirebaseAdmin.FirebaseApp.DefaultInstance == null)
                FirebaseAdmin.FirebaseApp.Create(firebaseConfig);

            var auth = FirebaseAdmin.Auth.FirebaseAuth.DefaultInstance;

            try
            {
                // Kullanıcıyı e-posta ile bul
                var userRecord = await auth.GetUserByEmailAsync(email);

                var args = new FirebaseAdmin.Auth.UserRecordArgs()
                {
                    Uid = userRecord.Uid,
                    Password = yeniSifre
                };

                await auth.UpdateUserAsync(args);

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Firebase Auth şifre güncelleme hatası: " + ex.Message);
                return false;
            }
        }


        //Firebase databasede alan oluşturma işlemleri
        public static async Task<string> CreateFirebaseUserAsync(string email, string password)
        {
            var firebaseConfig = new FirebaseAdmin.AppOptions()
            {
                Credential = Google.Apis.Auth.OAuth2.GoogleCredential.FromFile("firebase-key.json")
            };

            if (FirebaseAdmin.FirebaseApp.DefaultInstance == null)
                FirebaseAdmin.FirebaseApp.Create(firebaseConfig);

            var auth = FirebaseAdmin.Auth.FirebaseAuth.DefaultInstance;

            var userArgs = new FirebaseAdmin.Auth.UserRecordArgs()
            {
                Email = email,
                Password = password
            };

            var userRecord = await auth.CreateUserAsync(userArgs);

            return userRecord.Uid;
        }


        //Şifremi unuttum için mail gönderimi
        public static async Task<bool> SendPasswordResetEmailAsync(string email)
        {
            string apiKey = "AIzaSyCTcMheF5uL66Btscj4LrXG_wrpG_D0ca0"; 

            var client = new HttpClient();
            var url = $"https://identitytoolkit.googleapis.com/v1/accounts:sendOobCode?key={apiKey}";

            var requestData = new
            {
                requestType = "PASSWORD_RESET",
                email = email
            };

            var json = JsonSerializer.Serialize(requestData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var response = await client.PostAsync(url, content);
                var responseString = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("Şifre sıfırlama e-postası gönderildi: " + responseString);
                    return true;
                }
                else
                {
                    Console.WriteLine("Hata: " + responseString);
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Şifre sıfırlama isteği hatası: " + ex.Message);
                return false;
            }
        }


        //Aut olarak giriş yapma
public static async Task<bool> SignInWithEmailAndPasswordAsync(string email, string password)
    {
        string apiKey = "AIzaSyCTcMheF5uL66Btscj4LrXG_wrpG_D0ca0"; // 🔴 Buraya kendi API Key'ini yaz

        var client = new HttpClient();
        var url = $"https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key={apiKey}";

        var requestData = new
        {
            email = email,
            password = password,
            returnSecureToken = true
        };

        var json = JsonSerializer.Serialize(requestData);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            var response = await client.PostAsync(url, content);
            var responseString = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("Firebase Auth giriş başarılı: " + responseString);
                return true;
            }
            else
            {
                Console.WriteLine("Firebase Auth giriş hatası: " + responseString);
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Firebase Auth giriş isteği hatası: " + ex.Message);
            return false;
        }
    }

        //Databaseye proje ekleme işlemleri
        public static async Task AddProjectAsync(Proje proje)
        {
            InitializeFirebase();

            DocumentReference docRef = firestoreDb!.Collection("projeler").Document(proje.ProjeID.ToString());

            var veri = new Dictionary<string, object>
{
    { "ProjeID", proje.ProjeID },
    { "Baslik", proje.Baslik },
    { "Aciklama", proje.Aciklama },
    { "Butce", Convert.ToDouble(proje.Butce) }, 
    { "ParaBirimi", proje.ParaBirimi },
    { "YayinlayanEmail", proje.YayinlayanEmail },
    { "YayinlayanAdSoyad", proje.YayinlayanAdSoyad },
     { "OlusturmaTarihi", proje.OlusturmaTarihi.ToUniversalTime() }
};


            await docRef.SetAsync(veri);
        }

        //Databasedeki max proje ID getirme
        public static async Task<int> GetMaxProjeIDAsync()
        {
            InitializeFirebase();
            var snapshot = await firestoreDb!.Collection("projeler").GetSnapshotAsync();
            if (snapshot.Count == 0) return 0;

            int maxID = 0;
            foreach (var doc in snapshot.Documents)
            {
                if (doc.TryGetValue<long>("ProjeID", out var projeIDObj))
                {
                    int projeID = Convert.ToInt32(projeIDObj);
                    if (projeID > maxID)
                        maxID = projeID;
                }
            }
            return maxID;
        }

        //Kullanıcıya ait projeleri getirme
        public static async Task<List<Proje>> GetProjectsByUserEmailAsync(string email)
        {
            InitializeFirebase();
            var snapshot = await firestoreDb!.Collection("projeler")
                                              .WhereEqualTo("YayinlayanEmail", email)
                                              .GetSnapshotAsync();

            var userProjects = new List<Proje>();
            foreach (var doc in snapshot.Documents)
            {
                var dict = doc.ToDictionary();
                var proje = new Proje
                {
                    ProjeID = Convert.ToInt32(dict["ProjeID"]!),
                    Baslik = dict["Baslik"]!.ToString()!,
                    Aciklama = dict["Aciklama"]!.ToString()!,
                    Butce = Convert.ToDouble(dict["Butce"]!), // ✅ decimal yerine double
                    ParaBirimi = dict["ParaBirimi"]!.ToString()!,
                    YayinlayanEmail = dict["YayinlayanEmail"]!.ToString()!,
                    YayinlayanAdSoyad = dict["YayinlayanAdSoyad"]!.ToString()!,
                    OlusturmaTarihi = ((Timestamp)dict["OlusturmaTarihi"]!).ToDateTime().ToUniversalTime()
                };


                userProjects.Add(proje);
            }
            return userProjects.OrderByDescending(p => p.OlusturmaTarihi).ToList();
        }

       
        //Tüm projeleri getirme
        public static async Task<List<Proje>> GetAllProjectsAsync()
        {
            InitializeFirebase();
            var snapshot = await firestoreDb!.Collection("projeler").GetSnapshotAsync();

            var projects = new List<Proje>();
            foreach (var doc in snapshot.Documents)
            {
                var dict = doc.ToDictionary();
                var proje = new Proje
                {
                    ProjeID = Convert.ToInt32(dict["ProjeID"]!),
                    Baslik = dict["Baslik"]!.ToString()!,
                    Aciklama = dict["Aciklama"]!.ToString()!,
                    Butce = Convert.ToDouble(dict["Butce"]!), // ✅ Doğru: object → double
                    ParaBirimi = dict["ParaBirimi"]!.ToString()!,
                    YayinlayanEmail = dict["YayinlayanEmail"]!.ToString()!,
                    YayinlayanAdSoyad = dict["YayinlayanAdSoyad"]!.ToString()!,
                    OlusturmaTarihi = ((Timestamp)dict["OlusturmaTarihi"]!).ToDateTime().ToUniversalTime()
                };
                projects.Add(proje);
            }
            return projects.OrderByDescending(p => p.OlusturmaTarihi).ToList();
        }
        public static async Task<List<Basvuru>> GetBasvurularByProjectIdsAsync(List<int> projeIdList)
{
    InitializeFirebase();
    var snapshot = await firestoreDb!.Collection("basvurular").GetSnapshotAsync();

    var basvurular = new List<Basvuru>();
    foreach (var doc in snapshot.Documents)
    {
        var dict = doc.ToDictionary();
        int projeId = Convert.ToInt32(dict["ProjeID"]!);

        if (projeIdList.Contains(projeId))
        {
            basvurular.Add(new Basvuru
            {
                BasvuruID = Convert.ToInt32(dict["BasvuruID"]!),
                ProjeID = projeId,
                FreelancerEmail = dict["FreelancerEmail"]!.ToString()!,
                Mesaj = dict["Mesaj"]!.ToString()!,
                TeklifTutari = Convert.ToDecimal(dict["TeklifTutari"]!),
                BasvuruTarihi = ((Timestamp)dict["BasvuruTarihi"]!).ToDateTime().ToUniversalTime()
            });
        }
    }
    return basvurular.OrderByDescending(b => b.BasvuruTarihi).ToList();
}


        //Başvuruları getirme
public static async Task<List<Basvuru>> GetBasvurularByFreelancerEmailAsync(string freelancerEmail)
{
    InitializeFirebase();
    var snapshot = await firestoreDb!.Collection("basvurular").GetSnapshotAsync();

    var basvurular = new List<Basvuru>();
    foreach (var doc in snapshot.Documents)
    {
        var dict = doc.ToDictionary();
        if (dict["FreelancerEmail"]!.ToString() == freelancerEmail)
        {
            basvurular.Add(new Basvuru
            {
                BasvuruID = Convert.ToInt32(dict["BasvuruID"]!),
                ProjeID = Convert.ToInt32(dict["ProjeID"]!),
                FreelancerEmail = dict["FreelancerEmail"]!.ToString()!,
                Mesaj = dict["Mesaj"]!.ToString()!,
                TeklifTutari = Convert.ToDecimal(dict["TeklifTutari"]!),
                BasvuruTarihi = ((Timestamp)dict["BasvuruTarihi"]!).ToDateTime().ToUniversalTime()
            });
        }
    }
    return basvurular.OrderByDescending(b => b.BasvuruTarihi).ToList();
}


        //Başvuru yapma işlemleri
        public static async Task AddBasvuruAsync(Basvuru basvuru)
        {
            InitializeFirebase();
            DocumentReference docRef = firestoreDb!.Collection("basvurular").Document();

            var veri = new Dictionary<string, object>
    {
        { "BasvuruID", basvuru.BasvuruID },
        { "ProjeID", basvuru.ProjeID },
        { "FreelancerEmail", basvuru.FreelancerEmail },
        { "Mesaj", basvuru.Mesaj },
        { "TeklifTutari", Convert.ToDouble(basvuru.TeklifTutari) }, // 🔥 decimal → double dönüşümü
        { "BasvuruTarihi", Timestamp.FromDateTime(basvuru.BasvuruTarihi.ToUniversalTime()) }
    };

            await docRef.SetAsync(veri);
        }

    

        //Tüm başvuruları getirme işlemleri
    public static async Task<List<Basvuru>> GetAllBasvurularAsync()
        {
            InitializeFirebase();
            var snapshot = await firestoreDb!.Collection("basvurular").GetSnapshotAsync();

            var basvurular = new List<Basvuru>();
            foreach (var doc in snapshot.Documents)
            {
                var dict = doc.ToDictionary()!;
                basvurular.Add(new Basvuru
                {
                    BasvuruID = Convert.ToInt32(dict["BasvuruID"]!),
                    ProjeID = Convert.ToInt32(dict["ProjeID"]!),
                    FreelancerEmail = dict["FreelancerEmail"]!.ToString()!,
                    Mesaj = dict["Mesaj"]!.ToString()!,
                    TeklifTutari = Convert.ToDecimal(dict["TeklifTutari"]!),
                    BasvuruTarihi = ((Google.Cloud.Firestore.Timestamp)dict["BasvuruTarihi"]!).ToDateTime().ToUniversalTime()
                });
            }
            return basvurular;
        }

        //Mesaj gönderme işlemleri
        public static async Task AddMesajAsync(Mesaj mesaj)
        {
            InitializeFirebase();

            DocumentReference docRef = firestoreDb!.Collection("mesajlar").Document();

            var veri = new Dictionary<string, object>
    {
        { "ProjeID", mesaj.ProjeID },
        { "GonderenEmail", mesaj.GonderenEmail ?? "" },
        { "AliciEmail", mesaj.AliciEmail ?? "" },
        { "MesajIcerik", mesaj.MesajIcerik ?? "" }, // null'a karşı önlem
        { "GonderimTarihi", Google.Cloud.Firestore.Timestamp.FromDateTime(mesaj.GonderimTarihi.ToUniversalTime()) }
    };

            // Kontrol için log yazdır
            Console.WriteLine("Firestore mesaj eklenecek veri: " + string.Join(", ", veri));

            await docRef.SetAsync(veri);
        }




        //Mesajları getirme işlemleri
        public static async Task<List<Mesaj>> GetMesajlarAsync(string kullaniciEmail)
        {
            InitializeFirebase();
            var snapshot = await firestoreDb!.Collection("mesajlar").GetSnapshotAsync();

            var liste = new List<Mesaj>();
            foreach (var doc in snapshot.Documents)
            {
                var dict = doc.ToDictionary();

                // Kullanıcıya ait mesaj mı? kontrolü
                if (dict.ContainsKey("GonderenEmail") && dict.ContainsKey("AliciEmail") &&
                    (dict["GonderenEmail"]?.ToString() == kullaniciEmail || dict["AliciEmail"]?.ToString() == kullaniciEmail))
                {
                    // Null kontrolü ekleyelim 🔥
                    var mesajIcerik = dict.ContainsKey("MesajIcerik") ? dict["MesajIcerik"]?.ToString() ?? "" : "";

                    // GonderimTarihi de null olabilir, kontrol edelim 🔥
                    DateTime gonderimTarihi = DateTime.MinValue;
                    if (dict.ContainsKey("GonderimTarihi") && dict["GonderimTarihi"] is Google.Cloud.Firestore.Timestamp ts)
                        gonderimTarihi = ts.ToDateTime().ToUniversalTime();

                    liste.Add(new Mesaj
                    {
                        ProjeID = dict.ContainsKey("ProjeID") ? Convert.ToInt32(dict["ProjeID"]) : 0,
                        GonderenEmail = dict.ContainsKey("GonderenEmail") ? dict["GonderenEmail"]?.ToString() ?? "" : "",
                        AliciEmail = dict.ContainsKey("AliciEmail") ? dict["AliciEmail"]?.ToString() ?? "" : "",
                        MesajIcerik = mesajIcerik,
                        GonderimTarihi = gonderimTarihi
                    });
                }
            }

            return liste.OrderByDescending(m => m.GonderimTarihi).ToList();
        }


        //Başvuru yaparkenki ilk mesajı getirme işlemleri
        public static async Task<Mesaj?> GetIlkBasvuruMesajiAsync(int projeId, string freelancerEmail)
        {
            InitializeFirebase();
            var snapshot = await firestoreDb!.Collection("basvurular")
                .WhereEqualTo("ProjeID", projeId)
                .WhereEqualTo("FreelancerEmail", freelancerEmail)
                .GetSnapshotAsync();

            var doc = snapshot.Documents.FirstOrDefault();
            if (doc != null)
            {
                var dict = doc.ToDictionary();
                return new Mesaj
                {
                    ProjeID = Convert.ToInt32(dict["ProjeID"]!),
                    GonderenEmail = dict["FreelancerEmail"]!.ToString()!,
                    AliciEmail = "", // işveren e-postasını buraya eklemek istersen buradan ayarla
                    MesajIcerik = dict["Mesaj"]!.ToString()!,
                    GonderimTarihi = ((Google.Cloud.Firestore.Timestamp)dict["BasvuruTarihi"]!).ToDateTime().ToLocalTime()
                };
            }
            return null;
        }


        //İlgili projeyi getirme işlemleri
        public static async Task<Proje?> GetProjectByIdAsync(int projeId)
        {
            InitializeFirebase();
            var doc = await firestoreDb!.Collection("projeler").Document(projeId.ToString()).GetSnapshotAsync();
            if (!doc.Exists) return null;
            var dict = doc.ToDictionary();

            return new Proje
            {
                ProjeID = Convert.ToInt32(dict["ProjeID"]),
                Baslik = dict["Baslik"]?.ToString() ?? "",
                Aciklama = dict["Aciklama"]?.ToString() ?? "",
                Butce = Convert.ToDouble(dict["Butce"]),
                ParaBirimi = dict["ParaBirimi"]?.ToString() ?? "",
                YayinlayanEmail = dict["YayinlayanEmail"]?.ToString() ?? "",
                YayinlayanAdSoyad = dict["YayinlayanAdSoyad"]?.ToString() ?? "",
                OlusturmaTarihi = ((Timestamp)dict["OlusturmaTarihi"]).ToDateTime().ToUniversalTime()
            };
        }

        public static FirestoreDb GetFirestoreDb()
{
    InitializeFirebase();
    return firestoreDb!;
}

        //Profil güncelleme işlemleri
        public static async Task UpdateUserDataAsync(string collection, string documentId, Dictionary<string, object> data)
        {
            var db = FirestoreDb.Create("freelanceplatform-1155a"); // Proje ID'nizi buraya yazın
            var docRef = db.Collection(collection).Document(documentId);

            // UpdateAsync sadece mevcut alanları günceller
            await docRef.UpdateAsync(data);
        }

      public static async Task<bool> UpdateUserPasswordAsync(string email, string newPassword)
{
    try
    {
        // FirebaseApp'in başlatılması, eğer daha önce başlatılmamışsa
        if (FirebaseApp.DefaultInstance == null)
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "firebase-key.json");
            FirebaseApp.Create(new AppOptions()
            {
                Credential = GoogleCredential.FromFile(path)
            });
        }

        var auth = FirebaseAuth.DefaultInstance;

        var user = await auth.GetUserByEmailAsync(email);
        if (user == null) return false;

        var args = new UserRecordArgs()
        {
            Uid = user.Uid,
            Password = newPassword
        };

        await auth.UpdateUserAsync(args);
        return true;
    }
    catch (Exception ex)
    {
        Console.WriteLine("Firebase Auth şifre güncelleme hatası: " + ex.Message);
        return false;
    }
}


    }
}
