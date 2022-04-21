using JoinRpg.DataModel;
using JoinRpg.Web.Models.Characters;
using JoinRpg.Web.Models.ClaimList;

namespace JoinRpg.Web.Models;

public class MenuViewModelBase
{
    public int ProjectId { get; set; }
    public string ProjectName { get; set; }
    public bool IsActive { get; set; }
    public bool IsAcceptingClaims { get; set; }
    public bool EnableAccommodation { get; set; }
    public int? RootGroupId { get; set; }
    public IEnumerable<CharacterGroupLinkViewModel> BigGroups { get; set; }
    public bool IsAdmin { get; set; }
    public bool ShowSchedule { get; set; }
}

public class PlayerMenuViewModel : MenuViewModelBase
{
    public ICollection<ClaimShortListItemViewModel> Claims { get; set; }
    public bool PlotPublished { get; set; }
}

public class MasterMenuViewModel : MenuViewModelBase
{
    public ProjectAcl AccessToProject { get; set; }
    public bool CheckInModuleEnabled { get; set; }
}
