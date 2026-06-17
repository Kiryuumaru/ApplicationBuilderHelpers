using System.Reflection;
using Domain.Authorization.Constants;

namespace Application.UnitTests.Authorization;

/// <summary>
/// Merciless regression audit for the inherited-parameter emission contract in
/// <see cref="PermissionIds"/>. These tests exist because earlier tests only ever
/// exercised <c>WithTenantId</c> on <c>Forensics.Read</c>: <c>tenantId</c> was the
/// FIRST inherited parameter and so the single static factory the emitter generated
/// happened to work. Every other inherited parameter was only reachable as an
/// instance method on a <c>ScopeBuilder</c> that had already been initialized via
/// <c>WithTenantId</c>. A leaf like <c>Timeline.WithIncidentId(...)</c> failed to
/// compile and no test noticed.
///
/// The audit enforces:
///   1. For every emitted permission class with parameter constants, every parameter
///      MUST be reachable as a <c>public static With{Param}(string) -&gt; ScopeBuilder</c>
///      chain-starter on that class (not just the first one).
///   2. The corresponding <c>PermissionRequestBuilder</c> (where present) MUST expose
///      <c>With{Param}(string) -&gt; PermissionRequestBuilder</c> for every parameter.
///   3. The <c>ScopeBuilder</c> struct emitted inside the permission class MUST expose
///      <c>With{Param}(string) -&gt; ScopeBuilder</c> instance methods for every parameter.
///   4. Each parameter is independently usable as a chain starter and the emitted
///      directive preserves the call order verbatim.
/// </summary>
public sealed class PermissionIdsInheritedParameterAuditTests
{
    private const string ParameterConstantSuffix = "Parameter";

    #region Reflection-based completeness audit (the bug-killer test)

    [Fact]
    public void Audit_EveryParameterConstant_HasMatchingStaticWithFactory()
    {
        var failures = new List<string>();

        foreach (var type in EnumeratePermissionTypes(typeof(PermissionIds)))
        {
            // Only classes that emit a ScopeBuilder are actionable (scope/leaf classes).
            // Intermediate Node classes emit *Parameter constants for documentation but
            // have no builder API of their own - chains start on their child scope/leaf.
            if (type.GetNestedType("ScopeBuilder", BindingFlags.Public) is null)
            {
                continue;
            }

            var parameters = GetParameterConstants(type);
            if (parameters.Count == 0)
            {
                continue;
            }

            foreach (var (paramConstName, paramValue) in parameters)
            {
                var methodName = "With" + paramConstName[..^ParameterConstantSuffix.Length];

                var method = type.GetMethod(
                    methodName,
                    BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly,
                    binder: null,
                    types: new[] { typeof(string) },
                    modifiers: null);

                if (method is null)
                {
                    failures.Add($"{type.FullName} declares parameter '{paramValue}' ({paramConstName}) but has no public static {methodName}(string) factory.");
                    continue;
                }

                if (!typeof(string).IsAssignableFrom(method.GetParameters()[0].ParameterType))
                {
                    failures.Add($"{type.FullName}.{methodName} must accept a single string parameter.");
                }

                // Returns ScopeBuilder (a nested struct on the same type). Confirm by name.
                if (method.ReturnType.Name != "ScopeBuilder")
                {
                    failures.Add($"{type.FullName}.{methodName} must return ScopeBuilder (got {method.ReturnType.FullName}).");
                }
            }
        }

        Assert.True(
            failures.Count == 0,
            "Missing static With{Param} factories on permission classes:\n  - " + string.Join("\n  - ", failures));
    }

