using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Web;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using WebFrontTestB2C.Models;

namespace WebFrontTestB2C.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ITokenAcquisition _tokenAcquisition;

        private IConfiguration Configuration { get; }

        public HomeController(ILogger<HomeController> logger, IConfiguration configuration, ITokenAcquisition tokenAcquisition)
        {
            _logger = logger;
            _tokenAcquisition = tokenAcquisition;

            Configuration = configuration;
        }

        public IActionResult Index()
        {
            var user = HttpContext.User;
            ViewData["User"] = user;

            //CALL API
            var httpClient = PrepareAuthenticatedClient().GetAwaiter().GetResult();
            var resultApi = GetTest(httpClient).GetAwaiter().GetResult();
            ViewData.Add("ResultApi", resultApi);

            //CALL API GRAPH
            var resultApiGraph = GetTestGraph(httpClient).GetAwaiter().GetResult();
            ViewData.Add("ResultApiGraph", resultApiGraph);

            return View();
        }

        private async Task<HttpClient> PrepareAuthenticatedClient()
        {
            try
            {
                var accessToken = await _tokenAcquisition.GetAccessTokenForUserAsync(new[] { Configuration["TestApi:Scope"] });
                Debug.WriteLine($"access token-{accessToken}");
                HttpClient _httpClient = new HttpClient();
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                return _httpClient;
            }
            catch (Exception ex)
            {
                return new HttpClient();
            }

        }

        private async Task<string> GetTest(HttpClient httpClient)
        {
            try
            {
                string testApiBaseAdress = Configuration["TestApi:TestApiBaseAddress"];
                var response = await httpClient.GetAsync($"{ testApiBaseAdress}/weatherforecast");

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return content;
                }
                else
                {
                    return response.StatusCode.ToString();
                }
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        private async Task<Dictionary<string, string>> GetTestGraph(HttpClient httpClient)
        {
            try
            {
                string testApiBaseAdress = Configuration["TestApi:TestApiBaseAddress"];
                var response = await httpClient.GetAsync($"{ testApiBaseAdress}/weatherforecast/GetGraph");
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<Dictionary<string, string>>(content);

                    return result;
                }
                else
                {
                    return new Dictionary<string, string>();
                }
            }
            catch (Exception ex)
            {
                return new Dictionary<string, string>();
            }
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
