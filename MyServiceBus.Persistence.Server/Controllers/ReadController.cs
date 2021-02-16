using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MyServiceBus.Persistence.Domains;
using MyServiceBus.Persistence.Server.Models;

namespace MyServiceBus.Persistence.Server.Controllers
{
    [ApiController]
    public class ReadController : Controller
    {
        
        [HttpGet("read/byId")]
        public async ValueTask<ApiResultContract<MessageRestApiModel>> ByIdAsync([FromQuery]string topicId, [FromQuery]long messageId)
        {
            var (message, _) = await ServiceLocator.MessagesContentReader.TryGetMessageAsync(topicId, messageId, "API");
            
            if (message == null)
                return ApiResultContract<MessageRestApiModel>.CreateFail(ApiResult.RecordNotFound);
            
            return ApiResultContract<MessageRestApiModel>.CreateOk(MessageRestApiModel.Create(message));
        }

        [HttpPost("read/listFromDate")]
        [ProducesResponseType(typeof(ApiResultContract<IEnumerable<MessageRestApiModel>>), 200)]
        [ProducesResponseType(typeof(string), 403)]
        public async ValueTask<IActionResult> ListFromDateAsync(
            [FromQuery] string topicId, [FromQuery] DateTime fromDate, [FromQuery] int maxAmount)
        {
            if (maxAmount > Startup.Settings.MaxResponseRecordsAmount)
                return StatusCode(403, "Maximum amount of records can be less then " +
                                       Startup.Settings.MaxResponseRecordsAmount);

            var messages = await ServiceLocator.MessagesContentReader
                .GetMessagesByDate(topicId, fromDate, maxAmount, "API Request").ToListAsync();
            
            return Json(
                ApiResultContract<IEnumerable<MessageRestApiModel>>.CreateOk(
                    messages.Select(MessageRestApiModel.Create)));
        }

        [HttpPost("read/listFromMessageId")]
        [ProducesResponseType(typeof(ApiResultContract<IEnumerable<MessageRestApiModel>>), 200)]
        [ProducesResponseType(typeof(string), 403)]
        public async ValueTask<IActionResult> ListFromMessageIdAsync(
            [FromQuery] string topicId, [FromQuery]long messageId, [FromQuery] int maxAmount)
        {
            if (maxAmount > Startup.Settings.MaxResponseRecordsAmount)
                return StatusCode(403, "Maximum amount of records can be less then " +
                                  Startup.Settings.MaxResponseRecordsAmount);
            
            var messages = await ServiceLocator.MessagesContentReader.GetMessagesFromMessageId(topicId, messageId, maxAmount, "API Request").ToListAsync();
            return Json(ApiResultContract<IEnumerable<MessageRestApiModel>>.CreateOk(messages.Select(MessageRestApiModel.Create)));
        }
        
    }
}