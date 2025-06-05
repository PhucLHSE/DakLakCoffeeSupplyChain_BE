using DakLakCoffeeSupplyChain.Services.Base;
using DakLakCoffeeSupplyChain.Services.IServices;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace DakLakCoffeeSupplyChain.APIService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserAccountsController : ControllerBase
    {
        private readonly IUserAccountService _userAccountService;

        public UserAccountsController(IUserAccountService userAccountService)
            => _userAccountService = userAccountService;

        // GET: api/<UserAccountsController>
        [HttpGet]
        public async Task<IServiceResult> GetAllUserAccountsAsync()
        {
            return await _userAccountService.GetAll();
        }

        // GET api/<UserAccountsController>/{userId}
        [HttpGet("{userId}")]
        public async Task<IServiceResult> GetById(Guid userId)
        {
            return await _userAccountService.GetById(userId);
        }
    }
}
