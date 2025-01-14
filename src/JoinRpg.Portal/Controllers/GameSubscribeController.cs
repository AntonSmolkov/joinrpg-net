using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Portal.Infrastructure;
using JoinRpg.Portal.Infrastructure.Authentication;
using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Projects;
using JoinRpg.Services.Interfaces.Subscribe;
using JoinRpg.Web.GameSubscribe;
using JoinRpg.Web.Models;
using JoinRpg.Web.Models.Subscribe;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers;

[RequireMaster]
[Route("{projectId}/subscribe/[action]")]
public class GameSubscribeController : Common.ControllerGameBase
{
    private readonly IUserSubscribeRepository userSubscribeRepository;
    private readonly IUserRepository userRepository;
    private readonly IUriService uriService;
    private readonly IGameSubscribeClient subscribeClient;
    private readonly IGameSubscribeService gameSubscribeService;

    public GameSubscribeController(
        IUserSubscribeRepository userSubscribeRepository,
        IUserRepository userRepository,
        IUriService uriService,
        IProjectRepository projectRepository,
        IProjectService projectService,
        IGameSubscribeClient subscribeClient,
        IGameSubscribeService gameSubscribeService
        )
        : base(projectRepository, projectService, userRepository)
    {
        this.userSubscribeRepository = userSubscribeRepository;
        this.userRepository = userRepository;
        this.uriService = uriService;
        this.subscribeClient = subscribeClient;
        this.gameSubscribeService = gameSubscribeService;
    }

    [HttpGet("{masterId}")]
    public async Task<ActionResult> ByMaster(int projectId, int masterId)
    {
        var subscribeViewModel = await subscribeClient.GetSubscribeForMaster(projectId, masterId);

        var user = await UserRepository.GetById(masterId);
        var currentUser = await userRepository.GetById(User.GetUserIdOrDefault()!.Value);


        return View(
            new SubscribeByMasterPageViewModel(
                new UserProfileDetailsViewModel(user, currentUser),
                subscribeViewModel));
    }


    [HttpGet]
    public async Task<ActionResult> EditRedirect(int projectId, int subscriptionId)
    {
        var subscribe = await userSubscribeRepository.LoadSubscriptionById(projectId, subscriptionId);
        var link = subscribe.ToSubscribeTargetLink();
        return link.LinkType switch
        {
            LinkType.ResultCharacterGroup => RedirectToAction("EditForGroup", new { projectId, characterGroupId = link.Identification }),
            LinkType.Claim => Redirect(uriService.GetUri(link).AbsoluteUri),
            _ => Redirect(uriService.GetUri(link).AbsoluteUri),
        };
    }

    [HttpGet("{characterGroupId}")]
    public async Task<ActionResult> EditForGroup(int projectId, int characterGroupId)
    {
        var group = await ProjectRepository.LoadGroupWithTreeAsync(projectId, characterGroupId);
        if (group == null)
        {
            return NotFound();
        }

        var user = await UserRepository.GetWithSubscribe(CurrentUserId);

        return View(new SubscribeSettingsViewModel(user, group));
    }

    [HttpPost("{characterGroupId}")]
    public async Task<ActionResult> EditForGroup(SubscribeSettingsViewModel viewModel)
    {
        var group = await ProjectRepository.GetGroupAsync(viewModel.ProjectId, viewModel.CharacterGroupId);

        if (group == null)
        {
            return NotFound();
        }

        var user = await UserRepository.GetWithSubscribe(CurrentUserId);

        var serverModel = new SubscribeSettingsViewModel(user, group);

        _ = serverModel.Options.AssignFrom(viewModel.Options);

        try
        {
            await
                gameSubscribeService.UpdateSubscribeForGroup(new SubscribeForGroupRequest
                {
                    CharacterGroupId = group.CharacterGroupId,
                    ProjectId = group.ProjectId,
                }.AssignFrom(serverModel.GetOptionsToSubscribeDirectly()));

            return RedirectToIndex(group.Project);
        }
        catch (Exception e)
        {
            ModelState.AddException(e);
            return View(serverModel);
        }

    }

}
