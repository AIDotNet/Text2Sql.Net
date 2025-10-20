using AntDesign.ProLayout;
using Text2Sql.Net;
using Text2Sql.Net.Options;
using Text2Sql.Net.Web.Mock;
using Text2Sql.Net.Web.Options;
using Text2Sql.Net.Web.Services.Authentication;
using Text2Sql.Net.Web.Tools;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Embeddings;
using Microsoft.SemanticKernel.TextGeneration;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddAntDesign();
builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(sp.GetService<NavigationManager>()!.BaseUri)
});
builder.Services.Configure<ProSettings>(builder.Configuration.GetSection("ProSettings"));
builder.Services.Configure<AuthenticationOptions>(builder.Configuration.GetSection("Authentication"));
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<ProtectedSessionStorage>();
builder.Services.AddScoped<SessionAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<SessionAuthenticationStateProvider>());
builder.Services.AddScoped<ILoginService, LoginService>();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c => {
    c.SwaggerDoc("v1", new() { Title = "Text2Sql.Net.Api", Version = "v1" });
    //添加Api层注释（true表示显示控制器注释）
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath, true);
});

builder.Configuration.GetSection("Text2SqlOpenAI").Get<Text2SqlOpenAIOption>();
builder.Configuration.GetSection("Text2SqlConnection").Get<Text2SqlConnectionOption>();

//可是传入自定义Kernel，如果不传则使用默认Kernel
builder.Services.AddText2SqlNet();

// 添加MCP服务支持
builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithTools<Text2SqlMcpTool>();

//用户service中获取httpcontext
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// 初始化MCP上下文帮助类
var httpContextAccessor = app.Services.GetRequiredService<IHttpContextAccessor>();
Text2SqlMcpContextHelper.Initialize(httpContextAccessor);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();

app.UseAuthorization();

app.MapControllers();

// 添加MCP端点映射到指定路径，避免与根路径冲突
app.UseEndpoints(endpoints =>
{
    endpoints.MapMcp("/mcp");
});

// 设置Blazor回退路由，确保根路径正确响应
app.MapFallbackToPage("/_Host");

app.Run();
