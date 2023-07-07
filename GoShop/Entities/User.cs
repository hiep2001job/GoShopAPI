using Microsoft.AspNetCore.Identity;

namespace GoShop.Entities
{
    public class User:IdentityUser<int>
    {
        public UserAddress Address { get; set; }

        public string? RefreshToken { get; set; }
        public DateTime? TokenCreated { get; set; }
        public DateTime? TokenExpires { get; set; }
    }
}
