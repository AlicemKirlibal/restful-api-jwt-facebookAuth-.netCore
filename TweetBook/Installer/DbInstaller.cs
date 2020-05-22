using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TweetBook.Data;
using TweetBook.Domain;
using TweetBook.Services;

namespace TweetBook.Installer
{
    public class DbInstaller : IInstaller
    {
        public void InstallServices(IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<DataContext>(optionsAction:options =>
                   options.UseSqlServer(
                       configuration.GetConnectionString(name:"DefaultConnection")));
            services.AddDefaultIdentity<IdentityUser>()
              .AddEntityFrameworkStores<DataContext>();


            //post servisimi import ettim
            services.AddScoped<IPostService, PostService>();
        }
    }
}
