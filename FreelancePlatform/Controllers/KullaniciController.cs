using Microsoft.AspNetCore.Mvc;
using FreelancePlatform.Models;
using System.Text.Json;
using System.Net;
using System.Net.Mail;
using FreelancePlatform.Helpers;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using Google.Cloud.Firestore;

namespace FreelancePlatform.Controllers
{
    public class KullaniciController : Controller
    {
        private static List<AppUser> kullanicilar = new();
        private static Dictionary<string, string> resetTokens = new();

        public IActionResult Giris()
        {
            return View();
        }


        //Firebase Authentication giriş ve gerekli bilgileri kaydetme işlemleri
        [HttpPost]
        public async Task<IActionResult> Giris(string email, string sifre, string rolTipi)
        {
            // 🔒 Firebase Authentication ile doğrulama
            bool authBasarili = await FirebaseHelper.SignInWithEmailAndPasswordAsync(email, sifre);
            if (!authBasarili)
            {
                ViewBag.Hata = "Giriş bilgileri hatalı! (Firebase Auth)";
                return View();
            }

            // 🔍 Firestore'dan kullanıcı bilgileri alınır
            var user = await FirebaseHelper.GetUserByEmailAsync(email);

            if (user != null)
            {
                // 🚫 Rol uyumsuzluğu kontrolü
                if ((rolTipi == "Yonetici" && user.Rol != "Yonetici") ||
                    (rolTipi == "Normal" && user.Rol == "Yonetici"))
                {
                    ViewBag.Hata = "Bu alana uygun kullanıcı bulunamadı.";
                    return View();
                }

                // ✅ Kullanıcı bilgilerini Session'a kaydet
                HttpContext.Session.SetString("Kullanici", JsonSerializer.Serialize(user));

                // 🔥 Base64 profil resmi ayrıca Session'a ayrı olarak kaydedilir
                HttpContext.Session.SetString("ProfilResmiBase64", user.ProfilResmiBase64 ?? "");
                HttpContext.Session.SetString("AdSoyad", user.AdSoyad ?? "");
                HttpContext.Session.SetString("EmailAdres", user.EmailAdres ?? "");

                // 🔁 Rol bazlı yönlendirme
                return user.Rol switch
                {
                    "Yonetici" => RedirectToAction("YoneticiPaneli"),
                    "Isveren" => RedirectToAction("IsverenPaneli"),
                    _ => RedirectToAction("FreelancerPaneli")
                };
            }

            // ❌ Kullanıcı veritabanında bulunamadı
            ViewBag.Hata = "Kullanıcı Firestore'da bulunamadı!";
            return View();
        }


        public IActionResult YoneticiPaneli()
        {
            var user = GetSessionUser();
            if (user == null || user.Rol != "Yonetici") return Unauthorized();
            return View(user);
        }

        public IActionResult IsverenPaneli()
        {
            var user = GetSessionUser();
            if (user == null || user.Rol != "Isveren") return Unauthorized();
            return View(user);
        }

        public IActionResult FreelancerPaneli()
        {
            var user = GetSessionUser();
            if (user == null || user.Rol != "Freelancer") return Unauthorized();
            return View(user);
        }

        private AppUser? GetSessionUser()
        {
            var userJson = HttpContext.Session.GetString("Kullanici");
            return userJson == null ? null : JsonSerializer.Deserialize<AppUser>(userJson);
        }

        public IActionResult Kayit()
        {
            return View();
        }


        //Kullanıcıyı firebase databaseye kayıt işlemleri
        [HttpPost]
        public async Task<IActionResult> Kayit(AppUser yeniKullanici)
        {
            if (ModelState.IsValid)
            {
                var mevcutKullanici = await FirebaseHelper.GetUserByEmailAsync(yeniKullanici.EmailAdres);
                if (mevcutKullanici != null)
                {
                    ViewBag.Hata = "Bu e-posta adresi zaten kayıtlı!";
                    return View(yeniKullanici);
                }

                try
                {
                    // Kullanıcı oluştur
                    string firebaseUid = await FirebaseHelper.CreateFirebaseUserAsync(yeniKullanici.EmailAdres, yeniKullanici.Sifre);

                    int maxID = await FirebaseHelper.GetMaxKullaniciIDAsync();
                    yeniKullanici.KullaniciID = maxID + 1;

                    var veri = new Dictionary<string, object>
            {
                {"KullaniciID", yeniKullanici.KullaniciID},
                {"AdSoyad", yeniKullanici.AdSoyad},
                {"EmailAdres", yeniKullanici.EmailAdres},
                {"Sifre", yeniKullanici.Sifre},
                {"Rol", yeniKullanici.Rol},
                {"FirebaseUID", firebaseUid},
                {"ProfilResmiBase64","" }
            };

                    await FirebaseHelper.AddDataAsync("kullanicilar", yeniKullanici.EmailAdres, veri);

                    return RedirectToAction("Giris");
                }
                catch (Exception ex)
                {
                    // Hata detayını ViewBag'a yaz
                    ViewBag.Hata = "Kayıt sırasında hata oluştu: " + ex.ToString();
                    return View(yeniKullanici);
                }
            }

            return View(yeniKullanici);
        }


