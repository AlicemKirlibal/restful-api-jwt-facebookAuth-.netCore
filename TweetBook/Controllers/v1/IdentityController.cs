using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Threading.Tasks;
using TweetBook.Contract;
using TweetBook.Contract.v1.Requests;
using TweetBook.Contract.v1.Responses;
using TweetBook.Services;

namespace TweetBook.Controllers.v1
{
    public class IdentityController : Controller
    {
        private readonly IIdentityService _identityService;
        public IdentityController(IIdentityService identityService)
        {
            _identityService = identityService;
        }

        [HttpPost(template:ApiRoutes.Identity.Register)]
        public async Task<IActionResult> Register([FromBody]UserRegistrationRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new AuthFailedResponse
                {
                    Errors = ModelState.Values.SelectMany(i => i.Errors.Select(ii => ii.ErrorMessage))
                });
            }


            var authResponse = await _identityService.RegisterAsync(request.Email, request.Password);

            if (!authResponse.Success)
            {
                return BadRequest(new AuthFailedResponse
                {
                    Errors = authResponse.Errors
                });
            }



            return Ok(new AuthSuccessResponse
            {
                Token=authResponse.Token,
                RefreshToken = authResponse.RefreshToken
            });
        }

        [HttpPost(template:ApiRoutes.Identity.Login)]
        public async Task<IActionResult> Login([FromBody]UserLoginRequest request)
        {
            var authResponse = await _identityService.LoginAsync(request.Email, request.Password);


            if (!authResponse.Success)
            {
                return BadRequest(new AuthFailedResponse
                {
                    Errors = authResponse.Errors
                });
            }



            return Ok(new AuthSuccessResponse
            {
                Token = authResponse.Token,
                RefreshToken = authResponse.RefreshToken
            });
        }

        [HttpPost(ApiRoutes.Identity.Refresh)]
        public async Task<IActionResult>Refresh([FromBody]RefreshTokenRequest request)
        {
            var authResponse = await _identityService.RefreshTokenAsync(request.Token, request.RefreshToken);

            if (!authResponse.Success)
            {
                return BadRequest(new AuthFailedResponse
                {
                    Errors = authResponse.Errors
                });
            }



            return Ok(new AuthSuccessResponse
            {
                Token = authResponse.Token,
                RefreshToken = authResponse.RefreshToken
            });

        }

        [HttpPost(template: ApiRoutes.Identity.FacebookAuth)]
        public async Task<IActionResult> FacebookAuth([FromBody]UserFacebookAuthRequest request)
        {
            var authResponse = await _identityService.LoginWithFacebookAsync(request.accessToken);


            if (!authResponse.Success)
            {
                return BadRequest(new AuthFailedResponse
                {
                    Errors = authResponse.Errors
                });
            }



            return Ok(new AuthSuccessResponse
            {
                Token = authResponse.Token,
                RefreshToken = authResponse.RefreshToken
            });
        }






    }
}
