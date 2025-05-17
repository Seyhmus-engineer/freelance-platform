using Microsoft.AspNetCore.Mvc;
using FreelancePlatform.Models;
using System.Text.Json;

namespace FreelancePlatform.Controllers
{
    public class YoneticiController : Controller
    {
        // Tüm kullanıcılar
        public static List<AppUser> TumKullanicilar { get; set; } = new();

        // Projeler ve başvurular
        private static List<Proje> projeler => ProjeController.PublicProjeList;
        private static List<Basvuru> basvurular => BasvuruController.GetBasvurular();

        // Kullanıcıları Listele
        public IActionResult Kullanicilar()
        {
            if (!YoneticiMi()) return Unauthorized();
            return View(TumKullanicilar);
        }

        // Kullanıcı Sil
        public IActionResult Sil(int id)
        {
            if (!YoneticiMi()) return Unauthorized();

            var kullanici = TumKullanicilar.FirstOrDefault(k => k.KullaniciID == id);
            if (kullanici != null)
            {
                TumKullanicilar.Remove(kullanici);
            }
            return RedirectToAction("Kullanicilar");
        }

        // Tüm Projeleri Listele
        public IActionResult Projeler()
        {
            if (!YoneticiMi()) return Unauthorized();
            return View(projeler);
        }

        // Tüm Başvuruları Listele
        public IActionResult Basvurular()
        {
            if (!YoneticiMi()) return Unauthorized();
            return View(basvurular);
        }

        // 🔐 Yardımcı metod – Yönetici kontrolü
        private bool YoneticiMi()
        {
            var userJson = HttpContext.Session.GetString("Kullanici");
            if (userJson == null) return false;

            var user = JsonSerializer.Deserialize<AppUser>(userJson);
            return user != null && user.Rol == "Yonetici";
        }
    }
}
