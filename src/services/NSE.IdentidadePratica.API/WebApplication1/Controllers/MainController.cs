using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace WebApplication1.Controllers
{
    [ApiController]
    public abstract class MainController : Controller
    {
        protected ICollection<string> Erros = new List<string>();
        protected ActionResult CustomResponse(object result = null)
        {
            if (OperacaoValida())
            {
                return Ok(result);
            }

            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]> 
            {
                { "Mensagens", Erros.ToArray() } 
            }));
        }

        // Coleta erros da propriedade ModelState, se houver.
        protected ActionResult CustomResponse(ModelStateDictionary modelState)
        {
            var erros = modelState.Values.SelectMany(e => e.Errors);
            foreach (var erro in erros)
            {
                AdicionarErroProcessamento(erro.ErrorMessage);
            }

            return CustomResponse();
        }

        protected bool OperacaoValida()
        {
            return !Erros.Any(); // retorna true se não houverem erros
        }

        protected void AdicionarErroProcessamento(string erro)
        {
            Erros.Add(erro); // Adiciona os erros encontrados na lista de Erros
        }

        protected void LimparErrosProcessamento()
        {
            Erros.Clear(); // limpa a lista de Erros
        }
    }
}