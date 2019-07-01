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
using AutoMapper;

namespace DatingApp.API.Controllers 
{
    [Route ("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase 
    {
        private readonly IAuthRepository _repo;
        private readonly IConfiguration _config;
        private readonly IMapper _mapper;
        
        public AuthController (IAuthRepository repo, IConfiguration config, IMapper mapper) 
        {
            this._config = config;
            this._repo = repo;
            this._mapper = mapper;
        }

        [HttpPost ("register")]
        public async Task<IActionResult> Register (UserForRegisterDto userForRegisterDto) 
        {
            userForRegisterDto.Username = userForRegisterDto.Username.ToLower ();

            if (await _repo.UserExists (userForRegisterDto.Username))
                return BadRequest ("Username already exist");

            var userToCreate = _mapper.Map<User>(userForRegisterDto);

            var createUser = await _repo.Register (userToCreate, userForRegisterDto.Password);

            var userToReturn = _mapper.Map<UserForDetailedDto>(createUser);

            return CreatedAtRoute("GetUser", new {Controller = "Users", 
                id = createUser.UserId }, userToReturn);
        }

        [HttpPost ("login")]
        public async Task<IActionResult> Login ([FromBody]UserForLoginDto userForLogInDto) 
        {

            var userFromRepo = await _repo.Login (userForLogInDto.Username.ToLower(), userForLogInDto.Password);

            if (userFromRepo == null)
                return Unauthorized ();
 
            var claims = new [] {
                new Claim (ClaimTypes.NameIdentifier, userFromRepo.UserId.ToString ()),
                new Claim (ClaimTypes.Name, userFromRepo.Username)
            };

            var key = new SymmetricSecurityKey (Encoding.UTF8
                .GetBytes (_config.GetSection ("AppSettings:Token").Value));

            var creds = new SigningCredentials (key, SecurityAlgorithms.HmacSha256Signature);

            var tokenDescriptor = new SecurityTokenDescriptor {
                Subject = new ClaimsIdentity (claims),
                Expires = DateTime.Now.AddDays (1),
                SigningCredentials = creds
            };

            var tokenHandler = new JwtSecurityTokenHandler ();

            var token = tokenHandler.CreateToken (tokenDescriptor);

            var user = _mapper.Map<UserForListDto>(userFromRepo);

            return Ok (new {
                token = tokenHandler.WriteToken (token),
                user
            });

        }

    }
}