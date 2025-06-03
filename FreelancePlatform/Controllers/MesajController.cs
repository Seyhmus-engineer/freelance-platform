using Microsoft.AspNetCore.Mvc;
using FreelancePlatform.Models;
using System.Text.Json;

namespace FreelancePlatform.Controllers
{
    public class MesajController : Controller
    {
        // Statik, test amaçlı. Gerçekte DB'den çekilecektir.
        public static List<Mesaj> mesajlar = new List<Mesaj>();

        // Mesaj kutusu (ikili görüşme bazında)
        public IActionResult Mesajlarim(int projeId, string karsiTarafEmail)
        {
            var userJson = HttpContext.Session.GetString("Kullanici");
            if (userJson == null) return RedirectToAction("Giris", "Kullanici");
            var user = JsonSerializer.Deserialize<AppUser>(userJson);

            // Proje başlığı alınmak istenirse:
            string projeBaslik = ""; // İstersen projeler listesinden bulabilirsin

            // İlgili konuşmadaki tüm mesajları getir (gelen ve giden)
            var sohbetMesajlari = mesajlar
                .Where(m =>
                    m.ProjeID == projeId &&
                    ((m.GonderenEmail == user.EmailAdres && m.AliciEmail == karsiTarafEmail)
                     || (m.GonderenEmail == karsiTarafEmail && m.AliciEmail == user.EmailAdres)))
                .OrderBy(m => m.GonderimTarihi)
                .ToList();

            var viewModel = new MesajDetayViewModel
            {
                ProjeID = projeId,
                ProjeBaslik = projeBaslik,
                GirisYapanEmail = user.EmailAdres,
                GirisYapanAdSoyad = user.AdSoyad,
                KarsiTarafEmail = karsiTarafEmail,
                KarsiTarafAdSoyad = sohbetMesajlari.FirstOrDefault(m => m.GonderenEmail == karsiTarafEmail)?.GonderenAdSoyad ?? "",
                Mesajlar = sohbetMesajlari
            };

            return View(viewModel);
        }

        // Mesaj gönderme
        [HttpPost]
        public IActionResult MesajGonder(int projeId, string aliciEmail, string mesajIcerik)
        {
            var userJson = HttpContext.Session.GetString("Kullanici");
            if (userJson == null) return RedirectToAction("Giris", "Kullanici");
            var user = JsonSerializer.Deserialize<AppUser>(userJson);

            // Alıcı ad-soyadı opsiyonel, istersen DB'den çekebilirsin.
            var mesaj = new Mesaj
            {
                MesajID = mesajlar.Count + 1,
                ProjeID = projeId,
                GonderenEmail = user.EmailAdres,
                AliciEmail = aliciEmail,
                GonderenAdSoyad = user.AdSoyad,
                AliciAdSoyad = "", // Gerekirse doldurursun
                MesajIcerik = mesajIcerik,
                GonderimTarihi = DateTime.Now,
                Okundu = false
            };
            mesajlar.Add(mesaj);

            TempData["Basarili"] = "Mesajınız gönderildi!";
            return RedirectToAction("Mesajlarim", new { projeId = projeId, karsiTarafEmail = aliciEmail });
        }

        public IActionResult GelenKutusu()
        {
            var userJson = HttpContext.Session.GetString("Kullanici");
            if (userJson == null) return RedirectToAction("Giris", "Kullanici");
            var user = JsonSerializer.Deserialize<AppUser>(userJson);

            // Kullanıcıya ait tüm mesajlar (gelen veya giden)
            var tumMesajlar = mesajlar
                .Where(m => m.GonderenEmail == user.EmailAdres || m.AliciEmail == user.EmailAdres)
                .ToList();

            // Sohbetleri, proje ve karşı taraf bazında grupla
            var sohbetler = tumMesajlar
                .GroupBy(m =>
                    new
                    {
                        m.ProjeID,
                        KarsiTaraf = m.GonderenEmail == user.EmailAdres ? m.AliciEmail : m.GonderenEmail
                    })
                .Select(g => g.OrderByDescending(x => x.GonderimTarihi).First())
                .OrderByDescending(x => x.GonderimTarihi)
                .ToList();

            return View(sohbetler); // GelenKutusu.cshtml ile eşleşecek!
        }


    }
}
