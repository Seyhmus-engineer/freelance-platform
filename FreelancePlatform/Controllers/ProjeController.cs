using Microsoft.AspNetCore.Mvc;
using FreelancePlatform.Models;
using System.Text.Json;
using FreelancePlatform.Helpers;
using Google.Cloud.Firestore;
using Google.Cloud.Firestore.V1;
using Newtonsoft.Json.Linq;
using RestSharp;


namespace FreelancePlatform.Controllers
{
    public class ProjeController : Controller
    {
        // Herkesin erişeceği ortak proje listesi
        public static List<Proje> PublicProjeList { get; set; } = new();

        // Tüm projeleri listele
        public async Task<IActionResult> Listele()
        {
            var userJson = HttpContext.Session.GetString("Kullanici");
            if (string.IsNullOrEmpty(userJson)) return RedirectToAction("Giris", "Kullanici");

            var user = JsonSerializer.Deserialize<AppUser>(userJson)!;

            // Tüm projeleri Firestore'dan çek
            var projeler = await FirebaseHelper.GetAllProjectsAsync();

            // Eğer İşveren ise, sadece kendi projelerini listele
            if (user.Rol == "Isveren")
            {
                var isverenProjeleri = projeler
                    .Where(p => p.YayinlayanEmail == user.EmailAdres)
                    .OrderByDescending(p => p.OlusturmaTarihi)
                    .ToList();

                return View(isverenProjeleri);
            }

            // Eğer Freelancer veya başka roller ise, Tüm projeleri listele
            return View(projeler.OrderByDescending(p => p.OlusturmaTarihi).ToList());
        }



        // Proje ekleme sayfası (GET)
        // GET metodu: Formu gösterir
        public IActionResult Ekle()
        {
            var userJson = HttpContext.Session.GetString("Kullanici");
            if (userJson == null) return RedirectToAction("Giris", "Kullanici");

            var user = JsonSerializer.Deserialize<AppUser>(userJson);

            if (user.Rol != "Isveren" && user.Rol != "Yonetici")
                return Unauthorized();

            return View();
        }

        public async Task<IActionResult> ProjeOzet(int projeId)
        {
            FirebaseHelper.InitializeFirebase();

            var docRef = FirebaseHelper.GetFirestoreDb().Collection("projeler").Document(projeId.ToString());
            var snapshot = await docRef.GetSnapshotAsync();

            if (!snapshot.Exists)
                return NotFound();

            var proje = snapshot.ConvertTo<Proje>();

            var rates = await GetCurrencyRatesAsync(proje.ParaBirimi, proje.Butce);
            ViewBag.Kurlar = rates;

            // TL karşılığı ViewBag'e aktar
            if (rates.ContainsKey("TRY"))
            {
                ViewBag.TL = rates["TRY"].ToString("N2") + " TL";
            }
            else
            {
                ViewBag.TL = "Kur alınamadı";
            }

            return View(proje);
        }





        public async Task<IActionResult> YayindakiProjeler()
        {
            var projeler = await FirebaseHelper.GetAllProjectsAsync();

            var kurlar = await GetCurrencyRatesAsync("USD", 1); // TL dönüşüm için USD ve EUR oranlarını al
            ViewBag.Kurlar = kurlar;

            return View(projeler);
        }

        private async Task<Dictionary<string, double>> GetCurrencyRatesAsync(string baseCurrency, double tutar)
        {
            var rates = new Dictionary<string, double>();

            try
            {
                var client = new RestClient($"https://api.collectapi.com/economy/currencyToAll?int={tutar}&base={baseCurrency}");
                var request = new RestRequest();
                request.Method = Method.GET;
                request.AddHeader("authorization", "apikey 1vo3Vd92FkZzfaRnKQyVLA:4uadBPgfj319GfkcE2lByS");
                request.AddHeader("content-type", "application/json");

                var response = await client.ExecuteAsync(request);

                if (response.IsSuccessful && !string.IsNullOrWhiteSpace(response.Content))
                {
                    var obj = JObject.Parse(response.Content);
                    var dataArray = obj["result"]?["data"] as JArray;

                    if (dataArray != null)
                    {
                        foreach (var item in dataArray)
                        {
                            var code = item["code"]?.ToString();
                            var valueStr = item["calculated"]?.ToString()?.Replace(",", ".");

                            if (!string.IsNullOrEmpty(code) &&
                                double.TryParse(valueStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var value))
                            {
                                rates[code] = value;
                            }
                        }
                    }
                    else
                    {
                        rates["HATA"] = -1;
                    }
                }
                else
                {
                    rates["HATA"] = -1;
                }
            }
            catch
            {
                rates["HATA"] = -1;
            }

            return rates;
        }





        // Kullanıcının proje ekleme işlemleri
        [HttpPost]
        public async Task<IActionResult> Ekle(Proje yeniProje)
        {
            var userJson = HttpContext.Session.GetString("Kullanici");
            if (userJson == null) return RedirectToAction("Giris", "Kullanici");

            var user = JsonSerializer.Deserialize<AppUser>(userJson);

            if (user.Rol != "Isveren" && user.Rol != "Yonetici")
                return Unauthorized();

            if (ModelState.IsValid)
            {
                int maxProjeID = await FirebaseHelper.GetMaxProjeIDAsync();

                yeniProje.ProjeID = maxProjeID + 1;
                yeniProje.OlusturmaTarihi = DateTime.Now;
                yeniProje.YayinlayanEmail = user.EmailAdres;
                yeniProje.YayinlayanAdSoyad = user.AdSoyad;

                await FirebaseHelper.AddProjectAsync(yeniProje);

                return RedirectToAction("Listele");
            }

            return View(yeniProje);
        }






    }
}
