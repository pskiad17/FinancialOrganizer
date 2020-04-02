﻿using Application.Interfaces;
using Application.Models;
using FluentValidation;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Application.User
{
    public class LoginQuery : IRequest<LoggedUserModel>
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }

    public class LoginQueryValidator : AbstractValidator<LoginQuery>
    {
        public LoginQueryValidator()
        {
            RuleFor(x => x.Email).NotEmpty();
            RuleFor(x => x.Password).NotEmpty();
        }
    }

    public class LoginQueryHandler : IRequestHandler<LoginQuery, LoggedUserModel>
    {
        private readonly IUserManagerService _userManagerService;

        public LoginQueryHandler(IUserManagerService userManagerService) => 
            _userManagerService = userManagerService;

        public Task<LoggedUserModel> Handle(LoginQuery request, CancellationToken cancellationToken) => 
            _userManagerService.Login(request);
    }
}