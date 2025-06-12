using Microsoft.AspNetCore.Mvc;
using FreelancePlatform.Models;
using System.Text.Json;
using FreelancePlatform.Helpers;

namespace FreelancePlatform.Controllers
{
    public class BasvuruController : Controller
    {
        /// <summary>
        /// GET: Belirli bir projeye başvuru formu
        /// </summary>
        public async Task<IActionResult> Basvur(int projeId)
        {
            // Kullanıcının oturumda olup olmadığını kontrol et
            var userJson = HttpContext.Session.GetString("Kullanici");
            if (string.IsNullOrEmpty(userJson))
                return RedirectToAction("Giris", "Kullanici");

            // Oturumdaki kullanıcıyı deserialize et
            var user = JsonSerializer.Deserialize<AppUser>(userJson)!;

            // Sadece Freelancer rolündeki kullanıcılar başvuru yapabilir
            if (user.Rol != "Freelancer")
                return Unauthorized();

            // Firestore'dan proje bilgilerini al
            var proje = (await FirebaseHelper.GetAllProjectsAsync())
                            .FirstOrDefault(p => p.ProjeID == projeId);

            if (proje == null) return NotFound();

            // ViewBag ile projeye dair bilgileri View'a gönder
            ViewBag.ProjeId = projeId;
            ViewBag.ParaBirimi = proje.ParaBirimi;
            ViewBag.ProjeBaslik = proje.Baslik;

            return View();
        }

        /// <summary>
        /// Firestore’daki tüm başvuruları getirir (admin kullanımında yararlı olabilir)
        /// </summary>
        public static async Task<List<Basvuru>> GetBasvurular()
        {
            // Tüm başvuruları çekmek için boş bir proje ID listesi gönder
            var snapshot = await FirebaseHelper.GetBasvurularByProjectIdsAsync(new List<int>());
            return snapshot;
        }

        /// <summary>
        /// POST: Projeye başvuru yapıldığında çalışır
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Basvur(int projeId, string mesaj, decimal teklifTutari)
        {
            // Kullanıcı oturumu kontrolü
            var userJson = HttpContext.Session.GetString("Kullanici");
            if (string.IsNullOrEmpty(userJson))
                return RedirectToAction("Giris", "Kullanici");

            var user = JsonSerializer.Deserialize<AppUser>(userJson)!;
            if (user.Rol != "Freelancer")
                return Unauthorized();

            // Proje kontrolü
            var proje = (await FirebaseHelper.GetAllProjectsAsync())
                            .FirstOrDefault(p => p.ProjeID == projeId);

            if (proje == null) return NotFound();

            // 🔴 1️⃣ Başvuru oluştur ve Firestore’a kaydet
            var basvuru = new Basvuru
            {
                BasvuruID = new Random().Next(1, 1000000),
                ProjeID = projeId,
                FreelancerEmail = user.EmailAdres,
                Mesaj = mesaj,
                TeklifTutari = teklifTutari,
                BasvuruTarihi = DateTime.UtcNow
            };
            await FirebaseHelper.AddBasvuruAsync(basvuru);

            // 🟢 2️⃣ Başvuru mesajını ayrı olarak "mesajlar" koleksiyonuna ekle
            var mesajKaydi = new Mesaj
            {
                ProjeID = projeId,
                GonderenEmail = user.EmailAdres,
                AliciEmail = proje.YayinlayanEmail,
                MesajIcerik = mesaj,
                GonderimTarihi = DateTime.UtcNow
            };
            await FirebaseHelper.AddMesajAsync(mesajKaydi);

            TempData["Basarili"] = "Başvurunuz ve mesajınız başarıyla gönderildi!";
            return RedirectToAction("Listele", "Proje");
        }

        /// <summary>
        /// İşveren olarak, kendi projelerine gelen başvuruları görüntüleme
        /// </summary>
        public async Task<IActionResult> GelenBasvurular()
        {
            // Kullanıcı oturumu kontrolü
            var userJson = HttpContext.Session.GetString("Kullanici");
            if (string.IsNullOrEmpty(userJson))
                return RedirectToAction("Giris", "Kullanici");

            var user = JsonSerializer.Deserialize<AppUser>(userJson)!;

            // İşverenin kendi projelerinin ID'lerini al
            var projelerim = (await FirebaseHelper.GetAllProjectsAsync())
                                .Where(p => p.YayinlayanEmail == user.EmailAdres)
                                .Select(p => p.ProjeID)
                                .ToList();

            // Bu projelere gelen tüm başvuruları çek
            var gelenBasvurular = await FirebaseHelper.GetBasvurularByProjectIdsAsync(projelerim);

            return View(gelenBasvurular);
        }

        /// <summary>
        /// Freelancer olarak kullanıcının yaptığı başvuruları görüntüleme
        /// </summary>
        public async Task<IActionResult> Basvurularim()
        {
            // Oturum kontrolü
            var userJson = HttpContext.Session.GetString("Kullanici");
            if (string.IsNullOrEmpty(userJson))
                return RedirectToAction("Giris", "Kullanici");

            var user = JsonSerializer.Deserialize<AppUser>(userJson)!;

            // Firestore’dan kullanıcıya ait başvuruları al
            var benimBasvurularim = await FirebaseHelper.GetBasvurularByFreelancerEmailAsync(user.EmailAdres);

            return View(benimBasvurularim);
        }
    }
}
