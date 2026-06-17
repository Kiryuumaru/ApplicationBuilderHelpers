/**
 * WebAuthn interop module for Blazor.
 * Provides methods to interact with the Web Authentication API.
 */
window.webAuthnInterop = {
    /**
     * Checks if WebAuthn is supported by the browser.
     * @returns {boolean} True if WebAuthn is supported, false otherwise.
     */
    isSupported: function () {
        return window.PublicKeyCredential !== undefined;
    },

    /**
     * Creates a new passkey credential (registration).
     * @param {string} optionsJson - The JSON-serialized PublicKeyCredentialCreationOptions from the server.
     * @returns {Promise<string|null>} The JSON-serialized attestation response, or null if cancelled/failed.
     */
    createCredential: async function (optionsJson) {
        if (!window.PublicKeyCredential) {
            throw new Error("WebAuthn is not supported in this browser.");
        }

        try {
            const options = JSON.parse(optionsJson);

            // Convert base64url-encoded values to ArrayBuffer
            options.challenge = this._base64UrlToArrayBuffer(options.challenge);
            options.user.id = this._base64UrlToArrayBuffer(options.user.id);

            if (options.excludeCredentials) {
                options.excludeCredentials = options.excludeCredentials.map(cred => ({
                    ...cred,
                    id: this._base64UrlToArrayBuffer(cred.id)
                }));
            }

            const credential = await navigator.credentials.create({ publicKey: options });

            if (!credential) {
                return null;
            }

            // Serialize the response back to JSON
            const response = {
                id: credential.id,
                rawId: this._arrayBufferToBase64Url(credential.rawId),
                type: credential.type,
                response: {
                    clientDataJSON: this._arrayBufferToBase64Url(credential.response.clientDataJSON),
                    attestationObject: this._arrayBufferToBase64Url(credential.response.attestationObject)
                }
            };

            // Include authenticator data if available
            if (credential.response.getTransports) {
                response.response.transports = credential.response.getTransports();
            }

            if (credential.authenticatorAttachment) {
                response.authenticatorAttachment = credential.authenticatorAttachment;
            }

            return JSON.stringify(response);
        } catch (error) {
            if (error.name === "NotAllowedError") {
                // User cancelled or denied the request
                return null;
            }
            throw error;
        }
    },

    /**
     * Gets an existing passkey credential (authentication).
     * @param {string} optionsJson - The JSON-serialized PublicKeyCredentialRequestOptions from the server.
     * @returns {Promise<string|null>} The JSON-serialized assertion response, or null if cancelled/failed.
     */
    getCredential: async function (optionsJson) {
        if (!window.PublicKeyCredential) {
            throw new Error("WebAuthn is not supported in this browser.");
        }

        try {
            const options = JSON.parse(optionsJson);

            // Convert base64url-encoded values to ArrayBuffer
            options.challenge = this._base64UrlToArrayBuffer(options.challenge);

            if (options.allowCredentials) {
                options.allowCredentials = options.allowCredentials.map(cred => ({
                    ...cred,
                    id: this._base64UrlToArrayBuffer(cred.id)
                }));
            }

            const credential = await navigator.credentials.get({ publicKey: options });

            if (!credential) {
                return null;
            }

            // Serialize the response back to JSON
            const response = {
                id: credential.id,
                rawId: this._arrayBufferToBase64Url(credential.rawId),
                type: credential.type,
                response: {
                    clientDataJSON: this._arrayBufferToBase64Url(credential.response.clientDataJSON),
                    authenticatorData: this._arrayBufferToBase64Url(credential.response.authenticatorData),
                    signature: this._arrayBufferToBase64Url(credential.response.signature)
                }
            };

            // Include user handle if present (for resident keys)
            if (credential.response.userHandle) {
                response.response.userHandle = this._arrayBufferToBase64Url(credential.response.userHandle);
            }

            if (credential.authenticatorAttachment) {
                response.authenticatorAttachment = credential.authenticatorAttachment;
            }

            return JSON.stringify(response);
        } catch (error) {
            if (error.name === "NotAllowedError") {
                // User cancelled or denied the request
                return null;
            }
            throw error;
        }
    },

    /**
     * Converts a base64url-encoded string to an ArrayBuffer.
     * @param {string} base64url - The base64url-encoded string.
     * @returns {ArrayBuffer} The decoded ArrayBuffer.
     */
    _base64UrlToArrayBuffer: function (base64url) {
        // Replace base64url characters with base64 characters
        let base64 = base64url.replace(/-/g, '+').replace(/_/g, '/');

        // Add padding if necessary
        while (base64.length % 4 !== 0) {
            base64 += '=';
        }

        const binaryString = atob(base64);
        const bytes = new Uint8Array(binaryString.length);
        for (let i = 0; i < binaryString.length; i++) {
            bytes[i] = binaryString.charCodeAt(i);
        }
        return bytes.buffer;
    },

    /**
     * Converts an ArrayBuffer to a base64url-encoded string.
     * @param {ArrayBuffer} buffer - The ArrayBuffer to encode.
     * @returns {string} The base64url-encoded string.
     */
    _arrayBufferToBase64Url: function (buffer) {
        const bytes = new Uint8Array(buffer);
        let binaryString = '';
        for (let i = 0; i < bytes.length; i++) {
            binaryString += String.fromCharCode(bytes[i]);
        }
        const base64 = btoa(binaryString);

        // Convert base64 to base64url
        return base64.replace(/\+/g, '-').replace(/\//g, '_').replace(/=/g, '');
    }
};
