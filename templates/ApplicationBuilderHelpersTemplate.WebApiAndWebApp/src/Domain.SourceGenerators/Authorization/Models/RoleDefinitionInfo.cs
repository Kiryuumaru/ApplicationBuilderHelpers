using System.Collections.Immutable;

namespace Domain.SourceGenerators.Authorization.Models;

internal sealed class RoleDefinitionInfo
{
	public RoleDefinitionInfo(string code, string name, ImmutableArray<string> templateParameters)
	{
		Code = code;
		Name = name;
		TemplateParameters = templateParameters;
	}

	public string Code { get; }
	public string Name { get; }
	public ImmutableArray<string> TemplateParameters { get; }
}
