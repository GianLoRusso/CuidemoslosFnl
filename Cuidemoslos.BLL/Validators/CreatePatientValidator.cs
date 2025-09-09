using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cuidemoslos.BLL.Validators;
public class CreatePatientValidator : AbstractValidator<(string FullName, string Email)>
{
    public CreatePatientValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MinimumLength(3);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
    }
}
