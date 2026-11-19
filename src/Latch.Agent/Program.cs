// 에이전트 진입점: pair/reset 명령 처리 또는 Windows 서비스로 실행
using Latch.Agent;
using Latch.Agent.Enforcement;

switch (args)
{
    case ["pair", var url, var code]:
        return await Commands.PairAsync(url, code);
    case ["reset"]:
        return Commands.Reset();
}

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddWindowsService(o => o.ServiceName = "LatchAgent");
builder.Services.AddSingleton<FirewallBlocker>();
builder.Services.AddSingleton<LockUiLauncher>();
builder.Services.AddSingleton<PolicyEnforcer>();
builder.Services.AddHostedService<AgentWorker>();

builder.Build().Run();
return 0;
