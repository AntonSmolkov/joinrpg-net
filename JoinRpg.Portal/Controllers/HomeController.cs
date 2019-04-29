using System.Threading.Tasks;
using JoinRpg.Data.Interfaces;
using JoinRpg.WebPortal.Managers;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers
{
    public class HomeController : Common.ControllerBase
    {
        private ProjectListManager ProjectListManager { get; }
        private const int ProjectsOnHomePage = 9;

        public HomeController(
            IUserRepository userRepository,
            ProjectListManager projectListManager) : base(userRepository)
        {
            ProjectListManager = projectListManager;
        }

        public async Task<ActionResult> Index() =>
            View(await ProjectListManager.LoadModel(false, ProjectsOnHomePage));


        public ActionResult About() => View();

        public ActionResult Funding2016() => View();

        public ActionResult HowToHelp() => View();

        public ActionResult FromAllrpgInfo() => View();

        public async Task<ActionResult> BrowseGames() => View(await ProjectListManager.LoadModel());

        public async Task<ActionResult> GameArchive() =>
            View("BrowseGames", await ProjectListManager.LoadModel(true));
    }
}
