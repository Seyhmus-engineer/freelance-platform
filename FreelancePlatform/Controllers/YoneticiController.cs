using Microsoft.AspNetCore.Mvc;
using FreelancePlatform.Models;
using FreelancePlatform.Helpers;
using System.Text.Json;

namespace FreelancePlatform.Controllers
{
    public class YoneticiController : Controller
    {
        // Tüm kullanıcılar
        public static List<AppUser> TumKullanicilar { get; set; } = new();

        // Projeler (static liste)
        private static List<Proje> projeler => ProjeController.PublicProjeList;

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
        public async Task<IActionResult> Basvurular()
        {
            if (!YoneticiMi()) return Unauthorized();

            // Firestore’dan tüm başvuruları çek
            var basvurular = await FirebaseHelper.GetAllBasvurularAsync();

            return View(basvurular.OrderByDescending(b => b.BasvuruTarihi).ToList());
        }

        // 🔐 Yardımcı metod – Yönetici kontrolü
        private bool YoneticiMi()
        {
            var userJson = HttpContext.Session.GetString("Kullanici");
            if (string.IsNullOrEmpty(userJson)) return false;

            var user = JsonSerializer.Deserialize<AppUser>(userJson)!;
            return user.Rol == "Yonetici";
        }
    }
}
