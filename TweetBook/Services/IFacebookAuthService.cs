using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TweetBook.External.Contract;

namespace TweetBook.Services
{
    public interface IFacebookAuthService
    {
        Task<FacebookTokenValidationResult> ValidateAccessTokenAsync(string accessToken);

        Task<FacebookUserInfoResult> GetUserInfoResultAsync(string accessToken);
    }
}
