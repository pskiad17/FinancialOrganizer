﻿using Application.Common.Adapters;
using Infrastructure.Identity;
using Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace Infrastructure
{
    public static class InfrastructureDI
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<IUserManagerService, UserManagerService>();
            services.AddScoped<IJwtTokenGeneratorService, JwtTokenGeneratorService>();
            services.AddScoped<ICurrentUserService, CurrentUserService>();

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

            var builder = services.AddIdentityCore<ApplicationUser>(
               option =>
               {
                   option.User.RequireUniqueEmail = true;
                   option.Password.RequireDigit = true;
                   option.Password.RequiredLength = 8;
                   option.Password.RequireNonAlphanumeric = false;
                   option.Password.RequireUppercase = false;
                   option.Password.RequireLowercase = true;
               }
            ).AddRoles<ApplicationRole>()
             .AddEntityFrameworkStores<ApplicationDbContext>();

            var identityBuilder = new IdentityBuilder(builder.UserType, builder.RoleType, builder.Services);
            identityBuilder.AddEntityFrameworkStores<ApplicationDbContext>();
            identityBuilder.AddRoles<ApplicationRole>();
            identityBuilder.AddSignInManager<SignInManager<ApplicationUser>>();

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["TokenKey"]));
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(option =>
                {
                    option.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = key,
                        ValidateAudience = false,
                        ValidateIssuer = false
                    };
                });

            services.AddAuthorization();

            return services;
        }
    }
}
