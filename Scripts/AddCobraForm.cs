using Microsoft.EntityFrameworkCore;
using DigiDocWebApp.Data;
using DigiDocWebApp.Models;
using System.Text.Json;

namespace DigiDocWebApp.Scripts
{
    public static class AddCobraForm
    {
        public static async Task ExecuteAsync(AppDbContext context)
        {
            // Check if COBRA form already exists
            var existingForm = await context.FormTemplates
                .FirstOrDefaultAsync(f => f.Name == "COBRA Administration Request - Additional Benefits");

            if (existingForm != null)
            {
                Console.WriteLine("COBRA Administration Request form already exists. Updating...");
                context.FormTemplates.Remove(existingForm);
            }

            var formStructure = new
            {
                formName = "COBRA Administration Request - Additional Benefits",
                description = "COBRA Administration Request for Additional Benefits Coverage",
                pages = new object[]
                {
                    new
                    {
                        pageNumber = 1,
                        title = "Employee Information",
                        fields = new object[]
                        {
                            new
                            {
                                id = "employeeLegalName",
                                type = "text",
                                label = "Employee Legal Name",
                                placeholder = "Enter full legal name",
                                required = true,
                                position = new { x = 10, y = 50, width = 400, height = 30 }
                            }
                        }
                    },
                    new
                    {
                        pageNumber = 2,
                        title = "Benefit #4 Details",
                        fields = new object[]
                        {
                            new
                            {
                                id = "benefit4_remitPayment",
                                type = "text",
                                label = "NBS will remit payment to",
                                placeholder = "Payment recipient for Benefit #4",
                                required = true,
                                position = new { x = 10, y = 50, width = 300, height = 30 }
                            },
                            new
                            {
                                id = "benefit4_enrollmentChoice",
                                type = "radio",
                                label = "Enrollment Choice",
                                required = true,
                                validation = new
                                {
                                    options = new[] { "No", "Yes" }
                                },
                                position = new { x = 520, y = 50, width = 100, height = 30 }
                            },
                            new
                            {
                                id = "benefit4_coverage",
                                type = "checkbox",
                                label = "Coverage Types",
                                required = false,
                                validation = new
                                {
                                    options = new[] 
                                    { 
                                        "Medical", 
                                        "Dental", 
                                        "Vision", 
                                        "Health Reimbursement Arrangement",
                                        "Flexible Spending Account",
                                        "On-Site Health Care Facility",
                                        "Employee Assistance Program",
                                        "Executive Medical Reimbursement Plan"
                                    }
                                },
                                position = new { x = 10, y = 100, width = 600, height = 150 }
                            },
                            new
                            {
                                id = "benefit4_planType",
                                type = "text",
                                label = "Plan Type",
                                placeholder = "Specify plan type",
                                required = false,
                                position = new { x = 10, y = 270, width = 200, height = 30 }
                            },
                            new
                            {
                                id = "benefit4_benefitProvider",
                                type = "text",
                                label = "Benefit Provider",
                                placeholder = "Provider name",
                                required = true,
                                position = new { x = 10, y = 320, width = 300, height = 30 }
                            },
                            new
                            {
                                id = "benefit4_eligibilityContact",
                                type = "text",
                                label = "Eligibility/Enrollment Contact",
                                placeholder = "Contact information",
                                required = true,
                                position = new { x = 320, y = 320, width = 300, height = 30 }
                            },
                            new
                            {
                                id = "benefit4_address",
                                type = "textarea",
                                label = "Address, City, State, Zip Code",
                                placeholder = "Full address",
                                required = true,
                                position = new { x = 10, y = 370, width = 610, height = 60 }
                            },
                            new
                            {
                                id = "benefit4_phoneNumber",
                                type = "tel",
                                label = "Phone Number",
                                placeholder = "(xxx) xxx-xxxx",
                                required = true,
                                position = new { x = 10, y = 450, width = 150, height = 30 }
                            },
                            new
                            {
                                id = "benefit4_faxNumber",
                                type = "tel",
                                label = "Fax Number",
                                placeholder = "(xxx) xxx-xxxx",
                                required = false,
                                position = new { x = 180, y = 450, width = 150, height = 30 }
                            },
                            new
                            {
                                id = "benefit4_emailAddress",
                                type = "email",
                                label = "Email Address",
                                placeholder = "email@domain.com",
                                required = true,
                                position = new { x = 350, y = 450, width = 150, height = 30 }
                            },
                            new
                            {
                                id = "benefit4_policyNumber",
                                type = "text",
                                label = "Policy Number",
                                placeholder = "Policy number",
                                required = false,
                                position = new { x = 520, y = 450, width = 100, height = 30 }
                            },
                            new
                            {
                                id = "benefit4_planName",
                                type = "text",
                                label = "Plan Name",
                                placeholder = "Plan name",
                                required = true,
                                position = new { x = 10, y = 500, width = 300, height = 30 }
                            },
                            new
                            {
                                id = "benefit4_conversionAvailable",
                                type = "radio",
                                label = "Conversion Available",
                                required = false,
                                validation = new
                                {
                                    options = new[] { "Yes", "No" }
                                },
                                position = new { x = 350, y = 500, width = 100, height = 30 }
                            },
                            new
                            {
                                id = "benefit4_renewalDate",
                                type = "date",
                                label = "Renewal Date",
                                required = false,
                                position = new { x = 470, y = 500, width = 150, height = 30 }
                            },
                            new
                            {
                                id = "benefit4_rateSheet",
                                type = "select",
                                label = "Supply Rate Sheet or Fill in Below",
                                required = true,
                                validation = new
                                {
                                    options = new[] { "Rate Sheet Supplied", "Fill in Below" }
                                },
                                position = new { x = 10, y = 550, width = 200, height = 30 }
                            },
                            new
                            {
                                id = "benefit4_single",
                                type = "number",
                                label = "Single Rate ($)",
                                placeholder = "0.00",
                                required = false,
                                position = new { x = 10, y = 600, width = 100, height = 30 }
                            },
                            new
                            {
                                id = "benefit4_family",
                                type = "number",
                                label = "Family Rate ($)",
                                placeholder = "0.00",
                                required = false,
                                position = new { x = 130, y = 600, width = 100, height = 30 }
                            },
                            new
                            {
                                id = "benefit4_employeeChildren",
                                type = "number",
                                label = "Employee + Children Rate ($)",
                                placeholder = "0.00",
                                required = false,
                                position = new { x = 250, y = 600, width = 120, height = 30 }
                            },
                            new
                            {
                                id = "benefit4_twoParty",
                                type = "number",
                                label = "Two Party Rate ($)",
                                placeholder = "0.00",
                                required = false,
                                position = new { x = 10, y = 650, width = 100, height = 30 }
                            },
                            new
                            {
                                id = "benefit4_employeeChild",
                                type = "number",
                                label = "Employee + Child Rate ($)",
                                placeholder = "0.00",
                                required = false,
                                position = new { x = 130, y = 650, width = 120, height = 30 }
                            },
                            new
                            {
                                id = "benefit4_other",
                                type = "number",
                                label = "Other Rate ($)",
                                placeholder = "0.00",
                                required = false,
                                position = new { x = 270, y = 650, width = 100, height = 30 }
                            }
                        }
                    },
                    new
                    {
                        pageNumber = 3,
                        title = "Benefit #5 Details",
                        fields = new object[]
                        {
                            new
                            {
                                id = "benefit5_remitPayment",
                                type = "text",
                                label = "NBS will remit payment to",
                                placeholder = "Payment recipient for Benefit #5",
                                required = true,
                                position = new { x = 10, y = 50, width = 300, height = 30 }
                            },
                            new
                            {
                                id = "benefit5_enrollmentChoice",
                                type = "radio",
                                label = "Enrollment Choice",
                                required = true,
                                validation = new
                                {
                                    options = new[] { "No", "Yes" }
                                },
                                position = new { x = 520, y = 50, width = 100, height = 30 }
                            },
                            new
                            {
                                id = "benefit5_coverage",
                                type = "checkbox",
                                label = "Coverage Types",
                                required = false,
                                validation = new
                                {
                                    options = new[] 
                                    { 
                                        "Medical", 
                                        "Dental", 
                                        "Vision", 
                                        "Health Reimbursement Arrangement",
                                        "Flexible Spending Account",
                                        "On-Site Health Care Facility",
                                        "Employee Assistance Program",
                                        "Executive Medical Reimbursement Plan"
                                    }
                                },
                                position = new { x = 10, y = 100, width = 600, height = 150 }
                            },
                            new
                            {
                                id = "benefit5_planType",
                                type = "text",
                                label = "Plan Type",
                                placeholder = "Specify plan type",
                                required = false,
                                position = new { x = 10, y = 270, width = 200, height = 30 }
                            },
                            new
                            {
                                id = "benefit5_benefitProvider",
                                type = "text",
                                label = "Benefit Provider",
                                placeholder = "Provider name",
                                required = true,
                                position = new { x = 10, y = 320, width = 300, height = 30 }
                            },
                            new
                            {
                                id = "benefit5_eligibilityContact",
                                type = "text",
                                label = "Eligibility/Enrollment Contact",
                                placeholder = "Contact information",
                                required = true,
                                position = new { x = 320, y = 320, width = 300, height = 30 }
                            },
                            new
                            {
                                id = "benefit5_address",
                                type = "textarea",
                                label = "Address, City, State, Zip Code",
                                placeholder = "Full address",
                                required = true,
                                position = new { x = 10, y = 370, width = 610, height = 60 }
                            },
                            new
                            {
                                id = "benefit5_phoneNumber",
                                type = "tel",
                                label = "Phone Number",
                                placeholder = "(xxx) xxx-xxxx",
                                required = true,
                                position = new { x = 10, y = 450, width = 150, height = 30 }
                            },
                            new
                            {
                                id = "benefit5_faxNumber",
                                type = "tel",
                                label = "Fax Number",
                                placeholder = "(xxx) xxx-xxxx",
                                required = false,
                                position = new { x = 180, y = 450, width = 150, height = 30 }
                            },
                            new
                            {
                                id = "benefit5_emailAddress",
                                type = "email",
                                label = "Email Address",
                                placeholder = "email@domain.com",
                                required = true,
                                position = new { x = 350, y = 450, width = 150, height = 30 }
                            },
                            new
                            {
                                id = "benefit5_policyNumber",
                                type = "text",
                                label = "Policy Number",
                                placeholder = "Policy number",
                                required = false,
                                position = new { x = 520, y = 450, width = 100, height = 30 }
                            },
                            new
                            {
                                id = "benefit5_planName",
                                type = "text",
                                label = "Plan Name",
                                placeholder = "Plan name",
                                required = true,
                                position = new { x = 10, y = 500, width = 300, height = 30 }
                            },
                            new
                            {
                                id = "benefit5_conversionAvailable",
                                type = "radio",
                                label = "Conversion Available",
                                required = false,
                                validation = new
                                {
                                    options = new[] { "Yes", "No" }
                                },
                                position = new { x = 350, y = 500, width = 100, height = 30 }
                            },
                            new
                            {
                                id = "benefit5_renewalDate",
                                type = "date",
                                label = "Renewal Date",
                                required = false,
                                position = new { x = 470, y = 500, width = 150, height = 30 }
                            },
                            new
                            {
                                id = "benefit5_rateSheet",
                                type = "select",
                                label = "Supply Rate Sheet or Fill in Below",
                                required = true,
                                validation = new
                                {
                                    options = new[] { "Rate Sheet Supplied", "Fill in Below" }
                                },
                                position = new { x = 10, y = 550, width = 200, height = 30 }
                            },
                            new
                            {
                                id = "benefit5_single",
                                type = "number",
                                label = "Single Rate ($)",
                                placeholder = "0.00",
                                required = false,
                                position = new { x = 10, y = 600, width = 100, height = 30 }
                            },
                            new
                            {
                                id = "benefit5_family",
                                type = "number",
                                label = "Family Rate ($)",
                                placeholder = "0.00",
                                required = false,
                                position = new { x = 130, y = 600, width = 100, height = 30 }
                            },
                            new
                            {
                                id = "benefit5_employeeChildren",
                                type = "number",
                                label = "Employee + Children Rate ($)",
                                placeholder = "0.00",
                                required = false,
                                position = new { x = 250, y = 600, width = 120, height = 30 }
                            },
                            new
                            {
                                id = "benefit5_twoParty",
                                type = "number",
                                label = "Two Party Rate ($)",
                                placeholder = "0.00",
                                required = false,
                                position = new { x = 10, y = 650, width = 100, height = 30 }
                            },
                            new
                            {
                                id = "benefit5_employeeChild",
                                type = "number",
                                label = "Employee + Child Rate ($)",
                                placeholder = "0.00",
                                required = false,
                                position = new { x = 130, y = 650, width = 120, height = 30 }
                            },
                            new
                            {
                                id = "benefit5_other",
                                type = "number",
                                label = "Other Rate ($)",
                                placeholder = "0.00",
                                required = false,
                                position = new { x = 270, y = 650, width = 100, height = 30 }
                            }
                        }
                    },
                    new
                    {
                        pageNumber = 4,
                        title = "Benefit #6 Details",
                        fields = new object[]
                        {
                            new
                            {
                                id = "benefit6_remitPayment",
                                type = "text",
                                label = "NBS will remit payment to",
                                placeholder = "Payment recipient for Benefit #6",
                                required = true,
                                position = new { x = 10, y = 50, width = 300, height = 30 }
                            },
                            new
                            {
                                id = "benefit6_enrollmentChoice",
                                type = "radio",
                                label = "Enrollment Choice",
                                required = true,
                                validation = new
                                {
                                    options = new[] { "No", "Yes" }
                                },
                                position = new { x = 520, y = 50, width = 100, height = 30 }
                            },
                            new
                            {
                                id = "benefit6_coverage",
                                type = "checkbox",
                                label = "Coverage Types",
                                required = false,
                                validation = new
                                {
                                    options = new[] 
                                    { 
                                        "Medical", 
                                        "Dental", 
                                        "Vision", 
                                        "Health Reimbursement Arrangement",
                                        "Flexible Spending Account",
                                        "On-Site Health Care Facility",
                                        "Employee Assistance Program",
                                        "Executive Medical Reimbursement Plan"
                                    }
                                },
                                position = new { x = 10, y = 100, width = 600, height = 150 }
                            },
                            new
                            {
                                id = "benefit6_planType",
                                type = "text",
                                label = "Plan Type",
                                placeholder = "Specify plan type",
                                required = false,
                                position = new { x = 10, y = 270, width = 200, height = 30 }
                            },
                            new
                            {
                                id = "benefit6_benefitProvider",
                                type = "text",
                                label = "Benefit Provider",
                                placeholder = "Provider name",
                                required = true,
                                position = new { x = 10, y = 320, width = 300, height = 30 }
                            },
                            new
                            {
                                id = "benefit6_eligibilityContact",
                                type = "text",
                                label = "Eligibility/Enrollment Contact",
                                placeholder = "Contact information",
                                required = true,
                                position = new { x = 320, y = 320, width = 300, height = 30 }
                            },
                            new
                            {
                                id = "benefit6_address",
                                type = "textarea",
                                label = "Address, City, State, Zip Code",
                                placeholder = "Full address",
                                required = true,
                                position = new { x = 10, y = 370, width = 610, height = 60 }
                            },
                            new
                            {
                                id = "benefit6_phoneNumber",
                                type = "tel",
                                label = "Phone Number",
                                placeholder = "(xxx) xxx-xxxx",
                                required = true,
                                position = new { x = 10, y = 450, width = 150, height = 30 }
                            },
                            new
                            {
                                id = "benefit6_faxNumber",
                                type = "tel",
                                label = "Fax Number",
                                placeholder = "(xxx) xxx-xxxx",
                                required = false,
                                position = new { x = 180, y = 450, width = 150, height = 30 }
                            },
                            new
                            {
                                id = "benefit6_emailAddress",
                                type = "email",
                                label = "Email Address",
                                placeholder = "email@domain.com",
                                required = true,
                                position = new { x = 350, y = 450, width = 150, height = 30 }
                            },
                            new
                            {
                                id = "benefit6_policyNumber",
                                type = "text",
                                label = "Policy Number",
                                placeholder = "Policy number",
                                required = false,
                                position = new { x = 520, y = 450, width = 100, height = 30 }
                            },
                            new
                            {
                                id = "benefit6_planName",
                                type = "text",
                                label = "Plan Name",
                                placeholder = "Plan name",
                                required = true,
                                position = new { x = 10, y = 500, width = 300, height = 30 }
                            },
                            new
                            {
                                id = "benefit6_conversionAvailable",
                                type = "radio",
                                label = "Conversion Available",
                                required = false,
                                validation = new
                                {
                                    options = new[] { "Yes", "No" }
                                },
                                position = new { x = 350, y = 500, width = 100, height = 30 }
                            },
                            new
                            {
                                id = "benefit6_renewalDate",
                                type = "date",
                                label = "Renewal Date",
                                required = false,
                                position = new { x = 470, y = 500, width = 150, height = 30 }
                            },
                            new
                            {
                                id = "benefit6_rateSheet",
                                type = "select",
                                label = "Supply Rate Sheet or Fill in Below",
                                required = true,
                                validation = new
                                {
                                    options = new[] { "Rate Sheet Supplied", "Fill in Below" }
                                },
                                position = new { x = 10, y = 550, width = 200, height = 30 }
                            },
                            new
                            {
                                id = "benefit6_single",
                                type = "number",
                                label = "Single Rate ($)",
                                placeholder = "0.00",
                                required = false,
                                position = new { x = 10, y = 600, width = 100, height = 30 }
                            },
                            new
                            {
                                id = "benefit6_family",
                                type = "number",
                                label = "Family Rate ($)",
                                placeholder = "0.00",
                                required = false,
                                position = new { x = 130, y = 600, width = 100, height = 30 }
                            },
                            new
                            {
                                id = "benefit6_employeeChildren",
                                type = "number",
                                label = "Employee + Children Rate ($)",
                                placeholder = "0.00",
                                required = false,
                                position = new { x = 250, y = 600, width = 120, height = 30 }
                            },
                            new
                            {
                                id = "benefit6_twoParty",
                                type = "number",
                                label = "Two Party Rate ($)",
                                placeholder = "0.00",
                                required = false,
                                position = new { x = 10, y = 650, width = 100, height = 30 }
                            },
                            new
                            {
                                id = "benefit6_employeeChild",
                                type = "number",
                                label = "Employee + Child Rate ($)",
                                placeholder = "0.00",
                                required = false,
                                position = new { x = 130, y = 650, width = 120, height = 30 }
                            },
                            new
                            {
                                id = "benefit6_other",
                                type = "number",
                                label = "Other Rate ($)",
                                placeholder = "0.00",
                                required = false,
                                position = new { x = 270, y = 650, width = 100, height = 30 }
                            }
                        }
                    },
                    new
                    {
                        pageNumber = 5,
                        title = "Benefits #7, #8, #9 Summary & Submission",
                        fields = new object[]
                        {
                            new
                            {
                                id = "benefit7_remitPayment",
                                type = "text",
                                label = "Benefit #7 - NBS will remit payment to",
                                placeholder = "Payment recipient for Benefit #7",
                                required = false,
                                position = new { x = 10, y = 50, width = 300, height = 30 }
                            },
                            new
                            {
                                id = "benefit7_enrollmentChoice",
                                type = "radio",
                                label = "Benefit #7 - Enrollment Choice",
                                required = false,
                                validation = new
                                {
                                    options = new[] { "No", "Yes" }
                                },
                                position = new { x = 520, y = 50, width = 100, height = 30 }
                            },
                            new
                            {
                                id = "benefit8_remitPayment",
                                type = "text",
                                label = "Benefit #8 - NBS will remit payment to",
                                placeholder = "Payment recipient for Benefit #8",
                                required = false,
                                position = new { x = 10, y = 150, width = 300, height = 30 }
                            },
                            new
                            {
                                id = "benefit8_enrollmentChoice",
                                type = "radio",
                                label = "Benefit #8 - Enrollment Choice",
                                required = false,
                                validation = new
                                {
                                    options = new[] { "No", "Yes" }
                                },
                                position = new { x = 520, y = 150, width = 100, height = 30 }
                            },
                            new
                            {
                                id = "benefit9_remitPayment",
                                type = "text",
                                label = "Benefit #9 - NBS will remit payment to",
                                placeholder = "Payment recipient for Benefit #9",
                                required = false,
                                position = new { x = 10, y = 250, width = 300, height = 30 }
                            },
                            new
                            {
                                id = "benefit9_enrollmentChoice",
                                type = "radio",
                                label = "Benefit #9 - Enrollment Choice",
                                required = false,
                                validation = new
                                {
                                    options = new[] { "No", "Yes" }
                                },
                                position = new { x = 520, y = 250, width = 100, height = 30 }
                            },
                            new
                            {
                                id = "additionalComments",
                                type = "textarea",
                                label = "Additional Comments or Special Instructions",
                                placeholder = "Enter any additional information, special instructions, or comments regarding the COBRA administration request...",
                                required = false,
                                position = new { x = 10, y = 350, width = 610, height = 120 }
                            },
                            new
                            {
                                id = "submissionDate",
                                type = "date",
                                label = "Date of Submission",
                                required = true,
                                position = new { x = 10, y = 500, width = 150, height = 30 }
                            },
                            new
                            {
                                id = "submitterName",
                                type = "text",
                                label = "Submitted By (Name)",
                                placeholder = "Full name of person submitting",
                                required = true,
                                position = new { x = 180, y = 500, width = 200, height = 30 }
                            },
                            new
                            {
                                id = "submitterTitle",
                                type = "text",
                                label = "Title/Position",
                                placeholder = "Job title",
                                required = true,
                                position = new { x = 400, y = 500, width = 150, height = 30 }
                            }
                        }
                    }
                }
            };

            var newForm = new FormTemplate
            {
                Name = "COBRA Administration Request - Additional Benefits",
                Description = "COBRA Administration Request for Additional Benefits Coverage (Benefits #4-9) - NBS Form",
                Category = "COBRA",
                StructureJson = JsonSerializer.Serialize(formStructure),
                TotalPages = 5,
                IsActive = true,
                CreatedBy = "System",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            context.FormTemplates.Add(newForm);
            await context.SaveChangesAsync();

            Console.WriteLine("COBRA Administration Request form has been successfully added to the database!");
            Console.WriteLine($"Form ID: {newForm.Id}");
            Console.WriteLine($"Total Pages: {newForm.TotalPages}");
            Console.WriteLine($"Category: {newForm.Category}");
        }
    }
} 