using AuthService.DTO;
using AuthService.Models;
using AuthService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace AuthService.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IJwtTokenService _jwtService;

        public AuthController(UserManager<ApplicationUser> userManager, IJwtTokenService jwtService)
        {
            _userManager = userManager;
            _jwtService = jwtService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequest request)
        {
            var existing = await _userManager.FindByNameAsync(request.UserName);
            if (existing != null)
                return BadRequest(new { message = "Username already exists" });

            var user = new ApplicationUser
            {
                UserName = request.UserName,
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName
            };

            var result = await _userManager.CreateAsync(user, request.Password);

            if (!result.Succeeded)
                return BadRequest(result.Errors);

            await _userManager.AddToRoleAsync(user, "Member");

            // Return JSON instead of plain text
            return Ok(new
            {
                message = "User created",
                userName = user.UserName,
                email = user.Email
            });
        }


        
        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            var user =
                await _userManager.FindByNameAsync(request.UserNameOrEmail)
                ?? await _userManager.FindByEmailAsync(request.UserNameOrEmail);

            if (user == null)
                return Unauthorized("Invalid credentials");

            if (!await _userManager.CheckPasswordAsync(user, request.Password))
                return Unauthorized("Invalid credentials");

            var (token, expires) = await _jwtService.CreateToken(user);

            var roles = await _userManager.GetRolesAsync(user);

            return Ok(new
            {
                message = "Login successful",
                token,
                expiresAt = expires,
                roles
            });
        }

        // Register employee endpoint
        [Authorize(Roles = "Admin")]
        [HttpPost("register-employee")]
        public async Task<IActionResult> RegisterEmployee(RegisterRequest request)
        {
            var existing = await _userManager.FindByNameAsync(request.UserName);
            if (existing != null)
                return BadRequest("Username already exists");
            var user = new ApplicationUser
            {
                UserName = request.UserName,
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName
            };
            var result = await _userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
                return BadRequest(result.Errors);
            // Giv ny medarbejder rolle
            await _userManager.AddToRoleAsync(user, "Employee");
            return Ok("Employee user created");
        }
        
        // Sign out endpoint
        [HttpPost("logout")]
        public IActionResult Logout()
        {
            return Ok(new { message = "Logout successful" });
        }
    }
}
