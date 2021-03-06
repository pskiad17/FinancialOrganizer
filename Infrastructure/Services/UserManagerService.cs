﻿using Application.Common.Adapters;
using Application.Common.Exceptions;
using Application.Common.Models;
using Application.User;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
    public class UserManagerService : IUserManagerService
    {
        private readonly IJwtTokenGeneratorService _jwtGenerator;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ApplicationDbContext _identityContext;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<ApplicationRole> _roleManager;

        #region Constructor
        public UserManagerService(
            IJwtTokenGeneratorService jwtGenerator,
            IHttpContextAccessor httpContextAccessor,
            ApplicationDbContext identityContext,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<ApplicationRole> roleManager)
        {
            _identityContext = identityContext;
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _jwtGenerator = jwtGenerator;
            _httpContextAccessor = httpContextAccessor;
        }
        #endregion

        #region Methods
        public async Task<LoggedUser> RefreshUser()
        {
            var userId = _httpContextAccessor.HttpContext.User?.FindFirstValue(ClaimTypes.NameIdentifier);

            var user = await _userManager.FindByIdAsync(userId);
            var roleName = await GetRoleNameByUserIdAsync(user.Id);

            return new LoggedUser
            {
                Username = user.DisplayName,
                Token = _jwtGenerator.CreateToken(user, roleName)
            };
        }

        public async Task<LoggedUser> Login(LoginQuery request)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);

            if (user == null)
                throw new RestException(HttpStatusCode.BadRequest);

            var roleName = await GetRoleNameByUserIdAsync(user.Id);

            var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, false);

            if (result.Succeeded)
            {
                return new LoggedUser
                {
                    Username = user.UserName,
                    Token = _jwtGenerator.CreateToken(user, roleName)
                };
            };

            throw new RestException(HttpStatusCode.Unauthorized);
        }

        public async Task<LoggedUser> Register(RegisterCommand command)
        {
            if (await _identityContext.Users.AnyAsync(x => x.Email == command.Email))
                throw new BadRequestException("Email already exists");

            var username = $"{command.FirstName} {command.LastName}";
            if (await _identityContext.Users.AnyAsync(x => x.UserName == username))
                throw new BadRequestException("Username already exists");

            var user = new ApplicationUser(command);

            var result = await _userManager.CreateAsync(user, command.Password);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "User");

                return new LoggedUser
                {
                    Username = user.UserName,
                    Token = _jwtGenerator.CreateToken(user, "User")
                };
            }

            throw new Exception("Problem creating user");
        }

        private async Task<string> GetRoleNameByUserIdAsync(string userId)
        {
            var userRole = await _identityContext.UserRoles.FirstOrDefaultAsync(x => x.UserId == userId);
            var role = await _roleManager.FindByIdAsync(userRole.RoleId);

            return role?.Name ??
                throw new BadRequestException("Role not found");
        }
        #endregion
    }
}
