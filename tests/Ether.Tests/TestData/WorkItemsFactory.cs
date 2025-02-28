﻿using System;
using System.Collections.Generic;
using System.Linq;
using Ether.Contracts.Interfaces;
using Ether.Contracts.Types;
using Ether.Tests.Extensions;
using Ether.ViewModels;
using Ether.Vsts;
using Ether.Vsts.Types;

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
            workItem.Fields.Add(Constants.WorkItemTitleField, $"{id} - {type}");

            return new WorkItemTestData
            {
                WorkItem = workItem,
                Resolutions = new List<IWorkItemEvent>(),
                Type = type
            };
        }

        public static WorkItemTestData WithNormalLifecycle(
            this WorkItemTestData data,
            TeamMemberViewModel resolvedBy,
            int daysActive,
            TeamMemberViewModel activatedBy = null,
            TeamMemberViewModel assignedTo = null,
            Action<UpdateBuilder, DateTime> onAfterActivation = null)
        {
            var activationDate = DateTime.Today.GetMondayOfCurrentWeek();
            var resolutionDate = activationDate.AddBusinessDays(daysActive);

            var updatesBuilder = UpdateBuilder.Create()
                .New();

            assignedTo = assignedTo ?? resolvedBy;
            updatesBuilder.Then().With(Constants.WorkItemAssignedToField, assignedTo.Email, string.Empty);
            updatesBuilder.Then().Activated(by: activatedBy ?? resolvedBy).On(activationDate);

            onAfterActivation?.Invoke(updatesBuilder, activationDate);

            if (data.Type == Constants.WorkItemTypeBug)
            {
                updatesBuilder.Then().Resolved(resolvedBy).On(resolutionDate);
            }
            else if (data.Type == Constants.WorkItemTypeTask)
            {
                updatesBuilder.Then().ClosedFromActive(resolvedBy).On(resolutionDate);
            }

            data.WorkItem.Updates = updatesBuilder.Build();

            var resolution = new WorkItemResolvedEvent(
                new VstsWorkItem(data.WorkItem),
                DateTime.UtcNow,
                new UserReference { Email = resolvedBy.Email, Title = resolvedBy.DisplayName });

            data.Resolutions.Add(resolution);
            data.ExpectedDuration = daysActive;

            return data;
        }

        public static WorkItemTestData WithResolvedWithoutActivation(
            this WorkItemTestData data,
            int resolovedAfterDays,
            TeamMemberViewModel resolvedBy)
        {
            var createdOn = DateTime.Today.GetMondayOfCurrentWeek();
            var resolutionDate = createdOn.AddDays(resolovedAfterDays);

            var updatesBuilder = UpdateBuilder.Create()
                .New().On(createdOn);

            if (data.Type == Constants.WorkItemTypeBug)
            {
                updatesBuilder.Then().Resolved(resolvedBy).On(resolutionDate);
            }
            else if (data.Type == Constants.WorkItemTypeTask)
            {
                updatesBuilder.Then().ClosedFromActive(resolvedBy).On(resolutionDate);
            }

            data.WorkItem.Updates = updatesBuilder.Build();

            var resolution = new WorkItemClosedEvent(
                new VstsWorkItem(data.WorkItem),
                DateTime.UtcNow,
                new UserReference { Email = resolvedBy.Email, Title = resolvedBy.DisplayName });

            data.Resolutions.Add(resolution);
            data.ExpectedDuration = 1;

            return data;
        }

        public static WorkItemTestData WithActiveWorkItem(this WorkItemTestData data, int daysActive, TeamMemberViewModel activatedBy = null)
        {
            var activationDate = GetActivationDate(daysActive);

            data.WorkItem.Updates = UpdateBuilder.Create()
                .New()
                .Then().Activated().On(activationDate)
                .Build();

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

        public static WorkItemTestData WithNoUpdates(this WorkItemTestData data)
        {
            data.WorkItem.Updates = UpdateBuilder.Create().Build();
            return data;
        }

        private static DateTime GetActivationDate(int daysActive)
        {
            var numberOfWeeks = daysActive < 5 ? 0 : Math.Floor(daysActive / 5.0D);
            return DateTime.Today.AddDays(-(daysActive + (numberOfWeeks * 2)));
        }
    }
}