        // Helper method - controller içinde veya ayrı bir sınıfta
        private string ConvertImageToBase64(string relativePath)
        {
            var webRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var fullPath = Path.Combine(webRootPath, relativePath);

            if (!System.IO.File.Exists(fullPath))
                return string.Empty;

            byte[] imageBytes = System.IO.File.ReadAllBytes(fullPath);
            return Convert.ToBase64String(imageBytes);
        }





        public IActionResult Cikis()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Giris");
        }




        public IActionResult Profil()
        {
            var userJson = HttpContext.Session.GetString("Kullanici");
            if (string.IsNullOrEmpty(userJson)) return RedirectToAction("Giris", "Kullanici");
            var user = JsonSerializer.Deserialize<AppUser>(userJson);
            if (user == null) return RedirectToAction("Giris", "Kullanici");
            return View(user);
        }

        public IActionResult ProfilDuzenle()
        {
            var userJson = HttpContext.Session.GetString("Kullanici");
            if (string.IsNullOrEmpty(userJson)) return RedirectToAction("Giris", "Kullanici");
            var user = JsonSerializer.Deserialize<AppUser>(userJson);
            if (user == null) return RedirectToAction("Giris", "Kullanici");
            return View(user);
        }

        //Kullanının profili düzenleme ve kaydetme işlemleri
        [HttpPost]
        public async Task<IActionResult> ProfilDuzenle(AppUser guncellenen, string ProfilResmiBase64)
        {
            var userJson = HttpContext.Session.GetString("Kullanici");
            if (string.IsNullOrEmpty(userJson)) return RedirectToAction("Giris");

            var mevcut = JsonSerializer.Deserialize<AppUser>(userJson);
            if (mevcut == null) return RedirectToAction("Giris");

            // Şifre değiştiyse Firebase Auth şifresini de güncelle
            if (guncellenen.Sifre != mevcut.Sifre)
            {
                bool authUpdate = await FirebaseHelper.UpdateUserPasswordAsync(mevcut.EmailAdres, guncellenen.Sifre);
                if (!authUpdate)
                {
                    ViewBag.Hata = "Firebase Auth şifre güncellemesi başarısız oldu. Şifrenizin minimum 6 karakter olduğundan emin olun.";
                    return View(mevcut);
                }
            }

            mevcut.AdSoyad = guncellenen.AdSoyad;
            mevcut.Sifre = guncellenen.Sifre;

            // Gelen base64 profil fotoğrafını da kaydet
            if (!string.IsNullOrEmpty(ProfilResmiBase64))
            {
                mevcut.ProfilResmiBase64 = ProfilResmiBase64;
            }

            var veri = new Dictionary<string, object>
    {
        {"AdSoyad", mevcut.AdSoyad},
        {"Sifre", mevcut.Sifre},
        {"ProfilResmiBase64", mevcut.ProfilResmiBase64 ?? ""} // null kontrolü
    };

            try
            {
                await FirebaseHelper.UpdateUserDataAsync("kullanicilar", mevcut.EmailAdres, veri);
                HttpContext.Session.SetString("Kullanici", JsonSerializer.Serialize(mevcut));
                HttpContext.Session.SetString("ProfilResmiBase64", mevcut.ProfilResmiBase64 ?? ""); // 🔥 EKLENDİ
                HttpContext.Session.SetString("AdSoyad", mevcut.AdSoyad ?? ""); // 🔥 EKLENDİ
                ViewBag.Basarili = "Profiliniz başarıyla güncellendi!";
            }

            catch (Exception ex)
            {
                ViewBag.Hata = "Profil güncellenirken hata oluştu: " + ex.Message;
            }

            return View(mevcut);
        }




        public IActionResult SifremiUnuttum()
        {
            return View();
        }


        //Şifremi unuttum ve mail gönderme işlemleri
        [HttpPost]
        public async Task<IActionResult> SifremiUnuttum(string email)
        {
            // 🔎 Firebase’den kullanıcıyı kontrol et (Firestore’da)
            var user = await FirebaseHelper.GetUserByEmailAsync(email);
            if (user == null)
            {
                ViewBag.Hata = "Bu e-posta ile kayıtlı kullanıcı bulunamadı!";
                return View();
            }

            // 🔥 Firebase Auth ile şifre sıfırlama e-postasını gönder
            bool basarili = await FirebaseHelper.SendPasswordResetEmailAsync(email);

            if (basarili)
                ViewBag.Basarili = "Şifre sıfırlama bağlantısı e-posta adresinize gönderildi!";
            else
                ViewBag.Hata = "Firebase Auth'ta şifre sıfırlama bağlantısı gönderilemedi!";

            return View();
        }



        [HttpPost]
        public async Task<IActionResult> SifreSifirla(string email, string token, string yeniSifre)
        {
            if (!resetTokens.ContainsKey(email) || resetTokens[email] != token)
            {
                return Content("Geçersiz veya süresi dolmuş şifre sıfırlama bağlantısı!");
            }

            // Şifreyi Firebase'de güncelle
            bool basarili = await FirebaseHelper.UpdateUserPasswordByEmailAsync(email, yeniSifre);

            if (basarili)
            {
                resetTokens.Remove(email);
                ViewBag.Basarili = "Şifreniz başarıyla güncellendi!";
            }
            else
            {
                ViewBag.Hata = "Kullanıcı bulunamadı veya güncelleme hatası!";
            }

            return View();
        }
    }
}
