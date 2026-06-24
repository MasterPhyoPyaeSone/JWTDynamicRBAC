using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace JWTDynamicRBAC.Blazor.Auth
{
    public class DynamicPolicyProvider : DefaultAuthorizationPolicyProvider
    {
        public DynamicPolicyProvider(IOptions<AuthorizationOptions> options) : base(options) { }

        public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
        {
            var policy = await base.GetPolicyAsync(policyName);
            if (policy != null) return policy;

            return new AuthorizationPolicyBuilder()
                .RequireClaim("Permission", policyName)
                .Build();
        }
    }
}