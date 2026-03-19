using Isopoh.Cryptography.Argon2;
using Microsoft.AspNetCore.Mvc;
using Pagination_Project.Models;
using Pagination_Project.Services;
using Pagination_Project.Models;
using Pagination_Project.Services;

namespace Pagination_Project.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly SupabaseAuthService _supabaseAuthService;

        public AuthController(SupabaseAuthService supabaseAuthService)
        {
            _supabaseAuthService = supabaseAuthService;
        }

        [HttpPost("login")]
        public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new LoginResponse
                {
                    Success = false,
                    Message = "Invalid data."
                });
            }

            var user = await _supabaseAuthService.GetUserByUsernameAsync(model.Username);

            if (user == null)
            {
                return Unauthorized(new LoginResponse
                {
                    Success = false,
                    Message = "Incorrect user or password."
                });
            }

            bool passwordOk;

            try
            {
                passwordOk = Argon2.Verify(user.Password, model.Password);
            }
            catch
            {
                passwordOk = false;
            }

            if (!passwordOk)
            {
                return Unauthorized(new LoginResponse
                {
                    Success = false,
                    Message = "Incorrect user or password."
                });
            }

            return Ok(new LoginResponse
            {
                Success = true,
                Message = "Successful login.",
                Username = user.Username
            });
        }
    }
}