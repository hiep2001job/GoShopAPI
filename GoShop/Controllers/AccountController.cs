﻿using GoShop.Data;
using GoShop.DTOs;
using GoShop.Entities;
using GoShop.Extensions;
using GoShop.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace GoShop.Controllers
{
    [Authorize]
    public class AccountController : BaseApiController
    {
        private readonly UserManager<User> _userManager;
        private readonly TokenService _tokenService;
        private readonly GoShopContext _context;

        public AccountController(UserManager<User> userManager, TokenService tokenService, GoShopContext context)
        {
            _userManager = userManager;
            _tokenService = tokenService;
            _context = context;
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<ActionResult<UserDto>> Login(LoginDto loginDto)
        {
            var user = await _userManager.FindByNameAsync(loginDto.UserName);
            if (user == null || !await _userManager.CheckPasswordAsync(user, loginDto.Password))
            {
                return Unauthorized();
            }

            var basket = await RetrieveBasket(user);

            //Create and set refreshtoken
            var refreshToken = _tokenService.GenerateRefreshToken();
            SetRefreshToken(refreshToken, user);

            await _context.SaveChangesAsync();

            return new UserDto
            {
                Email = user.Email,
                Token = await _tokenService.GenerateToken(user),
                Basket = basket,

            };
        }

        private async Task<BasketDto?> RetrieveBasket(User user)
        {
            var userBasket = await RetrieveBasket(user.UserName);
            var anonBasket = await RetrieveBasket(Request.Cookies["buyerId"]);

            if (anonBasket != null)
            {
                if (userBasket != null) _context.Baskets.Remove(userBasket);
                anonBasket.BuyerId = user.UserName;
                Response.Cookies.Delete("buyerId");
            }

            var basket = (anonBasket ?? userBasket)?.MapBasketToDto();
            return basket;
        }

        [HttpPost("refresh-token")]
        [AllowAnonymous]
        public async Task<ActionResult<UserDto>> RefreshToken([FromBody] RefreshTokenRequestDto model)
        {
            var user = await _userManager.FindByNameAsync(model.UserName);
            if (user == null)
            {
                return Unauthorized();
            }

            var basket = await RetrieveBasket(user);

            var refreshToken = Request.Cookies["refreshToken"];

            if (!user.RefreshToken!.Equals(refreshToken))
            {
                return Unauthorized("Invalid Refresh Token."+refreshToken);
            }
            else if (user.TokenExpires < DateTime.Now)
            {
                return Unauthorized("Token expired");
            }

            string token = await _tokenService.GenerateToken(user);
            var newRefreshToken = _tokenService.GenerateRefreshToken();
            SetRefreshToken(newRefreshToken, user);

            await _context.SaveChangesAsync();
            return new UserDto
            {
                Email = user.Email,
                Token = token,
                Basket = basket,
            };
 
        }


        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<ActionResult> Register(RegisterDto registerDto)
        {
            var user = new User
            {
                UserName = registerDto.UserName,
                Email = registerDto.Email,
            };
            var result = await _userManager.CreateAsync(user, registerDto.Password);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(error.Code, error.Description);
                };
                return ValidationProblem();

            }
            await _userManager.AddToRoleAsync(user, "Member");
            return StatusCode(201);
        }

        [Authorize]
        [HttpGet("currentUser")]
        public async Task<ActionResult<UserDto>> GetCurrentUser()
        {
            var user = await _userManager.FindByNameAsync(User.Identity.Name);
            return new UserDto
            {
                Email = user.Email,
                Token = await _tokenService.GenerateToken(user),
            };
        }

        [Authorize]
        [HttpGet("savedAddress")]
        public async Task<ActionResult<UserAddress>> GetSavedAddress()
        {
            return await _userManager.Users
                .Where(x => x.UserName == User.Identity.Name)
                .Select(x => x.Address)
                .FirstOrDefaultAsync();
        }

        private async Task<Basket?> RetrieveBasket(string buyerId)
        {
            if (string.IsNullOrEmpty(buyerId))
            {
                Response.Cookies.Delete("buyerId");
                return null;
            }

            return await _context.Baskets
                            .Include(i => i.Items)
                            .ThenInclude(p => p.Product)
                            .FirstOrDefaultAsync(x => x.BuyerId == buyerId);
        }
        private void SetRefreshToken(RefreshToken newRefreshToken, User user)
        {
            var cookieOptions = new CookieOptions
            {
                SameSite = SameSiteMode.None,
                Secure = true,
                IsEssential = true,
                HttpOnly = true,
                Expires = newRefreshToken.Expires,
            };
            Response.Cookies.Append("refreshToken", newRefreshToken.Token, cookieOptions);
            user.RefreshToken = newRefreshToken.Token;
            user.TokenCreated = newRefreshToken.Created;
            user.TokenExpires = newRefreshToken.Expires;
        }

    }
}
