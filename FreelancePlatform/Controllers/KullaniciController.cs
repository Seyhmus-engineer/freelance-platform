using Microsoft.AspNetCore.Mvc;
using FreelancePlatform.Models;
using System.Text.Json;
using System.Net;
using System.Net.Mail;


namespace FreelancePlatform.Controllers
{
    public class KullaniciController : Controller
    {
        private static List<AppUser> kullanicilar = new();
        // Sıfırlama tokenlarını geçici olarak burada saklıyoruz (email → token)
        private static Dictionary<string, string> resetTokens = new();

        public IActionResult Giris()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Giris(string email, string sifre, string rolTipi)
        {
            var user = kullanicilar.FirstOrDefault(k => k.EmailAdres == email && k.Sifre == sifre);

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

                // Rol bazlı yönlendirme:
                if (user.Rol == "Yonetici")
                    return RedirectToAction("YoneticiPaneli");
                else if (user.Rol == "Isveren")
                    return RedirectToAction("IsverenPaneli");
                else
                    return RedirectToAction("FreelancerPaneli");
            }

            ViewBag.Hata = "Giriş bilgileri hatalı!";
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

        // Yardımcı fonksiyon:
        private AppUser GetSessionUser()
        {
            var userJson = HttpContext.Session.GetString("Kullanici");
            if (userJson == null) return null;
            return JsonSerializer.Deserialize<AppUser>(userJson);
        }


        public IActionResult Kayit()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Kayit(AppUser yeniKullanici)
        {
            // Aynı email ile kayıtlı kullanıcı var mı?
            if (kullanicilar.Any(k => k.EmailAdres == yeniKullanici.EmailAdres))
            {
                ViewBag.Hata = "Bu e-posta adresi ile daha önce kayıt olunmuş!";
                return View(yeniKullanici);
            }

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

        // Profil Düzenle (GET)
        public IActionResult ProfilDuzenle()
        {
            var userJson = HttpContext.Session.GetString("Kullanici");
            if (userJson == null) return RedirectToAction("Giris");

            var user = JsonSerializer.Deserialize<AppUser>(userJson);

            // Listeden kullanıcıyı bul ve gönder
            var mevcut = kullanicilar.FirstOrDefault(k => k.KullaniciID == user.KullaniciID);
            return View(mevcut);
        }

        // Profil Düzenle (POST)
        [HttpPost]
        public IActionResult ProfilDuzenle(AppUser guncellenen)
        {
            var userJson = HttpContext.Session.GetString("Kullanici");
            if (userJson == null) return RedirectToAction("Giris");

            var mevcut = kullanicilar.FirstOrDefault(k => k.KullaniciID == guncellenen.KullaniciID);
            if (mevcut != null)
            {
                mevcut.AdSoyad = guncellenen.AdSoyad;
                mevcut.Sifre = guncellenen.Sifre;
                // Email ve Rol güvenlik için burada değiştirilmiyor!
            }
            // Güncel bilgiyi tekrar Session’a yaz
            HttpContext.Session.SetString("Kullanici", JsonSerializer.Serialize(mevcut));
            ViewBag.Basarili = "Profiliniz başarıyla güncellendi!";
            return View(mevcut);
        }

        // GET: Şifremi Unuttum
        public IActionResult SifremiUnuttum()
        {
            return View();
        }

        // POST: Şifremi Unuttum
        [HttpPost]
        public IActionResult SifremiUnuttum(string email)
        {
            var user = kullanicilar.FirstOrDefault(k => k.EmailAdres == email);
            if (user == null)
            {
                ViewBag.Hata = "Bu e-posta ile kayıtlı kullanıcı bulunamadı!";
                return View();
            }

            // Token üret
            var token = Guid.NewGuid().ToString();
            resetTokens[email] = token;

            // Şifre sıfırlama linki (localhost portunu kendi projenin portuna göre güncelle!)
            var resetUrl = Url.Action("SifreSifirla", "Kullanici", new { email = email, token = token }, Request.Scheme);

            // Gmail SMTP ile mail gönder
            var fromAddress = new MailAddress("220260026@firat.edu.tr", "Freelance Platform");
            var toAddress = new MailAddress(email);
            const string fromPassword = "zlrl uplj bert gjnz"; // Gmail'den app password al!
            string subject = "Şifre Sıfırlama";
            string body = $"<p>Şifrenizi sıfırlamak için tıklayın: <a href='{resetUrl}'>Şifreyi Sıfırla</a></p>";

            var smtp = new SmtpClient
            {
                Host = "smtp.gmail.com",
                Port = 587,
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(fromAddress.Address, fromPassword)
            };

            using (var message = new MailMessage(fromAddress, toAddress)
            {
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            })
            {
                try
                {
                    smtp.Send(message);
                    ViewBag.Basarili = "Şifre sıfırlama bağlantısı e-posta adresinize gönderildi!";
                }
                catch (Exception ex)
                {
                    ViewBag.Hata = "E-posta gönderilemedi! " + ex.Message;
                }
            }

            return View();
        }

        // GET: Şifre sıfırlama ekranı
        public IActionResult SifreSifirla(string email, string token)
        {
            if (!resetTokens.ContainsKey(email) || resetTokens[email] != token)
            {
                return Content("Geçersiz veya süresi dolmuş şifre sıfırlama bağlantısı!");
            }
            ViewBag.Email = email;
            ViewBag.Token = token;
            return View();
        }

        // POST: Şifre sıfırlama işlemi
        [HttpPost]
        public IActionResult SifreSifirla(string email, string token, string yeniSifre)
        {
            if (!resetTokens.ContainsKey(email) || resetTokens[email] != token)
            {
                return Content("Geçersiz veya süresi dolmuş şifre sıfırlama bağlantısı!");
            }
            var user = kullanicilar.FirstOrDefault(k => k.EmailAdres == email);
            if (user == null)
            {
                return Content("Kullanıcı bulunamadı!");
            }
            user.Sifre = yeniSifre;
            resetTokens.Remove(email);
            ViewBag.Basarili = "Şifreniz başarıyla güncellendi!";
            return View();
        }


    }
}
