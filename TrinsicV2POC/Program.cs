using Google.Protobuf.WellKnownTypes;
using Trinsic;
using Trinsic.Services.UniversalWallet.V1;
using Trinsic.Services.VerifiableCredentials.Templates.V1;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapPost("/ecosystem", async (string? name) =>
{
    var trinsicService = new TrinsicService();

    var (ecosystem, authToken) = await trinsicService.Provider.CreateEcosystemAsync(new()
    {
        Description = name ?? "My ecosystem",
    });

    return new { ecosystem, authToken };
})
.WithName("CreateEcosystem")
.WithOpenApi();

app.MapGet("/template", async (string authToken, string templateId) =>
{
    var trinsicService = new TrinsicService(new Trinsic.Sdk.Options.V1.TrinsicOptions
    {
        AuthToken = authToken
    });

    var getTemplateResponse = await trinsicService.Template.GetAsync(new() { Id = templateId });
    return new { TemplateId = getTemplateResponse.Template.Id, getTemplateResponse.Template.SchemaUri };
})
.WithName("GetTemplate")
.WithOpenApi();

app.MapPost("/template", async (string authToken) =>
{
    var trinsicService = new TrinsicService(new Trinsic.Sdk.Options.V1.TrinsicOptions
    {
        AuthToken = authToken
    });

    CreateCredentialTemplateRequest createRequest = new()
    {
        Name = "An Example Credential",
        Title = "Example Credential",
        Description = "A credential for Trinsic's SDK samples",
        AllowAdditionalFields = false,
        Fields =
        {
            { "firstName", new() { Title = "First Name", Description = "Given name of holder" } },
            { "lastName", new() { Title = "Last Name", Description = "Surname of holder", Optional = true } },
            { "age", new() { Title = "Age", Description = "Age in years of holder", Type = FieldType.Number } }
        },
        FieldOrdering =
        {
            { "firstName", new() { Order = 0, Section = "Name" } },
            { "lastName", new() { Order = 1, Section = "Name" } },
            { "age", new() { Order = 2, Section = "Miscellanous" } }
        },
        AppleWalletOptions = new()
        {
            PrimaryField = "firstName",
            SecondaryFields = { "lastName" },
            AuxiliaryFields = { "age" }
        }
    };

    var template = await trinsicService.Template.CreateAsync(createRequest);

    return new { TemplateId = template.Data.Id, template.Data.SchemaUri };
})
.WithName("CreateTemplate")
.WithOpenApi();

app.MapPost("/issuer", async (string authToken, string schemaUri) =>
{
    var trinsicService = new TrinsicService(new Trinsic.Sdk.Options.V1.TrinsicOptions
    {
        AuthToken = authToken
    });

    var didUri = $"did:{Guid.NewGuid()}";
    _ = await trinsicService.TrustRegistry.RegisterMemberAsync(new()
    {
        DidUri = didUri,
        SchemaUri = schemaUri
    });

    return didUri;
})
.WithName("RegisterIssuer")
.WithOpenApi();

app.MapGet("/issuers", async (string authToken, string schemaUri) =>
{
    var trinsicService = new TrinsicService(new Trinsic.Sdk.Options.V1.TrinsicOptions
    {
        AuthToken = authToken
    });

    var members = await trinsicService.TrustRegistry.ListAuthorizedMembersAsync(new()
    {
        SchemaUri = schemaUri
    });

    return members;
})
.WithName("ListIssuers")
.WithOpenApi();

app.MapGet("/wallet", async (string walletAuthToken) =>
{
    var trinsicService = new TrinsicService(new Trinsic.Sdk.Options.V1.TrinsicOptions
    {
        AuthToken = walletAuthToken
    });

    var walletItems = await trinsicService.Wallet.SearchWalletAsync(new());

    return walletItems;
})
.WithName("SearchWallet")
.WithOpenApi();

app.MapPost("/wallet", async (string ecosystemId, string? description) =>
{
    var trinsic = new TrinsicService();

    var request = new CreateWalletRequest
    {
        EcosystemId = ecosystemId,
        Description = description ?? "MyWallet"
    };

    return await trinsic.Wallet.CreateWalletAsync(request);
})
.WithName("CreateWallet")
.WithOpenApi();

app.MapPost("/credential", async (string walletAuthToken, string templateId, string values) =>
{
    var trinsicService = new TrinsicService(new Trinsic.Sdk.Options.V1.TrinsicOptions
    {
        AuthToken = walletAuthToken
    });

    var credentialJson = await trinsicService.Credential.IssueFromTemplateAsync(new()
    {
        TemplateId = templateId,
        ValuesJson = values,
        IncludeGovernance = true,
    });

    return credentialJson;
})
.WithName("IssueCredential")
.WithOpenApi();

app.Run();
