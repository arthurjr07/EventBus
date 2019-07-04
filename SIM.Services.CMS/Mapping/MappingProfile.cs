using AutoMapper;
using SIM.BuildingBlocks.EventBus.Messages;
using SIM.Services.CMS.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SIM.Services.CMS.Mapping
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // Add as many of these lines as you need to map your objects
            CreateMap<RegisterNewCustomerEvent, NewCustomer>();
        }
    }
}
