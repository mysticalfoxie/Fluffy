using Newtonsoft.Json;

namespace Fluffy.Models;

public class GuildConfig
{
    public ulong Guild { get; set; }
    public ulong RoleChannel { get; set; }
    
    // When null send it, otherwise use it
    public ulong? FoxTypeMessage { get; set; }
    public ulong? PronounsMessage { get; set; }
    public ulong? NsfwMessage { get; set; }
    
    public ulong FoxRoleHeaderRoleId { get; set; }
    
    public ulong AdminFoxRoleId { get; set; }
    public ulong SupporterFoxRoleId { get; set; }

    public ulong FoxInfoHeaderRoleId { get; set; }
    
    public ulong RedFoxRoleId { get; set; }
    public string RedFoxRoleEmote { get; set; }
    public ulong GrayFoxRoleId { get; set; }
    public string GrayFoxRoleEmote { get; set; }
    public ulong SilverFoxRoleId { get; set; }
    public string SilverFoxRoleEmote { get; set; }
    public ulong FennecFoxRoleId { get; set; }
    public string FennecFoxRoleEmote { get; set; }
    public ulong ArcticFoxRoleId { get; set; }
    public string ArcticFoxRoleEmote { get; set; }
    public ulong UnknownFoxRoleId { get; set; }
    
    public ulong HumanRoleId { get; set; }
    public string HumanRoleEmote { get; set; }
    public ulong MemberRoleId { get; set; }
    
    public ulong FemalePronounsRoleId { get; set; }
    public string FemalePronounsRoleEmote { get; set; }
    public ulong MalePronounsRoleId { get; set; }
    public string MalePronounsRoleEmote { get; set; }
    public ulong DiversPronounsRoleId { get; set; }
    public string DiversPronounsRoleEmote { get; set; }
    public ulong UnknownPronounsRoleId { get; set; }
    
    public ulong NaughtyFoxRoleId { get; set; }
    public string NaughtyFoxRoleEmote { get; set; }

    public ulong ErrorChannel { get; set; }
    
    public async Task Update(Action<GuildConfig> action)
    {
        action(this);
        var json = JsonConvert.SerializeObject(this, Formatting.Indented);
        await File.WriteAllTextAsync("guild.json", json);
    }
}