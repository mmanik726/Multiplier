﻿using CoinbaseExchange.NET.Core;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace CoinbaseExchange.NET.Endpoints.Account
{
    public class ListAccountsResponse : ExchangePageableResponseBase
    {
        public IEnumerable<Account> Accounts { get; private set; }

        public ListAccountsResponse(ExchangeResponse response) : base(response)
        {
            var json = response.ContentBody;
            var jArray = JArray.Parse(json);

            Accounts = jArray.Select(elem => new Account(elem)).ToList();
        }
    }
}
