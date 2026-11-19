using FluentValidation;
using SoftwareServices.Application.DTOs.Auth;
using SoftwareServices.Application.DTOs.Enquiry;
using SoftwareServices.Application.DTOs.Payment;
using SoftwareServices.Application.DTOs.Project;
using SoftwareServices.Application.DTOs.Quotation;

namespace SoftwareServices.Application.Validators;

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.UsernameOrEmail).NotEmpty().WithMessage("Username or email is required.");
        RuleFor(x => x.Password).NotEmpty().WithMessage("Password is required.");
    }
}

public class CreateEnquiryRequestValidator : AbstractValidator<CreateEnquiryRequest>
{
    public CreateEnquiryRequestValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(150).WithMessage("Full name is required (max 150 chars).");
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(150).WithMessage("A valid email address is required.");
        RuleFor(x => x.PhoneNumber).NotEmpty().MaximumLength(50).WithMessage("Phone number is required.");
        RuleFor(x => x.ProjectTitle).NotEmpty().MaximumLength(250).WithMessage("Project title is required.");
        RuleFor(x => x.ProjectDescription).NotEmpty().MinimumLength(15).WithMessage("Please describe your project (at least 15 characters).");
        RuleFor(x => x.RequiredTechnology).NotEmpty().WithMessage("Required technology is required.");
        RuleFor(x => x.EstimatedBudget).NotEmpty().WithMessage("Estimated budget is required.");
    }
}

public class CreateQuotationRequestValidator : AbstractValidator<CreateQuotationRequest>
{
    public CreateQuotationRequestValidator()
    {
        RuleFor(x => x.EnquiryId).GreaterThan(0).WithMessage("Valid enquiry ID is required.");
        RuleFor(x => x.ServiceDescription).NotEmpty().WithMessage("Service description is required.");
        RuleFor(x => x.Deliverables).NotEmpty().WithMessage("Deliverables are required.");
        RuleFor(x => x.DevelopmentCost).GreaterThan(0).WithMessage("Development cost must be greater than zero.");
        RuleFor(x => x.ExpectedDeliveryDate).GreaterThan(DateTime.UtcNow).WithMessage("Delivery date must be in the future.");
        RuleFor(x => x.ValidityDays).GreaterThan(0).WithMessage("Validity days must be at least 1 day.");
    }
}

public class RecordPaymentRequestValidator : AbstractValidator<RecordPaymentRequest>
{
    public RecordPaymentRequestValidator()
    {
        RuleFor(x => x.ProjectId).GreaterThan(0).WithMessage("Project ID is required.");
        RuleFor(x => x.Amount).GreaterThan(0).WithMessage("Payment amount must be greater than zero.");
        RuleFor(x => x.PaymentMethod).NotEmpty().WithMessage("Payment method is required.");
    }
}

public class CreateProjectRequestValidator : AbstractValidator<CreateProjectRequest>
{
    public CreateProjectRequestValidator()
    {
        RuleFor(x => x.CustomerId).GreaterThan(0).WithMessage("Customer ID is required.");
        RuleFor(x => x.ProjectTitle).NotEmpty().MaximumLength(250).WithMessage("Project title is required.");
        RuleFor(x => x.AgreedAmount).GreaterThanOrEqualTo(0).WithMessage("Agreed amount must be non-negative.");
        RuleFor(x => x.ExpectedCompletionDate).GreaterThan(x => x.StartDate).WithMessage("Completion date must be after start date.");
    }
}
