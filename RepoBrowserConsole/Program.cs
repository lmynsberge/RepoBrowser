using System;
using System.Net.Http;
using Newtonsoft.Json;
using DataModels.Internal;

namespace RepoBrowserConsole
{
    class Program
    {
        private const string s_BaseUri = "http://localhost:5000"; 
        static void Main(string[] args)
        {
            Console.WriteLine("== Welcome to the Pull Request Repository Browser ==");
            Console.WriteLine("");

            Console.WriteLine("Please enter the number of an available organization to perform PR searches on the organization.");
            Console.WriteLine(" --> 1 - Ramda Organization");
            string organization = Console.ReadLine();
            if (organization != "1")
            {
                Console.WriteLine("You did not select a valid organization. Thank you for using the program. Exiting...(Type anything to close)");

                Console.ReadKey();
                return;
            }

            Console.WriteLine("Please enter whether to search for open, closed, or all (default)");
            Console.WriteLine(" --> 1 - All");
            Console.WriteLine(" --> 2 - Open");
            Console.WriteLine(" --> 3 - Closed");
            string stateInput = Console.ReadLine();
            string state = "all";
            if (!",1,2,3,".Contains("," + stateInput + ","))
            {
                Console.WriteLine("You did not select a valid state. Using default (All)");
            }
            if (stateInput == "2")
            {
                state = "open";
            }
            else if (stateInput == "3")
            {
                state = "closed";
            }

            Console.WriteLine("Performing PullRequst search for the Ramda Organization (#1)");

            HttpClient httpClient = new HttpClient();
            HttpRequestMessage requestMessage = new HttpRequestMessage();
            requestMessage.RequestUri = new Uri(new Uri(s_BaseUri), "/api/prs/1?state=" + state);
            requestMessage.Headers.Add("Accept", "application/json");
            HttpResponseMessage response = httpClient.SendAsync(requestMessage).Result;

            // Parse the result
            PullRequestResponse prContent = JsonConvert.DeserializeObject<PullRequestResponse>(response.Content.ReadAsStringAsync().Result);

            // TODO: Any fun analyzing that's desired.
            // END TODO

            Console.WriteLine("There are " + prContent.TotalCount + " total pull requests at the Ramda organization.");
        }
    }
}
