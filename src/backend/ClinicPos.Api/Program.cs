using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using ClinicPos.Api.Auth;
using ClinicPos.Api.Data;
using ClinicPos.Api.Domain;
using ClinicPos.Api.Features.Appointments;
using ClinicPos.Api.Features.Patients;
using ClinicPos.Api.Features.Users;
using ClinicPos.Api.Infrastructure;
using ClinicPos.Api.Options;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

var postgresConnection = builder.Configuration.GetConnectionString("Postgres")
    ?? "Host=localhost;Port=5432;Database=clinic_pos;Username=postgres;Password=postgres";
var redisConnection = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddHttpContextAccessor();
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy
            .WithOrigins("http://localhost:3000")
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});
builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection("RabbitMQ"));

builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(postgresConnection));
builder.Services.AddStackExchangeRedisCache(options => options.Configuration = redisConnection);
builder.Services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConnection));

builder.Services
    .AddAuthentication("Token")
    .AddScheme<AuthenticationSchemeOptions, TokenAuthenticationHandler>("Token", _ => { });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(Policies.CreatePatients, policy => policy.RequireAssertion(context => HasPermission(context, Permissions.CreatePatients)));
    options.AddPolicy(Policies.ViewPatients, policy => policy.RequireAssertion(context => HasPermission(context, Permissions.ViewPatients)));
    options.AddPolicy(Policies.CreateAppointments, policy => policy.RequireAssertion(context => HasPermission(context, Permissions.CreateAppointments)));
    options.AddPolicy(Policies.ManageUsers, policy => policy.RequireAssertion(context => HasPermission(context, Permissions.ManageUsers)));
});

builder.Services.AddScoped<TenantRequestContext>();
builder.Services.AddScoped<ITenantCacheVersionService, TenantCacheVersionService>();
builder.Services.AddScoped<IPatientService, PatientService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IAppointmentEventPublisher, AppointmentEventPublisher>();
builder.Services.AddScoped<IAppointmentService, AppointmentService>();
builder.Services.AddHostedService<DbInitializer>();

var app = builder.Build();

app.UseMiddleware<ApiExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthentication();
app.UseCors("Frontend");
app.UseAuthorization();

var api = app.MapGroup("/api").RequireAuthorization();

api.MapGet("/auth/me", (HttpContext context) =>
{
    var branchIds = context.User.FindAll("branch_id").Select(x => x.Value).ToArray();
    return Results.Ok(new
    {
        userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier),
        email = context.User.FindFirstValue(ClaimTypes.Name),
        role = context.User.FindFirstValue(ClaimTypes.Role),
        tenantId = context.User.FindFirstValue("tenant_id"),
        branchIds
    });
});

api.MapGet("/branches", async (
    [FromQuery] Guid tenantId,
    TenantRequestContext tenantContext,
    AppDbContext dbContext,
    CancellationToken cancellationToken) =>
{
    if (!tenantContext.ValidateTenant(tenantId, out var error))
    {
        return Results.Problem(statusCode: StatusCodes.Status403Forbidden, title: "Forbidden", detail: error);
    }

    var branches = await dbContext.Branches.AsNoTracking()
        .Where(x => x.TenantId == tenantId)
        .OrderBy(x => x.Name)
        .Select(x => new { x.Id, x.Name })
        .ToListAsync(cancellationToken);

    return Results.Ok(branches);
}).RequireAuthorization(Policies.ViewPatients);

api.MapPost("/patients", async (
    CreatePatientRequest request,
    TenantRequestContext tenantContext,
    IPatientService service,
    CancellationToken cancellationToken) =>
{
    var errors = ValidationHelper.Validate(request);
    if (errors is not null)
    {
        return ValidationHelper.ValidationProblem(errors);
    }

    if (!tenantContext.ValidateTenant(request.TenantId, out var error))
    {
        return Results.Problem(statusCode: StatusCodes.Status403Forbidden, title: "Forbidden", detail: error);
    }

    if (!tenantContext.CanAccessBranch(request.PrimaryBranchId))
    {
        return Results.Problem(statusCode: StatusCodes.Status403Forbidden, title: "Forbidden", detail: "Branch access denied");
    }

    try
    {
        var created = await service.CreateAsync(request, cancellationToken);
        return Results.Created($"/api/patients/{created.Id}", created);
    }
    catch (DuplicatePhoneException)
    {
        return Results.Problem(statusCode: StatusCodes.Status409Conflict, title: "Conflict", detail: "Phone number already exists in this tenant");
    }
}).RequireAuthorization(Policies.CreatePatients);

