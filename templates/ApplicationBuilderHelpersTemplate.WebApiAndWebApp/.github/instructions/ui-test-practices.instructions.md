---
applyTo: '**/*Tests.cs'
---
# UI Test Rules

## Core Principle

UI tests use mouse clicks and keyboard input only. No API calls, no header injection, no bypassing client-side flows.

---

## Prohibited Patterns

- NEVER use `HttpClient.PostAsJsonAsync()` in tests (bypasses UI)
- NEVER use `HttpClient.GetAsync()` in tests (bypasses UI)
- NEVER use `request.Headers.Authorization` in tests (bypasses token storage)
- NEVER use `new AuthenticationHeaderValue("Bearer", token)` in tests (bypasses auth flow)
- NEVER use `Task.Delay(n)` in tests (flaky, use explicit waits)
- NEVER expose `HttpClient` in test base class (enables API bypass)

---

## Required UI Actions

- MUST fill input using `Page.Locator("#email").FillAsync(value)`
- MUST click button using `Page.Locator("button[type='submit']").ClickAsync()`
- MUST navigate using `Page.Locator("a[href='/profile']").ClickAsync()`
- MUST check checkbox using `Page.Locator("#terms").CheckAsync()`

---

## Required Wait Strategies

- For URL change, USE `Page.WaitForURLAsync(url => url.Contains("/dashboard"))`
- For element visible, USE `Page.Locator("[data-testid='profile']").WaitForAsync()`
- For element hidden, USE `Page.Locator("[data-testid='spinner']").WaitForAsync(new() { State = WaitForSelectorState.Hidden })`

---

## Selector Priority

1. PREFER `[data-testid='submit-login']` - Test ID attributes
2. PREFER `#email`, `#password` - Form element IDs
3. PREFER `button[type='submit']` - Semantic selectors
4. PREFER `GetByText("Sign out")` - Text content (when unique)

---

## Required data-testid Attributes

- User menu MUST have `data-testid="user-menu"`
- Sign out button MUST have `data-testid="sign-out"`
- Login form MUST have `data-testid="login-form"`
- Submit button MUST have `data-testid="submit-login"`
- Success message MUST have `data-testid="success-message"`
- Error message MUST have `data-testid="error-message"`
- Loading spinner MUST have `data-testid="loading-spinner"`
- Profile username MUST have `data-testid="profile-username"`
- Profile email MUST have `data-testid="profile-email"`

---

## Pre-Commit Verification

- MUST have zero `HttpClient` usage in test code
- MUST have zero `Authorization` header manipulation
- MUST use only `ClickAsync()`, `FillAsync()`, `TypeAsync()` for actions
- MUST use condition-based waits, not `Task.Delay()`
- MUST verify Login -> Profile -> Data visible works without 401
