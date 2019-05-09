using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using DatingApp.API.Data;
using DatingApp.API.Dtos;
using DatingApp.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace DatingApp.API.Controllers {
    [Route ("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase {
        private readonly IAuthRepository _repo;
        private readonly IConfiguration config;
        public AuthController (IAuthRepository _repo, IConfiguration config) {
            this.config = config;
            this._repo = _repo;
        }

        [HttpPost ("register")]
        public async Task<IActionResult> Register (UserForRegisterDto userForRegisterDto) {
            userForRegisterDto.Username = userForRegisterDto.Username.ToLower ();

            if (await _repo.UserExists (userForRegisterDto.Username))
                return BadRequest ("Username already exist");

            var userToCreate = new User {
                Username = userForRegisterDto.Username
            };

            var createUser = await _repo.Register (userToCreate, userForRegisterDto.Password);

            return StatusCode (201);
        }

        [HttpPost ("login")]
        public async Task<IActionResult> Login (UserForLoginDto userForLogInDto) {

            var userFromRepo = await _repo.Login (userForLogInDto.Username.ToLower(), userForLogInDto.Password);

            if (userFromRepo == null)
                return Unauthorized ();

            var claims = new [] {
                new Claim (ClaimTypes.NameIdentifier, userFromRepo.UserId.ToString ()),
                new Claim (ClaimTypes.Name, userFromRepo.Username)
            };

            var key = new SymmetricSecurityKey (Encoding.UTF8
            .GetBytes(config.GetSection("AppSettings:Token").Value));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256Signature);

            var tokenDescriptor = new SecurityTokenDescriptor{
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.Now.AddDays(1),
                SigningCredentials = creds
            };

            var tokenHandler = new JwtSecurityTokenHandler();

            var token = tokenHandler.CreateToken(tokenDescriptor);

            return Ok(new {
                token = tokenHandler.WriteToken(token)
            });
        }

    }
}