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

        // GET: Başvuru formu
        public IActionResult Basvur(int projeId)
        {
            var userJson = HttpContext.Session.GetString("Kullanici");
            if (userJson == null) return RedirectToAction("Giris", "Kullanici");

            var user = JsonSerializer.Deserialize<AppUser>(userJson);
            if (user.Rol != "Freelancer")
                return Unauthorized();

            var proje = ProjeController.PublicProjeList.FirstOrDefault(p => p.ProjeID == projeId);
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
            var proje = ProjeController.PublicProjeList.FirstOrDefault(p => p.ProjeID == projeId);
            if (proje == null)
                return NotFound();

            var basvuru = new Basvuru
            {
                BasvuruID = BasvuruController.basvurular.Count + 1,
                ProjeID = projeId,
                FreelancerEmail = user.EmailAdres,
                Mesaj = mesaj,
                TeklifTutari = teklifTutari,
                BasvuruTarihi = DateTime.Now
            };
            BasvuruController.basvurular.Add(basvuru);
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