api.MapGet("/patients", async (
    [FromQuery] Guid tenantId,
    [FromQuery] Guid? branchId,
    TenantRequestContext tenantContext,
    IPatientService service,
    CancellationToken cancellationToken) =>
{
    if (!tenantContext.ValidateTenant(tenantId, out var error))
    {
        return Results.Problem(statusCode: StatusCodes.Status403Forbidden, title: "Forbidden", detail: error);
    }

    if (!tenantContext.CanAccessBranch(branchId))
    {
        return Results.Problem(statusCode: StatusCodes.Status403Forbidden, title: "Forbidden", detail: "Branch access denied");
    }

    var result = await service.ListAsync(tenantId, branchId, cancellationToken);
    return Results.Ok(result);
}).RequireAuthorization(Policies.ViewPatients);

api.MapPost("/appointments", async (
    CreateAppointmentRequest request,
    TenantRequestContext tenantContext,
    IAppointmentService service,
    CancellationToken cancellationToken) =>
{
    var errors = ValidationHelper.Validate(request);
    if (errors is not null)
    {
        return ValidationHelper.ValidationProblem(errors);
    }

    if (!tenantContext.ValidateTenant(request.TenantId, out var error))
    {
        return Results.Problem(statusCode: StatusCodes.Status403Forbidden, title: "Forbidden", detail: error);
    }

    if (!tenantContext.CanAccessBranch(request.BranchId))
    {
        return Results.Problem(statusCode: StatusCodes.Status403Forbidden, title: "Forbidden", detail: "Branch access denied");
    }

    try
    {
        var created = await service.CreateAsync(request, cancellationToken);
        return Results.Created($"/api/appointments/{created.Id}", created);
    }
    catch (MissingPatientException)
    {
        return Results.Problem(statusCode: StatusCodes.Status400BadRequest, title: "ValidationError", detail: "Patient not found in this tenant");
    }
    catch (DuplicateAppointmentException)
    {
        return Results.Problem(statusCode: StatusCodes.Status409Conflict, title: "Conflict", detail: "Duplicate appointment in the same tenant and branch");
    }
}).RequireAuthorization(Policies.CreateAppointments);

api.MapPost("/users", async (
    CreateUserRequest request,
    TenantRequestContext tenantContext,
    IUserService service,
    CancellationToken cancellationToken) =>
{
    var errors = ValidationHelper.Validate(request);
    if (errors is not null)
    {
        return ValidationHelper.ValidationProblem(errors);
    }

    if (!tenantContext.ValidateTenant(request.TenantId, out var error))
    {
        return Results.Problem(statusCode: StatusCodes.Status403Forbidden, title: "Forbidden", detail: error);
    }

    if (request.BranchIds.Count == 0)
    {
        return Results.Problem(statusCode: StatusCodes.Status400BadRequest, title: "ValidationError", detail: "At least one branch is required");
    }

    var user = await service.CreateAsync(request, cancellationToken);
    return Results.Created($"/api/users/{user.Id}", user);
}).RequireAuthorization(Policies.ManageUsers);

api.MapPut("/users/{userId:guid}/role", async (
    Guid userId,
    AssignRoleRequest request,
    IUserService service,
    CancellationToken cancellationToken) =>
{
    var errors = ValidationHelper.Validate(request);
    if (errors is not null)
    {
        return ValidationHelper.ValidationProblem(errors);
    }

    var user = await service.AssignRoleAsync(userId, request.Role, cancellationToken);
    return user is null ? Results.NotFound() : Results.Ok(user);
}).RequireAuthorization(Policies.ManageUsers);

api.MapPut("/users/{userId:guid}/branches", async (
    Guid userId,
    AssignBranchesRequest request,
    IUserService service,
    CancellationToken cancellationToken) =>
{
    var errors = ValidationHelper.Validate(request);
    if (errors is not null)
    {
        return ValidationHelper.ValidationProblem(errors);
    }

    var user = await service.AssignBranchesAsync(userId, request.BranchIds, cancellationToken);
    return user is null ? Results.NotFound() : Results.Ok(user);
}).RequireAuthorization(Policies.ManageUsers);

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run();

static bool HasPermission(AuthorizationHandlerContext context, string permission)
{
    var roleValue = context.User.FindFirstValue(ClaimTypes.Role);
    if (!Enum.TryParse<UserRole>(roleValue, ignoreCase: true, out var role))
    {
        return false;
    }

    return RolePermissions.HasPermission(role, permission);
}