    [Fact]
    public void Audit_EveryParameterConstant_HasMatchingScopeBuilderInstanceMethod()
    {
        var failures = new List<string>();

        foreach (var type in EnumeratePermissionTypes(typeof(PermissionIds)))
        {
            var scopeBuilderType = type.GetNestedType("ScopeBuilder", BindingFlags.Public);
            if (scopeBuilderType is null)
            {
                // Intermediate Node - no builder API by design.
                continue;
            }

            var parameters = GetParameterConstants(type);
            if (parameters.Count == 0)
            {
                // A class with a ScopeBuilder but no parameter constants is structurally invalid.
                failures.Add($"{type.FullName} has a ScopeBuilder but no *Parameter constants - parameters were not propagated by the emitter.");
                continue;
            }

            foreach (var (paramConstName, paramValue) in parameters)
            {
                var methodName = "With" + paramConstName[..^ParameterConstantSuffix.Length];

                var method = scopeBuilderType.GetMethod(
                    methodName,
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly,
                    binder: null,
                    types: new[] { typeof(string) },
                    modifiers: null);

                if (method is null)
                {
                    failures.Add($"{type.FullName}.ScopeBuilder is missing instance method {methodName}(string) for parameter '{paramValue}'.");
                }
                else if (method.ReturnType.Name != "ScopeBuilder")
                {
                    failures.Add($"{type.FullName}.ScopeBuilder.{methodName} must return ScopeBuilder (got {method.ReturnType.FullName}).");
                }
            }
        }

        Assert.True(
            failures.Count == 0,
            "Missing ScopeBuilder.With{Param} instance methods:\n  - " + string.Join("\n  - ", failures));
    }

    [Fact]
    public void Audit_EveryParameterConstant_HasMatchingPermissionRequestBuilderInstanceMethod()
    {
        var failures = new List<string>();

        foreach (var type in EnumeratePermissionTypes(typeof(PermissionIds)))
        {
            // Only leaf-permission classes get a PermissionRequestBuilder.
            var permissionRequestBuilderType = type.GetNestedType("PermissionRequestBuilder", BindingFlags.Public);
            if (permissionRequestBuilderType is null)
            {
                continue;
            }

            var parameters = GetParameterConstants(type);
            foreach (var (paramConstName, paramValue) in parameters)
            {
                var methodName = "With" + paramConstName[..^ParameterConstantSuffix.Length];

                var method = permissionRequestBuilderType.GetMethod(
                    methodName,
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly,
                    binder: null,
                    types: new[] { typeof(string) },
                    modifiers: null);

                if (method is null)
                {
                    failures.Add($"{type.FullName}.PermissionRequestBuilder is missing instance method {methodName}(string) for parameter '{paramValue}'.");
                }
                else if (method.ReturnType.Name != "PermissionRequestBuilder")
                {
                    failures.Add($"{type.FullName}.PermissionRequestBuilder.{methodName} must return PermissionRequestBuilder (got {method.ReturnType.FullName}).");
                }
            }
        }

        Assert.True(
            failures.Count == 0,
            "Missing PermissionRequestBuilder.With{Param} instance methods:\n  - " + string.Join("\n  - ", failures));
    }

    [Fact]
    public void Audit_EveryInheritedParameter_IsRepresentedAsParameterConstantOnDescendant()
    {
        // If a Node declares parameters, every descendant permission class MUST expose
        // matching *Parameter constants. This catches the case where the emitter forgot
        // to thread combinedParameters into a child's WriteParameterConstants call.

        var failures = new List<string>();

        // Walk: any permission class that has a nested ScopeBuilder or PermissionRequestBuilder
        // must declare *Parameter constants for every parameter its ancestors' parameter set
        // makes reachable on its builder structs.
        foreach (var type in EnumeratePermissionTypes(typeof(PermissionIds)))
        {
            var scopeBuilder = type.GetNestedType("ScopeBuilder", BindingFlags.Public);
            if (scopeBuilder is null)
            {
                continue;
            }

            // Collect all With{X} instance methods on the ScopeBuilder.
            var builderParams = scopeBuilder
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(m => m.Name.StartsWith("With", StringComparison.Ordinal)
                         && m.GetParameters().Length == 1
                         && m.GetParameters()[0].ParameterType == typeof(string))
                .Select(m => m.Name)
                .ToHashSet(StringComparer.Ordinal);

            var declaredParamConstants = GetParameterConstants(type)
                .Select(kvp => "With" + kvp.ConstantName[..^ParameterConstantSuffix.Length])
                .ToHashSet(StringComparer.Ordinal);

            foreach (var builderMethod in builderParams)
            {
                if (!declaredParamConstants.Contains(builderMethod))
                {
                    failures.Add($"{type.FullName}.ScopeBuilder.{builderMethod} exists but the owning class lacks the matching *Parameter constant.");
                }
            }
        }

        Assert.True(
            failures.Count == 0,
            "Parameter constants not propagated to descendants:\n  - " + string.Join("\n  - ", failures));
    }

