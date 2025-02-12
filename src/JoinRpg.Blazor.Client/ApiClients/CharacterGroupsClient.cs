using System.Net.Http.Json;
using JoinRpg.Web.CharacterGroups;

namespace JoinRpg.Blazor.Client.ApiClients;

public class CharacterGroupsClient : ICharacterGroupsClient
{
    private readonly HttpClient httpClient;
    private readonly ILogger<CharacterGroupsClient> logger;
    private readonly CsrfTokenProvider csrfTokenProvider;

    public CharacterGroupsClient(HttpClient httpClient, ILogger<CharacterGroupsClient> logger, CsrfTokenProvider csrfTokenProvider)
    {
        this.httpClient = httpClient;
        this.logger = logger;
        this.csrfTokenProvider = csrfTokenProvider;
    }

    public async Task<List<CharacterGroupDto>> GetCharacterGroups(int projectId)
    {
        return await httpClient.GetFromJsonAsync<List<CharacterGroupDto>>(
            $"/webapi/character-groups/GetCharacterGroups?projectId={projectId}")
            ?? throw new Exception("Couldn't get result from server");
    }

}
