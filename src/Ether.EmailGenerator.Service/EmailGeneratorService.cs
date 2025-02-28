﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading.Tasks;
using Ether.EmailGenerator.Outlook;
using Google.Protobuf;
using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace Ether.EmailGenerator
{
    public class EmailGeneratorService : EmailGenerator.EmailGeneratorBase
    {
        private readonly ILogger<EmailGeneratorService> _logger;

        public EmailGeneratorService(ILogger<EmailGeneratorService> logger)
        {
            _logger = logger;
        }

        // TODO: Async?
        public override Task<EmailReply> Generate(EmailRequest request, ServerCallContext context)
        {
            _logger.LogInformation("Generating report for {Id}", request.Id);

            try
            {
                var subject = ApplyCommonPlaceholders(request.Template.Subject, request);
                var body = ApplyCommonPlaceholders(request.Template.Body, request);
                body = ApplyBodyPlaceholders(body, request);
                body = body.Replace("style=\"\"", string.Empty);

                var msg = OutlookMsgFile.New(request.Id);
                msg.SetSubjectAndBody(subject, body);
                var bytes = File.ReadAllBytes(msg.FilePath);

                return Task.FromResult(new EmailReply { File = ByteString.CopyFrom(bytes) });
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error while generating report.");
                ExceptionDispatchInfo.Capture(ex).Throw();
                throw;
            }
        }

        private string ApplyCommonPlaceholders(string value, EmailRequest request)
        {
            return value
                .Replace("{Profile}", request.Name)
                .Replace("{Date}", DateTime.Now.ToString("d"));
        }

        private string ApplyBodyPlaceholders(string value, EmailRequest request)
        {
            var resolvedTable = CreateTable(request.Report.Completed, "#70AD47");
            var codeReviewTable = CreateTable(request.Report.Inreview, "#FFC000");
            var activeTable = CreateTable(request.Report.Active, "#5B9BD5");
            var points = CreatePoints(request.Points);
            var createTeam = CreateTeamTable(request.Attendance);
            var teamCount = GetTeamCount(request.Attendance);

            return value
                 .Replace("{ResolvedItems}", resolvedTable)
                 .Replace("{InReviewItems}", codeReviewTable)
                 .Replace("{ActiveItems}", activeTable)
                 .Replace("{ResolvedCount}", request.Report.Completed.Count.ToString())
                 .Replace("{InReviewCount}", request.Report.Inreview.Count.ToString())
                 .Replace("{ActiveCount}", request.Report.Active.Count.ToString())
                 .Replace("{TeamCount}", teamCount.ToString())
                 .Replace("{Points}", points)
                .Replace("{Team}", createTeam);
        }

        private string CreateTable(IEnumerable<WorkItem> items, string color)
        {
            var table = new StringBuilder();
            table.Append($"<table style=\"border: 1px solid {color};border-collapse: collapse;\">");
            table.Append($"<thead style=\"background: {color};color: white;\">");
            table.Append("<tr>");
            table.Append($"<th style=\"border: 1px solid {color};border-right: 1px solid white;\">Id</th>");
            table.Append($"<th style=\"border: 1px solid {color};border-right: 1px solid white;\">Title</th>");
            table.Append($"<th style=\"border: 1px solid {color};border-right: 1px solid white;\">Type</th>");
            table.Append($"<th style=\"border: 1px solid {color};border-right: 1px solid white;\">Estimated (Days)</th>");
            table.Append($"<th style=\"border: 1px solid {color};border-right: 1px solid white;\">Time Spent (Days)</th>");
            table.Append("</tr>");
            table.Append("</thead>");
            table.Append("<tbody>");
            foreach (var item in items)
            {
                table.Append($"<tr>");
                table.Append($"<td style=\"border: solid {color} 1.0pt;border-right: none;margin-left:5pt;margin-right:5pt;\"><a href=\"{item.Url}\">{item.Id}</a></td>");
                table.Append($"<td style=\"border: solid {color} 1.0pt;border-right: none;margin-left:5pt;margin-right:5pt;\">{item.Title}</td>");
                table.Append($"<td style=\"border: solid {color} 1.0pt;border-right: none;margin-left:5pt;margin-right:5pt;\">{item.Type}</td>");
                table.Append($"<td style=\"border: solid {color} 1.0pt;border-right: none;margin-left:5pt;margin-right:5pt;\">{item.Estimated}</td>");
                table.Append($"<td style=\"border: solid {color} 1.0pt;margin-left:5pt;margin-right:5pt;\">{item.Spent}</td>");
                table.Append("</tr>");
            }

            table.Append("</tbody>");
            table.Append("</table>");

            return table.ToString();
        }

        private string CreateTeamTable(IEnumerable<TeamAttendance> attendance)
        {
            var table = new StringBuilder();
            table.Append("<table>");
            table.Append("<thead>");
            table.Append("<tr>");
            table.Append("<th style=\"border: 1px solid black;\">Member</th>");
            table.Append("<th style=\"border: 1px solid black;\">Monday</th>");
            table.Append("<th style=\"border: 1px solid black;\">Tuesday</th>");
            table.Append("<th style=\"border: 1px solid black;\">Wednesday</th>");
            table.Append("<th style=\"border: 1px solid black;\">Thursday</th>");
            table.Append("<th style=\"border: 1px solid black;\">Friday</th>");
            table.Append("<th style=\"border: 1px solid black;\">Saturday</th>");
            table.Append("<th style=\"border: 1px solid black;\">Sunday</th>");
            table.Append("</tr>");
            table.Append("</thead>");
            table.Append("<tbody>");
            foreach (var memberAttendance in attendance)
            {
                table.Append("<tr>");
                table.Append($"<td style=\"border: 1px solid black;\">{memberAttendance.Name}</td>");
                table.Append($"<td style=\"background:{GetColor(memberAttendance.Attendance[0])}; border: 1px solid black;width: 60pt;\">{(memberAttendance.Attendance[0] ? "V" : "OOF")}</td>");
                table.Append($"<td style=\"background:{GetColor(memberAttendance.Attendance[1])}; border: 1px solid black;width: 60pt;\">{(memberAttendance.Attendance[1] ? "V" : "OOF")}</td>");
                table.Append($"<td style=\"background:{GetColor(memberAttendance.Attendance[2])}; border: 1px solid black;width: 60pt;\">{(memberAttendance.Attendance[2] ? "V" : "OOF")}</td>");
                table.Append($"<td style=\"background:{GetColor(memberAttendance.Attendance[3])}; border: 1px solid black;width: 60pt;\">{(memberAttendance.Attendance[3] ? "V" : "OOF")}</td>");
                table.Append($"<td style=\"background:{GetColor(memberAttendance.Attendance[4])}; border: 1px solid black;width: 60pt;\">{(memberAttendance.Attendance[4] ? "V" : "OOF")}</td>");
                table.Append("<td style=\"background:#A6A6A6; border: 1px solid black;width: 60pt;\">OOF</td>");
                table.Append("<td style=\"background:#A6A6A6; border: 1px solid black;width: 60pt;\">OOF</td>");
                table.Append("</tr>");
            }

            table.Append("</tbody>");

            table.Append("</table>");

            return table.ToString();
        }

        private int GetTeamCount(IEnumerable<TeamAttendance> attendance)
        {
            return (int)Math.Round(attendance.Sum(t => t.Attendance.Count(a => a)) / 5.0d, MidpointRounding.AwayFromZero);
        }

        private string GetColor(bool isAttending)
        {
            return isAttending ? "#A8D08D" : "#FFE599";
        }

        private string CreatePoints(string points)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("<ul>");
            foreach (var line in points.Split('\n'))
            {
                sb.Append($"<li>{line}</li>");
            }

            sb.Append("</ul>");

            return sb.ToString();
        }
    }
}
