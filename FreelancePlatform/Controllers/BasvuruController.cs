using Microsoft.AspNetCore.Mvc;
using FreelancePlatform.Models;
using System.Text.Json;

namespace FreelancePlatform.Controllers
{
    public class BasvuruController : Controller
    {
        // Statik başvuru ve proje listeleri (prototip için)
        private static List<Basvuru> basvurular = new();
        private static List<Proje> projeler = ProjeController.PublicProjeList;

        public IActionResult Detay(int id)
        {
            var proje = projeler.FirstOrDefault(p => p.ProjeID == id);
            if (proje == null)
                return NotFound();
            return View(proje);
        }

        // GET: Başvuru formu
        public IActionResult Basvur(int projeId)
        {
            var userJson = HttpContext.Session.GetString("Kullanici");
            if (userJson == null) return RedirectToAction("Giris", "Kullanici");
            var user = JsonSerializer.Deserialize<AppUser>(userJson);
            if (user.Rol != "Freelancer")
                return Unauthorized();

            var proje = projeler.FirstOrDefault(p => p.ProjeID == projeId);
            if (proje == null) return NotFound();

            ViewBag.ProjeId = projeId;
            ViewBag.ParaBirimi = proje.ParaBirimi;
            ViewBag.ProjeBaslik = proje.Baslik;
            return View();
        }

        // POST: Başvuru gönderme
        [HttpPost]
        public IActionResult Basvur(int projeId, string mesaj, decimal teklifTutari)
        {
            var userJson = HttpContext.Session.GetString("Kullanici");
            if (userJson == null) return RedirectToAction("Giris", "Kullanici");
            var user = JsonSerializer.Deserialize<AppUser>(userJson);
            if (user.Rol != "Freelancer")
                return Unauthorized();

            // Proje ve işveren bilgisini al
            var proje = projeler.FirstOrDefault(p => p.ProjeID == projeId);
            if (proje == null)
                return NotFound();

            var basvuru = new Basvuru
            {
                BasvuruID = basvurular.Count + 1,
                ProjeID = projeId,
                ProjeBaslik = proje.Baslik,
                ProjeYayinlayanAdSoyad = proje.YayinlayanAdSoyad,
                FreelancerAdSoyad = user.AdSoyad,
                FreelancerEmail = user.EmailAdres,
                Mesaj = mesaj,
                TeklifTutari = teklifTutari,
                ParaBirimi = proje.ParaBirimi,
                BasvuruTarihi = DateTime.Now,
                BasvuruDurumu = "Beklemede" // Varsayılan: beklemede
            };
            basvurular.Add(basvuru);
            TempData["Basarili"] = "Başvurunuz başarıyla gönderildi!";
            return RedirectToAction("Listele", "Proje");
        }

        // İşverenin kendi projelerine gelen başvuruları görüntülemesi için
        public IActionResult GelenBasvurular()
        {
            var userJson = HttpContext.Session.GetString("Kullanici");
            if (userJson == null) return RedirectToAction("Giris", "Kullanici");
            var user = JsonSerializer.Deserialize<AppUser>(userJson);

            var kendiProjeler = projeler.Where(p => p.YayınlayanEmail == user.EmailAdres).Select(p => p.ProjeID).ToList();
            var gelen = basvurular.Where(b => kendiProjeler.Contains(b.ProjeID)).ToList();
            return View(gelen);
        }

        // Freelancer'ın yaptığı başvurular
        public IActionResult Basvurularim()
        {
            var userJson = HttpContext.Session.GetString("Kullanici");
            if (userJson == null) return RedirectToAction("Giris", "Kullanici");
            var user = JsonSerializer.Deserialize<AppUser>(userJson);

            var benim = basvurular.Where(b => b.FreelancerEmail == user.EmailAdres).ToList();
            return View(benim);
        }

        // KABUL: İşveren başvuruyu kabul ettiğinde
        [HttpPost]
        public IActionResult BasvuruKabul(int basvuruId)
        {
            var userJson = HttpContext.Session.GetString("Kullanici");
            if (userJson == null) return RedirectToAction("Giris", "Kullanici");
            var isveren = JsonSerializer.Deserialize<AppUser>(userJson);

            var basvuru = basvurular.FirstOrDefault(b => b.BasvuruID == basvuruId);
            if (basvuru != null)
            {
                basvuru.BasvuruDurumu = "Kabul Edildi";
                // Freelancer'a sistem mesajı gönder (kim tarafından belli)
                MesajController.mesajlar.Add(new Mesaj
                {
                    ProjeID = basvuru.ProjeID,
                    GonderenEmail = isveren.EmailAdres,
                    GonderenAdSoyad = isveren.AdSoyad + " (İşveren)",
                    AliciEmail = basvuru.FreelancerEmail,
                    AliciAdSoyad = basvuru.FreelancerAdSoyad,
                    MesajIcerik = $"Tebrikler! Başvurunuz {isveren.AdSoyad} tarafından KABUL EDİLDİ.",
                    GonderimTarihi = DateTime.Now,
                    OkunduMu = false
                });
            }
            return RedirectToAction("GelenBasvurular");
        }

        // RED: İşveren başvuruyu reddettiğinde
        [HttpPost]
        public IActionResult BasvuruRed(int basvuruId)
        {
            var userJson = HttpContext.Session.GetString("Kullanici");
            if (userJson == null) return RedirectToAction("Giris", "Kullanici");
            var isveren = JsonSerializer.Deserialize<AppUser>(userJson);

            var basvuru = basvurular.FirstOrDefault(b => b.BasvuruID == basvuruId);
            if (basvuru != null)
            {
                basvuru.BasvuruDurumu = "Reddedildi";
                MesajController.mesajlar.Add(new Mesaj
                {
                    ProjeID = basvuru.ProjeID,
                    GonderenEmail = isveren.EmailAdres,
                    GonderenAdSoyad = isveren.AdSoyad + " (İşveren)",
                    AliciEmail = basvuru.FreelancerEmail,
                    AliciAdSoyad = basvuru.FreelancerAdSoyad,
                    MesajIcerik = $"Üzgünüz! Başvurunuz {isveren.AdSoyad} tarafından REDDEDİLDİ.",
                    GonderimTarihi = DateTime.Now,
                    OkunduMu = false
                });
            }
            return RedirectToAction("GelenBasvurular");
        }

        public static List<Basvuru> GetBasvurular()
        {
            return basvurular;
        }
    }
}
