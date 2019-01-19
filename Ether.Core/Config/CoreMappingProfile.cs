﻿using Ether.Contracts.Dto;
using Ether.Contracts.Dto.Reports;
using Ether.Core.Types.Commands;
using Ether.ViewModels;

namespace Ether.Core.Config
{
    public class CoreMappingProfile : AutoMapper.Profile
    {
        public CoreMappingProfile()
        {
            CreateMap<Identity, IdentityViewModel>();
            CreateMap<IdentityViewModel, Identity>();
            CreateMap<PullRequestsReport, PullRequestReportViewModel>();
            CreateMap<ReportJobState, JobLog>()
                .ForMember(l => l.Id, o => o.MapFrom(m => m.JobId));
            CreateMap<JobLog, JobLogViewModel>();
        }
    }
}
