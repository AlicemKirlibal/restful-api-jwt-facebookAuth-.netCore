using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TweetBook.Extensions
{
    public static class GeneralExtensions
    {
        public static string GetUserId(this HttpContext httpContext)
        {
            if (httpContext.User==null)
            {
                return string.Empty;
            }

            //tokendan userıd dönücek
            return httpContext.User.Claims.Single(i => i.Type == "id").Value;

        }
    }
}