    private static IEnumerable<Type> EnumeratePermissionTypes(Type root)
    {
        foreach (var nested in root.GetNestedTypes(BindingFlags.Public))
        {
            if (nested.IsClass && nested.IsAbstract && nested.IsSealed)
            {
                yield return nested;
                foreach (var deeper in EnumeratePermissionTypes(nested))
                {
                    yield return deeper;
                }
            }
        }
    }

    private static List<(string ConstantName, string Value)> GetParameterConstants(Type type)
    {
        var result = new List<(string, string)>();
        foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly))
        {
            if (!field.IsLiteral || field.FieldType != typeof(string))
            {
                continue;
            }
            if (!field.Name.EndsWith(ParameterConstantSuffix, StringComparison.Ordinal))
            {
                continue;
            }
            // Skip the rare false positive: there is no non-parameter constant with this suffix today,
            // but guard against it explicitly by skipping a 0-length prefix.
            if (field.Name.Length == ParameterConstantSuffix.Length)
            {
                continue;
            }

            var value = (string?)field.GetRawConstantValue();
            if (value is null)
            {
                continue;
            }
            result.Add((field.Name, value));
        }
        return result;
    }

    #endregion

    #region Parameter constant emission

    [Fact]
    public void ParameterConstant_OnLeaf_InheritedFromAncestor_IsEmitted()
    {
        // `me` inherits userId from `auth`. The *Parameter constant must be emitted on Me itself.
        Assert.Equal("userId", PermissionIds.Api.Auth.Me.UserIdParameter);
    }

    [Fact]
    public void ParameterConstant_OnLeaf_LocalParameter_IsEmitted()
    {
        // `users:list` has userId declared on its parent `users` Node. The constant must be on List.
        Assert.Equal("userId", PermissionIds.Api.Iam.Users.List.UserIdParameter);
    }

#if DEBUG
    [Fact]
    public void ParameterConstant_OnLeaf_MultipleAncestorsAndLocal_AllEmitted()
    {
        // `forensics:upload` has: tenantId (root), datasetId + region (forensics), hash (own).
        Assert.Equal("tenantId", PermissionIds.SecOpsDebug.V1.Forensics.Upload.TenantIdParameter);
        Assert.Equal("datasetId", PermissionIds.SecOpsDebug.V1.Forensics.Upload.DatasetIdParameter);
        Assert.Equal("region", PermissionIds.SecOpsDebug.V1.Forensics.Upload.RegionParameter);
        Assert.Equal("hash", PermissionIds.SecOpsDebug.V1.Forensics.Upload.HashParameter);
    }
#endif

    [Fact]
    public void ParameterConstant_AllParametersList_ContainsEveryDeclaredParameter()
    {
        // Cross-check that the global AllParameters list contains userId (declared on `auth` and `users`).
        Assert.Contains("userId", PermissionIds.AllParameters);
    }

    #endregion

    #region Each parameter as chain-starter (the bug the original tests missed)

