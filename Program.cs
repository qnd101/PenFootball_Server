using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Cors.Infrastructure;
using PenFootball_Server.Services;
using PenFootball_Server.DB;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.HttpOverrides;
using System.Runtime;
using PenFootball_Server.Settings;
using Microsoft.AspNetCore.Identity;

/*
var pw = "hellogoodbye";
PasswordHasher<object> _passwordHasher = new PasswordHasher<object>();
var hpw = _passwordHasher.HashPassword(null, pw);
Console.WriteLine("Passord: " + pw + ", Hashed Password: " + hpw);*/

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddDbContext<UserDataContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("UserDataConnection") ?? throw new InvalidOperationException("Connection string 'UserDataConnection' not found.")));
builder.Services.AddControllers();
builder.Services.Configure<RatingSettings>(builder.Configuration.GetSection("RatingSettings"));
builder.Services.Configure<ServerSettings>(builder.Configuration.GetSection("GameServers"));
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));

var tokenkeysettings = new TokenKeySettings(); //서버 시작 시 무작위적으로 키 생성
builder.Services.AddSingleton(tokenkeysettings);
var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins,
                      policy =>
                      {
                           policy.WithOrigins(builder.Configuration.GetValue<string>("CORSOrigins")?.Split(";") ?? throw new InvalidOperationException("CorsOrigins not found."))
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials();
                      });
});
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.MapInboundClaims = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {

        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,

        ValidIssuer = "penfootball-server",
        ValidAudience = "penfootball-frontend",
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(tokenkeysettings.Secret))
    };
    options.Events = new JwtBearerEvents
    { 
        OnAuthenticationFailed = context =>
        {
            Console.WriteLine($"Authentication failed: {context.Exception.Message}");
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            Console.WriteLine("Token validated successfully");
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    //SeedData.Initialize(services, builder.Configuration );
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    //app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseStaticFiles();

app.UseRouting();

app.UseCors(MyAllowSpecificOrigins);

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.MapControllers();

app.Run();
