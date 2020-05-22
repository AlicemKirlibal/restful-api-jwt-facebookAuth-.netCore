using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TweetBook.Contract.v1.Requests
{
    public class UserFacebookAuthRequest
    {
        public string accessToken { get; set; }
    }
}
