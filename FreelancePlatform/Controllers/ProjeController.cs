using Microsoft.AspNetCore.Mvc;
using FreelancePlatform.Models;

namespace FreelancePlatform.Controllers
{
    public class ProjeController : Controller
    {
        // 🔧 Bu liste artık public, herkes erişebilir
        public static List<Proje> PublicProjeList { get; set; } = new();

        public IActionResult Listele()
        {
            return View(PublicProjeList.OrderByDescending(p => p.OlusturmaTarihi).ToList());
        }

        public IActionResult Ekle()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Ekle(Proje yeniProje)
        {
            if (ModelState.IsValid)
            {
                yeniProje.ProjeID = PublicProjeList.Count + 1;
                yeniProje.OlusturmaTarihi = DateTime.Now;

                var userJson = HttpContext.Session.GetString("Kullanici");
                if (userJson != null)
                {
                    var kullanici = System.Text.Json.JsonSerializer.Deserialize<AppUser>(userJson);
                    yeniProje.YayınlayanEmail = kullanici?.EmailAdres ?? "bilinmiyor@example.com";
                }

                PublicProjeList.Add(yeniProje);
                return RedirectToAction("Listele");
            }

            return View(yeniProje);
        }
    }
}
