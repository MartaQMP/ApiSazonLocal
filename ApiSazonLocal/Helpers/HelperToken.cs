using SazonLocalModels.Dto;
using Newtonsoft.Json;
using System.Security.Claims;

namespace ApiSazonLocal.Helpers
{
    public class HelperToken
    {
        private IHttpContextAccessor accessor;

        public HelperToken(IHttpContextAccessor accessor)
        {
            this.accessor = accessor;
        }

        public UsuarioLogin GetUsuario()
        {
            Claim claim = this.accessor.HttpContext.User.FindFirst(z => z.Type == "UserData");
            string json = claim.Value;
            string jsonEmpleado = HelperCifrado.DescifrarString(json);
            return JsonConvert.DeserializeObject<UsuarioLogin>(jsonEmpleado);
        }
    }
}
