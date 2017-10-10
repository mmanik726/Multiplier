using CoinbaseExchange.NET.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoinbaseExchange.NET.Endpoints.Account
{
    public class AccountClient : ExchangeClientBase
    {
        public AccountClient(CBAuthenticationContainer authContainer)
            : base(authContainer)
        {
        }

        public async Task<ListAccountsResponse> ListAccounts(string accountId = null, string cursor = null, long recordCount = 100, RequestPaginationType paginationType = RequestPaginationType.After)
        {
            var request = new ListAccountsRequest(accountId, cursor, recordCount, paginationType);



            ExchangeResponse response = null;

            try
            {
                response = await this.GetResponse(request);
            }
            catch (Exception)
            {
                throw new Exception("ListAccountError");
            }


            var accountResponse = new ListAccountsResponse(response);
            return accountResponse;
        }

        public async Task<GetAccountHistoryResponse> GetAccountHistory(string accountId)
        {
            var request = new GetAccountHistoryRequest(accountId);


            ExchangeResponse response = null;

            try
            {
                response = await this.GetResponse(request);
            }
            catch (Exception)
            {
                throw new Exception("GetAcHistoryError");
            }

            var accountHistoryResponse = new GetAccountHistoryResponse(response);
            return accountHistoryResponse;
        }

        public async Task<GetAccountHoldsResponse> GetAccountHolds(string accountId)
        {
            var request = new GetAccountHoldsRequest(accountId);

            ExchangeResponse response = null;

            try
            {
                response = await this.GetResponse(request);
            }
            catch (Exception)
            {
                throw new Exception("GetAcHoldsError");
            }

            var accountHoldsResponse = new GetAccountHoldsResponse(response);
            return accountHoldsResponse;
        }
    }
}
