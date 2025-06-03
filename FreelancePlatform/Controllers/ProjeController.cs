using Microsoft.AspNetCore.Mvc;
using FreelancePlatform.Models;
using System.Text.Json;
using RestSharp;
using Newtonsoft.Json.Linq;

namespace FreelancePlatform.Controllers
{
    public class ProjeController : Controller
    {
        // Herkesin erişeceği ortak proje listesi
        public static List<Proje> PublicProjeList { get; set; } = new();

        // Tüm projeleri listele
        public async Task<IActionResult> Listele()
        {
            var projeler = PublicProjeList; 

            
            Dictionary<string, decimal> kurKarsiliklari = null;
            string anaParaBirimi = "USD";
            decimal anaTutar = 0;

            if (projeler.Any())
            {
                var ilkProje = projeler.First();
                anaParaBirimi = ilkProje.ParaBirimi;
                anaTutar = ilkProje.Butce;
                kurKarsiliklari = await GetCurrencyRatesAsync(anaParaBirimi, anaTutar);
            }

            ViewBag.KurKarsiliklari = kurKarsiliklari;
            ViewBag.KurAnaParaBirimi = anaParaBirimi;
            ViewBag.KurAnaTutar = anaTutar;

            return View(projeler);
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
        //Canlı Döviz kuru API kullanımı
        private async Task<Dictionary<string, decimal>> GetCurrencyRatesAsync(string baseCurrency, decimal tutar)
        {
            var client = new RestClient($"https://api.collectapi.com/economy/currencyToAll?int={tutar}&base={baseCurrency}");
            var request = new RestRequest();
            request.Method = Method.Get;
            request.AddHeader("authorization", "apikey 4Z7HNyNZ2tLWNJunM45AZ5:0nG4wM3sckOQN5rkYWEEu8");
            request.AddHeader("content-type", "application/json");

            var response = await client.ExecuteAsync(request);
            var rates = new Dictionary<string, decimal>();

            if (response.IsSuccessful && !string.IsNullOrWhiteSpace(response.Content))
            {
                var obj = JObject.Parse(response.Content);
                var results = obj["result"] as JArray;

                if (results != null)
                {
                    foreach (var item in results)
                    {
                        // item: JObject
                        var code = item["code"]?.ToString();
                        var valueStr = item["rateForAmount"]?.ToString()?.Replace(",", ".");
                        if (!string.IsNullOrEmpty(code) && !string.IsNullOrEmpty(valueStr))
                        {
                            if (decimal.TryParse(valueStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var value))
                            {
                                rates[code] = value;
                            }
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
            return rates;
        }

    }
}
