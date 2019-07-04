using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using SIM.BuildingBlocks.EventBus.Messages;
using SIM.Services.CMS.ViewModel;
using SMI.BuildingBlocks.EventBus.Interfaces;

namespace SIM.Services.CMS.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CustomerController : ControllerBase
    {
        private readonly IMapper _mapper;
        private readonly IEventBus _eventbus;
        public CustomerController(IMapper mapper, IEventBus eventbus)
        {
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _eventbus = eventbus ?? throw new ArgumentNullException(nameof(eventbus));
        }

        [HttpGet]
        public ActionResult<IEnumerable<string>> Get()
        {
            return new string[] { "value1", "value2" };
        }
        // POST api/values
        [HttpPost]
        public void Post([FromBody] NewCustomer customer)
        {
            //Do your stuff here
            //Call the service to register the new customer

            //After you have registered the new customer, publish an event to other services
            var message = _mapper.Map<RegisterNewCustomerEvent>(customer);
            _eventbus.Publish(message);
        }
    }
}