#if DEBUG
    [Fact]
    public void ChainStarter_TenantId_OnForensicsUpload_ProducesCorrectDirective()
    {
        var directive = PermissionIds.SecOpsDebug.V1.Forensics.Upload
            .WithTenantId("t1").Allow();
        Assert.Equal("allow;sec_ops_debug:v1:forensics:upload;tenantId=t1", directive);
    }

    [Fact]
    public void ChainStarter_DatasetId_OnForensicsUpload_ProducesCorrectDirective()
    {
        // This is the test that would have failed pre-fix: datasetId is NOT the first
        // combined parameter on `upload`, so the old emitter never emitted it as a
        // static factory. Calling `.WithDatasetId(...)` directly was a compile error.
        var directive = PermissionIds.SecOpsDebug.V1.Forensics.Upload
            .WithDatasetId("ds-1").Allow();
        Assert.Equal("allow;sec_ops_debug:v1:forensics:upload;datasetId=ds-1", directive);
    }

    [Fact]
    public void ChainStarter_Region_OnForensicsUpload_ProducesCorrectDirective()
    {
        var directive = PermissionIds.SecOpsDebug.V1.Forensics.Upload
            .WithRegion("us-east-1").Allow();
        Assert.Equal("allow;sec_ops_debug:v1:forensics:upload;region=us-east-1", directive);
    }

    [Fact]
    public void ChainStarter_Hash_OnForensicsUpload_ProducesCorrectDirective()
    {
        var directive = PermissionIds.SecOpsDebug.V1.Forensics.Upload
            .WithHash("sha256:deadbeef").Allow();
        Assert.Equal("allow;sec_ops_debug:v1:forensics:upload;hash=sha256:deadbeef", directive);
    }

    [Fact]
    public void ChainStarter_EachOfFourParameters_OnForensicsUpload_StartsValidChain()
    {
        // All four parameters MUST be reachable as chain-starters on a leaf that combines
        // 1 root-inherited (tenantId), 2 ancestor-inherited (datasetId, region), and 1 local (hash).
        var tenant = PermissionIds.SecOpsDebug.V1.Forensics.Upload.WithTenantId("t").Allow();
        var dataset = PermissionIds.SecOpsDebug.V1.Forensics.Upload.WithDatasetId("d").Allow();
        var region = PermissionIds.SecOpsDebug.V1.Forensics.Upload.WithRegion("r").Allow();
        var hash = PermissionIds.SecOpsDebug.V1.Forensics.Upload.WithHash("h").Allow();

        Assert.Contains("tenantId=t", tenant);
        Assert.Contains("datasetId=d", dataset);
        Assert.Contains("region=r", region);
        Assert.Contains("hash=h", hash);
    }

    [Fact]
    public void ChainStarter_EachOfFourParameters_OnPlaybooksDeploy_StartsValidChain()
    {
        // Inherited tenantId + 3 local (playbookId, deviceId, appId).
        var tenant = PermissionIds.SecOpsDebug.V1.Playbooks.Deploy.WithTenantId("t").Allow();
        var playbook = PermissionIds.SecOpsDebug.V1.Playbooks.Deploy.WithPlaybookId("p").Allow();
        var device = PermissionIds.SecOpsDebug.V1.Playbooks.Deploy.WithDeviceId("d").Allow();
        var app = PermissionIds.SecOpsDebug.V1.Playbooks.Deploy.WithAppId("a").Allow();

        Assert.Equal("allow;sec_ops_debug:v1:playbooks:deploy;tenantId=t", tenant);
        Assert.Equal("allow;sec_ops_debug:v1:playbooks:deploy;playbookId=p", playbook);
        Assert.Equal("allow;sec_ops_debug:v1:playbooks:deploy;deviceId=d", device);
        Assert.Equal("allow;sec_ops_debug:v1:playbooks:deploy;appId=a", app);
    }

    [Fact]
    public void ChainStarter_EachOfFiveParameters_OnIncidentWorkspaceContainment_StartsValidChain()
    {
        // The deepest worst case in the tree:
        //   tenantId (sec_ops_debug)
        //   incidentId, region (incident_workspace)
        //   deviceId, approvalCode (containment local)
        // All five MUST be reachable as chain-starters.
        var tenant = PermissionIds.SecOpsDebug.V1.IncidentWorkspace.Containment.WithTenantId("t").Allow();
        var incident = PermissionIds.SecOpsDebug.V1.IncidentWorkspace.Containment.WithIncidentId("i").Allow();
        var region = PermissionIds.SecOpsDebug.V1.IncidentWorkspace.Containment.WithRegion("r").Allow();
        var device = PermissionIds.SecOpsDebug.V1.IncidentWorkspace.Containment.WithDeviceId("d").Allow();
        var approval = PermissionIds.SecOpsDebug.V1.IncidentWorkspace.Containment.WithApprovalCode("a").Allow();

        Assert.Equal("allow;sec_ops_debug:v1:incident_workspace:containment;tenantId=t", tenant);
        Assert.Equal("allow;sec_ops_debug:v1:incident_workspace:containment;incidentId=i", incident);
        Assert.Equal("allow;sec_ops_debug:v1:incident_workspace:containment;region=r", region);
        Assert.Equal("allow;sec_ops_debug:v1:incident_workspace:containment;deviceId=d", device);
        Assert.Equal("allow;sec_ops_debug:v1:incident_workspace:containment;approvalCode=a", approval);
    }
