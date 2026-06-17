# UI Dialog Architecture Plan

## New Structure

```
Components/
  Notifications/
    # Building Blocks (generic, reusable)
    DialogBase.razor              # Foundation with @bind-IsOpen support
    ConfirmDialog.razor           # Used by IConfirmDialogService
    AlertDialog.razor             # Used by IAlertDialogService
    PromptDialog.razor            # Used by IPromptDialogService
    DialogContainer.razor         # Renders active dialogs from services
    ToastContainer.razor
    
    Enums/
      DialogSize.cs
      DialogIconVariant.cs
      DialogVariant.cs
      
    Models/
      ToastMessage.cs
      ToastType.cs
      ConfirmDialogRequest.cs
      AlertDialogRequest.cs
      PromptDialogRequest.cs
      
    Interfaces/
      IConfirmDialogService.cs
      IAlertDialogService.cs
      IPromptDialogService.cs
      IToastService.cs
      
    Services/
      ConfirmDialogService.cs
      AlertDialogService.cs
      PromptDialogService.cs
      ToastService.cs
      
    Extensions/
      ServiceCollectionExtensions.cs

  Shared/
    Dialogs/
      SecretDialog.razor          # Shared, might be reused
      SecretDialogRequest.cs

Pages/
  Account/
    Settings/
      ApiKeys/
        ApiKeysPage.razor
        CreateApiKeyDialog.razor  # Feature-specific
```

---

## DialogBase Changes

Add two-way binding support:

```csharp
[Parameter]
public bool IsOpen { get; set; }

[Parameter]
public EventCallback<bool> IsOpenChanged { get; set; }

[Parameter]
public EventCallback OnClose { get; set; }

private async Task Close()
{
    IsOpen = false;
    await IsOpenChanged.InvokeAsync(false);
    await OnClose.InvokeAsync();
}
```

---

## Interface Separation

```csharp
public interface IConfirmDialogService
{
    Task<bool> ConfirmAsync(string message, string? title = null, 
        DialogVariant variant = DialogVariant.Default,
        string confirmText = "Confirm", string cancelText = "Cancel");
}

public interface IAlertDialogService
{
    Task AlertAsync(string message, string? title = null, string okText = "OK");
}

public interface IPromptDialogService
{
    Task<string?> PromptAsync(string message, string? title = null,
        string? placeholder = null, string? defaultValue = null,
        string confirmText = "OK", string cancelText = "Cancel");
}
```

---

## Usage Examples

**Global confirm (via service):**
```razor
@inject IConfirmDialogService ConfirmDialog

var confirmed = await ConfirmDialog.ConfirmAsync("Delete this item?");
```

**Shared dialog (local component with @bind-IsOpen):**
```razor
<SecretDialog @bind-IsOpen="_showSecret" Secret="@_apiKey" />

<Button OnClick="() => _showSecret = true">Show Secret</Button>
```

**Feature dialog (local component with @bind-IsOpen):**
```razor
<CreateApiKeyDialog @bind-IsOpen="_showCreate" OnResult="HandleResult" />

<Button OnClick="() => _showCreate = true">Create Key</Button>
```

**Inline custom (using DialogBase directly):**
```razor
<DialogBase @bind-IsOpen="_showCustom" Title="Custom">
    <BodyContent>Custom content here</BodyContent>
    <FooterContent>
        <Button OnClick="() => _showCustom = false">Close</Button>
    </FooterContent>
</DialogBase>

<Button OnClick="() => _showCustom = true">Open</Button>
```

---

## Design Rules

| Dialog Type | Location | How to Open |
|-------------|----------|-------------|
| Confirm/Alert/Prompt | `Components/Notifications/` | Via `IConfirmDialogService`, `IAlertDialogService`, `IPromptDialogService` |
| Shared (reusable) | `Components/Shared/Dialogs/` | Local component, `@bind-IsOpen` pattern |
| Feature-specific | With the page | Local component, `@bind-IsOpen` pattern |
| Custom inline | N/A | Use `DialogBase` directly with `@bind-IsOpen` |

---

## Tasks

1. ~~Update `DialogBase` to support `@bind-IsOpen` (add `IsOpenChanged`)~~ ✅
2. ~~Create `Components/Shared/Dialogs/` folder~~ ✅
3. ~~Split `IDialogService` into `IConfirmDialogService`, `IAlertDialogService`, `IPromptDialogService`~~ ✅
4. ~~Create separate service implementations for each~~ ✅
5. ~~Update `DialogContainer` to use the three services~~ ✅
6. ~~Move `SecretDialog` to `Components/Shared/Dialogs/` with `@bind-IsOpen` pattern~~ ✅
7. ~~Move `CreateApiKeyDialog` to `Pages/Account/` with `@bind-IsOpen` pattern~~ ✅
8. ~~Update `ApiKeysPage` to use local dialog pattern~~ ✅
9. ~~Delete old `IDialogService` and `DialogService`~~ ✅
10. ~~Update `ServiceCollectionExtensions` to register three services~~ ✅
11. ~~Build and verify~~ ✅
