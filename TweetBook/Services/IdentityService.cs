using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using TweetBook.Data;
using TweetBook.Domain;
using TweetBook.Options;

namespace TweetBook.Services
{
    public class IdentityService : IIdentityService
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly JwtSettings _jwtSettings;
        private readonly TokenValidationParameters _tokenValidationParameters;
        private readonly DataContext _context;
        private readonly IFacebookAuthService _facebookAuthService;
      

        public IdentityService(UserManager<IdentityUser> userManager, JwtSettings jwtSettings,TokenValidationParameters tokenValidationParameters,DataContext context, IFacebookAuthService facebookAuthService)
        {
            _userManager = userManager;
            _jwtSettings = jwtSettings;
            _tokenValidationParameters = tokenValidationParameters;
            _context = context;
            _facebookAuthService = facebookAuthService;
        }


        public async Task<AuthenticationResult> RegisterAsync(string email, string password)
        {
            var existingUser = await _userManager.FindByEmailAsync(email);

            if (existingUser != null)
            {
                return new AuthenticationResult
                {
                    Errors = new[] { "Bu email adresi alınmış lütfen başka bir adres deneyiniz." }
                };
            }

            var newUser = new IdentityUser
            {
                Email = email,
                UserName = email
            };

            var createdUser = await _userManager.CreateAsync(newUser, password);

            if (!createdUser.Succeeded)
            {
                return new AuthenticationResult
                {
                    Errors = createdUser.Errors.Select(i => i.Description)
                };
            }

            return await GenerateAuthenticationResultForUserAsync(newUser);
        }


        public async Task<AuthenticationResult> LoginAsync(string email, string password)
        {
            var user = await _userManager.FindByEmailAsync(email);

            if (user==null)
            {
                return new AuthenticationResult
                {
                    Errors = new[] { "Kullanıcı bulunamadı" }
                };
            }

            var userHasValidPassword = await _userManager.CheckPasswordAsync(user, password);

            if (!userHasValidPassword)
            {
                return new AuthenticationResult
                {
                    Errors = new[] { "Kullanıcı adı ve parola eşleşmiyor" }
                };
            }

            return await GenerateAuthenticationResultForUserAsync(user);

        }

        public async Task<AuthenticationResult> RefreshTokenAsync(string token, string refreshToken)
        {
            var validatedToken = GetPrincipalFromToken(token);

            if (validatedToken==null)
            {
                return new AuthenticationResult { Errors = new[] { "Invalid Token" } };
            }

            var expiryDateUnix =
                long.Parse(validatedToken.Claims.Single(i => i.Type == JwtRegisteredClaimNames.Exp).Value);
            var expiryDateTimeUtc = new DateTime(year: 1970, month: 1, day: 1, hour: 0, minute: 0, second: 0, DateTimeKind.Utc)
                .AddSeconds(expiryDateUnix);

            if (expiryDateTimeUtc > DateTime.UtcNow)
            {
                return new AuthenticationResult { Errors = new[] { "This Token hasnt expired yet" } };
            }

            var jti = validatedToken.Claims.Single(i => i.Type == JwtRegisteredClaimNames.Jti).Value;

            var storedRefreshToken = await _context.RefreshTokens.SingleOrDefaultAsync(predicate:i=>i.Token == refreshToken);

            if (storedRefreshToken==null)
            {
                return new AuthenticationResult { Errors = new[] { "This refresh token does not exist" } };
            }
            if (DateTime.UtcNow > storedRefreshToken.ExpiryDate)
            {
                return new AuthenticationResult { Errors = new []{ "This refresh token has expired" } };
            }

            if (storedRefreshToken.Invalidated)
            {
                return new AuthenticationResult { Errors = new[] { "This refresh token has been invalited" } };
            }
            if (storedRefreshToken.Used)
            {
                return new AuthenticationResult { Errors = new[] { "This refresh token has been used" } };
            }
            if (storedRefreshToken.JwtId !=jti)
            {
                return new AuthenticationResult { Errors = new[] { "This refresh token does not match this jwt" } };
            }
            storedRefreshToken.Used = true;
            _context.RefreshTokens.Update(storedRefreshToken);
            await _context.SaveChangesAsync();

            var user = await _userManager.FindByIdAsync(validatedToken.Claims.Single(i => i.Type == "id").Value);
            return await GenerateAuthenticationResultForUserAsync(user);
        }

        private ClaimsPrincipal GetPrincipalFromToken(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();

            try
            {
                var principal = tokenHandler.ValidateToken(token, _tokenValidationParameters, out var validatedToken);
                if (!IsJwtWithValidSecurityAlgorithm(validatedToken))
                {
                    return null;
                }
                return principal;
            }
            catch
            {
                return null;
            }
            
        }

        private bool IsJwtWithValidSecurityAlgorithm(SecurityToken validatedToken)
        {
            return (validatedToken is JwtSecurityToken jwtSecurityToken) &&
                jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256,StringComparison.InvariantCultureIgnoreCase);
        }


        private async Task<AuthenticationResult> GenerateAuthenticationResultForUserAsync(IdentityUser user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_jwtSettings.Secret);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(type:JwtRegisteredClaimNames.Sub,value:user.Email),
                     new Claim(type:JwtRegisteredClaimNames.Jti,value:Guid.NewGuid().ToString()),
                      new Claim(type:JwtRegisteredClaimNames.Email,value:user.Email),
                      new Claim(type:"id",value:user.Id)
                }),
                Expires = DateTime.UtcNow.Add(_jwtSettings.TokenLifetime),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), algorithm: SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);

            var refreshToken = new RefreshToken
            {
                JwtId = token.Id,
                UserId = user.Id,
                CreationDate = DateTime.UtcNow,
                ExpiryDate = DateTime.UtcNow.AddMonths(6),

            };

            await _context.RefreshTokens.AddAsync(refreshToken);
            await _context.SaveChangesAsync();

            return new AuthenticationResult
            {
                Success = true,
                Token = tokenHandler.WriteToken(token),
                RefreshToken = refreshToken.Token
            };
        }

        public  async Task<AuthenticationResult> LoginWithFacebookAsync(string accessToken)
        {
            var validatedToken = await _facebookAuthService.ValidateAccessTokenAsync(accessToken);

            if (!validatedToken.Data.IsValid)
            {
                return new AuthenticationResult
                {
                    Errors = new[] { "Invalid Facebook token" }
                };
            }

            var userInfo = await _facebookAuthService.GetUserInfoResultAsync(accessToken);

            var user = await _userManager.FindByEmailAsync(userInfo.Email);

            if (user==null)
            {
                var identityUser = new IdentityUser
                {
                    Id = Guid.NewGuid().ToString(),
                    Email=userInfo.Email,
                    UserName=userInfo.Email
                };

                var createdResult = await _userManager.CreateAsync(identityUser);

                if (!createdResult.Succeeded)
                {
                    return new AuthenticationResult
                    {
                        Errors = new[] { "someting went wrong" }
                    };
                }

                return await GenerateAuthenticationResultForUserAsync(identityUser);
            }

            return await GenerateAuthenticationResultForUserAsync(user);





           



        }
    }
}
