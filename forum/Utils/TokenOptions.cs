using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace forum.Utils
{
    public class TokenOptions
    {
        public const string ISSUER = "https://localhost:3001";
        public const string AUDIENCE = "https://localhost:3000";
        const string KEY = "SecretCodeEgorka";
        public const int LIFETIME = 60;

        public static SymmetricSecurityKey GetSymmeetricSecurityKey()
        {
            return new SymmetricSecurityKey(Encoding.ASCII.GetBytes(KEY));
        }
    }
}
