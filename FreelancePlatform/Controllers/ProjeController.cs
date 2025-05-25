using Microsoft.AspNetCore.Mvc;
using FreelancePlatform.Models;
using System.Text.Json;

namespace FreelancePlatform.Controllers
{
    public class ProjeController : Controller
    {
        // Herkesin erişeceği ortak proje listesi
        public static List<Proje> PublicProjeList { get; set; } = new();

        // Tüm projeleri listele
        public IActionResult Listele()
        {
            return View(PublicProjeList.OrderByDescending(p => p.OlusturmaTarihi).ToList());
        }

        // Proje ekleme sayfası (GET)
        public IActionResult Ekle()
        {
            var userJson = HttpContext.Session.GetString("Kullanici");
            if (userJson == null) return RedirectToAction("Giris", "Kullanici");

            var user = JsonSerializer.Deserialize<AppUser>(userJson);

            // Sadece İşveren veya Yönetici proje ekleyebilsin
            if (user.Rol != "Isveren" && user.Rol != "Yonetici")
                return Unauthorized();

            return View();
        }

        // Proje ekleme işlemi (POST)
        [HttpPost]
        public IActionResult Ekle(Proje yeniProje)
        {
            var userJson = HttpContext.Session.GetString("Kullanici");
            if (userJson == null) return RedirectToAction("Giris", "Kullanici");

            var user = JsonSerializer.Deserialize<AppUser>(userJson);

            if (user.Rol != "Isveren" && user.Rol != "Yonetici")
                return Unauthorized();

            if (ModelState.IsValid)
            {
                yeniProje.ProjeID = PublicProjeList.Count + 1;
                yeniProje.OlusturmaTarihi = DateTime.Now;
                yeniProje.YayınlayanEmail = user.EmailAdres;
                yeniProje.YayinlayanAdSoyad = user.AdSoyad;
                PublicProjeList.Add(yeniProje);
                return RedirectToAction("Listele");
            }
            return View(yeniProje);
        }
    }
}
