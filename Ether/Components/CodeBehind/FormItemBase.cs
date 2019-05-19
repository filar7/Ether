﻿using System;
using System.Linq;
using Ether.Types;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;

namespace Ether.Components.CodeBehind
{
    public class FormItemBase : ComponentBase, IDisposable
    {
        protected string Id { get; set; } = Guid.NewGuid().ToString();

        protected string ErrorMessage { get; set; } = string.Empty;

        [Inject]
        protected JsUtils JsUtils { get; set; }

        [Inject]
        protected ILogger<FormItemBase> Logger { get; set; }

        [CascadingParameter]
        protected IFormValidator ContainerForm { get; set; }

        [Parameter]
        protected string Title { get; set; }

        [Parameter]
        protected bool NoLabel { get; set; }

        [Parameter]
        protected string[] Properties { get; set; }

        [Parameter]
        protected RenderFragment ChildContent { get; set; }

        public void Dispose()
        {
            if (ContainerForm == null)
            {
                return;
            }

            ContainerForm.OnValidated -= OnValidated;
        }

        protected override void OnInit()
        {
            if (ContainerForm == null)
            {
                Logger.LogWarning("Model validation container is not set.");
                return;
            }

            ContainerForm.OnValidated += OnValidated;
        }

        private async void OnValidated(object sender, ValidationResult result)
        {
            if (Properties == null)
            {
                Logger.LogInformation($"Validation properties set to null, Sender: {sender}");
                return;
            }

            if (result.IsValid || !result.Errors.Keys.Any(k => Properties.Contains(k)))
            {
                await JsUtils.SucceedValidation(Id);
                return;
            }

            var allErrors = result.Errors
                .Where(e => Properties.Contains(e.Key))
                .SelectMany(e => e.Value);
            ErrorMessage = string.Join(Environment.NewLine, allErrors);
            await JsUtils.FailValidation(Id);
            StateHasChanged();
        }
    }
}
