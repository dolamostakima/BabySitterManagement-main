using Microsoft.AspNetCore.Identity;
using SmartBabySitter.Models;
using SmartBabySitter.Services.DTOs;

namespace SmartBabySitter.Services;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterRequestDto req);
    Task<AuthResponseDto> LoginAsync(LoginRequestDto req);
}

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signIn;
    private readonly IJwtTokenService _jwt;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signIn,
        IJwtTokenService jwt)
    {
        _userManager = userManager;
        _signIn = signIn;
        _jwt = jwt;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterRequestDto req)
    {
        var role = req.Role.Trim();
        if (role is not ("Parent" or "BabySitter"))
            throw new InvalidOperationException("Role must be Parent or BabySitter.");

        var user = new ApplicationUser
        {
            FullName = req.FullName,
            Email = req.Email,
            UserName = req.Email,
            EmailConfirmed = true,
            NidNo = req.nidNo,
            Gender=req.gender,
            DateOfBirth=req.dateOfBirth,
            Address=req.address,
            PhoneNo=req.phoneNo,
            Experience=req.Experience,
            Type=req.type == 1 ? "Parent" : "BabySitter"

        };

        var result = await _userManager.CreateAsync(user, req.Password);
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));

        await _userManager.AddToRoleAsync(user, role);

        var roles = await _userManager.GetRolesAsync(user);
        var token = await _jwt.CreateTokenAsync(user, roles);

        return new AuthResponseDto(token, user.Id, user.FullName, user.Email ?? "", roles);
    }

    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto req)
    {
        var user = await _userManager.FindByEmailAsync(req.Email)
            ?? throw new UnauthorizedAccessException("Invalid credentials.");

        var ok = await _signIn.CheckPasswordSignInAsync(user, req.Password, false);
        if (!ok.Succeeded) throw new UnauthorizedAccessException("Invalid credentials.");

        var roles = await _userManager.GetRolesAsync(user);
        var token = await _jwt.CreateTokenAsync(user, roles);

        return new AuthResponseDto(token, user.Id, user.FullName, user.Email ?? "", roles);
    }
}