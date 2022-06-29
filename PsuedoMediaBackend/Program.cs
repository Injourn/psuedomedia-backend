using Microsoft.AspNetCore.Authentication;
using Microsoft.OpenApi.Models;
using PsuedoMediaBackend.Filters;
using PsuedoMediaBackend.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddCors(options => {
    options.AddDefaultPolicy(policy => {
        policy.WithOrigins("*").AllowAnyHeader().WithExposedHeaders("pm-jwttoken","pm-refreshtoken").AllowAnyMethod();
    });
});

builder.Services.AddControllers(options => {
    options.Filters.Add<PsuedoMediaActionFilter>();
});
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen( opt => {
    opt.SwaggerDoc("v1", new OpenApiInfo { Title = "MyAPI", Version = "v1" });
    opt.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme {
        In = ParameterLocation.Header,
        Description = "Please enter token",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = "bearer"
    });
    opt.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type=ReferenceType.SecurityScheme,
                    Id="Bearer"
                }
            },
            new string[]{}
        }
    });
});

builder.Services.Configure<PsuedoMediaDatabaseSettings>(
    builder.Configuration.GetSection("PsuedoMediaDatabase"));
builder.Services.AddSingleton<PsuedoMediaBackend.Services.AuthenticationService>();
builder.Services.AddSingleton<PsuedoMediaBackend.Services.AccountService>();
builder.Services.AddSingleton<PsuedoMediaBackend.Services.PostsService>();

var app = builder.Build();
app.UseCors();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment()) {
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
