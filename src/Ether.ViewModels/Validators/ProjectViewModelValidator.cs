﻿using FluentValidation;

namespace Ether.ViewModels.Validators
{
    public class ProjectViewModelValidator : AbstractValidator<VstsProjectViewModel>
    {
        public ProjectViewModelValidator()
        {
            RuleFor(p => p.Name).NotEmpty();
        }
    }
}
