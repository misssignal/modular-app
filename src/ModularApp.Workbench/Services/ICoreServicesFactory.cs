using ModularApp.Sdk;

namespace ModularApp.Workbench.Services;

public interface ICoreServicesFactory
{
    ICoreServices CreateForModule(string moduleId, string moduleDirectory);
}