#endif

    #endregion

    #region Permutation / order-preservation tests

#if DEBUG
    [Fact]
    public void Permutation_ChainOrderIsPreservedInDirective()
    {
        // The directive must encode parameters in the exact order they were chained,
        // independent of declaration order. This proves _params concatenation has no
        // hidden reordering or de-dup that could mask a bug.
        var aThenB = PermissionIds.SecOpsDebug.V1.Forensics.Upload
            .WithHash("h")
            .WithTenantId("t")
            .Allow();
        var bThenA = PermissionIds.SecOpsDebug.V1.Forensics.Upload
            .WithTenantId("t")
            .WithHash("h")
            .Allow();

        Assert.Equal("allow;sec_ops_debug:v1:forensics:upload;hash=h;tenantId=t", aThenB);
        Assert.Equal("allow;sec_ops_debug:v1:forensics:upload;tenantId=t;hash=h", bThenA);
    }

    [Fact]
    public void Permutation_AllFiveParametersChained_PreservesOrderExactly()
    {
        var directive = PermissionIds.SecOpsDebug.V1.IncidentWorkspace.Containment
            .WithApprovalCode("ac")
            .WithDeviceId("dev")
            .WithRegion("eu-west-1")
            .WithIncidentId("inc-99")
            .WithTenantId("tnt")
            .Allow();

        Assert.Equal(
            "allow;sec_ops_debug:v1:incident_workspace:containment;approvalCode=ac;deviceId=dev;region=eu-west-1;incidentId=inc-99;tenantId=tnt",
            directive);
    }

    [Fact]
    public void Permutation_RepeatedWithCall_AppendsRatherThanReplaces()
    {
        // Document the current builder semantics: With{Param} called twice produces
        // two key=value pairs in the directive. If this ever changes to "replace",
        // this test will fail loudly and the change must be considered breaking.
        var directive = PermissionIds.SecOpsDebug.V1.Forensics.Upload
            .WithHash("first")
            .WithHash("second")
            .Allow();

        Assert.Equal("allow;sec_ops_debug:v1:forensics:upload;hash=first;hash=second", directive);
    }
#endif

    #endregion

    #region PermissionRequestBuilder chain-starter equivalents

