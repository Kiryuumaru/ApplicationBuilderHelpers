using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace ApplicationBuilderHelpers;

public static class ApplicationRuntime
{
    public static IConfiguration Configuration { get; internal set; } = null!;
}
