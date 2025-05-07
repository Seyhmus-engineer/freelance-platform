using Microsoft.AspNetCore.Mvc;
using FreelancePlatform.Models;
using System.Text.Json;

namespace FreelancePlatform.Controllers
{
    public class MesajController : Controller
    {
        // Hafızada tüm mesajları saklayan liste
        private static List<Mesaj> mesajlar = new();

        [HttpGet]
        public IActionResult Mesajlasma(int projeId, string digerEmail)
        {
            var userJson = HttpContext.Session.GetString("Kullanici");
            if (userJson == null) return RedirectToAction("Giris", "Kullanici");

            var user = JsonSerializer.Deserialize<AppUser>(userJson);

            // Geçmiş mesajları filtrele
            var gecmis = mesajlar
                .Where(m =>
                    m.ProjeID == projeId &&
                    ((m.GonderenEmail == user.EmailAdres && m.AliciEmail == digerEmail) ||
                     (m.GonderenEmail == digerEmail && m.AliciEmail == user.EmailAdres)))
                .OrderBy(m => m.Tarih)
                .ToList();

            ViewBag.ProjeID = projeId;
            ViewBag.KarsiTaraf = digerEmail;

            return View(gecmis);
        }

        [HttpPost]
        public IActionResult MesajGonder(int projeId, string aliciEmail, string icerik)
        {
            var userJson = HttpContext.Session.GetString("Kullanici");
            if (userJson == null) return RedirectToAction("Giris", "Kullanici");

            var user = JsonSerializer.Deserialize<AppUser>(userJson);

            var yeni = new Mesaj
            {
                MesajID = mesajlar.Count + 1,
                ProjeID = projeId,
                GonderenEmail = user.EmailAdres,
                AliciEmail = aliciEmail,
                Icerik = icerik,
                Tarih = DateTime.Now
            };

            mesajlar.Add(yeni);

            return RedirectToAction("Mesajlasma", new { projeId = projeId, digerEmail = aliciEmail });
        }

        public IActionResult Mesajlarim()
        {
            var userJson = HttpContext.Session.GetString("Kullanici");
            if (userJson == null) return RedirectToAction("Giris", "Kullanici");

            var user = JsonSerializer.Deserialize<AppUser>(userJson);

            var ilgiliMesajlar = mesajlar
                .Where(m => m.GonderenEmail == user.EmailAdres || m.AliciEmail == user.EmailAdres)
                .OrderByDescending(m => m.Tarih)
                .ToList();

            return View(ilgiliMesajlar);
        }

    }
}
