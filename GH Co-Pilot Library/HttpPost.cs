using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft.Json;

namespace GH_Co_Pilot_Library
{
    //this class will post a string to an  API using an HTTPClient

    internal class HttpPost
    {
        #region Config

        //It is recommended to instantiate one HttpClient for your application's lifetime and share it unless you have a specific reason not to.           
        private static readonly HttpClient client = new HttpClient();
        const string API_URL = "https://api.twitter.com/1.1/statuses/update.json";
        #endregion

        /// <summary>
        /// Creates a connection to the API and returns the response as a string
        /// </summary>
        /// <param name="content">The conetnt to send to the API</param>
        /// <returns>the response form the POST request</returns>
        private string SendMessageToAPI(string content)
        {

        }
    }
}
