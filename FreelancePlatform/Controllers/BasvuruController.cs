using Microsoft.AspNetCore.Mvc;
using FreelancePlatform.Models;
using System.Text.Json;

namespace FreelancePlatform.Controllers
{
    public class BasvuruController : Controller
    {
        private static List<Basvuru> basvurular = new();
        private static List<Proje> projeler = ProjeController.PublicProjeList;

        public IActionResult Detay(int id)
        {
            var proje = projeler.FirstOrDefault(p => p.ProjeID == id);
            if (proje == null)
                return NotFound();

            return View(proje);
        }

        [HttpPost]
        public IActionResult Basvur(int projeId, string mesaj, decimal teklifTutari)
        {
            var userJson = HttpContext.Session.GetString("Kullanici");
            if (userJson == null) return RedirectToAction("Giris", "Kullanici");

            var user = JsonSerializer.Deserialize<AppUser>(userJson);
            var proje = projeler.FirstOrDefault(p => p.ProjeID == projeId);

            if (user == null || proje == null)
                return NotFound();

            // Daha önce başvurmuş mu kontrolü
            bool zatenBasvurdu = basvurular.Any(b => b.ProjeID == projeId && b.FreelancerEmail == user.EmailAdres);
            if (zatenBasvurdu)
            {
                TempData["Uyari"] = "Bu projeye daha önce başvurdunuz.";
                return RedirectToAction("Detay", new { id = projeId });
            }

            var yeni = new Basvuru
            {
                BasvuruID = basvurular.Count + 1,
                ProjeID = projeId,
                FreelancerEmail = user.EmailAdres,
                Mesaj = mesaj,
                TeklifTutari = teklifTutari,
                BasvuruTarihi = DateTime.Now
                

            };

            basvurular.Add(yeni);
            TempData["Basarili"] = "Başvurunuz başarıyla iletildi.";
            return RedirectToAction("Detay", new { id = projeId });
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
        public IActionResult Basvurularim()
        {
            var userJson = HttpContext.Session.GetString("Kullanici");
            if (userJson == null) return RedirectToAction("Giris", "Kullanici");

            var user = JsonSerializer.Deserialize<AppUser>(userJson);

            var benim = basvurular.Where(b => b.FreelancerEmail == user.EmailAdres).ToList();
            return View(benim);
        }

        public static List<Basvuru> GetBasvurular()
        {
            return basvurular;
        }


    }
}
