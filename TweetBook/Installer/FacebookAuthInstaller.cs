using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TweetBook.Options;
using TweetBook.Services;

namespace TweetBook.Installer
{
    public class FacebookAuthInstaller : IInstaller
    {
        public void InstallServices(IServiceCollection services, IConfiguration configuration)
        {
           
            var facebookAuthSettings = new FacebookAuthSettings();

            configuration.Bind(key: nameof(FacebookAuthSettings), facebookAuthSettings);
            services.AddSingleton(facebookAuthSettings);

            services.AddHttpClient();
            services.AddSingleton<IFacebookAuthService, FacebookAuthService>();


        }
    }
}
