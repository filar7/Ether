﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Ether.Contracts.Interfaces.CQS;
using Ether.Core.Extensions;
using Ether.Core.Types;
using Ether.Core.Types.Commands;
using Ether.Core.Types.Queries;
using Ether.Types.Excel;
using Ether.ViewModels;
using Ether.Vsts.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Ether.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("[controller]")]
    [Produces("application/json")]
    public class ReportController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IEnumerable<ReporterDescriptor> _reporters;
        private readonly IMapper _mapper;
        private readonly ILogger<ReportController> _logger;

        public ReportController(IMediator mediator, IEnumerable<ReporterDescriptor> reporters, IMapper mapper, ILogger<ReportController> logger)
        {
            _mediator = mediator;
            _reporters = reporters;
            _mapper = mapper;
            _logger = logger;
        }

        [HttpPost]
        [Route(nameof(Generate))]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> Generate(GenerateReportViewModel requestModel)
        {
            var id = Guid.Empty;
            var reporter = _reporters.FirstOrDefault(r => string.Equals(r.UniqueName, requestModel.ReportType, StringComparison.OrdinalIgnoreCase));
            if (reporter == null)
            {
                return BadRequest($"Reporter of type {requestModel.ReportType} is not supported.");
            }

            id = await GenerateReport(reporter.CommandType, requestModel);
            return Ok(id);
        }

        [HttpGet]
        [Route(nameof(Types))]
        [ProducesResponseType(200)]
        public IActionResult Types()
        {
            var model = _mapper.MapCollection<ReporterDescriptorViewModel>(_reporters);
            return Ok(model);
        }

        [HttpGet]
        [Route(nameof(GetAll))]
        [ProducesResponseType(200)]
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
            var report = await _mediator.Request<GetReportById, ReportViewModel>(new GetReportById(id));
            return Ok(report);
        }

        [HttpGet] // TODO: POST?
        [Route(nameof(GenerateExcel))]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GenerateExcel(Guid id)
        {
            var report = await _mediator.Request<GetReportById, ReportViewModel>(new GetReportById(id));
            ReportToExcelConverter excelConverter = null;

            // TODO: DI
            switch (report.ReportType)
            {
                case "PullRequestsReport":
                    excelConverter = new PullRequestsReportToExcelConverter();
                    break;
                case "WorkitemsReporter":
                    excelConverter = new WorkItemsReportToExcelConverter();
                    break;
                default:
                    throw new NotSupportedException($"Report of type '{report.ReportType}' is not supported.");
            }

            return Ok(excelConverter.Convert(report));
        }

        [HttpPost]
        [Route(nameof(GenerateEmail))]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GenerateEmail(GenerateEmailViewModel model)
        {
            try
            {
                var emailGenerator = (Types.Email.EmailGenerator)HttpContext.RequestServices.GetService(typeof(Types.Email.EmailGenerator));
                var report = await _mediator.Request<GetReportById, ReportViewModel>(new GetReportById(model.Id));
                var profile = await _mediator.Request<GetProfileById, ProfileViewModel>(new GetProfileById(report.ProfileId));
                var vstsConfig = await _mediator.Request<GetVstsDataSourceConfiguration, VstsDataSourceViewModel>(new GetVstsDataSourceConfiguration());
                if (report.ReportType == "WorkitemsReporter")
                {
                    var email = await emailGenerator.Generate(profile, report as WorkItemsReportViewModel, model, vstsConfig);
                    return Ok(email);
                }

                throw new NotSupportedException($"Report of type '{report.ReportType}' is not supported.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating email for report.");
                throw;
            }
        }

        private Task<Guid> GenerateReport(Type generateCommandType, GenerateReportViewModel requestModel)
        {
            var request = _mapper.Map(requestModel, typeof(GenerateReportViewModel), generateCommandType) as GenerateReportCommand;
            return _mediator.Execute<Guid>(request);
        }
    }
}