#if DEBUG
    [Fact]
    public void PermissionRequestBuilder_EveryParameter_OnForensicsUpload_IsChainable()
    {
        var byTenant = PermissionIds.SecOpsDebug.V1.Forensics.Upload.Permission.WithTenantId("t").ToString();
        var byDataset = PermissionIds.SecOpsDebug.V1.Forensics.Upload.Permission.WithDatasetId("d").ToString();
        var byRegion = PermissionIds.SecOpsDebug.V1.Forensics.Upload.Permission.WithRegion("r").ToString();
        var byHash = PermissionIds.SecOpsDebug.V1.Forensics.Upload.Permission.WithHash("h").ToString();

        Assert.Equal("sec_ops_debug:v1:forensics:upload;tenantId=t", byTenant);
        Assert.Equal("sec_ops_debug:v1:forensics:upload;datasetId=d", byDataset);
        Assert.Equal("sec_ops_debug:v1:forensics:upload;region=r", byRegion);
        Assert.Equal("sec_ops_debug:v1:forensics:upload;hash=h", byHash);
    }

    [Fact]
    public void PermissionRequestBuilder_FullChain_PreservesAllParametersAndOrder()
    {
        var perm = PermissionIds.SecOpsDebug.V1.IncidentWorkspace.Containment.Permission
            .WithTenantId("t")
            .WithIncidentId("i")
            .WithRegion("r")
            .WithDeviceId("d")
            .WithApprovalCode("a")
            .ToString();

        Assert.Equal(
            "sec_ops_debug:v1:incident_workspace:containment;tenantId=t;incidentId=i;region=r;deviceId=d;approvalCode=a",
            perm);
    }
#endif

    #endregion

    #region Local-only and zero-parameter sanity

    [Fact]
    public void NoInheritance_LeafWithoutParameters_HasNoScopeBuilder()
    {
        // `api:iam:roles:list` lives under a parameterless Node and has no local params.
        // Its containing class must NOT expose a ScopeBuilder nested type.
        var type = typeof(PermissionIds.Api.Iam.Roles.List);
        var scopeBuilder = type.GetNestedType("ScopeBuilder", BindingFlags.Public);
        Assert.Null(scopeBuilder);
    }

    [Fact]
    public void NoInheritance_LeafWithoutParameters_AllowIsBareDirective()
    {
        var directive = PermissionIds.Api.Iam.Roles.List.Allow();
        Assert.Equal("allow;api:iam:roles:list", directive);
    }

    [Fact]
    public void NoInheritance_LeafWithoutParameters_PermissionToStringIsBarePath()
    {
        var perm = PermissionIds.Api.Iam.Roles.List.Permission.ToString();
        Assert.Equal("api:iam:roles:list", perm);
    }

    #endregion

    #region Boundary value tests

    [Fact]
    public void BoundaryValue_EmptyStringValue_StillProducesParameterSuffix()
    {
        // Empty value is not validated by the builder. The emitted directive will have
        // "userId=" - a trailing-empty value. Document this behavior so a future change
        // that decides to filter empty values is recognized as a breaking change.
        var directive = PermissionIds.Api.Auth.Me.WithUserId("").Allow();
        Assert.Equal("allow;api:auth:me;userId=", directive);
    }

    [Fact]
    public void BoundaryValue_ValueWithSemicolon_IsEmittedVerbatim()
    {
        // The builder does NOT escape special directive characters. A value containing
        // ';' will corrupt the directive grammar at parse time. This test pins down the
        // current unescaped behavior; a future change adding escaping would be breaking.
        var directive = PermissionIds.Api.Auth.Me.WithUserId("a;b").Allow();
        Assert.Equal("allow;api:auth:me;userId=a;b", directive);
    }

    [Fact]
    public void BoundaryValue_ValueWithEquals_IsEmittedVerbatim()
    {
        var directive = PermissionIds.Api.Auth.Me.WithUserId("k=v").Allow();
        Assert.Equal("allow;api:auth:me;userId=k=v", directive);
    }

    [Fact]
    public void BoundaryValue_VeryLongValue_IsEmittedWithoutTruncation()
    {
        var longValue = new string('x', 4096);
        var directive = PermissionIds.Api.Auth.Me.WithUserId(longValue).Allow();
        Assert.EndsWith(";userId=" + longValue, directive);
        Assert.StartsWith("allow;api:auth:me;userId=", directive);
    }

    #endregion
}
