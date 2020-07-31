using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using WebApplication1.Extensions;
using Microsoft.IdentityModel.Tokens;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    [Route("api/identidade")] // Definindo uma rota para nossa API
    public class AuthController : MainController
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly AppSettings _appSettings;

        public AuthController(SignInManager<IdentityUser> signInManager, UserManager<IdentityUser> userManager, IOptions<AppSettings> appSettings)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _appSettings = appSettings.Value;
        }

        [HttpPost("nova-conta")]
        public async Task<ActionResult> Register(UsuarioRegistro usuarioRegistro)
        {
            if (!ModelState.IsValid)
                return CustomResponse(ModelState); // Se ModelState não for válida, passamos a ModelState por parametro para o método CustomResponse do controlador Main para que possa coletar os erros.

            var user = new IdentityUser
            {
                UserName = usuarioRegistro.Email,
                Email = usuarioRegistro.Email,
                EmailConfirmed = true
            };

            // Criando novo usuário
            var result = await _userManager.CreateAsync(user, usuarioRegistro.Senha);

            if (result.Succeeded)
            {
                // Se usuário foi criado com sucesso, geramos um JWT através da chamada do método GerarJwt passando o E-mail do usuário por parâmetro.
                return CustomResponse(await GerarJwt(usuarioRegistro.Email));
            }
            else
            {
                // Senão coleta erros do nosso resultado e os passa por parâmetro para o método AdicionarErroProcessamento
                foreach (var error in result.Errors)
                {
                    AdicionarErroProcessamento(error.Description);
                }

                return CustomResponse();
            }
        }

        [HttpPost("autenticar")]
        public async Task<ActionResult> Login(UsuarioLogin usuarioLogin)
        {
            if (!ModelState.IsValid)
                return CustomResponse(ModelState);

            var result = await _signInManager.PasswordSignInAsync(usuarioLogin.Email, usuarioLogin.Senha, false, true);

            if (result.Succeeded)
            {
                return CustomResponse(await GerarJwt(usuarioLogin.Email));
            }
            else
            {
                if (result.IsLockedOut) // Se tentativas de logar exceder o limite estabelecido, geralmente 5 tentativas.
                {
                    AdicionarErroProcessamento("Usuário temporariamente bloqueado por tentativas inválidas.");
                    return CustomResponse();
                }

                AdicionarErroProcessamento("Usuário ou senha incorretos.");
                return CustomResponse();
            }
        }

        private async Task<UsuarioRespostaLogin> GerarJwt(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            var claims = await _userManager.GetClaimsAsync(user);
            var identityClaims = await ObterClaimsUsuario(claims, user);
            var encodedToken = CodificarToken(identityClaims);

            return ObterRespostaToken(encodedToken, user, claims);
        }

        private async Task<ClaimsIdentity> ObterClaimsUsuario(ICollection<Claim> claims, IdentityUser user)
        {
            var userRoles = await _userManager.GetRolesAsync(user);

            claims.Add(new Claim(JwtRegisteredClaimNames.Sub, user.Id));
            claims.Add(new Claim(JwtRegisteredClaimNames.Email, user.Email));
            claims.Add(new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()));
            claims.Add(new Claim(JwtRegisteredClaimNames.Nbf, ToUnixEpochDate(DateTime.UtcNow).ToString()));
            claims.Add(new Claim(JwtRegisteredClaimNames.Iat, ToUnixEpochDate(DateTime.UtcNow).ToString(), ClaimValueTypes.Integer64));

            foreach (var userRole in userRoles)
            {
                claims.Add(new Claim("role", userRole));
            }

            var identityClaims = new ClaimsIdentity();
            identityClaims.AddClaims(claims);

            return identityClaims;
        }

        private string CodificarToken(ClaimsIdentity identityClaims)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_appSettings.Secret);

            var token = tokenHandler.CreateToken(
                new SecurityTokenDescriptor
                {
                    Issuer = _appSettings.Emissor,
                    Audience = _appSettings.ValidoEm,
                    Subject = identityClaims,
                    Expires = DateTime.UtcNow.AddHours(_appSettings.ExpiracaoHoras),
                    SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
                });

            var encodedToken = tokenHandler.WriteToken(token);

            return encodedToken;
        }

        private UsuarioRespostaLogin ObterRespostaToken(string encodedToken, IdentityUser user, ICollection<Claim> claims)
        {
            var response = new UsuarioRespostaLogin
            {
                AccessToken = encodedToken,
                ExpiresIn = TimeSpan.FromHours(_appSettings.ExpiracaoHoras).TotalSeconds,
                UsuarioToken = new UsuarioToken
                {
                    Id = user.Id,
                    Email = user.Email,
                    Claims = claims.Select(c => new UsuarioClaim 
                    { 
                        Type = c.Type, 
                        Value = c.Value 
                    })
                }
            };

            return response;
        }

        // Converte data em formato Unix Epoch para ser integrada ao JWT
        private static long ToUnixEpochDate(DateTime date) => (long)Math.Round((date.ToUniversalTime() - new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero)).TotalSeconds);

        private static string EpochToString(int epoch) => new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(epoch).ToShortDateString();
    }
}