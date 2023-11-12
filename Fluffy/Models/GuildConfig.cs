using Newtonsoft.Json;

namespace Fluffy.Models;

public class GuildConfig
{
    public ulong Guild { get; set; }
    public ulong RoleChannel { get; set; }
    public ulong MemberLogChannel { get; set; }
    
    // When null send it, otherwise use it
    public ulong? FoxTypeMessage { get; set; }
    public ulong? PronounsMessage { get; set; }
    public ulong? NsfwMessage { get; set; }
    public ulong? NsfwRulesMessage { get; set; }
    public ulong? GeneralRulesMessage { get; set; }
    
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
    public ulong NaughtyFoxUnapprovedRoleId { get; set; }
    public string NaughtyFoxRoleEmote { get; set; }
    
    public string PronounsBannerImageUrl { get; set; }
    public string RulesBannerImageUrl { get; set; }
    public string SupportBannerImageUrl { get; set; }
    public string FoxTypeBannerImageUrl { get; set; }
    public string NsfwRulesBannerImageUrl { get; set; }
    public string NsfwSectionBannerImageUrl { get; set; }

    public ulong ErrorChannel { get; set; }
    public ulong GeneralRulesChannel { get; set; }
    public ulong NsfwRulesChannel { get; set; }
    public ulong StrangerRoleId { get; set; }

    public ulong? VoiceHubId { get; set; }
    public ulong VoiceHubCategoryId { get; set; }
    public string VoiceHubCategoryName { get; set; }
    public string VoiceHubName { get; set; }
    public string TempVoiceNamingPattern { get; set; } // @"🔊・「\d+」fox hole from .*"

    public async Task Update(Action<GuildConfig> action)
    {
        action(this);
        var json = JsonConvert.SerializeObject(this, Formatting.Indented);
        await File.WriteAllTextAsync("guild.json", json);
    }
}