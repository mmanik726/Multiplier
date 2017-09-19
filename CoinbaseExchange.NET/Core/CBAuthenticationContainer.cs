﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace CoinbaseExchange.NET.Core
{
    public class CBAuthenticationContainer
    {
        private readonly string Secret;
        public string ApiKey { get; private set; }
        public string Passphrase { get; private set; }

        public CBAuthenticationContainer(string apiKey, string passphrase, string secret)
        {
            if (String.IsNullOrWhiteSpace(apiKey))
                throw new ArgumentNullException("apiKey", "An API key is required to use the coinbase API");

            if (String.IsNullOrWhiteSpace(passphrase))
                throw new ArgumentNullException("passphrase", "A passphrase is required to use the coinbase API");

            if (String.IsNullOrWhiteSpace(secret))
                throw new ArgumentNullException("secret", "A secret is required to use the coinbase API");

            this.ApiKey = apiKey;
            this.Passphrase = passphrase;
            this.Secret = secret;
        }

        public CBAuthenticationContainer()
        {

        }

        public string ComputeSignature(string timestamp, string relativeUrl, string method, string body)
        {
            byte[] data = Convert.FromBase64String(this.Secret);
            var prehash = timestamp + method + relativeUrl + body;
            return HashString(prehash, data);
        }

        private string HashString(string str, byte[] secret)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(str);
            using (var hmac = new HMACSHA256(secret))
            {
                byte[] hash = hmac.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }
    }
}
