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
        public IActionResult Giris(string email, string sifre, string rolTipi)
        {
            var user = kullanicilar.FirstOrDefault(k => k.EmailAdres == email && k.Sifre == sifre); // 🔥 Doğru satır

            if (user != null)
            {
                // Rol kontrolü
                if ((rolTipi == "Yonetici" && user.Rol != "Yonetici") ||
                    (rolTipi == "Normal" && user.Rol == "Yonetici"))
                {
                    ViewBag.Hata = "Bu alana uygun kullanıcı bulunamadı.";
                    return View();
                }

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

        public IActionResult Cikis()
        {
            HttpContext.Session.Clear(); // Tüm oturum verilerini siler
            return RedirectToAction("Giris");
        }

    }
}
