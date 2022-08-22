using GeneratorToken.Data;
using GeneratorToken.Dtos.AppUserDtos;
using GeneratorToken.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using static GeneratorToken.Helper.Helpers;

namespace GeneratorToken.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly AppDbContext _context;
        public AccountController(UserManager<AppUser> userManager, RoleManager<IdentityRole> roleManager, AppDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
        }


        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterUserDto registerUserDto)
        {
            AppUser user = await _userManager.FindByNameAsync(registerUserDto.UserName);
            if (user != null)
            {
                return BadRequest();
            }
            user = new AppUser
            {
                Name = registerUserDto.Name,
                Email = registerUserDto.Email,
                UserName = registerUserDto.UserName,
                Surname = registerUserDto.Surname,
            };
            var result = await _userManager.CreateAsync(user, registerUserDto.Password);
            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }
            result = await _userManager.AddToRoleAsync(user, "Admin");
            return Ok();
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginUserDto loginUserDto)
        {
            AppUser user = await _userManager.FindByNameAsync(loginUserDto.UserName);
            if (user == null)
            {
                return NotFound();
            }
            if (!await _userManager.CheckPasswordAsync(user, loginUserDto.Password))
            {
                return BadRequest();
            }
            List<Claim> claims = new List<Claim>();
            claims.Add(new Claim(ClaimTypes.NameIdentifier, user.Id));
            claims.Add(new Claim("Name", user.Name));
            claims.Add(new Claim("Surname", user.Surname));
            claims.Add(new Claim(ClaimTypes.Name, user.UserName));
            var roles = await _userManager.GetRolesAsync(user);
            foreach (var item in roles)
            {
                claims.Add(new Claim("Role", item));
            }
            string secreKey = "2ee9d5f7-3dd0-4a06-a341-7f7cdc1a7f9c";
            SymmetricSecurityKey key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(secreKey));
            SigningCredentials credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.Now.AddHours(3),
                SigningCredentials = credentials,
                Audience = "http://localhost:20525/",
                Issuer = "http://localhost:20525/"

            };
            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

            return Ok(new { token = tokenHandler.WriteToken(token) });


        }

        [HttpGet("users")]

        public async Task<IActionResult> GetUsers()
        {
            List<AppUser> appUsers = _context.Users.ToList();
            return Ok(appUsers);

        }
        [HttpGet("userprofile")]
        [Authorize]
        public async Task<IActionResult> GetProfile()
        {
            AppUser user = await _userManager.FindByNameAsync(User.Identity.Name);

            return Ok(new { Name = user.UserName });
         
        }


        [HttpGet]
        public async Task CreateRole()
        {
            foreach (var item in Enum.GetValues(typeof(UserRoles)))
            {
                if (!await _roleManager.RoleExistsAsync(item.ToString()))
                {
                    await _roleManager.CreateAsync(new IdentityRole { Name = item.ToString() });
                }
            }
        }
    }
}
