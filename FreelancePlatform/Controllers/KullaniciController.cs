using Microsoft.AspNetCore.Mvc;
using FreelancePlatform.Models;
using System.Text.Json;

namespace FreelancePlatform.Controllers
{
    public class KullaniciController : Controller
    {
        private static List<AppUser> kullanicilar = new();

        public IActionResult Giris()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Giris(string email, string sifre)
        {
            var user = kullanicilar.FirstOrDefault(k => k.EmailAdres == email && k.Sifre == sifre);
            if (user != null)
            {
                HttpContext.Session.SetString("Kullanici", JsonSerializer.Serialize(user));
                return RedirectToAction("Profil", new { id = user.KullaniciID });
            }

            ViewBag.Hata = "Giriş bilgileri hatalı!";
            return View();
        }

        public IActionResult Kayit()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Kayit(AppUser yeniKullanici)
        {
            if (ModelState.IsValid)
            {
                yeniKullanici.KullaniciID = kullanicilar.Count + 1;
                kullanicilar.Add(yeniKullanici);
                return RedirectToAction("Giris");
            }

            return View(yeniKullanici);
        }

        public IActionResult Profil(int id)
        {
            var kullanici = kullanicilar.FirstOrDefault(k => k.KullaniciID == id);
            return View(kullanici);
        }
    }
}
