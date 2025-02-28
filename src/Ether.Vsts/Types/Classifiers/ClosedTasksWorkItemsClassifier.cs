﻿using System;
using System.Collections.Generic;
using System.Linq;
using Ether.Contracts.Interfaces;
using Ether.Contracts.Types;
using Ether.ViewModels;
using static Ether.Vsts.Constants;

namespace Ether.Vsts.Types.Classifiers
{
    public class ClosedTasksWorkItemsClassifier : VstsBaseWorkItemsClassifier
    {
        protected override IEnumerable<IWorkItemEvent> ClassifyInternal(WorkItemResolutionRequest request)
        {
            var resolutionUpdate = request.WorkItem.Updates.LastOrDefault(u => WasClosedByTeamMember(u, request));
            var wasEverResolved = request.WorkItem.Updates.Any(u => u[WorkItemStateField].NewValue == WorkItemStateResolved);
            if (resolutionUpdate == null || wasEverResolved)
            {
                return Enumerable.Empty<IWorkItemEvent>();
            }

            var reason = resolutionUpdate[WorkItemReasonField].NewValue;
            var assignedToMember = request.Team.SingleOrDefault(m => !resolutionUpdate[WorkItemAssignedToField].IsEmpty() &&
                !string.IsNullOrEmpty(resolutionUpdate[WorkItemAssignedToField].OldValue) &&
                resolutionUpdate[WorkItemAssignedToField].OldValue.Contains(m.Email));
            if (assignedToMember == null)
            {
                assignedToMember = request.Team.SingleOrDefault(t => (!string.IsNullOrEmpty(request.WorkItem[WorkItemAssignedToField]) && request.WorkItem[WorkItemAssignedToField].Contains(t.Email)));
            }

            var closedByMemeber = request.Team.SingleOrDefault(m => resolutionUpdate[WorkItemClosedByField].NewValue.Contains(m.Email));
            if (assignedToMember != null)
            {
                closedByMemeber = assignedToMember;
            }

            var closedDate = DateTime.Parse(resolutionUpdate[WorkItemChangedDateField].NewValue);
            var closedBy = new UserReference { Email = closedByMemeber.Email, Title = closedByMemeber.DisplayName };

            return new[]
            {
                new WorkItemClosedEvent(new VstsWorkItem(request.WorkItem), closedDate, closedBy)
            };
        }

        protected override bool IsSupported(WorkItemViewModel item)
        {
            var type = item[WorkItemTypeField];
            return string.Equals(type, WorkItemTypeTask, StringComparison.OrdinalIgnoreCase);
        }

        private bool WasClosedByTeamMember(WorkItemUpdateViewModel update, WorkItemResolutionRequest request)
        {
            var assignedTo = update[Constants.WorkItemAssignedToField].IsEmpty() ? request.WorkItem[Constants.WorkItemAssignedToField] : update[Constants.WorkItemAssignedToField].OldValue;
            var closedBy = update[WorkItemClosedByField].NewValue;
            return update[WorkItemStateField].NewValue == WorkItemStateClosed
                && update[WorkItemStateField].OldValue != WorkItemStateResolved
                && request.Team.Any(t => (!string.IsNullOrEmpty(assignedTo) && assignedTo.Contains(t.Email)) || (!string.IsNullOrEmpty(closedBy) && closedBy.Contains(t.Email)));
        }
    }
}
