﻿using System;
using System.Collections.Generic;
using System.Linq;
using Ether.Contracts.Types;
using Ether.ViewModels;
using Ether.Vsts;

namespace Ether.Tests.TestData
{
    public static class WorkItemsFactory
    {
        private static Random _random = new Random();

        private static int NextId => _random.Next(1, int.MaxValue);

        public static WorkItemTestData CreateBug() => Create(Constants.WorkItemTypeBug);

        public static WorkItemTestData CreateTask() => Create(Constants.WorkItemTypeTask);

        public static WorkItemTestData Create(string type)
        {
            var id = NextId;
            var workItem = new WorkItemViewModel { Fields = new Dictionary<string, string>() };
            workItem.WorkItemId = id;
            workItem.Fields.Add(Constants.WorkItemTypeField, type);

            return new WorkItemTestData
            {
                WorkItem = workItem,
                Resolutions = new List<WorkItemResolution>(),
                Type = type
            };
        }

        public static WorkItemTestData WithNormalLifecycle(this WorkItemTestData data, TeamMemberViewModel resolvedBy, int daysActive)
        {
            var dayOfWeek = DateTime.Today.DayOfWeek;
            var activationDate = DateTime.Today.AddDays(-((int)dayOfWeek - 1));
            var numberOfWeeks = daysActive < 5 ? 0 : Math.Floor(daysActive / 5.0D);
            var resolutionDate = activationDate.AddDays(daysActive + (numberOfWeeks * 2));

            var updatesBuilder = UpdateBuilder.Create()
                .New()
                .Then().Activated().On(activationDate);

            if (data.Type == Constants.WorkItemTypeBug)
            {
                updatesBuilder.Then().Resolved(resolvedBy).On(resolutionDate);
            }
            else if (data.Type == Constants.WorkItemTypeTask)
            {
                updatesBuilder.Then().ClosedFromActive(resolvedBy).On(resolutionDate);
            }

            data.WorkItem.Updates = updatesBuilder.Build();

            var resolution = new WorkItemResolution(
                data.WorkItem.WorkItemId,
                $"Work item #{data.WorkItem.WorkItemId}",
                data.Type,
                Constants.WorkItemStateResolved, "Fixed",
                DateTime.UtcNow,
                resolvedBy.Email,
                resolvedBy.DisplayName);

            data.Resolutions.Add(resolution);
            data.ExpectedDuration = daysActive;

            return data;
        }

        public static WorkItemTestData WithETA(this WorkItemTestData data, int original, int remaining, int completed)
        {
            if (original > 0)
            {
                data.WorkItem.Fields.Add(Constants.OriginalEstimateField, original.ToString());
            }

            if (remaining > 0)
            {
                data.WorkItem.Fields.Add(Constants.RemainingWorkField, remaining.ToString());
            }

            if (completed > 0)
            {
                data.WorkItem.Fields.Add(Constants.CompletedWorkField, completed.ToString());
            }

            data.ExpectedOriginalEstimate = original;
            data.ExpectedEstimatedToComplete = remaining + completed;

            return data;
        }
    }
}
