using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Stem.Data.Models;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Stem.Data.Repository
{
    public interface ITokenRepository
    {
        string GenerateJwtToken(User user, string role);
        Task<RefreshToken> CreateOrUpdateRefreshTokenAsync(User user);
        Task<string?> GetUserIdByRefreshTokenAsync(int refreshTokenId);
    }
    public class TokenRepository : ITokenRepository
    {
        private readonly int refreshTokenExpirationTime = 60 * 60 * 24 * 7;
        private readonly int accessTokenExpirationTime = 60 * 15;
        private readonly IConfiguration _configuration;
        private readonly STEMKITshopDBContext _dbContext;
        public TokenRepository(IConfiguration configuration, STEMKITshopDBContext dbContext)
        {
            _configuration = configuration;
            _dbContext = dbContext;
        }
        public string GenerateJwtToken(User user, string role)
        {
            var claims = new List<Claim>()
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId),
                new Claim(ClaimTypes.Email, user.Email!),
                new Claim(ClaimTypes.Role, role)
            };
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? throw new ArgumentException("Key cannot be null")));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                _configuration["Jwt:Issuer"],
                _configuration["Jwt:Audience"],
                claims,
                expires: DateTime.Now.AddSeconds(accessTokenExpirationTime),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
        public async Task<RefreshToken> CreateOrUpdateRefreshTokenAsync(User user)
        {
            var rt = await GetRefreshTokenAsync(user.UserId);
            if (rt != null)
            {
                _dbContext.RefreshTokens.Remove(rt);
            }

            var newRt = CreateNewRefreshToken(user);
            await _dbContext.RefreshTokens.AddAsync(newRt);
            await _dbContext.SaveChangesAsync();

            return newRt;
        }

        private RefreshToken CreateNewRefreshToken(User user)
        {
            return new RefreshToken()
            {
                UserId = user.UserId,
                ExperationTime = DateTime.Now.AddSeconds(refreshTokenExpirationTime)
            };
        }

        private async Task<RefreshToken?> GetRefreshTokenAsync(string userId)
        {
            return await _dbContext.RefreshTokens.FirstOrDefaultAsync(rt => rt.UserId == userId);
        }

        public async Task<string?> GetUserIdByRefreshTokenAsync(int refreshTokenId)
        {
            var rt = await _dbContext.RefreshTokens.FirstOrDefaultAsync(rt => rt.Id == refreshTokenId);
            if (rt == null || rt.ExperationTime < DateTime.Now)
            {
                return null;
            }

            return rt.UserId;
        }
    }
}
