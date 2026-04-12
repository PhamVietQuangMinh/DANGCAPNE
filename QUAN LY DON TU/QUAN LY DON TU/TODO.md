# TODO: Separate IT Management from General Management

## Step 1: Create ITController.cs [PENDING]
- Extract IT-specific actions from AdminController: ToggleUserLock, SendResetPasswordOtp, ResetPasswordWithOtp, ResetRegisteredLoginIp, ResetBiometricEnrollment, ClearTrustedDevice, Whitelist.
- Add IT dashboard (audits, online sessions, assets).
- Role guards: IT/ITManager only.

## Step 2: Refactor AdminController.cs [PENDING]
- Remove IT actions (moved to ITController).
- Focus on general management: Employee directory, HR tabs (no tech resets).
- Update Index tabs & Can* methods.

## Step 3: Update ModulesController.cs [PENDING]
- Filter IT modules to ITController or stricter roles.

## Step 4: Create IT Views [PENDING]
- Views/IT/Index.cshtml (dashboard).
- Views/IT/Whitelist.cshtml, etc.

## Step 5: Update Program.cs [PENDING]
- Add routes if needed.

## Step 6: Test & Complete [PENDING]
- Login tests: IT user → ITController only; HR/Manager → Admin only.
- `dotnet run` & verify.

**Progress: 0/6**
