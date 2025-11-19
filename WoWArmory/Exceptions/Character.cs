namespace WoWArmory.Exceptions;

public class Character : Exception
{
    public Character(string characterName, string realmName, Exception innerException) : base(
        $"Error for {characterName} - {realmName}", innerException)
    {
        CharacterName = characterName;
        RealmName = realmName;
    }

    public string CharacterName { get; set; }
    public string RealmName { get; set; }
}