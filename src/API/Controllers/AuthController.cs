using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Auth;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{

    private readonly ITokenService _tokenService;
    private readonly InMemoryUserStore _userStore;
    
    public AuthController(ITokenService tokenService, InMemoryUserStore userStore)
    {
        _tokenService = tokenService;
        _userStore = userStore;
    }
    
    [HttpPost("login")]
    public IActionResult Login(LoginRequestDto request)
    {
        var user = _userStore.FindByUsername(request.Username);
        if (user == null)
            return Unauthorized();

        if (request.Password != user.PasswordHash)
            return Unauthorized();

        var token = _tokenService.GenerateToken(user);

        return Ok(new { token });
    }
}