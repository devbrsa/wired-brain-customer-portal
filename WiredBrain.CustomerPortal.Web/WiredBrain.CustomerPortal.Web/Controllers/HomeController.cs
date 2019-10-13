﻿using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Http.Logging;
using Newtonsoft.Json;
using WiredBrain.CustomerPortal.Web.Models;
using WiredBrain.CustomerPortal.Web.Repositories;

namespace WiredBrain.CustomerPortal.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly ICustomerRepository repo;
        private readonly IConfiguration config;

        public HomeController(ICustomerRepository repo, IConfiguration config)
        {
            this.repo = repo;
            this.config = config;
        }

        [AllowAnonymous]
        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> Welcome()
        {
            ViewBag.Title = "Enter loyalty number";

            using (var httpClient = new HttpClient(new StandardHttpMessageHandler(HttpContext)))
            {
                var response = await httpClient.GetAsync("https://localhost:44354/weatherforecast");
                var items = JsonConvert.DeserializeObject<List<WeatherForecast>>(
                    await response.Content.ReadAsStringAsync());
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Welcome(int loyaltyNumber)
        {
            var customer = await repo.GetCustomerByLoyaltyNumber(loyaltyNumber);
            if (customer == null)
            {
                ModelState.AddModelError(string.Empty, "Unknown loyalty number");
                return View();
            }
            return RedirectToAction("LoyaltyOverview", new { loyaltyNumber });
        }

        public async Task<IActionResult> LoyaltyOverview(int loyaltyNumber)
        {
            ViewBag.Title = "Your points";

            var customer = await repo.GetCustomerByLoyaltyNumber(loyaltyNumber);
            var pointsNeeded = int.Parse(config["CustomerPortalSettings:PointsNeeded"]);

            var loyaltyModel = LoyaltyModel.FromCustomer(customer, pointsNeeded);
            return View(loyaltyModel);
        }

        public async Task<IActionResult> EditFavorite(int loyaltyNumber)
        {
            ViewBag.Title = "Edit favorite";

            var customer = await repo.GetCustomerByLoyaltyNumber(loyaltyNumber);
            return View(new EditFavoriteModel
            {
                LoyaltyNumber = customer.LoyaltyNumber,
                Favorite = customer.FavoriteDrink
            });
        }

        [HttpPost]
        public async Task<IActionResult> EditFavorite(EditFavoriteModel model)
        {
            await repo.SetFavorite(model);
            return RedirectToAction("LoyaltyOverview", new { loyaltyNumber = model.LoyaltyNumber });
        }
    }
}
