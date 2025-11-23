using Microsoft.EntityFrameworkCore;
using WoWArmory.Context;
using WoWArmory.Contracts.Models;
using WoWArmory.Models;

namespace WoWArmory.Services;

public class UserService(ArmoryDbContext dbContext) : BaseService(dbContext)
{
    public bool VerifyUser(string userName, string password)
    {
        var user = GetUser(userName);
        if (user == null)
            return false;

        // var hasher = new  PasswordHasher<User>();
        // var hashedPassword = hasher.HashPassword(user, password);
        //
        // if (hasher.VerifyHashedPassword(user, hashedPassword, user.Password) == PasswordVerificationResult.Failed)
        //     return false;

        return true;
    }

    public User? GetUser(string name)
    {
        return DbContext.Users.FirstOrDefault(u => u.Name.ToLower() == name.ToLower());
    }
    
    public async Task<User?> GetUserAsync(Guid id)
    {
        return await DbContext.Users.FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task UpdateUserAsync(Guid userId, User user, string hashedPassword)
    {
        var dbUser = await GetUserAsync(userId);
        
        if (dbUser != null)
        {
            dbUser.FirstName = user.FirstName;
            dbUser.LastName = user.LastName;
            dbUser.Email = user.Email;
            if (!string.IsNullOrEmpty(user.Password) && dbUser.Password != user.Password) 
                dbUser.Password = hashedPassword;
            dbUser.MainCharacterId = user.MainCharacterId;
            dbUser.MainGuildId = user.MainGuildId;
            dbUser.MainRaidId = user.MainRaidId;
            await SaveChangesAsync();
        }
    }

    public async Task RefreshWoWAccessToken(Guid userId)
    {
        var dbUser = await GetUserAsync(userId);
        if (dbUser == null) return;

        var token = await GetOAuthTokenAsync();
        if (token == null) return;
        
        dbUser.WoWAccessToken = token.AccessToken;
    }

    protected override void UpdateEntity(QueueEntity entity, bool saveChanges)
    {
        
    }
}