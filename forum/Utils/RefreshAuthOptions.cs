using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace forum.Utils
{
    public class RefreshAuthOptions
    {
        public const string ISSUER = "ForumServer";
        public const string AUDIENCE = "ForumReact";
        const string KEY = "zafagkflkfasfmnbasfhasfjhal";
        public const int LIFETIME = 1000;

        public static SymmetricSecurityKey GetSymmeetricSecurityKey()
        {
            return new SymmetricSecurityKey(Encoding.ASCII.GetBytes(KEY));
        }
    }
}
