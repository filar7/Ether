﻿using System;
using System.Threading.Tasks;
using AutoMapper;
using Ether.Contracts.Interfaces.CQS;
using Ether.Core.Types.Commands;
using Ether.Core.Types.Queries;
using Ether.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ether.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class ReportController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IMapper _mapper;

        public ReportController(IMediator mediator, IMapper mapper)
        {
            _mediator = mediator;
            _mapper = mapper;
        }

        [HttpPost]
        [Route(nameof(Generate))]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Generate(GenerateReportViewModel requestModel)
        {
            var id = Guid.Empty;
            switch (requestModel.ReportType)
            {
                case "PullRequestsReport":
                    id = await GenerateReport<GeneratePullRequestsReport>(requestModel);
                    break;
                case "AggregatedWorkitemsETAReport":
                    id = await GenerateReport<GenerateAggregatedWorkitemsETAReport>(requestModel);
                    break;
            }

            return Ok(id);
        }

        [HttpGet]
        [Route(nameof(GetAll))]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetAll()
        {
            var reports = await _mediator.RequestCollection<GetAllReports, ReportViewModel>();
            return Ok(reports);
        }

        [HttpGet]
        [Route(nameof(GetById))]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetById(Guid id)
        {
            var reports = await _mediator.Request<GetReportById, ReportViewModel>(new GetReportById(id));
            return Ok(reports);
        }

        private Task<Guid> GenerateReport<T>(GenerateReportViewModel requestModel)
            where T : GenerateReportCommand
        {
            var request = _mapper.Map<T>(requestModel);
            return _mediator.Execute<T, Guid>(request);
        }
    }
}