using Microsoft.AspNetCore.Mvc;
using FreelancePlatform.Models;
using System.Text.Json;
using FreelancePlatform.Helpers;

namespace FreelancePlatform.Controllers
{
    public class MesajController : Controller
    {

        //Kullanıcının ilgili kullanıcı ile mesajlasma işlemleri
        [HttpGet]
        public async Task<IActionResult> Mesajlasma(int projeId, string digerEmail)
        {
            var userJson = HttpContext.Session.GetString("Kullanici");
            if (userJson == null) return RedirectToAction("Giris", "Kullanici");

            var user = JsonSerializer.Deserialize<AppUser>(userJson)!;

            // Firebase'den mesajları al
            var tumMesajlar = await FirebaseHelper.GetMesajlarAsync(user.EmailAdres);

            // Başvurudan ilk mesaj varsa ekle
            var ilkBasvuruMesaji = await FirebaseHelper.GetIlkBasvuruMesajiAsync(projeId, digerEmail);
            if (ilkBasvuruMesaji != null)
                tumMesajlar.Add(ilkBasvuruMesaji);

            var gecmis = tumMesajlar
                .Where(m =>
                    m.ProjeID == projeId &&
                    ((m.GonderenEmail == user.EmailAdres && m.AliciEmail == digerEmail) ||
                     (m.GonderenEmail == digerEmail && m.AliciEmail == user.EmailAdres)))
                .OrderBy(m => m.GonderimTarihi)
                .ToList();

            // Proje başlığını al
            var proje = await FirebaseHelper.GetProjectByIdAsync(projeId);

            // ViewModel hazırla
            var model = new MesajDetayViewModel
            {
                ProjeID = projeId,
                ProjeBaslik = proje?.Baslik ?? "Bilinmeyen Proje",
                GirisYapanEmail = user.EmailAdres,
                KarsiTarafEmail = digerEmail,
                Mesajlar = gecmis
            };

            return View(model); // MesajDetayViewModel gönderiyoruz
        }

        //Mesaj gönderme işlemi
        [HttpPost]
        public async Task<IActionResult> MesajGonder(int projeId, string aliciEmail, string mesajIcerik)
        {
            var userJson = HttpContext.Session.GetString("Kullanici");
            if (userJson == null) return RedirectToAction("Giris", "Kullanici");

            var user = JsonSerializer.Deserialize<AppUser>(userJson)!;

            // 🔴 Eğer mesajIcerik boşsa engelle
            if (string.IsNullOrWhiteSpace(mesajIcerik))
            {
                TempData["Hata"] = "Mesaj içeriği boş olamaz!";
                return RedirectToAction("Mesajlasma", new { projeId = projeId, digerEmail = aliciEmail });
            }

            var mesaj = new Mesaj
            {
                ProjeID = projeId,
                GonderenEmail = user.EmailAdres,
                AliciEmail = aliciEmail,
                MesajIcerik = mesajIcerik,
                GonderimTarihi = DateTime.UtcNow
            };

            await FirebaseHelper.AddMesajAsync(mesaj);

            TempData["Basarili"] = "Mesajınız gönderildi!";
            return RedirectToAction("Mesajlasma", new { projeId = projeId, digerEmail = aliciEmail });
        }


        //Kullanıcıya ait mesajları getirme işlemleri
        public async Task<IActionResult> Mesajlarim()
        {
            var userJson = HttpContext.Session.GetString("Kullanici");
            if (string.IsNullOrEmpty(userJson)) return RedirectToAction("Giris", "Kullanici");

            var user = JsonSerializer.Deserialize<AppUser>(userJson)!;

            // Firestore’dan çek
            var mesajlarim = await FirebaseHelper.GetMesajlarAsync(user.EmailAdres);

            return View(mesajlarim);
        }

    }
}
