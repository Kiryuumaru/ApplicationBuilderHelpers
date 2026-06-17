using Application.AppEnvironment.Services;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Credential.Interfaces.Inbound;

/// <summary>
/// Service for retrieving application credentials based on environment.
/// </summary>
public interface ICredentialService
{
    /// <summary>
    /// Gets credentials for a specific environment tag.
    /// </summary>
    Task<Models.Credentials> GetCredentials(string envTag, CancellationToken cancellationToken);

    /// <summary>
    /// Gets credentials for the current environment.
    /// </summary>
    Task<Models.Credentials> GetCredentials(CancellationToken cancellationToken);
}
