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
                .OrderBy(m => m.GonderimTarihi)
                .ToList();

            ViewBag.ProjeID = projeId;
            ViewBag.KarsiTaraf = digerEmail;

            return View(gecmis);
        }

        [HttpPost]
        public IActionResult MesajGonder(int projeId, string aliciEmail, string mesajIcerik)
        {
            var userJson = HttpContext.Session.GetString("Kullanici");
            if (userJson == null) return RedirectToAction("Giris", "Kullanici");

            var user = JsonSerializer.Deserialize<AppUser>(userJson);

            var mesaj = new Mesaj
            {
                GonderenEmail = user.EmailAdres,
                AliciEmail = aliciEmail,
                ProjeID = projeId,
                MesajIcerik = mesajIcerik,
                GonderimTarihi = DateTime.Now
            };
            MesajController.mesajlar.Add(mesaj);
            TempData["Basarili"] = "Mesajınız gönderildi!";
            return RedirectToAction("MesajDetay", new { projeId = projeId, karsiTarafEmail = aliciEmail });
        }


        public IActionResult Mesajlarim()
        {
            var userJson = HttpContext.Session.GetString("Kullanici");
            if (userJson == null) return RedirectToAction("Giris", "Kullanici");

            var user = JsonSerializer.Deserialize<AppUser>(userJson);

            var ilgiliMesajlar = mesajlar
                .Where(m => m.GonderenEmail == user.EmailAdres || m.AliciEmail == user.EmailAdres)
                .OrderByDescending(m => m.GonderimTarihi)
                .ToList();

            return View(ilgiliMesajlar);
        }

    }
}
