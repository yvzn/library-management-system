using Microsoft.Extensions.Localization;

// Enforces the RootNamespace as project name is not a valid .NET identifier (it has hyphens)
// and the <RootNamespace> property in the .csproj file does not seem to be honored by Localization extensions
// https://learn.microsoft.com/en-us/aspnet/core/fundamentals/localization/provide-resources#rootnamespaceattribute
[assembly: RootNamespace("library_management_system")]
