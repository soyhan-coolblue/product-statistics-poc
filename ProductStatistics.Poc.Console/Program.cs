using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace ProductStatistics.Poc.Console
{
    public class Program
    {
        static async Task Main(string[] args)
        {
            while (true)
            {
                System.Console.WriteLine("How many product you want to query:");
                int productNumberToBeSent = Convert.ToInt32(System.Console.ReadLine());

                for (int i = 1; i < 6; i++)
                {
                    using (var client = new HttpClient())
                    {
                        client.BaseAddress = new Uri("https://acc-rest.onetrail.net");
                        var base64Credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"PUT_USERNAME_HERE:PUT_PASSWORD_HERE"));
                        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", base64Credentials);
                        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                        Stopwatch timer = Stopwatch.StartNew();
                        var response = await client.PostAsync("productApi/v1/products/bulk", GetProductBatch(productNumberToBeSent));
                        timer.Stop();
                        System.Console.WriteLine($"Attempt {i} Onetrail responded with : {response.StatusCode} in {timer.Elapsed} for {productNumberToBeSent} number of product request");
                        System.Console.WriteLine("");
                        try
                        {
                            response.EnsureSuccessStatusCode();
                        }
                        catch (HttpRequestException e)
                        {
                            System.Console.WriteLine($"Request failed. {e.Message}");
                        }
                    }
                }
                System.Console.WriteLine("");
                System.Console.WriteLine("");
                await Task.Delay(2000);
            }
        }

        private static HttpContent GetProductBatch(int productNumberToBeSent)
        {
            var request = new Root();

            var productIdentifications = GetDataFromCsv();

            request.products.AddRange(productIdentifications.Take(productNumberToBeSent).Select(a => new Product()
            {
                gtin = a.EAN,
                mfpn = a.MPN
            }));

            var json = JsonConvert.SerializeObject(request);

            return new StringContent(json, Encoding.UTF8, "application/json");
        }

        private static IReadOnlyCollection<(string MPN, string EAN)> GetDataFromCsv()
        {
            var pathToCsv = @$"{Directory.GetCurrentDirectory()}\ean-mpn-product.csv";

            var lines = File.ReadAllLines(pathToCsv);

            return lines.Where(a => a.Contains(","))
                .Select(a =>
                {
                    var values = a.Split(",");
                    return (values[0], values[1]);
                }).ToArray();
        }
    }

    public class Product
    {
        public string mfpn { get; set; }
        public string gtin { get; set; }
        public string pdiIdentifier { get; set; }
    }

    public class Root
    {
        public List<Product> products { get; set; } = new List<Product>();
    }
}
