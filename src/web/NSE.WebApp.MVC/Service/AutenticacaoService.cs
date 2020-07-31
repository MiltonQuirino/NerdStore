using NSE.WebApp.MVC.Models;
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace NSE.WebApp.MVC.Service
{
    public class AutenticacaoService : IAutenticacaoService
    {
        //HttpClient: Biblioteca usada para se comunicar com serviços externos como APIs por meio de requisições HTTP.
        private readonly HttpClient _httpClient;
        
        public AutenticacaoService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<UsuarioRespostaLogin> Login(UsuarioLogin usuarioLogin)
        {
            var loginContent = new StringContent(JsonSerializer.Serialize(usuarioLogin),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync("https://localhost:44379/api/identidade/autenticar", loginContent);

            
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true // Necessário habilitar por meio de código pois diferente do Newtonsoft.Json o System.Text.Json não é case insensitive por padrão
            };

            return JsonSerializer.Deserialize<UsuarioRespostaLogin>(await response.Content.ReadAsStringAsync(), options);
        }

        public async Task<UsuarioRespostaLogin> Registro(UsuarioRegistro usuarioRegistro)
        {
            var registerContent = new StringContent(JsonSerializer.Serialize(usuarioRegistro),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync("https://localhost:44379/api/identidade/nova-conta", registerContent);

            return JsonSerializer.Deserialize<UsuarioRespostaLogin>(await response.Content.ReadAsStringAsync());
        }

        // Não se consome serviços externos de APIs de forma sincrona portanto utilizaremos métodos com chamadas assincronas a API.
    }
}
