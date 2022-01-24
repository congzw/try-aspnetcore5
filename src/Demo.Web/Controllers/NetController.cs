using Demo.Web.Common;
using Microsoft.AspNetCore.Mvc;

namespace Demo.Web.Controllers
{
    [ApiController]
    public class NetController : ControllerBase
    {
        [Route("/api/net/info")]
        [HttpGet]
        public NetInfo GetNetInfo()
        {
            var myIpHelper = MyIpHelper.Instance;
            return myIpHelper.GetNetInfo(this.HttpContext.Connection);
        }
    }
}
